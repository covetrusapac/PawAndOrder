using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using PawAndOrder.Api;
using PawAndOrder.Application;
using PawAndOrder.Infrastructure;
using Swashbuckle.AspNetCore.Swagger;

// Generates the committed API contract artifact without booting the full application.
// It deliberately does not start Program.cs, so no database connection is made and no
// seed data runs. Only the pieces needed to register routes and describe them are wired up.

var repoRoot = FindRepositoryRoot();
var outputPath = ResolveOutputPath(args, repoRoot);

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = "PawAndOrder.Api" });

builder.Logging.ClearProviders();

// Match Program.cs so enum values are described the same way the running app emits them.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "PawAndOrder.Api", Version = "1.0" }));

builder.Services.AddValidatorsFromAssemblyContaining<ClientUpsertRequestValidator>();

// Handlers take a PawAndOrderDbContext, so the endpoint/generator infrastructure must know it
// is an injected service rather than a request body. Registration alone is enough: nothing is
// ever resolved, and this factory fails loudly if that assumption is ever broken.
builder.Services.AddSingleton<PawAndOrderDbContext>(_ =>
    throw new InvalidOperationException("The contract generator never resolves PawAndOrderDbContext."));

var app = builder.Build();
app.MapClinicApi();

// Registering routing makes the clinic endpoints visible to the API explorer without starting
// the host. No request is ever served, so no handler (and therefore no database) is invoked.
app.UseRouting();
app.UseEndpoints(_ => { });

var swaggerProvider = app.Services.GetRequiredService<ISwaggerProvider>();
var document = swaggerProvider.GetSwagger("v1");

using var output = new StringWriter();
document.SerializeAs(OpenApiSpecVersion.OpenApi3_0, new OpenApiJsonWriter(output));
var compactJson = output.ToString();

// OpenApiJsonWriter emits compact JSON. Re-indent for a reviewable, diff-friendly artifact.
var prettyJson = JsonNode.Parse(compactJson)!.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, prettyJson + "\n");

Console.WriteLine($"Wrote {outputPath}");

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "PawAndOrder.slnx")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new InvalidOperationException("Could not locate the repository root (PawAndOrder.slnx).");
}

static string ResolveOutputPath(string[] args, string repoRoot)
{
    if (args.Length > 1)
    {
        throw new ArgumentException("Usage: PawAndOrder.ApiContract [output-path]");
    }

    return args.Length == 1
        ? Path.GetFullPath(args[0])
        : Path.Combine(repoRoot, "docs", "api", "openapi.v1.json");
}
