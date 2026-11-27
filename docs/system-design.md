# System Design — Paw & Order

This document records the structural decisions that are not obvious from reading a single file. It
currently focuses on how the HTTP API is organised; add sections here as other parts of the system
acquire structure worth writing down.

## Runtime topology

Three processes, orchestrated locally by .NET Aspire (`PawAndOrder.AppHost`):

1. **SQL Server 2022** — a container with a persistent data volume. The schema is created by
   `EnsureCreated()` at API startup, not by migrations.
2. **`PawAndOrder.Api`** — the ASP.NET Core Minimal API. It seeds reference and demo data on
   startup, so it cannot boot without a reachable database.
3. **Vite dev server** — the React frontend. Aspire injects the API endpoint and the frontend
   origin (as `Cors__AllowedOrigins__0`), so neither side hard-codes the other.

The backend is layered: `Domain` (entities and enums) → `Application` (request contracts,
validators, pricing maths, password hashing) → `Infrastructure` (EF Core context and seeding) →
`Api` (HTTP surface). The `Api` project is the only one that knows about HTTP.

## API structure

### The dispatcher

`ApiRoutes.cs` is deliberately tiny (26 lines). It exposes one public entry point:

```csharp
public static IEndpointRouteBuilder MapClinicApi(this IEndpointRouteBuilder app)
```

Its body creates the single `/api` route group and registers twelve endpoint modules in a fixed
order, then returns the builder. It contains no handlers, no entity access and no validation.

The fixed order is part of the design: route groups are disjoint, but the ordering keeps the
OpenAPI document stable so contract diffs stay readable. Do not reorder the registrations.

`MapClinicApi` is public specifically because two non-production consumers build a bare host around
it: the contract generator (`backend/tools/PawAndOrder.ApiContract`) and the route-table test. That
signature is load-bearing — changing it breaks the contract gate, not just call sites.

### Module ownership

Each module owns exactly one route group (except Lookups — see below), declares its own group, and
registers only its own handlers. Everything is `internal`; the module is reachable only through the
dispatcher.

| Module | Lines | Owns |
|---|---|---|
| `AuthEndpoints.cs` | 42 | `/api/auth` — login, current user |
| `LookupEndpoints.cs` | 48 | `/api/lookups/bootstrap` — reference data for the UI |
| `ClientEndpoints.cs` | 133 | `/api/clients` — list/search, detail, create, update |
| `PatientEndpoints.cs` | 152 | `/api/patients` — list/search, detail, create, update |
| `AppointmentEndpoints.cs` | 161 | `/api/appointments` — list by range/clinician, create, update, arrive, start-consult, cancel |
| `ConsultEndpoints.cs` | 410 | `/api/consults` — clinical record, diagnoses, procedures, medications, vaccinations, finalize, generate-invoice |
| `InvoiceEndpoints.cs` | 251 | `/api/invoices` — create, list/search, detail, add/remove line, payment, issue |
| `ProductEndpoints.cs` | 105 | `/api/products` — list/search, detail with lots and recent movements, create |
| `InventoryEndpoints.cs` | 93 | `/api/inventory` — stock view, lots, manual adjustments |
| `VendorEndpoints.cs` | 48 | `/api/vendors` — list, create |
| `PurchaseOrderEndpoints.cs` | 173 | `/api/purchase-orders` — list, detail, create, submit, receive |
| `ReportEndpoints.cs` | 126 | `/api/reports` — dashboard, daily summary, revenue, appointments, low stock, product usage |

**Lookups is the one exception.** It does not create a group; it registers a bare route on the
`/api` group. The path `/api/lookups/bootstrap` is pinned by the contract artifact. Do not
"tidy" it into a `MapGroup("/lookups")` — the resulting path would be identical but the module would
no longer match the convention, and any future prefix change would silently re-path it.

No module exceeds roughly 450 lines. That ceiling is the signal for splitting, not an absolute rule;
the Consult module sits closest to it and is the next candidate (see Follow-ups).

### Shared behaviour

Only three behaviours are genuinely shared across modules. They live in `Shared/` as `internal`
static classes, and cross-module reuse must go through them rather than one module calling another.

