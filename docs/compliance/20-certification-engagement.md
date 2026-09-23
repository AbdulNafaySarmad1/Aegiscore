# 20 — ISO/IEC 27001 certification engagement

**Version 1.0 · 2026-09-23.** Phase 6 of the compliance programme. This document prepares and tracks the external certification engagement. It is the one phase the company cannot complete by itself: the certificate is issued by an accredited body on its own schedule, after audits it conducts.

## 1. Where the company actually stands

Stated first, because everything below depends on it and because a hopeful version of this section is how organisations end up claiming a certificate they do not have.

| | Position on 2026-09-23 |
|---|---|
| ISMS established (clauses 4–8) | **Yes** — `13`–`19` |
| Risk assessment and treatment (6.1) | **Yes** — `14`, 20 risks, one High retained with written acceptance |
| Statement of Applicability (6.1.3 d) | **Yes** — `15`, all 93 Annex A controls, 59 Implemented, 19 Partial, 1 Planned, 14 N/A |
| Internal audit executed (9.2) | **Yes, once** — `audits/2026-09-22-full-scope.md`, 7 findings |
| Management review held (9.3) | **Yes, once** — `reviews/2026-q3.md` |
| Improvement / corrective action (10) | **Partially evidenced** — findings carry owners and dates; two closed with evidence |
| **Certified** | **No.** No body engaged, no Stage 1 booked |
| **Honest external claim today** | "ISO/IEC 27001-aligned; ISMS implemented and internally audited; certification not yet sought" |

**Not yet ready to engage.** Three things must be true first, and two are not:

1. ~~A.5.1 policy issued~~ — done 2026-09-22 (`19`).
2. **A-06 closed by a second party.** The Major finding from the internal audit is remediated but, per `16` §1, a Major closed by the person who caused it is not closed. **Blocking.**
3. **Clause 9.2 independence.** The internal audit was performed by the person who built the controls. A certification body may accept this for a one-person organisation with the mitigations in `16` §1, or may raise it at Stage 1. It cannot be resolved internally. **Disclose, do not hide.**

An honest reading: the ISMS is real and evidenced, and a Stage 1 could be booked now, but the company would be walking in with a known-open Major and a structural independence limitation. Better to close item 2 first.

## 2. Choosing a certification body

**Accreditation matters more than brand.** A certificate is worth what its accreditation is worth. Two routes:

