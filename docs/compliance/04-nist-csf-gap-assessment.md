# 04 — Unified control set and NIST CSF 2.0 gap assessment

**As of:** 2026-09-22 (Phase 3 complete). Baseline assessment was 2026-09-20 at commit `1fa1d306`; rows marked with a date were re-assessed against the work that landed since. This is the **one control set** every framework maps onto. Control ids are `AC-nn`. Status vocabulary is deliberately narrow:

- **Verified** — evidence exists in the repo (code + test) or was exercised in this assessment.
- **Exists, unverified** — code exists; no test, log or drill proves it does what it says.
- **Partial** — works for part of the data or part of the system; the uncovered part is named.
- **Gap** — nothing in the repo does this.
- **Not ours** — the control belongs to the university under Model A; goes in the DPA as a controller obligation.

Framework cross-reference columns: PECA (2016/2025), PDPA-draft, NDGP-2026 (expected), ISO 27001:2022 Annex A, GDPR/FERPA (as design patterns).

## GOVERN (GV)

| Id | Control | Status | Evidence / gap | ISO A. | Other |
|---|---|---|---|---|---|
| AC-01 | Named data-protection point of contact | **Verified** | `05-governance.md` §1 (confirmation of the named holder pending) | 5.2, 5.4 | PDPA-draft (DPO), GDPR 37 |
| AC-02 | Information-security policy set, approved, versioned | **Gap** (a 2025 architecture document exists but it describes controls that are not implemented — see AC-20) | `docs/06-security-architecture-document.md` | 5.1 | |
| AC-03 | Roles and responsibilities for security | **Gap** | Single-maintainer repository; 3 committer identities, 5 merge commits in the last 100 (`git log`) — no reviewer role exists | 5.2, 5.3 | |
| AC-04 | Legal/regulatory register, re-checked each phase | **Verified** (this phase) | `00-PROGRAM.md` legal check dated 2026-09-20 | 5.31 | PECA, PDPA, NDGP, HEC |
| AC-05 | Risk assessment method + register | **Gap** | `docs/14-risk-assessment.md` is a 2025 project-risk list, not a scored security risk register | 6.1 (27001 cl.), 5.7 | |
| AC-06 | Supplier / sub-processor register | **Partial** | List produced in `02-data-flow-map.md` §4; no security assessment of any supplier yet | 5.19–5.22 | PDPA-draft, GDPR 28 |
| AC-07 | Records of processing (ROPA) | **Verified** | `08-ropa.md` — 23 University activities, 6 Nocturne activities | 5.34 | GDPR 30 |
| AC-08 | Privacy notice / policy specific to actual practices | **Verified** | `06-privacy-notice.md` | 5.34 | PDPA-draft, GDPR 13 |
| AC-09 | DPA template for university clients | **Verified** (drafted; lawyer review pending) | `07-dpa-template.md` | 5.20 | GDPR 28 |

## IDENTIFY (ID)

| Id | Control | Status | Evidence / gap | ISO A. |
|---|---|---|---|---|
| AC-10 | Personal-data inventory and classification | **Verified** (this phase) | `01-data-inventory.md` | 5.9, 5.12 |
| AC-11 | Data-flow map incl. exits and third parties | **Verified** (this phase) | `02-data-flow-map.md` | 5.14 |
| AC-12 | Software/component inventory (SBOM) | **Partial** | `docs/DEPENDENCY_AUDIT.md` (2026-08-05, manual); no generated SBOM in CI; `docs/06` claims "SBOM generated per build" — false | 5.9, 8.8 |
| AC-13 | Vulnerability identification in dependencies | **Verified** (2026-09-20) | `NuGetAudit` on at level low for all packages; CI fails on a vulnerable NuGet package, on `npm audit --omit=dev --audit-level=high`, and on Trivy CRITICAL/HIGH in both images; `.github/dependabot.yml` covers NuGet, npm ×2, pip, Docker ×2, Actions. Findings and dispositions: `12-vulnerability-register.md` | 8.8 |
| AC-14 | Threat model | **Exists, unverified** | STRIDE table in `docs/06` §3; not linked to any test | 5.7 |

## PROTECT (PR)

