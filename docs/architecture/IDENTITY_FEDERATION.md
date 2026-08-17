# AegisCore IDENTITY_FEDERATION.md

Provider-agnostic institutional identity design. Generated 2026-08-12 on branch `agent/prod-hardening`.
Analysis + design ONLY. No implementation performed.

Evidence classes: `CONFIRMED_BY_CODE`, `CONFIRMED_BY_CONFIG`, `INFERRED`, `NOT_FOUND`, `MEASUREMENT_REQUIRED`, `CLIENT_INFORMATION_REQUIRED`.

---

## 1. Current identity state (audited)

| Component | Status | Evidence |
|---|---|---|
| Local auth (email/password) | IMPLEMENTED | `AuthController.cs` (login/refresh/logout), Argon2id, JWT access 15min + refresh-family 14d in Redis, `JwtTokenService.cs`, `LoginAuditService.cs` |
| JWT bearer validation | IMPLEMENTED | `AddAuthenticationServices` (`ProductionServicesExtensions.cs:153-176`) — validates RS256/ECDSA; optional external `Auth:Authority`; HS256 fallback dev-only; Production requires RSA authority or `Jwt:PublicKey` |
| SAML SP | IMPLEMENTED_NEEDS_HARDENING | `SamlService.cs` (metadata, AuthnRequest, response validation w/ XXE guard, replay cache, NotOnOrAfter, issuer lookup), `SamlController.cs` (metadata/login/acs/config), `SamlConfiguration.cs` |
| OIDC login flow | STUB | No OIDC handler registered; `SsoHitlServices.cs:36-44` only builds authorize URLs for microsoft/google/okta |
| SSO redirect discovery | STUB | `MultiTenantSsoService.DetectProviderAsync` (`SsoHitlServices.cs:60-66`) — email-domain → `UniversitySsoProfile` → redirect URL. No callback/exchange. |
| SCIM provisioning | IMPLEMENTED (thin) | `ScimController.cs` — Users/Groups CRUD, `[R=Admin,SuperAdmin]`, not tenant-scoped (uses `Guid.Empty` tenant on create) |
| Claims mapping | NOT_FOUND | no role/dept/program/studentID/employeeID mapping from IdP claims |
| JIT provisioning | NOT_FOUND | no on-first-login user creation from IdP |
| Account linking | NOT_FOUND | no external-subject ↔ AegisCore-user link table/flow (migrations add `Users.StudentId`/`ExternalId` columns but no linking service) |
| Break-glass admin | PARTIAL | SuperAdmin license override + local admin via wizard; no dedicated emergency SSO-bypass login flow |
| Multiple IdPs per tenant | PARTIAL | `UniversitySsoProfile` table supports multiple rows; no runtime router confirmed |
| Disabled-user handling | PARTIAL | SCIM sets `UserStatus.Inactive`; no IdP-driven disable propagation |

**Key negative finding:** the SSO journey dead-ends at an authorize URL. There is no callback endpoint, no token exchange, no ID-token validation, no userinfo, no claims→AegisCore mapping, and no JWT issuance for SSO users. The `UniversitySsoProfile.SsoClientSecretEncrypted` field exists but is not used by any token-exchange code path (`CONFIRMED_BY_CODE`).

**Verified SAML finding (`CONFIRMED_BY_CODE`, re-checked in this audit):**
- `SamlService.cs:155-159` creates a `SignedXml`, `LoadXml(signatureElement)` and checks `SignatureMethod != null` — but **never calls `CheckSignature(...)`/`CheckSignatureReturningKey(...)`** and never loads the IdP certificate from `SamlConfiguration.IdpCertificate` into `KeyInfo`. The assertion signature is therefore **not actually verified** — a self-signed or attacker-crafted assertion with any well-formed `<Signature>` element passes the signature gate. (XXE is mitigated; replay/expiry/issuer checks do run.)
- `SamlController.acs` (`SamlController.cs:46-69`) calls `_identityService.UpsertUserFromSamlAsync(..., Guid.Empty, ...)` and returns the user JSON — it does **not** issue AegisCore JWTs or create a session. An SSO user cannot actually authenticate into the portal through SAML today.
- This elevates SAML from "needs hardening" to **security-critical**: it must not be exposed to a real IdP until signature verification and ACS→JWT issuance are implemented.

