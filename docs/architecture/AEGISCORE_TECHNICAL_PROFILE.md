# AegisCore TECHNICAL PROFILE — AEGISCORE_TECHNICAL_PROFILE.md

Repository Intelligence artifact. Generated 2026-08-11, branch `agent/prod-hardening`.
Full technical profile of the AegisCore multi-tenant university ERP.

## 1. Executive technical summary

AegisCore is a multi-tenant modular-monolith university ERP. Backend: ASP.NET Core on .NET 10 with EF Core over PostgreSQL 17 + Redis 7; frontend: Next.js 16 / React 19 portal; PQC-grade cryptography (SLH-DSA/ML-DSA/ML-KEM) for finance integrity; Casbin + ABAC + JWT RBAC authorization; Cloudflare at the edge. ~200 API routes + ~130 finance v2 endpoints; 601 domain entities; 61 migrations. It currently runs as a working demo and is functionally rich but has a **confirmed per-request license DB read**, **confirmed OBE N+1**, **incomplete tenant scoping on core SIS entities**, and **no origin-side rate limiting**. The correct next phase is measured optimization and targeted security hardening, not architecture rewrite.

## 2. Current architecture

- Modular monolith (`docs/architecture/0001-modular-monolith.md` ADR): one codebase, modules collaborate via contracts/events; every tenant row carries TenantId.
- Layered .NET: API → Application → Contracts → Domain; Infrastructure implements persistence/integrations.
- Portal (Next.js) proxies `/api/*` to the API; JWT in localStorage; client-side role nav.
- Docker Compose multi-mode (local/cloud/hybrid/uni); k8s manifests; Cloudflare edge (WAF, Turnstile, AOP, R2, tunnel).

## 3. Repository/domain structure

- Authoritative root: `AegisCore.{API,Application,Contracts,Domain,Infrastructure,LedgerVerifier,Tests,PluginCli,PluginSdk}` + `AegisCore.Portal`.
- STALE: `aegiscore-backend/`, `aegiscore-frontend/` (ignore).
- Domains: student lifecycle, admissions, faculty, registrar/academics, registration, timetable, attendance, exams, grades + grade-change approval, OBE/CLO-PLO, finance (largest), HR, welfare, cafeteria, documents, reporting, PQC/QR, audit, multi-tenancy, RBAC/ABAC, licensing, campus ops, AI/telemetry, communications, governance, accreditation/HEC.

## 4. Runtime request architecture

`Browser → Cloudflare → Next.js rewrites → Kestrel → 17-middleware pipeline (license DB read, tenant context, Casbin, ABAC, Turnstile) → Controller/Minimal-API → Service → EF (tenant filter + GUC) → Postgres/Redis → response`. Background workers: license heartbeat (daily), PQC signing queue, audit seal (15min), ledger signature (1min), payment plan (1h), vendor rescreen (1d), backup (hourly).

## 5. Data architecture

- 601 entities / 599 DbSets / 61 migrations; high-cardinality: Attendance, Enrollments, CourseMarks/Grades, CLOAttainment, AuditLogs, JournalEntryLines, PaymentGatewayEvents.
- Tenant: `ITenantScoped` (172 classes) + global filter; **145 entities TenantId-without-filter; ~40 core SIS entities no TenantId**.
- Missing indexes on Attendance.Date, CourseGrades composites, CLOAttainment, Result, Timetable.

## 6. Security boundaries

- Trust zones: Internet → Cloudflare edge → Portal → API middleware → EF filter → Postgres (NOBYPASSRLS role).
- Enforced: fallback auth policy, `[Authorize(Roles)]`, Casbin in-memory, ABAC tenant-header equality, PQC transport on finance, upload magic-byte checks, refresh-family rotation.
- Gaps: rate limiting edge-delegated; `IgnoreQueryFilters` audit needed; upload parser surfaces; EOL NEST.

## 7. Tenant-isolation model

JWT `tenant_id` claim → `TenantContextMiddleware` → `HttpContext.Items` → `AegisDbContext.CurrentTenantId` → global query filter + `TenantSessionInterceptor` GUC. Robust where `ITenantScoped`; **incomplete for User/Employee/StudentFee/FeePayment/StudentProfile/Student/Course/Enrollment/Attendance/Grades**.

## 8. License architecture

