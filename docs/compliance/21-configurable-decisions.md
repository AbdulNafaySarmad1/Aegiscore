# 21 — The decisions each installation owns

**Version 1.0 · 2026-09-23.** Controls AC-01, AC-38; ISO/IEC 27001 A.5.1, A.5.31, A.5.33, A.5.34.

Three things in this programme are **not the supplier's to decide**, and are therefore not fixed values in any document here. They are settings an administrator reads and changes at any time, in the portal at **Administration → Compliance**, or over the API at `GET`/`PUT /api/admin/compliance-settings`.

| Decision | Why it cannot be a product constant |
|---|---|
| **Data protection contact** | Naming an accountable person for someone else's institution is not something software can do. A placeholder would be worse than a blank: a privacy notice that names the wrong person is a false statement to every data subject who reads it. |
| **Where the data lives** | The institution chose the host. The product cannot know whether the database is in Lahore or Frankfurt, and the answer decides whether a cross-border transfer is happening at all. |
| **How long records are kept** | Retention is set by the registrar, the finance office, tax and companies legislation, and — when it is enacted — by statute. A supplier guessing "seven years" for disciplinary records is guessing about a real person's file. |

## How they behave

- **Stored** as tenant configuration under the `compliance.*` keys, which is where the nightly retention worker already reads. A period changed this afternoon is applied tonight; nothing is cached in the process, and no restart or redeploy is involved.
- **Contact and location ship empty.** The product names nobody and claims no location. Both are reported as unmet obligations on the compliance screen until the institution fills them in.
- **Retention ships with working starting values** — because a period of "unset" would mean the worker either deletes nothing or deletes everything, and both are worse than a sensible number the institution then revises. The shipped values are engineering's reading of common Pakistani record-keeping practice, not advice, and the screen says so.
- **Periods are clamped** when written (for example audit retention 1–100 years, sign-in records 30–3650 days). A typed zero cannot erase a register overnight.
- **Every change is audited**, and takes the `Settings.Write` permission to make.

## What is enforced automatically, and what is a published commitment

Being straight about this matters: an institution reading a retention screen should know which numbers the software acts on and which it merely records.

| Period | Enforced by the nightly worker? |
|---|---|
| Access audit and consent records | **Yes** |
| Change audit | **Yes** |
| Sign-in records | **Yes** |
| Visitor and gate records | **Yes** |
| Unsuccessful and withdrawn applicants (and their documents) | **Yes** |
| Student records after graduation, welfare records, staff records, financial records, unsuccessful candidates | **No — recorded and published, applied by the institution's own procedures.** The mechanism for each is named in `09-retention-policy.md` §2 and building the remainder is tracked there |

## Where the documents defer to the settings

| Document | What it used to assert | What it says now |
|---|---|---|
| `05-governance.md` §1 | A named data protection contact | Points here; the live value is the setting |
| `05-governance.md` D-A, `03-data-residency.md` | An assumed deployment location | Points here; the live value is the setting |
| `09-retention-policy.md` §2 | Periods marked "[DEFAULT — confirm]" | Shipped starting values, with the live value being whatever the institution has set |
| `06-privacy-notice.md` | Bracketed retention periods | Filled from the settings when the notice is published |

## For an auditor

Asking "what is your retention period for counselling notes?" should be answered by opening the compliance screen of the installation in question, not by reading a supplier document. That is the intended design: the supplier evidences the *mechanism*, the institution owns the *decision*, and the two are not confused with each other.
