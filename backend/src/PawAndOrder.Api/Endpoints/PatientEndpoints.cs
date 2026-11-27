using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PawAndOrder.Api.Shared;
using PawAndOrder.Application;
using PawAndOrder.Domain;
using PawAndOrder.Infrastructure;

namespace PawAndOrder.Api.Endpoints;

internal static class PatientEndpoints
{
    internal static void MapPatientEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/patients");

        group.MapGet("", async (string? search, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            IQueryable<Patient> query = db.Patients.AsNoTracking()
                .Include(x => x.Client)
                .Include(x => x.VaccinationRecords);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.Name.Contains(search) ||
                    x.Species.Contains(search) ||
                    (x.Client != null && (x.Client.FirstName + " " + x.Client.LastName).Contains(search)));
            }

            var patients = await query
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    x.ClientId,
                    x.Name,
                    x.Species,
                    x.Breed,
                    x.WeightKg,
                    ownerName = x.Client != null ? x.Client.FirstName + " " + x.Client.LastName : string.Empty,
                    nextVaccinationDue = x.VaccinationRecords.OrderBy(v => v.DueDate).Select(v => (DateOnly?)v.DueDate).FirstOrDefault(),
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items = patients });
        });

        group.MapGet("/{id:int}", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var patient = await db.Patients.AsNoTracking()
                .Include(x => x.Client)
                .Include(x => x.Appointments).ThenInclude(x => x.Clinician)
                .Include(x => x.Consults)
                .Include(x => x.Invoices)
                .Include(x => x.VaccinationRecords).ThenInclude(v => v.VaccinationType)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (patient is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new
            {
                patient.Id,
                patient.Name,
                patient.Species,
                patient.Breed,
                patient.Sex,
                patient.DateOfBirth,
                patient.EstimatedAgeYears,
                patient.Color,
                patient.MicrochipNumber,
                patient.WeightKg,
                patient.IsDesexed,
                patient.AlertsSummary,
                patient.AllergiesSummary,
                patient.Notes,
                owner = patient.Client is null ? null : new { patient.Client.Id, displayName = patient.Client.FirstName + " " + patient.Client.LastName, patient.Client.Phone },
                appointments = patient.Appointments.OrderByDescending(x => x.StartAt).Take(8).Select(x => new { x.Id, x.StartAt, x.Status, x.Reason, clinician = x.Clinician!.DisplayName }),
                consults = patient.Consults.OrderByDescending(x => x.StartedAt).Take(8).Select(x => new { x.Id, x.VisitReason, x.Status, x.StartedAt, x.CompletedAt }),
                invoices = patient.Invoices.OrderByDescending(x => x.IssuedAt).Take(8).Select(x => new { x.Id, x.InvoiceNumber, x.Status, x.TotalIncGst, x.BalanceDue, x.IssuedAt }),
                vaccinations = patient.VaccinationRecords.OrderBy(x => x.DueDate).Select(x => new { x.Id, name = x.VaccinationType!.Name, x.DateAdministered, x.DueDate, x.Notes }),
            });
        });

        group.MapPost("", async (PatientUpsertRequest request, IValidator<PatientUpsertRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var patient = new Patient
            {
                ClientId = request.ClientId,
                Name = request.Name,
                Species = request.Species,
                Breed = request.Breed,
                Sex = request.Sex,
                DateOfBirth = request.DateOfBirth,
                EstimatedAgeYears = request.EstimatedAgeYears,
                Color = request.Color,
                MicrochipNumber = request.MicrochipNumber,
                WeightKg = request.WeightKg,
                IsDesexed = request.IsDesexed,
                AlertsSummary = request.AlertsSummary,
                AllergiesSummary = request.AllergiesSummary,
                Notes = request.Notes,
            };

            db.Patients.Add(patient);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/patients/{patient.Id}", new { patient.Id });
        });

        group.MapPut("/{id:int}", async (int id, PatientUpsertRequest request, IValidator<PatientUpsertRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var patient = await db.Patients.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (patient is null)
            {
                return Results.NotFound();
            }

            patient.ClientId = request.ClientId;
            patient.Name = request.Name;
            patient.Species = request.Species;
            patient.Breed = request.Breed;
            patient.Sex = request.Sex;
            patient.DateOfBirth = request.DateOfBirth;
            patient.EstimatedAgeYears = request.EstimatedAgeYears;
            patient.Color = request.Color;
            patient.MicrochipNumber = request.MicrochipNumber;
            patient.WeightKg = request.WeightKg;
            patient.IsDesexed = request.IsDesexed;
            patient.AlertsSummary = request.AlertsSummary;
            patient.AllergiesSummary = request.AllergiesSummary;
            patient.Notes = request.Notes;
            patient.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { patient.Id });
        });
    }
}
