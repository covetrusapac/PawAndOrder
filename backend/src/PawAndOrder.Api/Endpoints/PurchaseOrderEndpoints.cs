using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PawAndOrder.Api.Shared;
using PawAndOrder.Application;
using PawAndOrder.Domain;
using PawAndOrder.Infrastructure;

namespace PawAndOrder.Api.Endpoints;

internal static class PurchaseOrderEndpoints
{
    internal static void MapPurchaseOrderEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/purchase-orders");

        group.MapGet("", async (PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var items = await db.PurchaseOrders.AsNoTracking()
                .Include(x => x.Vendor)
                .Include(x => x.Lines)
                .OrderByDescending(x => x.OrderedAt)
                .Select(x => new
                {
                    x.Id,
                    x.OrderNumber,
                    vendor = x.Vendor!.Name,
                    x.Status,
                    x.OrderedAt,
                    x.ExpectedAt,
                    totalLines = x.Lines.Count,
                    totalOrdered = x.Lines.Sum(l => l.QuantityOrdered),
                    totalReceived = x.Lines.Sum(l => l.QuantityReceived),
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        });

        group.MapGet("/{id:int}", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var order = await db.PurchaseOrders.AsNoTracking()
                .Include(x => x.Vendor)
                .Include(x => x.Lines).ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (order is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new
            {
                order.Id,
                order.OrderNumber,
                order.Status,
                order.OrderedAt,
                order.ExpectedAt,
                order.ReceivedAt,
                order.Notes,
                vendor = order.Vendor is null ? null : new { order.Vendor.Id, order.Vendor.Name },
                lines = order.Lines.Select(x => new { x.Id, x.ProductId, product = x.Product!.Name, x.QuantityOrdered, x.QuantityReceived, x.UnitCostExGst, x.BatchNumber, x.ExpiryDate }),
            });
        });

        group.MapPost("", async (PurchaseOrderUpsertRequest request, IValidator<PurchaseOrderUpsertRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var count = await db.PurchaseOrders.CountAsync(cancellationToken);
            var order = new PurchaseOrder
            {
                OrderNumber = $"PO-{1000 + count + 1}",
                VendorId = request.VendorId,
                ExpectedAt = request.ExpectedAt,
                Notes = request.Notes,
                Status = PurchaseOrderStatus.Draft,
            };

            foreach (var line in request.Lines)
            {
                order.Lines.Add(new PurchaseOrderLine
                {
                    ProductId = line.ProductId,
                    QuantityOrdered = line.QuantityOrdered,
                    UnitCostExGst = line.UnitCostExGst,
                    BatchNumber = line.BatchNumber,
                    ExpiryDate = line.ExpiryDate,
                });
            }

            db.PurchaseOrders.Add(order);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/purchase-orders/{order.Id}", new { order.Id });
        });

        group.MapPost("/{id:int}/submit", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var order = await db.PurchaseOrders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (order is null)
            {
                return Results.NotFound();
            }

            order.Status = PurchaseOrderStatus.Submitted;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { order.Id, order.Status });
        });

        group.MapPost("/{id:int}/receive", async (int id, ReceivePurchaseOrderRequest request, IValidator<ReceivePurchaseOrderRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var order = await db.PurchaseOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (order is null)
            {
                return Results.NotFound();
            }

            foreach (var receivedLine in request.Lines)
            {
                var line = order.Lines.FirstOrDefault(x => x.Id == receivedLine.PurchaseOrderLineId);
                if (line is null)
                {
                    return Results.BadRequest($"Purchase order line {receivedLine.PurchaseOrderLineId} does not belong to this order.");
                }

                line.QuantityReceived += receivedLine.QuantityReceived;
                line.BatchNumber = receivedLine.BatchNumber;
                line.ExpiryDate = receivedLine.ExpiryDate;

                var lot = new InventoryLot
                {
                    ProductId = line.ProductId,
                    BatchNumber = receivedLine.BatchNumber,
                    ExpiryDate = receivedLine.ExpiryDate,
                    ReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    UnitCostExGst = line.UnitCostExGst,
                    QuantityReceived = receivedLine.QuantityReceived,
                    QuantityOnHand = receivedLine.QuantityReceived,
                };
                db.InventoryLots.Add(lot);
                await db.SaveChangesAsync(cancellationToken);

                db.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = line.ProductId,
                    InventoryLotId = lot.Id,
                    Type = InventoryTransactionType.Receive,
                    QuantityDelta = receivedLine.QuantityReceived,
                    ReferenceType = "PurchaseOrder",
                    ReferenceId = order.OrderNumber,
                    Notes = "Received against purchase order",
                });
            }

            order.ReceivedAt = DateTimeOffset.UtcNow;
            order.Status = order.Lines.All(x => x.QuantityReceived >= x.QuantityOrdered)
                ? PurchaseOrderStatus.Received
                : PurchaseOrderStatus.PartiallyReceived;

            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { order.Id, order.Status });
        });
    }
}
