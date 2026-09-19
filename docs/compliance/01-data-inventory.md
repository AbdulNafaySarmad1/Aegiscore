# 01 — Personal data inventory

**As of:** 2026-09-20, commit `1fa1d306`. Every row was checked against the entity source, not against earlier design documents. `docs/06-security-architecture-document.md` (2025-01) claims "AES-256-GCM column-level encryption for PII" — **that claim is not true for the columns below** and that document should be treated as superseded by this inventory.

## 1. Where personal data lives — the stores

| Store | What | Evidence |
|---|---|---|
| PostgreSQL (one database, one `AegisDbContext`, ~450 DbSets) | All structured records below. Row-level tenant separation via global query filters + a per-connection `SET` of the tenant id (`AegisCore.Infrastructure/TenantSessionInterceptor.cs:14-36`) | `AegisCore.Infrastructure/AegisDbContext.cs` |
| Object storage (`AEGISCORE_STORAGE_PROVIDER`: local disk `/app/storage` by default; S3/Azure/R2 optional) | Uploaded files: student/employee/applicant documents, profile pictures, document-vault entries, signed finance artefacts | `AegisCore.Infrastructure/Cloud/Storage/`, `docker-compose.client.yml:79` |
| Redis | Rate-limit counters, brute-force lockouts, refresh-token/session state, PQC replay store, Infisical cache | `docs/CODEBASE_KNOWLEDGE.md §14` |
| Encrypted local secret vault `/app/config/secrets/vault.bin` (FILE provider, default for client installs) | OIDC client secrets, payment-gateway credentials, wizard-collected secrets. AES-256-GCM, key = `AEGISCORE_PQC_KEY_BASE64` | `AegisCore.Infrastructure/Cloud/OnPremise/OnPremiseProvider.cs:32-131`, `docker-compose.client.yml:77` |
| Config volume `university_config` | `generated-secrets.env` (JWT/encryption/PQC keys, setup token — **plaintext file**), RSA JWT signing keys, Data Protection key ring (encrypted with the PKCS#12 certificate) | `docker-compose.client.yml:19-25,46-48`, `AegisCore.API/Extensions/ProductionServicesExtensions.cs:101-107` |
| Serilog sinks | Console JSON; optional PostgreSQL sink. `SensitiveDataDestructuringPolicy` masks known sensitive properties | `AegisCore.API/Logging/SensitiveDataDestructuringPolicy.cs` |
| Backups | `pg_dump` custom-format files under the backup directory; optional S3 upload | `deploy/scripts/backup.sh`, `AegisCore.Infrastructure/Backup/BackupService.cs` |
| Mobile device (Expo) | Offline attendance events and cached records in WatermelonDB/SQLite; tokens in `expo-secure-store` | `AegisCore.Mobile/package.json` |

## 2. Data categories

Legend — **Enc. at rest:** "app" = application-level AES-GCM via `IPqcCryptographyService`; "disk" = only whatever the host's volume/disk encryption provides (the application stores plaintext); **Erasure:** covered by `PrivacyComplianceService.ProcessErasureAsync` (`AegisCore.Infrastructure/Compliance/PrivacyComplianceService.cs:113-232`) or not.

### 2.1 Students and their records

| Data | Entity / table | Fields of note | Enc. at rest | Read-audited (`RequireFerpaAudit`) | Erasure |
|---|---|---|---|---|---|
| Identity | `Students` (`AegisCore.Domain/Entities/Student.cs`) | name, university id, roll number, applicant id, `DirectoryOptOut` | disk | partly (grade/transcript/attendance endpoints only) | yes (anonymised) |
| Extended profile | `StudentProfiles.ProfileJson` (jsonb, free-form) | **unknown content — whatever the wizard/import writes**; treat as PII of unknown sensitivity until schema is fixed | disk | no | yes |
| Guardian / parent | `StudentGuardians` | name, relationship, phone, email | disk | no | yes |
| Academic record | `Enrollments`, `Results`, `CourseGrades`, `Transcripts`, `GradeAppeals`, `DegreeAudit` | grades, GPA, appeals | disk | yes on `S3ExaminationController`, `DegreeAuditController` (19 endpoints in total across the API) | **no** — academic facts intentionally preserved (see `PrivacyComplianceTests.FullErasureCycle_AnonymizesPii_PreservesAcademicFacts…`) |
| Attendance incl. geolocation | `Attendances` (`Latitude`/`Longitude` stored as `LatitudeEncrypted` bytes, `Attendance.cs:23-59`), `OfflineAttendanceEvents` | GPS coordinates | **app** (coordinates) | yes on `AttendanceController`/`AttendanceGeolocationController` | no |
| Discipline | `StudentDisciplines` | severity, 2000-char free-text description | disk | no | **no** |
| Counselling | `StudentCounselings` | free-text session notes, counsellor id | disk | no | **no** |
| Mental health | `MentalHealthTickets` (`MentalHealthEntities.cs:8`), `MandatoryReports`, `WellnessCheckIns` | concern type/description, crisis and mandatory-report flags, report content sent to an authority | disk | no | **no** |
| Health / disability | not found as dedicated fields; may appear in `ProfileJson` or uploaded documents | — | — | — | — |
| Documents | `StudentDocuments.FileUrl` + object storage | scans of CNIC, certificates, photos | disk / storage provider | no | **no** (DB row and file both remain) |
| Profile picture | `ProfilePictureRecords` + storage | face image | disk | no | yes (file deleted) |
| Foreign students | `ForeignStudentRegistries`, `VisaDocuments`, `VisaRenewals` | nationality, **passport number**, visa number, MORA registration | disk | no | **no** |
| Hostel, transport, library, cafeteria, elections | `StudentHostels`, `StudentTransports`, library loans, cafeteria orders, `EncryptedBallots` | room, route, borrowing, purchases; ballots are encrypted | disk (ballots: app) | no | no |
| Achievements, badges, leaderboard | `StudentAchievements`, `StudentBadges`, `Leaderboard` | public-facing by design | disk | no | no |
| Fees and payments | `FeeDemands`, `StudentFeePayments`, `FinancePayments`, gateway events | amounts, payment method, gateway references | disk | no | **no** (financial record — legitimate retention, must be stated in the retention policy) |
| Directory | search index (`SearchService` / optional Elasticsearch) | name, programme | disk / ES | n/a | `DirectoryOptOut` removes from index |

### 2.2 Applicants (pre-enrolment; many are minors)

| Data | Entity | Fields of note | Enc. | Read-audited | Erasure |
|---|---|---|---|---|---|
| Application | `AdmissionApplications` (`AdmissionsEntities.cs:10-25`) | name, email, phone, **CNIC**, date of birth, gender, quota type, Matric/FSc marks, entry-test score | disk | yes on `S2AdmissionsController`, `AdmissionsAdminController`, `AdmissionsMigrationController` | yes |
| Applicant portal profile | `ApplicantProfiles` (`ApplicantPortalEntities.cs:47-70`) | DOB, gender, nationality, phone | disk | no | **no** (separate entity from `AdmissionApplications`) |
| Applicant documents | `ApplicantDocuments` + storage; validated by the CLIP sidecar | document images, validation confidence | disk | no | **no** |
| Admission leads / CRM | `AdmissionLeads`, `EnquiryLeads` | contact details of people who never applied | disk | no | **no** |

Minors: applicants can be under 18 (intermediate leavers). There is **no age check and no parental-consent capture** anywhere in the admissions flow (grep for `DateOfBirth` consumers found none that compute age). Phase 2 item.

### 2.3 Staff, faculty and visiting faculty

| Data | Entity | Fields of note | Enc. | Read-audited | Erasure |
|---|---|---|---|---|---|
| Employee master | `Employees` (`CoreErpFinanceHrSis.cs:47-56`) | name, code, email, designation, **salary** | disk | **no** (`HrController` has no `RequireFerpaAudit`) | **no** — erasure is student/user-centric only |
| Contracts, documents | `EmployeeContracts`, `EmployeeDocuments` + storage | salary, contract type, scanned documents | disk | no | no |
| Payroll | `PayrollRuns`, `Payslips`, `TaxSlabs`, settlement `EncryptedAccountReference` (`FinancePayrollSettlementEntities.cs:12`) | salary, tax, EOBI/PF, **bank account (app-encrypted)** | disk; bank account **app** | no | no |
| Leave, performance, training | `LeaveApplications`, `PerformanceReviews`, `TrainingAssignments` | reviews are evaluative personal data | disk | no | no |
| Recruitment | `JobApplications`, `InterviewSchedules` | candidates who were never hired | disk | no | no |
| Visiting faculty | `VisitingFacultyProfiles` (`VisitingFacultyEntities.cs:13`) | **CNIC**, contract | disk | no | no |
| Faculty CVs (accreditation) | `FacultyCVs` | full career history | disk | no | no |

### 2.4 Accounts and authentication

| Data | Entity | Fields of note | Enc. | Notes |
|---|---|---|---|---|
| User accounts | `Users`, `UserRoles`, `UserSecondFactors` | email, Argon2id password hash, TOTP secret | hash; TOTP secret: **check** (not verified in Phase 1) | erasure covers `Users` |
| Login audit | `LoginAuditRecords` (`LoginAuditRecord.cs`) | email, role, **IP, GeoIP city, GPS lat/long**, user agent, device, Microsoft OID | disk — **plaintext IP and GPS**, unlike the consent/FERPA logs | not covered by erasure or by the retention worker |
| SSO sessions | `SsoSessions`, `IdentityProviderConfigurations` | external subject ids; client-secret **name** only (`IdentityProviderConfiguration.cs:33-41`) | secret value lives in the vault | erasure removes `SsoSessions` |
| Refresh tokens | `UserRefreshTokens` + Redis | opaque tokens | — | revoked on logout |
| API keys / webhooks | `ApiKeys`, `WebhookSubscriptions` | partner credentials | **check** hashing (not verified) | — |

### 2.5 Compliance records (themselves personal data)

| Data | Entity | Enc. | Retention |
|---|---|---|---|
| Consent ledger | `ConsentLedgers` — exact consent text, category, timestamp, expiry, **IP (app-encrypted)** | app | `compliance.audit_retention_years` (default 6) |
| FERPA-style access log | `FerpaAuditLogs` — who read what, permitted/denied, **IP (app-encrypted)** | app | same |
| Erasure requests | `ErasureRequests` — metadata kept 7 years after completion | disk | fixed 7 years |
| DSAR export | generated archive (5 files), expires after 24 h | storage provider | 24 h |
| General audit log | `AuditLogs` — action, resource, actor, IP, details JSON | disk | **not** covered by the retention worker |

### 2.6 Other natural persons

| Data | Entity | Fields | Notes |
|---|---|---|---|
| Campus visitors | `Visitors` (`Visitor.cs:17`), `GateEntries` (`VisitorCNIC`) | name, **CNIC**, entry/exit times | no retention rule, no erasure |
| Vendors (sole traders) | `FinanceVendors.VendorCnic` (`FinanceEntities.cs:84`) | CNIC, bank details | financial retention |
| Drivers, guards | `TransportDrivers`, `SecurityGuards` | licence, shifts, GPS of vehicles (`Latitude` on vehicle tracking, `CoreErpEntities3.cs:92`) | driver location = personal data while on shift |
| Board members, election candidates | `BoardMeetingPacks`, `CandidateNominations` | names, votes (ballots encrypted) | — |

## 3. Who can access what

- **Server-side gate:** JWT (RS256 only in production) → `[Authorize]` per controller → `RequirePermissionAttribute`/`PermissionAuthorizationHandler` → Casbin ABAC middleware. Default deny (`docs/CODEBASE_KNOWLEDGE.md §9`, commit `6677e151`).
- **Role catalogue:** `docs/ROLE_CAPABILITIES.md` (296 lines). Phase 3 must map each data category above to the roles that legitimately need it; today the mapping is by *feature*, not by *data category*. Known over-reach candidates to test in Phase 3: can `Admin` read `StudentCounselings`/`MentalHealthTickets`? can `HOD` read `Employees.Salary` outside their department? can `Registrar` read payroll?
- **Vendor (Nocturne) access:** none by design in the client model — the university runs the container, owns the database, and holds the keys. The licence heartbeat carries only `university_id`, `university_name`, `current_user_count`, `suite`, `timestamp` (`docs/FERPA_GDPR_COMPLIANCE.md`, verified at `AegisCore.Infrastructure/License/LicenseService.cs:173-176`). In the cloud model (`docker-compose.cloud.yml`) the vendor is a processor with full infrastructure access — see `03-data-residency.md`.

## 4. Findings that change the compliance posture (carried into 04)

| # | Finding | Severity |
|---|---|---|
| F-01 | Application-level encryption at rest exists but covers only five things: consent IPs, FERPA-log IPs, attendance coordinates, payroll bank references, ballots. CNIC, passport numbers, DOB, salary, counselling and mental-health text, guardian contacts and `LoginAuditRecords` (IP + GPS) are plaintext in PostgreSQL. | High |
| F-02 | Read-audit (`RequireFerpaAudit`) is applied to 19 endpoints (admissions, grades, transcripts, attendance). Employee records, counselling, discipline, mental health, foreign-student/passport data and applicant-portal profiles have **no read audit**. | High |
| F-03 | The general write audit (`AuditLogs`) is written by `IAuditService` from only 4 call sites (`grep IAuditService` → `PasswordController` + 3 in Infrastructure). `AuditLogService` (`AegisCore.Infrastructure/Security/AuditLogService.cs`) is **not registered anywhere** — dead code. Grade changes, salary edits, role assignments are not reliably audited. | High |
| F-04 | Erasure covers `Students`, `StudentProfiles`, `StudentGuardians`, `AdmissionApplications`, `Users`, `ConsentLedgers`, `FerpaAuditLogs`, `SsoSessions`, `ProfilePictureRecords`. It does **not** touch counselling, discipline, mental-health, student documents/files, applicant-portal profiles, applicant documents, foreign-student registry, login audit, employees. "Delete path incl. backups" (Phase 2 §5) does not exist for backups at all. | High |
| F-05 | `StudentProfiles.ProfileJson` is schemaless; nobody can say what personal data is in it. | Medium |
| F-06 | No age computation / parental consent for applicants who are minors. | Medium |
| F-07 | `LoginAuditRecords` store IP, GeoIP and GPS in plaintext with no retention rule — the one table the retention worker most needs to cover. | Medium |
| F-08 | Visitor and gate-entry CNICs have no retention or deletion rule. | Medium |
| F-09 | TOTP secrets (`UserSecondFactors`) and API-key storage format not verified in Phase 1. | To verify in Phase 3 |