| Id | Control | Status | Evidence / gap | ISO A. | Other |
|---|---|---|---|---|---|
| AC-20 | Encryption of personal data at rest (application level) | **Verified** (2026-09-20) | AES-256-GCM under the installation master key. Was: consent IPs, FERPA-log IPs, attendance coordinates, payroll bank refs, ballots. Added: CNIC (admissions, visiting faculty, visitors, gate logs, vendors), passport and visa numbers, applicant/guardian phone and email, counselling notes, disciplinary narrative, mental-health text, mandatory-report content, sign-in IP, city, user agent and GPS (`Security/FieldEncryption/FieldEncryptionModel.cs`). Identifier columns use a deterministic nonce so equality lookups still work; `FieldEncryptionTests` proves ciphertext at rest, working lookups, and that pre-encryption rows still read. `dotnet AegisCore.API.dll encrypt-fields` rewrites legacy rows. **Remaining**: date of birth and salary are still plaintext (both are queried by range); `docs/06`'s blanket claim is still wrong and that document is superseded | 8.24 | PECA s.3–4, PDPA-draft security |
| AC-21 | Encryption of the database volume | **Not ours** (Model A) / **Gap** (Model B) | University's PostgreSQL host | 8.24 | |
| AC-22 | Encryption in transit | **Not ours** at the edge (compose comment: "bind behind university TLS ingress"); internal Docker network plaintext between ingress→app→sidecar | `docker-compose.client.yml:28`; HSTS/HTTPS-redirect only in Production (`Program.cs`) | 8.24 | |
| AC-23 | Key management | **Partial** | Keys are env vars / files in the config volume, generated at first start (`GeneratedSecrets.cs:25-27`), never hardcoded. Rotation exists **only** for JWT signing keys (`rotate-jwt-keys`, `docker-compose.client.yml:22-24`). No rotation for `AEGISCORE_PQC_KEY_BASE64` (which also encrypts the secret vault) or `AEGISCORE_ENCRYPTION_KEY`. `generated-secrets.env` is plaintext on the volume. `DemoExpiryMiddleware.cs:34` falls back to the literal `"demo-hmac-key-fallback"` — demo-only but should be removed | 8.24 | |
| AC-24 | Secrets storage | **Verified** for design, unverified for rotation | FILE provider = AES-256-GCM `vault.bin` with 0600 perms (`OnPremiseProvider.cs:32-131`); OIDC client secrets stored by **name** only in the DB (`IdentityProviderConfiguration.cs:33-41`, `ExternalSignInService.cs:190-196`); test `InfisicalSecretServiceTests` covers the Infisical contract | 8.24 | |
| AC-25 | Data Protection key ring protected | **Verified** by config | `PersistKeysToFileSystem` + `ProtectKeysWithCertificate` (`ProductionServicesExtensions.cs:101-107`); production refuses to start without the PFX (`docker-compose.client.yml:19-21`) | 8.24 | |
| AC-26 | Authentication strength | **Verified** (recent hardening) | Argon2id (`Security/PasswordHashingService.cs`), RS256-only in production, no demo relaxation (commit `6677e151`), brute-force lockout in Redis, session binding middleware | 5.17, 8.5 | PECA |
| AC-27 | MFA for privileged accounts | **Verified** (2026-09-20) | TOTP enrol/confirm/verify with single-use backup codes (`Security/Mfa/MfaService.cs`); secret encrypted under the master key, codes hashed. A password sign-in for an enrolled account returns a 5-minute single-use challenge, never tokens. `MfaEnforcementMiddleware` refuses every request outside sign-in/session/enrolment from a role in `Security:MfaRequiredRoles` (Admin, SuperAdmin) whose session is password-only; SSO sessions count as verified. `MfaTests` | 5.17, 8.5 | |
| AC-28 | Authorisation, default deny, server-side | **Verified** | `[Authorize]` + `RequirePermissionAttribute` + Casbin middleware; tenant isolation test gated in CI (`ci.yml:42-43`); commit `22618736` (rows belong to request tenant) | 5.15, 8.3 | |
| AC-29 | Least privilege **by data category** | **Verified** (2026-09-22) | Staff, Payroll, Welfare and Graduation are their own permission resources instead of the catch-all `System` (`Identity/PermissionCatalog.cs`). `PermissionMatrixTests` walks eight roles against nine data categories from `01` and asserts both directions, plus a reflection test that no HR or welfare endpoint is gated on `System` and that every gated resource is in the catalogue. Administrators of upgraded installations are granted the new resources at startup | 5.15, 5.18 | FERPA "legitimate educational interest" pattern |
| AC-30 | Identity lifecycle (joiners/leavers) | **Partial** | SCIM deprovisioning (`ScimController.cs`) and OIDC group→role mapping on every sign-in; no periodic access review | 5.16, 5.18 | |
| AC-31 | Multi-tenant isolation | **Verified** | Global query filters + per-connection tenant `SET` (`TenantSessionInterceptor.cs`) + CI test | 8.3 | |
| AC-32 | Input validation, upload validation, rate limiting | **Verified** (code + `tests/run-all.sh` abuse harness) | FluentValidation, `UploadValidationService`, `AspNetCoreRateLimit` keyed on forwarded client address (commit `578daaff`) | 8.26, 8.28 | PECA |
| AC-33 | Secure configuration baseline | **Partial** (improved 2026-09-22) | Compose refuses `latest` images and requires digests; Watchtower behind an opt-in profile. `/metrics` now answers only a scraper with `Metrics:ScrapeToken`, a signed-in user, or a private-network address, and the tenant label is opt-in (`MetricsAccessMiddleware`, `MetricsAccessTests`). **Remaining**: no CIS-style baseline document for the host itself | 8.9 | |
| AC-34 | Secure development (review, scanning, disclosure triage) | **Gap** | No mandatory review (AC-03), no SAST/dependency scan (AC-13), no documented triage path for reported vulnerabilities | 8.25–8.29 | |
| AC-35 | Security awareness training | **Gap** | none | 6.3 | |
| AC-36 | Backups exist | **Verified** (2026-09-20) | One mechanism: `deploy/scripts/backup.sh` (`pg_dump` custom format, verified before encryption, SHA-256 sidecar, 30-day prune, optional S3) with `restore.sh`. The in-app `BackupService` shares the format | 8.13 | |
| AC-37 | Backups encrypted | **Verified** (2026-09-20) | Encryption is unconditional: both the script and `BackupService` refuse to run without `BACKUP_ENCRYPTION_KEY` rather than write a plaintext dump, and the key is resolved before `pg_dump` starts. Format is OpenSSL `aes-256-cbc/pbkdf2` so either side restores the other's archive (`BackupCipherTests` decrypts a file produced by real `openssl`). The unregistered `BackupEncryptionService`, which generated a key it never stored, is deleted | 8.13 | |
| AC-38 | Retention and deletion enforced | **Verified** (2026-09-20) for the categories in `09` | Erasure now reaches counselling notes, disciplinary narrative, student and applicant documents (files deleted from storage), applicant-portal identity, foreign-student travel documents and sign-in records. Retention covers sign-ins, the write audit, visitor logs and closed applicants, with per-tenant periods and `compliance.legal_holds`. A subject restored from a backup older than their erasure is detected and erased again. `ErasureScopeTests` | 5.33, 8.10 | PDPA-draft, GDPR 5(1)(e), 17 |
| AC-39 | Consent captured with exact text | **Verified** (2026-09-20) for admissions | The applicant portal shows the tenant-configurable text unticked and copies it verbatim into `ConsentLedgers` on submission, so a later edit cannot change what was agreed (`ApplicantPortalTests`). **Remaining**: the directory, geolocation, welfare and AI-assistant consent categories designed in `10` §3.2 are not yet wired to their screens | 5.34 | PDPA-draft, GDPR 7 |
| AC-40 | Parental consent for minors | **Verified** (2026-09-20) | Age computed at submission; under 18 requires a named guardian whose consent is its own ledger row; `ApplicantPortalTests` | 5.34 | PDPA-draft, GDPR 8 |
| AC-41 | Data-subject access (DSAR) | **Verified** by test | `CreateDsarArchiveAsync`, `PrivacyComplianceTests` | 5.34 | GDPR 15, FERPA inspection right |
| AC-42 | Logging hygiene (no secrets/PII in logs) | **Exists, unverified** | `SensitiveDataDestructuringPolicy`; request logging not tested for PII in paths/queries | 8.15 | |

