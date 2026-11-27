using Microsoft.EntityFrameworkCore;
using PawAndOrder.Domain;
using PawAndOrder.Infrastructure;

namespace PawAndOrder.Api.Endpoints;

internal static class LookupEndpoints
{
    // The bootstrap endpoint is a bare route on the API group, so its path stays /api/lookups/bootstrap
    // and it does not become a /lookups route group.
    internal static void MapLookupEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/lookups/bootstrap", async (PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var clinicians = await db.Users.AsNoTracking()
                .Where(x => x.Role == UserRole.Veterinarian && x.IsActive)
                .OrderBy(x => x.DisplayName)
                .Select(x => new { x.Id, x.DisplayName })
                .ToListAsync(cancellationToken);

            var appointmentTypes = await db.AppointmentTypes.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name, x.DurationMinutes, x.ColorHex })
                .ToListAsync(cancellationToken);

            var diagnoses = await db.DiagnosisCatalogItems.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name, x.Code })
                .ToListAsync(cancellationToken);

            var procedures = await db.ProcedureCatalogItems.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name, x.Code, x.DefaultProductId })
                .ToListAsync(cancellationToken);

            var vaccinationTypes = await db.VaccinationTypes.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name, x.Species, x.DefaultIntervalMonths })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { clinicians, appointmentTypes, diagnoses, procedures, vaccinationTypes });
        });
    }
}
