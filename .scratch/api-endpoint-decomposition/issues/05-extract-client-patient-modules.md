# 05: Extract Client, Patient modules

**What to build:** Move the two core entity groups into their own modules.

- **Client** — searchable list with patient counts and computed display names, detail including patients and the eight most recent invoices, create, and update.
- **Patient** — searchable list with owner name and next vaccination due date, detail including owner, recent appointments, recent consults, recent invoices, and vaccination records, create, and update.

Handler bodies move verbatim. These groups carry the Australian-specific validation rules, which stay in the existing application-layer validators and are not touched.

**Blocked by:** 04: Extract Inventory, Product, Report modules

**Status:** done

- [x] Client and Patient each have a dedicated module with its own mapping entry point.
- [x] The dispatcher no longer contains any handler or route-group construction for these two groups.
- [x] The client and patient detail responses still include the same nested collections, same ordering, and same result limits.
- [x] The route-table test passes and the reference API description is unchanged.
- [x] The existing test suite passes unmodified.
- [x] No endpoint outside these two groups was modified.
- [x] No validation rule was changed or moved.
