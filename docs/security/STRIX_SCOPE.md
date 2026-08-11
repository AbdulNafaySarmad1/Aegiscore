# AegisCore STRIX_SCOPE.md

Authorized future penetration-test scope for a DISPOSABLE staging environment. Generated 2026-08-11, branch `agent/prod-hardening`.
This is a scope definition ONLY. No destructive testing has been run. Any actual pentest must be explicitly authorized and executed against a disposable staging environment, never the working demo.

## 1. Scope boundary

- Target: disposable staging deployment (docker-compose or k8s clone) with seed data. NOT the production/demo instance.
- In-scope: AegisCore.API origin (bypass Cloudflare where feasible to test origin; otherwise test through edge and note edge assumptions), AegisCore.Portal, webhooks, upload surfaces, verification endpoints.
- Out-of-scope: destructive DB operations, data destruction, DoS against shared infra, third-party SaaS (Cloudflare, gateways, AI providers) — do not attack them, only test our handling of their inputs.

## 2. Authentication & roles

- Auth: JWT bearer (access 15min, refresh family 14d in Redis), `/api/auth/login|refresh|logout`, `/api/session`.
- Roles to exercise: SuperAdmin, Admin, VC, Registrar, ExamController, HOD, Dean, Faculty, Student, Finance/CFO/Finance Controller, HR/HRManager, OBECoordinator, Auditor (opaque token), Employee, IT.
- Test: privilege escalation across role ladder; HOD→department boundary; Auditor token path; refresh-token reuse/rotation; logout semantics; token in localStorage (XSS chained with CSP bypass).

## 3. API surface to cover

- `/api/auth/*`, `/api/session`, `/api/identity/*`
- `/api/student-portal/*`, `/api/student-attendance/*`, `/api/student-documents/*`
- `/api/faculty-grade-entry/*`, `/api/grade-approvals`, `/api/grade-appeals/*`, `/api/obe/*`, `/api/obe/v2/*`
- `/api/attendance`, `/api/timetable`, `/api/courses/*`
- `/api/finance/*`, `/api/finance/v2/*`, `/api/auditor/*`, `/api/webhooks/{tenantId}/{gatewaySlug}`
- `/api/document-vault`, `/api/profile-pictures`, `/api/data-import/*`, `/api/catalog/ingest/*`
- `/api/admin/*` (dr, replication, sync-lag, licenses, zero-trust, hardcoding-audit, queues, diagnostics)
- `/api/developer/*`, `/api/marketplace/*`, `/api/feature-flags`, `/api/setup/*`, `/api/wizard/*`
- `/api/examination/transcripts/verify` and other anonymous verify endpoints
- SignalR hubs `/hubs/*`

## 4. Tenant boundaries

- Multi-tenant model: JWT `tenant_id` claim → `HttpContext.Items` → EF global filter; Postgres GUC `app.tenant_id`; NOBYPASSRLS app role.
- Test targets:
  - Every `IgnoreQueryFilters()` call site (AuthController, FinanceWebhookController, TransportService, GradeApprovalDiscoveryController, finance services).
  - 145 entities with TenantId but no EF filter; 40 core SIS entities with no TenantId (Student, Course, Enrollment, Attendance, Grades, CLOAttainment, AdmissionApplication, MeritList, GradeOverrideRequest).
  - Cross-tenant ID guesses on ID-addressed endpoints (IDOR/BOLA).

## 5. Upload surfaces

- Document vault (50MB, no content validation), profile pictures (5MB, magic bytes + face heuristic), branding (10MB), grade-sheet imports (CSV/XLSX/PDF), OCR uploads (Azure DocumentIntelligence), CIS evidence, grade-appeal attachments, proctoring face images, voice audio, voucher/vendor attachments, setup templates.
- Test: malicious PDF/XLSX/zip (OOXML) parser abuse, magic-byte bypass, polyglot files, path traversal in filenames, oversized payloads, decompression bombs (zip), content-type spoofing.

## 6. Priority validation list

| Priority | Category | What to prove/refute |
|---|---|---|
| 1 | Tenant escape | Achieve cross-tenant read/write via IgnoreQueryFilters, non-filtered entities, or ID enumeration |
| 1 | IDOR/BOLA | Access another student's transcript, grades, finance, documents by changing IDs |
| 1 | Authorization bypass | Privilege escalation (Student→Faculty→HOD→Admin); missing attribute on mutating endpoint |
| 1 | RBAC/ABAC bypass | Casbin allow-list gaps; X-Tenant-Id manipulation; role-claim tampering |
| 2 | Grade manipulation | Forge grade entry; change grades without moderation; HOD approval outside department |
| 2 | Approval bypass | Approve/reject with wrong actor; bypass grade-change workflow |
| 2 | File upload abuse | Parser abuse, magic-byte bypass, traversal, zip bomb |
| 2 | Parser abuse | PDF/XLSX/OCR hostile inputs causing crash/hang/RCE |
| 2 | Finance authorization | Non-finance role reads/writes vouchers; auditor token misuse |
| 3 | Authentication bypass | Refresh reuse, token forging (HS256 vs RS256 confusion), session fixation |
| 3 | SSRF | Data-import external connections; gateway/webhook URL config; GeoIP/turnstile endpoints |
| 3 | Injection | SQL injection (esp. data-import queries, moderation parsing), XSS via i18n/forms, header injection |
| 3 | Business logic | Registration race (seat double-booking), payment idempotency, fee evasion, replication/sync-lag manipulation |
| 3 | Low-rate fuzzing | Slow authenticated requests, expensive endpoints (transcript/OBE/reports), abusive pagination, query amplification |
| 3 | Resource exhaustion | 50MB uploads, unbounded report ranges, connection-pool drain (license read amplification) |

## 7. Rules of engagement (for the executing agent)

- Use a disposable staging environment; seed with golden-demo data.
- Do not hit production credentials, real gateways, or third-party services.
- After each finding: reproduce → document exact location + evidence + exploitability assumptions + severity + affected boundary + proposed remediation.
- Report format matches `docs/architecture/SECURITY_BOUNDARIES.md` finding IDs (SEC-xx) or new IDs.

## 8. Out of scope (rejected for now)

- Cryptographic breakage of PQC primitives (treat SLH-DSA/ML-DSA/ML-KEM as sound; test integration/usage, not math).
- Cloudflare edge itself.
- Third-party SaaS abuse.
