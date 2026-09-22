# 06 — Privacy notice (product-specific)

**Version 1.0 · 2026-09-20 · approved by the DPC (`05-governance.md`).** Control AC-08.

This notice is written for what AegisCore actually does (see `01-data-inventory.md`, `02-data-flow-map.md`). It has two audiences and two parts: **Part A** is the notice a university publishes to its students, applicants and staff (the university is the controller and may edit it); **Part B** is Nocturne's own notice as the software vendor. Square-bracketed fields are filled per installation.

---

## Part A — Notice for people whose data is in [University name]'s AegisCore

**Who is responsible.** [University name] ("we") is the controller of your personal data. AegisCore is software supplied by Nocturne Systems (Pvt.) Ltd. and runs on infrastructure we control. Nocturne cannot see your data (see Part B).

**What we collect and why.**

| Category | Examples | Why |
|---|---|---|
| Identity and contact | name, roll number, CNIC, date of birth, gender, nationality, phone, email, photograph | to identify you, run admissions and enrolment, issue documents |
| Guardian / emergency contact | guardian name, relationship, phone, email | to contact a guardian in an emergency and, for applicants under 18, to obtain consent |
| Academic record | enrolments, attendance, marks, grades, transcripts, appeals, degree audit | to deliver and certify your education; retained permanently as the academic register |
| Attendance location | GPS coordinates when attendance is marked from the mobile app | to verify attendance; stored encrypted |
| Welfare and conduct | counselling notes, disciplinary records, mental-health support tickets, wellness check-ins | to support you and to run the university's disciplinary procedures; restricted to the staff involved |
| Foreign-student status | passport, visa, MORA registration | legal obligation for international students |
| Fees and payments | fee demands, payments, scholarships, payment references | to bill you and account for your payments |
| Accommodation, transport, library, cafeteria, elections | room, route, loans, orders, encrypted ballots | to run those services |
| Staff records (if you are staff) | employment, contracts, salary, payroll, bank account reference (encrypted), leave, performance, training, recruitment | employment administration and payroll (EOBI, income tax) |
| Account and security | login times, IP address, approximate location, device | to keep your account secure and detect misuse |
| Consent and privacy requests | the exact consent text you saw, when, and any access or erasure request | to prove and honour your choices |

**Where it is kept.** On [University name]'s own servers at [location]. Data leaves those servers only for the services listed below, each of which we have enabled deliberately:

- Email through [university mail relay] — always.
- [Only if enabled] SMS / WhatsApp / push notifications through [Twilio / Meta / Google].
- [Only if enabled] Payment processing through [Raast / gateway name].
- [Only if enabled] AI assistance through [Ollama on our own servers — no data leaves / OpenAI — text you type into the assistant is sent to OpenAI in the United States].
- [Only if enabled] Document reading through Microsoft Azure Document Intelligence in [region].
- [Only if enabled] Microsoft 365 / Moodle roster synchronisation.
- [Only if enabled] Encrypted backups to [storage location].
- Your IP address is sent to ipapi.co (United States) when you sign in from outside the campus network, so that we can show you where your account was used and spot a sign-in that is not you.
- Licence check to Nocturne: the university's name, id, licensed-user count and time. **No personal data.**

**Disclosures required by law.** Mental-health mandatory reports to [authority]; foreign-student reporting to [MORA / relevant authority]; payroll reporting to FBR/EOBI; responses to lawful requests under PECA 2016 (as amended 2025).

**How long we keep it.** Per `09-retention-policy.md`, summarised: academic register — permanently; admissions data for unsuccessful applicants — [2 years]; financial records — [10 years]; staff records — [7 years after leaving]; counselling notes — [7 years after last session]; login and access logs — [1 year]; visitor CNICs — [90 days]; consent and privacy-request records — 6 years (7 for completed erasures).

**Your rights.** Whether or not the law currently requires it, you can ask us to:

- **see** what we hold (a data-subject access export is produced by the system within [30 days]);
- **correct** it;
- **erase** it where we have no duty to keep it — academic facts are anonymised, not deleted (the degree stays on the register; your name comes off the identifiable record) — within 30 days;
- **withdraw consent** where processing was based on consent;
- **opt out of the directory** (your name will not appear in the campus directory or search).

Send requests to [university privacy contact]. We will answer within 30 days.

**Complaints.** [University ombudsperson / HEC complaint route]. Once the Pakistan Digital Authority or a data-protection commission publishes a complaints route, it will be listed here.

---

## Part B — Nocturne Systems (Pvt.) Ltd. as software vendor

**Model A (per-university installation, the standard offer).** Nocturne is not a processor of student or staff data: the software runs on the university's infrastructure, the database and files are the university's, and the encryption keys are generated on the university's server. The only data Nocturne receives is the licence heartbeat: `university_id`, `university_name`, `current_user_count`, `suite`, `timestamp`, sent daily to `license.aegiscore.systems` unless the installation is in offline mode. Nocturne keeps heartbeat records for the life of the licence plus 2 years for billing evidence.

**Support access.** Nocturne has no standing access. If a university grants temporary access for support, it does so under the DPA (`07-dpa-template.md` §7) and the access is logged by the university's installation.

**Model B (vendor-hosted).** Restricted to demonstrations and non-production pilots with synthetic data until a production region is approved (`05-governance.md` D-A). If that changes, this part will be rewritten as a processor notice and the DPA will be mandatory.

**Contact.** Data Protection Contact: `05-governance.md` §1.

---

## What this notice must not say (checked against `00-PROGRAM.md`)

Not "GDPR compliant", "FERPA compliant", "PDPA compliant", "ISO 27001 certified". Permitted: "built to the principles of the draft Personal Data Protection Act", "GDPR-aligned architecture", "PECA-aware".
