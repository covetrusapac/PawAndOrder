# 01: Pin the API contract

**What to build:** Before a single endpoint moves, lock down what the API currently exposes so every later ticket has an objective pass/fail check. Produce a committed reference artifact describing the whole clinic API — every route, every verb, every parameter, every response shape — generated from the current build. Then add an automated test that registers the clinic API on a minimal host and asserts the complete set of route templates and HTTP methods it produces.

The test must not depend on a live SQL Server. Registering endpoints does not resolve request-scoped services, so build a bare host, attach the clinic API, and inspect its endpoint data sources. Do not boot the full application startup path, which seeds the database.

This ticket moves no endpoint code. Its only purpose is to make the rest of the refactor verifiable.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] A reference API description artifact is committed, generated from the current build.
- [ ] The artifact documents how to regenerate it.
- [ ] An automated test asserts the complete set of route templates and HTTP methods registered by the clinic API.
- [ ] The test passes without a running SQL Server instance.
- [ ] The test fails if a route is added, removed, or has its template or method changed.
- [ ] No endpoint has moved; the solution builds and the existing test suite passes unchanged.
- [ ] The frontend type definitions are untouched.
