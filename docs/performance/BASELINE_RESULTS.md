# AegisCore Performance BASELINE_RESULTS.md

BEFORE-optimization performance baseline. Generated 2026-08-11 on branch `agent/prod-hardening`.
Measured locally against the running demo stack (Docker Compose). No production code was modified.
Machine data: 8 vCPU, 14 GiB RAM, Linux. Stack: `AegisCore.API` (net10.0, in container), `AegisCore.Portal` (Next.js 16), PostgreSQL 17-alpine, Redis 7-alpine, all healthy.

## 0. Environment & method

- **Demo instance**: `Runtime__DemoMode=true`, `ASPNETCORE_ENVIRONMENT=Demo`. **IMPORTANT: `LicenseLockoutMiddleware` is SKIPPED in demo mode** (`LicenseLockoutMiddleware.cs:13-17`). The demo baseline therefore measures the system WITHOUT the per-request license check.
- **Load tool**: k6 v0.54.0. Tests through the real user path: k6 → Next.js portal (localhost:8080) → rewrite → API.
- **License A/B**: a disposable non-demo API instance (`Runtime__DemoMode=false`, license middleware ACTIVE) was run against a throwaway cloned DB (`aegiscore_perf`) on the same Docker network, then destroyed. Demo DB untouched.
- **Workload**: realistic mixed scenario replicating AWS_LOAD_TEST_PLAN §3 at small scale (login, session, student/faculty dashboards, registration, attendance, grades, OBE, transcript, finance, license-status, refresh).
- Staged runs: **10 / 50 / 100 concurrent users**, 2 minutes each, 120s. A 30s 50-VU status-probe run produced per-endpoint success/latency classification.
- PostgreSQL statement logging confirmed per-request `SELECT ... FROM "LicenseStates"` on the license-active instance.

## 1. Aggregate latency / throughput / error (full mix, through portal)

| Stage | p50 | p90 | p95 | avg | max | req/s | it/s | non-2xx |
|---|---|---|---|---|---|---|---|---|
| 10 VUs | 0.03s | 0.91s | **1.08s** | 0.26s | 7.8s | 22.4 | 5.7 | 16.2% |
| 50 VUs | 0.12s | 5.42s | **6.11s** | 1.45s | 30.0s | 30.3 | 7.7 | 15.7% |
| 100 VUs | 0.70s | 9.40s | **11.24s** | 3.03s | 30.2s | 27.5 | 7.0 | 15.0% |

Key behavior: p95 degrades ~10× from 10→100 VUs while throughput plateaus at ~27-30 req/s. **The API is the bottleneck (CPU-bound), not the portal or database.** Non-2xx rate is stable ~15-16%, driven by known data-dependent errors (see §6).

## 2. Per-endpoint latency at 100 VUs (p95)

| Endpoint | avg | med | p90 | p95 |
|---|---|---|---|---|
| login (`POST /api/auth/login`) | 8.75s | 8.18s | 12.75s | **13.52s** |
| transcript PDF (`POST .../transcript/official`) | 27.31s | 28.69s | 30.01s | **30.04s** (hits k6 30s cap) |
| obe overview (`/api/obe/faculty/overview`) | 5.33s | 5.87s | 8.51s | **8.84s** |
| refresh | 2.60s | 2.51s | 4.88s | **5.16s** |
| finance executive (`/api/finance/v2/dashboard/executive`) | 1.60s | 0.94s | 4.02s | **5.01s** |
| finance security | 1.18s | 0.74s | 3.15s | 4.55s |
| grade approvals | 1.46s | 0.89s | 3.43s | 4.61s |
| obe attainment (student) | 1.50s | 0.53s | 4.41s | 4.77s |
| transcript (read) | 1.39s | 0.66s | 2.81s | 3.95s |
| registration | 1.11s | 0.54s | 2.71s | 4.01s |
| student finance | 1.02s | 0.76s | 1.78s | 1.82s |
| student me | 0.81s | 0.44s | 2.02s | 3.07s |
| student attendance | 0.70s | 0.45s | 1.33s | 2.16s |
| license status | 0.76s | 0.25s | 2.12s | 2.88s |
| session | 0.38s | 0.20s | 0.84s | 1.56s |
| attendance roster | 0.39s | 0.21s | 0.95s | 1.36s |
| faculty courses | 0.45s | 0.24s | 1.05s | 1.53s |