## DETECT (DE)

| Id | Control | Status | Evidence / gap | ISO A. | Other |
|---|---|---|---|---|---|
| AC-50 | Access logging of reads on sensitive records | **Verified** (2026-09-20) | `RequireFerpaAudit` on 19 student-record endpoints, plus `RequireAccessAudit` for the records it cannot cover (it enforces a student relationship): employee list and detail, payroll bank export, gratuity, counsellor queue, foreign-student alerts and compliance, student health records. Both write to one log with an encrypted IP (`AccessAuditTests`) | 8.15 | FERPA 99.32 pattern, GDPR accountability, PECA evidence |
| AC-51 | Audit logging of writes on sensitive records | **Verified** (2026-09-20) | A `SaveChanges` interceptor writes an `AuditLog` row for every insert, update and delete of 24 entity types across identity, academic record, welfare, HR, payroll and privacy — in the same transaction, whichever code path made the change. Names, contact details, secrets and free text are recorded as changed but never copied. The dead `AuditLogService` is deleted. `SensitiveChangeAuditTests` on both providers | 8.15 | |
| AC-52 | Authentication event logging | **Verified** | `LoginAuditRecords` + `LoginAuditService.DetectAnomaliesAsync` + `/api/login-audit` | 8.15 | PECA |
| AC-53 | Log integrity / append-only | **Partial** | Finance ledger append-only guards + hash chain (migrations `FinanceLedgerAppendOnlyGuards`, `FinanceLedgerHashChain`); `AuditLogs`/`FerpaAuditLogs` are ordinary tables — `docs/06` "append-only policy; tamper detection via checksums" is true only for finance | 8.15 | |
| AC-54 | Monitoring and alerting | **Verified** (2026-09-22) | Login anomaly detection existed but discarded its findings; alerts are now recorded in `SecurityAlerts`, logged, and emailed to `Security:AlertEmail` for High and Critical, deduplicated per subject and type. `BulkAccessAlertWorker` watches the access audit for bulk reads and exports. `SecurityAlertTests`. **Remaining**: no alert on privilege change (the audit row exists; no rule reads it) | 8.16 | |
| AC-55 | Vulnerability scanning cadence | **Verified** | Every push (CI) and weekly (Dependabot); cadence and remediation windows in `12-vulnerability-register.md` | 8.8 | |

