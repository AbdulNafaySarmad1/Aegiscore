# 14 — Risk assessment methodology and risk register

**Version 1.0 · 2026-09-22.** ISO/IEC 27001:2022 clauses 6.1.2, 6.1.3, 8.2, 8.3; control AC-05. Supersedes `docs/14-risk-assessment.md` (a 2025 project-risk list, not a security risk assessment) for information-security purposes.

## 1. Method

**Scope of assessment:** the assets and activities in `13-isms-scope.md` §2.

**Identification.** A risk is stated as *threat × vulnerability × asset → consequence*, sourced from: the STRIDE model in `docs/06-security-architecture-document.md` §3, the findings in `01-data-inventory.md` and `02-data-flow-map.md`, the gaps in `04-nist-csf-gap-assessment.md`, exercise findings (`exercises/`), the vulnerability register (`12`), and incidents.

**Owner.** Every risk has one named owner accountable for the treatment decision. In a one-person company that is the DPC for all of them; the column stays because it must be filled in correctly the day a second person joins.

**Analysis.** Likelihood and impact are each scored 1–5. The score is their product (1–25). Both scales are defined here so two assessments a year apart are comparable.

| Score | Likelihood (in a 12-month window) | Impact |
|---|---|---|
| 1 | Would be surprising (< 2 %) | Negligible: no personal data, no client-visible effect |
| 2 | Unlikely (2–10 %) | Minor: one client inconvenienced, no personal data exposed |
| 3 | Possible (10–35 %) | Moderate: a client's operations disrupted, or a small number of records exposed internally |
| 4 | Likely (35–70 %) | Major: personal data of many people exposed; notification required; a client could terminate |
| 5 | Near certain (> 70 %) | Severe: mass exposure of identity documents or welfare records; regulatory action; the company's ability to sell ends |

**Acceptance criteria.**

| Band | Score | Rule |
|---|---|---|
| Critical | 20–25 | Not acceptable. Treat immediately; the product does not ship with an open critical risk |
| High | 12–19 | Not acceptable beyond 90 days. Treatment plan with a date |
| Medium | 6–11 | Acceptable for up to 12 months with a treatment plan |
| Low | 1–5 | Acceptable. Reviewed annually |

