# 07 — Data Processing Addendum (template)

**Version 1.0 · 2026-09-20.** Controls AC-09, AC-06. This is a contract template drafted by the engineering side for a lawyer to review before first use; it is not legal advice. It attaches to the AegisCore licence agreement.

---

**DATA PROCESSING ADDENDUM** between **[University name]** ("University", "Controller") and **Nocturne Systems (Pvt.) Ltd.** ("Nocturne").

## 1. Roles

1.1 The University determines the purposes and means of processing the personal data held in its AegisCore installation and is the **Controller**.

1.2 Under the standard deployment (Schedule 1, "Model A") Nocturne supplies software only, has no access to the University's data and is **not a processor**. Clauses 5–9 apply to Nocturne only when it processes personal data on the University's behalf, namely: (a) support access granted under clause 7; (b) any vendor-hosted deployment under Schedule 1 "Model B".

1.3 Licence heartbeat data (Schedule 2) is processed by Nocturne as an independent controller for licence administration and contains no personal data.

## 2. Applicable law

2.1 The parties will comply with the Prevention of Electronic Crimes Act 2016 as amended in 2025, and with the Digital Nation Pakistan Act 2025 and any standards or policy the Pakistan Digital Authority notifies under it (including the National Data Governance Policy once in force), in each case as applicable to them.

2.2 Should Pakistan enact a personal data protection statute, the parties will amend this Addendum within 90 days of its commencement to reflect the statute's requirements for controllers and processors, and in the meantime will apply the principles in Schedule 3.

2.3 GDPR and FERPA do not apply unless the University notifies Nocturne in writing that it processes personal data of EU-resident individuals or records subject to US federal education law; on such notice the parties will agree the additional terms those laws require.

## 3. The University's obligations (Model A)

The University will: (a) operate the installation on infrastructure it controls and record its physical location in Schedule 1; (b) protect the database host with disk or volume encryption and restricted network access; (c) safeguard the generated keys in the configuration volume and the licence credentials; (d) run and test backups per the retention schedule it adopts; (e) assign roles in the system on a least-privilege basis and review them at least annually; (f) publish a privacy notice to its data subjects; (g) decide which optional integrations in Schedule 4 it enables and accept the transfer consequences of each.

## 4. Nocturne's obligations (all models)

Nocturne will: (a) build and maintain the software in accordance with the security controls described in `docs/compliance/04-nist-csf-gap-assessment.md` as updated from time to time, and make the current gap assessment available to the University on request; (b) not build into the software any transmission of personal data to Nocturne or third parties other than those disclosed in Schedule 2 and Schedule 4; (c) publish security advisories and fixed releases for vulnerabilities it becomes aware of, with severity and remediation guidance; (d) maintain a vulnerability-disclosure channel (`SECURITY.md`); (e) notify the University under clause 8 of any security incident in the software or in Nocturne's systems that could affect the University's data.

## 5. Processing on documented instructions (when Nocturne is a processor)

Nocturne will process personal data only on the University's documented instructions, including with regard to transfers outside Pakistan; will ensure persons authorised to process are bound by confidentiality; and will not engage a sub-processor without the University's prior written authorisation, giving at least 30 days' notice of any intended change to Schedule 4.

## 6. Security measures

Nocturne will apply the technical and organisational measures in Schedule 5. The University may request evidence of them once per year, and Nocturne will supply the current internal-audit report and, when available, the ISO/IEC 27001 Statement of Applicability and certificate. Nocturne will not represent itself as certified before a certificate is issued.

## 7. Support access

7.1 Support access is granted per incident, in writing (email suffices), time-limited (default 48 hours), using an account the University creates in its own installation with the minimum role needed.

7.2 All actions under support access are logged by the University's installation (access audit and login audit) and the University may revoke the account at any time.

7.3 Nocturne will not copy personal data out of the installation during support except with the University's written agreement, and will delete any such copy when the incident closes, confirming deletion in writing.

## 8. Security incident and breach notification

8.1 Each party will notify the other **without undue delay and in any case within 48 hours** of becoming aware of a personal-data breach affecting the University's data that it is responsible for or has discovered, using the contact points in Schedule 6.

