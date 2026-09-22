# Internal audit — full scope, against the Statement of Applicability — 2026-09-22

**Auditor:** the DPC. **Independence limitation:** the auditor built the controls being audited. Per `16-internal-audit-programme.md` §1 this is mitigated by running commands rather than relying on recollection, by a checklist fixed before the audit (`16` §4), and by the rule that any Major finding is re-checked by a second party before closure. It is **not** equivalent to independent audit, and the Stage 1 review by a certification body remains the first genuinely independent look.

**Criteria:** `15-statement-of-applicability.md` (91 applicable Annex A controls), `14` (risk register), `13` (scope).
**Method:** evidence commands E-1 … E-17 from `16` §4, plus inspection.

---

## 1. Evidence run

| # | Check | Result | Note |
|---|---|---|---|
| E-1 | Full test suite | **FAIL — 3 of 803** | 790 passed, 10 skipped. See findings A-01 and A-02. Every compliance-control suite passed (below) |
| E-2 | Tenant isolation | PASS | 1/1 |
| E-3 | No vulnerable dependency ships | **PASS with recorded exception** | NuGet: none. Portal npm: 0 of any severity. Mobile npm: 4 high, all in the Expo/Metro **development** bundler, registered and accepted as V-006 with review date 2026-12-20. Not in the shipped app |
| E-4 | A backup restores | **NOT RUN** | Finding A-03: Docker Desktop was not running on the audit host. Last evidence: 2026-09-20 drill (576 tables, marker restored, 13 s) and the CI gate on every push |
| E-5 | Backups encrypted, no plaintext survives | **NOT RUN** | As E-4; covered by `BackupCipherTests` (PASS) which proves format, refusal without a key, and that no dump is left when the key is missing |
| E-6 | Administrators need a second factor | PASS | `MfaTests` 2/2 |
| E-7 | Sensitive writes audited | PASS | `SensitiveChangeAuditTests` 3/3. Live-row sampling not performed — no live installation available to this audit (A-04) |
| E-8 | Sensitive reads audited | PASS | `AccessAuditTests` 1/1; same sampling limitation |
| E-9 | Identifiers ciphertext at rest | PASS | `FieldEncryptionTests` 4/4, which asserts on the raw column value, not on the entity; same live-sampling limitation |
| E-10 | Least privilege by data category | PASS | `PermissionMatrixTests` 4/4 |
| E-11 | Erasure reaches everything `09` promises | PASS | `ErasureScopeTests` 3/3, `PrivacyComplianceTests` 4/4 |
| E-12 | Consent text preserved verbatim | PASS | `ApplicantPortalTests` 14/14 |
| E-13 | Alerts reach a person | PASS | `SecurityAlertTests` 2/2 |
| E-14 | Metrics closed from outside | **NOT RUN** | No live host reachable from this audit; `MetricsAccessTests` (PASS) covers the logic including the public-address refusal |
| E-15 | Legal register current | PASS | Re-checked 2026-09-22, within the quarter |
| E-16 | Workstation controls | **UNVERIFIED** | Finding A-05: the auditor cannot evidence their own device's configuration in a way another party could rely on |
| E-17 | No unsupported claim in circulation | **PASS with a Minor** | No occurrence of "certified", "GDPR compliant", "PDPA compliant" or "fully compliant" anywhere outside the compliance folder's own prohibition table. But `docs/06` presented unimplemented controls as fact — finding A-06, **remediated during this audit** |

**Provider coverage note.** The PostgreSQL leg of the test theories could not run (Docker down), so the results above are the SQLite leg. The PostgreSQL leg ran green for every one of these suites on 2026-09-20 and runs in CI's migration matrix job. Recorded rather than glossed.

## 2. Findings

