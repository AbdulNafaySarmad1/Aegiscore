# Tabletop exercise — leaked `generated-secrets.env` from a university backup bucket

**Date:** 2026-09-22 · **Control:** AC-60, AC-61 (ISO A.5.24–5.27; NIST RS) · **Type:** discussion-based tabletop, single participant (the DPC; the company has one person — recorded honestly rather than staged as a team drill) · **Duration:** 50 minutes · **Procedure under test:** `docs/compliance/11-breach-notification.md`.

## Scenario

A university's IT administrator emails at 09:10 PKT: their S3 backup bucket was listable in public for an unknown period, discovered when a search-engine cache entry for `aegiscore-20260915-021500.dump.enc` was reported by a student. The bucket also holds `config/generated-secrets.env`, copied there by a local sysadmin script that predates AegisCore's own backup tooling. The university has ~18,000 student records, ~1,400 staff.

## What the procedure said to do, and what actually happened when we tried it

| Clock | Step per `11-breach-notification.md` | Outcome in the exercise |
|---|---|---|
| 0 h | Open an incident record; contain | **Worked.** Record template used. Containment steps identified: bucket made private, keys rotated. |
| 0 h | Rotate keys | **Partly blocked — finding F-1.** `rotate-jwt-keys` exists. There is no command to rotate `AEGISCORE_PQC_KEY_BASE64`, which encrypts the secret vault, the audit IPs and (since this release) the CNIC, passport, welfare and sign-in columns. The only honest containment for a leaked master key today is: restore to a new installation with a new key and re-import, or accept the exposure. |
| ≤ 4 h | Severity | S1. Straightforward. |
| ≤ 24 h | Preliminary assessment: what data, whose, how many | **Worked, with effort.** `01-data-inventory.md` answered "what is in a dump" in about three minutes. Counting affected people needed a manual query; **finding F-2**: no "how many data subjects are in this installation" report exists. |
| ≤ 48 h | Notify the other party (DPA §8) | **Worked.** Template usable as written. Note: in this scenario Nocturne is *not* the breaching party — the university's own script copied the secrets — so the notification runs university → Nocturne, and the DPA covers that direction too. |
| ≤ 72 h | Controller decides on notification | **Worked as a decision aid.** The backups are encrypted (`backup.sh` refuses to write plaintext since 2026-09-20), so the exposure hinges entirely on whether the leaked key file and the dumps were exposed in the same window. |
| ≤ 72 h | NCCIA report where a criminal act is suspected | **Finding F-3.** No contact details for the NCCIA are recorded anywhere. Nobody should be looking up a phone number at hour 70. |
| ≤ 7 d | Notify data subjects | Reviewed the template. Adequate. |
| ≤ 30 d | Post-incident review | Not exercised (out of scope for a tabletop). |

## Findings

| # | Finding | Severity | Action | Owner / due |
|---|---|---|---|---|
| F-1 | No rotation path for `AEGISCORE_PQC_KEY_BASE64`. A leaked master key cannot be rotated in place; every encrypted column, the vault and the audit IPs are encrypted under it. | **High** | Build `rotate-master-key`: re-encrypt vault, `FerpaAuditLogs`/`ConsentLedger` IPs and every `FieldEncryptionModel` column under a new key, with a resumable batch and a dry run. Tracked as AC-23 in `04`. | DPC, due 2026-12-22 |
| F-2 | No report of how many data subjects an installation holds, by category. Needed within 24 h of any breach. | Medium | Add a counts view to the compliance screen (students, staff, applicants, guardians, visitors — no names). | DPC, due 2026-11-22 |
| F-3 | No regulator contact details on file (NCCIA, PDA, HEC). | Low, trivial to fix | Add a contacts block to `11-breach-notification.md` once numbers are confirmed from official sources. | DPC, due 2026-10-22 |
| F-4 | The procedure assumes the *vendor* detects or causes the breach. Half of realistic scenarios start at the university. | Low | Clarified in `11` §2 that either party may be the discovering party; no structural change needed. | Done in this exercise |

## What the exercise confirmed works

- The encrypted-backup change made three weeks earlier is what keeps this scenario from being an automatic mass notification: a listable bucket of `Salted__` archives without the key is not a disclosure of student records.
- `01-data-inventory.md` answered the "what data" question fast enough to be useful under time pressure, which is the whole point of having written it.

## Next exercise

Scheduled for 2027-03 (six-monthly). Scenario to rehearse: a compromised administrator account with a valid session — tests AC-27 (MFA), AC-50/51 (audit), AC-54 (bulk-access alerting) end to end.
