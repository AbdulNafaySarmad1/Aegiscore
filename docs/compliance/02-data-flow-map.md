# 02 — Data flow map

**As of:** 2026-09-20. Each flow was located in code; "default on" means it runs in a stock `docker-compose.client.yml` install without extra configuration.

```
                 ┌─────────────────────── University-controlled boundary ───────────────────────┐
 Browser/Portal ─┤ ingress (HAProxy/TLS) ─► AegisCore.API ─► PostgreSQL (university-provided)   │
 Mobile (Expo) ──┤                              │      ├──► Redis                                │
 OIDC IdP ◄──────┤◄── ExternalSignInService ────┘      ├──► object storage (local volume)        │
 SCIM client ────┤                                     ├──► vault.bin (AES-GCM)                  │
 CSV/XLSX import ┤                                     ├──► aegiscore-clip sidecar (LAN)         │
                 │                                     ├──► SMTP (university relay)              │
                 │                                     ├──► Ollama (university AI host)          │
                 └─────────────────────────────────────┼───────────────────────────────────────┘
                                                       ├──► license.aegiscore.systems  (vendor)   [5 fields, no PII]
                                                       ├──► challenges.cloudflare.com  (optional) [IP + token]
                                                       ├──► api.openai.com             (optional) [prompt text]
                                                       ├──► Azure Document Intelligence(optional) [document images]
                                                       ├──► Microsoft Graph            (optional) [roster: names, emails]
                                                       ├──► Twilio / WhatsApp / FCM    (optional) [phone, message body]
                                                       ├──► payment gateways           (optional) [payer, amount, reference]
                                                       ├──► S3 backup bucket           (optional) [full database dump]
                                                       └──► Elasticsearch             (optional) [directory index]
```

## 1. Entry points (where personal data comes in)

| # | Entry | Data | Code | Notes |
|---|---|---|---|---|
| E-01 | Local sign-in (`POST /api/auth/login`) | email, password | `AegisCore.API/Controllers/AuthController.cs` | Argon2id verify; brute-force lockout in Redis; `LoginAuditRecord` written with IP/GeoIP/GPS/UA |
| E-02 | OIDC sign-in (Entra ID, Google Workspace, Okta) | ID-token claims: subject, name, email, groups, `hd` | `AegisCore.Infrastructure/Identity/ExternalSignInService.cs` | Client secret read from the vault by name (`:190-196`); issuer pinned to `ExternalTenantId`; group→role mapping applied per sign-in |
| E-03 | SCIM 2.0 provisioning (`/scim/v2/Users`) | user name, email, active flag, groups | `AegisCore.API/Controllers/ScimController.cs:13,83,110` | Bearer-token authenticated; creates/updates `Users` |
| E-04 | Setup wizard | institution identity, admin account, provider credentials, CSV imports | `AegisCore.API/Controllers/WizardController.cs`, `AegisCore.Infrastructure/Onboarding/` | Secrets written to vault; generated keys written to `generated-secrets.env` |
| E-05 | Bulk data import (`/api/data-import`, `/api/import`) | students, staff, applicants from CSV/XLSX | `AegisCore.Infrastructure/DataImport/` | Column mapping heuristics include `cnic`, `matric_no`, etc. (`WizardController.cs:827`) |
| E-06 | Applicant portal | application form, DOB, CNIC, documents | `S2AdmissionsController`, `ApplicantPortalEntities` | Anonymous-facing; Turnstile optional; **no age gate** |
| E-07 | Staff-entered records | marks, attendance, counselling notes, discipline, HR, payroll | ~50 controllers | Authenticated + Casbin |
| E-08 | Mobile app offline sync | attendance events with device GPS | `OfflineAttendanceEvents`, `AegisCore.Mobile` | Coordinates encrypted at rest once stored (`Attendance.cs:29`) |
| E-09 | Payment-gateway webhooks | payer reference, amount, gateway event id | `FinanceWebhookController`, idempotency table | Signature-verified per adapter |
| E-10 | Support-bot / voice assistant | free-text questions that may contain PII | `SupportBotQueryService`, `MapVoiceAssistantEndpoints` | Text is forwarded to the configured LLM provider (see X-04) |

## 2. Internal processing that moves data between stores

| # | Flow | Code | Notes |
|---|---|---|---|
| I-01 | Applicant document validation → CLIP sidecar | `services/aegiscore-clip/app/main.py`; compose service `aegiscore-clip` | Document image leaves the API container over the Docker network to the sidecar; sidecar loads a Hugging Face model (`main.py:39-41`) — **model download is an outbound call at first start** unless `clip_models` volume is pre-seeded |
| I-02 | Search indexing | `AegisCore.Infrastructure/Search/` | Student directory into Elasticsearch when configured (default `http://localhost:9200`, `appsettings.json:70`); honours `DirectoryOptOut` |
| I-03 | Compliance retention worker (02:00 tenant-local) | `AegisCore.Infrastructure/Compliance/ComplianceRetentionWorker.cs` | Deletes `FerpaAuditLogs`, `ConsentLedgers` older than `compliance.audit_retention_years`, completed `ErasureRequests` > 7 y. **Nothing else** |
| I-04 | Erasure processing | `PrivacyComplianceService.ProcessErasureAsync` | Scope in `01-data-inventory.md` F-04 |
| I-05 | DSAR archive | `PrivacyComplianceService.CreateDsarArchiveAsync` | Five files, written to the storage provider, 24-hour expiry |
| I-06 | Structured logging | Serilog → console; optional PostgreSQL sink | `SensitiveDataDestructuringPolicy` masks flagged properties. Request logging middleware logs path + status; **verify in Phase 3 that query strings carrying ids are not logged with PII** |
| I-07 | Finance ledger signing / audit seals | `AuditSealService` reads `AuditLogs`; PQC signatures | Integrity, not confidentiality |
| I-08 | Read replicas / replication | `AegisCore.Infrastructure/Replication/` | Copies of the full database to replica hosts the university configures |

