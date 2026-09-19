# 04 — Unified control set and NIST CSF 2.0 gap assessment

**As of:** 2026-09-20, commit `1fa1d306`. This is the **one control set** every framework maps onto. Control ids are `AC-nn`. Status vocabulary is deliberately narrow:

- **Verified** — evidence exists in the repo (code + test) or was exercised in this assessment.
- **Exists, unverified** — code exists; no test, log or drill proves it does what it says.
- **Partial** — works for part of the data or part of the system; the uncovered part is named.
- **Gap** — nothing in the repo does this.
- **Not ours** — the control belongs to the university under Model A; goes in the DPA as a controller obligation.

Framework cross-reference columns: PECA (2016/2025), PDPA-draft, NDGP-2026 (expected), ISO 27001:2022 Annex A, GDPR/FERPA (as design patterns).

## GOVERN (GV)

| Id | Control | Status | Evidence / gap | ISO A. | Other |
|---|---|---|---|---|---|
| AC-01 | Named data-protection point of contact | **Gap** | Nobody is named anywhere | 5.2, 5.4 | PDPA-draft (DPO), GDPR 37 |
| AC-02 | Information-security policy set, approved, versioned | **Gap** (a 2025 architecture document exists but it describes controls that are not implemented — see AC-20) | `docs/06-security-architecture-document.md` | 5.1 | |
| AC-03 | Roles and responsibilities for security | **Gap** | Single-maintainer repository; 3 committer identities, 5 merge commits in the last 100 (`git log`) — no reviewer role exists | 5.2, 5.3 | |
| AC-04 | Legal/regulatory register, re-checked each phase | **Verified** (this phase) | `00-PROGRAM.md` legal check dated 2026-09-20 | 5.31 | PECA, PDPA, NDGP, HEC |
| AC-05 | Risk assessment method + register | **Gap** | `docs/14-risk-assessment.md` is a 2025 project-risk list, not a scored security risk register | 6.1 (27001 cl.), 5.7 | |
| AC-06 | Supplier / sub-processor register | **Partial** | List produced in `02-data-flow-map.md` §4; no security assessment of any supplier yet | 5.19–5.22 | PDPA-draft, GDPR 28 |
| AC-07 | Records of processing (ROPA) | **Gap** — inventory in `01` is the raw material | | 5.34 | GDPR 30 |
| AC-08 | Privacy notice / policy specific to actual practices | **Gap** | none in repo | 5.34 | PDPA-draft, GDPR 13 |
| AC-09 | DPA template for university clients | **Gap** | none in repo | 5.20 | GDPR 28 |

## IDENTIFY (ID)

| Id | Control | Status | Evidence / gap | ISO A. |
|---|---|---|---|---|
| AC-10 | Personal-data inventory and classification | **Verified** (this phase) | `01-data-inventory.md` | 5.9, 5.12 |
| AC-11 | Data-flow map incl. exits and third parties | **Verified** (this phase) | `02-data-flow-map.md` | 5.14 |
| AC-12 | Software/component inventory (SBOM) | **Partial** | `docs/DEPENDENCY_AUDIT.md` (2026-08-05, manual); no generated SBOM in CI; `docs/06` claims "SBOM generated per build" — false | 5.9, 8.8 |
| AC-13 | Vulnerability identification in dependencies | **Gap** | `Directory.Build.props` sets `<NuGetAudit>false</NuGetAudit>`; no `dependabot.yml`; CI (`.github/workflows/ci.yml`) has no scan step; `docs/06` claims "weekly `dotnet list package --vulnerable`" — no evidence it runs | 8.8 |
| AC-14 | Threat model | **Exists, unverified** | STRIDE table in `docs/06` §3; not linked to any test | 5.7 |

## PROTECT (PR)

