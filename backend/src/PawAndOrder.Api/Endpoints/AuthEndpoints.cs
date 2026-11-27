using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PawAndOrder.Api.Shared;
using PawAndOrder.Application;
using PawAndOrder.Infrastructure;

namespace PawAndOrder.Api.Endpoints;

internal static class AuthEndpoints
{
    internal static void MapAuthEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/auth");

        group.MapPost("/login", async (LoginRequest request, IValidator<LoginRequest> validator, PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var validationProblem = await EndpointValidation.ValidateAsync(request, validator);
            if (validationProblem is not null)
            {
                return validationProblem;
            }

            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Username == request.Username && x.IsActive, cancellationToken);
            if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new
            {
                token = $"demo-{user.Username}",
                user = new { user.Id, user.Username, user.DisplayName, user.Role },
            });
        });

        group.MapGet("/me", async (PawAndOrderDbContext db, CancellationToken cancellationToken) =>
        {
            var user = await db.Users.AsNoTracking().OrderBy(x => x.Id).FirstAsync(cancellationToken);
            return Results.Ok(new { user.Id, user.Username, user.DisplayName, user.Role });
        });
    }
}
