# AegisCore compliance programme

**Status:** Phase 1 (data mapping and gap assessment) and Phase 2 (governance and policy) complete — 2026-09-20. Phase 3 (technical controls) in progress.
**Owner:** to be named in Phase 2 (data protection point of contact).
**Scope:** the root-level AegisCore product (`AegisCore.API`, `AegisCore.Infrastructure`, `AegisCore.Portal`, `AegisCore.Mobile`, the `aegiscore-clip` sidecar) as shipped by `docker-compose.client.yml` (per-university container) and `docker-compose.cloud.yml` (vendor-hosted). Legacy `aegiscore-*` directories are out of scope: they are not built or deployed.

## What may honestly be claimed today

| Claim | Allowed? | Basis |
|---|---|---|
| "PECA 2016 / PECA Amendment 2025 aware; controls against unauthorised access are in place" | Yes, once the Phase 3 gaps in `04-nist-csf-gap-assessment.md` are closed | Authentication, RBAC, audit and brute-force controls exist in code; gaps are recorded |
| "Built ahead of Pakistan's draft Personal Data Protection Act" | Yes | Consent ledger, erasure, DSAR export and retention worker exist (`AegisCore.Infrastructure/Compliance/`) |
| "PDA-aware; preparing for the National Data Governance Policy 2026" | Yes | See legal check below |
| "GDPR-aligned architecture" / "FERPA-aligned access logging" | Yes, with the caveats in `01-data-inventory.md` §4 and `04` PR.AA / DE.CM | Partial coverage — see gap assessment |
| "GDPR compliant", "FERPA compliant", "PDPA compliant", "ISO 27001 certified" | **No** | No EU/US data subjects identified; PDPA not enacted; no certificate |
| "ISO 27001-aligned" / "audit-ready" | Not yet — after Phase 4 | ISMS documents do not exist yet |

## Legal landscape check — 2026-09-20 (repeat at the start of every phase)

- **Draft Personal Data Protection Act:** still not enacted. Cabinet-approved in 2023; no National Assembly / Senate passage reported as of mid-2026. Sources: [Chambers Data Protection & Privacy 2026 — Pakistan](https://practiceguides.chambers.com/practice-guides/data-protection-privacy-2026/pakistan), [DLA Piper — Pakistan](https://www.dlapiperdataprotection.com/index.html?t=law&c=PK), [NCPDP (Wikipedia)](https://en.wikipedia.org/wiki/National_Commission_for_Personal_Data_Protection).
- **National Data Governance Policy 2026 (Pakistan Digital Authority):** finalised by MoITT/PDA in August 2026; **awaiting federal cabinet approval and Gazette notification**. It binds federal ministries, departments, regulators, public-sector companies **and their contractors**, with a 12-month alignment window after notification. Public universities are public-sector bodies, so a vendor supplying their information system should expect to be in scope as a contractor. Sources: [ProPakistani, 2026-08-05](https://propakistani.pk/2026/08/05/it-ministry-finalizes-national-data-governance-policy-2026/), [Business Recorder](https://www.brecorder.com/news/amp/40427660), [Pakistan Digital Authority](https://www.pda.gov.pk/).
- **PECA 2016 + Amendment 2025:** in force. Investigation moved to the National Cyber Crime Investigation Agency. Source: [Lexology year in review](https://www.lexology.com/library/detail.aspx?g=1809c8d8-8609-4545-9432-c6bba6bd3338).
- **HEC data-handling circulars:** a web search on 2026-09-20 found **no HEC circular specific to university information-system data security**; only revised curricula and the Degree Attestation System. **Action for Phase 2:** ask HEC's IT/QA directorate directly and record the answer here. Do not design around an assumed HEC requirement.
- **PKCERT National Cybersecurity Handbook (Sept 2026):** guidance for government employees; useful as a reference for the Phase 4 awareness training. Source: [ProPakistani, 2026-09-06](https://propakistani.pk/2026/09/06/pakistan-issues-new-cybersecurity-guide-for-govt-employees/).

## Framework applicability (decided in Phase 1)

| Framework | Applies? | Treatment |
|---|---|---|
| PECA 2016/2025 | Yes, now | Mandatory. Mapped onto the unified control set |
| Draft PDPA | Not yet law | Build ahead; consent, DSAR, erasure, breach notice |
| NDGP 2026 / PDA | Likely, after gazette | Track monthly; expect data-sovereignty, classification, cross-border and disposal rules for public-sector contractors |
| HEC | Unknown — no circular found | Confirm with HEC in Phase 2 |
| GDPR | No EU data subjects identified in the current pipeline | Design pattern only. Reassess if an EU exchange programme or partner appears |
| FERPA | No US institution or US-student records identified | Design pattern only (the `RequireFerpaAudit` mechanism already borrows its access-logging model) |
| NIST CSF 2.0 | Voluntary | **Backbone of the unified control set** (GV/ID/PR/DE/RS/RC) |
| ISO/IEC 27001:2022 / 27002 | Certification target | Phase 4 ISMS; 27002 as implementation reference |

## Phase 1 deliverables

| File | Content |
|---|---|
| `01-data-inventory.md` | Every category of personal data, where it lives, who can reach it, whether it is encrypted, whether erasure covers it |
| `02-data-flow-map.md` | Entry points, stores, exits, third parties — each verified against code |
| `03-data-residency.md` | Where data physically lives under each deployment model |
| `04-nist-csf-gap-assessment.md` | Unified control set (AC-xx ids) mapped to NIST CSF 2.0 with evidence and gaps; cross-reference to the hardening work already landed |

## Rules (from the programme brief, restated so nobody forgets them)

1. No "compliant" or "certified" claim that is not literally true.
2. Every control is verified with a test, a log or a restored backup. A policy alone satisfies nothing.
3. One control set. Every framework maps onto it.
4. Re-check the Pakistani legal landscape at the start of each phase and update the section above.
5. Report each phase boundary honestly: done / partial / gap.

## Phase 2 deliverables

| File | Content |
|---|---|
| `05-governance.md` | Data-protection contact, roles, decisions taken (hosting region, retention defaults, AI provider) |
| `06-privacy-notice.md` | Privacy notice for the product's actual practices — University part and vendor part |
| `07-dpa-template.md` | Data Processing Addendum with sub-processor schedule |
| `08-ropa.md` | Record of Processing Activities (23 University activities, 6 Nocturne activities) |
| `09-retention-policy.md` | Retention schedule naming the enforcing mechanism per category, erasure path, backup bound |
| `10-minors-and-consent.md` | Age gate, guardian consent, consent categories — design for Phase 3 item 7 |
| `11-breach-notification.md` | Incident severity, 48 h / 72 h / 7 d clock, NCCIA/PDA/HEC routes, templates |
