# AegisCore HOT_PATH_TRACES.md

Repository Intelligence artifact. Generated 2026-08-11, branch `agent/prod-hardening`.
End-to-end traces of the important operations. Each step notes DB/network/crypto work. Classification per finding.

## T1 — LOGIN

1. Browser `POST /api/auth/login {email,password}` (portal `login/page.tsx:37`) → Next rewrite → API.
2. Middleware: exception, logging, mTLS, DemoExpiry (skip), PQC transport (not finance), SessionBinding (no session yet), Cors, Authentication (none), LicenseLockout (**DB read LicenseStates**), TenantContext (no claims), Authorization (AllowAnonymous), Turnstile (if token).
3. `AuthController.Login` (`AuthController.cs:26`): user query `IgnoreQueryFilters()` with `Include(UserRoles).ThenInclude(Role)` (`:43-44`).
4. Argon2id password verify (Konscious, CPU-heavy).
5. `ResolveTenantId()` from config `TENANT_ID` (`:158-161`).
6. `JwtTokenService` issues access (15min) + refresh (14-day family in Redis) (`:82,116-154`).
7. Login audit + GeoIP external call (`https://ipapi.co/{ip}/json`, 3s) (`LoginAuditService.cs:44-47`) — verify async/non-blocking.
8. Response tokens.
- **Costs**: 1 DB read (license) + 1 DB read (user+roles) + Argon2 + 1 Redis write + 1 external GeoIP. `LB-01`.

## T2 — STUDENT DASHBOARD

1. Portal `/` (`page.tsx:81-103`): `GET /api/session` → then `GET /api/student-portal/me` → then `Promise.all[attendance/history, finance/v2/student-portal/:id]`. **3-deep serial waterfall.**
2. Each request through middleware: **license DB read per request** (3 more reads).
3. `/api/student-portal/me` → StudentPortalController → queries Student (no TenantId entity) via parent scoping + finance KPI queries.
4. Attendance history → `AttendanceRepository.GetAttendancePercentageAsync` (load-all) if used; date-range query.
5. Finance student portal → finance module tenant-scoped queries.
- **Costs**: 4 API round trips (3 serial) + ≥4 license reads + attendance load + finance queries. `LB-02`.

## T3 — FACULTY DASHBOARD / CLASSES

1. `/api/session` → `/api/courses/my` (or admin offerings+sections). Serial on session (`classes/page.tsx:31-58`).
2. `FacultyCoursesController` → course query scoped by Faculty→Department.
- **Costs**: 2-3 serial requests + license read each. Fine at 10k.

## T4 — COURSE REGISTRATION

1. `GET /api/student-portal/registration` (list) then `POST` register (`student/courses/page.tsx:64,93`; full refetch after mutation).
2. Registration service: check conflicts, seat availability, fee computation (dynamic fee on credit hours — `U10` design), enroll + mark moderation writes.
- **Costs**: N queries per registration (conflict/seat/fee/enroll). `NEEDS_RUNTIME_MEASUREMENT`.

## T5 — ATTENDANCE MARKING

1. Faculty selects course (`/api/courses/my`), loads roster (`/api/attendance/roster/:course`), POST `/api/attendance/mark` (hardcoded `markedByFacultyId` zero-GUID in `attendance/page.tsx:46`).
2. Server should derive actor from JWT — verify; zero-GUID in payload is a data-integrity/audit smell.
- **Costs**: roster read + batched attendance upsert. Offline-first sync designed (`U11`).

## T6 — GRADE ENTRY

1. Faculty `POST /api/faculty-grade-entry` (manual/Excel/PDF/OCR modes) → `FacultyGradeEntryController:18`.
2. `GradingService.ApplyGracemarksAsync` / `CalculateCourseGradeAsync` per student (`GradingService.cs:12-116`) → **N+1 re-queries.**
3. Grade-change path writes `MarkModeration` with `ReviewComments = "GRADE_CHANGE:..."` payload (parsed by `GradeApprovalDiscoveryController.Parse`, `:82-87`).
4. Optional OCR upload (Azure DocumentIntelligence) + Excel/PDF import preview/commit.
- **Costs**: per-student grading queries (HP-05). `CONFIRMED_BY_CODE`.

## T7 — GRADE CHANGE / APPROVAL

1. Faculty submits grade change → moderation record (status submitted, `GRADE_CHANGE:` comment).
2. HOD/ExamController `GET /api/grade-approvals` (`GradeApprovalDiscoveryController.Queue`): AllowedCourses (department-scoped, `:72-80`) → pending `MarkModerations` filter + string prefix scan → batch-load courses/submitters/students/components (5 queries, correct batching) → return.
3. `POST .../approvals/:id/review` → approval decision.
- **Costs**: efficient batching in the new endpoint (good). Watch `ReviewComments.StartsWith` full-scan on large tables — add index/computed column later.

## T8 — TRANSCRIPT GENERATION

1. `POST /api/student-documents/transcript/official` (`StudentDocumentsController.cs:28-109`): query transcript data, `BuildTranscriptPdf` (PdfSharpCore, `:237-357`), SLH-DSA PQC sign, QR encode, persist to disk `official-transcripts/{tenantId}/`, return document.
2. Download `GET transcript/{documentId}/download` (`:112-130`); public verify `GET .../verify` (`:133-158`).
- **Costs**: synchronous PDF+sign+QR+disk. `LIKELY_BOTTLENECK` (R8). CPU-bound per request.

## T9 — OBE/CLO/PLO

