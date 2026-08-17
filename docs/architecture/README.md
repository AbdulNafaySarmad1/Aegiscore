# AegisCore Repository Intelligence Layer

Generated 2026-08-11 on branch `agent/prod-hardening`. This layer exists so future AI agents can understand AegisCore WITHOUT rescanning the repository. Read these before touching code.

## Reading order (by task type)

| If you need... | Read first |
|---|---|
| 60-second overview | `AEGISCORE_TECHNICAL_PROFILE.md` (§1-2) then `REPO_INTELLIGENCE.md` |
| Architecture / request flow | `SYSTEM_MAP.md` |
| What domain lives where | `DOMAIN_MAP.md` |
| Endpoint inventory | `API_CATALOG.md` |
| Database / entities / tenant model | `DATA_MODEL.md` |
| Roles & authorization | `ROLE_MATRIX.md` |
| Frontend routes / fetch patterns | `ROUTE_MATRIX.md` |
| Security findings | `SECURITY_BOUNDARIES.md` → `docs/security/STRIX_SCOPE.md` |
| Performance findings | `PERFORMANCE_MAP.md` + `HOT_PATH_TRACES.md` |
| Service/worker/external deps | `SERVICE_DEPENDENCIES.md` + `EXTERNAL_INTEGRATIONS.md` |
| Uploads/imports/exports | `FILE_UPLOAD_FORMATS.md` + `DOCUMENT_PIPELINE.md` |
| Risk register | `ARCHITECTURE_RISKS.md` |
| What to build next | `AGENT_HANDOFF.md` |
| Load-test design | `docs/performance/AWS_LOAD_TEST_PLAN.md` |
| Machine-readable graph | `REPO_GRAPH.json` + `graphify-out/graph.json` (query via `graphify query`) |

## Files

- `AEGISCORE_TECHNICAL_PROFILE.md` — final 20-section technical profile + priority table
- `SYSTEM_MAP.md` — runtime architecture, middleware pipeline, project graph
- `DOMAIN_MAP.md` — domain → module → controller → entities; license finding summary
- `API_CATALOG.md` — endpoint inventory with auth
- `DATA_MODEL.md` — entities, tenant model, indexes, N+1 findings
- `ROLE_MATRIX.md` — roles, capabilities, enforcement stack
- `ROUTE_MATRIX.md` — frontend routes, shells, fetch hygiene
- `SECURITY_BOUNDARIES.md` — security findings (SEC-01..22)
- `SERVICE_DEPENDENCIES.md` — DI, workers, Redis, pool
- `EXTERNAL_INTEGRATIONS.md` — external APIs, timeouts, resilience
- `PERFORMANCE_MAP.md` — confirmed hot paths + profiling plan
- `HOT_PATH_TRACES.md` — 18 end-to-end traces
- `ARCHITECTURE_RISKS.md` — ranked risk register (R1..R14)
- `REPO_INTELLIGENCE.md` — agent onboarding facts & pitfalls
- `AGENT_HANDOFF.md` — queues A–G with acceptance criteria
- `FILE_UPLOAD_FORMATS.md`, `DOCUMENT_PIPELINE.md` — document intelligence
- `REPO_GRAPH.json` — machine-readable component graph

## Ground truth constraints

- Authoritative code: repo root `AegisCore.*` + `AegisCore.Portal`.
- **Ignore `aegiscore-backend/` (stale duplicate) and `aegiscore-frontend/` (legacy).**
- Working demo must not be destabilized. Do not rewrite, migrate languages, or weaken auth.
- Findings marked CONFIRMED_BY_CODE are proven by source; LIKELY/NEEDS_RUNTIME_MEASUREMENT require measurement before action.
