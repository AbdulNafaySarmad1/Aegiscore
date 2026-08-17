# AegisCore PROVIDER_ADAPTERS.md

Provider-adapter architecture and inventory. Generated 2026-08-12 on branch `agent/prod-hardening`.
Analysis + design ONLY. No implementation.

Evidence classes: `CONFIRMED_BY_CODE`, `CONFIRMED_BY_CONFIG`, `INFERRED`, `NOT_FOUND`, `MEASUREMENT_REQUIRED`, `CLIENT_INFORMATION_REQUIRED`.

---

## 1. The abstraction rule

Separate **PROTOCOL / PROVIDER / ADAPTER / TENANT CONFIGURATION / DOMAIN LOGIC** everywhere:

- Domain logic never names a vendor.
- Adapters implement a protocol contract for a provider.
- Tenant configuration selects which adapter + provider config applies.
- Adding a provider = add an adapter + config, NEVER a code fork.

AegisCore already demonstrates this correctly in payments (`IPaymentGatewayAdapter`). Replicate that pattern for identity, email, notifications, BI, and analytics.

---

## 2. Adapter inventory (current)

### 2.1 Payments — REFERENCE PATTERN (EXEMPLARY)
| Item | Status | Files |
|---|---|---|
| `IPaymentGatewayAdapter` | IMPLEMENTED | `AegisCore.Infrastructure/Finance/Gateways/PaymentGatewayAdapters.cs:16-20` |
| `PaymentGatewayRouter` | IMPLEMENTED | same file `:22-39` (slug → adapter; tenant + health check; `adapterBySlug` dictionary) |
| Sandbox adapter | IMPLEMENTED | `:42-50` |
| HTTP base adapter + HTTPS enforcement | IMPLEMENTED | `:53-68` |
| Adapters: RAAST, Stripe, Plaid ACH, GoCardless, Billplz FPX, DuitNow | IMPLEMENTED | `:91-150` |
| Secrets via `secretNamespace` | IMPLEMENTED | `SendRtpAsync(tenantId, secretNamespace, ...)` |

**Takeaway:** add-an-adapter-not-a-fork is already the payment norm. `CONFIRMED_BY_CODE`.

### 2.2 Identity
| Item | Status | Files |
|---|---|---|
| Local password auth | IMPLEMENTED | `AuthController.cs`, `JwtTokenService.cs` |
| SAML engine | IMPLEMENTED (signature unverified — see IDENTITY_FEDERATION.md) | `Identity/SamlService.cs`, `SamlController.cs` |
| OIDC adapter | STUB | `SsoHitl/SsoHitlServices.cs:36-44` (authorize URL builders) |
| SCIM server | IMPLEMENTED (thin, not tenant-scoped) | `ScimController.cs` |
| Claims normalizer / linker / JIT | NOT_FOUND | — |

### 2.3 Email
| Item | Status | Files |
|---|---|---|
| SMTP sender | IMPLEMENTED (monolithic) | `Workflow/EmailNotificationService.cs:39-77` |
| Graph mail provider | NOT_FOUND | — |
| `IEmailProvider` abstraction | NOT_FOUND | — |

### 2.4 Notifications
| Item | Status | Files |
|---|---|---|
| `INotificationService` (channel switch) | IMPLEMENTED | `Workflow/EmailNotificationService.cs:183-215` |
| WhatsApp Business | IMPLEMENTED (in switch) | same file `:144-181` |
| Twilio SMS | IMPLEMENTED (in switch) | same file `:79-119` |
| In-app | IMPLEMENTED | same file `:121-142` + `NotificationHub` |
| Teams notifications | NOT_FOUND | — |
| `INotificationProvider` abstraction | NOT_FOUND | — |

### 2.5 Storage
| Item | Status | Files |
|---|---|---|
| `IStorageService` | IMPLEMENTED | `AegisCore.Contracts/Abstractions/Cloud/CloudAbstractions.cs`, `AegisCore.Application/Abstractions/Cloud/CloudAbstractions.cs` |
| Local / S3 / R2 / Azure / GCP / on-prem providers | IMPLEMENTED | `Infrastructure/Cloud/*` (`Foundation/StorageProviders.cs`) |
| `IR2StorageService` (finance docs) | IMPLEMENTED | finance module |

### 2.6 AI/LLM
| Item | Status | Files |
|---|---|---|
| `LlmProviderFactory` + OpenAI/Ollama providers | IMPLEMENTED | `Infrastructure/AI/LlmProviderFactory.cs`, `OpenAiLlmProvider.cs`, `OllamaLlmProvider.cs` |
| Embeddings | IMPLEMENTED | `OpenAiEmbeddingProvider.cs`, `OllamaEmbeddingProvider.cs` |
| OCR providers | IMPLEMENTED | `AI/DocumentOcrService.cs`, `AzureDocumentIntelligenceService.cs` |
| HttpClient hygiene | NOT DONE (raw `new HttpClient()` in ctors — SEC-05) | AI providers |

