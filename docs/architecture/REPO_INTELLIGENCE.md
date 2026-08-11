# AegisCore REPO_INTELLIGENCE.md

Repository Intelligence artifact. Generated 2026-08-11, branch `agent/prod-hardening`.

## 1. Mandatory facts for any agent entering this repo

- **Working tree is a MONOREPO with multiple codebases.** The authoritative backend is at the repo ROOT: `AegisCore.API`, `AegisCore.Application`, `AegisCore.Contracts`, `AegisCore.Domain`, `AegisCore.Infrastructure`, `AegisCore.LedgerVerifier`, `AegisCore.Tests`, `AegisCore.PluginCli`, `AegisCore.PluginSdk`, referenced by `AegisCore.slnx` and `Dockerfile`.
- **`aegiscore-backend/` is a STALE DUPLICATE.** It is git-tracked but NOT authoritative. **DO NOT** read, edit, analyze, or trust it. Same for `aegiscore-frontend/` (legacy Vite/React 18 shell superseded by `AegisCore.Portal`).
- **Primary frontend** = `AegisCore.Portal` (Next.js 16, React 19, App Router).
- Graphify knowledge graph: `graphify-out/graph.json` (20,866 nodes / 51,192 edges / 1,097 communities), `graphify-out/GRAPH_REPORT.md`, `graphify-out/graph.html`. Query it before rescanning the repo.

## 2. Stack snapshot

| Layer | Tech | Version |
|---|---|---|
| Backend | ASP.NET Core / C# | net10.0 |
| ORM | EF Core + Npgsql | 10.0.0 |
| DB | PostgreSQL (pgvector build in compose) | 17 |
| Cache | Redis | 7 (alpine) |
| Auth | JWT Bearer (RS256/HS256), refresh families in Redis | — |
| AuthZ | `[Authorize(Roles)]` + Casbin (in-memory) + ABAC tenant-header check | Casbin.NET 2.21.2 |
| Frontend | Next.js (App Router) / React / TS | 16.2.12 / 19.2.3 / ^5 |
| Realtime | SignalR | 10.0.0 |
| Scheduler | Quartz | 3.13.0 |
| PQC | BouncyCastle (SLH-DSA, ML-DSA, ML-KEM), QRCoder, PdfSharpCore | — |
| Logging | Serilog → Console | 9.0.0 |
| Metrics | prometheus-net | 8.2.1 |
| Payments | RAAST, Stripe, Plaid, GoCardless, Billplz, DuitNow adapters | — |

## 3. Where things live (quick map)

| Concern | Path |
|---|---|
| HTTP pipeline / startup | `AegisCore.API/Program.cs`, `AegisCore.API/Extensions/` |
| Middleware | `AegisCore.API/Middleware/`, `AegisCore.Infrastructure/Security/Abac/` |
| Controllers | `AegisCore.API/Controllers/` (many files), `AegisCore.API/Finance/*.cs` (minimal APIs) |
| DbContext / migrations | `AegisCore.Infrastructure/AegisDbContext.cs`, `Migrations/` (61) |
| Entities | `AegisCore.Domain/Entities/` (601 classes, 173 files) |
| Repositories | `AegisCore.Infrastructure/Repositories/` |
| License | `AegisCore.API/Middleware/LicenseLockoutMiddleware.cs`, `AegisCore.Infrastructure/License/LicenseService.cs`, `AegisCore.API/Background/LicenseHeartbeatHostedService.cs` |
| PQC/signing | `AegisCore.Infrastructure/Security/Pqc/` |
| Finance module | `AegisCore.Infrastructure/Finance/`, `AegisCore.API/Finance/` |
| Tenant | `AegisCore.Domain/Common/ITenantScoped.cs`, `AegisCore.Infrastructure/TenantSessionInterceptor.cs` |
| Portal (frontend) | `AegisCore.Portal/src/` (`app/`, `components/`, `lib/`) |
| Deploy | `docker-compose*.yml`, `Dockerfile`, `k8s/`, `deploy/` |
| Docs (SDD) | `docs/01..30` |
| Graphify outputs | `graphify-out/` |

## 4. Developer workflow facts

- Baseline build (this phase): `dotnet build AegisCore.API/AegisCore.API.csproj -c Release` → **0 errors, 17 warnings** (unread-logger CS9113, nullability, NU1510 prunable refs).
- Frontend: `npm run typecheck` in `AegisCore.Portal` → **passes** (must `rm -rf .next` first; stale `.next` types reference deleted `/analytics`/`/telemetry` routes).
- Frontend lint: `npm run lint` → **37 errors / 96 warnings** (react-hooks/set-state-in-effect, unescaped-entities, no-explicit-any) — hygiene, not build-blocking.
- Tests: `AegisCore.Tests` project exists; Playwright in Portal. No full-suite run in this phase (baseline only).
- Auto-migrate controlled by `Database:AutoMigrate`; demo can use `EnsureCreated`.

## 5. Agent pitfalls to avoid

- Don't grep `aegiscore-backend/` results — filter `-g '!aegiscore-backend/**'`.
- Don't assume `aegiscore-frontend/` is the portal.
- Don't treat Sprint-design docs (`md/S*.md`, `md/U*.md`) as implemented — verify code. Examples: SHAP merit engine is PROPOSED (no implementation); Moodle LTI is PROPOSED; multi-tenant SSO/HITL is PARTIAL.
- Don't assume OpenTelemetry exists (it doesn't).
- Don't rely on Swagger UI (only JSON endpoint is exposed).
- `AspNetCoreRateLimit` and several middleware are dead code — don't assume they're active.

## 6. Graphify integration

- Graph built 2026-08-11 (full repo). God nodes: `AegisCore.Domain.Entities` (858 edges), `AegisDbContext` (638/374), `IRepository` (281/270), `Repository` (243), `AegisCore.Application.Common.Interfaces` (181), `ITenantScoped` (180).
- Health: 2,949 dangling-endpoint edges + 5,851 collapsed edges reported — dominated by DbContext field→DbSet self-references (structural noise), not corruption. Trust relationship edges, treat counts with care.
- Query: `graphify query "<question>"` from repo root (uses `graphify-out/`).

## 7. Classification summary (full detail in AEGISCORE_TECHNICAL_PROFILE.md)

- `KEEP_DOTNET`: all core ERP domains (students, admissions, faculty, academics, registrar, attendance, exams, grades, OBE, finance, HR, RBAC, workflows).
- `OPTIMIZE_DOTNET`: license cache, OBE aggregation, attendance aggregates, indexes, async docs, HttpClient factory for AI.
- `GO_CANDIDATE`: none approved. `MEASURE_FIRST`: license authority (conf<80), telemetry ingestion (if ever built). No evidence today.
- `RUST_CANDIDATE`: none. (Parser hardening, if pursued, would be inside upload/OCR processing — needs evidence.)
