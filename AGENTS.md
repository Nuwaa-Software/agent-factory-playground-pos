# MiniPos — instructions for AI agents

A small multi-tenant POS (point of sale) built **exactly like nuwaa-pos**: .NET 10 vertical slices,
EF Core + PostgreSQL, Saudi VAT, ZATCA-style invoice QR, Arabic + English, and a React + TypeScript web
frontend. It is a training ground
for the Agent Factory before it works on the real nuwaa-pos.

## Build & test

| What | Command (run from repo root) |
|---|---|
| Build | `dotnet build backend/MiniPos.sln` (warnings are errors) |
| All tests | `dotnet test backend/MiniPos.sln` |
| Unit only | `dotnet test backend/tests/MiniPos.Tests.Unit` |
| Architecture rules | `dotnet test backend/tests/MiniPos.Tests.Architecture` |
| Integration | `dotnet test backend/tests/MiniPos.Tests.Integration` — needs Postgres: set `MINIPOS_TEST_DB` to a server connection string (the agent sandbox does this), otherwise Testcontainers starts one (needs Docker) |
| Frontend: everything | `cd frontend && npm ci && npm run check` — type check, lint, tests, production build |
| Frontend tests only | `cd frontend && npx vitest run` (one file: `npx vitest run src/pages/ProductsPage.test.tsx`) |

A change is done only when the build has **no warnings** and **all** tests pass — backend and frontend.

## Layout

- `backend/src/MiniPos.Domain` — entities and pure logic (VAT, invoice QR). Depends on nothing.
- `backend/src/MiniPos.Infrastructure` — EF Core `AppDbContext`, tenant context. Never references the Api.
- `backend/src/MiniPos.Api` — Minimal APIs, one folder per slice: `Features/<Area>/<Slice>/{Request,Validator,Handler,Endpoint}.cs`. Register new slices in `Features/FeatureEndpoints.cs`.
- `backend/tests/` — `Unit`, `Integration` (real Postgres via `WebApplicationFactory`), `Architecture` (convention tests).
- `frontend/` — React 19, TypeScript, Vite, Tailwind CSS 4, TanStack Query, React Router, i18next; tests with
  Vitest + Testing Library + vitest-axe. `src/pages/` one file per screen (+ its `.test.tsx`), `src/components/`
  shared pieces, `src/api/` API calls and their types (mirror the backend's response records), `src/lib/`
  pure helpers, `src/locales/{ar,en}.json` all texts, `src/test/` test helpers.

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

## Frontend conventions

1. **Arabic first, right-to-left.** Arabic is the default language. Every user-visible text is a key in
   **both** `src/locales/ar.json` and `src/locales/en.json` — never hard-coded in a component
   (`locales.test.ts` fails if the two files differ). Product names: `nameAr` in Arabic, `nameEn` in English.
2. **Logical directions only**: `ms-`/`me-`/`ps-`/`pe-`, `text-start`/`text-end`, `start-0`, `border-s`,
   `rounded-s` — never `ml-`, `pr-`, `text-left`, `left-0` (`conventions.test.ts` fails otherwise). Codes and
   IDs in Latin script inside Arabic text go in `<bdi>`.
3. **Money** comes from the API in halalas: show it with `formatSar` (`src/lib/money.ts`) inside
   `t('money.sar', { amount })`. Never divide by 100 or use floating point in a component.
4. **API calls** go through `src/api/` (`apiGet` adds the dev auth headers) as TanStack Query hooks. Every screen
   that loads data shows loading (`role="status"`), empty, and error (`role="alert"` with a retry) states.
5. **Accessible and touch-friendly**: every input has a `<label>`, actions are real `<button>`s and links,
   controls are at least 44px high (`min-h-11`), errors are linked with `aria-describedby` and `aria-invalid`.
6. **Tests** render the screen with `renderWithProviders` and stub `fetch` with `stubFetch` (never a real
   server), act like the user (`userEvent`), and find elements by role and visible text, in Arabic and in
   English where texts change. Every new screen gets an axe check (`src/test/axe.ts`) in both languages.
7. **No new npm packages** unless the issue asks for one: the agent sandbox is offline and `npm ci` only
   installs what `package-lock.json` already lists.
8. A frontend issue changes only `frontend/`. If the screen needs an endpoint or field the API doesn't have,
   say so — it is a separate backend issue.
