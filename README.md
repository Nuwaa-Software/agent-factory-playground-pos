# agent-factory-playground-pos

**MiniPos** — a small multi-tenant point-of-sale API and web frontend built like
[nuwaa-pos](https://github.com/Nuwaa-Software/nuwaa-pos) (.NET 10 vertical slices, EF Core + PostgreSQL,
FluentValidation, MediatR, `Result<T>`, xUnit unit / integration / architecture tests, Saudi VAT,
ZATCA-style invoice QR, Arabic + English).

It is the **realistic playground** for the Agent Factory's AI employees: if they can work here safely,
they are ready for nuwaa-pos. Conventions for agents (and humans): [AGENTS.md](AGENTS.md).

```bash
dotnet build backend/MiniPos.sln
dotnet test  backend/MiniPos.sln          # integration tests need Docker (Testcontainers) or MINIPOS_TEST_DB
dotnet run --project backend/src/MiniPos.Api   # needs Postgres (see appsettings.json)
```

Dev auth: send `X-Company-Id: <guid>` and `X-Role: Admin|Cashier|Viewer` headers.

The web frontend (`frontend/`, React + TypeScript + Vite, Arabic/English) talks to the API through Vite's
dev proxy, so start the API first:

```bash
cd frontend
npm install
npm run dev        # http://localhost:5173 — sign in with a company ID (GUID) and a role
npm run check      # type check, lint, tests, build
```
