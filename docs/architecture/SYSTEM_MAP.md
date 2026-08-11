# AegisCore SYSTEM_MAP.md

Repository Intelligence artifact. Generated 2026-08-11 on branch `agent/prod-hardening`.
Source of truth: `Aegiscore-demo-integration` repo root. `aegiscore-backend/` is a stale git-tracked duplicate and is NOT authoritative.

## 1. Top-level repository layout

| Path | Role | Authority |
|---|---|---|
| `AegisCore.API/` | ASP.NET Core web host (Program.cs, controllers, middleware, finance minimal-API endpoints) | AUTHORITATIVE |
| `AegisCore.Application/` | Use cases, service contracts, DTOs, MediatR, PDF/QR libs | AUTHORITATIVE |
| `AegisCore.Contracts/` | Cross-layer contracts + common interfaces | AUTHORITATIVE |
| `AegisCore.Domain/` | Domain entities (`Entities/*.cs`, 601 classes), `Common/ITenantScoped` | AUTHORITATIVE |
| `AegisCore.Infrastructure/` | EF Core `AegisDbContext`, repositories, security (PQC, auth, audit), finance module, cloud providers, Redis | AUTHORITATIVE |
| `AegisCore.LedgerVerifier/` | Standalone executable that verifies PQC-signed finance ledger archives | AUTHORITATIVE |
| `AegisCore.Tests/` | Test project | AUTHORITATIVE |
| `AegisCore.PluginCli/` `AegisCore.PluginSdk/` | Plugin scaffolding CLI + SDK | AUTHORITATIVE |
| `AegisCore.Portal/` | **Primary frontend** Next.js 16 / React 19 (App Router) | AUTHORITATIVE |
| `AegisCore.Mobile/` `AegisCore.Native/` | Mobile workspaces (React Native shell / native) | Secondary |
| `aegiscore-backend/` | **STALE DUPLICATE** of the .NET backend (6.3 MB, older) | IGNORE |
| `aegiscore-frontend/` | **LEGACY** Vite + React 18 demo shell | IGNORE (superseded) |
| `aegiscore-android/` `aegiscore-mobile/` `aegiscore-mobile-app/` | Mobile app copies | Secondary |
| `deploy/` | Production compose, Prometheus config, runbooks | Ops |
| `k8s/` | Kubernetes manifests (deployment, HPA, statefulsets, network policy) | Ops |
| `docs/` | SDD docs (01–30), agent reports | Docs |
| `graphify-out/` | Graphify knowledge graph (graph.json, GRAPH_REPORT.md, graph.html) | Generated |

## 2. Runtime request architecture

```
Browser (Next.js 16 Portal, AegisCore.Portal)
   │  JWT in localStorage (aegiscore_access_token / refresh)
   ▼
Cloudflare (edge): WAF, Turnstile, Authenticated Origin Pulls, rate limiting (delegated per Program.cs:173-176)
   │
   ▼
Next.js rewrites /api/*, /health/*, /hubs/* → AEGISCORE_API_INTERNAL_URL (http://api:8080)
   │
   ▼
Kestrel (MaxConcurrentConnections=1000, MaxRequestBodySize=50MB)
   │
   ▼
Program.cs middleware pipeline (installed mode, order at Program.cs:161-193):
  1. UseHsts / UseHttpsRedirection          (prod only)
  2. GlobalExceptionMiddleware               (security headers + 500 JSON)
  3. UseSerilogRequestLogging
  4. RequestLoggingMiddleware                (X-Request-Id)
  5. AuthenticatedOriginPullsMiddleware      (Cloudflare mTLS thumbprint → 403)
  6. DemoExpiryMiddleware
  7. HybridPqcTransportMiddleware            (/api/finance*, Redis nonce per request when enforced)
  8. SessionBindingMiddleware                (X-Session-Binding vs __Host-session-id)
  9. UseCors("CloudflarePages")
 10. UseAuthentication                       (JWT Bearer)
 11. DemoIdentityMiddleware                  (demo mode identity injection)
 12. LicenseLockoutMiddleware  ← LICENSE DB READ PER REQUEST (non-demo)
 13. TenantContextMiddleware                 (tenant_id claim → HttpContext.Items)
 14. UseAuthorization                        (fallback = RequireAuthenticatedUser)
 15. TurnstileMiddleware                     (cf-turnstile-response verify → Cloudflare)
 16. AbacMiddleware                          (X-Tenant-Id header vs claim)
 17. CasbinAuthorizationMiddleware           (role/path/method enforcer)
   │
   ▼
Controller / Minimal-API endpoint group
   │
   ▼
Application/Infrastructure service
   │
   ▼
EF Core AegisDbContext (TenantSessionInterceptor sets app.tenant_id GUC; global query filter TenantId == CurrentTenantId)
   │
   ▼
PostgreSQL 17 (pgvector build) ─ Redis 7 (cache, refresh families, replay store, idempotency)
```

## 3. Solution/project dependencies

```
AegisCore.API ─┬─ AegisCore.Application ── AegisCore.Contracts ── AegisCore.Domain
               ├─ AegisCore.Infrastructure ─┬─ AegisCore.Application
               │                            ├─ AegisCore.Contracts
               │                            └─ AegisCore.Domain
               ├─ AegisCore.Domain
               └─ AegisCore.Contracts
AegisCore.LedgerVerifier ── AegisCore.Infrastructure
```

