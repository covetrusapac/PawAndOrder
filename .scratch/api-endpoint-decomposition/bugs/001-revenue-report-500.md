# Bug 001: `GET /api/reports/revenue` returns 500

**Found during:** ticket 09 manual smoke walk (2026-11-27), after the API endpoint decomposition.
**Severity:** Medium — the revenue report is completely unusable, but no other endpoint depends on it.
**Status:** Open. Logged for separate work. **Deliberately not fixed** by the refactor.

## Symptom

```
GET http://localhost:5077/api/reports/revenue
500 Internal Server Error
```

## Root cause

EF Core cannot translate the `GroupBy` key. The projection calls `DateOnly.FromDateTime` over a
`DateTimeOffset` inside the query:

```csharp
.GroupBy(x => DateOnly.FromDateTime(x.IssuedAt!.Value.UtcDateTime))
```

Error:

```
The LINQ expression 'DbSet()
    .Where(i => i.IssuedAt >= @start && i.IssuedAt < @end)
    .GroupBy(i => DateOnly.FromDateTime(i.IssuedAt.Value.UtcDateTime))'
could not be translated.
```

Location: `backend/src/PawAndOrder.Api/Endpoints/ReportEndpoints.cs:73-80`.

## Pre-existing, not introduced by the refactor

The identical expression exists on `main`:

```
$ git show main:backend/src/PawAndOrder.Api/ApiRoutes.cs | Select-String 'DateOnly.FromDateTime'
main:1597: .GroupBy(x => DateOnly.FromDateTime(x.IssuedAt!.Value.UtcDateTime))
```

The handler block was verified byte-identical when it moved in ticket 04, and the committed OpenAPI
artifact is unchanged. This is a latent defect in the original implementation, exposed by running
the endpoint against real data for the first time in this environment.

## Suggested fix (for the separate ticket, not applied here)

Group in a way EF can translate, for example:

```csharp
.GroupBy(x => x.IssuedAt!.Value.UtcDateTime.Date)
.Select(x => new { date = DateOnly.FromDateTime(x.Key), total = x.Sum(i => i.TotalIncGst), invoiceCount = x.Count() })
```

or project the issued timestamps first and group client-side. Either way this is a **behaviour-adjacent
change** and must not ride along with the structural refactor; it needs its own ticket and its own
evidence.

## Repro

1. Start SQL Server and the API (`docker compose up -d`, then `dotnet run --project backend/src/PawAndOrder.Api`).
2. Ensure at least one issued invoice exists.
3. `GET /api/reports/revenue` → 500.