8.2 The notification will describe the nature of the breach, categories and approximate numbers of data subjects and records, the likely consequences, and measures taken or proposed. Information may be provided in phases.

8.3 The University, as Controller, decides on notification to data subjects and to authorities (PDA, NCCIA under PECA where a criminal act is suspected, HEC). Nocturne will assist and will not notify the University's data subjects itself.

## 9. Data-subject requests

Where Nocturne receives a request from one of the University's data subjects it will forward it to the University within 5 business days and will not respond directly. The software provides access-export, correction, erasure and directory-opt-out functions for the University to use.

## 10. Return and deletion

On termination of the licence the software's data remains the University's (Model A). For any Model B deployment Nocturne will return the data in `pg_dump` custom format and the object-storage contents within 30 days and delete its copies including backups within 90 days, certifying deletion in writing.

## 11. Audit

The University may audit Nocturne's compliance with this Addendum once per year on 30 days' notice, or following a breach, by document review or, for Model B, on site.

## 12. Liability

As per the licence agreement.

---

## Schedule 1 — Deployment model and location

| Field | Value |
|---|---|
| Model | ☐ A (University-hosted)  ☐ B (Nocturne-hosted, pilot only) |
| Physical location of database and file storage | [City, Country; provider] |
| Backup location | [ ] |
| Read-replica / WAL archive location | [ ] |

## Schedule 2 — Data Nocturne receives (Model A)

Daily licence heartbeat to `license.aegiscore.systems`: `university_id`, `university_name`, `current_user_count`, `suite`, `timestamp`. Authentication header `X-AegisCore-License-Token`. Off in offline mode.

## Schedule 3 — Principles applied pending a Pakistani data protection statute

Lawfulness and transparency; purpose limitation; data minimisation; accuracy; storage limitation; integrity and confidentiality; accountability; data-subject rights of access, correction, erasure and objection; parental consent for minors; breach notification.

## Schedule 4 — Optional integrations / sub-processors

| Integration | Provider | Data | Location | Enabled? |
|---|---|---|---|---|
| Email | University SMTP relay | addresses, message bodies | University | ☑ always |
| SMS / WhatsApp / push | Twilio / Meta / Google | phone, message | USA/global | ☐ |
| LLM assistant | Ollama (University host) **or** OpenAI | prompt text | University / USA | ☐ Ollama ☐ OpenAI |
| OCR | Microsoft Azure Document Intelligence | document images | [Azure region] | ☐ |
| Microsoft 365 / Moodle | Microsoft / University Moodle | rosters, grades | M365 tenant region | ☐ |
| Payments | SBP Raast / [gateway] | payer reference, amount | Pakistan / [ ] | ☐ |
| Bot protection | Cloudflare Turnstile | IP address | Cloudflare edge | ☐ |
| Sign-in location lookup | ipapi.co | IP address of each person signing in | USA | ☑ on by default — see `docs/compliance/02-data-flow-map.md` X-15 |
| Encrypted backups | [S3-compatible provider] | full database | [region] | ☐ |
| Secrets / storage | Infisical / AWS / Azure / GCP / R2 | secrets; files | [region] | ☐ |
| Search | Elasticsearch (University host) | directory index | University | ☐ |

## Schedule 5 — Technical and organisational measures (summary; details in `04-nist-csf-gap-assessment.md`)

Argon2id credential storage; RS256 access tokens with key rotation; brute-force lockout; role and attribute-based authorisation, default deny; per-tenant isolation at the query and connection level; application-level AES-256-GCM encryption of designated sensitive fields; encrypted local secret vault; Data Protection key ring encrypted with a certificate; access audit of designated record reads; authentication audit; encrypted backups with a tested restore; dependency vulnerability scanning; vulnerability-disclosure process; incident-response and breach-notification procedure.

## Schedule 6 — Contacts

| Party | Security / privacy contact | Escalation |
|---|---|---|
| University | [name, email, phone] | [ ] |
| Nocturne | Data Protection Contact per `05-governance.md` | [ ] |