### 2.7 BI
| Item | Status | Files |
|---|---|---|
| Power BI embed token service | PARTIAL | `MicrosoftEcosystem/PowerBiEmbeddingService.cs` |
| Built-in analytics engine | NOT_FOUND (deleted namespace; tests broken) | — |
| Configurable external BI adapter | NOT_FOUND | — |

### 2.8 Microsoft ecosystem
| Item | Status | Files |
|---|---|---|
| Teams provisioning | PARTIAL | `MicrosoftEcosystemServices.cs:11-63` |
| Moodle LTI | PARTIAL | `:65-111` |
| Graph client | PARTIAL | `MicrosoftEcosystemServiceExtensions.cs` |

---

## 3. Target adapter contracts (design)

### 3.1 Identity
```csharp
public interface IIdentityProviderAdapter
{
    string ProviderName { get; }                       // "oidc" | "saml" | "local"
    string BuildAuthorizeUrl(Guid tenantId, string returnUrl, out string state, out string nonce);
    Task<NormalizedIdentity> ExchangeAsync(string codeOrResponse, string state, string nonce, CancellationToken ct);
}
public sealed record NormalizedIdentity(
    string ExternalSubjectId, string Provider,
    string Email, string? FirstName, string? LastName,
    bool IsActive, IReadOnlyDictionary<string, string> Claims);
```
- Adapters: `OidcIdentityAdapter`, `SamlIdentityAdapter`, `LocalIdentityAdapter`.
- Domain logic consumes `NormalizedIdentity` only.

### 3.2 Email
```csharp
public interface IEmailProvider
{
    string ProviderName { get; }                       // "smtp" | "graph" | "mailjet"
    Task SendAsync(EmailMessage message, CancellationToken ct);
}
public interface IEmailRouter   // tenant-config selects provider
```
- `SmtpEmailProvider` (extract from `EmailNotificationService`), `GraphEmailProvider` (future).

### 3.3 Notifications
```csharp
public interface INotificationProvider
{
    string ProviderName { get; }                       // "email" | "whatsapp" | "sms" | "teams" | "push"
    Task SendAsync(NotificationMessage message, CancellationToken ct);
}
public interface INotificationRouter  // per-tenant enabled providers + priority + template resolution
```
- Domain emits intent (`SendNotificationAsync(recipient, templateKey, payload, preferredChannels)`); router resolves tenant templates → providers.

### 3.4 BI / Analytics
```csharp
public interface IAnalyticsProvider
{
    string ProviderName { get; }                       // "builtin" | "powerbi" | "looker"
    Task<AnalyticsEmbed> GetEmbedAsync(Guid tenantId, string role, string resource, CancellationToken ct);
}
public interface IAnalyticsEngine  // built-in .NET statistical engine (rebuild deleted one)
```

### 3.5 Provider selection is tenant configuration
- Each provider adapter is registered in DI; tenant config holds per-tenant provider selection + credentials namespace (not inline secrets).
- `TenantConfigService` extended to resolve `identity.providers`, `email.provider`, `notifications.providers`, `analytics.provider`, `payments.gateways` per tenant.

---

## 4. Adapter-development policy

- New provider = new adapter class implementing an existing interface + tenant config row + optional credentials entry. **No domain-logic changes.**
- Each adapter gets: timeout, retry (Polly), circuit-breaker where network-heavy (payment/BI), secret retrieval via `ISecretService` (Infisical/config), and a health check.
- Adapters must be tenant-scoped in behavior (tenantId flows through), and their config must live in the tenant manifest (see TENANT_CONFIGURATION.md).
- Audit trail: every outbound provider call logs to the existing audit service where material.

---

## 5. Provider onboarding matrix (client input required)

| Provider | Adapter needed | Client must supply | Notes |
|---|---|---|---|
| SMTP email | yes (extract existing) | host, port, TLS, username/password, from-name/address | most common |
| Graph mail | new | Azure app + scopes + consent | when university uses M365 mail |
| WhatsApp Business | refactor existing | phone-number ID, access token, approved templates | template approval is Meta-side |
| Twilio SMS | refactor existing | SID, token, from-number | |
| Teams provisioning | harden existing | Azure app + Graph scopes + consent | batch + membership sync |
| Power BI | harden existing | AAD principal + workspace/report IDs | RLS design needed |
| Entra OIDC | new | client id/secret, tenant, redirect URI, claims mapping | primary pilot IdP candidate |
| Google OIDC | new (after generic OIDC) | client id/secret, hosted-domain, redirect | |
| Okta OIDC | new (after generic OIDC) | org URL, client id/secret, group mapping | |
| ADFS SAML | fix existing | metadata, cert, attribute mapping | signature verification first |
| RAAST / local bank | existing adapter pattern | gateway creds via secrets | already adapter-based |
| Cloudflare | existing | edge config | ERP core not Cloudflare-dependent |
| Analytics/BI | rebuild built-in; PowerBI embed | — | see POLYGLOT matrix |
