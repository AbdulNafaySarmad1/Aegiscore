# AegisCore TENANT_CONFIGURATION.md

Tenant manifest design for zero-fork deployment. Generated 2026-08-12 on branch `agent/prod-hardening`.
Analysis + design ONLY. No implementation.

Evidence classes: `CONFIRMED_BY_CODE`, `CONFIRMED_BY_CONFIG`, `INFERRED`, `NOT_FOUND`, `MEASUREMENT_REQUIRED`, `CLIENT_INFORMATION_REQUIRED`.

---

## 1. Current state (audited)

| Aspect | Current | Evidence |
|---|---|---|
| Tenant identity | Single `TENANT_ID` env var per deployment; wizard writes `.env.generated` | `Program.cs:332`, `OnboardingService.GenerateEnvFile:216` |
| Tenant scoping | `ITenantScoped` (172 classes, global EF filter) + `app.tenant_id` GUC; **~145 entities with TenantId but no filter; ~40 core SIS entities with no TenantId** | `DATA_MODEL.md` §2 |
| Config | `ConfigurationCenterService` key/value (versioned, cached 5min) + `TenantConfigService` getters with hard-coded fallbacks | `ConfigCenter/ConfigurationCenterService.cs:89`, `Config/TenantConfigService.cs` |
| Config tenant-awareness | **`TenantConfigService` resolves without a tenant** (`Resolve(...)` calls pass no tenantId) — config is effectively instance-global, not tenant-scoped | `TenantConfigService.cs:49-71` |
| Feature flags | `FeatureFlagService` with tenant override | `api/feature-flags` (`PlatformControllers.cs:20-29`) |
| Branding | single client-logo endpoint; no tenant bundle | `BrandingController.cs` |
| Secrets | env/config + Infisical secret provider (900s cache) + `AEGISCORE_ENCRYPTION_KEY` | `.env.example`, `SERVICE_DEPENDENCIES.md` §3 |
| SSO config | `UniversitySsoProfile` rows (provider, tenant, client, encrypted secret) | `SsoHitlEntities.cs` |
| Payments config | `PaymentGatewayConfigurations` per tenant + `secretNamespace` | `PaymentGatewayAdapters.cs:32-34` |
| Academic structure | Departments/Programs/Batches entities (used for scoping) | `DATA_MODEL.md` §3 |
| Document templates | none (fixed in-code layouts) | `StudentDocumentsController.cs` |

**Verdict:** AegisCore is today **one-instance-per-tenant** (env-var driven). This is a valid pilot topology (isolated instances) but is NOT the canonical release + tenant-config model. The tenant manifest is the core productization deliverable.

---

## 2. Design principles

1. **One canonical release, many tenants.** The same immutable images serve every tenant.
2. **Configuration ≠ secrets.** Config is deployable, reviewable, versioned. Secrets live in a secrets provider (Infisical/KeyVault/SM), referenced by namespace.
3. **Tenant manifest is declarative and complete** — a university onboarding should be a manifest + assets + secrets, not code.
4. **Every runtime knob that differs per university becomes a manifest field** (academic, identity, integrations, branding, documents, features, locale).
5. **Backward compatible:** env vars remain as deployment-level overrides for a single-tenant pilot; manifest is additive.

---

## 3. Conceptual tenant manifest schema

```yaml
tenant:
  id: 7f3c…          # UUID (matches TENANT_ID semantics)
  slug: iobm         # short machine name
  status: live       # draft | active | suspended
  name: Institute of Business Management
  short_name: IoBM
  primary_domain: erp.iobm.edu.pk
  subdomains: [erp., admissions., portal.]
  locale: en-PK
  timezone: Asia/Karachi
  currency: PKR
  deployment_profile: onprem      # onprem | aws | azure | gcp | private

branding:
  logos: [object-key-primary, object-key-favicon]
  colors: { primary: "#…", secondary: "#…", accent: "#…" }
  institution_text: { full_name, short_name, tagline }
  portal_theme: default

academic_structure:
  departments: [{ code: "CS", name: "Computer Science", chair_email: … }]
  programs: [{ code: "BSCS", name: "BS CS", department: "CS", degree_level: bachelor }]
  batches: [{ code: "2026", program: "BSCS", start_year: 2026 }]
  grading_policy: { scale_ref: "AegisCoreDefault", min_cgpa: 2.0, probation_cgpa: 2.0, credit_hour_limit: 21 }
  fee_defaults: { late_reg: 500, transcript: 500, … }
  academic_calendar: { semesters: [{ code: "F2026", start, end }] }

modules: { admissions: true, registration: true, attendance: true, exams: true,
           grading: true, obe: true, finance: true, hr: true, welfare: true,
           cafeteria: true, analytics: true, accreditation: true }

feature_flags: { ai_assistant: true, hybrid_pqc_transport: false, … }   # → FeatureFlagService

identity:
  local_auth_enabled: true
  providers:
    - slug: microsoft         # adapter: oidc
      domain: iobm.edu.pk
      client_id_ref: secrets:microsoft.client_id
      client_secret_ref: secrets:microsoft.client_secret
      scopes: [openid, email, profile]
      claims_mapping: { role: "http://schemas…/role", department: "dept",
                        program: "program", student_id: "studentId",
                        employee_id: "employeeId", disabled: "active" }
      role_mapping: { "Faculty-All": Faculty, "Student-All": Student, … }
    - slug: local             # always available as fallback
  break_glass_admin: { email_ref: secrets:breakglass.email }

email:
  provider: smtp               # smtp | graph
  from_address: noreply@iobm.edu.pk
  from_name: IoBM
  smtp_ref: secrets:smtp      # host/port/user/pass namespace

notifications:
  providers: [email, whatsapp, sms]
  templates_ref: templates/en-PK/      # per-tenant template bundle

storage:
  provider: s3                 # s3 | r2 | azure | gcp | onprem
  bucket_prefix: iobm
  ref: secrets:storage

analytics:
  provider: builtin            # builtin | powerbi
  powerbi: { workspace_id: …, report_ids: { … } }

payments:
  gateways:
    - slug: raast
      enabled: true
      secret_namespace: iobm-payments

documents:
  templates: { transcript: templates/transcript-iobm.html,
               grade_sheet: …, fee_challan: …, admit_card: …,
               obe_report: …, accreditation_report: … }
  template_version: 1

integrations:
  teams: { enabled: true, graph_ref: secrets:graph }
  moodle: { base_url: …, token_ref: secrets:moodle }
  powerbi: { enabled: true, ref: secrets:azure }

security:
  upload_policy: { max_mb: 50, vault_content_validation: true }
  rate_limit: { per_minute: 60, burst: 10 }
  session: { access_minutes: 15, refresh_days: 14 }
```

