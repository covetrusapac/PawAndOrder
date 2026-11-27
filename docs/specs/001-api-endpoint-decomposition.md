# Spec 001 — Decompose the Clinic API Endpoint Module

**Status:** Implemented — see [system-design.md](../system-design.md)
**Type:** Pure structural refactor (no behaviour change)
**Area:** Backend — `PawAndOrder.Api`

---

## Problem Statement

Every HTTP endpoint in the clinic API lives in a single source file. It is currently ~1,700 lines long and contains twelve route-group mappers, a dispatcher, and four private helpers. This creates four concrete problems for the team:

1. **It is unreadable.** Finding the handler for a given URL requires scrolling through unrelated domains. Consults alone occupy roughly a quarter of the file.
2. **It is unmergeable.** Any two concurrent changes to different features collide, because every change touches the same file. Git conflicts are near-guaranteed on parallel work.
3. **It violates the project's own size standard.** The repo enforces a 1,000-line ceiling on source files in review. This file is nearly double that, so it fails its own gate.
4. **It hides shared behaviour.** The four shared helpers at the bottom of the file — request validation, inventory lot resolution, and two invoice-line constructors — are invisible to anyone reading a handler, and they silently couple the Consults and Invoices domains together.

The single dispatcher method that registers all route groups is already the correct seam. The file has simply never been cut along it.

## Solution

Split the endpoint module into one module per route group, each holding exactly the handlers that belong to that group. The existing dispatcher keeps its public signature and becomes a short, readable manifest of the API surface.

Extract the four shared helpers into named shared modules so their coupling is explicit rather than buried at the bottom of a file.

The HTTP contract does not change. Not a URL, not a verb, not a parameter, not a response shape, not a status code. The refactor is complete when a machine-generated description of the API is identical before and after.

## User Stories

1. As a backend developer, I want each route group in its own module, so that I can open a file and immediately see every client, patient, or invoice endpoint.
2. As a backend developer, I want the dispatcher module to read as a short ordered list of route groups, so that the API surface is comprehensible at a glance.
3. As a backend developer, I want no source file in the API layer to exceed roughly 450 lines, so that the project's own size standard is satisfied and reviewable diffs stay small.
4. As a backend developer, I want parallel work on unrelated features to touch different files, so that merge conflicts stop being routine.
5. As a backend developer, I want request validation to live in one named shared module, so that I do not have to scroll to the bottom of a large file to understand how validation failures are produced.
6. As a backend developer, I want inventory allocation policy to live in one named shared module, so that the rule "which lot do we dispense from" has a single obvious home.
7. As a backend developer, I want invoice-line construction to live in one named shared module, so that GST and line-type rules are not duplicated or obscured.
8. As a backend developer, I want the Consults and Invoices coupling to be visible through shared modules rather than implicit, so that I understand the dependency before I change either side.
9. As a backend developer, I want extraction to proceed one route group per change set, so that each step is independently reviewable and revertable.
10. As a backend developer, I want every step of the refactor to leave the solution compiling and all tests passing, so that I am never stranded in a broken intermediate state.
11. As a backend developer, I want shared helpers extracted before any route group moves, so that no route group is blocked by another route group's dependencies.
12. As a backend developer, I want a captured description of the API surface taken before the refactor begins, so that I can prove the contract was preserved.
13. As a reviewer, I want to compare the API description before and after the refactor, so that I can verify no route, verb, parameter, or schema drifted.
14. As a reviewer, I want each route group extraction to be a verbatim move of handler bodies, so that I can review the diff by confirming "nothing changed" rather than re-reading business logic.
15. As a reviewer, I want shared helpers to remain internal to the API assembly, so that the refactor does not widen the assembly's public surface.
16. As a reviewer, I want the public dispatcher signature preserved, so that the application startup code requires no edit.
17. As a QA tester, I want the existing test suite to pass unchanged, so that I have evidence the refactor did not alter behaviour.
18. As a QA tester, I want the frontend to build against unchanged API types, so that I have evidence the response shapes did not drift.
19. As a QA tester, I want a manual smoke script covering the main workflows, so that integration-level regressions are caught even where automated coverage is thin.
20. As a maintainer, I want the new module layout recorded in the project's system design documentation, so that new contributors can navigate the API layer without archaeology.
21. As a maintainer, I want the Consults module's further subdivision documented as a follow-up opportunity, so that the next improvement is visible without being forced into this change.
22. As a maintainer, I want the unrelated duplication I noticed (document numbering) explicitly excluded from this spec, so that a structural refactor is not quietly turned into a behaviour change.