## 3. Exit points (where personal data leaves the system)

| # | Exit | Data | Default on? | Code | Third party |
|---|---|---|---|---|---|
| X-01 | Licence heartbeat → `https://license.aegiscore.systems` | `university_id`, `university_name`, `current_user_count`, `suite`, `timestamp` — **no personal data** | **Yes** (`License__OfflineMode: 'false'`, `docker-compose.client.yml:56-59`) | `LicenseService.cs:173-176` | Nocturne Systems (vendor) |
| X-02 | Email notifications | recipient address, message body (fee reminders, results, password resets) | Yes, to the university's SMTP relay (`appsettings.json:51-55`) | `Workflow/EmailNotificationService.cs`, `SmtpEmailService` | University mail provider |
| X-03 | SMS / WhatsApp / push | phone number, message body | No — needs `Twilio`/`WhatsApp`/FCM config | `Platform/PlatformServices.cs`, `FcmPushService` | Twilio, Meta, Google |
| X-04 | LLM provider | prompt text (support bot, document AI, student agent) — may contain names, ids, free text | No — `AI_PROVIDER` unset; when `openai`, base URL `https://api.openai.com/v1/` (`OpenAiLlmProvider.cs:28`); `ollama` keeps it on-premise | `AegisCore.Infrastructure/AI/` | OpenAI **or** university-hosted Ollama |
| X-05 | OCR | scanned documents (CNIC, certificates) | No — needs Azure key | `AzureDocumentIntelligenceService.cs` | Microsoft Azure |
| X-06 | Microsoft 365 / Moodle | rosters: names, emails, course membership; grade passback | No | `MicrosoftEcosystem/` | Microsoft, university Moodle |
| X-07 | Payment gateways (Raast, Stripe, Plaid, GoCardless, Billplz, DuitNow) | payer name/reference, amount, challan id | No | `Finance/Gateways/PaymentGatewayAdapters.cs`, `Payments/Raast/` | SBP Raast, gateway operators |
| X-08 | Cloudflare Turnstile | client IP + challenge token | No — only with `CF_TURNSTILE_*` | `InfrastructureMiddleware.cs:309` | Cloudflare |
| X-09 | Backups → S3 | **entire database** (all categories in `01`) | No — needs `AWS_ACCESS_KEY_ID` | `deploy/scripts/backup.sh` | AWS or S3-compatible host |
| X-10 | PDF / CSV / XLSX exports | transcripts, fee statements, payslips, board packs, signed finance reports | Yes (user-initiated) | `api/export`, `FinanceModuleEndpoints` signed reports, `PdfSharpCore` | Whoever the user sends the file to — outside our control; logged only where `RequireFerpaAudit` applies (transcripts yes, payslips **no**) |
| X-11 | Mandatory reports (mental health) | report content, authority notified | Yes (manual) | `MandatoryReports` | The notified authority — a legal disclosure, must be in the privacy notice |
| X-12 | Cloud secret / storage providers | secrets; uploaded files | No — only when `AEGISCORE_SECRET_PROVIDER`/`AEGISCORE_STORAGE_PROVIDER` point at AWS/Azure/GCP/Infisical/R2 | `Cloud/` | Chosen cloud |
| X-13 | Container registry / Watchtower | none (pulls images) | Watchtower off by default (`profiles: [legacy-updater]`) | `docker-compose.client.yml:164` | ghcr.io |
| X-14 | Hugging Face model pull (CLIP sidecar) | none (downloads a model) | Yes, at first start unless pre-seeded | `services/aegiscore-clip/app/main.py:39` | Hugging Face |

## 4. Sub-processor list (input to the Phase 2 DPA)

Always present in the client model: **none other than the university's own SMTP relay** — the vendor sees only X-01.

Conditional (must be disclosed per install when enabled): OpenAI, Microsoft (Azure Document Intelligence, Graph), Twilio, Meta (WhatsApp), Google (FCM, Secret Manager), AWS (S3, Secrets Manager), Cloudflare (Turnstile, R2), Infisical, payment-gateway operators, Elasticsearch host, Hugging Face (model download only).

Cloud model (`docker-compose.cloud.yml`): Nocturne Systems becomes the hosting processor and every item above is a sub-processor of Nocturne. Images default to `ghcr.io/abdulnafaysarmad1/aegiscore-*:latest` — see `03-data-residency.md`.

## 5. Flows that need a decision or a fix (carried into 04)

| # | Issue |
|---|---|
| D-01 | X-04: if a university switches `AI_PROVIDER` to `openai`, student free text leaves the country. The privacy notice, DPA and sub-processor list must be conditional on this setting, and the wizard should say so on the AI page. |
| D-02 | X-09: S3 backups are the only flow that exports **everything**; `backup.sh` encrypts with `openssl enc -aes-256-cbc -pbkdf2` **only if `BACKUP_ENCRYPTION_KEY` is set**, and the in-app `BackupService` does not encrypt at all (`BackupEncryptionService` is unregistered dead code with no decrypt path). |
| D-03 | X-10: payslip and employee-record exports are not access-logged. |
| D-04 | I-06: log content vs. PII masking not yet tested. |
| D-05 | E-06: applicant portal takes CNIC + DOB from anonymous users with no age gate and no consent text capture (the consent ledger exists but the admissions form does not call it — `grep RecordConsentAsync` → only `PrivacyComplianceController`). |
