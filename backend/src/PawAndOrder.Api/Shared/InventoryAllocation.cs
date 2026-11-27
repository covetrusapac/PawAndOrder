using Microsoft.EntityFrameworkCore;
using PawAndOrder.Domain;
using PawAndOrder.Infrastructure;

namespace PawAndOrder.Api.Shared;

// Dispensing policy: honour an explicitly requested lot only if it is a valid candidate;
// otherwise allocate the earliest expiry (undated last), then the earliest received.
internal static class InventoryAllocation
{
    internal static async Task<InventoryLot?> ResolveLotAsync(PawAndOrderDbContext db, int productId, int? requestedLotId, decimal quantity, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        IQueryable<InventoryLot> query = db.InventoryLots.Where(x =>
            x.ProductId == productId &&
            x.QuantityOnHand >= quantity &&
            (x.ExpiryDate == null || x.ExpiryDate >= today));

        if (requestedLotId.HasValue)
        {
            return await query.FirstOrDefaultAsync(x => x.Id == requestedLotId.Value, cancellationToken);
        }

        return await query.OrderBy(x => x.ExpiryDate ?? DateOnly.MaxValue).ThenBy(x => x.ReceivedDate).FirstOrDefaultAsync(cancellationToken);
    }
}
