# 18 — Security awareness, training and the joiner/leaver procedure

**Version 1.0 · 2026-09-22.** ISO/IEC 27001:2022 A.6.3 (and A.6.1, 6.2, 6.4, 6.5 written in advance); control AC-35.

## 1. Who takes it and when

| Who | When |
|---|---|
| Everyone in the company, including the founder | On joining, before access to source or client systems |
| Everyone | Annually, and after any incident that the training would have prevented |
| A contractor with repository or client access | Before access; the same content |

Completion is recorded in §5. "Everyone" is currently one person; the record exists so that the first hire does not arrive into an undocumented practice.

## 2. Content

Aimed at what actually goes wrong for a small company that holds other people's student records, not at generic slideware.

1. **What we hold, and why it matters.** The categories in `01-data-inventory.md`: CNICs, passports, guardians' contact details, counselling notes, mental-health tickets, salaries. Concrete consequence of exposure for a real person, not an abstraction.
2. **The claims rule.** Never say "certified", "GDPR compliant", "PDPA compliant" outside the approved table in `00-PROGRAM.md`. An overstated claim to a procurement team is a compliance failure of its own.
3. **Phishing and pretexting**, with the shapes that actually target us: a "university IT" request for support access outside the DPA route; a fake procurement questionnaire asking for architecture detail; a package or dependency that wants credentials; an "urgent" request to bypass the release process.
4. **Credentials.** Password manager for everything; MFA everywhere it exists; never a shared account; never a credential in the repository, an image, a ticket or a chat message. Where secrets do live: the vault, the config volume, the password manager.
5. **Client data.** Support access only under DPA §7 — written request, time-limited, minimum role, logged, and any copy deleted at close with written confirmation. Client personal data never lands on the workstation.
6. **Reporting.** Anything odd goes into `11-breach-notification.md` at hour zero. Reporting a suspicion that turns out to be nothing is always the right call; the disciplinary process (A.6.4) applies to concealment, never to reporting.
7. **The workstation rules** in §3.
8. **Secure development**, for anyone who writes code: the control set in `04`, what the CI gates mean and why bypassing one is a nonconformity, adding a new personal-data field means updating `01` and `08` before release.

## 3. Workstation and remote-working rules (A.6.7, A.7.7–7.9, A.8.1)

- Full-disk encryption on, verified at each internal audit (E-16).
- Screen lock at 5 minutes, password or biometric on wake.
- OS, browser and toolchain on automatic update.
- Password manager with MFA; no credentials in plain files, shell history or notes.
- No client personal data on the device — support work happens inside the client's installation.
- No work touching source or client systems on an untrusted network without a VPN.
- No removable media for anything in scope.
- Device disposal: cryptographic erase of the encrypted drive plus a factory reset, recorded in §5.
- Loss or theft is an incident, reported immediately under `11`.

## 4. Joiner and leaver procedure (A.6.1, 6.2, 6.5, 5.11)

Written before the first hire so that the day it happens there is a procedure rather than an improvisation.

**Before access is granted**
1. Identity and right-to-work verified; references checked proportionately to the access (A.6.1).
2. Employment or contractor terms signed, including confidentiality, the acceptable-use rules in §3, and the obligation to report events (A.6.2, A.6.6).
3. This training completed and recorded (A.6.3).
4. Accounts created with the minimum role; MFA enrolled before first use; access recorded in an access list.

**During**
5. Access reviewed at every management review against what the person actually does.
6. A role change is treated as a leaver-plus-joiner for access purposes.

**On leaving** — same day, in this order
7. Repository, registry, cloud, password-manager and client-installation accounts disabled.
8. Shared credentials the person knew are rotated — including `AEGISCORE_PQC_KEY_BASE64` where they had access to a config volume, once R-002's rotation command exists.
9. Company equipment returned; the return recorded (A.5.11).
10. Confidentiality obligations confirmed in writing as continuing (A.6.6).
11. The offboarding recorded in §5.

## 5. Training and personnel record

| Date | Person | Activity | Evidence |
|---|---|---|---|
| 2026-09-22 | Abdul Nafay Sarmad (DPC) | Initial security awareness training, §2 items 1–8 | Self-certified: content reviewed in full; workstation rules in §3 verified on the device the same day (full-disk encryption on, screen lock 5 min, updates current, password manager MFA on). Honest note: a single-person company self-certifying its own training is a weak control, and the next internal audit re-checks the §3 items with evidence rather than accepting this row |
| — | — | Next due: 2027-09-22 | — |

## 6. Effectiveness

A training programme nobody tests is decoration. Effectiveness is checked by:
- a simulated phishing message to company addresses at least annually, result recorded here;
- the tabletop exercises (`exercises/`), which test whether the reporting and response behaviour actually happens;
- internal audit check E-16 and E-17.

First phishing simulation due: 2027-03, alongside the second tabletop.
