using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PawAndOrder.Api.Shared;
using PawAndOrder.Application;
using PawAndOrder.Domain;
using PawAndOrder.Infrastructure;

namespace PawAndOrder.Api.Endpoints;

internal static class AppointmentEndpoints
{
    internal static void MapAppointmentEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/appointments");

        group.MapGet("", async (DateTimeOffset? from, DateTimeOffset? to, int? clinicianId, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var rangeStart = from ?? DateTimeOffset.UtcNow.Date;
            var rangeEnd = to ?? rangeStart.AddDays(7);

            IQueryable<Appointment> query = db.Appointments.AsNoTracking()
                .Include(x => x.Patient)
                .Include(x => x.Client)
                .Include(x => x.Clinician)
                .Include(x => x.AppointmentType)
                .Where(x => x.StartAt >= rangeStart && x.StartAt <= rangeEnd);

            if (clinicianId.HasValue)
            {
                query = query.Where(x => x.ClinicianUserId == clinicianId.Value);
            }

            var items = await query.OrderBy(x => x.StartAt)
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
                    appointmentType = x.AppointmentType!.Name,
                    appointmentTypeColor = x.AppointmentType!.ColorHex,
                    consultId = x.Consult != null ? x.Consult.Id : (int?)null,
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        });

        group.MapPost("", async (AppointmentUpsertRequest request, IValidator<AppointmentUpsertRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var appointment = new Appointment
            {
                PatientId = request.PatientId,
                ClientId = request.ClientId,
                ClinicianUserId = request.ClinicianUserId,
                AppointmentTypeId = request.AppointmentTypeId,
                StartAt = request.StartAt,
                EndAt = request.EndAt,
                Status = request.Status,
                Reason = request.Reason,
                Notes = request.Notes,
            };

            db.Appointments.Add(appointment);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/appointments/{appointment.Id}", new { appointment.Id });
        });

        group.MapPut("/{id:int}", async (int id, AppointmentUpsertRequest request, IValidator<AppointmentUpsertRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var appointment = await db.Appointments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (appointment is null)
            {
                return Results.NotFound();
            }

            appointment.PatientId = request.PatientId;
            appointment.ClientId = request.ClientId;
            appointment.ClinicianUserId = request.ClinicianUserId;
            appointment.AppointmentTypeId = request.AppointmentTypeId;
            appointment.StartAt = request.StartAt;
            appointment.EndAt = request.EndAt;
            appointment.Status = request.Status;
            appointment.Reason = request.Reason;
            appointment.Notes = request.Notes;
            appointment.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { appointment.Id });
        });

        group.MapPost("/{id:int}/arrive", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var appointment = await db.Appointments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (appointment is null)
            {
                return Results.NotFound();
            }

            appointment.Status = AppointmentStatus.Arrived;
            appointment.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { appointment.Id, appointment.Status });
        });

        group.MapPost("/{id:int}/start-consult", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var appointment = await db.Appointments.Include(x => x.Consult).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (appointment is null)
            {
                return Results.NotFound();
            }

            appointment.Status = AppointmentStatus.InConsult;
            if (appointment.Consult is null)
            {
                appointment.Consult = new Consult
                {
                    AppointmentId = appointment.Id,
                    PatientId = appointment.PatientId,
                    ClinicianUserId = appointment.ClinicianUserId,
                    VisitReason = appointment.Reason,
                    StartedAt = DateTimeOffset.UtcNow,
                };
            }

            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { consultId = appointment.Consult.Id });
        });

        group.MapPost("/{id:int}/cancel", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var appointment = await db.Appointments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (appointment is null)
            {
                return Results.NotFound();
            }

            appointment.Status = AppointmentStatus.Cancelled;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { appointment.Id, appointment.Status });
        });
    }
}
