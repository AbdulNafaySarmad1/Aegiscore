# 13 — ISMS scope statement

**Version 1.0 · 2026-09-22 · approved by the DPC (`05-governance.md`).** ISO/IEC 27001:2022 clause 4.3. This is the statement an auditor reads first; everything in the Statement of Applicability (`15`) is judged against it.

## 1. The organisation

Nocturne Systems (Pvt.) Ltd., a software company registered in Pakistan, developing and supplying AegisCore, a university enterprise resource planning system, to higher-education institutions. At the date of this statement the company has one person performing all roles (`05-governance.md` §2) and no premises of its own; development is done from a home office in Pakistan on company-controlled equipment.

Stating the size plainly matters: an ISMS for one person is a real ISMS with fewer people in it, not a smaller set of obligations. Where a control assumes separation between people, the compensating arrangement is named in `15` rather than the control being quietly excluded.

## 2. In scope

| # | Item | Why |
|---|---|---|
| S-1 | Development, build, release and support of the AegisCore product — the `AegisCore.*` projects, the Portal, the Mobile app, the `aegiscore-clip` sidecar, the deployment scripts and the documentation in this repository | This is what the company makes |
| S-2 | The source repository, its CI/CD pipelines and the release artefacts (container images, NuGet packages, the dotnet tool) | The supply chain a client installs from |
| S-3 | The licence authority service `license.aegiscore.systems` and the records it holds | The only service Nocturne operates that a client depends on |
| S-4 | Company information assets: source code, signing and licence keys, customer contact records, support correspondence, vulnerability reports | What an attacker would want from the company |
| S-5 | The developer workstation and any company device holding S-4 | The realistic point of compromise for a one-person company |
| S-6 | Support access to a client installation, when granted (`07-dpa-template.md` §7) | The only route by which Nocturne touches a client's personal data under Model A |
| S-7 | Vendor-hosted demonstration and pilot environments holding synthetic data (`05-governance.md` D-A) | Operated by Nocturne |

## 3. Out of scope, and who owns it instead

| # | Item | Owner | Recorded where |
|---|---|---|---|
| O-1 | A university's own installation: its servers, database, storage, network, backups and the personal data in them | The university, as controller | `07-dpa-template.md` §3 |
| O-2 | The physical security of any university campus or data centre | The university | — |
| O-3 | Identity providers a university connects (Entra ID, Google Workspace, Okta) and their MFA policy | The university | `02-data-flow-map.md` E-02 |
| O-4 | Optional third-party integrations a university enables (payments, SMS, OCR, LLM) | The university, with the provider | `07-dpa-template.md` Schedule 4 |
| O-5 | Cloud infrastructure underlying the licence authority and any pilot environment | The hosting provider, under its own certifications | Supplier register, `14` |
| O-6 | A production vendor-hosted deployment of client data | **Does not exist.** Prohibited until a region is approved | `05-governance.md` D-A, `03-data-residency.md` |

O-1 is the boundary that matters commercially: Nocturne can be certified for how it builds and supports the product, and a university's own installation is certified — or not — by the university. An auditor will be told this plainly, and a client's procurement team should be too. The DPA carries the controller-side obligations so the split is contractual, not just documentary.

## 4. Interfaces and dependencies across the boundary

| Interface | Direction | Control |
|---|---|---|
| Release artefacts (images, packages) | Nocturne → University | Signed/digest-pinned releases; advisories (`SECURITY.md`) |
| Licence heartbeat | University → Nocturne | Five non-personal fields (`02-data-flow-map.md` X-01) |
| Support access | University → Nocturne, per incident | Time-limited, logged, deleted at close (DPA §7) |
| Vulnerability reports | Public → Nocturne | `SECURITY.md`, `12-vulnerability-register.md` |
| Breach notification | Either direction | `11-breach-notification.md`, DPA §8 |

## 5. Exclusions from Annex A

None taken on the grounds of "we are small". Controls that do not apply do so because the activity does not exist in this organisation (for example, no outsourced development, no company-controlled server room), and each is justified individually in `15`. Nothing is excluded merely because it is inconvenient.

## 6. Interested parties and their requirements (clause 4.2)

| Party | What they require |
|---|---|
| University clients | Confidentiality and integrity of their students' and staff records; evidence before procurement; breach notice |
| Students, staff, applicants, guardians | That their records are not exposed and their rights are honoured |
| HEC | Whatever it issues for university information systems (`00-PROGRAM.md` — none found; enquiry open) |
| Pakistan Digital Authority | Compliance with the National Data Governance Policy once notified, including its annual audits and the National Data Maturity Index |
| NCCIA / PECA | Lawful conduct and cooperation on cybercrime |
| The certification body | Evidence that the ISMS in this scope is implemented and effective |

## 7. Review

Reviewed at each management review (`17`) and whenever the company gains staff, premises, a production hosted deployment, or a client outside Pakistan.
