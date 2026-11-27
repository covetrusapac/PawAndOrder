using System.Text.Json;
using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PawAndOrder.Application;
using PawAndOrder.Infrastructure;

namespace PawAndOrder.Api.Tests;

/// <summary>
/// Pins the clinic API surface to the committed OpenAPI contract in docs/api/openapi.v1.json.
/// The expected route set is derived from that artifact at test time, so the contract is the
/// single source of truth; this test cannot drift from it silently.
/// </summary>
public class ClinicApiRouteTableTests
{
    private const string ContractRelativePath = "ApiContract/openapi.v1.json";

    private static readonly Regex RouteConstraintPattern = new(
        @"\{([^}:]+)(?::[^}]*)?\}",
        RegexOptions.Compiled);

    private static readonly HashSet<string> HttpMethods = new(StringComparer.Ordinal)
    {
        "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS", "TRACE",
    };

    [Fact]
    public void ClinicApi_RegistersExactlyTheRoutesInTheCommittedContract()
    {
        var actual = ReadRegisteredRoutes();
        var documented = ReadDocumentedRoutes();

        Assert.NotEmpty(actual);
        Assert.NotEmpty(documented);

        // The root redirect and health probe live outside MapClinicApi and must stay out of this contract.
        Assert.All(actual, route => Assert.StartsWith("/api/", PathOf(route)));
        Assert.All(documented, route => Assert.StartsWith("/api/", PathOf(route)));

        var documentedButNotRegistered = Except(documented, actual);
        var registeredButNotDocumented = Except(actual, documented);

        Assert.True(
            documentedButNotRegistered.Length == 0 && registeredButNotDocumented.Length == 0,
            $"""
             The clinic API route table no longer matches {ContractRelativePath}.

             Documented but not registered:
             {Format(documentedButNotRegistered)}

             Registered but not documented:
             {Format(registeredButNotDocumented)}

             Regenerate the contract with:
               dotnet run --project backend/tools/PawAndOrder.ApiContract
             """);
    }

    /// <summary>
    /// Builds a bare host and attaches only the clinic API. It never starts the host, seeds data,
    /// or touches SQL Server: mapping routes does not resolve request-scoped services.
    /// </summary>
    private static WebApplication CreateClinicApiHost()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();

        // Handlers take a DbContext, so the endpoint builder must recognise it as a service rather
        // than a request body. Registering it is enough; nothing is resolved because no request runs.
        builder.Services.AddSingleton<PawAndOrderDbContext>(_ =>
            throw new InvalidOperationException("The route-table test must never resolve a DbContext."));
        builder.Services.AddValidatorsFromAssemblyContaining<ClientUpsertRequestValidator>();

        var app = builder.Build();
        app.MapClinicApi();
        return app;
    }

    private static IReadOnlySet<string> ReadRegisteredRoutes()
    {
        using var app = CreateClinicApiHost();

        var routes = new HashSet<string>(StringComparer.Ordinal);
        var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints);

        foreach (var endpoint in endpoints)
        {
            var routeEndpoint = Assert.IsType<RouteEndpoint>(endpoint);
            var template = NormaliseTemplate(routeEndpoint.RoutePattern.RawText ?? string.Empty);
            var methods = routeEndpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;

            Assert.NotNull(methods);
            foreach (var method in methods)
            {
                routes.Add($"{method} {template}");
            }
        }

        return routes;
    }

    private static IReadOnlySet<string> ReadDocumentedRoutes()
    {
        var contractPath = Path.Combine(AppContext.BaseDirectory, ContractRelativePath);
        Assert.True(File.Exists(contractPath), $"Missing committed API contract at {contractPath}.");

        using var contract = JsonDocument.Parse(File.ReadAllText(contractPath));
        var routes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var path in contract.RootElement.GetProperty("paths").EnumerateObject())
        {
            foreach (var operation in path.Value.EnumerateObject())
            {
                var method = operation.Name.ToUpperInvariant();
                if (HttpMethods.Contains(method))
                {
                    routes.Add($"{method} {path.Name}");
                }
            }
        }

        return routes;
    }

    /// <summary>
    /// Removes route constraints such as <c>{id:int}</c> and trailing slashes so ASP.NET Core
    /// templates compare directly to OpenAPI path keys.
    /// </summary>
    private static string NormaliseTemplate(string template)
    {
        var normalised = RouteConstraintPattern.Replace(template, "{$1}");
        return normalised.Length > 1 ? normalised.TrimEnd('/') : normalised;
    }

    private static string PathOf(string route) => route[(route.IndexOf(' ') + 1)..];

    private static string[] Except(IReadOnlySet<string> left, IReadOnlySet<string> right) =>
        left.Except(right).OrderBy(route => route, StringComparer.Ordinal).ToArray();

    private static string Format(IReadOnlyCollection<string> routes) =>
        routes.Count == 0 ? "  (none)" : string.Join(Environment.NewLine, routes.Select(route => $"  {route}"));
}
