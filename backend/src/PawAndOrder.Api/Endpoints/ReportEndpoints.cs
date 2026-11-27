using Microsoft.EntityFrameworkCore;
using PawAndOrder.Domain;
using PawAndOrder.Infrastructure;

namespace PawAndOrder.Api.Endpoints;

internal static class ReportEndpoints
{
    internal static void MapReportEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/reports");

        group.MapGet("/dashboard", async (PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var today = DateTimeOffset.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var todaysAppointments = await db.Appointments.AsNoTracking()
                .Include(x => x.Patient)
                .Include(x => x.Client)
                .Include(x => x.Clinician)
                .Where(x => x.StartAt >= today && x.StartAt < tomorrow)
                .OrderBy(x => x.StartAt)
                .Select(x => new
                {
                    x.Id,
                    x.StartAt,
                    x.EndAt,
                    x.Status,
                    x.Reason,
                    x.Notes,
                    patient = x.Patient!.Name,
                    client = x.Client!.FirstName + " " + x.Client!.LastName,
                    clinician = x.Clinician!.DisplayName,
                    consultId = x.Consult != null ? x.Consult.Id : (int?)null,
                })
                .ToListAsync(cancellationToken);

            var openInvoices = await db.Invoices.AsNoTracking().CountAsync(x => x.BalanceDue > 0, cancellationToken);
            var inConsult = todaysAppointments.Count(x => x.Status == AppointmentStatus.InConsult);

            return Results.Ok(new
            {
                metrics = new
                {
                    todaysAppointments = todaysAppointments.Count,
                    arrivalsWaiting = todaysAppointments.Count(x => x.Status == AppointmentStatus.Arrived),
                    inConsult,
                    openInvoices,
                },
                todaysAppointments,
            });
        });

        group.MapGet("/daily-summary", async (DateOnly? date, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var target = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var start = new DateTimeOffset(target.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            var end = start.AddDays(1);

            var appointments = await db.Appointments.AsNoTracking().CountAsync(x => x.StartAt >= start && x.StartAt < end, cancellationToken);
            var revenue = await db.Invoices.AsNoTracking().Where(x => x.IssuedAt >= start && x.IssuedAt < end).SumAsync(x => (decimal?)x.TotalIncGst, cancellationToken) ?? 0m;
            var paid = await db.Payments.AsNoTracking().Where(x => x.PaidAt >= start && x.PaidAt < end).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

            return Results.Ok(new { date = target, appointments, revenue, paid });
        });

        group.MapGet("/revenue", async (DateOnly? from, DateOnly? to, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var start = new DateTimeOffset((from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30))).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            var end = new DateTimeOffset((to ?? DateOnly.FromDateTime(DateTime.UtcNow)).AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

            var items = await db.Invoices.AsNoTracking()
                .Where(x => x.IssuedAt >= start && x.IssuedAt < end)
                .GroupBy(x => DateOnly.FromDateTime(x.IssuedAt!.Value.UtcDateTime))
                .Select(x => new { date = x.Key, total = x.Sum(i => i.TotalIncGst), invoiceCount = x.Count() })
                .OrderBy(x => x.date)
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        });

        group.MapGet("/appointments", async (DateOnly? from, DateOnly? to, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var start = new DateTimeOffset((from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7))).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            var end = new DateTimeOffset((to ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))).AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

            var items = await db.Appointments.AsNoTracking()
                .Where(x => x.StartAt >= start && x.StartAt < end)
                .GroupBy(x => x.Status)
                .Select(x => new { status = x.Key, count = x.Count() })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        });

        group.MapGet("/low-stock", async (PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var items = await db.Products.AsNoTracking()
                .Include(x => x.Lots)
                .Where(x => x.TrackInventory)
                .Select(x => new { x.Id, x.Name, onHand = x.Lots.Sum(l => l.QuantityOnHand), x.ReorderThreshold })
                .Where(x => x.onHand <= x.ReorderThreshold)
                .OrderBy(x => x.onHand)
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        });

        group.MapGet("/product-usage", async (DateOnly? from, DateOnly? to, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var start = new DateTimeOffset((from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30))).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            var end = new DateTimeOffset((to ?? DateOnly.FromDateTime(DateTime.UtcNow)).AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

            var items = await db.InvoiceLines.AsNoTracking()
                .Where(x => x.Invoice!.IssuedAt >= start && x.Invoice.IssuedAt < end)
                .GroupBy(x => x.Description)
                .Select(x => new { name = x.Key, quantity = x.Sum(i => i.Quantity), revenue = x.Sum(i => i.LineTotalIncGst) })
                .OrderByDescending(x => x.revenue)
                .Take(10)
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        });
    }
}