**Secrets separation:** every `*_ref` points to a secrets provider namespace (Infisical/KeyVault/Azure SM/Google SM — `ISecretService` already exists), NOT inline values.

---

## 4. Mapping current knobs → manifest fields

| Current knob (env/config) | Manifest field | Notes |
|---|---|---|
| `TENANT_ID` | `tenant.id` | identity anchor |
| `Attendance__CampusLatitude/Longitude/Radius` | `academic_structure` or new `campus.geofence` | confirm location in schema |
| `Email:*` / `SMTP_*` | `email.*` + `secrets:smtp` | |
| `WhatsApp:*`, `Twilio:*` | `notifications.providers` + secrets | |
| `Azure:*` / `MicrosoftGraph:*` / `PowerBi:*` | `integrations.*` + `analytics.powerbi` | |
| `AI_*` / `ai.*` | `ai:` section (add) | OpenAI/Ollama base/model/keys |
| `PaymentGateways:*` | `payments.gateways` | keep existing per-tenant `PaymentGatewayConfigurations` |
| `Storage:*` / `AEGISCORE_STORAGE_*` | `storage.*` | |
| `Jwt:*` / `AEGISCORE_JWT_SECRET` | `security.session` + secrets | |
| `AEGISCORE_ENCRYPTION_KEY` / PQC keys | secrets | never in manifest |
| `Locale/Currency/Timezone` (`DefaultTenantConfigTemplate`) | `tenant.locale/currency/timezone` | replace defaults |
| config-center dotted keys (`identity.university_name`, `email.smtp_host`, …) | manifest sections | migrate from key/value to manifest (or wrap: manifest → config-center on deploy) |

---

## 5. Tenant resolution model (target)

- **Single-instance multi-tenant (logical):** tenant resolved from JWT `tenant_id` claim (already the model). Manifest loaded per tenant into the config-center/config service at request time (cached).
- **Isolated-instance (physical):** one deployment = one tenant, manifest injected at deploy time. This is the immediate pilot model and needs NO runtime tenant resolution change — the manifest just replaces env-var sprawl.
- Both models use the SAME manifest; `deployment_profile` chooses topology.

### Runtime implementation sketch (design only)
- `TenantManifestService` loads a tenant manifest (from DB table, object storage, or deployed file) into a typed, versioned model.
- `TenantConfigService` refactored to resolve against the manifest with per-tenant scope (today it ignores tenantId — `INFERRED` from signature, `CONFIRMED_BY_CODE` that callers pass none).
- Config-center remains the runtime-config override layer (operator edits), manifest is the source of truth at deploy.
- Feature flags already support tenant override — wire to `modules`/`feature_flags`.

---

## 6. What must stay as secrets (never in manifest)

- SMTP username/password, WhatsApp token, Twilio SID/token
- Azure/Google/Okta client secrets, Graph tokens
- DB/Redis passwords, encryption keys, PQC keys, data-protection cert password
- Payment gateway secrets
- Break-glass admin credentials
- JWT signing keys

`ISecretService` (Infisical 900s cache) + `AEGISCORE_ENCRYPTION_KEY` already exist — extend with per-tenant namespaces (`{tenantSlug}/{service}`).

---

## 7. Migration path

1. **P0:** Define the manifest schema (JSON/YAML) + validation; add per-tenant config resolution to `TenantConfigService`; add tenant-scoped branding bundle endpoints.
2. **P1:** Migrate the most deployment-critical knobs (identity providers, email, branding, locale, attendance geofence, document template selection) to manifest-backed config while keeping env-var fallback.
3. **P2:** Full manifest → secrets wiring; deployment factory consumes the manifest.
4. **P3:** Manifest UI (tenant admin), versioning, audit of manifest changes.

---

## 8. Zero-fork checklist (what the manifest enables)

- 10 universities → 10 manifests + asset bundles + secrets → 10 isolated deployments of the SAME images. ✓ achievable after P0/P1.
- No `AegisCore-University-X` forks. ✓ by construction.
- Tenant branding without code: manifest `branding` + portal consumes it. Requires frontend branding-work (P0/P1).
- Tenant documents without code: manifest `documents.templates` + template engine (CLIENT_DOCUMENT_CUSTOMIZATION.md).
- Feature/module configuration per university: manifest `modules` + feature flags.
- Identity per university: manifest `identity.providers` + OIDC/SAML adapters (IDENTITY_FEDERATION.md).
