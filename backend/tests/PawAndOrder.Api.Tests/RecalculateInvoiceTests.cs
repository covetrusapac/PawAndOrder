using PawAndOrder.Domain;
using PawAndOrder.Infrastructure;

namespace PawAndOrder.Api.Tests;

public class RecalculateInvoiceTests
{
    [Fact]
    public void EmptyDraftInvoice_StaysDraft()
    {
        var invoice = new Invoice { Status = InvoiceStatus.Draft };

        SeedData.RecalculateInvoice(invoice);

        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
    }

    [Fact]
    public void FullyPaidInvoiceWithLines_IsPaid()
    {
        var invoice = new Invoice { Status = InvoiceStatus.Issued };
        invoice.Lines.Add(new InvoiceLine { LineTotalIncGst = 11m });
        invoice.Payments.Add(new Payment { Amount = 11m });

        SeedData.RecalculateInvoice(invoice);

        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(0m, invoice.BalanceDue);
    }
}
