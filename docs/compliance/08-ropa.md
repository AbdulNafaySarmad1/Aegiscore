# 08 — Record of Processing Activities (ROPA)

**Version 1.0 · 2026-09-20 · living document — update whenever a module, integration or retention period changes.** Control AC-07 (ISO A.5.34; GDPR Art. 30 pattern; PDPA-draft record-keeping).

Two records are kept, because the roles differ:

- **Part 1 — processing the software performs for each University (the University is controller).** Written once here as the template every installation inherits; the University completes the bracketed fields.
- **Part 2 — processing Nocturne performs itself.**

Legal-basis vocabulary (pending a Pakistani statute; borrowed from GDPR Art. 6 so the record needs no rewrite later): *contract* (education/employment relationship), *legal obligation*, *public task* (public university statute), *consent*, *legitimate interest*, *vital interest*.

## Part 1 — Processing in a University's AegisCore installation

| # | Activity | Data subjects | Categories (→ `01-data-inventory.md`) | Purpose | Legal basis | Recipients | Transfer outside PK | Retention (→ `09`) | Access (roles) | Security (→ `04`) |
|---|---|---|---|---|---|---|---|---|---|---|
| P1 | Admissions: enquiry, application, merit, offer | Applicants (some minors), guardians | 2.2 | select and admit students | public task / consent (minors: guardian consent) | none | none unless OCR/AI enabled | unsuccessful: 2 y; admitted: becomes P2 | Admissions, Registrar | AC-26/28/50, app-encrypted CNIC (Phase 3) |
| P2 | Student register and academic record | Students | 2.1 identity, academic | deliver and certify education | contract / public task | HEC (attestation), examining bodies | none | permanent (register), identity anonymised on erasure | Registrar, Exam Controller, Faculty (own courses), Student (self) | AC-50 |
| P3 | Attendance incl. geolocation | Students, faculty | 2.1 attendance | attendance policy | contract | none | none | 7 y after graduation | Faculty (own courses), Registrar | encrypted coordinates |
| P4 | Student welfare: counselling, mental-health tickets, wellness | Students | 2.1 welfare | support and duty of care | vital interest / consent | mandatory-report authority where triggered | none | 7 y after last session | Counsellors, welfare officer only | AC-20/50 (Phase 3) |
| P5 | Discipline and misconduct | Students | 2.1 discipline | disciplinary procedure | public task | none | none | 7 y after closure | Registrar, discipline committee | AC-50 (Phase 3) |
| P6 | Foreign-student compliance | International students | 2.1 foreign | visa/MORA obligations | legal obligation | MORA / immigration authorities | to the authority only | 7 y after leaving | International office | AC-20 passport (Phase 3) |
| P7 | Fees, payments, scholarships | Students, payers | 2.1 fees | billing, accounting | contract / legal obligation | payment gateway (if enabled), auditors | gateway region | 10 y (financial) | Finance, Student (self) | ledger hash chain, PQC seals |
| P8 | Accommodation, transport, library, cafeteria | Students, staff | 2.1 services | run campus services | contract | none | none | 2 y after service ends | service staff | — |
| P9 | Student elections | Students | ballots (encrypted) | elections | public task | none | none | ballots: until certified + 1 y | election officer | app-encrypted |
| P10 | Achievements, badges, leaderboard | Students | 2.1 achievements | recognition, publicly displayed | consent (opt-out via directory flag) | public | none | permanent | all users | — |
| P11 | HR: employment, contracts, leave, performance, training | Staff, faculty, visiting faculty | 2.3 | employment administration | contract / legal obligation | EOBI, FBR | none | 7 y after leaving | HR, HOD (own dept), Employee (self) | AC-50/20 (Phase 3) |
| P12 | Payroll | Staff | 2.3 payroll | pay and statutory deductions | legal obligation | bank, FBR, EOBI, PIFRA | none | 10 y | Payroll officer | encrypted bank reference |
| P13 | Recruitment | Candidates | 2.3 recruitment | hiring | consent / pre-contract | none | none | 1 y after decision | HR | — |
| P14 | Accreditation: faculty CVs, OBE evidence | Faculty, students (aggregated) | 2.3 CVs | HEC/PEC accreditation | public task | accreditation bodies | none | duration of cycle + 5 y | Accreditation office | — |
| P15 | Accounts, authentication, SSO, MFA | All users | 2.4 | secure access | contract / legitimate interest | OIDC provider (Entra/Google/Okta) if enabled | provider region | account life + 1 y logs | IT | AC-26/27 |
| P16 | Login audit and access audit | All users | 2.4 login audit, 2.5 access logs | security monitoring, accountability | legitimate interest / legal obligation (PECA evidence) | NCCIA on lawful request | none | 1 y (login), 6 y (access audit) | IT, Auditor | app-encrypted IPs (Phase 3 extends) |
| P17 | Consent, DSAR, erasure records | Data subjects who exercise rights | 2.5 | honour and prove rights | legal obligation / legitimate interest | none | none | 6 y; erasure metadata 7 y | DPC role / System.Admin | app-encrypted IP |
| P18 | Visitor and gate management | Visitors | 2.6 | campus security | legitimate interest | police on lawful request | none | 90 d | Security office | — |
| P19 | Vendor management | Vendor contacts, sole traders | 2.6 vendors | procurement, payment | contract / legal obligation | FBR | none | 10 y | Procurement, Finance | — |
| P20 | Notifications (email/SMS/WhatsApp/push) | All users | contact data + message | communication | contract | SMTP relay; Twilio/Meta/Google if enabled | USA/global if enabled | logs 1 y | system | — |
| P21 | AI assistant, document AI, support bot | Users who use them | free text, documents | assistance | consent (feature use) | OpenAI if enabled; Azure if enabled | USA / Azure region if enabled | prompts not stored beyond logs 1 y | users | D-D in `05` |
| P22 | Backups and replicas | all of the above | everything | continuity | legitimate interest | backup storage provider if enabled | bucket region | 30 d rolling (default) | IT | encrypted (Phase 3) |
| P23 | Directory / search index | Students, staff | name, programme | find people | legitimate interest, opt-out | none | none | rebuilt continuously | all users | `DirectoryOptOut` |

