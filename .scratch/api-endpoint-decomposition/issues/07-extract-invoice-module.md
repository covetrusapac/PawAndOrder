# 07: Extract Invoice module

**What to build:** Move the whole invoicing group into its own module. This is the group that consumes the invoice-line and inventory-allocation shared modules extracted earlier, so it is the first proof that the shared-module split was necessary rather than cosmetic.

Scope:

- Create a direct invoice for a client and patient pair, rejecting a patient that does not belong to the client.
- Searchable list, capped at the fifty most recent.
- Detail including lines with their allocated lots, and payments.
- Add a line, optionally from a product, which may dispense stock and write a dispense transaction, then recalculates the invoice.
- Delete a line, which returns stock only when that line originally took stock out, then recalculates.
- Record a payment, rejecting an amount above the remaining balance, and recalculate.
- Issue an invoice.

Handler bodies move verbatim. The document-numbering scheme stays exactly as implemented, including its known fragility — changing it is separate work.

**Blocked by:** 06: Extract Appointment, Purchase Order modules

**Status:** ready-for-agent

- [ ] Invoice has a dedicated module with its own mapping entry point.
- [ ] The dispatcher no longer contains any handler or route-group construction for invoicing.
- [ ] Adding a line still dispenses stock only for inventory-tracked products, and still writes the dispense transaction with the same reference type and notes.
- [ ] Deleting a line still returns stock only when the line was marked as having dispensed it.
- [ ] Invoice totals, GST, amount paid, and balance due are recomputed identically after every mutation.
- [ ] An overpayment is still rejected with the same status and message.
- [ ] The route-table test passes and the reference API description is unchanged.
- [ ] The existing test suite passes unmodified, including the invoice recalculation test.
- [ ] No endpoint outside invoicing was modified.
