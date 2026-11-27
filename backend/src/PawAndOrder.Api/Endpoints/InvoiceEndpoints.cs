using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PawAndOrder.Api.Shared;
using PawAndOrder.Application;
using PawAndOrder.Domain;
using PawAndOrder.Infrastructure;

namespace PawAndOrder.Api.Endpoints;

internal static class InvoiceEndpoints
{
    internal static void MapInvoiceEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/invoices");

        group.MapPost("", async (DirectInvoiceCreateRequest request, IValidator<DirectInvoiceCreateRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.PatientId && x.ClientId == request.ClientId, cancellationToken);
            if (patient is null)
            {
                return Results.BadRequest("The selected patient does not belong to the selected client.");
            }

            var invoiceCount = await db.Invoices.CountAsync(cancellationToken);
            var invoice = new Invoice
            {
                InvoiceNumber = $"INV-{1000 + invoiceCount + 1}",
                ClientId = request.ClientId,
                PatientId = request.PatientId,
                Status = InvoiceStatus.Draft,
                IssuedAt = DateTimeOffset.UtcNow,
            };

            SeedData.RecalculateInvoice(invoice);
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/invoices/{invoice.Id}", new { invoice.Id, invoice.InvoiceNumber });
        });

        group.MapGet("", async (string? search, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            IQueryable<Invoice> query = db.Invoices.AsNoTracking().Include(x => x.Client).Include(x => x.Patient);
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => x.InvoiceNumber.Contains(search) || x.Patient!.Name.Contains(search) || (x.Client!.FirstName + " " + x.Client.LastName).Contains(search));
            }

            var items = await query.OrderByDescending(x => x.IssuedAt).Take(50)
                .Select(x => new
                {
                    x.Id,
                    x.InvoiceNumber,
                    x.Status,
                    x.TotalIncGst,
                    x.BalanceDue,
                    x.IssuedAt,
                    patient = x.Patient!.Name,
                    client = x.Client!.FirstName + " " + x.Client!.LastName,
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        });

        group.MapGet("/{id:int}", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var invoice = await db.Invoices.AsNoTracking()
                .Include(x => x.Client)
                .Include(x => x.Patient)
                .Include(x => x.Lines).ThenInclude(x => x.InventoryLot)
                .Include(x => x.Payments)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (invoice is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new
            {
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.Status,
                invoice.IssuedAt,
                invoice.SubtotalExGst,
                invoice.TotalGst,
                invoice.TotalIncGst,
                invoice.AmountPaid,
                invoice.BalanceDue,
                client = invoice.Client is null ? null : invoice.Client.FirstName + " " + invoice.Client.LastName,
                patient = invoice.Patient?.Name,
                lines = invoice.Lines.Select(x => new { x.Id, x.Description, x.LineType, x.Quantity, x.UnitPriceExGst, x.LineSubtotalExGst, x.GstAmount, x.LineTotalIncGst, lot = x.InventoryLot != null ? x.InventoryLot.BatchNumber : null }),
                payments = invoice.Payments.OrderByDescending(x => x.PaidAt).Select(x => new { x.Id, x.PaymentMethod, x.Amount, x.PaidAt, x.Reference, x.Notes }),
            });
        });

        group.MapPost("/{id:int}/lines", async (int id, InvoiceLineRequest request, IValidator<InvoiceLineRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var invoice = await db.Invoices.Include(x => x.Lines).Include(x => x.Payments).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (invoice is null)
            {
                return Results.NotFound();
            }

            Product? product = null;
            InventoryLot? lot = null;
            if (request.ProductId.HasValue)
            {
                product = await db.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId.Value, cancellationToken);
                if (product is null)
                {
                    return Results.BadRequest("The selected product was not found.");
                }

                if (product.TrackInventory)
                {
                    lot = await InventoryAllocation.ResolveLotAsync(db, product.Id, request.InventoryLotId, request.Quantity, cancellationToken);
                    if (lot is null)
                    {
                        return Results.BadRequest("Unable to allocate stock for the selected line item.");
                    }

                    lot.QuantityOnHand -= request.Quantity;
                    db.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = product.Id,
                        InventoryLotId = lot.Id,
                        Type = InventoryTransactionType.Dispense,
                        QuantityDelta = -request.Quantity,
                        ReferenceType = "InvoiceLine",
                        ReferenceId = invoice.Id.ToString(),
                        Notes = $"POS line added to {invoice.InvoiceNumber}",
                    });
                }
            }

            var isTaxable = product?.IsTaxable ?? request.IsTaxable;
            var unitPriceExGst = product?.DefaultSalePriceExGst ?? request.UnitPriceExGst;
            var description = string.IsNullOrWhiteSpace(request.Description) ? product?.Name ?? string.Empty : request.Description;
            var lineType = product is not null ? (product.TrackInventory ? InvoiceLineType.Product : InvoiceLineType.Service) : request.LineType;

            var line = InvoiceLineBuilder.BuildInvoiceLineFromValues(product?.Id, description, lineType, request.Quantity, unitPriceExGst, isTaxable, lot?.Id);
            line.StockDispensed = lot is not null;
            invoice.Lines.Add(line);
            SeedData.RecalculateInvoice(invoice);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(new { lineId = line.Id, invoiceId = invoice.Id, invoice.TotalIncGst, invoice.BalanceDue });
        });

        group.MapDelete("/{id:int}/lines/{lineId:int}", async (int id, int lineId, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var invoice = await db.Invoices.Include(x => x.Lines).Include(x => x.Payments).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (invoice is null)
            {
                return Results.NotFound();
            }

            var line = invoice.Lines.FirstOrDefault(x => x.Id == lineId);
            if (line is null)
            {
                return Results.NotFound();
            }

            if (line.StockDispensed && line.ProductId.HasValue && line.InventoryLotId.HasValue)
            {
                var lot = await db.InventoryLots.FirstOrDefaultAsync(x => x.Id == line.InventoryLotId.Value, cancellationToken);
                if (lot is not null)
                {
                    lot.QuantityOnHand += line.Quantity;
                    db.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = line.ProductId.Value,
                        InventoryLotId = lot.Id,
                        Type = InventoryTransactionType.Correction,
                        QuantityDelta = line.Quantity,
                        ReferenceType = "InvoiceLineRemoval",
                        ReferenceId = invoice.Id.ToString(),
                        Notes = $"Removed line from {invoice.InvoiceNumber}",
                    });
                }
            }

            db.InvoiceLines.Remove(line);
            SeedData.RecalculateInvoice(invoice);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { invoice.Id, invoice.TotalIncGst, invoice.BalanceDue });
        });

        group.MapPost("/{id:int}/payments", async (int id, PaymentRequest request, IValidator<PaymentRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var invoice = await db.Invoices.Include(x => x.Payments).Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (invoice is null)
            {
                return Results.NotFound();
            }

            if (request.Amount > invoice.BalanceDue)
            {
                return Results.BadRequest("Payment amount exceeds the remaining balance.");
            }

            invoice.Payments.Add(new Payment
            {
                InvoiceId = invoice.Id,
                PaymentMethod = request.PaymentMethod,
                Amount = request.Amount,
                PaidAt = DateTimeOffset.UtcNow,
                Reference = request.Reference,
                Notes = request.Notes,
            });

            invoice.Status = InvoiceStatus.Issued;
            SeedData.RecalculateInvoice(invoice);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { invoice.Id, invoice.Status, invoice.BalanceDue });
        });

        group.MapPost("/{id:int}/issue", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var invoice = await db.Invoices.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (invoice is null)
            {
                return Results.NotFound();
            }

            invoice.Status = InvoiceStatus.Issued;
            invoice.IssuedAt = invoice.IssuedAt ?? DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { invoice.Id, invoice.Status });
        });
    }
}