- Runtime: `LicenseLockoutMiddleware` per request → `GetStatusAsync` → `db.LicenseStates.ToListAsync()` **every request, no cache** (CONFIRMED_BY_CODE). Non-demo installed mode only; health/license paths exempt.
- Heartbeat: daily 08:00 external HTTPS (10s) with user count; 2 consecutive failures → global 403; SuperAdmin manual override escape; offline = fail-closed after 2 misses.
- Wizard: external `license.aegiscore.io/api/v1/validate` (15s, setup only).
- **Future design evaluated**: central authority → periodic refresh → signed license lease → local verification/cache → cheap local check per request. ERP should not need synchronous external license round trips (current heartbeat already avoids that). License authority is a possible future GO_CANDIDATE, confidence < 80 → MEASURE_FIRST.

## 9. Performance risks

1. License full-table read per request (HP-01).
2. OBE N+1 + in-memory AVG (HP-03).
3. Attendance load-all count (HP-04).
4. Grading per-student requery (HP-05).
5. Sync document/report generation (R8) + double serialization (LB-04).
6. Frontend waterfalls + no SWR.
7. Connection pool + Argon2 login at 10k.

## 10. Database risks

- Incomplete tenant scoping (SEC-04) → cross-tenant read/write potential.
- Missing indexes (A4) on hot lookups.
- High-cardinality tables without composite indexes; `ReviewComments.StartsWith` scan in grade approvals.
- MaxPoolSize 200 vs 10k users; single Postgres primary (WAL replica in hybrid).
- Quartz hourly backup on same instance.

## 11. Top optimization opportunities

1. Cache license lockout (kill per-request DB read) — highest ROI.
2. OBE attainment → SQL aggregates.
3. Attendance percentage → COUNT aggregate.
4. Add missing indexes.
5. Targeted origin rate limits for expensive endpoints.
6. Grading batch evaluation.
7. Portal data-fetch consolidation + SWR.
8. AI providers → IHttpClientFactory.
9. Report builders: single-pass serialization; async document generation off the request path.
10. Add OpenTelemetry so the rest can be measured.

## 12. Top security concerns

1. Tenant escape via non-filtered entities / IgnoreQueryFilters (F1).
2. No origin rate limiting (brute force/DoS of expensive endpoints) (F2).
3. IDOR/BOLA surface on ID-addressed endpoints (SEC-06).
4. Upload/parser abuse (SEC-11).
5. Webhook replay window (SEC-07).
6. localStorage tokens + CSP reliance (SEC-09).
7. EOL NEST client (SEC-18).

## 13. KEEP_DOTNET components

All core ERP domains: Students, Admissions, Faculty, Academics, Registrar, Attendance, Examinations, Grades, OBE, Finance, HR, Procurement, Workflows, RBAC/ABAC, Campus ops, Documents, Reporting, digital twin, AI orchestration, grade approval (HOD queue pattern is correct). No evidence supports migrating any core domain.

## 14. OPTIMIZE_DOTNET components

License cache, OBE aggregation, attendance aggregates, grading batch, finance report builders, AI HttpClient factory, index additions, async doc generation, telemetry instrumentation, dead-code removal.

## 15. GO_CANDIDATE components

**None approved.** Two `MEASURE_FIRST` candidates (confidence < 80, no performance data yet):
- GO-1 Central license authority (independent scaling, lightweight network service, independent lifecycle) — valid characteristics but needs license-server traffic data.
- GO-2 Telemetry/analytics ingestion (if ever built: high-volume ingest, independent scaling) — currently no ingestion exists.
Decision rule applied: no microservice from fashion; ERP domains stay .NET.

## 16. RUST_CANDIDATE components

**None approved.** Conditional: if hostile-parser hardening is pursued (PDF/XLSX/OCR/DocumentIntelligence), a Rust high-assurance parser boundary is technically justified, but requires security/load evidence first (uploads currently go through managed .NET/Azure parsing).

## 17. SECURITY_REVIEW items

Tenant-scoping audit (F1), origin rate limits (F2), webhook replay (F3), upload parsers (F4), ABAC/Casbin semantics (F5), refresh/localStorage (F6), NEST migration (F7), auditor token path (F8), data-import SSRF (F9).

## 18. LOAD_TEST_REQUIRED items

Login (Argon2+GeoIP), dashboards, registration, grade entry, OBE reports, finance reports, transcript generation, verification endpoints, 10k-user full mix. See `AWS_LOAD_TEST_PLAN.md`.

## 19. Documentation gaps

