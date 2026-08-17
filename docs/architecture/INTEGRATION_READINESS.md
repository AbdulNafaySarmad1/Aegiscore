# AegisCore INTEGRATION_READINESS.md

Provider-agnostic integration readiness audit. Generated 2026-08-12 on branch `agent/prod-hardening`.
Audits each external integration against the PROTOCOL / PROVIDER / ADAPTER / TENANT CONFIG / DOMAIN LOGIC separation principle.

Evidence classes: `CONFIRMED_BY_CODE`, `CONFIRMED_BY_CONFIG`, `DOCUMENTED_NOT_VERIFIED`, `INFERRED`, `NOT_FOUND`, `MEASUREMENT_REQUIRED`, `CLIENT_INFORMATION_REQUIRED`.

---

## 1. Abstraction-boundary audit (current vs target)

| Boundary | Target abstraction | Current state | Gap |
|---|---|---|---|
| IDENTITY | OIDC/SAML/local adapter layer, provider-neutral | JwtBearer + SAML service + manual SSO redirect-URL builders (`SsoHitlServices.cs`); no OIDC handler | OIDC handler missing; SSO stops at authorize URL |
| EMAIL | IEmailProvider (SMTP / Graph / send API) | `System.Net.Mail.SmtpClient` hard-coded in `EmailNotificationService.cs:48` | monolithic, sync, SMTP-only |
| OBJECT STORAGE | IStorageService (S3/R2/Azure/GCP/on-prem) | `IStorageService` abstraction EXISTS (`Cloud/`, `Foundation/StorageProviders.cs`, providers for S3/R2/Azure/GCP) | mostly OK; branding/template storage not yet integrated |
| ANALYTICS/BI | built-in analytics + pluggable BI | finance dashboards + PowerBI embed (partial); analytics engine deleted | rebuild .NET analytics; BI provider adapter |
| NOTIFICATIONS | INotificationProvider (email/WhatsApp/SMS/Teams/push) | monolithic `INotificationService` channel-switch (`EmailNotificationService.cs`) | refactor to provider abstraction + intent model |
| PAYMENTS | IPaymentGatewayAdapter | EXISTS — `PaymentGatewayAdapters.cs` interface + router + 6 adapters (RAAST/Stripe/Plaid/GoCardless/Billplz/DuitNow) + sandbox | exemplary; keep the pattern |
| INFRASTRUCTURE | cloud-neutral provisioning | compose/k8s/Caddy multi-mode; Cloudflare at edge (Turnstile/AOP/R2/tunnel) | Cloudflare used but not required at origin (AOP/Turnstile degrade gracefully); acceptable |
| AI | provider adapter (OpenAI/Ollama) | `LlmProviderFactory`, `LlmProviderFactory`/providers exist; raw HttpClient in ctors (SEC-05) | wire IHttpClientFactory; keep adapter pattern |

**Verdict:** The payment-gateway adapter pattern is the model to replicate for identity, email, notifications, BI, and analytics. AegisCore already knows how to do this well — it just hasn't applied the pattern uniformly.

---

## 2. Identity provider readiness

| Provider | Protocol | Status | Files | Config required | Client credentials required | Permissions/scopes | Backend work | Frontend work | Deploy work | Security considerations |
|---|---|---|---|---|---|---|---|---|---|---|
| OIDC (generic) | OIDC | STUB | `SsoHitlServices.cs:36-44` (authorize URLs only); JwtBearer `Auth:Authority` (`ProductionServicesExtensions.cs:155-176`) | `Auth:Authority`, client_id, redirect URIs | client_id + client_secret | openid, email, profile (+ offline_access if refresh) | OIDC handler: callback, token validation, claims→role/dept/program/studentID mapping, JWT issuance, JIT provisioning, account linking | SSO buttons from tenant config; callback route | redirect URI registration per IdP | state/nonce validation, PKCE, CSRF on callback, token replay |
| Microsoft Entra ID / M365 | OIDC (v2) + Graph | PARTIAL (redirect only) | `SsoHitlServices.cs:39`; `MicrosoftEcosystemServices.cs` | `MicrosoftGraph:AccessToken`, Azure tenant/client/secret for PowerBI | client_id + secret + (delegated scopes for Graph) | User.Read, Mail.Send, GroupMember.Read.All, Team/Tab config | complete OIDC flow + Graph token service | SSO button | app registration, admin consent, reply URLs | delegated vs app-only tokens; Graph scopes least-privilege |
| Google identity | OIDC | STUB | `SsoHitlServices.cs:40` | client_id, redirect | client_id + secret | openid, email, profile | OIDC completion | SSO button | Google Cloud OAuth consent | hosted domain verification (`hd` claim) for institutional accounts |
| Okta | OIDC | STUB | `SsoHitlServices.cs:41` | Okta org URL, client_id | client_id + secret | openid, email, profile | OIDC completion | SSO button | Okta app registration | group claims mapping |
| ADFS | SAML 2.0 | PARTIAL | `SamlService.cs`, `SamlController.cs` | IdP metadata (EntityId, SSO URL, cert), SP entity/ACS | none (cert-based) | n/a | complete signature verification (INFERRED incomplete), ACS→JWT | SSO button | metadata exchange | **SAML signature verification must be confirmed/implemented**; replay store → Redis; cert rotation |
| Other institutional IdP | OIDC or SAML | STUB | same as above | per-IdP | varies | varies | generic adapter | SSO button | — | — |
| Local AegisCore auth | password | IMPLEMENTED | `AuthController.cs`, Argon2, refresh families | none | none | n/a | rate limit (SEC-02) | none | none | origin rate limiting; break-glass account (SuperAdmin override exists for license; add emergency local admin) |
| GitHub (optional) | OIDC | NOT_FOUND | — | — | — | — | — | — | — | NOT recommended for student/faculty institutional identity |

