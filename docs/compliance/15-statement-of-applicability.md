# 15 — Statement of Applicability

**Version 1.1 · 2026-09-22 · approved by the DPC.** (1.1: A.5.1 and A.5.10 closed by `19-information-security-policy.md` following internal audit finding A-07.) ISO/IEC 27001:2022 clause 6.1.3 d), all 93 Annex A controls. Scope: `13-isms-scope.md`. Risks: `14`. Unified control set: `04-nist-csf-gap-assessment.md` (the `AC-nn` column). ISO/IEC 27002:2022 is the implementation reference.

**Status vocabulary** (same as `04`, so the two documents cannot drift): **Implemented** (evidence exists), **Partial** (named remainder), **Planned** (dated), **N/A** (the activity does not exist in this organisation — never "too small to bother").

**Applicability:** *Yes* = the control applies to this scope. *No* = excluded, with the reason.

---

## A.5 Organizational controls (37)

| # | Control | Applies | Status | How it is implemented / why excluded | AC |
|---|---|---|---|---|---|
| 5.1 | Policies for information security | Yes | Implemented (2026-09-22) | `19-information-security-policy.md`, approved and versioned, governing the rest of the folder. `docs/06` now carries a superseded banner naming each statement of it that was never true | AC-02 |
| 5.2 | Information security roles and responsibilities | Yes | Implemented | `05-governance.md` §§1–2; one person, named, with each responsibility listed against the role | AC-01, AC-03 |
| 5.3 | Segregation of duties | Yes | **Partial** | Impossible with one person. Compensating: CI gates that the author cannot bypass, PR checklist and 24-hour soak on sensitive paths, append-only finance ledger with hash chain, audit trail the author cannot edit silently. Risk R-014 retained | AC-03 |
| 5.4 | Management responsibilities | Yes | Implemented | `17-management-review.md`; the DPC is also the management, and the review record shows the decisions | — |
| 5.5 | Contact with authorities | Yes | **Partial** | Routes identified (NCCIA, PDA, HEC) in `11` §9; the contact details are placeholders pending confirmation (R-04) | AC-61 |
| 5.6 | Contact with special interest groups | Yes | Implemented | CVE/NVD and GitHub advisory feeds drive Dependabot; PKCERT handbook tracked; vendor advisories for .NET, Next.js, PostgreSQL | AC-13 |
| 5.7 | Threat intelligence | Yes | **Partial** | STRIDE model in `docs/06` §3 and the advisory feeds above; not yet linked to the risk register on a cadence | AC-14 |
| 5.8 | Information security in project management | Yes | Implemented | Every change enters through the compliance programme's control set; new personal data must be added to `01` and `08` before release | AC-10 |
| 5.9 | Inventory of information and other associated assets | Yes | Implemented | `01-data-inventory.md` (information), `13` §2 (company assets), `DEPENDENCY_AUDIT.md` + lockfiles (software) | AC-10, AC-12 |
| 5.10 | Acceptable use of information and other associated assets | Yes | Implemented (2026-09-22) | `19` §3–4 and the workstation rules in `18` §3 | AC-02 |
| 5.11 | Return of assets | Yes | **N/A in practice, retained** | No employees or contractors hold assets. The clause activates on the first hire; the onboarding checklist in `18` carries the offboarding half | — |
| 5.12 | Classification of information | Yes | Implemented | `01-data-inventory.md` classifies by category and sensitivity; `docs/06` §2 gives the Restricted/Critical/Internal scheme | AC-10 |
| 5.13 | Labelling of information | Yes | **Partial** | Data categories are labelled in the inventory and in code (`FieldEncryptionModel`, the audit interceptor's entity map). Documents are not labelled | AC-10 |
| 5.14 | Information transfer | Yes | Implemented | Transfers enumerated in `02-data-flow-map.md` §3 with the control on each; DPA Schedule 4 binds the client side | AC-11 |
| 5.15 | Access control | Yes | Implemented | Default-deny, `[Authorize]` + permission attribute + Casbin; tenant isolation tested in CI | AC-28, AC-31 |
| 5.16 | Identity management | Yes | Implemented | Local accounts, OIDC with group→role mapping applied per sign-in, SCIM provisioning and deprovisioning | AC-30 |
| 5.17 | Authentication information | Yes | Implemented | Argon2id, RS256-only tokens, brute-force lockout, TOTP with hashed single-use backup codes, secrets in the vault by name | AC-26, AC-27 |
| 5.18 | Access rights | Yes | **Partial** | Granted and revoked through roles; the data-category matrix is tested (AC-29). No periodic access review yet (R-08) | AC-29, AC-30 |
| 5.19 | Information security in supplier relationships | Yes | **Partial** | Supplier list in `02` §4 and DPA Schedule 4; no security assessment of each supplier yet | AC-06 |
| 5.20 | Addressing information security within supplier agreements | Yes | Implemented | `07-dpa-template.md` §§4–8 carries the obligations both ways | AC-09 |
| 5.21 | Managing information security in the ICT supply chain | Yes | Implemented | Digest-pinned base images, lockfiles, Dependabot across six ecosystems, Trivy on both shipped images | AC-13 |
| 5.22 | Monitoring, review and change management of supplier services | Yes | **Partial** | Dependabot and Trivy watch the technical supply chain; no scheduled review of the commercial suppliers | AC-06 |
| 5.23 | Information security for use of cloud services | Yes | Implemented | Provider choice is the client's (`03-data-residency.md`); the product supports on-premise for every provider-backed function; pilot environments hold synthetic data only | AC-06 |
| 5.24 | Information security incident management planning and preparation | Yes | Implemented | `11-breach-notification.md`, exercised 2026-09-22 | AC-60 |
| 5.25 | Assessment and decision on information security events | Yes | Implemented | Severity table `11` §3; vulnerability triage windows in `12` | AC-60, AC-62 |
| 5.26 | Response to information security incidents | Yes | Implemented | `11` §4 clock and §5 templates | AC-60 |
| 5.27 | Learning from information security incidents | Yes | Implemented | `11` §8 post-incident review; the tabletop already fed four findings into `04` and `14` | AC-72 (partial) |
| 5.28 | Collection of evidence | Yes | Implemented | `11` §4 requires logs copied not truncated; §7 records evidence paths and hashes; audit tables are append-only in practice and the finance ledger is hash-chained | AC-53 |
| 5.29 | Information security during disruption | Yes | **Partial** | Backup and restore proven; licence grace period prevents a lockout during an outage. No documented continuity plan for the company itself (R-013) | AC-71 |
| 5.30 | ICT readiness for business continuity | Yes | **Partial** | Restore drill in CI and quarterly at the client; RTO/RPO not agreed per client (AC-71 gap) | AC-70, AC-71 |
| 5.31 | Legal, statutory, regulatory and contractual requirements | Yes | Implemented | `00-PROGRAM.md` legal register, re-checked at every phase boundary and dated | AC-04 |
| 5.32 | Intellectual property rights | Yes | Implemented | Licence terms; third-party licences tracked in lockfiles and `DEPENDENCY_AUDIT.md` | — |
| 5.33 | Protection of records | Yes | Implemented | Retention and deletion per `09`; audit and consent records protected and retained; finance ledger append-only with PQC seals | AC-38, AC-53 |
| 5.34 | Privacy and protection of PII | Yes | Implemented | The whole of `01`, `06`, `08`, `09`, `10`; encryption, consent, erasure, DSAR all evidenced | AC-20, AC-38–41 |
| 5.35 | Independent review of information security | Yes | **Planned** — Phase 5 (internal audit), Phase 6 (external) | `16-internal-audit-programme.md` | — |
| 5.36 | Compliance with policies, rules and standards | Yes | Implemented | CI enforces the technical rules (audits, tenant isolation, accessibility, migrations, restore drill); internal audit covers the rest | AC-34 |
| 5.37 | Documented operating procedures | Yes | Implemented | `INSTALL.md`, `ADMINISTRATION.md`, `TROUBLESHOOTING.md`, `dcr-runbook.md`, the deploy scripts | — |

## A.6 People controls (8)

| # | Control | Applies | Status | How / why | AC |
|---|---|---|---|---|---|
| 6.1 | Screening | Yes | **N/A until first hire** | No employees or contractors. The procedure is written in `18` §4 so it exists before it is needed | — |
| 6.2 | Terms and conditions of employment | Yes | **N/A until first hire** | Same; security terms drafted in `18` §4 | — |
| 6.3 | Information security awareness, education and training | Yes | Implemented | `18-security-awareness-training.md`, completed by the one person on 2026-09-22 with the record kept | AC-35 |
| 6.4 | Disciplinary process | Yes | **N/A until first hire** | Same | — |
| 6.5 | Responsibilities after termination or change of employment | Yes | **N/A until first hire** | Offboarding checklist in `18` §4 | — |
| 6.6 | Confidentiality or non-disclosure agreements | Yes | Implemented | NDA/confidentiality in the client licence and DPA §5; an NDA template is required before any contractor engagement | AC-09 |
| 6.7 | Remote working | Yes | Implemented | All work is remote. `18` §3 sets the rules: full-disk encryption, screen lock, no client personal data on the workstation, no work from untrusted networks without the VPN | AC-34 |
| 6.8 | Information security event reporting | Yes | Implemented | `SECURITY.md` externally; `11` §6 internally | AC-62 |

## A.7 Physical controls (14)

The company has no offices, server rooms or data centres. Client infrastructure is out of scope (`13` §3 O-1, O-2) and covered contractually by DPA §3. The one physical asset in scope is the developer workstation (S-5).

| # | Control | Applies | Status | How / why | AC |
|---|---|---|---|---|---|
| 7.1 | Physical security perimeters | No | N/A | No company premises. Client and cloud premises are out of scope; the provider's own certification covers 5.23 | — |
| 7.2 | Physical entry | No | N/A | As 7.1 | — |
| 7.3 | Securing offices, rooms and facilities | No | N/A | As 7.1 | — |
| 7.4 | Physical security monitoring | No | N/A | As 7.1 | — |
| 7.5 | Protecting against physical and environmental threats | No | N/A | As 7.1. Loss of the workstation is treated as a security risk (R-015), not an environmental one | — |
| 7.6 | Working in secure areas | No | N/A | As 7.1 | — |
| 7.7 | Clear desk and clear screen | Yes | Implemented | `18` §3: screen lock on, no printed client records, no client personal data on the workstation | — |
| 7.8 | Equipment siting and protection | Yes | Implemented | Workstation kept in the private home office; not used in public spaces for work touching S-4 | — |
| 7.9 | Security of assets off-premises | Yes | Implemented | Full-disk encryption; the device is the off-premises asset by definition (`18` §3) | — |
| 7.10 | Storage media | Yes | Implemented | No removable media used for client data; backups in scope are encrypted files, not media | AC-37 |
| 7.11 | Supporting utilities | No | N/A | No company facility to power or cool | — |
| 7.12 | Cabling security | No | N/A | As 7.11 | — |
| 7.13 | Equipment maintenance | Yes | Implemented | OS and firmware auto-update on the workstation; verified at internal audit | — |
| 7.14 | Secure disposal or re-use of equipment | Yes | **Planned** — on first disposal | Procedure in `18` §3: encrypted drive plus cryptographic erase, recorded. No device has been disposed of yet | — |

## A.8 Technological controls (34)

| # | Control | Applies | Status | How / why | AC |
|---|---|---|---|---|---|
| 8.1 | User endpoint devices | Yes | Implemented | `18` §3; risk R-015 | — |
| 8.2 | Privileged access rights | Yes | Implemented | Admin and SuperAdmin require TOTP, enforced per request; privileged grants are audited by the change interceptor | AC-27, AC-51 |
| 8.3 | Information access restriction | Yes | Implemented | Permission resources per data category; tenant isolation at query and connection level | AC-28, AC-29, AC-31 |
| 8.4 | Access to source code | Yes | Implemented | Private repository, account with MFA; release credentials in the password manager | — |
| 8.5 | Secure authentication | Yes | Implemented | Argon2id, RS256, lockout, MFA, session binding, refresh-token rotation | AC-26, AC-27 |
| 8.6 | Capacity management | Yes | Implemented | Kestrel limits, rate limiting, health and readiness checks, capacity guidance in `ADMINISTRATION.md` | AC-54 |
| 8.7 | Protection against malware | Yes | **Partial** | Upload validation with signature checking; Trivy on images; OS defender on the workstation. No server-side AV scan of uploaded files (documents are stored, never executed) | AC-32 |
| 8.8 | Management of technical vulnerabilities | Yes | Implemented | `12-vulnerability-register.md`, CI gates, Dependabot, documented windows | AC-13, AC-55 |
| 8.9 | Configuration management | Yes | **Partial** | Compose and Kubernetes manifests in version control; digest-pinned images; schema drift check at startup. No host baseline document | AC-33 |
| 8.10 | Information deletion | Yes | Implemented | `09-retention-policy.md` with the enforcing mechanism per category; erasure reaches files as well as rows | AC-38 |
| 8.11 | Data masking | Yes | Implemented | Log destructuring policy masks sensitive properties; the audit trail records identity fields as changed without copying them; DSAR export is scoped to the subject | AC-42, AC-51 |
| 8.12 | Data leakage prevention | Yes | **Partial** | Bulk read and export alerting, access audit, tenant isolation, metrics closed. No egress filtering on a client's host — that is theirs | AC-54 |
| 8.13 | Information backup | Yes | Implemented | Encrypted, verified, retained, restore proven in CI and on the pilot database | AC-36, AC-37, AC-70 |
| 8.14 | Redundancy of information processing facilities | Yes | **Partial** | Two application replicas behind the ingress; read replicas supported. Database redundancy is the client's choice | AC-71 |
| 8.15 | Logging | Yes | Implemented | Access audit, write audit, login audit, structured application logs with masking | AC-50, AC-51, AC-52 |
| 8.16 | Monitoring activities | Yes | Implemented | Security alerts recorded, logged and emailed; bulk-access worker; health and readiness; Prometheus metrics | AC-54 |
| 8.17 | Clock synchronization | Yes | Implemented | Containers take host NTP time; all timestamps stored in UTC with a UTC convention applied in the model | — |
| 8.18 | Use of privileged utility programs | Yes | Implemented | Operator commands are explicit and enumerated (`migrate`, `rotate-jwt-keys`, `encrypt-fields`); the container runs the application, not a shell, for normal operation | — |
| 8.19 | Installation of software on operational systems | Yes | Implemented | Immutable images; compose refuses `latest`; auto-update behind an opt-in profile | AC-33 |
| 8.20 | Networks security | Yes | **Partial** | Internal Docker networks, TLS terminated at the client's ingress, CORS restricted, rate limiting. Internal traffic plaintext (R-018) | AC-22 |
| 8.21 | Security of network services | Yes | Implemented | Only the ingress is published; database and Redis stay on the internal network; metrics closed | AC-33 |
| 8.22 | Segregation of networks | Yes | Implemented | `edge` and `backend` networks in compose; the sidecar is not published | — |
| 8.23 | Web filtering | No | N/A | The product does not provide user web access, and there is no corporate network to filter | — |
| 8.24 | Use of cryptography | Yes | Implemented | AES-256-GCM columns and vault, Argon2id, RS256, TLS at the ingress, PQC signatures on finance artefacts, OpenSSL-compatible backup encryption. Key management is **partial** — no master-key rotation (R-002) | AC-20, AC-23, AC-24 |
| 8.25 | Secure development life cycle | Yes | **Partial** | CI gates, tests, migrations checked on four dialects, accessibility checks. No second reviewer (R-014) | AC-34 |
| 8.26 | Application security requirements | Yes | Implemented | The control set in `04` is the requirement list; OWASP ASVS mapping in `docs/06` §5 where it is accurate | AC-32 |
| 8.27 | Secure system architecture and engineering principles | Yes | Implemented | Default deny, least privilege by data category, tenant isolation by construction, secrets by reference not value | AC-28, AC-29 |
| 8.28 | Secure coding | Yes | Implemented | Parameterised access through EF Core, output encoding by the framework, validation on input, nullable reference types, warnings-as-errors in the verifier project | AC-32 |
| 8.29 | Security testing in development and acceptance | Yes | Implemented | 800 tests including the security and privacy suites; abuse harness (`tests/run-all.sh`); restore drill; dependency and image scanning in CI | AC-34 |
| 8.30 | Outsourced development | No | N/A | No development is outsourced. Becomes applicable on the first contractor | — |
| 8.31 | Separation of development, test and production environments | Yes | Implemented | Throwaway test databases, demo mode refused in Production, pilot environments hold synthetic data only | — |
| 8.32 | Change management | Yes | Implemented | Version control, migrations with a startup policy that refuses rather than guesses, release notes, rollback script | AC-33 |
| 8.33 | Test information | Yes | Implemented | Tests use generated data; the demo seeder is synthetic; no client personal data is copied into development | — |
| 8.34 | Protection of information systems during audit testing | Yes | Implemented | Audits read this repository and read-only evidence; any test against a client installation needs the client's written agreement under DPA §7 | — |

---

## Summary

| Theme | Controls | Applicable | Implemented | Partial | Planned | N/A |
|---|---|---|---|---|---|---|
| A.5 Organizational | 37 | 37 | 26 | 11 | 0 | 0 (1 retained-inactive) |
| A.6 People | 8 | 8 | 4 | 0 | 0 | 4 (until first hire) |
| A.7 Physical | 14 | 14 | 5 | 0 | 1 | 8 |
| A.8 Technological | 34 | 32 | 24 | 8 | 0 | 2 |
| **Total** | **93** | **91** | **59** | **19** | **1** | **14** |

One control remains Planned (7.14, secure disposal — no device has been disposed of yet). Two controls are excluded as not applicable to the organisation's activities (8.23 web filtering, 8.30 outsourced development); the physical exclusions follow from having no premises, and each is justified individually above rather than as a block. Twelve controls are marked N/A *until an event* (first hire, first disposal) and their procedures are written in advance so the ISMS does not have a hole on the day the event happens.

**Honest position for a certification body:** 59 of 91 applicable controls carry evidence today. The 19 partials each name what is missing. Nothing here is claimed as implemented on the strength of a document alone — every "Implemented" points at code, a test, a CI job, or a dated exercise record.