| Shared module | Lines | Call sites | Responsibility |
|---|---|---|---|
| `EndpointValidation.cs` | 13 | 20 | `ValidateAsync<T>` runs a FluentValidation validator and converts failures into a `ValidationProblem` result, so every endpoint rejects bad input in the same shape. |
| `InventoryAllocation.cs` | 26 | 3 | `ResolveLotAsync` implements the allocation policy: honour an explicitly requested lot only if it belongs to the product, has sufficient stock and is not expired; otherwise pick the earliest expiry (undated lots last), then earliest receipt. |
| `InvoiceLineBuilder.cs` | 40 | 4 | Builds an `InvoiceLine` from either a `Product` or raw values, applying GST through `PricingMath.Calculate` so line totals are computed in exactly one place. |

Everything else is deliberately duplicated rather than shared — see Follow-ups.

### Boundary rules

- A module never references another module's type. The only cross-module channel is `Shared/`.
- The dispatcher never contains logic. If a registration needs a condition, that condition belongs in
  the module.
- HTTP shape is frozen independently of the code layout. The contract artifact and the route-table
  test are the enforcement mechanism, so a structural refactor can move code freely as long as they
  stay green.

## Contract protection

`docs/api/openapi.v1.json` is a committed snapshot of the HTTP surface: 42 paths, 52 operations.
`docs/api/README.md` explains how to regenerate it. Two guards depend on it:

1. **`ClinicApiRouteTableTests`** builds a bare host (no database, no seeding), calls
   `MapClinicApi()`, and asserts that the registered `METHOD /template` set is exactly the set
   derived from the artifact — in both directions. Adding, removing, re-verbing or re-pathing a
   route without regenerating the artifact fails the test.
2. **The regeneration diff.** Regenerating the artifact must produce a byte-identical file. This is
   the oracle for "structure changed, behaviour did not". It covers parameters, request schemas and
   response declarations that the route-table test does not.

`/` and `/health` live in `Program.cs` and are intentionally outside `MapClinicApi` and outside the
artifact.

The bare-host technique matters: the app's `Program.cs` seeds the database at startup, so a test
that booted the real application would require SQL Server. Instead the test registers
`PawAndOrderDbContext` as a never-resolved singleton and the validators by assembly scan, which is
enough for endpoint construction to treat them as services rather than request bodies.

## Follow-ups

These were observed while decomposing the API and are **logged, not fixed**. A structural refactor
that quietly changes behaviour is a failed refactor, so each of these needs its own ticket.

1. **Consult sub-resource seam (deferred).** `ConsultEndpoints.cs` (410 lines) mixes the clinical
   record itself (create, read, update, finalize) with four sub-resource collections (diagnoses,
   procedures, medications, vaccinations). A natural split exists — `ConsultEndpoints` for the
   record, and a sub-resource module (or modules) for the collections. **This split was explicitly
   not taken.** It is the next size-driven extraction if the module grows, or if the clinical record
   gains enough behaviour to justify separating it.
2. **Document numbering is duplicated.** Invoice numbers are generated as
   `INV-{1000 + count + 1}` in both `ConsultEndpoints` (generate-invoice) and `InvoiceEndpoints`
   (direct invoice creation). Purchase order numbers use the same pattern in
   `PurchaseOrderEndpoints`. This is fragile: the count-based scheme can collide after deletions or
   under concurrency, and `InvoiceNumber` is uniquely indexed. Fixing it is a behaviour change.
3. **Consult and Invoice are genuinely coupled.** Generating an invoice from a consult writes
   `Invoice` and `InvoiceLine` entities from inside the Consult module. That is intentional and was
   preserved verbatim; moving it into `InvoiceEndpoints` would be a redesign, not a move.
4. **Purchase-order receipt saves inside its loop.** Each received line persists a lot before the
   next line is processed. This is not transactional across lines. Preserved as-is.
5. **Auth is a demo stub**, not authentication: login returns a synthetic token, `me` ignores the
   caller, and no endpoint is protected. Out of scope for this work but should not be mistaken for a
   security boundary.

## Verification status

Automated gates for the decomposition (branch `refactor/api-endpoint-decomposition`) pass: the
solution builds with no new warnings, all 31 backend tests pass with no pre-existing test modified,
the regenerated contract is byte-identical to the pre-refactor baseline, the route-table test passes
with no database running, and the frontend type-checks and builds.

The manual smoke walk (the full clinic workflow: book → arrive → consult → finalize → invoice →
payment, plus inventory receipt and all six reports) had **not** been performed at merge time because
no SQL Server instance was running in the verification environment. It remains the one outstanding
gate; the automated evidence proves the HTTP contract and the build, not runtime behaviour.