**Email hosting ≠ SSO:** A university using PTCL/local mail/SMTP/IMAP has email hosting, not identity federation. Local auth must remain the default when no OIDC/SAML IdP exists.

---

## 3. Microsoft requirements detail (first pilot may request)

### 3.1 Power BI Embedded
- CURRENT STATUS: PARTIAL — `PowerBiEmbeddingService.GetEmbedConfigAsync` (`MicrosoftEcosystemServices.cs:113-200`) returns embed config + token. Static `PowerBi:EmbedToken` fallback OR Azure AD client-credentials token acquisition (Polly circuit-breaker+retry+timeout). `PowerBiReports` entity stores report/workspace/embedUrl per role. Endpoint `GET /api/microsoft-ecosystem/powerbi/embed?userId&role` (controller checks privilege/subject).
- SOURCE EVIDENCE: `MicrosoftEcosystemServices.cs:113-200`, `MicrosoftEcosystemController.cs` (PowerBiEmbed).
- READY/PARTIAL/STUB/NOT_FOUND: **PARTIAL** (backend token service exists and works behind config; frontend embed UI NOT_FOUND; RLS not implemented).
- CONFIGURATION REQUIRED: `PowerBi:TokenMinutes`, `PowerBi:EmbedToken` (or `Azure:TenantId/ClientId/ClientSecret`), `PowerBiReports` rows per role.
- CLIENT CREDENTIALS REQUIRED: Azure AD service principal (or static embed token for demo).
- PERMISSIONS/SCOPES REQUIRED: `https://analysis.windows.net/powerbi/api/.default` (app-only) — currently no user-scoped/RLS delegation.
- BACKEND WORK: RLS (per-user row-level security token via user identity), IHttpClientFactory for the AAD token call (`new HttpClient()` at `:169`).
- FRONTEND WORK: Power BI embed page (reportId/embedUrl/token → `powerbi-client`), role-aware.
- DEPLOYMENT WORK: Azure AD app registration, consent, firewall allow-list, tenant `PowerBiReports` seeding.
- SECURITY CONSIDERATIONS: embed tokens are capability tokens — must be short-lived, tied to user identity, never cached client-side long-term; RLS must be enforced for cross-user data isolation.
- STAY IN .NET: **YES** — this is an HTTP/token integration; no Go/Rust justification.

### 3.2 Microsoft Teams auto-provisioning
- CURRENT STATUS: PARTIAL — `TeamsProvisioningService.ProvisionClassAsync` (`MicrosoftEcosystemServices.cs:11-63`) creates a single education class per timetable entry via Graph; writes `TeamsSyncLog`; POST endpoint `/api/microsoft-ecosystem/teams/timetable/{id}/provision`. Uses a **single static `MicrosoftGraph:AccessToken`** from config.
- READY/PARTIAL/STUB/NOT_FOUND: **PARTIAL** (single-class creation works; no batch, no membership sync, no token lifecycle).
- CONFIGURATION REQUIRED: `MicrosoftGraph:AccessToken` (short-term; must move to client-credentials acquisition), `MicrosoftGraph:BaseUrl`.
- CLIENT CREDENTIALS REQUIRED: Azure AD app with Graph `Group.ReadWrite.All`/`Education` scopes (or Team.Create), admin consent.
- PERMISSIONS/SCOPES REQUIRED: Graph `Education` or `Teamwork` scopes; `User.Read.All` for membership.
- BACKEND WORK: Graph token service (client-credentials, cached, scoped), batch provisioning for all classes, membership sync (roster ↔ Teams members), channel/tab creation, owner assignment.
- FRONTEND WORK: provisioning UI/status page (sync log view).
- DEPLOYMENT WORK: Azure AD app registration, consent, admin tenant config.
- SECURITY CONSIDERATIONS: static token in config is a secret-management problem (move to secrets provider); delegated vs app-only tokens must be chosen deliberately; Teams teams must be tenant-scoped.
- STAY IN .NET: **YES** — Graph SDK/HTTP in .NET is appropriate.

