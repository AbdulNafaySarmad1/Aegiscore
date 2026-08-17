# AegisCore EXTERNAL_INTEGRATIONS.md

Repository Intelligence artifact. Generated 2026-08-11, branch `agent/prod-hardening`.

## 1. Integration inventory

| Integration | Status | Files | Direction | Auth method |
|---|---|---|---|---|
| **Cloudflare Turnstile** | PARTIAL | `API/Middleware/InfrastructureMiddleware.cs:233-282` | Outbound POST to `challenges.cloudflare.com/turnstile/v0/siteverify` | Site secret; pass-through if no token |
| **Cloudflare Authenticated Origin Pulls** | IMPLEMENTED | `InfrastructureMiddleware.cs:94-120` | Inbound mTLS thumbprint check | `X-Argo-Client-Thumbprint` vs client cert |
| **Cloudflare R2 storage** | IMPLEMENTED | `Infrastructure/Cloud/R2/R2StorageService.cs` | Outbound S3 | AWS-style credentials |
| **Cloudflare Tunnel** | IMPLEMENTED (deploy) | `docker-compose.uni.yml`, `OnboardingService.cs:170-179`, `ExtendedHealthChecks.cs:48-58` | Outbound | tunnel token |
| **License server** | IMPLEMENTED | `License/LicenseService.cs:105-174`, `OnboardingService.cs:22-51` | Outbound daily heartbeat + wizard validate | HMAC token + HTTPS-only |
| **RAAST payments** | IMPLEMENTED | `Finance/Gateways/PaymentGatewayAdapters.cs`, `RaastApiClient` | Outbound `rtp/requests` | per-gateway creds, HTTPS enforced |
| **Stripe / Plaid / GoCardless / Billplz / DuitNow** | IMPLEMENTED (adapters) | `Finance/Gateways/PaymentGatewayAdapters.cs` | Outbound | per-gateway creds |
| **Payment webhooks** | IMPLEMENTED | `FinanceWebhookController.cs` | Inbound POST `/api/webhooks/{tenantId}/{gatewaySlug}` | HMAC + optional PQC signature; idempotent via Redis |
| **Twilio SMS** | IMPLEMENTED | `Workflow/EmailNotificationService.cs:95-111` | Outbound | Account SID + token |
| **WhatsApp Business API** | IMPLEMENTED | `Workflow/EmailNotificationService.cs:168-171` | Outbound | Graph phone-number token |
| **Microsoft Graph / Teams** | PARTIAL | `MicrosoftEcosystem/MicrosoftEcosystemServices.cs` | Outbound | Graph token |
| **Moodle LTI 1.3** | PROPOSED | `MicrosoftEcosystem/MoodleLmsService.cs` | Outbound | LTI token (design per S6 doc) |
| **Power BI Embedded** | PARTIAL | `MicrosoftEcosystem/PowerBiEmbeddingService.cs` | Outbound Azure AD token | AAD client credentials |
| **AI providers (OpenAI/Ollama)** | IMPLEMENTED | `AI/` providers | Outbound | API keys; `new HttpClient()` per ctor |
| **GeoIP** | IMPLEMENTED | `Security/LoginAuditService.cs:44-47` | Outbound `ipapi.co/{ip}/json` | none |
| **Infisical** | IMPLEMENTED | secret provider | Outbound | token; 900s cache |
| **S3 / Azure / GCP cloud** | IMPLEMENTED | `Cloud/*` | Outbound | SDK credentials |
| **SMTP** | IMPLEMENTED | `Workflow/EmailNotificationService.cs` | Outbound | SMTP creds |
| **Elasticsearch (search)** | IMPLEMENTED (optional profile) | `docker-compose.uni.yml` search profile; NEST 7.17.5 | Outbound | — |
| **Data import (external DBs)** | IMPLEMENTED | `DataImportService.cs:107` | Outbound ADO.NET | user-supplied connection |

## 2. Security posture per integration

- **HTTPS enforcement**: license heartbeat requires HTTPS (`LicenseService.cs:115-119`); finance gateways require HTTPS (`PaymentGatewayAdapters.cs:62`); onboarding license server default HTTPS.
- **Secrets**: payment gateway creds via config/env/Infisical; webhook HMAC shared secret; PQC signatures for finance docs/webhooks.
- **Timeouts**: license 10s, gateways 30s, sanctions 20s, WhatsApp/SMS 15s, GeoIP 3s, Turnstile 5s, AI 1-5 min.
- **Resilience**: Polly retry/circuit-breaker on RAAST, Power BI, Email/WhatsApp. License/gateway failures are fail-closed (lockout) or fail-slow accordingly.

## 3. Risks

- Raw `HttpClient` in AI provider constructors (socket exhaustion).
- Turnstile pass-through when token/secret absent.
- Webhook replay window not externally verified (Redis nonce + optional PQC) — verify during STRIX.
- GeoIP per-login external call adds latency to login audit path (non-blocking? verify) — `MEASUREMENT_REQUIRED`.
- Onboarding license validation is a synchronous 15s external call in the setup wizard only (not request hot path).
- Moodle/Teams/PowerBI are PROPOSED/PARTIAL — do not assume implemented (S6 is a design doc).
- Payment adapters: six gateways share the `finance-gateways` client; adapter bugs are shared.
