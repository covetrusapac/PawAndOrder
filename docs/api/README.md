# Clinic API contract

`openapi.v1.json` is the machine-generated OpenAPI 3.0 description of the **clinic API only**:
everything registered by `ApiRoutes.MapClinicApi`. It is the reference artifact for the
API endpoint decomposition refactor — regenerate it and diff it to prove the HTTP contract did
not change.

The root redirect (`/`) and the health probe (`/health`) are mapped by `Program.cs`, outside the
clinic API group, so they are deliberately absent from this document.

## Regenerate

Run from the repository root:

```powershell
dotnet run --project backend/tools/PawAndOrder.ApiContract
```

The tool writes `docs/api/openapi.v1.json` (resolved from the repository root, not the shell's
working directory). An optional first argument overrides the output path:

```powershell
dotnet run --project backend/tools/PawAndOrder.ApiContract -- path/to/output.json
```

The generator builds a bare `WebApplication`, calls `MapClinicApi()`, and asks Swashbuckle for the
document. It never runs `Program.cs`, so **no database is required and no seed data is written**.
It registers only what route registration and OpenAPI generation need (API explorer, Swagger
generator, validators, and a service marker for the injected `PawAndOrderDbContext`).

The output is re-indented JSON so regeneration produces a byte-identical, diff-friendly file.

If a sandboxed shell blocks a spawned executable from writing inside the repository, write to a
temporary path and copy the file in:

```powershell
dotnet run --project backend/tools/PawAndOrder.ApiContract -- "$env:TEMP\openapi.v1.json"
Copy-Item "$env:TEMP\openapi.v1.json" docs/api/openapi.v1.json
```

## Verify

`backend/tests/PawAndOrder.Api.Tests/ClinicApiRouteTableTests.cs` reads this file, derives the
expected `METHOD /path` set from it, builds the same bare host, and asserts the registered route
templates and HTTP methods match exactly. Because the expected set comes from this document, the
artifact is the single source of truth: adding, removing, or re-pathing a route without
regenerating fails the test.