| Id | Control | Status | Evidence / gap | ISO A. | Other |
|---|---|---|---|---|---|
| AC-20 | Encryption of personal data at rest (application level) | **Partial** | AES-GCM via `IPqcCryptographyService` (`Security/Pqc/PqcCryptographyService.cs`, master key `AEGISCORE_PQC_KEY_BASE64`) applied to consent IPs, FERPA-log IPs, attendance coordinates, payroll bank refs, ballots. **Not** applied to CNIC, passport, DOB, salary, counselling/mental-health text, guardian contacts, login-audit IP/GPS (`01` F-01). `docs/06` "AES-256-GCM column-level encryption for PII" is not true | 8.24 | PECA s.3–4 (unauthorised access impact), PDPA-draft security |
| AC-21 | Encryption of the database volume | **Not ours** (Model A) / **Gap** (Model B) | University's PostgreSQL host | 8.24 | |
| AC-22 | Encryption in transit | **Not ours** at the edge (compose comment: "bind behind university TLS ingress"); internal Docker network plaintext between ingress→app→sidecar | `docker-compose.client.yml:28`; HSTS/HTTPS-redirect only in Production (`Program.cs`) | 8.24 | |
| AC-23 | Key management | **Partial** | Keys are env vars / files in the config volume, generated at first start (`GeneratedSecrets.cs:25-27`), never hardcoded. Rotation exists **only** for JWT signing keys (`rotate-jwt-keys`, `docker-compose.client.yml:22-24`). No rotation for `AEGISCORE_PQC_KEY_BASE64` (which also encrypts the secret vault) or `AEGISCORE_ENCRYPTION_KEY`. `generated-secrets.env` is plaintext on the volume. `DemoExpiryMiddleware.cs:34` falls back to the literal `"demo-hmac-key-fallback"` — demo-only but should be removed | 8.24 | |
| AC-24 | Secrets storage | **Verified** for design, unverified for rotation | FILE provider = AES-256-GCM `vault.bin` with 0600 perms (`OnPremiseProvider.cs:32-131`); OIDC client secrets stored by **name** only in the DB (`IdentityProviderConfiguration.cs:33-41`, `ExternalSignInService.cs:190-196`); test `InfisicalSecretServiceTests` covers the Infisical contract | 8.24 | |
| AC-25 | Data Protection key ring protected | **Verified** by config | `PersistKeysToFileSystem` + `ProtectKeysWithCertificate` (`ProductionServicesExtensions.cs:101-107`); production refuses to start without the PFX (`docker-compose.client.yml:19-21`) | 8.24 | |
| AC-26 | Authentication strength | **Verified** (recent hardening) | Argon2id (`Security/PasswordHashingService.cs`), RS256-only in production, no demo relaxation (commit `6677e151`), brute-force lockout in Redis, session binding middleware | 5.17, 8.5 | PECA |
| AC-27 | MFA for privileged accounts | **Partial / unverified** | TOTP verify exists (`SsoHitl/SsoHitlServices.cs:79-90`) via `UserSecondFactors`; **no evidence it is enforced for Admin/SuperAdmin** on the local login path. `docs/06` claims "MFA for admin accounts" | 5.17, 8.5 | |
| AC-28 | Authorisation, default deny, server-side | **Verified** | `[Authorize]` + `RequirePermissionAttribute` + Casbin middleware; tenant isolation test gated in CI (`ci.yml:42-43`); commit `22618736` (rows belong to request tenant) | 5.15, 8.3 | |
| AC-29 | Least privilege **by data category** | **Gap** | Roles are mapped to features (`docs/ROLE_CAPABILITIES.md`), never to the categories in `01`. Phase 3 test list in `01` §3 | 5.15, 5.18 | FERPA "legitimate educational interest" pattern |
| AC-30 | Identity lifecycle (joiners/leavers) | **Partial** | SCIM deprovisioning (`ScimController.cs`) and OIDC group→role mapping on every sign-in; no periodic access review | 5.16, 5.18 | |
| AC-31 | Multi-tenant isolation | **Verified** | Global query filters + per-connection tenant `SET` (`TenantSessionInterceptor.cs`) + CI test | 8.3 | |
| AC-32 | Input validation, upload validation, rate limiting | **Verified** (code + `tests/run-all.sh` abuse harness) | FluentValidation, `UploadValidationService`, `AspNetCoreRateLimit` keyed on forwarded client address (commit `578daaff`) | 8.26, 8.28 | PECA |
| AC-33 | Secure configuration baseline | **Partial** | Compose refuses `latest` images, requires digests (`docker-compose.client.yml:38`); Watchtower auto-update behind an opt-in profile; `/metrics` is **anonymous** with a `tenant` label (`Program.cs:352-356`) — leaks tenant ids and traffic shape | 8.9 | |
| AC-34 | Secure development (review, scanning, disclosure triage) | **Gap** | No mandatory review (AC-03), no SAST/dependency scan (AC-13), no documented triage path for reported vulnerabilities | 8.25–8.29 | |
| AC-35 | Security awareness training | **Gap** | none | 6.3 | |
| AC-36 | Backups exist | **Exists, unverified** | Two mechanisms: `deploy/scripts/backup.sh` (`pg_dump` custom format, `pg_restore --list` verification, 30-day prune, optional openssl encryption + S3) and in-app `BackupService` (Quartz `AutomatedBackupJob`, SHA-256 checksum, `RestoreFromBackupAsync` shells `pg_restore`). Neither has a test in `AegisCore.Tests` (grep found none) | 8.13 | |
| AC-37 | Backups encrypted | **Partial** | `backup.sh` encrypts only when `BACKUP_ENCRYPTION_KEY` is set; `BackupService` never encrypts (`config.EncryptBackup` is recorded on the record but no encryption is called); `BackupEncryptionService` is unregistered, has no decrypt method, and if `Backup:EncryptionKey` is unset generates a **random key it never stores** (`BackupEncryptionService.cs:29-30`) — a backup made with it would be unrecoverable | 8.13 | |
| AC-38 | Retention and deletion enforced | **Partial** | Retention worker covers 3 tables (`ComplianceRetentionWorker.cs`); erasure covers 9 entities (`01` F-04); no deletion path for backups, logs, files of uncovered entities, visitors, login audit | 5.33, 8.10 | PDPA-draft, GDPR 5(1)(e), 17 |
| AC-39 | Consent captured with exact text | **Partial** | `ConsentLedgers` + `RecordConsentAsync` exist; only caller is `PrivacyComplianceController.cs:90` — the admissions form, applicant portal and wizard never call it | 5.34 | PDPA-draft, GDPR 7 |
| AC-40 | Parental consent for minors | **Gap** | no age computation anywhere | 5.34 | PDPA-draft, GDPR 8 |
| AC-41 | Data-subject access (DSAR) | **Verified** by test | `CreateDsarArchiveAsync`, `PrivacyComplianceTests` | 5.34 | GDPR 15, FERPA inspection right |
| AC-42 | Logging hygiene (no secrets/PII in logs) | **Exists, unverified** | `SensitiveDataDestructuringPolicy`; request logging not tested for PII in paths/queries | 8.15 | |

