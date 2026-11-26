# 08: Extract Consult module

**What to build:** Move the largest and most stateful group — the clinical record — into its own module. This is the ticket that finishes the job: once it lands, the dispatcher contains no handlers at all.

Scope:

- Create a consult, and return the full clinical record: patient and owner summary, clinician, diagnoses, procedures, medications with their allocated lots, vaccinations, and any linked invoice.
- Update the clinical narrative and vital signs.
- Add a diagnosis, optionally creating a catalogue entry first.
- Add a procedure, optionally creating a catalogue entry first and carrying a default product.
- Dispense a medication, allocating stock when the product is inventory-tracked and writing a dispense transaction.
- Record a vaccination, optionally allocating a stock dose and writing a dispense transaction, and creating the patient's vaccination record with its due date.
- Finalise the consult, which completes the linked appointment.
- Generate an invoice from the consult, returning the existing invoice if one is already linked.

Handler bodies move verbatim. The fallback behaviour when no procedure carries a product — billing a general consultation line instead — must survive unchanged.

**Blocked by:** 07: Extract Invoice module

**Status:** ready-for-agent

- [ ] Consult has a dedicated module with its own mapping entry point.
- [ ] The dispatcher contains no handlers and no route-group construction; it is a short ordered list of module registrations.
- [ ] The dispatcher's public signature is unchanged.
- [ ] The consult detail response still includes every nested collection with the same shape and ordering.
- [ ] Adding a diagnosis or procedure with the save-to-catalogue option still creates the catalogue entry before the consult line.
- [ ] Dispensing a medication and recording a vaccination still allocate stock and write dispense transactions with the same reference types.
- [ ] Finalising a consult still completes its appointment.
- [ ] Generating an invoice twice still returns the existing invoice rather than creating a second.
- [ ] The route-table test passes and the reference API description is unchanged.
- [ ] The existing test suite passes unmodified.
- [ ] No endpoint outside the consult group was modified.
