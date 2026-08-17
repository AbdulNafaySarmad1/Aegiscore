# AegisCore SECURITY_BOUNDARIES.md

Repository Intelligence artifact. Generated 2026-08-11, branch `agent/prod-hardening`.
This is a static architecture review. Findings are classified `CONFIRMED_BY_CODE` (verified by reading code), `LIKELY` (pattern suggests it), or `NEEDS_RUNTIME_MEASUREMENT`. No destructive testing was performed. A future authorized pentest scope is in `docs/security/STRIX_SCOPE.md`.

## 0. Edge assumptions

- Cloudflare is the edge (WAF, Turnstile, Authenticated Origin Pulls, DDoS, rate limiting delegated per `Program.cs:173-176`). Origin assumes legitimate-looking low-frequency traffic can always reach it — so origin must be secure on its own.
- **Rate limiting middleware exists but is NOT wired** (`GlobalRateLimitMiddleware`, `RateLimitAuthMiddleware`, `RateLimitingMiddleware`). Only edge controls and per-endpoint logic apply. Expensive endpoints are therefore unprotected at origin from low-and-slow abuse.

## 1. Boundary model

```
[Internet] → Cloudflare edge (WAF/Turnstile/AOP/rate-limit assumptions)
   → [Boundary 1] Portal (Next.js rewrites; no auth enforcement)
   → [Boundary 2] API Kestrel + middleware
        → [Boundary 3] EF/AegisDbContext tenant filter + app.tenant_id GUC
   → PostgreSQL (app role NOBYPASSRLS) / Redis
```

## 2. Findings

### HIGH severity

| ID | Finding | Evidence | Classification |
|---|---|---|---|
| SEC-01 | **License full-table read on every request** — not a vulnerability per se, but a DoS amplifier: `LicenseLockoutMiddleware` → `LicenseService.CurrentAsync` → `db.LicenseStates.ToListAsync()` on every non-demo request with no cache (`LicenseService.cs:238-242`; `LicenseLockoutMiddleware.cs:28`). Under abuse it multiplies DB load; also means license lockout state is unbounded query cost. | Direct code | CONFIRMED_BY_CODE |
| SEC-02 | **Rate limiting not enforced at origin** — auth and expensive endpoints (transcript generation, OBE reports, login) have no origin-side limit; relies solely on Cloudflare edge. A bypassed/misconfigured edge = brute-force + DoS surface. | `Program.cs:173-176`, middleware unregistered | CONFIRMED_BY_CODE |
| SEC-03 | **`IgnoreQueryFilters()` cross-tenant reads** — several endpoints disable the tenant filter and must re-apply tenant predicates manually: `AuthController.cs:43`, `FinanceWebhookController.cs:53`, `TransportService.cs:12`, grade/submitter lookups `GradeApprovalDiscoveryController.cs:32`. Any missed predicate = tenant escape / IDOR. | Direct code | CONFIRMED_BY_CODE (audit each call site) |
| SEC-04 | **~145 entities have TenantId but no EF global filter** (User, Employee, StudentFee, FeePayment, StudentProfile, ApiKey, WebhookSubscription, DocumentVaultEntry, etc.); **~40 core SIS entities have no TenantId at all** (Student, Course, Enrollment, Attendance, Grades, CLOAttainment...). Tenant isolation for these is app-code only or indirect. | `DATA_MODEL.md` §2 | CONFIRMED_BY_CODE |
| SEC-05 | **Raw `new HttpClient()` in constructors** — `OpenAiLlmProvider`, `OllamaLlmProvider`, embedding providers create `HttpClient` per instance (socket exhaustion risk under load). | Backend report §10 | CONFIRMED_BY_CODE |

### MEDIUM severity