## DETECT (DE)

| Id | Control | Status | Evidence / gap | ISO A. | Other |
|---|---|---|---|---|---|
| AC-50 | Access logging of reads on sensitive records | **Partial** | `RequireFerpaAudit` on 19 endpoints (admissions, grades, transcripts, attendance) with encrypted IP (`Compliance/FerpaAuditService.cs`). Not on HR, counselling, discipline, mental health, foreign students, applicant portal, payslips (`01` F-02) | 8.15 | FERPA 99.32 pattern, GDPR accountability, PECA evidence |
| AC-51 | Audit logging of writes on sensitive records | **Gap in practice** | `IAuditService.LogAsync` called from 4 places (`PasswordController`, `GradingService` grace marks, `LicenseService`, DI); `AuditLogService` unregistered dead code (`01` F-03). Finance ledger has its own append-only hash chain + PQC seals (`AuditSealService`) — that part is strong | 8.15 | |
| AC-52 | Authentication event logging | **Verified** | `LoginAuditRecords` + `LoginAuditService.DetectAnomaliesAsync` + `/api/login-audit` | 8.15 | PECA |
| AC-53 | Log integrity / append-only | **Partial** | Finance ledger append-only guards + hash chain (migrations `FinanceLedgerAppendOnlyGuards`, `FinanceLedgerHashChain`); `AuditLogs`/`FerpaAuditLogs` are ordinary tables — `docs/06` "append-only policy; tamper detection via checksums" is true only for finance | 8.15 | |
| AC-54 | Monitoring and alerting | **Partial** | `/health/*`, Prometheus metrics, `SystemAlertService`, module health (commit `37cc872d`). No security alert rules (failed-login spikes, mass export, privilege change) | 8.16 | |
| AC-55 | Vulnerability scanning cadence | **Gap** | see AC-13 | 8.8 | |

## RESPOND (RS)

| Id | Control | Status | Evidence / gap | ISO A. | Other |
|---|---|---|---|---|---|
| AC-60 | Incident response plan | **Exists, unverified** | `docs/15-operations-maintenance-guide.md` §7 has severity levels and a flow; no named roles, no contact list, no evidence of a tabletop | 5.24–5.26 | |
| AC-61 | Breach notification procedure (regulator, client, data subjects) | **Gap** | `docs/06` says "HEC notification for data breaches" — no procedure, no timeframe, no template. Must cover: the university (controller) within a fixed window; NCCIA/PECA reporting where a cybercrime is suspected; NDGP-2026 requirements once notified; GDPR-style 72 h internal target | 5.24, 5.26 | PECA, PDPA-draft, GDPR 33/34 |
| AC-62 | Vulnerability disclosure intake and triage | **Gap** | no `SECURITY.md`, no triage SLA | 5.25 | |

## RECOVER (RC)

| Id | Control | Status | Evidence / gap | ISO A. |
|---|---|---|---|---|
| AC-70 | Restore tested | **Gap** | `docs/dcr-runbook.md` has a PITR procedure (`pg_restore` + WAL `restore_command`, `:50-70`) — never run per repo; `BackupService.ExecutePitrRestoreAsync` throws `NotSupportedException` (`BackupService.cs:228-230`). `docs/06` "RTO < 1 hour, RPO < 5 minutes" is unsubstantiated | 8.13, 8.14 |
| AC-71 | DR/continuity plan with RTO/RPO agreed per client | **Gap** | numbers asserted, not agreed or measured | 5.29, 5.30 |
| AC-72 | Post-incident review | **Gap** | none on record | 5.27 |