- HEMIS export actual behavior (PARTIAL vs IMPLEMENTED) — verify.
- Document-vault content-type validation absent; auth-header path for portal upload unverified.
- Swagger UI not exposed (JSON only) — API exploration gap.
- No OpenTelemetry → no runtime perf data anywhere.
- `aegiscore-backend/`/`aegiscore-frontend/` stale code confuses tooling (flagged).
- Moodle/Teams/PowerBI/SSO-HITL are design-doc-driven (PARTIAL) — mark clearly in docs.
- Existing `docs/12-performance-report.md` and `docs/13-quality-evaluation-report.md` predate this profiling and should be refreshed with real measurements.

## 20. Recommended implementation sequence

1. **Measure** (Queue G): OTel + dotnet-counters + pg_stat_statements + staged load test → baseline.
2. **A1** license cache; **A2** OBE SQL; **A3/A4** attendance + indexes (fast, low risk, high ROI).
3. **F1/F2** tenant audit + targeted rate limits (security).
4. **A5-A7/B1-B5** grading, HttpClient factory, finance sweep, frontend waterfalls/lint.
5. **F3-F9** remaining security queue.
6. **A8** dead-code removal; **R8** async document generation.
7. Re-run load test; regression gate.

---

## Final priority table

| PRIORITY | COMPONENT | CURRENT STATE | CLASSIFICATION | RECOMMENDED ACTION | OWNER AGENT | EVIDENCE | CONFIDENCE |
|---|---|---|---|---|---|---|---|
| P0 | License middleware/service | Per-request LicenseStates ToListAsync, no cache | OPTIMIZE_DOTNET | Cache IsLocked (TTL) / signed lease; keep fail-closed | A | `LicenseService.cs:238-242`, `LicenseLockoutMiddleware.cs:28` | 90 |
| P0 | OBE attainment | N+1 + in-memory AVG | OPTIMIZE_DOTNET | SQL aggregates + batch | A | `ObeServices.cs:83-126` | 90 |
| P0 | Tenant scoping | 145 filtered-less + 40 unfiltered entities; IgnoreQueryFilters | SECURITY_REVIEW | Full audit + predicates/RLS | F | `DATA_MODEL.md` §2, `SEC-03/04` | 85 |
| P1 | Origin rate limits | Edge-delegated; middleware dead | SECURITY_REVIEW | Targeted limits (auth/expensive) | F | `SEC-02`, `Program.cs:173-176` | 85 |
| P1 | Attendance aggregates | Load-all count | OPTIMIZE_DOTNET | COUNT aggregate + index | A | `AttendanceRepository.cs:13-24` | 85 |
| P1 | Missing indexes | Date/grade/CLO/timetable gaps | OPTIMIZE_DOTNET | Add composites | A | `DATA_MODEL.md` §6 | 80 |
| P2 | Grading N+1 | Per-student requery | OPTIMIZE_DOTNET | Batch evaluation | A | `GradingService.cs:93-116` | 80 |
| P2 | Report builders | Sync + double serialize | OPTIMIZE_DOTNET | Single-pass + async off-request | A | `FinanceModuleEndpoints.cs:1815-1985` | 70 |
| P2 | Frontend fetches | Waterfalls, no SWR, zero-GUIDs | OPTIMIZE (JS) | Consolidate, SWR, derive identity | B | `page.tsx:81-103`, ROUTE §6 | 80 |
| P2 | AI HttpClient | Raw ctor client | OPTIMIZE_DOTNET | IHttpClientFactory | A | `SEC-05` | 85 |
| P3 | NEST client | EOL 7.17.5 | SECURITY_REVIEW | Migrate to Elastic.Clients.Elasticsearch | F | `docs/DEPENDENCY_AUDIT.md` | 90 |
| P3 | Upload parsers | Size-only vault, PDF/XLSX/OCR | SECURITY_REVIEW | Parser hardening + pentest | F | `SEC-11` | 75 |
| P3 | Webhook replay | HMAC+Redis, window unverified | SECURITY_REVIEW | Verify window + pentest | F | `SEC-07` | 70 |
| P4 | Telemetry | No OTel, console Serilog | OPTIMIZE_DOTNET | Instrument + dashboards | G | `R11` | 85 |
| P5 | License authority (future) | Heartbeat only | GO_CANDIDATE? | MEASURE_FIRST (conf <80) | D | `R1`, §8 | 55 |
| P5 | Telemetry ingestion (future) | Not built | GO_CANDIDATE? | MEASURE_FIRST (conf <80) | D | §15 | 40 |
| P5 | Rust parser (future) | Not built | RUST_CANDIDATE? | MEASURE_FIRST (conf <80) | E | §16 | 40 |
