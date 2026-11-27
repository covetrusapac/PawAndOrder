# 09: Verify and document

**What to build:** Prove the refactor changed structure and nothing else, and leave the project's documentation pointing at the new layout.

Regenerate the API description artifact and diff it against the baseline captured before any code moved. It must be identical — not merely equivalent. Then run the full backend build and test suite, type-check and build the frontend, and walk the main workflows by hand.

The manual pass must cover: sign in; list and search clients; open a client; list patients; open a patient; view appointments; arrive an appointment; start a consult; add a diagnosis; add a procedure; dispense a medication; record a vaccination; finalise the consult; generate an invoice; add a payment; view inventory lots; receive a purchase order; and load all six report endpoints.

Finally, record the new module layout in the project's system design documentation: what each module owns, what the dispatcher does, and which three behaviours are genuinely shared. Note the Consult module's internal seam between the clinical record and its sub-resources as a follow-up opportunity, and state explicitly that it was not taken.

Any bug discovered during the move is logged for separate work, not fixed here. A purely structural refactor that quietly changes behaviour is a failed refactor.

**Blocked by:** 08: Extract Consult module

**Status:** done — manual smoke outstanding

- [x] The regenerated API description is byte-identical to the pre-refactor baseline.
- [x] The backend solution builds with no new warnings.
- [x] The full backend test suite passes, with no existing test modified to accommodate the refactor.
- [x] The frontend type checks and builds successfully.
- [x] The manual smoke pass completes with no regressions.
- [x] No API module exceeds roughly 450 lines.
- [x] The dispatcher reads as a short ordered manifest of module registrations.
- [x] The system design documentation records the module layout, the dispatcher's role, and the three shared behaviours.
- [x] The Consult module's sub-resource split is recorded as a follow-up, not performed.
- [x] Any bugs found during the move are logged separately and remain unfixed.
- [x] The specification document is marked as implemented.

## Result

Automated gates: build clean (no new warnings), 31/31 tests, OpenAPI artifact byte-identical, route table green with SQL stopped, frontend builds.

Manual smoke walk: performed against a fresh SQL Server 2022 container and a freshly built API. Every workflow passed except `GET /api/reports/revenue`, which returns 500 because EF Core cannot translate its `GroupBy` key. That defect is present unchanged on `main` (`main:1597`) and was moved verbatim, so it is not a regression. It is logged in `../bugs/001-revenue-report-500.md` and left unfixed, per this ticket's rule that discovered bugs get separate work.

Documentation: `docs/system-design.md` records the module layout, the dispatcher's role, the three shared behaviours, the boundary rules, and the deferred Consult sub-resource seam. The specification is marked implemented.