## Implementation Decisions

### Module split

- The API endpoint layer is organised as **one module per route group**: Auth, Lookup, Client, Patient, Appointment, Consult, Invoice, Product, Inventory, Vendor, Purchase Order, Report.
- Each module exposes a single mapping entry point that registers its own route group on the caller-supplied API route group. It re-groups with its own prefix internally, so each module owns its full path segment and is self-contained.
- The **dispatcher module** keeps its existing public signature (the extension method on the endpoint route builder) and its existing behaviour of creating the `/api` route group. Its body becomes an ordered manifest of module-registration calls.
- Route groups keep their current order of registration, so any route-precedence behaviour is unchanged.
- No module is moved to a separate assembly. Everything stays in the existing API project; only file and type organisation changes.

### Shared modules

Four helpers become named, internal shared modules:

- **Endpoint validation** — generic over the request type, taking a validator and returning either a validation problem result or a null marker. Every module that validates requests depends on this.
- **Inventory allocation** — resolves which inventory lot to dispense from, honouring an explicitly requested lot when supplied, otherwise applying the existing first-expiry-first-out-with-fallback policy and excluding expired or under-stocked lots.
- **Invoice line construction** — two constructors: one from a product and one from explicit values. Both delegate the arithmetic to the existing pricing module, which is unchanged.
- **The two invoice-line constructors live together** in the invoice-line module, since one delegates to the other.

- Shared modules are `internal`. The refactor must not widen the assembly's public surface.
- Shared module method names and signatures may change for clarity, provided call sites are updated in the same change set.

### Extraction order

1. **Contract baseline.** Capture the running API's OpenAPI document before any edit. This is the acceptance artifact for the whole effort.
2. **Shared helpers first.** Extract validation, inventory allocation, and invoice-line construction; update call sites in place; delete the originals.
3. **Route groups ascending by size.** Auth, Lookup, Vendor, Inventory, Product, Report, Client, Patient, Appointment, Purchase Order, Invoice, Consult.
4. **Collapse the dispatcher.**
5. **Verify and document.**

- Consults and Invoices must not be moved before step 2, because both depend on the invoice-line and inventory-allocation helpers.
- Each numbered step is a separate, independently reviewable change set that leaves the solution building and tests green.

### Contracts

- The HTTP contract is frozen: every route, verb, query parameter, body shape, status code, and JSON property name is identical before and after.
- The response payloads remain anonymous-object projections. The refactor does not introduce response DTOs; that is a separate concern.
- The presence of enum values as strings in JSON is unchanged.
- Authentication behaviour, demo-token issuance, and the "return the first user" behaviour of the current-user endpoint are **unchanged**. They are known weaknesses but are explicitly not touched here.

### Documentation

- The resulting module layout is recorded in the project's system design documentation as part of the final step.

## Testing Decisions

### What makes a good test here

Good tests assert **external behaviour through a stable seam**, never file layout or type names. A refactor that moves code between files must not be able to fail a test that was written correctly. Conversely, a test that asserts "class X exists in namespace Y" is worthless here — it pins the implementation and would block the very change we want.

The highest available seam is the **HTTP surface**. Prefer it.

### Seams