## RESPOND (RS)

| Id | Control | Status | Evidence / gap | ISO A. | Other |
|---|---|---|---|---|---|
| AC-60 | Incident response plan | **Verified** (2026-09-22) | `11-breach-notification.md`: roles, severities, a 48 h / 72 h / 7 d clock, templates, an incident-record format. Tested by tabletop on 2026-09-22, which produced four findings with owners and dates (`exercises/2026-09-22-tabletop-leaked-secrets.md`) | 5.24–5.26 | |
| AC-61 | Breach notification procedure (regulator, client, data subjects) | **Verified** (2026-09-20), contacts pending | Routes for the University (48 h, DPA §8), NCCIA under PECA, the PDA after gazette, HEC, and data subjects at 7 days, with content templates. **Remaining**: the NCCIA and PDA contact details are placeholders (tabletop finding F-3, due 2026-10-22) | 5.24, 5.26 | PECA, PDPA-draft, GDPR 33/34 |
| AC-62 | Vulnerability disclosure intake and triage | **Verified** | `SECURITY.md`: 2 business days to acknowledge, 7 to triage, 7/30 days to fix critical/high | 5.25 | |

## RECOVER (RC)

| Id | Control | Status | Evidence / gap | ISO A. |
|---|---|---|---|---|
| AC-70 | Restore tested | **Verified** (2026-09-20) | `deploy/scripts/backup-restore-drill.sh` writes a marker row, backs up, restores into a fresh database and asserts table count and marker. Run against the pilot database on 2026-09-20: 576 tables and the marker restored in 13 s (`exercises/2026-09-20-backup-restore-drill.md`); runs in CI on every push. `BackupService.RestoreFromBackupAsync` decrypts, restores and removes the plaintext. PITR still refuses rather than pretending — the runbook covers it | 8.13, 8.14 |
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

