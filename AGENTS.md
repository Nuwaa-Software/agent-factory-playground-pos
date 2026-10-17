# MiniPos — instructions for AI agents

A small multi-tenant POS (point of sale) built **exactly like nuwaa-pos**: .NET 10 vertical slices,
EF Core + PostgreSQL, Saudi VAT, ZATCA-style invoice QR, Arabic + English. It is a training ground
for the Agent Factory before it works on the real nuwaa-pos.

## Build & test

| What | Command (run from repo root) |
|---|---|
| Build | `dotnet build backend/MiniPos.sln` (warnings are errors) |
| All tests | `dotnet test backend/MiniPos.sln` |
| Unit only | `dotnet test backend/tests/MiniPos.Tests.Unit` |
| Architecture rules | `dotnet test backend/tests/MiniPos.Tests.Architecture` |
| Integration | `dotnet test backend/tests/MiniPos.Tests.Integration` — needs Postgres: set `MINIPOS_TEST_DB` to a server connection string (the agent sandbox does this), otherwise Testcontainers starts one (needs Docker) |

A change is done only when the build has **no warnings** and **all** tests pass.

## Layout

- `backend/src/MiniPos.Domain` — entities and pure logic (VAT, invoice QR). Depends on nothing.
- `backend/src/MiniPos.Infrastructure` — EF Core `AppDbContext`, tenant context. Never references the Api.
- `backend/src/MiniPos.Api` — Minimal APIs, one folder per slice: `Features/<Area>/<Slice>/{Request,Validator,Handler,Endpoint}.cs`. Register new slices in `Features/FeatureEndpoints.cs`.
- `backend/tests/` — `Unit`, `Integration` (real Postgres via `WebApplicationFactory`), `Architecture` (convention tests).

## Conventions (each one exists because of a real bug class — keep them)

1. **Vertical slices.** A feature = `Request.cs` (MediatR request + response records), `Validator.cs`
   (FluentValidation), `Handler.cs` (sealed, returns `Result<T>`), `Endpoint.cs` (maps the route).
   Copy the nearest existing slice. Handlers are `sealed` (`Tests.Architecture/LayerRulesTests`).
2. **Expected failures are `Result<T>`, not exceptions.** Return `new Error(code, message, ErrorKind.X)`;
   `ResultHttp.ToHttp` maps NotFound → 404, Conflict → 409, Validation → 400.
3. **Multi-tenancy.** Every tenant entity implements `ITenantScoped` and gets a global `CompanyId` query
   filter. New rows take `CompanyId` from `ITenantContext`, **never** from the request body.
   **Never call `IgnoreQueryFilters()`** in the Api: it lets one company read another's data.
   A missing row of another company is a **404**, not a 403 (don't reveal it exists).
4. **Every `/api` endpoint names a policy**: `.RequireAuthorization(Policies.X)` — never a bare
   `RequireAuthorization()` (admits every role, Viewer included).
   `Tests.Integration/EndpointPolicyConventionTests` fails otherwise.
   Policies: `CompanyAdmin` (Admin), `PosCashier` (Admin, Cashier), `CatalogRead` (all roles).
5. **Free text is validated, never stripped.** Every `*Name` / `*NameEn` / `*NameAr` rule uses
   `TextRules.NoControlCharacters()` (a NUL reaches Postgres and fails as a 500).
   `Tests.Architecture/ValidationConventions` fails otherwise. Reject bad input with a 400; never
   silently remove characters (it changes what the user typed).
6. **Money is `long` halalas** (1 SAR = 100 halalas). Never `double`/`float`/`decimal` in storage.
   VAT is 15%, computed **once on the invoice net total**, **rounded half up** to the halala
   (`Vat.Compute`). Display: `InvoiceQr.FormatSar`.
7. **Invoice QR (ZATCA style)**: TLV records — 1 tag byte, 1 length byte = number of **UTF-8 bytes**
   of the value, then the bytes. Base64 of the whole. Seller name is the company's **Arabic** name.
8. **Logging** uses `[LoggerMessage]` source-generated methods with a unique `EventId` — no
   `logger.LogInformation(...)` calls.
9. **Concurrency.** Two cashiers can act at the same moment. Anything that reads-then-writes shared
   state (stock, sale numbers) must stay correct under parallel requests: the loser gets a **409**,
   never a 500 and never a wrong result.
10. **Arabic and English.** Every user-visible name has `NameEn` and `NameAr`. Arabic text is normal
    input — test with it.

## Working rules for agents

- Keep changes small and focused on the issue; don't reformat unrelated code.
- Every bug fix or feature includes tests (unit and/or integration) that fail before the change.
- Never edit generated files, `bin/` or `obj/`. Don't add NuGet packages unless the issue asks.
- Branch: `agent/<issue-number>-<short-slug>`. Never push to `main`.