1. Faculty/OBE coordinator uploads CIS (`/api/obe/v2/cis/:id/upload`, process) → `S5ObeController`.
2. Attainment: `OBEAttainmentService` — PO loop → mapping loop → CLO calc (**N+1 + in-memory AVG**, HP-03).
3. `Hemis()` export; `ApproveCis()`; `MapCloToPo()`; `PoAttainment()`.
- **Costs**: HP-03 confirmed. `CONFIRMED_BY_CODE`.

## T10 — OBE REPORT GENERATION

1. `POST /api/obe/faculty/course/:id/signed-report` → PDF + PQC + QR (synchronous, `FacultyObeController`).
2. Underlying attainment = T9 N+1 + in-memory aggregation.
- **Costs**: HP-03 + R8. Two compounding hot paths.

## T11 — FINANCE / PAYMENT

1. `GET /api/finance/v2/dashboard/executive` (server comp) → finance module tenant-scoped aggregate queries.
2. Payment: student `POST /api/finance/v2/payments/:gw/request-to-pay` → gateway adapter (external HTTPS, 30s, Polly) → webhook inbound `POST /api/webhooks/{tenantId}/{gatewaySlug}` (HMAC+PQC, Redis idempotency) → ledger posting + PQC signature queue.
3. Reports: `signed-csv/xlsx/pdf/xbrl/ixbrl` → serialize→re-parse JsonDocument → build → sign (R8, LB-04).
- **Costs**: external gateway latency + synchronous report building + PQC per doc. `MEASUREMENT_REQUIRED`.

## T12 — FILE UPLOAD

1. Portal uploads (document-vault 50MB raw fetch without auth header — relies on cookie; profile 5MB with Bearer; wizard branding 10MB; proctoring/voice/OCR ≤10MB).
2. Server: `[RequestSizeLimit]` + `UploadValidationService` (magic bytes, traversal) → storage provider → PQC/audit as needed.
- **Costs**: I/O + magic-byte check + storage write + optional signing. Parser abuse risk on PDF/XLSX/OCR (SEC-11).

## T13 — DOCUMENT GENERATION (finance)

See T11 reports. Hand-built OOXML ZIP (`ReportXlsx`), CSV (`ReportCsv`), XBRL/iXBRL (`ReportXbrl`) — all re-parse serialized JSON. `LIKELY_BOTTLENECK`.

## T14 — PDF GENERATION

See T8/T10/T11. PdfSharpCore + DejaVu fonts on Alpine (Dockerfile installs fontconfig/ttf-dejavu). Synchronous.

## T15 — PQC SIGNING

1. Finance: `PqcSigningQueue` (bounded channel, single reader) signs ledger entries/vouchers; `FinanceLedgerSignatureWorker` signs posted entries (batch 100, 1min); `FinanceAuditSealWorker` seals audit logs (15min).
2. Documents: synchronous SLH-DSA at generation time.
3. Transport: `HybridPqcTransportMiddleware` ML-KEM-1024+X25519 envelope, SHA-256 body hash, HMAC, Redis nonce — per `/api/finance*` request when `HybridPqcTransport:Enforce`.
- **Costs**: queue-based (good) vs synchronous per-doc (ok at low rates). Nonce Redis write per finance request when enforced.

## T16 — QR VERIFICATION

1. Public `GET .../verify` on transcripts/OBE (anonymous) → parse QR payload → re-compute/hash-check vs stored `VerificationHash` → return.
- **Costs**: single indexed lookup (`Transcript.VerificationHash` UNIQUE). Low. Verify rate-limit surface (public endpoint, no auth) — `SEC-02` applies.

## T17 — LICENSE VALIDATION

1. Per-request: `LicenseLockoutMiddleware` → `GetStatusAsync` → **`db.LicenseStates.ToListAsync()` full table** (HP-01). No cache. `CONFIRMED_BY_CODE`.
2. Daily 08:00: heartbeat external HTTPS (10s), `current_user_count = db.Users.CountAsync`, fail→increment; ≥2 → global 403.
3. Wizard: `OnboardingService.ValidateLicenseAsync` external 15s (setup only).
4. SuperAdmin manual override endpoint.
- **Classification**: `LIKELY_BOTTLENECK`. Future: signed lease + local cache (see ARCHITECTURE_RISKS R1).

## T18 — TELEMETRY

1. No OpenTelemetry. prometheus-net `UseHttpMetrics` + tenant label; `/metrics` anonymous.
2. Serilog console request logging with sensitive-data destructuring.
- **Costs**: minimal. Gap: no tracing → cannot prove most `LIKELY` findings. See PERFORMANCE_MAP §4.

## Classification summary

| Finding | Class |
|---|---|
| License per-request DB read (HP-01) | CONFIRMED_BY_CODE / LIKELY_BOTTLENECK |
| OBE N+1 + in-memory AVG (HP-03) | CONFIRMED_BY_CODE |
| Attendance load-all (HP-04) | CONFIRMED_BY_CODE |
| Grading N+1 (HP-05) | CONFIRMED_BY_CODE |
| Finance sweep per-installment (HP-07) | CONFIRMED_BY_CODE |
| Sync PDF/OBE/report generation (R8) | LIKELY_BOTTLENECK |
| Dashboard waterfall (LB-02) | LIKELY_BOTTLENECK |
| Login Argon2+GeoIP (LB-01) | NEEDS_RUNTIME_MEASUREMENT |
| Connection pool at 10k (LB-08) | NEEDS_RUNTIME_MEASUREMENT |