## Phase 3 work order — outcome

| Rank | Item | Controls | Status |
|---|---|---|---|
| 1 | Backup: one mechanism, always encrypted, restore proven in CI | AC-36, 37, 70 | **Done** — `5f52544a`; drill record in `exercises/` |
| 2 | Dependency scanning back on, CI gate, Dependabot | AC-13, 55 | **Done** — `d728dbbd`; found and fixed a critical Next.js RCE |
| 3 | Write-audit on sensitive records; delete dead code | AC-51 | **Done** — `18a93413` |
| 4 | Read-audit beyond student records | AC-50 | **Done** — `18a93413` |
| 5 | Field encryption for plaintext identifiers | AC-20 | **Done** — `74c72bf0`; DOB and salary remain (both queried by range) |
| 6 | Erasure and retention scope | AC-38 | **Done** — `98554fe1` |
| 7 | Consent capture and age gate | AC-39, 40 | **Done** — `3c32bb08`; non-admissions consent categories remain |
| 8 | Enforce MFA for administrators | AC-27 | **Done** — `a12b6f12` |
| 9 | Data-category least-privilege matrix | AC-29 | **Done** — `221e2cbf`; found `Graduation` could never be granted |
| 10 | `/metrics` behind auth or network policy | AC-33 | **Done** — `d9d201cd` |
| 11 | Security alert rules | AC-54 | **Done** — `d9d201cd` |
| 12 | Incident plan, breach procedure, `SECURITY.md`, one tabletop | AC-60–62 | **Done** — `d728dbbd`, `d7c3251b` |

## Carried into Phase 5 remediation

| # | Item | Control | Source |
|---|---|---|---|
| R-01 | GeoIP lookup sends every public sign-in IP to ipapi.co, on by default, previously undisclosed | AC-06, AC-08 | `02-data-flow-map.md` D-06 |
| R-02 | No rotation path for `AEGISCORE_PQC_KEY_BASE64` — a leaked master key cannot be contained in place | AC-23 | Tabletop F-1 |
| R-03 | No "how many data subjects" report for the 24-hour breach assessment | AC-60 | Tabletop F-2 |
| R-04 | NCCIA and PDA contact details are placeholders | AC-61 | Tabletop F-3 |
| R-05 | Date of birth and salary still plaintext | AC-20 | This assessment |
| R-06 | Consent categories beyond admissions not wired to their screens | AC-39 | This assessment |
| R-07 | Python sidecar dependencies never audited; image scanning only just enabled | AC-13 | `12-vulnerability-register.md` V-007, V-008 |
| R-08 | No periodic access review | AC-30 | This assessment |
| R-09 | Internal Docker network traffic is plaintext | AC-22 | This assessment |
| R-10 | No SBOM generated per build (the 2025 document claims one) | AC-12 | This assessment |

## NIST CSF 2.0 function scorecard

Scoring: share of controls in each function that are Verified (full weight) or Partial/Exists-unverified (half weight). "Not ours" excluded. Re-scored 2026-09-22 against the table above.

| Function | Verified | Partial / unverified | Gap | Score (2026-09-22) | Baseline (2026-09-20) |
|---|---|---|---|---|---|
| Govern (AC-01–09) | 5 | 1 | 3 | **61 %** | 17 % |
| Identify (AC-10–14) | 3 | 2 | 0 | **80 %** | 60 % |
| Protect (AC-20–42) | 15 | 4 | 2 | **81 %** | 57 % |
| Detect (AC-50–55) | 5 | 1 | 0 | **92 %** | 42 % |
| Respond (AC-60–62) | 3 | 0 | 0 | **100 %** | 17 % |
| Recover (AC-70–72) | 1 | 0 | 2 | **33 %** | 0 % |

Recover stays low on purpose: the restore is proven, but an agreed RTO/RPO per client (AC-71) and a post-incident review process that has actually been used (AC-72) do not exist yet. Govern is held down by the three genuine gaps — no approved policy set (AC-02), no second reviewer (AC-03), no scored risk register (AC-05) — and the first and third are Phase 4 deliverables.