---

## 2. Identity principle

- OIDC is a protocol. Entra/Google/Okta/ADFS are providers. AegisCore domain logic must not care which IdP issued identity.
- AegisCore local authentication is a first-class option and the **default fallback** for universities without an institutional IdP (email hosting ≠ SSO).
- Provider buttons on the login page MUST be derived from tenant configuration, never hard-coded globally.

---

## 3. Target architecture

```
Portal /login
   │  (tenant configuration → which buttons render)
   ├── Continue with Microsoft   ─┐
   ├── Continue with Google      ─┤── OIDC Provider Adapter ──┐
   ├── Continue with Okta        ─┘                           │
   ├── Continue with Institutional SSO ── SAML Provider Adapter
   └── AegisCore Account ── local auth                        │
                                                              ▼
                                          IAuthenticationGateway (provider-neutral)
                                                              │
                                       ┌──────────────────────┼─────────────────────┐
                                       ▼                      ▼                     ▼
                              ClaimsNormalizer          AccountLinker        JitProvisioner
                              (email, name, role,       (external subject ↔   (create user/student/
                               dept, program, studentId,  user)                 faculty/employee
                               employeeId, disabled)                             from IdP identity)
                                                              │
                                                              ▼
                                          AegisCore IdentityService
                                                              │
                                                              ▼
                                          JWT issuance (existing JwtTokenService) + refresh family
                                                              │
                                                              ▼
                                          TenantContextMiddleware → Casbin → ABAC → EF filter (existing)
```

### Layering

| Layer | Responsibility | Existing |
|---|---|---|
| **PROTOCOL** | OIDC / OAuth2 / SAML 2.0 mechanics, token validation | JwtBearer (validation); SAML engine (present); OIDC handler MISSING |
| **PROVIDER** | Entra, Google, Okta, ADFS, custom IdP registration | `UniversitySsoProfile` (config rows) |
| **ADAPTER** | Implements the protocol for a given provider config; returns normalized identity | authorize-URL builders only (STUB) |
| **CLAIMS MAPPING** | Map IdP claims → AegisCore identity (email, name, role, department, program, studentId, employeeId) | `SamlService` has an attribute-mapping JSON; OIDC equivalent missing |
| **DOMAIN LOGIC** | Account linking, JIT provisioning, local fallback, authorization | partially present |

---

## 4. Designed flows