| # | Grade | Control | Finding | Evidence | Owner | Due |
|---|---|---|---|---|---|---|
| A-01 | **Minor** (remediated) | A.8.29 | `ControllerWiringTests` (×2) fail on this host: the OS Application Control policy blocks `Google.Cloud.SecretManager.V1.dll` from loading, so the reflection-based tests cannot enumerate types. Environmental, not a product defect — the same tests pass in CI — but it means a developer on a locked-down machine gets a red suite and may learn to ignore it | `ReflectionTypeLoadException … 0x800711C7` | DPC | **Addressed 2026-09-22** — xunit v2 cannot skip at runtime, so the tests now fail with a message naming the OS policy as the cause and stating that CI covers them. Still red on such a machine, which is the honest outcome; verified by running them |
| A-02 | **Minor** | A.5.36, A.8.25 | `DynamicRbacTests.All_controllers_use_dynamic_permissions_without_legacy_role_literals` fails: one controller in uncommitted work by another contributor reintroduces `[Authorize(Roles = "Faculty,Admin,SuperAdmin")]`, which the repository's own gate forbids. The gate worked — it caught it before merge. Confirmed not present at `HEAD` | `git show HEAD:…AttendanceController.cs` → 0 occurrences; working tree → 1 | The contributor, tracked by DPC | Before that work merges |
| A-03 | **Observation** | A.8.13 | The restore drill could not be run during this audit because the container runtime was down. The control is evidenced by CI and by the 2026-09-20 drill, but an audit that cannot run its most important check on demand is weaker than one that can | — | DPC | Next audit: ensure the runtime is up before starting |
| A-04 | **Observation** | A.8.15 | Audit-row and consent-row sampling against a live installation was not possible (no live installation available to Nocturne — by design, `13` §3 O-1). Sampling will have to be done with a client's cooperation or on a seeded reference installation | — | DPC | Build a reference installation for Q1 2027 audit |
| A-05 | **Minor** | A.6.7, A.8.1 | Workstation controls (E-16) are self-asserted; `18` §5 already flags this. No independent evidence exists | `18` §5 | DPC | Q1 2027 — produce screenshot/config evidence retained with the audit record |
| A-06 | **Major** | A.5.1, A.5.36 | `docs/06-security-architecture-document.md` stated six controls as implemented that were not (column encryption for all PII, SBOM per build, weekly dependency scans, admin MFA, RTO/RPO figures, append-only audit logs). It is a client-facing document. This is the exact failure mode the programme exists to prevent | Phase 1 gap assessment; the document itself | DPC | **Closed during this audit** — a superseded banner naming each false statement was added at the head of the document, pointing to `04` and `15`. Per `16` §1 a Major finding needs second-party re-check before final closure: **open pending that re-check** |
| A-07 | **Minor** | A.5.1 | The top-level approved information-security policy (A.5.1) does not exist; the policy set is a folder of documents without a governing statement | `15` A.5.1 = Planned | DPC | **Closed 2026-09-22** — `19-information-security-policy.md` issued and approved; A.5.1 and A.5.10 now Implemented |

No finding contradicts a control marked **Implemented** in the SoA, with the single exception of A-06, which concerned an older document rather than a control.

## 3. Conclusion

Within the scope of `13` and the limitations stated above, the ISMS **conforms** to the documented control set and is **operating**, with one Major finding (A-06, remediated but pending second-party re-check), four Minor and two Observations.

The ISMS is **not yet certification-ready**. Blocking items:

1. A-06 must be closed by a second party (`16` §1).
2. A.7.14 (disposal procedure) is Planned, not Implemented. A.5.1 was closed during this audit by issuing `19-information-security-policy.md`.
3. 19 controls remain Partial; each names its remainder, which an auditor may accept or may raise.
4. Clause 9.2's independence expectation is not met and cannot be met by this company today. A certification body will form its own view; the honest presentation is the one in `16` §1, not a claim of independence.

## 4. Follow-up on previous findings

None — first audit. The four tabletop findings (2026-09-22) are tracked in `04` as R-02 … R-04 and remain open within their dates.
