# 03: Extract Auth, Lookup, Vendor modules

**What to build:** Move the three smallest route groups into their own modules, establishing the module convention that every later ticket follows.

- **Auth** — sign in, and the current-user endpoint.
- **Lookup** — the single bootstrap endpoint that returns clinicians, appointment types, diagnosis catalog, procedure catalog, and vaccination types together.
- **Vendor** — list with open-order counts, and create.

Each module exposes one mapping entry point that registers its own route group on the caller-supplied API route group, then registers its handlers. Handler bodies move verbatim. The dispatcher keeps its public signature and its creation of the API route group; it simply calls the new modules in the existing order.

Watch the lookup group: it is registered as a bare route on the API group rather than as its own route group. Do not invent a lookup prefix — the path must stay exactly as it is.

**Blocked by:** 02: Extract shared endpoint helpers

**Status:** done

- [x] Auth, Lookup, and Vendor each have a dedicated module with its own mapping entry point.
- [x] The dispatcher no longer contains any handler or route-group construction for these three groups.
- [x] The lookup endpoint's path is unchanged; no new path segment was introduced.
- [x] The route-table test passes and the reference API description is unchanged.
- [x] The existing test suite passes unmodified.
- [x] No endpoint outside these three groups was modified.
