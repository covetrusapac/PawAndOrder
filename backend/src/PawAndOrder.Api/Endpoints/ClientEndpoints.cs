using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PawAndOrder.Api.Shared;
using PawAndOrder.Application;
using PawAndOrder.Domain;
using PawAndOrder.Infrastructure;

namespace PawAndOrder.Api.Endpoints;

internal static class ClientEndpoints
{
    internal static void MapClientEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/clients");

        group.MapGet("", async (string? search, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            IQueryable<Client> query = db.Clients.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    (x.FirstName + " " + x.LastName).Contains(search) ||
                    x.Phone.Contains(search) ||
                    (x.Email != null && x.Email.Contains(search)));
            }

            var clients = await query
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .Select(x => new
                {
                    x.Id,
                    x.FirstName,
                    x.LastName,
                    displayName = x.FirstName + " " + x.LastName,
                    x.Phone,
                    x.Email,
                    patientCount = x.Patients.Count,
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items = clients });
        });

        group.MapGet("/{id:int}", async (int id, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var client = await db.Clients.AsNoTracking()
                .Include(x => x.Patients)
                .Include(x => x.Invoices)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (client is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new
            {
                client.Id,
                client.FirstName,
                client.LastName,
                displayName = client.FirstName + " " + client.LastName,
                client.Phone,
                client.Email,
                client.AddressLine1,
                client.AddressLine2,
                client.Suburb,
                client.State,
                client.Postcode,
                client.Notes,
                patients = client.Patients.OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Species, x.Breed, x.WeightKg }),
                invoices = client.Invoices.OrderByDescending(x => x.IssuedAt).Take(8).Select(x => new { x.Id, x.InvoiceNumber, x.Status, x.TotalIncGst, x.BalanceDue, x.IssuedAt }),
            });
        });

        group.MapPost("", async (ClientUpsertRequest request, IValidator<ClientUpsertRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var client = new Client
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Phone = request.Phone,
                Email = request.Email,
                AddressLine1 = request.AddressLine1,
                AddressLine2 = request.AddressLine2,
                Suburb = request.Suburb,
                State = request.State,
                Postcode = request.Postcode,
                Notes = request.Notes,
            };

            db.Clients.Add(client);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/clients/{client.Id}", new { client.Id });
        });

        group.MapPut("/{id:int}", async (int id, ClientUpsertRequest request, IValidator<ClientUpsertRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var client = await db.Clients.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (client is null)
            {
                return Results.NotFound();
            }

            client.FirstName = request.FirstName;
            client.LastName = request.LastName;
            client.Phone = request.Phone;
            client.Email = request.Email;
            client.AddressLine1 = request.AddressLine1;
            client.AddressLine2 = request.AddressLine2;
            client.Suburb = request.Suburb;
            client.State = request.State;
            client.Postcode = request.Postcode;
            client.Notes = request.Notes;
            client.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { client.Id });
        });
    }
}