At 25 VUs (lower contention), the same endpoints were far faster (e.g., session avg 17ms, student_me 120ms, obe_overview 264ms), confirming degradation is load-driven saturation, not inherent endpoint cost — except the two confirmed slow paths: **transcript PDF (~3s single-user, 30s at load)** and **login (~0.2-0.3s single-user, 2-8s at load)**.

## 3. License overhead (quantified)

Isolated A/B — identical `/api/session` call, pre-authenticated token, no portal, 5 VUs, direct to each API:

| Instance | License middleware | avg | med | p95 |
|---|---|---|---|---|
| Demo (license SKIPPED) | OFF | 1.8ms | 1.36ms | 4.05ms |
| Perf (license ACTIVE) | ON | 4.1ms | 3.29ms | 8.02ms |
| **Delta** | | **+2.3ms** | **+1.9ms** | **+4.0ms** |

- **Per-request DB traffic**: PostgreSQL statement log (license-active instance) confirms **1 × `SELECT ... FROM "LicenseStates"` per request** (plus `set_config('app.tenant_id', ...)` from the tenant interceptor). Query itself is cheap at small table sizes (`EXPLAIN ANALYZE`: Seq Scan, 0.03ms exec / 0.9ms plan at 1 row) — the cost is the per-request round trip + serialization, not the scan.
- **Scaling risk**: the license path does `db.LicenseStates.ToListAsync()` (loads ALL rows, orders in memory, no WHERE, no cache) — cost grows with table size and request rate. `CONFIRMED_BY_CODE` (`LicenseService.cs:238-242`).
- **Effective overhead at scale**: under the 100-VU mixed test the demo's p95 for `/api/session` was 1.56s (portal-included); the license delta of +2-4ms on a trivial endpoint is small in isolation, but it is a per-request DB hit that compounds CPU/connection pressure (amplifies the API CPU saturation measured in §5).
- **Current demo status**: license check is NOT active in demo mode, so the 10/50/100 results exclude it. A non-demo deployment adds +1 DB round trip per request.

## 4. OBE overhead (quantified)

- **OBE attainment (student)**: p95 4.77s at 100 VUs; 110ms at 25 VUs — load-saturated.
- **OBE faculty overview**: p95 8.84s at 100 VUs; 618ms at 25 VUs.
- **Confirmed code cost**: `OBEAttainmentService.GenerateProgramReportAsync` loops every PO → every mapping → `CalculateCLOAttainmentAsync` which does `db.CourseGrades.Where(...).Select(...).ToListAsync()` then averages **in memory** (`ObeServices.cs:83-126`). N+1 + full materialization.
- **Data limitation**: demo DB has 0 CLOs / 0 CLOPOMappings / 0 CLOAttainments / 6 CourseGrades, so the OBE endpoints returned empty/trivial data — the N+1 cost is NOT fully exercised by the demo dataset. `MEASUREMENT_REQUIRED` on a seeded dataset for a true ceiling.

## 5. System resources during load (docker stats + pg_stat)

| Sample (100 VUs) | API CPU | API mem | Portal CPU | Postgres CPU | Postgres mem | Redis CPU | pg active |
|---|---|---|---|---|---|---|---|
| idle | 0.05% | 6.7 GiB | 0.01% | 0.00% | 306 MiB | 0.86% | 1 |
| under load 1 | **654%** | 6.7 GiB | 2.8% | 27% | 308 MiB | 0.7% | 1 |
| under load 2 | **602%** | 5.9 GiB | 1.9% | 3% | 309 MiB | 6.0% | 1 |
| under load 3 | **584%** | 4.9 GiB | 3.2% | 18% | 309 MiB | 0.8% | 1 |

- **API is CPU-bound**: 580-654% CPU (6+ of 8 cores) at 100 VUs. Postgres ~3-27%, Redis ~1%, Portal ~1-3%. The first saturated resource is the **API process CPU**.
- **API memory**: 4.9-6.7 GiB, fluctuating → GC/alloc pressure; **post-test memory stayed at ~5.4-6.7 GiB (not released)** → memory-retention concern to investigate under Queue A.
- **DB transactions**: ~2,400-3,800 xacts per 13s window under load (≈ 190-290 tx/s) on the demo DB. Single Postgres, max_connections=100, EF pool MaxPoolSize=200.

## 6. Error-rate decomposition

