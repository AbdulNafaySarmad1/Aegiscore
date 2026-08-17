# AegisCore PERFORMANCE_MAP.md

Repository Intelligence artifact. Generated 2026-08-11, branch `agent/prod-hardening`.
Every finding is classified: `CONFIRMED_BY_CODE` (verified by reading source), `LIKELY_BOTTLENECK` (pattern, needs measurement), `NEEDS_RUNTIME_MEASUREMENT` (cannot be proven statically). No benchmark numbers are fabricated; runtime measurements are pending.

## 1. Confirmed hot-path findings

### HP-01 — License status DB read on every request — `CONFIRMED_BY_CODE`
- `LicenseLockoutMiddleware.InvokeAsync` (`AegisCore.API/Middleware/LicenseLockoutMiddleware.cs:28`) runs for every installed-mode non-demo request and calls `ILicenseService.GetStatusAsync`.
- `LicenseService.GetStatusAsync` → `CurrentAsync` → `db.LicenseStates.ToListAsync()` + `OrderByDescending(CreatedAt)` (`AegisCore.Infrastructure/License/LicenseService.cs:238-242`).
- **No cache, no filter, full table load per request.**
- Impact: one extra DB round trip per request; unbounded cost as LicenseStates rows grow; DoS amplifier.
- Recommendation (future, not now): cache `IsLocked` snapshot in memory/Redis with short TTL; or verify a signed license lease locally (design in ARCHITECTURE_RISKS #1).

### HP-02 — Middleware chain does N network/DB checks per request — `CONFIRMED_BY_CODE`
Every request traverses: Serilog logging, exception middleware, mTLS thumbprint (no I/O), SessionBinding (no I/O), TenantContext (no I/O), LicenseLockout (1 DB read), Turnstile (external only if token present), Abac (no I/O), Casbin (in-memory). The per-request DB read (HP-01) is the only guaranteed I/O; Turnstile adds external latency when the portal sends a token.

### HP-03 — OBE program report N+1 + in-memory aggregation — `CONFIRMED_BY_CODE`
`ObeServices.cs:120-126` loops every PO → `CalculatePOAttainmentAsync` (`:97-118`) loops every CLOPOMapping → `CalculateCLOAttainmentAsync` (`:85-95`):
- `CourseGrades` for (courseId, semesterId) materialized **in memory** then `.Average()` and `.Count(p>=70)` in C#.
- ~1 + P + P·M queries; the 2 inner queries per CLO (`CourseLearningOutcomes` + `CourseGrades`).
- At 10k students × dozens of CLOs → thousands of rows materialized per CLO, hundreds of queries per report.
- Fix (future): aggregate with `SELECT AVG/COUNT` server-side; batch mappings; single query joining PO→CLOPOMapping→CLO.

### HP-04 — Attendance percentage loads full row set — `CONFIRMED_BY_CODE`
`AttendanceRepository.GetAttendancePercentageAsync` (`Repositories/AttendanceRepository.cs:13-24`) loads ALL attendance rows for an enrollment to count Present/Total in memory. Per-enrollment OK; in a loop (exam eligibility) it becomes N×full scans.
- Also `GetByDateRangeAsync` (`:26-34`) does a date-range scan with `Include` and no index on `Date`.

### HP-05 — Grading grace marks per-student requery — `CONFIRMED_BY_CODE`
`GradingService.ApplyGracemarksAsync` (`Application/Grading/GradingService.cs:93-116`) loops entries calling `CalculateCourseGradeAsync` per student, each re-querying scheme/marks/band/grade (`:12-70`).

### HP-06 — Generic `Repository<T>` returns entities with no Include — `CONFIRMED_BY_CODE`
`Repositories/Repository.cs:34-49` — callers accessing navigations get silent null/empty or N+1. Many services therefore either over-Include or under-Include.

### HP-07 — Finance payment-plan sweep per-installment queries — `CONFIRMED_BY_CODE`
`FinanceModuleService.cs:558-587`: correct `Include(Demand).ThenInclude(Installments)` for the main list, but inside the loop `AccountAsync` per account + `JournalEntries.AnyAsync` per installment.

## 2. Likely bottlenecks — NEEDS_RUNTIME_MEASUREMENT

| ID | Area | Why | Test |
|---|---|---|---|
| LB-01 | Login (`POST /api/auth/login`) | Argon2id password hash (Konscious) is intentionally slow (~100ms+); plus `IgnoreQueryFilters` user query with Includes, refresh-family write, login-audit + GeoIP external call | k6 login ramp; dotnet-counters on Argon2 |
| LB-02 | Student dashboard | Frontend 3-deep waterfall (session→me→[attendance,finance]) + backend per-request license read; many endpoints | k6 student-dashboard scenario |
| LB-03 | Grade entry (faculty) | MarkModeration writes + review queue reads; moderation `ReviewComments.StartsWith("GRADE_CHANGE:")` string scan | measure queue page |
| LB-04 | Finance reports | CSV/XLSX/PDF/XBRL builders serialize full report object then re-parse to JsonDocument (`FinanceModuleEndpoints.cs:1815-1985`); PQC signing each; synchronous under request | k6 report generation |
| LB-05 | Transcript PDF | PdfSharpCore layout + SLH-DSA signing + QR, synchronous in request; fontconfig on Alpine | k6 transcript |
| LB-06 | Redis saturation | refresh families, replay store, webhook idempotency all in Redis; license read bypasses it | Redis metrics |
| LB-07 | NEST 7.17.5 search | EOL client; per-query overhead; optional profile | — |
| LB-08 | Connection pool | MaxPoolSize 200, Kestrel 1000 conns — 10k users may exhaust pool during spikes | pg_stat_activity |

## 3. Query-hygiene anti-patterns catalog

- `ToList()` before filtering: `AttendanceRepository.GetAttendancePercentageAsync` (load-all then count).
- Full-table materialization for aggregation: `ObeServices.CalculateCLOAttainmentAsync`.
- N+1 loops: OBE, grading, finance sweep.
- Unnecessary Include: audit service, academic calendar (minor).
- `IgnoreQueryFilters()` used broadly — each is a deliberate cross-tenant query, but each bypasses tenant indexing on TenantId (finance uses explicit tenant predicates).
- No pagination on several admin lists (courses, users in `AllowedCourses` returns full `List<Guid>`).

## 4. Profiling plan (to run later, not now)

Tools: OpenTelemetry (add), dotnet-counters, dotnet-trace, `EXPLAIN ANALYZE`, `pg_stat_statements`, Redis INFO, prometheus `/metrics`, k6.

| Metric | Method | Threshold targets (initial hypothesis, to be validated) |
|---|---|---|
| p50 / p95 / p99 latency | k6 + OTel spans | p95 < 300ms CRUD; < 2s reports |
| CPU / memory / GC | dotnet-counters | GC gen0 < 100/s at 5k users |
| DB latency / pool | `pg_stat_statements` + `pg_stat_activity` | pool < 80% at 5k users |
| Redis latency | `INFO commandstats` | p95 < 2ms |
| Throughput / error rate | k6 | < 1% 5xx at target load |
| External API latency | OTel on HttpClient | track license/gateway/turnstile/geoip |

Measure at 100 / 500 / 1,000 / 2,500 / 5,000 / 10,000 users per `docs/performance/AWS_LOAD_TEST_PLAN.md`.

## 5. Duplicated cryptographic work

- Finance report endpoints sign CSV/XLSX/PDF/XBRL each with PQC per document — signing is not shared/cached; generation is synchronous.
- `HybridPqcTransportMiddleware` hashes request body + Redis nonce per `/api/finance*` request when enforced — per-request crypto+Redis even before hitting the endpoint.
- Transcript: SHA-512 + SLH-DSA + QR per generated document.

## 6. Summary priority list

1. Cache license lockout state (kill per-request DB read) — biggest single win.
2. OBE attainment aggregation to SQL.
3. Attendance percentage to `COUNT` aggregate.
4. Add missing indexes (Attendance.Date, CourseGrades composite, CLOAttainment).
5. Origin rate limiting for auth/expensive endpoints.
6. Grading grace-mark batch evaluation.
7. Portal data-fetch: parallelize/consolidate waterfall + react-query.
8. Switch AI providers to `IHttpClientFactory`.
