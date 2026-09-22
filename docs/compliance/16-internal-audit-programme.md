# 16 — Internal audit programme

**Version 1.0 · 2026-09-22.** ISO/IEC 27001:2022 clause 9.2; control AC-34, A.5.35.

## 1. Purpose and the independence problem, stated plainly

Clause 9.2 requires auditors to be objective and impartial and not to audit their own work. In a one-person company that is impossible in the ordinary sense, and an auditor will ask about it. The programme handles it in three ways, in ascending order of strength:

1. **Evidence over assertion.** Every check below is answered by running a command or opening a file, not by the auditor recalling what they built. A check whose answer is "yes, I remember doing that" is recorded as a finding.
2. **A fixed checklist written before the audit.** The checks are in §4 and change only by a dated amendment, so the audit cannot quietly narrow to what passes.
3. **External eyes where it matters most.** The Stage 1 documentation review by the certification body (Phase 6) is the first genuinely independent review; until then, any finding rated Major is re-checked by a second party — a contracted consultant or a client's IT auditor — before it is closed. This is recorded honestly as a limitation, not presented as equivalent to independence.

## 2. Scope and cycle

Everything in `13-isms-scope.md` §2, and all 91 applicable controls in `15`, over a 12-month cycle:

| Quarter | Focus | Controls |
|---|---|---|
| Q1 (Oct–Dec 2026) | Access, identity, cryptography | A.5.15–5.18, A.8.2–8.5, 8.24 |
| Q2 (Jan–Mar 2027) | Logging, monitoring, incident response; second tabletop | A.5.24–5.28, A.8.15, 8.16 |
| Q3 (Apr–Jun 2027) | Data protection, retention, erasure, suppliers | A.5.12–5.14, 5.19–5.23, 5.33, 5.34, A.8.10–8.13 |
| Q4 (Jul–Sep 2027) | Development, change, continuity, backup restore | A.5.29, 5.30, A.8.8, 8.25–8.34 |

Plus a full-scope audit against the whole SoA immediately before engaging the certification body (Phase 5).

Out-of-cycle audits are triggered by: a Critical or High risk materialising, an S1 incident, a failed restore drill, or a material architecture change.

## 3. Method

- **Inputs:** `15` (the control list), `14` (risks), `12` (vulnerabilities), `04` (the unified set), CI run history, the exercise and incident records.
- **Techniques:** inspection of code and configuration; re-running the automated evidence (§4); sampling records (audit rows, consent rows, alerts); walkthroughs of a procedure end to end; interview is not available and is replaced by evidence.
- **Sampling:** where a control produces many records, examine 10 or at least 5, chosen by the auditor, not by convenience.
- **Finding grades:** **Major** (a control in the SoA is absent or ineffective, or a claim is unsupported), **Minor** (control works but with a gap), **Observation** (improvement, no nonconformity).
- **Record:** one file per audit in `docs/compliance/audits/<yyyy-mm-dd>-<scope>.md` using the template in §5. Findings go to `14` (risk) or `04` (control) with an owner and a date; closure requires evidence, not an assertion.

## 4. The evidence commands

An audit begins by running these. If a command fails or the output disagrees with the SoA, that is a finding before anything else is examined.

| # | Check | Command / evidence | Expected |
|---|---|---|---|
| E-1 | The suite passes | `dotnet test AegisCore.Tests/AegisCore.Tests.csproj -c Release` | 0 failed |
| E-2 | Tenant isolation holds | same, filter `TenantIsolationTest` | pass |
| E-3 | No vulnerable dependency ships | `dotnet list AegisCore.slnx package --vulnerable --include-transitive`; `npm audit --omit=dev --audit-level=high` in Portal and Mobile | no vulnerable packages; any exception recorded in `12` with a review date |
| E-4 | A backup restores | `deploy/scripts/backup-restore-drill.sh` against a copy | PASS, table count and marker match |
| E-5 | Backups are encrypted and no plaintext dump survives | inspect the drill output; `head -c 8` of an archive | `Salted__`; no `*.dump` left |
| E-6 | Administrators cannot work without a second factor | `MfaTests` | pass |
| E-7 | Sensitive writes are audited | `SensitiveChangeAuditTests`; sample 10 `AuditLogs` rows from a live installation | rows present, identity values absent |
| E-8 | Sensitive reads are audited | `AccessAuditTests`; sample 10 `FerpaAuditLogs` rows | rows present, IP encrypted |
| E-9 | Identifiers are ciphertext at rest | `FieldEncryptionTests`; `select "Cnic" from "AdmissionApplications" limit 5` on a live installation | every value prefixed `enc1d:`/`enc1r:` |
| E-10 | Least privilege by data category | `PermissionMatrixTests` | pass |
| E-11 | Erasure reaches everything `09` says | `ErasureScopeTests`; walk one real request end to end | pass; no residue |
| E-12 | Consent text is preserved verbatim | `ApplicantPortalTests`; sample 5 `ConsentLedgers` rows | text matches the version shown |
| E-13 | Alerts reach a person | `SecurityAlertTests`; sample `SecurityAlerts` and confirm the email route | rows present; email sent for High |
| E-14 | Metrics are closed | `curl -s -o /dev/null -w '%{http_code}' https://<host>/metrics` from outside | 404 |
| E-15 | The legal register is current | `00-PROGRAM.md` date | checked within the last quarter |
| E-16 | The workstation controls hold | inspect: full-disk encryption on, screen lock, OS updates current, password manager MFA on | all yes |
| E-17 | No unsupported claim is in circulation | grep the marketing and docs for "certified", "GDPR compliant", "PDPA compliant" | only the approved wording in `00-PROGRAM.md` |

## 5. Audit record template

```
# Internal audit — <scope> — <date>
Auditor: <name>   Independence limitation: <state it>
Scope and criteria: <SoA controls, documents>
Evidence run: E-1…E-17 results (attach output)
Samples examined: <what, how many, how chosen>
Findings:
  | # | Grade | Control | What was found | Evidence | Owner | Due |
Conclusion: <is the ISMS conforming and effective in this scope?>
Follow-up on previous findings: <closed with evidence / still open>
```

## 6. Reporting

Every audit report goes to the management review (`17`) with its findings and their status. An open Major finding is reported to the management review every time until it is closed with evidence.
