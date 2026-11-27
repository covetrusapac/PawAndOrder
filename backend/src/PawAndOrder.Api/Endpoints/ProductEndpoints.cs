using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PawAndOrder.Api.Shared;
using PawAndOrder.Application;
using PawAndOrder.Domain;
using PawAndOrder.Infrastructure;

namespace PawAndOrder.Api.Endpoints;

internal static class ProductEndpoints
{
    internal static void MapProductEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/products");

        group.MapGet("", async (string? search, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            IQueryable<Product> query = db.Products.AsNoTracking().Include(x => x.Lots).Where(x => x.IsActive);
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => x.Name.Contains(search) || x.Sku.Contains(search));
            }

            var items = await query.OrderBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    x.Sku,
                    x.Name,
                    x.Type,
                    x.TrackInventory,
                    x.DefaultSalePriceExGst,
                    x.ReorderThreshold,
                    quantityOnHand = x.Lots.Sum(l => l.QuantityOnHand),
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        });

        group.MapGet("/{id:int}", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var product = await db.Products.AsNoTracking()
                .Include(x => x.Lots)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (product is null)
            {
                return Results.NotFound();
            }

            var transactions = await db.InventoryTransactions.AsNoTracking()
                .Where(x => x.ProductId == product.Id)
                .OrderByDescending(x => x.CreatedAt)
                .Take(20)
                .Select(x => new { x.Id, x.Type, x.QuantityDelta, x.ReferenceType, x.ReferenceId, x.CreatedAt, x.InventoryLotId })
                .ToListAsync(cancellationToken);

            return Results.Ok(new
            {
                product.Id,
                product.Sku,
                product.Name,
                product.Type,
                product.Description,
                product.UnitOfMeasure,
                product.TrackInventory,
                product.IsTaxable,
                product.DefaultSalePriceExGst,
                product.DefaultCostPriceExGst,
                product.ReorderThreshold,
                quantityOnHand = product.Lots.Sum(x => x.QuantityOnHand),
                lots = product.Lots.OrderBy(x => x.ExpiryDate).ThenBy(x => x.ReceivedDate).Select(x => new { x.Id, x.BatchNumber, x.ExpiryDate, x.ReceivedDate, x.UnitCostExGst, x.QuantityReceived, x.QuantityOnHand }),
                transactions,
            });
        });

        group.MapPost("", async (ProductUpsertRequest request, IValidator<ProductUpsertRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var product = new Product
            {
                Sku = request.Sku,
                Name = request.Name,
                Type = request.Type,
                Description = request.Description,
                UnitOfMeasure = request.UnitOfMeasure,
                TrackInventory = request.TrackInventory,
                IsTaxable = request.IsTaxable,
                DefaultSalePriceExGst = request.DefaultSalePriceExGst,
                DefaultCostPriceExGst = request.DefaultCostPriceExGst,
                ReorderThreshold = request.ReorderThreshold,
            };

            db.Products.Add(product);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/products/{product.Id}", new { product.Id });
        });
    }
}