- **Primary seam — the API description document.** Capture the OpenAPI document from the running application before the refactor and diff it after. This single artifact verifies every route, verb, parameter, schema, and response shape at once. It requires no new production seam.
- **Secondary seam — the server application factory.** The API already exposes the hook needed to boot the application in-process for tests (the program type is deliberately made accessible), but the existing test only asserts that the type is reachable. Exercising real requests through this factory is the natural next step and needs no production change.
- **Tertiary seam — the module registration entry point.** A test may boot the application and assert the set of registered routes. This is acceptable as a safety net but is weaker than the OpenAPI diff, because it ignores parameters and schemas.

No new seam is required in production code. This is a deciding factor in favour of the chosen split.

### Modules tested

- The **API surface as a whole** — via the OpenAPI document diff and/or a route-table assertion through the server application factory.
- The **pricing module** — already covered by existing unit tests; those must pass unchanged and must not be modified.
- The **client request validation module** — already covered by existing unit tests; unchanged.
- The **invoice recalculation path** — already covered; unchanged.

### Prior art

- Existing unit tests cover pricing arithmetic, client request validation, and invoice recalculation. They are the pattern for anything extracted into a genuinely shared, testable unit.
- The API test project already references the program type via the deliberated public-partial-program pattern. That pattern is the prior art for in-process integration tests, even though the current test does not yet use it meaningfully.

### Verification gates

Each change set must pass:

- The solution builds with no new warnings.
- The full existing test suite passes, unmodified.
- The OpenAPI document is identical to the captured baseline.
- The frontend type-check and build succeed.

At the end of the effort, a manual smoke pass covers: sign in, list and search clients, open a client, list patients, open a patient, view the appointment list, arrive and start a consult, add a diagnosis, add a procedure, dispense a medication, record a vaccination, finalise the consult, generate an invoice, add a payment, view inventory lots, receive a purchase order, and load every report endpoint.

## Out of Scope

- **Any behaviour change.** If a bug is found during the move, it is recorded and fixed separately.
- **Document numbering extraction.** The invoice and purchase-order number generators are duplicated and fragile, but changing them is a behaviour change. Separate work.
- **Response DTOs.** Replacing anonymous-object projections with typed contracts is a separate, larger design decision.
- **Authentication hardening.** The demo token and the current-user endpoint's behaviour are untouched.
- **Migrations.** The project still creates schema via `EnsureCreated`; unchanged.
- **Splitting Consults further.** The Consults module will land at roughly 410 lines. Its internal seam between consult CRUD and the four sub-resource endpoints is a documented follow-up, not part of this spec.
- **Moving the API layer into a separate project or assembly.** Explicitly rejected as unnecessary.
- **Frontend changes.** Only the build is run, as a verification gate.

## Further Notes

### Expected module sizes

| Module | Approx. lines |
|---|---|
| Auth | 40 |
| Lookup | 45 |
| Vendor | 45 |
| Inventory | 95 |
| Product | 100 |
| Report | 125 |
| Client | 130 |
| Patient | 150 |
| Appointment | 160 |
| Purchase Order | 170 |
| Invoice | 250 |
| Consult | 410 |
| Shared — validation | 12 |
| Shared — inventory allocation | 25 |
| Shared — invoice lines | 40 |
| **Dispatcher** | **~20** |

Largest resulting module is the Consult module at roughly 410 lines, well inside the project's 1,000-line ceiling.

### Why this coupling exists

Consults generates invoices but Invoices owns the line-construction rules, and both dispense inventory. That is a genuine domain dependency, not an accident of file layout. Extracting the two shared modules makes it explicit and gives it a name, which is the real value of the change — the line count reduction is a side effect.

### Tooling note

This repo has no issue tracker configured and no `docs/` tree prior to this spec, so this document is the source of truth for the work. Publishing to a real tracker and applying the `ready-for-agent` label would require running `/setup-matt-pocock-skills` first.
