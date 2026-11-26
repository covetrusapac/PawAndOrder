# 09: Verify and document

**What to build:** Prove the refactor changed structure and nothing else, and leave the project's documentation pointing at the new layout.

Regenerate the API description artifact and diff it against the baseline captured before any code moved. It must be identical — not merely equivalent. Then run the full backend build and test suite, type-check and build the frontend, and walk the main workflows by hand.

The manual pass must cover: sign in; list and search clients; open a client; list patients; open a patient; view appointments; arrive an appointment; start a consult; add a diagnosis; add a procedure; dispense a medication; record a vaccination; finalise the consult; generate an invoice; add a payment; view inventory lots; receive a purchase order; and load all six report endpoints.

Finally, record the new module layout in the project's system design documentation: what each module owns, what the dispatcher does, and which three behaviours are genuinely shared. Note the Consult module's internal seam between the clinical record and its sub-resources as a follow-up opportunity, and state explicitly that it was not taken.

Any bug discovered during the move is logged for separate work, not fixed here. A purely structural refactor that quietly changes behaviour is a failed refactor.

**Blocked by:** 08: Extract Consult module

**Status:** ready-for-agent

- [ ] The regenerated API description is byte-identical to the pre-refactor baseline.
- [ ] The backend solution builds with no new warnings.
- [ ] The full backend test suite passes, with no existing test modified to accommodate the refactor.
- [ ] The frontend type checks and builds successfully.
- [ ] The manual smoke pass completes with no regressions.
- [ ] No API module exceeds roughly 450 lines.
- [ ] The dispatcher reads as a short ordered manifest of module registrations.
- [ ] The system design documentation records the module layout, the dispatcher's role, and the three shared behaviours.
- [ ] The Consult module's sub-resource split is recorded as a follow-up, not performed.
- [ ] Any bugs found during the move are logged separately and remain unfixed.
- [ ] The specification document is marked as implemented.