## Cross-reference to the hardening already landed (brief rule 4)

The compliance programme depends on the foundation being real. Checked against `git log` since 2026-07:

| Area | Landed? | Commit(s) | Compliance reliance |
|---|---|---|---|
| Real auth, RS256 only, no demo relaxation | Yes | `6677e151` | AC-26 |
| DI wiring: every controller resolves from the installed container | Yes | `56e5e1b0` | Everything — dead endpoints would be unauditable |
| Tenant ownership of created rows | Yes | `22618736` | AC-31 |
| Licensing: signed offline keys, grace period, lock page | Yes | `2272e29a`, `43ab5a49` | X-01 disclosure |
| Bootstrap: schema initialised/migrated/refused by policy; drift check by name and type | Yes | `daae5b67`, `d4dbb170` | AC-33 |
| JWT signing keys provisioned + rotation command | Yes | `70c116a7`, compose header | AC-23 (partial) |
| Data Protection ring certificate required in production | Yes | `12d43cea` | AC-25 |
| Uploads actually stored | Yes | `97bd2c29` | AC-36/38 — files now exist and must be in the deletion path |
| Rate limits keyed on real client address | Yes | `578daaff` | AC-32 |
| SSO via OIDC with vault-held secrets | Yes | `a5d18bc1` | AC-24, E-02 |
| Backups restored in a test | **No** | — | AC-36/37/70 |
| Secrets rotation beyond JWT | **No** | — | AC-23 |
| Dependency scanning | **No** (explicitly disabled) | `Directory.Build.props` | AC-13 |

Conclusion: the auth/DI/licence/bootstrap foundation is real and the programme can be built on it. Backups and vulnerability management are not yet real and are the first Phase 3 items.

## Phase 3 work order (derived from the gaps above, ranked by risk)

| Rank | Item | Controls | Nature |
|---|---|---|---|
| 1 | Backup: pick one mechanism, encrypt with a stored key, restore into a scratch database in CI and assert row counts; delete `BackupEncryptionService` | AC-36, 37, 70 | code + CI |
| 2 | Turn `NuGetAudit` back on, add `dependabot.yml` for NuGet/npm/pip/actions, fail CI on high/critical | AC-13, 34, 55 | config |
| 3 | Write-audit: register one audit path and call it from grade/mark, salary, role/permission, consent, erasure and export handlers; delete dead `AuditLogService` | AC-51 | code |
| 4 | Read-audit: extend `RequireFerpaAudit` (or a generic `RequireAccessAudit`) to HR, counselling, discipline, mental health, foreign students, applicant portal, payslip export | AC-50 | code |
| 5 | Field encryption for CNIC, passport, DOB, salary, counselling/mental-health text, guardian contacts, login-audit IP/GPS; add a key-rotation command for `AEGISCORE_PQC_KEY_BASE64` | AC-20, 23 | code + migration |
| 6 | Erasure/retention: extend to the entities in `01` F-04, F-07, F-08 including stored files; define the backup expiry that bounds "deleted" | AC-38 | code |
| 7 | Consent capture on the applicant form + age gate + guardian consent | AC-39, 40 | code |
| 8 | Enforce MFA for Admin/SuperAdmin on local login; verify TOTP-secret storage | AC-27 | code |
| 9 | Data-category least-privilege test matrix (roles × categories) as xUnit tests | AC-29 | tests |
| 10 | `/metrics` behind auth or network policy | AC-33 | config |
| 11 | Security alert rules on login-audit anomalies and mass export | AC-54 | code |
| 12 | Incident-response plan with names, breach-notification procedure, `SECURITY.md`, one tabletop | AC-60–62 | docs + exercise |

## NIST CSF 2.0 function scorecard (Phase 1 baseline, evidence-based)

Scoring: share of controls in each function that are Verified (full weight) or Partial/Exists-unverified (half weight). "Not ours" excluded.

| Function | Verified | Partial / unverified | Gap | Score |
|---|---|---|---|---|
| Govern (AC-01–09) | 1 | 1 | 7 | **17 %** |
| Identify (AC-10–14) | 2 | 2 | 1 | **60 %** |
| Protect (AC-20–42, 21 scored) | 7 | 10 | 4 | **57 %** |
| Detect (AC-50–55) | 1 | 3 | 2 | **42 %** |
| Respond (AC-60–62) | 0 | 1 | 2 | **17 %** |
| Recover (AC-70–72) | 0 | 0 | 3 | **0 %** |

These numbers are the honest starting line. They are re-scored at the end of Phase 5.
