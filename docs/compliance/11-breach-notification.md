# 11 — Security incident and personal-data breach procedure

**Version 1.0 · 2026-09-20.** Controls AC-60, AC-61 (ISO A.5.24–5.28; PECA reporting; PDPA-draft/NDGP breach duties as expected; GDPR 33/34 pattern). Supersedes `docs/15-operations-maintenance-guide.md` §7 for security incidents. The tabletop exercise that tests it is Phase 3 item 12 and its record lives in `docs/compliance/exercises/`.

## 1. Definitions

- **Security incident:** any event that compromises or credibly threatens the confidentiality, integrity or availability of AegisCore or of data it holds — including a vulnerability report that is exploitable in a shipped release.
- **Personal-data breach:** an incident that leads to accidental or unlawful destruction, loss, alteration, unauthorised disclosure of, or access to, personal data.
- **Aware:** the moment a Nocturne person or a University administrator has reasonable certainty that a breach has occurred. The clock starts then, not at confirmation of every detail.

## 2. Roles

| Role | Model A (University-hosted) | Model B / Nocturne systems |
|---|---|---|
| Incident lead | University IT head | Nocturne DPC (`05-governance.md`) |
| Software support | Nocturne DPC | — |
| Controller decisions (notify subjects/authorities) | University | University (Nocturne assists) |
| Record keeper | incident lead | incident lead |

## 3. Severity

| Sev | Definition | Examples |
|---|---|---|
| S1 | Confirmed exposure or loss of personal data, or ledger integrity broken | database exfiltration; unauthorised export of transcripts; audit seal verification fails |
| S2 | Credible threat, contained | exploitable vulnerability in a shipped release, no evidence of use; lost device with cached data |
| S3 | Security weakness, no data effect | misconfiguration found in review; failed backup restore drill |

## 4. Timeline — the clock

| Elapsed since aware | Action | Who |
|---|---|---|
| 0 h | Open an incident record (§7). Contain: revoke credentials, rotate keys (`rotate-jwt-keys`; vault re-key if `AEGISCORE_PQC_KEY_BASE64` is suspected), isolate the host, preserve logs (`LoginAuditRecords`, `FerpaAuditLogs`, `AuditLogs`, ingress logs, Serilog) — copy, do not truncate | incident lead |
| ≤ 4 h | Severity assigned; DPC informed if not already lead | incident lead |
| ≤ 24 h | Preliminary assessment: what data, whose, how many, how, still ongoing? | incident lead + support |
| **≤ 48 h** | **Notify the other party under DPA §8** (Nocturne ↔ University), even if incomplete | whichever party is aware |
| **≤ 72 h** | **Internal escalation deadline** (GDPR-style discipline adopted as policy): the Controller has decided whether to notify authorities and data subjects, and has drafted the notices | University, assisted by Nocturne |
| ≤ 72 h | If a criminal act is suspected (unauthorised access, data theft — PECA ss. 3–5, 21): report to the **National Cyber Crime Investigation Agency** (NCCIA, successor to FIA Cyber Crime Wing under the 2025 amendment) and, where a PDA/NDGP duty exists after gazette, to the PDA | University (Nocturne files for its own systems) |
| ≤ 7 d | Data subjects notified where the breach is likely to cause them harm (identity documents, financial, welfare data): what happened, what data, what we did, what they should do, contact point | University |
| ≤ 7 d | HEC informed for public universities where student records are involved (institutional reporting line; no fixed statutory window — treat 7 d as policy) | University |
| ≤ 30 d | Post-incident review (§8); fixes scheduled; risk register updated | DPC |

Where no legal duty to notify exists today, the policy still is to notify the University within 48 h and to let the University decide on subjects. Silence is not an option under this procedure.

## 5. Notification content (template)

```
Subject: [S1/S2] Security incident affecting <installation> — notice under DPA §8

1. What happened (plain language, one paragraph)
2. When it started / when we became aware
3. Data affected: categories (per 01-data-inventory), approximate number of people and records
4. Likely consequences for the people affected
5. What has been done: containment, keys rotated, patches
6. What we recommend the University does now
7. What we do not yet know, and when the next update comes
8. Contact: name, phone, email (DPA Schedule 6)
```

## 6. Vulnerability reports from outside (`SECURITY.md`)

A report received through the disclosure channel is triaged within 2 business days; a reproducible high/critical issue in a shipped release is an S2 incident and follows this procedure; a fix is released with an advisory that names affected versions; the reporter is credited with consent. Reports are kept 3 years (`08-ropa.md` N4).

## 7. Incident record (kept in `docs/compliance/incidents/<yyyy-mm-dd>-<slug>.md`, one per incident; never deleted)

| Field | |
|---|---|
| Id, title, severity | |
| Aware at (UTC), reported by | |
| Timeline of actions with timestamps | |
| Data affected (categories, counts, tenants) | |
| Root cause | |
| Notifications sent (to whom, when, copy attached) | |
| Evidence preserved (paths, hashes) | |
| Closure date, reviewer | |

## 8. Post-incident review

Within 30 days: blameless write-up; actions with owners and dates; risk register and SoA updated; this procedure amended if it failed. Reviewed at the next management review.

## 9. Testing

A tabletop exercise is run at least annually and after any material change to the architecture. First exercise: Phase 3 item 12 (scenario: leaked `generated-secrets.env` from a university backup bucket). The record of each exercise, with what went wrong, is kept in `docs/compliance/exercises/`.
