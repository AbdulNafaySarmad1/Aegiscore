# 09 — Data retention and deletion policy

**Version 1.0 · 2026-09-20.** Control AC-38 (ISO A.5.33, A.8.10; PDPA-draft storage limitation; GDPR 5(1)(e)/17 pattern). Periods marked **[DEFAULT — confirm]** were chosen by engineering from common Pakistani record-keeping practice (Companies Act 10-year financial records, HEC academic register permanence, 7-year employment records) and must be confirmed by each University's registrar/legal team, who can override them per tenant.

## 1. Principles

1. A period is only real if the system enforces it. Every row below names the mechanism; where the mechanism is "Phase 3", the period is a target until that item ships and is listed as a gap in `04`.
2. "Delete" means the live database row and the file in object storage. Backups are bounded separately (§4): a deleted record persists in encrypted backups for at most the backup retention window and is never restored selectively.
3. Academic facts (a grade, a degree) are never deleted; the person is **anonymised** (name and identifiers replaced) on erasure. The register survives, the identity does not.
4. Legal holds override this policy: a record under an investigation, appeal, audit or lawful request is retained until the hold is lifted, and the hold is noted on the record.

## 2. Retention schedule

Mechanism key: **worker** = `ComplianceRetentionWorker` (02:00 tenant-local); **erasure** = `PrivacyComplianceService.ProcessErasureAsync`; **manual** = administrator action with audit entry; **P3** = Phase 3 code item.

| Category (→ `01`) | Retention | Trigger | Mechanism today | Target mechanism |
|---|---|---|---|---|
| Academic register (grades, transcripts, degrees, enrolments) | permanent | — | none (correct) | anonymise identity on erasure (exists) |
| Student identity (`Students`, `StudentProfiles`, `StudentGuardians`) | life of relationship + 7 y after graduation/withdrawal [DEFAULT — confirm] | graduation / withdrawal date | erasure on request only | worker: anonymise after period (P3-6) |
| Attendance incl. coordinates | 7 y after graduation [DEFAULT] | as above | none | worker (P3-6) |
| Counselling notes, mental-health tickets, wellness | 7 y after last session [DEFAULT] | last session | none | worker + erasure (P3-6) |
| Discipline / misconduct | 7 y after case closure [DEFAULT] | closure | none | worker + erasure (P3-6) |
| Student documents and files | with the student identity; deleted on erasure | erasure / period end | **none — files and rows remain** | erasure + worker delete file via `IStorageService` (P3-6) |
| Applicants — unsuccessful/withdrawn (`AdmissionApplications`, `ApplicantProfiles`, `ApplicantDocuments`, leads) | 2 y after the admission cycle closes [DEFAULT] | cycle close | erasure covers `AdmissionApplications` only | worker + erasure for all four (P3-6) |
| Foreign-student registry, visa documents | 7 y after leaving [DEFAULT] | leaving | none | worker + erasure (P3-6) |
| Fees, payments, ledgers, payslips | 10 y [DEFAULT — Companies Act / tax] | transaction date | none (append-only ledger) | worker archive/purge after 10 y — **ledger hash chain must be sealed before purge**; design in Phase 4 risk register |
| Staff records (employment, contracts, leave, performance, training, CVs) | 7 y after leaving [DEFAULT] | leaving date | none | worker (P3-6) |
| Recruitment (unsuccessful candidates) | 1 y after decision [DEFAULT] | decision | none | worker (P3-6) |
| Payroll bank references | with staff record | — | encrypted | as staff record |
| Visitors, gate entries (CNIC) | 90 d [DEFAULT] | entry date | none | worker (P3-6) |
| Login audit (`LoginAuditRecords`: IP, GeoIP, GPS, device) | 1 y [DEFAULT] | event date | **none** | worker (P3-6); IP/GPS encrypted (P3-5) |
| Access audit (`FerpaAuditLogs`) | `compliance.audit_retention_years`, default 6 | event date | worker ✔ | — |
| Write audit (`AuditLogs`) | 6 y (same setting) | event date | none | worker (P3-6) |
| Consent ledger | 6 y after expiry/revocation | — | worker ✔ | — |
| Erasure request metadata | 7 y after completion (fixed) | completion | worker ✔ | — |
| DSAR export archive | 24 h | creation | expiry stamp ✔ (verify the file is actually removed — P3) | storage lifecycle |
| Application logs (Serilog console / PostgreSQL sink) | 1 y [DEFAULT] | — | host log rotation (University) | document in `INSTALL.md`; PostgreSQL sink table purge (P3-6) |
| Refresh tokens, sessions, rate-limit keys (Redis) | token lifetime / 24 h | expiry | Redis TTL ✔ | — |
| Notification logs | 1 y | — | none | worker (P3-6) |
| Backups | 30 d rolling [DEFAULT] | backup date | `backup.sh` prune ✔ (local + S3) | keep; encrypt always (P3-1) |
| Read replicas / WAL archive | mirrors primary; WAL 30 d | — | University config | document |
| Search index | rebuilt from live data | — | `DirectoryOptOut` ✔ | — |

## 3. Erasure (data-subject request) — the actual path

1. Request recorded (`POST /api/compliance/erasure` → `ErasureRequests`, status Pending, 30-day deadline).
2. Worker or administrator runs `ProcessErasureAsync`: today anonymises `Students`/`StudentProfiles`/`StudentGuardians`/`AdmissionApplications`/`Users`, deletes `SsoSessions`, `ConsentLedgers`, `FerpaAuditLogs` for the subject, deletes profile-picture files. Phase 3 extends the scope to every row in §2 marked "erasure".
3. Completion recorded; metadata kept 7 years as proof.
4. Backups containing the record expire within 30 days (§4). The person is told this in the acknowledgement.
5. Verification: `AegisCore.Tests/PrivacyComplianceTests.cs` proves the anonymise-and-preserve behaviour on every CI run; Phase 3 adds a test that the extended scope leaves no row or file behind.

## 4. Backups and "deleted"

- Backups are full `pg_dump` images, encrypted (Phase 3 makes this unconditional), retained 30 days locally and in the optional bucket, then pruned by `deploy/scripts/backup.sh`.
- A restore replaces the whole database; there is no selective restore of one person's data. After a restore from a backup older than an erasure, the erasure worker re-runs (`ErasureRequests` with status Completed but a live identifiable `Students` row are re-processed — Phase 3 item).
- Therefore the honest statement is: *"erased from live systems within 30 days of the request; from backups within a further 30 days."*

## 5. Legal holds

An administrator marks a hold by adding the record id to the tenant setting `compliance.legal_holds` (Phase 3 implements the setting and the worker honours it). Holds are reviewed at each management review.

## 6. Ownership

The University's registrar owns the periods; the DPC owns the mechanism. Changes to a period are made in tenant settings (`compliance.*` keys) and noted in the ROPA.
