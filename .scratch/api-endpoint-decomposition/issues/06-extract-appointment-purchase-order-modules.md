# 06: Extract Appointment, Purchase Order modules

**What to build:** Move the two groups whose handlers drive state transitions into their own modules.

- **Appointment** — range query with optional clinician filter, create, update, arrive, start consult, and cancel. Starting a consult creates the consult record when one does not already exist, carrying the appointment's patient, clinician, and visit reason forward.
- **Purchase Order** — list with line totals, detail with vendor and lines, create with generated order number, submit, and receive. Receiving creates an inventory lot per received line, writes a receive transaction, and derives the order status from whether every line is fully received.

These are the most stateful handlers in the codebase. Move them verbatim; the status transitions and lot creation logic are not to be reordered or tidied.

**Blocked by:** 05: Extract Client, Patient modules

**Status:** done

- [x] Appointment and Purchase Order each have a dedicated module with its own mapping entry point.
- [x] The dispatcher no longer contains any handler or route-group construction for these two groups.
- [x] Arriving, starting a consult, and cancelling an appointment still produce the same statuses.
- [x] Starting a consult on an appointment that already has one still returns the existing consult rather than creating a second.
- [x] Receiving a purchase order still creates one lot and one receive transaction per received line, and still derives received versus partially received status correctly.
- [x] The route-table test passes and the reference API description is unchanged.
- [x] The existing test suite passes unmodified.
- [x] No endpoint outside these two groups was modified.