### 3.3 Microsoft Graph (general)
- CURRENT STATUS: PARTIAL — `graph` HttpClient registered (`MicrosoftEcosystemServiceExtensions.cs`); used by Teams/Moodle; no general Graph service (mail/calendar/files).
- READY/PARTIAL/STUB/NOT_FOUND: **PARTIAL**.
- CONFIGURATION REQUIRED: `MicrosoftGraph:BaseUrl`, token source.
- CLIENT CREDENTIALS REQUIRED: Azure AD app + scopes.
- PERMISSIONS/SCOPES REQUIRED: varies by use (Mail.Send for email provider, etc.).
- BACKEND WORK: token service + scoped Graph client (mail sending as Graph mail provider, calendar).
- FRONTEND WORK: none directly.
- DEPLOYMENT WORK: app registration + consent.
- SECURITY CONSIDERATIONS: least-privilege scopes; separate app registrations per integration tier (mail vs teams vs BI) is recommended.
- STAY IN .NET: **YES**.

### 3.4 Microsoft Entra ID / M365 SSO
- CURRENT STATUS: PARTIAL/STUB — only authorize-URL generation; no code-exchange, no callback, no claims mapping, no JWT issuance. See IDENTITY_FEDERATION.md for the full plan.
- STAY IN .NET: **YES** — OIDC is HTTP + token validation, natively in ASP.NET.

**Summary for §8 of the brief:** All three Microsoft items remain in .NET. No Go/Rust introduction is warranted by any measured or architectural reason.

---

## 4. Notifications / communications readiness

### 4.1 Current state
- `INotificationService` (`EmailNotificationService.cs`) is a **single implementation** with a channel `switch` (`:183-215`): Email (SMTP), SMS (Twilio), InApp (DB), WhatsApp (Graph API). `CommunicationsController` adds notices/grievances + WhatsApp webhook. `PlatformControllers.cs` `api/notifications` (send/templates/deliveries). SignalR `NotificationHub` + in-app bell (non-functional).
- Status: IMPLEMENTED but monolithic. `CONFIRMED_BY_CODE`.

### 4.2 Target conceptual boundary
```
Domain logic emits notification intent (channel-agnostic)
        │
        ▼
NotificationService (router) ── per tenant: enabled providers + priority
        │
        ├─ EmailProvider (SMTP | Graph mail | vendor API)
        ├─ WhatsAppProvider (Business API | vendor)
        ├─ SmsProvider (Twilio | vendor)
        ├─ TeamsProvider (Graph)
        └─ PushProvider (future)
```
- Tenants configure which providers are enabled + per-channel templates.
- Domain code calls `INotificationService.SendAsync(recipient, template, payload, channels)` — no provider API logic in domain.
- Templates per tenant, versioned, with placeholder substitution; WhatsApp requires approved templates.

### 4.3 WhatsApp fit
- WhatsApp belongs as one `INotificationProvider` adapter (already has the API call), behind the router. It should NOT remain buried in the channel switch.
- Missing today: template lifecycle (WhatsApp requires Meta-approved templates — `whatsapp-templates.json` exists but no approval workflow), provider selection per tenant, delivery receipts/webhook handling (webhook endpoint exists), retry/queue.

---

## 5. Storage readiness
- `IStorageService` + providers (Local/S3/R2/Azure/GCP) exist and work (`Foundation/StorageProviders.cs`, `Cloud/*`). Finance signed reports go through `IR2StorageService`. Transcripts write to local disk `official-transcripts/{tenantId}/`.
- Gaps: per-tenant bucket/prefix policy; branding/template storage; consistent use of `IStorageService` everywhere (transcript disk path is local-only).
- Classification: IMPLEMENTED with hardening needs.

## 6. Payments readiness
- `IPaymentGatewayAdapter` + `PaymentGatewayRouter` + Sandbox adapter + 6 live adapters + webhook HMAC/PQC + Redis idempotency: **the reference pattern**. `CONFIRMED_BY_CODE`.
- Gaps: per-gateway secret storage flows through `secretNamespace` (good); onboarding docs; bank-provider addition process (should be add-an-adapter, not fork).

## 7. Infrastructure readiness
- compose (local/cloud/hybrid/uni/local-demo), k8s (deployment/HPA/PDB/network-policy/ingress/statefulsets), Caddy, pgbouncer, prometheus, runbooks. `CONFIRMED_BY_CONFIG`.
- Cloudflare is present (Turnstile, AOP, R2, tunnel) but: Turnstile pass-through when no token/secret (SEC-10); AOP is a thumbprint check; R2 is an S3-compatible provider. **The ERP core is not Cloudflare-dependent** — R2 is behind `IStorageService`, Turnstile degrades, AOP degrades to 403-only-with-cert. Good.
- Gap: no tenant-provisioning automation (see DEPLOYMENT_FACTORY.md).

## 8. Integration priority for the 120-day pilot
1. **OIDC completion + local-auth fallback** (identity is the gating item).
2. **Email provider abstraction + background delivery** (every pilot university needs reliable email).
3. **Power BI embed frontend** (if pilot leadership wants BI dashboards).
4. **Teams auto-provisioning batch + Graph token service** (if pilot wants Teams).
5. **Notification provider refactor** (enables WhatsApp + Teams + future providers cleanly).
6. Payment onboarding (per-university gateway).