| Route | Accreditor | Notes |
|---|---|---|
| Locally accredited | **PNAC** (Pakistan National Accreditation Council) — its ISO/IEC 17021 certification-body register is the authoritative list ([pnac.gov.pk](https://pnac.gov.pk/Certification-Bodies/Active)) | Cheaper, local auditors, defensible with HEC and the PDA |
| Internationally accredited | UKAS, ANAB, or another IAF MLA signatory — bodies such as BSI, SGS, TÜV, DQS and Bureau Veritas operate in Pakistan | Costs more, recognised by international clients and investors |

**Requirements a candidate body must meet** — these are checks, not preferences:

1. Accredited **specifically for ISO/IEC 27001** under ISO/IEC 17021-1 and ISO/IEC 27006, not merely for ISO 9001. Verify on the accreditor's own register, not the body's website.
2. Accreditation currently active — check the register's expiry, not a PDF the body sends.
3. Auditors experienced with software product companies and with cloud/multi-tenant deployments.
4. Willing to state, in writing before contracting, how they treat clause 9.2 independence for a one-person organisation. If a body cannot answer this, it is the wrong body.
5. **Not** the same organisation that provides consultancy on the same ISMS — that is an impartiality breach under 17021 and can invalidate the certificate.

**Anti-pattern to avoid:** "certificate in 2 weeks, no audit" offers. They exist in this market, they are unaccredited, and a client's IT team that checks the accreditation register will find nothing — which is worse than having no certificate.

## 3. What to send with the enquiry

| # | Item | Document |
|---|---|---|
| 1 | Scope statement | `13-isms-scope.md` |
| 2 | Statement of Applicability | `15-statement-of-applicability.md` |
| 3 | Information security policy | `19-information-security-policy.md` |
| 4 | Risk method and register | `14-risk-methodology-and-register.md` |
| 5 | Internal audit report | `audits/2026-09-22-full-scope.md` |
| 6 | Management review minute | `reviews/2026-q3.md` |
| 7 | Incident procedure and exercise record | `11`, `exercises/` |
| 8 | Organisation size and structure | one person; state it in the enquiry, because it determines audit duration under ISO/IEC 27006 and a body that learns it late will re-quote |

Send the internal audit report **including its findings**. A body that sees a real internal audit with a Major finding recorded and tracked forms a better view than one that receives a report saying everything is fine.

## 4. Expected sequence and realistic timing

Timings are from the point of engagement and are **largely outside the company's control** — auditor availability is the usual constraint.

| Stage | What happens | Typical elapsed |
|---|---|---|
| Enquiry and quotation | Scope, headcount and site count determine audit days | 2–4 weeks |
| Contract and scheduling | The first date offered is often 6–10 weeks out | 4–8 weeks |
| **Stage 1** — documentation review | Body reads the ISMS documents and judges readiness for Stage 2; raises findings. Often remote | 1 day |
| Stage 1 findings remediation | Whatever they raise | 2–8 weeks |
| **Stage 2** — implementation audit | Evidence that controls operate: they will ask to see the audit logs, the restore drill, the alerting, the consent records. `16` §4's evidence commands are the rehearsal | 2–3 days |
| Findings and corrective action | Major findings must be closed before recommendation | 2–12 weeks |
| Certification decision | Independent reviewer inside the body, then issue | 2–6 weeks |
| **Total** | | **6–12 months from engagement, commonly longer** |

Then: surveillance audits at roughly 12 and 24 months, recertification at 3 years. The certificate is not an endpoint; it is a subscription to keeping the ISMS running.

## 5. What Stage 2 will probably probe, and what answers exist

| Likely question | Answer |
|---|---|
| "Show me that a backup restores." | `deploy/scripts/backup-restore-drill.sh`; the 2026-09-20 record; the CI job on every push |
| "Show me who read a student's record last month." | `GET /api/admin/ferpa-audit` |
| "Show me that a grade change is attributable." | `AuditLogs` rows via the change interceptor; `SensitiveChangeAuditTests` |
| "Prove an erased person is gone." | `ErasureScopeTests`; walk one request; `09` §3 |
| "Who can see salaries?" | `PermissionMatrixTests`; the Staff/Payroll split |
| "How do you know your dependencies are clean?" | `12`; the CI gates; the Next.js RCE found and fixed |
| "When did you last test your incident plan?" | `exercises/2026-09-22`, with its four findings |
| "How do you audit yourself with one person?" | `16` §1 — stated as a limitation, with mitigations. **Expect this to be probed hardest** |
| "Your 2025 architecture document says things your SoA does not." | The superseded banner on `docs/06`, and internal audit finding A-06 |

## 6. Cost planning

Not quoted here because quotations depend on the body, the accreditation and the audit days assessed. Budget for: Stage 1 + Stage 2 audit days, the certification decision fee, annual surveillance, and travel for local auditors. For a one-person organisation with a single scope, audit duration is at the low end of the ISO/IEC 27006 table — say so in the enquiry to avoid being quoted for a larger organisation.

## 7. Actions before engaging

| # | Action | Owner | Status |
|---|---|---|---|
| C-1 | Close internal audit finding A-06 with a second-party re-check | DPC | **Open — blocking** |
| C-2 | Shortlist three bodies from the PNAC ISO/IEC 17021 register and any IAF MLA accreditor, verifying 27001 scope on the register | DPC | Open |
| C-3 | Ask each in writing how they handle clause 9.2 for a one-person organisation | DPC | Open |
| C-4 | Confirm none of the shortlist has provided consultancy on this ISMS | DPC | Open |
| C-5 | Assemble the §3 pack | DPC | **Ready** — every document exists |
| C-6 | Reduce the 19 Partial controls where cheap to do so; leave the rest stated | DPC | Ongoing |
| C-7 | Second management review before Stage 1 | DPC | 2026 Q4 |

## 8. Claims permitted during the engagement

Amends the table in `00-PROGRAM.md` for the duration:

| Situation | Permitted wording |
|---|---|
| Today, before engagement | "ISO/IEC 27001-aligned. ISMS implemented, internally audited, Statement of Applicability available on request." |
| Body engaged, Stage 1 not held | "ISO/IEC 27001 certification in progress with an accredited body; Stage 1 scheduled for <date>." |
| Stage 1 passed | "ISO/IEC 27001 Stage 1 complete; Stage 2 scheduled for <date>." |
| Stage 2 held, decision pending | "ISO/IEC 27001 Stage 2 complete; certification decision pending." Never "certified" |
| Certificate issued | "ISO/IEC 27001:2022 certified by <body>, certificate <number>, scope <scope statement>, valid to <date>." Quote the scope — a certificate for this scope does **not** cover a university's own installation |
| Any stage failed or withdrawn | Revert to "aligned". Do not describe a withdrawn engagement as "in progress" |

The last row matters most. An engagement that stalls is the moment the temptation to overstate is strongest.
