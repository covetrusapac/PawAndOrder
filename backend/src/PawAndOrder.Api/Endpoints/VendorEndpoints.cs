using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PawAndOrder.Api.Shared;
using PawAndOrder.Application;
using PawAndOrder.Domain;
using PawAndOrder.Infrastructure;

namespace PawAndOrder.Api.Endpoints;

internal static class VendorEndpoints
{
    internal static void MapVendorEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/vendors");

        group.MapGet("", async (PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var items = await db.Vendors.AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name, x.Phone, x.Email, x.ContactName, openOrders = x.PurchaseOrders.Count(po => po.Status != PurchaseOrderStatus.Received && po.Status != PurchaseOrderStatus.Cancelled) })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        });

        group.MapPost("", async (VendorUpsertRequest request, IValidator<VendorUpsertRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var vendor = new Vendor
            {
                Name = request.Name,
                Phone = request.Phone,
                Email = request.Email,
                ContactName = request.ContactName,
                Notes = request.Notes,
            };

            db.Vendors.Add(vendor);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/vendors/{vendor.Id}", new { vendor.Id });
        });
    }
}
