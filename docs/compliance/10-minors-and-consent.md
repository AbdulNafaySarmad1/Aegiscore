# 10 — Children's data and consent handling

**Version 1.0 · 2026-09-20.** Controls AC-39, AC-40. Design document; the code lands in Phase 3 item 7.

## 1. Where minors appear

- **Applicants.** Intermediate/A-level leavers apply at 16–18. `AdmissionApplications.DateOfBirth` is collected (`AdmissionsEntities.cs:16`) but never used to compute age. No guardian is captured at application time; `StudentGuardians` exists only after enrolment.
- **Dependants.** Staff dependants are not modelled; if a future HR module adds them, this document applies.
- **Visitors.** Minors visiting campus with a guardian: the guardian's CNIC is recorded, not the child's. No change.

## 2. Consent model (already built, under-used)

`ConsentLedgers` stores the exact text shown, category, timestamp, optional expiry, revocation and an encrypted IP (`PrivacyComplianceService.RecordConsentAsync`). Consent is explicit; the application never infers it. The only caller is `PrivacyComplianceController` — no form calls it. That is the gap.

## 3. Design

### 3.1 Age gate on the application form

- Compute age at submission from `DateOfBirth`. Threshold: **18** (age of majority in Pakistan under the Majority Act 1875).
- If under 18: the form requires a guardian block — name, relationship, CNIC, phone, email — and the guardian's consent (3.3). The application cannot be submitted without it. Server-side enforced in `S2AdmissionsController` / `AdmissionsServices`, not only in the portal.
- Guardian details are stored in `StudentGuardians` keyed to the application (a nullable `AdmissionApplicationId` is added) so they carry over on enrolment and are erased with the applicant.

### 3.2 Consent texts (tenant-configurable, versioned)

Stored as tenant settings `consent.<category>.text` with a version; the version and the full text are copied into each `ConsentLedgers` row so a later edit cannot change what a person agreed to.

| Category key | Shown to | When |
|---|---|---|
| `admissions.processing` | applicant (or guardian if minor) | application submission |
| `admissions.guardian` | guardian | application submission when applicant < 18 |
| `directory.listing` | student | first sign-in; reversible via `DirectoryOptOut` |
| `attendance.geolocation` | student | first mobile attendance mark |
| `welfare.counselling` | student | first counselling/mental-health ticket |
| `ai.assistant` | any user | first use of the assistant; text names the provider (Ollama/OpenAI) from the live setting |
| `notifications.sms_whatsapp` | user | when a phone channel is enabled |

Each is recorded via `RecordConsentAsync` with the category above; the form shows the text verbatim and the checkbox is unticked by default.

### 3.3 Guardian consent mechanics

- Guardian consent is captured in the same session (the guardian is physically present or the applicant relays the guardian's details) **and** confirmed out-of-band: an email/SMS with a one-time link to the guardian's contact; the application stays in status `AwaitingGuardianConsent` until the link is used or the admissions office records a signed paper consent (upload + audit entry). This mirrors how universities already handle guardian undertakings on paper.
- On the applicant's 18th birthday the guardian consent is not revoked, but the student is shown `admissions.processing` again at next sign-in to consent in their own name.

### 3.4 Erasure and retention

- Guardian data is erased with the applicant (Phase 3 extends erasure to `StudentGuardians` rows linked to applications — the erasure path already covers guardians of students).
- Consent records are kept 6 years after expiry per `09`.

### 3.5 Withdrawal

- `RevokeConsentAsync` exists. Phase 3 adds a portal page (`/privacy`) listing the person's consents with a revoke button, and the DSAR export already includes the ledger.
- Revoking `directory.listing` sets `DirectoryOptOut`; revoking `attendance.geolocation` disables GPS capture for the account (attendance is still markable without coordinates); revoking `ai.assistant` hides the assistant.

## 4. What is not consent-based

Processing needed to deliver the education or employment, or required by law (academic register, fees, payroll, visa reporting, mandatory mental-health reports), does not rest on consent and is not offered as revocable. The privacy notice says so plainly so that a revoked consent is never mistaken for a right to be removed from the register.

## 5. Acceptance tests (Phase 3)

1. Application with DOB giving age 17 and no guardian → 400 with a message naming the missing guardian block.
2. Same with guardian and consent → application created, status `AwaitingGuardianConsent`, `ConsentLedgers` has two rows (`admissions.processing` by guardian, `admissions.guardian`), `StudentGuardians` row linked.
3. Guardian link used → status `Submitted`.
4. Age 18 → no guardian block required; one `admissions.processing` row in the applicant's name.
5. Consent text edited in tenant settings → old ledger rows keep old text and version.
6. Erasure of the applicant removes the guardian row and both consent rows.
