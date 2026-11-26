# 04: Extract Inventory, Product, Report modules

**What to build:** Move the reading-heavy route groups into their own modules, following the convention established in the previous ticket.

- **Inventory** — stock levels with low-stock flagging and next expiry, lot listing with an optional product filter, and manual stock adjustments (including the negative-stock guard and the adjustment transaction record).
- **Product** — searchable catalogue listing with quantity on hand, product detail with lots and recent transactions, and product creation.
- **Report** — dashboard metrics and today's appointments, daily summary, revenue by day, appointments by status, low stock, and top product usage.

Handler bodies move verbatim. Where two groups overlap in purpose — the inventory stock view and the low-stock report both summarise lots — leave both as they are. Consolidating them is a behaviour change and belongs to separate work.

**Blocked by:** 03: Extract Auth, Lookup, Vendor modules

**Status:** ready-for-agent

- [ ] Inventory, Product, and Report each have a dedicated module with its own mapping entry point.
- [ ] The dispatcher no longer contains any handler or route-group construction for these three groups.
- [ ] All six report endpoints are registered and their query parameters are unchanged.
- [ ] The route-table test passes and the reference API description is unchanged.
- [ ] The existing test suite passes unmodified.
- [ ] No endpoint outside these three groups was modified.
- [ ] The stock view and the low-stock report both still behave exactly as before; neither was merged into the other.
