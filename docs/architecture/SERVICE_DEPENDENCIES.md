# AegisCore SERVICE_DEPENDENCIES.md

Repository Intelligence artifact. Generated 2026-08-11, branch `agent/prod-hardening`.

## 1. Service registration layers

`AegisCore.API/Program.cs` composition (installed mode):
`AddCoreServices` + `AddInfrastructure` + `AddProductionServices` + `AddAuthenticationServices` + `AddBackgroundWorkers`.

Key registration files:
- `AegisCore.API/Extensions/ProductionServicesExtensions.cs` — CORS "CloudflarePages", auth, health, JwtBearer, SignalR, background workers.
- `AegisCore.Infrastructure/InfrastructureServiceExtensions.cs` — Redis singleton, EF `AddAegisDbContext`, license HttpClient (`license`), infisical HttpClient.
- `AegisCore.Infrastructure/Finance/FinanceModuleServiceExtensions.cs` — PQC queue, audit-seal workers, payment-plan worker, key-rotation worker, ledger-signature worker, vendor-rescreen worker, `finance-gateways`/`sanctions` HttpClients.
- `AegisCore.Infrastructure/Backup/DRServiceExtensions.cs` — health checks + Quartz backup job.
- `AegisCore.Infrastructure/Security/Pqc/PqcCryptographyService.cs` — weekly snapshot service.
- `AegisCore.Infrastructure/Deployment/SystemAlertService.cs`.

## 2. Background services (hosted workers)

| Worker | Class file | Interval | Notes |
|---|---|---|---|
| License heartbeat | `AegisCore.API/Background/LicenseHeartbeatHostedService.cs` | daily 08:00 | External HTTPS to license server; 2-fail lockout |
| PQC signing queue | `Infrastructure/Security/Pqc/PqcSigningQueue.cs` | continuous | bounded channel, single reader |
| Finance audit seal | `Infrastructure/Finance/AuditSealService.cs:71` | 15 min | Merkle-rooted PQC-sealed per-tenant audit |
| Finance ledger signature | `Infrastructure/Finance/AuditSealService.cs:151` | 1 min | PQC-sign posted entries, batch 100 |
| Payment plan worker | `Infrastructure/Finance/PaymentPlanWorker.cs:9` | initial + 1h | sweep all tenants |
| Vendor sanctions rescreen | `Infrastructure/Finance/VendorSanctionsService.cs:35` | 1 day | batch 100 |
| Automated backup (Quartz) | `Infrastructure/Backup/AutomatedBackupJob.cs` | hourly `0 0 * * * ?` | backups + retention |
| Login-audit archive | `Infrastructure/Security/LoginAuditService.cs:119` | continuous | NDJSON archive to storage |
| PQC key rotation | `Infrastructure/Security/Pqc/PqcKeyCeremonyService.cs:82` | 12h | near-expiry key ceremonies |
| System alerts | `Infrastructure/Deployment/SystemAlertService.cs` | 1 min | no-op |
| Weekly snapshot | `Infrastructure/Security/Pqc/PqcCryptographyService.cs:120` | 7d | no-op log |
| AutoLockBackgroundService | `API/Background/AutoLockBackgroundService.cs` | 1 min | **NOT REGISTERED (orphan)** |

## 3. External service dependencies

| External | Client | Timeout | Resilience | Used for |
|---|---|---|---|---|
| License server | `license` (IHttpClientFactory) | 10s | none (fail→lockout) | daily heartbeat |
| Cloudflare Turnstile | raw HttpClient | 5s | none | siteverify on POST with token |
| Twilio SMS | raw HttpClient | 15s | Polly retry×2 | SMS |
| WhatsApp Business | raw HttpClient | 15s | Polly retry×2 | WhatsApp |
| Microsoft Graph / Teams | `graph` | default | none | Teams provisioning |
| Moodle LTI | `moodle` | default | none | roster sync / grade passback |
| Power BI Embedded | raw HttpClient | 10s | circuit breaker + retry | embed tokens |
| RAAST (payments) | `RaastApiClient` (typed) | 30s | WaitAndRetry(3) + CircuitBreaker(5,30s) | bank transfers |
| Payment gateways (Stripe/Plaid/GoCardless/Billplz/DuitNow) | `finance-gateways` | 30s | HTTPS-enforced | payments |
| Sanctions screening | `sanctions` | 20s | none | OFAC/UN/FATF vendor screen |
| GeoIP (login audit) | raw HttpClient | 3s | none | `https://ipapi.co/{ip}/json` |
| Infisical secrets | `infisical` | default | cache 900s | secret provider |
| AI providers (OpenAI/Ollama + embeddings) | raw HttpClient in ctor | 1–5 min | none | AI/telemetry |
| Storage providers | S3/R2/Azure/GCP/SFTP stubs | — | — | files/docs/backups |
| AWS/Azure/GCP clouds | SDKs | — | — | cloud-agnostic providers |
| Onboarding license validate | raw HttpClient | 15s | none | `https://license.aegiscore.io/api/v1/validate` (setup wizard only) |

## 4. Dependency graph (runtime)

```
API (Program.cs)
 ├── AuthenticationServices (JWT)
 ├── Infrastructure
 │    ├── AegisDbContext ── PostgreSQL (Npgsql, retry 3×5s, MaxPoolSize 200)
 │    ├── Redis (IConnectionMultiplexer, AbortOnConnectFail=false)
 │    ├── Security (Casbin, Argon2, JWT, PQC, Audit, UploadValidation)
 │    ├── Finance module (workers, gateways, sanctions, PQC)
 │    ├── Cloud providers (S3/R2/Azure/GCP/on-prem)
 │    ├── Backup (Quartz + WAL)
 │    ├── AI providers
 │    └── Microsoft ecosystem (Graph/Teams/Moodle/PowerBI)
 ├── Application (MediatR, PdfSharpCore, QRCoder, AutoMapper)
 ├── Domain (entities, ITenantScoped)
 └── Contracts
```

## 5. Caching

- Redis: refresh-token families (`aegiscore:auth:refresh:family:*`), HybridPQC replay nonce store, webhook idempotency, Infisical secret cache (900s).
- **No caching** for: license status (per-request DB), session, course/student lookups, reports.
- EF: no second-level cache; `AsNoTracking` used selectively.

## 6. Redis misuse / risks

- License middleware could reuse Redis for a cached lease — currently it does a DB round trip instead (`LIKELY_BOTTLENECK`).
- Redis `AbortOnConnectFail=false` → silent cache degradation when Redis down (app continues, cache misses).
- No distributed lock usage identified for payment-plan sweep across multiple replicas (3 replicas) → duplicate sweep risk (idempotency keys mitigate).

## 7. Connection pool

- EF: `MaxPoolSize=200`, `CommandTimeout=30`, `ConnectionIdleLifetime=300`, retry 3×5s.
- Kestrel max 1000 concurrent connections. At 10k users with p95 < 300ms this is adequate; profile DB pool saturation under load (see AWS_LOAD_TEST_PLAN).
