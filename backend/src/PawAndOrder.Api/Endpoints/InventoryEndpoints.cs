using Microsoft.EntityFrameworkCore;
using PawAndOrder.Domain;
using PawAndOrder.Infrastructure;

namespace PawAndOrder.Api.Endpoints;

internal static class InventoryEndpoints
{
    internal static void MapInventoryEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/inventory");

        group.MapGet("/stock", async (PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var items = await db.Products.AsNoTracking()
                .Include(x => x.Lots)
                .Where(x => x.TrackInventory)
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    x.Sku,
                    x.Name,
                    x.Type,
                    x.ReorderThreshold,
                    quantityOnHand = x.Lots.Sum(l => l.QuantityOnHand),
                    nextExpiry = x.Lots.Where(l => l.QuantityOnHand > 0 && l.ExpiryDate != null).OrderBy(l => l.ExpiryDate).Select(l => l.ExpiryDate).FirstOrDefault(),
                    lowStock = x.Lots.Sum(l => l.QuantityOnHand) <= x.ReorderThreshold,
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        });

        group.MapGet("/lots", async (int? productId, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            IQueryable<InventoryLot> query = db.InventoryLots.AsNoTracking().Include(x => x.Product);
            if (productId.HasValue)
            {
                query = query.Where(x => x.ProductId == productId.Value);
            }

            var items = await query.OrderBy(x => x.ExpiryDate).ThenBy(x => x.ReceivedDate)
                .Select(x => new { x.Id, x.ProductId, product = x.Product!.Name, x.BatchNumber, x.ExpiryDate, x.ReceivedDate, x.UnitCostExGst, x.QuantityReceived, x.QuantityOnHand })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        });

        group.MapPost("/adjustments", async (int productId, int? inventoryLotId, decimal quantityDelta, string? notes, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var product = await db.Products.Include(x => x.Lots).FirstOrDefaultAsync(x => x.Id == productId, cancellationToken);
            if (product is null)
            {
                return Results.BadRequest("Product not found.");
            }

            InventoryLot? lot = null;
            if (product.TrackInventory)
            {
                lot = inventoryLotId.HasValue
                    ? await db.InventoryLots.FirstOrDefaultAsync(x => x.Id == inventoryLotId.Value, cancellationToken)
                    : product.Lots.OrderBy(x => x.ExpiryDate).ThenBy(x => x.ReceivedDate).FirstOrDefault();

                if (lot is null)
                {
                    return Results.BadRequest("No inventory lot found for adjustment.");
                }

                if (lot.QuantityOnHand + quantityDelta < 0)
                {
                    return Results.BadRequest("Adjustment would produce negative stock.");
                }

                lot.QuantityOnHand += quantityDelta;
            }

            db.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = productId,
                InventoryLotId = lot?.Id,
                Type = InventoryTransactionType.Adjustment,
                QuantityDelta = quantityDelta,
                ReferenceType = "ManualAdjustment",
                ReferenceId = Guid.NewGuid().ToString("N"),
                Notes = notes,
            });

            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { productId, inventoryLotId = lot?.Id, quantityDelta });
        });
    }
}