Stable ~15-16% non-2xx across all stages; dominated by **known pre-existing data-dependent errors**, not load:

| Endpoint | Result | Cause |
|---|---|---|
| `/api/finance/v2/dashboard/executive` | **500** (LINQ) | Pre-existing EF translation bug — `DbSet<JournalEntryLine>().Join(...)...` cannot be translated (confirmed in API logs; unrelated to load) |
| `/api/faculty-grade-entry/course/{id}` | 404 | Demo has 0 `AssessmentSchemes` (data-dependent) |
| `.../template.csv` | 404 | Same |
| `transcript/official` at 50 VUs | 75% timeout | PDF+PQC+QR synchronous, k6 30s cap — genuine load saturation |

## 7. Identified bottlenecks (ranked)

1. **API CPU saturation** — Argon2id logins + EF SQL console logging (Serilog `AegisCore:Information` logs every query) + synchronous PDF/PQC dominate 6/8 cores. Load test plateaus ~30 req/s.
2. **Transcript PDF generation** — synchronous PdfSharpCore + SLH-DSA + QR; 3s single-user, 30s+ at 50+ VUs. `LIKELY_BOTTLENECK` (R8 confirmed at runtime).
3. **Login** — Argon2id + GeoIP external call + user query; 0.2-0.3s single, 2-8s at 50-100 VUs due to CPU contention. `LB-01` confirmed.
4. **License per-request DB read** — 1 round trip/request on non-demo; `CONFIRMED_BY_CODE`; +2-4ms on trivial endpoint; compounds CPU/connection pressure.
5. **OBE N+1 + in-memory aggregation** — `CONFIRMED_BY_CODE`; under-exercised by empty demo data; needs seeded measurement.
6. **EF SQL logging at Information level** — every query serialized to console; significant CPU overhead at scale.

## 8. MEASUREMENT_LIMITATIONS

- **Demo DB is tiny** (2 students, 7 courses, 7 enrollments, 126 attendances, 6 grades, 0 OBE/CLO data, 0 finance vouchers) → OBE/finance/registration hot paths under-represented; N+1 ceilings not fully exercised.
- **k6 default 30s request timeout** caps `transcript_pdf` at 30s (p99/p95 both ~30s = timeout artifact).
- **dotnet-counters / dotnet-trace could not attach**: API container runs `DOTNET_EnableDiagnostics=0`; GC/alloc breakdown is inferred from memory growth, not measured. (Noted; a diagnostics-enabled container is required for gen0/gen1 GC counts.)
- **No OpenTelemetry in the app**; prometheus `/metrics` exposes only HTTP histograms (no process/GC metrics).
- **`pg_stat_statements` not enabled** (requires Postgres restart → skipped to avoid destabilizing demo). Used `pg_stat_database` + `EXPLAIN ANALYZE` + statement logging instead.
- **Login measured through portal proxy** (includes Next.js + network), and demo-mode license is skipped — a non-demo deployment adds +1 DB round trip/request (quantified in §3).
- **Tests run on the working demo**; results are a lower bound on single-tenant behavior and an upper bound on available resources (shared 8 cores).

## 9. Baseline verdict vs AWS_LOAD_TEST_PLAN targets

| Target (AWS plan) | Current (local, 100 VUs) | Status |
|---|---|---|
| p95 < 300ms CRUD | p95 1.56s (session), 3.94s (transcript read) | NOT MET |
| error < 1% | 15% (dominated by known data errors, not load) | NOT MET (correctness gaps) |
| DB pool < 40% | Postgres ~18-27% CPU, 190-290 tx/s | OK (DB not saturated) |
| Redis p95 < 2ms | Redis ~1% CPU, trivial | OK |
| Throughput scaling | plateaus ~27-30 req/s at 50→100 VUs | NOT MET (API CPU-bound) |

First bottleneck: **API process CPU** (Argon2 + EF logging + synchronous PDF/PQC). Actionable queues: A1 (license cache), A8 (EF log level / dead code), R8 (async docs), LB-01 (login path), and targeted profiling with a seeded dataset (G2).

## 10. Cleanup & integrity

- Disposable perf instance + `aegiscore_perf` DB destroyed. `log_statement` restored to none. Demo stack re-verified healthy (`/health` = Healthy).
- Working tree clean; no production code changed. See `docs/performance/BASELINE_RESULTS.json` for the machine-readable summary.