**Treatment options:** modify (add or strengthen a control), avoid (stop doing the thing), share (contract, insurance), retain (accept, with the owner's explicit sign-off recorded here).

**Residual risk** is re-scored after treatment. A retained residual risk above Medium requires the owner's written acceptance in this document — there is no implicit acceptance.

**Cadence.** Full reassessment annually and at each management review (`17`); an individual risk is re-scored whenever an incident, exercise or audit touches it.

## 2. Risk register

Status as at 2026-09-22. "Initial" is the score before the Phase 3 work; "residual" is after it, so the register shows what the work bought.

| Id | Risk | Asset | Owner | Initial (L×I) | Treatment | Residual | Accepted? |
|---|---|---|---|---|---|---|---|
| R-001 | An attacker obtains a database backup and reads every student and staff record, because backups were written unencrypted | S-1, O-1 data | DPC | 4×5 = **20 Critical** | Modified: encryption unconditional, key required before `pg_dump` runs, restore drill in CI (AC-36/37/70) | 2×5 = **10 Medium** | Yes — residual is the case where the key leaks with the archive (R-002) |
| R-002 | The master key `AEGISCORE_PQC_KEY_BASE64` leaks and cannot be rotated, exposing the vault, audit IPs and every encrypted column | S-4, O-1 data | DPC | 2×5 = **10 Medium** | Modify: build `rotate-master-key` (R-02 in `04`), due 2026-12-22. Interim: key lives only in the config volume, never in the repository or an image | 2×5 = **10 Medium** until built | Retained to 2026-12-22 with the interim control |
| R-003 | A shipped release carries a known-exploitable dependency because nobody looks | S-2 | DPC | 5×4 = **20 Critical** | Modified: audit on every restore, CI gates on NuGet/npm/Trivy, Dependabot, documented triage windows (AC-13/55/62) | 2×4 = **8 Medium** | Yes |
| R-004 | An administrator's password is phished and the account opens every record | O-1 data | DPC | 4×5 = **20 Critical** | Modified: TOTP required for Admin/SuperAdmin, enforced per request, brute-force lockout, single-use backup codes (AC-27) | 2×4 = **8 Medium** | Yes |
| R-005 | Someone reads or exports student records at scale and nobody notices | O-1 data | DPC | 4×4 = **16 High** | Modified: read audit extended, write audit on 24 entity types, bulk-access alerting (AC-50/51/54) | 2×3 = **6 Medium** | Yes |
| R-006 | A staff member with an unrelated operations permission reads salaries or counselling notes | O-1 data | DPC | 4×4 = **16 High** | Modified: Staff/Payroll/Welfare split out of `System`, matrix test (AC-29) | 2×3 = **6 Medium** | Yes |
| R-007 | Identity documents (CNIC, passport) and welfare notes sit in plaintext columns; anyone with database access reads them | O-1 data | DPC | 4×5 = **20 Critical** | Modified: column encryption under the master key with deterministic nonces for looked-up identifiers (AC-20) | 2×4 = **8 Medium** | Yes — date of birth and salary remain (R-05 in `04`) |
| R-008 | An erasure request is answered but the person's records remain in welfare notes, documents, travel records and sign-in logs | O-1 data | DPC | 5×3 = **15 High** | Modified: erasure scope extended, files deleted from storage, re-queue after a restore (AC-38) | 2×3 = **6 Medium** | Yes |
| R-009 | A minor's application is processed with no consent from anyone able to give it | O-1 data | DPC | 4×3 = **12 High** | Modified: age gate, guardian block, two ledger rows, guardian emailed (AC-39/40) | 1×3 = **3 Low** | Yes |
| R-010 | Every sign-in from a public address sends the person's IP to a US service nobody disclosed | O-1 data | DPC | 5×3 = **15 High** | Share + modify: disclosed in the privacy notice and DPA Schedule 4 now; make it opt-in or offline (R-01 in `04`), due 2026-11-22 | 3×3 = **9 Medium** until changed | Retained to 2026-11-22 |
| R-011 | The metrics feed tells the internet which tenants exist and how busy they are | S-3, O-1 | DPC | 4×2 = **8 Medium** | Modified: token, session or private-network only; tenant label opt-in (AC-33) | 1×2 = **2 Low** | Yes |
| R-012 | A breach happens and the company misses a notification window or tells the wrong body | S-4 | DPC | 3×4 = **12 High** | Modified: procedure with a clock, templates and routes; tabletop run (AC-60/61) | 2×3 = **6 Medium** | Yes — contacts still placeholders (R-04 in `04`) |
| R-013 | The single maintainer is unavailable and no one else can release a security fix | S-1, S-2 | DPC | 3×4 = **12 High** | Retain with compensating controls: release procedure documented, keys escrowed in the password manager, client contact list maintained. Real fix needs a second person | 3×4 = **12 High** | **Retained — accepted by the DPC 2026-09-22.** This is the company's largest open risk and is stated plainly to clients rather than hidden |
| R-014 | A malicious or careless change reaches `main` without a second pair of eyes | S-1, S-2 | DPC | 3×4 = **12 High** | Modify: sensitive-path PR checklist, 24-hour soak, CI gates (AC-03). Compensating, not equivalent to review | 3×3 = **9 Medium** | Retained until a second reviewer exists |
| R-015 | The developer workstation is compromised and the source, keys and client contacts go with it | S-4, S-5 | DPC | 3×5 = **15 High** | Modify: full-disk encryption, screen lock, OS and browser auto-update, password manager with MFA, no client personal data held locally (DPA §7.3). Verify at the internal audit | 2×5 = **10 Medium** | Yes |
| R-016 | A client's own installation is breached through the university's infrastructure and AegisCore is blamed | O-1 | DPC | 4×3 = **12 High** | Share: DPA §3 assigns the controls; installation guide states them; breach procedure covers the direction | 3×3 = **9 Medium** | Yes |
| R-017 | The National Data Governance Policy is notified and the product cannot meet a data-sovereignty or disposal requirement in the 12-month window | S-1 | DPC | 3×3 = **9 Medium** | Modify: monthly tracking (`00-PROGRAM.md`), residency decision already taken (D-A), disposal implemented (AC-38) | 2×3 = **6 Medium** | Yes |
| R-018 | Internal traffic between the ingress, application and sidecar is plaintext on the Docker network | O-1 data | DPC | 2×3 = **6 Medium** | Retain: the network is host-local; an attacker at that level has the database anyway. Revisit for multi-host deployments (R-09 in `04`) | 2×3 = **6 Medium** | Yes |
| R-019 | The Python sidecar's dependencies have never been audited | S-1 | DPC | 3×3 = **9 Medium** | Modify: Dependabot pip enabled; add `pip-audit` to CI (V-007), due 2026-10-22 | 2×3 = **6 Medium** | Retained to 2026-10-22 |
| R-020 | A client is sold on a compliance claim the company cannot evidence | S-4 | DPC | 3×4 = **12 High** | Modify: the claims table in `00-PROGRAM.md` is the only approved wording; the 2025 architecture document is marked superseded where it overstates | 1×4 = **4 Low** | Yes |

## 3. Summary

| Band | Initial | Residual |
|---|---|---|
| Critical (20–25) | 5 | 0 |
| High (12–19) | 9 | 1 |
| Medium (6–11) | 6 | 17 |
| Low (1–5) | 0 | 2 |

One High remains: **R-013, the single maintainer**, retained with the owner's explicit acceptance because no control the company can implement removes it — only hiring does. It is disclosed here rather than scored down to look better.

## 4. Risk treatment plan (clause 6.1.3 e)

| Risk | Action | Owner | Due |
|---|---|---|---|
| R-002 | `rotate-master-key` command | DPC | 2026-12-22 |
| R-010 | GeoIP opt-in or offline database | DPC | 2026-11-22 |
| R-012 | Confirm NCCIA and PDA contacts | DPC | 2026-10-22 |
| R-019 | `pip-audit` in CI | DPC | 2026-10-22 |
| R-014 / R-013 | Second reviewer when headcount allows | DPC | Reviewed each management review |
