using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PawAndOrder.Api.Shared;
using PawAndOrder.Application;
using PawAndOrder.Domain;
using PawAndOrder.Infrastructure;

namespace PawAndOrder.Api.Endpoints;

internal static class ConsultEndpoints
{
    internal static void MapConsultEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/consults");

        group.MapPost("", async (ConsultUpsertRequest request, IValidator<ConsultUpsertRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var consult = new Consult
            {
                AppointmentId = request.AppointmentId,
                PatientId = request.PatientId,
                ClinicianUserId = request.ClinicianUserId,
                VisitReason = request.VisitReason,
                PresentingComplaint = request.PresentingComplaint,
                SubjectiveNotes = request.SubjectiveNotes,
                ObjectiveNotes = request.ObjectiveNotes,
                AssessmentNotes = request.AssessmentNotes,
                PlanNotes = request.PlanNotes,
                FollowUpNotes = request.FollowUpNotes,
                WeightKg = request.WeightKg,
                TemperatureC = request.TemperatureC,
                HeartRateBpm = request.HeartRateBpm,
                RespiratoryRateBpm = request.RespiratoryRateBpm,
            };

            db.Consults.Add(consult);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/consults/{consult.Id}", new { consult.Id });
        });

        group.MapGet("/{id:int}", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var consult = await db.Consults.AsNoTracking()
                .Include(x => x.Patient)!.ThenInclude(x => x!.Client)
                .Include(x => x.Clinician)
                .Include(x => x.Diagnoses)
                .Include(x => x.Procedures)
                .Include(x => x.Medications).ThenInclude(x => x.Product)
                .Include(x => x.Medications).ThenInclude(x => x.InventoryLot)
                .Include(x => x.Vaccinations).ThenInclude(x => x.VaccinationType)
                .Include(x => x.Invoice)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (consult is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new
            {
                consult.Id,
                consult.AppointmentId,
                consult.PatientId,
                consult.ClinicianUserId,
                consult.VisitReason,
                consult.PresentingComplaint,
                consult.SubjectiveNotes,
                consult.ObjectiveNotes,
                consult.AssessmentNotes,
                consult.PlanNotes,
                consult.FollowUpNotes,
                consult.WeightKg,
                consult.TemperatureC,
                consult.HeartRateBpm,
                consult.RespiratoryRateBpm,
                consult.Status,
                consult.StartedAt,
                consult.CompletedAt,
                patient = consult.Patient is null ? null : new
                {
                    consult.Patient.Id,
                    consult.Patient.Name,
                    consult.Patient.Species,
                    consult.Patient.Breed,
                    consult.Patient.WeightKg,
                    owner = consult.Patient.Client is null ? null : consult.Patient.Client.FirstName + " " + consult.Patient.Client.LastName,
                },
                clinician = consult.Clinician?.DisplayName,
                diagnoses = consult.Diagnoses.OrderBy(x => x.SortOrder).Select(x => new { x.Id, x.DiagnosisCatalogItemId, x.DiagnosisText }),
                procedures = consult.Procedures.OrderBy(x => x.SortOrder).Select(x => new { x.Id, x.ProcedureCatalogItemId, x.ProcedureText, x.Quantity, x.Notes }),
                medications = consult.Medications.Select(x => new { x.Id, x.ProductId, productName = x.Product!.Name, x.InventoryLotId, lot = x.InventoryLot != null ? x.InventoryLot.BatchNumber : null, x.Quantity, x.DoseInstructions, x.Notes }),
                vaccinations = consult.Vaccinations.Select(x => new { x.Id, x.VaccinationTypeId, name = x.VaccinationType!.Name, x.DateAdministered, x.DueDate, x.InventoryLotId, x.Notes }),
                invoice = consult.Invoice is null ? null : new { consult.Invoice.Id, consult.Invoice.InvoiceNumber, consult.Invoice.Status, consult.Invoice.TotalIncGst },
            });
        });

        group.MapPut("/{id:int}", async (int id, ConsultUpsertRequest request, IValidator<ConsultUpsertRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var consult = await db.Consults.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (consult is null)
            {
                return Results.NotFound();
            }

            consult.VisitReason = request.VisitReason;
            consult.PresentingComplaint = request.PresentingComplaint;
            consult.SubjectiveNotes = request.SubjectiveNotes;
            consult.ObjectiveNotes = request.ObjectiveNotes;
            consult.AssessmentNotes = request.AssessmentNotes;
            consult.PlanNotes = request.PlanNotes;
            consult.FollowUpNotes = request.FollowUpNotes;
            consult.WeightKg = request.WeightKg;
            consult.TemperatureC = request.TemperatureC;
            consult.HeartRateBpm = request.HeartRateBpm;
            consult.RespiratoryRateBpm = request.RespiratoryRateBpm;
            consult.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { consult.Id });
        });

        group.MapPost("/{id:int}/diagnoses", async (int id, ConsultDiagnosisRequest request, IValidator<ConsultDiagnosisRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var consult = await db.Consults.Include(x => x.Diagnoses).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (consult is null)
            {
                return Results.NotFound();
            }

            var diagnosisCatalogItemId = request.DiagnosisCatalogItemId;
            if (request.SaveToCatalog && request.DiagnosisCatalogItemId is null)
            {
                var catalogItem = new DiagnosisCatalogItem { Name = request.DiagnosisText };
                db.DiagnosisCatalogItems.Add(catalogItem);
                await db.SaveChangesAsync(cancellationToken);
                diagnosisCatalogItemId = catalogItem.Id;
            }

            var item = new ConsultDiagnosis
            {
                ConsultId = id,
                DiagnosisCatalogItemId = diagnosisCatalogItemId,
                DiagnosisText = request.DiagnosisText,
                SortOrder = consult.Diagnoses.Count + 1,
            };
            db.ConsultDiagnoses.Add(item);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { item.Id });
        });

        group.MapPost("/{id:int}/procedures", async (int id, ConsultProcedureRequest request, IValidator<ConsultProcedureRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var consult = await db.Consults.Include(x => x.Procedures).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (consult is null)
            {
                return Results.NotFound();
            }

            var procedureCatalogItemId = request.ProcedureCatalogItemId;
            if (request.SaveToCatalog && request.ProcedureCatalogItemId is null)
            {
                var catalogItem = new ProcedureCatalogItem { Name = request.ProcedureText, DefaultProductId = request.DefaultProductId };
                db.ProcedureCatalogItems.Add(catalogItem);
                await db.SaveChangesAsync(cancellationToken);
                procedureCatalogItemId = catalogItem.Id;
            }

            var item = new ConsultProcedure
            {
                ConsultId = id,
                ProcedureCatalogItemId = procedureCatalogItemId,
                ProcedureText = request.ProcedureText,
                Quantity = request.Quantity,
                Notes = request.Notes,
                SortOrder = consult.Procedures.Count + 1,
            };

            db.ConsultProcedures.Add(item);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { item.Id });
        });

        group.MapPost("/{id:int}/medications", async (int id, ConsultMedicationRequest request, IValidator<ConsultMedicationRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var consult = await db.Consults.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (consult is null)
            {
                return Results.NotFound();
            }

            var product = await db.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);
            if (product is null)
            {
                return Results.BadRequest("Selected product could not be found.");
            }

            InventoryLot? lot = null;
            if (product.TrackInventory)
            {
                lot = await InventoryAllocation.ResolveLotAsync(db, product.Id, request.InventoryLotId, request.Quantity, cancellationToken);
                if (lot is null)
                {
                    return Results.BadRequest("Unable to allocate stock for the selected medication.");
                }

                lot.QuantityOnHand -= request.Quantity;
                db.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = product.Id,
                    InventoryLotId = lot.Id,
                    Type = InventoryTransactionType.Dispense,
                    QuantityDelta = -request.Quantity,
                    ReferenceType = "ConsultMedication",
                    ReferenceId = id.ToString(),
                    Notes = request.Notes,
                    CreatedByUserId = consult.ClinicianUserId,
                });
            }

            var item = new ConsultMedication
            {
                ConsultId = id,
                ProductId = product.Id,
                InventoryLotId = lot?.Id,
                Quantity = request.Quantity,
                DoseInstructions = request.DoseInstructions,
                Notes = request.Notes,
            };

            db.ConsultMedications.Add(item);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { item.Id });
        });

        group.MapPost("/{id:int}/vaccinations", async (int id, ConsultVaccinationRequest request, IValidator<ConsultVaccinationRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var consult = await db.Consults.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (consult is null)
            {
                return Results.NotFound();
            }

            InventoryLot? lot = null;
            if (request.ProductId.HasValue)
            {
                var product = await db.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId.Value, cancellationToken);
                if (product is null)
                {
                    return Results.BadRequest("Selected vaccination stock product could not be found.");
                }

                if (product.TrackInventory)
                {
                    lot = await InventoryAllocation.ResolveLotAsync(db, product.Id, request.InventoryLotId, 1m, cancellationToken);
                    if (lot is null)
                    {
                        return Results.BadRequest("Unable to allocate stock for the selected vaccination.");
                    }

                    lot.QuantityOnHand -= 1m;
                    db.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = product.Id,
                        InventoryLotId = lot.Id,
                        Type = InventoryTransactionType.Dispense,
                        QuantityDelta = -1m,
                        ReferenceType = "Vaccination",
                        ReferenceId = id.ToString(),
                        Notes = request.Notes,
                        CreatedByUserId = consult.ClinicianUserId,
                    });
                }
            }

            var item = new PatientVaccinationRecord
            {
                PatientId = consult.PatientId,
                ConsultId = id,
                VaccinationTypeId = request.VaccinationTypeId,
                ProductId = request.ProductId,
                InventoryLotId = lot?.Id,
                DateAdministered = request.DateAdministered,
                DueDate = request.DueDate,
                Notes = request.Notes,
            };

            db.PatientVaccinationRecords.Add(item);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { item.Id });
        });

        group.MapPost("/{id:int}/finalize", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var consult = await db.Consults.Include(x => x.Appointment).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (consult is null)
            {
                return Results.NotFound();
            }

            consult.Status = ConsultStatus.Finalized;
            consult.CompletedAt = DateTimeOffset.UtcNow;
            if (consult.Appointment is not null)
            {
                consult.Appointment.Status = AppointmentStatus.Completed;
            }

            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { consult.Id, consult.Status });
        });

        group.MapPost("/{id:int}/generate-invoice", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var consult = await db.Consults
                .Include(x => x.Patient)
                .Include(x => x.Appointment)
                .Include(x => x.Procedures).ThenInclude(x => x.ProcedureCatalogItem)
                .Include(x => x.Medications).ThenInclude(x => x.Product)
                .Include(x => x.Invoice)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (consult is null || consult.Patient is null || consult.Appointment is null)
            {
                return Results.NotFound();
            }

            if (consult.Invoice is not null)
            {
                return Results.Ok(new { consult.Invoice.Id, consult.Invoice.InvoiceNumber });
            }

            var invoiceCount = await db.Invoices.CountAsync(cancellationToken);
            var invoice = new Invoice
            {
                InvoiceNumber = $"INV-{1000 + invoiceCount + 1}",
                ClientId = consult.Patient.ClientId,
                PatientId = consult.PatientId,
                ConsultId = consult.Id,
                Status = InvoiceStatus.Draft,
                IssuedAt = DateTimeOffset.UtcNow,
            };

            foreach (var procedure in consult.Procedures)
            {
                Product? product = null;
                if (procedure.ProcedureCatalogItem?.DefaultProductId is int defaultProductId)
                {
                    product = await db.Products.FirstOrDefaultAsync(x => x.Id == defaultProductId, cancellationToken);
                }

                product ??= await db.Products.FirstAsync(x => x.Sku == "SRV-CONSULT", cancellationToken);
                invoice.Lines.Add(InvoiceLineBuilder.BuildInvoiceLine(product, procedure.Quantity, null, procedure.ProcedureText));
            }

            if (!invoice.Lines.Any())
            {
                var consultProduct = await db.Products.FirstAsync(x => x.Sku == "SRV-CONSULT", cancellationToken);
                invoice.Lines.Add(InvoiceLineBuilder.BuildInvoiceLine(consultProduct, 1m, null));
            }

            foreach (var medication in consult.Medications)
            {
                if (medication.Product is not null)
                {
                    invoice.Lines.Add(InvoiceLineBuilder.BuildInvoiceLine(medication.Product, medication.Quantity, medication.InventoryLotId));
                }
            }

            SeedData.RecalculateInvoice(invoice);
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { invoice.Id, invoice.InvoiceNumber });
        });
    }
}