## Part 2 — Processing by Nocturne Systems (Pvt.) Ltd.

| # | Activity | Data subjects | Categories | Purpose | Legal basis | Recipients | Transfer | Retention | Security |
|---|---|---|---|---|---|---|---|---|---|
| N1 | Licence administration (heartbeat) | none — institutional data only | `university_id`, name, user count, suite, timestamp | licensing, billing | contract | none | to the licence server's hosting location [record here] | licence life + 2 y | TLS, token header |
| N2 | Customer contacts | University staff who deal with Nocturne | name, role, email, phone | sales, support, contract | contract / legitimate interest | none | none | relationship + 3 y | ordinary business systems |
| N3 | Support incidents | University staff; possibly data subjects if the University shares records | whatever the University shares | support | contract (DPA §7) | none | none | deleted at incident close per DPA §7.3 | DPA §7 |
| N4 | Vulnerability reports | reporters | name/handle, email, report | security | legitimate interest | none | none | 3 y | `SECURITY.md` |
| N5 | Model B pilots | synthetic data only | — | demonstration | — | — | — | end of pilot | D-A in `05` |
| N6 | Source code and CI | committers | name, email in git history | development | legitimate interest | GitHub | USA (GitHub) | permanent (git) | GitHub controls |

## Maintenance

- Re-issue when: a new module stores personal data; an integration is added; a retention period changes; the legal basis vocabulary is replaced by statutory terms.
- Cross-check against `01-data-inventory.md` at each phase boundary; every entity in `01` must map to a row here.