### 4.1 OIDC login flow (target)
1. `/login` renders SSO buttons from tenant config (`UniversitySsoProfile` rows for the tenant's domain, or explicit list).
2. User clicks → redirect to provider authorize endpoint with `state` + `nonce` (PKCE for public clients).
3. Provider redirects back to `/api/auth/oidc/callback?tenant={tenantId}&provider={slug}`.
4. Adapter exchanges code, validates ID token (issuer, audience, nonce, exp, `hd` for Google if configured), fetches userinfo if needed.
5. `ClaimsNormalizer` maps: `email`, `given_name`/`family_name`, role claims (group names → AegisCore roles via tenant mapping), department/program/studentID/employeeID claims.
6. `AccountLinker` matches by external subject (preferred), then by verified email; on conflict → identity-conflict handling (step 4.4).
7. `JitProvisioner` creates User (+ Student/Faculty/Employee stub if claims identify one) on first login.
8. Existing `JwtTokenService` issues access+refresh tokens; `TenantContextMiddleware` populates tenant from the mapped tenant.
9. Disabled users (IdP `active=false` or group revoked) → treated as inactive; account linking refuses reactivation.

### 4.2 SAML login flow (target)
1. `/api/identity/saml/login?tenant={tenantId}` → build AuthnRequest (exists).
2. IdP POSTs assertion to ACS (`SamlController.acs` — exists).
3. Validate: **signature (must confirm/complete `SignedXml.CheckSignature`)** → replay (move replay cache from process-local `IMemoryCache` to Redis) → NotOnOrAfter (exists) → issuer (exists).
4. Map attributes → normalized identity (existing mapping JSON).
5. Same AccountLinker/JitProvisioner/JWT issuance as OIDC.

### 4.3 Local auth fallback
- `AuthController.login` remains; add **origin rate limiting** (SEC-02) on login/refresh.
- Break-glass: keep the SuperAdmin license override; add an emergency local superuser path usable when SSO is unavailable (config-gated, audited).

### 4.4 Identity conflict handling
- Match priority: external subject ID → exact verified email → matching name+department (manual review).
- Conflict = external subject unknown but email already linked to a different external subject → do NOT auto-link; require admin adjudication (an identity-conflict queue).
- Never merge two local accounts silently.

### 4.5 Multi-IdP per tenant
- `UniversitySsoProfile` supports N rows per tenant (different domains or multiple providers for one domain).
- Router: email-domain match first; if no domain match, show configured provider buttons.
- Provider selection is **tenant configuration**, not code.

### 4.6 SCIM feasibility
- A SCIM server already exists (`ScimController.cs`). It is thin and not tenant-scoped (`Guid.Empty`). 
- For pilot: **SCIM is optional**. JIT provisioning from OIDC/SAML covers identity; SCIM becomes useful for HR-system-driven user lifecycle later.
- If used, it must become tenant-scoped and password-field compliant. Feasibility: YES, effort low.

---

## 5. Tenant-specific authentication options (desired)

| Tenant | Desired buttons | Mechanism |
|---|---|---|
| Tenant A | Continue with Microsoft + AegisCore Account | OIDC (Entra) + local |
| Tenant B | Continue with Google + Continue with Institutional SSO | OIDC (Google) + OIDC/SAML (custom IdP) |
| Tenant C | Continue with Okta | OIDC (Okta) |
| Tenant D | Institutional SSO + AegisCore Account | SAML/OIDC + local |

All rendered from tenant config. No hard-coded global provider buttons.

---

## 6. Gap-to-implementation summary

| Item | Current | Required work | Owner | Blocker? |
|---|---|---|---|---|
| OIDC handler (login flow) | STUB | Add OIDC adapter: callback, code exchange, ID-token validation, PKCE/state/nonce | BACKEND | YES if SSO required |
| OIDC claims mapping | NOT_FOUND | ClaimsNormalizer for role/dept/program/studentID/employeeID | BACKEND + CLIENT | YES if SSO required |
| SAML signature verification | NOT verified (CONFIRMED_BY_CODE) | Implement `SignedXml.CheckSignature` against `IdpCertificate`; Redis replay store | BACKEND | YES if SAML required (security-critical) |
| ACS→JWT issuance | NOT_FOUND (ACS upserts user only) | bridge normalized identity → JwtTokenService + session | BACKEND | YES if SAML required |
| Account linking + JIT | NOT_FOUND | AccountLinker, JitProvisioner, conflict queue | BACKEND | YES if SSO required |
| JWT issuance for SSO | NOT_FOUND | bridge normalized identity → JwtTokenService | BACKEND | YES if SSO required |
| SSO buttons from tenant config | NOT_FOUND | login page renders tenant SSO buttons | FRONTEND + CLIENT | YES if SSO required |
| Local auth rate limit | NOT_FOUND (SEC-02) | origin rate limiting | BACKEND/SECURITY | HIGH |
| SCIM hardening | PARTIAL | tenant scoping if used | BACKEND | NO |
| Break-glass login | PARTIAL | emergency local admin flow | BACKEND/SECURITY | NO (exists) |

**Verification note (resolved in this audit):** `SamlService.cs` loads `SignedXml` and checks `SignatureMethod` but never calls `CheckSignature`, and `SamlController.acs` upserts a user without issuing JWTs. Both must be fixed before SAML is enabled against a real IdP.

---

## 7. Recommended sequencing (identity)

1. **P0**: Local-auth rate limiting + origin abuse controls (protects the fallback auth that every pilot uses).
2. **P0**: OIDC generic adapter + Entra as first provider (the most probable pilot IdP). Include claims mapping, account linking, JIT, JWT issuance.
3. **P0**: Login-page SSO buttons driven by tenant config.
4. **P1**: Implement SAML signature verification (`SignedXml.CheckSignature` vs `IdpCertificate`) + Redis replay store + ACS→JWT issuance (ADFS/institutional IdPs). **Security-critical — do not expose SAML ACS until done.**
5. **P1**: Add Google and Okta adapters (config-only once the OIDC adapter is generic).
6. **P2**: SCIM tenant-scoping; identity-conflict admin UI; disabled-user propagation; break-glass polish.