Key NuGet surface:
- API: JwtBearer, prometheus-net, Npgsql EF, Serilog (+PostgreSQL sink, unused), Casbin.NET, StackExchange.Redis, FluentValidation, HealthChecks, AspNetCoreRateLimit (unused), Hellang.ProblemDetails.
- Application: MediatR, PdfSharpCore, QRCoder, Quartz, SixLabors.ImageSharp, AutoMapper.
- Infrastructure: AWSSDK.S3/SecretsManager, Azure.* (DocumentIntelligence, KeyVault, Identity), BouncyCastle, Argon2, Polly, Microsoft.Graph, NEST 7.17.5 (EOL — see DEPENDENCY_AUDIT), Google Secret Manager, Casbin.NET, Serilog, System.Security.Cryptography.Xml.

## 4. Middleware inventory (AegisCore.API/Middleware + Infrastructure)

| Middleware | File | Per-request DB/network? |
|---|---|---|
| RequestLoggingMiddleware | `AegisCore.API/Middleware/InfrastructureMiddleware.cs:11` | No |
| GlobalExceptionMiddleware | `InfrastructureMiddleware.cs:48` | No (security headers) |
| AuthenticatedOriginPullsMiddleware | `InfrastructureMiddleware.cs:94` | No (mTLS thumbprint check) |
| SessionBindingMiddleware | `InfrastructureMiddleware.cs:122` | No |
| HybridPqcTransportMiddleware | `InfrastructureMiddleware.cs:152` | Redis write (nonce) + body hash on /api/finance* when enforced |
| TurnstileMiddleware | `InfrastructureMiddleware.cs:233` | External HTTPS → Cloudflare (only when token present) |
| TenantContextMiddleware | `Middleware/TenantContextMiddleware.cs:5` | No (claim → Items) |
| **LicenseLockoutMiddleware** | `Middleware/LicenseLockoutMiddleware.cs:5` | **YES — full LicenseStates table read on every request** |
| DemoExpiryMiddleware | `Middleware/DemoExpiryMiddleware.cs:11` | No (HMAC verify) |
| DemoIdentityMiddleware | `Middleware/DemoIdentityMiddleware.cs:9` | No (demo only) |
| CasbinAuthorizationMiddleware | `Middleware/CasbinAuthorizationMiddleware.cs:10` | No (in-memory enforcer) |
| AbacMiddleware | `Infrastructure/Security/Abac/AbacPolicyService.cs:43` | No |
| SetupRequiredMiddleware | `Middleware/SetupRequiredMiddleware.cs:7` | Setup mode only |
| GlobalExceptionHandlerMiddleware | `Middleware/GlobalExceptionHandlerMiddleware.cs:11` | NOT wired |
| GlobalRateLimitMiddleware | `Middleware/GlobalRateLimitMiddleware.cs:6` | NOT wired (edge-delegated) |
| RateLimitAuthMiddleware | `Middleware/RateLimitAuthMiddleware.cs:10` | NOT wired |
| RateLimitingMiddleware | `Middleware/RateLimitingMiddleware.cs:8` | NOT wired |
| ValidationExceptionMiddleware | `Middleware/ValidationExceptionMiddleware.cs:11` | NOT wired |

## 5. Frontend client architecture

- Two API clients: `src/lib/api-client.ts` (browser, Bearer from localStorage, 401→refresh retry) and `src/lib/server-api.ts` (RSC, forwards cookie jar, `cache: "no-store"`).
- No middleware.ts, no route guards server-side; role gating is client-side UI only (AppShell + `roles.ts`). API is the enforcement boundary.
- Session: `GET /api/session` → `{ userId, tenantId, studentId, departmentId, name, roles }`.
- Multi-tenancy: no tenant header; tenant rides in JWT claims server-side.

## 6. Observability

- Serilog → Console (JSON in prod). No file/seq/elastic sink configured (PostgreSQL sink package referenced but unused).
- prometheus-net: `UseHttpMetrics` + tenant label, `MapMetrics()` anonymous (Program.cs:257-261). No custom metrics defined.
- Health: `/health`, `/health/live`, `/health/ready`, `/health/detail` (auth), `/health/deep` (auth), `/health/installation`, `/portal-health`.
- OpenTelemetry: NOT present.

## 7. Deployment topologies

- Docker Compose: base (`postgres,redis,api,portal`), `cloud` (Supabase postgres override, Redis only), `hybrid` (WAL primary), `uni` (university prod: + elasticsearch, minio, cloudflare-tunnel), `local-demo` (postgres 17-alpine, demo flags).
- k8s: api 3 replicas (HPA min3/max10 @70% CPU), portal 2 replicas, postgres StatefulSet (50Gi), redis (5Gi AOF), network-policy default-deny, ingress nginx TLS.
- Cloudflare assumed at edge (Program.cs comment defers rate limiting; AuthenticatedOriginPulls; Turnstile; CF-Connecting-IP in login audit; R2 storage provider; tunnel health check).