| ID | Finding | Evidence | Classification |
|---|---|---|---|
| SEC-06 | **IDOR surface on ID-addressed endpoints** — many endpoints take `{id}` and rely on role attributes, not object ownership checks. Verified safe examples exist (HOD grade queue is department-scoped), but the surface is huge and uniform ownership checks are not evident. | API_CATALOG + controllers | LIKELY — needs per-endpoint audit |
| SEC-07 | **Webhook endpoint anonymous + HMAC** — `FinanceWebhookController` (no `[Authorize]`); validates HMAC + optional PQC. Idempotency via Redis. Replay risk if nonce/HMAC window not enforced; gateway slug routing must be tenant-scoped. | Backend report §4 | CONFIRMED_BY_CODE (verify replay window) |
| SEC-08 | **`X-Tenant-Id` header trust** — `AbacMiddleware` compares header to claim; if any client sends a mismatched header they get 403, but the middleware does NOT set tenant from the header — tenant always from JWT. Safe, but header is advisory. | `AbacPolicyService.cs:28-37` | CONFIRMED_BY_CODE |
| SEC-09 | **Auth flow** — access token 15 min, refresh 14-day family in Redis, atomic rotation, reuse→401 (good). Tokens in localStorage (XSS-exposed) — the portal relies on CSP to mitigate; demo credentials hardcoded in source (demo mode). | `JwtTokenService.cs`, `api-client.ts`, `login/page.tsx` | CONFIRMED_BY_CODE |
| SEC-10 | **Turnstile pass-through** — `TurnstileMiddleware` only verifies when token present; missing token + missing secret → pass-through. Low-frequency bot traffic with no token bypasses Turnstile entirely. | `InfrastructureMiddleware.cs:248-249` | CONFIRMED_BY_CODE |
| SEC-11 | **Upload validation gaps** — `UploadValidationService` checks magic bytes + traversal for images/PDF (10MB default). DocumentVault upload (50MB) has no content-type/parser hardening; PDF/XLSX/OCR parsers (PdfSharpCore, DocumentIntelligence, OOXML zip) process uploads → parser-abuse surface. | `DocumentVaultController.cs:49`, `UploadValidationService.cs` | CONFIRMED_BY_CODE (needs pentest) |
| SEC-12 | **Mass-assignment risk** — controllers bind DTOs; presence of auto-mapping (AutoMapper) + large DTO surfaces means binder allow-lists must be verified. Not evident from static scan alone. | Application layer | LIKELY — NEEDS_RUNTIME |
| SEC-13 | **Data import** — `DataImportService` opens external ADO.NET connections; `IsQuerySafe` guard exists (`DataImportController.cs:21-34`). Direct SQL to customer DBs = SSRF/arbitrary-schema risk if connection params untrusted. | Backend report §10 | LIKELY |

### LOW / hardening

| ID | Finding | Evidence |
|---|---|---|
| SEC-14 | `[RequirePermission]` infra unused → no granular permission checks; coarse roles only | grep zero usages |
| SEC-15 | `AspNetCoreRateLimit` package referenced but unused | csproj |
| SEC-16 | `GlobalExceptionHandlerMiddleware`, `ValidationExceptionMiddleware` unused (redundant) | files |
| SEC-17 | `AutoLockBackgroundService` orphaned (not registered) | API/Background |
| SEC-18 | `NEST 7.17.5` EOL/CRITICAL per DEPENDENCY_AUDIT — needs `Elastic.Clients.Elasticsearch` migration | docs/DEPENDENCY_AUDIT.md |
| SEC-19 | Serilog PostgreSQL sink referenced but not configured; logs console-only (no centralized audit retention beyond DB audit tables) | API csproj, Program.cs |
| SEC-20 | CSP/security headers set by `GlobalExceptionMiddleware` (good); verify CSP allows required external (Cloudflare, recharts, signalr) | `InfrastructureMiddleware.cs:48` |
| SEC-21 | Kestrel: MaxRequestBodySize 50MB, 1000 connections — sane; but unbounded queries (report endpoints) can hold connections long | Program.cs:53-63 |
| SEC-22 | Demo mode: `DemoIdentityMiddleware` injects SuperAdmin principal (non-prod); `Runtime:DemoMode` rejected in Production (`Program.cs:65-71`) — good guard | Direct |

## 3. Attack-surface priority (for STRIX scope)

1. Tenant escape via `IgnoreQueryFilters()` call sites and non-filtered entities.
2. IDOR/BOLA on ID-addressed academic/finance endpoints.
3. Grade manipulation / approval bypass (grade entry, approval review, moderation comments parsing `GRADE_CHANGE:`).
4. Finance authorization bypass (vouchers, auditor token path, webhooks).
5. File upload / parser abuse (PDF, XLSX OOXML, OCR, DocumentIntelligence).
6. Auth brute force (login) and refresh-token abuse — origin rate limiting absent.
7. SSRF via data-import external connections and gateway/webhook URL config.
8. Low-and-slow fuzzing and expensive-endpoint DoS (transcript/OBE/report generation).

## 4. Recurring secure patterns to preserve

- Fallback auth policy requires auth everywhere.
- HOD grade queue is department-scoped (correct pattern).
- Uploads validated by magic bytes + traversal checks.
- Refresh-token family rotation with reuse detection.
- PQC transport for finance with nonce replay protection.
- Demo mode hard-rejected in Production.
