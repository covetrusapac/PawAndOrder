# 02: Extract shared endpoint helpers

**What to build:** Pull the four private helpers that currently sit at the bottom of the endpoint file into three named, internal shared modules, so the coupling they represent becomes explicit and so the clinical and financial route groups can be extracted independently afterwards.

The three modules are:

- **Request validation** — generic over the request type, taking a validator and returning either a validation-problem result or a null marker. Every validating endpoint depends on this. It must stay generic; specialising it per request type defeats the purpose.
- **Inventory allocation** — resolves which inventory lot to dispense from. If a specific lot is requested, honour it only when it is valid; otherwise apply the existing policy of earliest expiry first, then earliest received, excluding expired lots and lots with insufficient quantity.
- **Invoice line construction** — two constructors, one from a product and one from explicit values, co-located in one module. Both delegate the arithmetic to the existing GST pricing module, which must not change.

Update every call site in the same change. Delete the originals. This is a move, not a rewrite: behaviour, including error messages and the order of operations, is unchanged.

**Blocked by:** 01: Pin the API contract

**Status:** done

- [x] Request validation logic exists in exactly one internal shared module; no endpoint handler contains its own copy.
- [x] Inventory lot resolution exists in exactly one internal shared module; both clinical dispensing and invoice-line dispensing call it.
- [x] Invoice line construction exists in exactly one internal shared module; both constructors are co-located and delegate to the existing GST pricing module.
- [x] No shared helper is public.
- [x] No duplicate copy of any of the four helpers remains anywhere in the API assembly.
- [x] The route-table test passes and the reference API description is unchanged.
- [x] The existing unit tests pass unmodified.
- [x] No endpoint's behaviour, status codes, or error messages changed.
