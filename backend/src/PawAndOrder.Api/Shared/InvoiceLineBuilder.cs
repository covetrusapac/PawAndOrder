using PawAndOrder.Application;
using PawAndOrder.Domain;

namespace PawAndOrder.Api.Shared;

// Both constructors delegate the GST arithmetic to PricingMath so line rules are defined once.
internal static class InvoiceLineBuilder
{
    internal static InvoiceLine BuildInvoiceLine(Product product, decimal quantity, int? inventoryLotId, string? descriptionOverride = null)
    {
        var (subtotal, gstAmount, total) = PricingMath.Calculate(quantity, product.DefaultSalePriceExGst, product.IsTaxable);
        return BuildInvoiceLineFromValues(
            product.Id,
            descriptionOverride ?? product.Name,
            product.TrackInventory ? InvoiceLineType.Product : InvoiceLineType.Service,
            quantity,
            product.DefaultSalePriceExGst,
            product.IsTaxable,
            inventoryLotId);
    }

    internal static InvoiceLine BuildInvoiceLineFromValues(int? productId, string description, InvoiceLineType lineType, decimal quantity, decimal unitPriceExGst, bool isTaxable, int? inventoryLotId)
    {
        var (subtotal, gstAmount, total) = PricingMath.Calculate(quantity, unitPriceExGst, isTaxable);
        return new InvoiceLine
        {
            ProductId = productId,
            Description = description,
            LineType = lineType,
            Quantity = quantity,
            UnitPriceExGst = unitPriceExGst,
            LineSubtotalExGst = subtotal,
            GstRate = isTaxable ? PricingMath.GstRate : 0m,
            GstAmount = gstAmount,
            LineTotalIncGst = total,
            IsTaxable = isTaxable,
            InventoryLotId = inventoryLotId,
        };
    }
}
