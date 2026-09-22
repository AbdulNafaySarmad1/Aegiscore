# 19 — Information security policy

**Version 1.0 · approved 2026-09-22 by Abdul Nafay Sarmad, Founder, Nocturne Systems (Pvt.) Ltd.** ISO/IEC 27001:2022 clause 5.2 and A.5.1; control AC-02. Reviewed at least annually and at every management review (`17`). Closes internal audit finding A-07.

## 1. Commitment

Nocturne Systems builds software that holds other people's records — students' identity documents, their marks, their counselling notes, staff salaries. Those people did not choose us and cannot inspect what we do. That asymmetry is the reason this policy exists.

The company commits to:

1. **Protect** the confidentiality, integrity and availability of information in the scope of `13-isms-scope.md`.
2. **Satisfy** applicable legal, regulatory and contractual requirements — today PECA 2016 as amended 2025, the Digital Nation Pakistan Act 2025 and what the Pakistan Digital Authority notifies under it, and each client's contract — and to build ahead of Pakistan's forthcoming data protection statute.
3. **Continually improve** the ISMS, through measured objectives (`17` §4), internal audit (`16`), risk management (`14`) and management review (`17`).
4. **Tell the truth about our security posture.** No claim of compliance or certification is made that is not literally true and evidenced. The approved wording is the table in `00-PROGRAM.md` and nothing else.

Point 4 is a security control, not marketing hygiene: a client who believes a control exists when it does not will make decisions — about what data to put in the system, and what to tell their own students — that they would not otherwise make.

## 2. Scope

As defined in `13-isms-scope.md`. Everyone who works for or with the company is bound by it, including the founder.

## 3. Principles

| # | Principle | What it means in practice |
|---|---|---|
| P-1 | Least privilege, by category of data | Access is granted to a category of personal data, not to a vague "system" role (`15` A.8.3) |
| P-2 | Default deny | A request with no explicit grant is refused |
| P-3 | Evidence over assertion | A control is "implemented" when a test, a log, a CI job or a dated exercise proves it. A policy alone proves nothing |
| P-4 | Data minimisation and limited retention | Only what the university needs, only as long as `09-retention-policy.md` allows, deleted by a mechanism that is named and tested |
| P-5 | The client owns their data | Under the standard deployment the university holds the data and the keys. Nocturne has no standing access; support access is per incident, time-limited, logged and deleted (`07` §7) |
| P-6 | Secure by default | The shipped default is the safe one. A less safe option must be chosen deliberately and is disclosed |
| P-7 | Accountability | Every action on a sensitive record is attributable to a person, and the trail cannot be quietly edited |
| P-8 | Report without fear | Raising a suspected incident is always correct. The disciplinary process applies to concealment, never to reporting (`18` §2) |

## 4. Requirements

1. **Risk.** Risks are assessed by the method in `14`, treated per its acceptance criteria, and a residual risk above Medium is retained only with the owner's written acceptance.
2. **Controls.** The applicable control set is `15`, mapped to the unified set in `04`. A control is not silently dropped; it is changed through a management review decision.
3. **Personal data.** Handled per `06-privacy-notice.md`, `08-ropa.md`, `09-retention-policy.md` and `10-minors-and-consent.md`. New personal data requires `01` and `08` to be updated before the feature ships.
4. **Incidents.** Handled per `11-breach-notification.md`, to its clock.
5. **Vulnerabilities.** Handled per `12-vulnerability-register.md`, to its windows. A shipped release does not carry a known Critical or High advisory.
6. **Suppliers.** Engaged per `07-dpa-template.md` and the supplier register; a new sub-processor is disclosed before it processes anything.
7. **Change.** Reaches `main` only through CI gates. Bypassing a gate is a nonconformity and is recorded as one.
8. **People.** Trained before access and annually (`18`); joiners and leavers follow `18` §4.
9. **Continuity.** Backups are encrypted and a restore is proven; the drill record is kept.

## 5. Responsibilities

The Data Protection Contact (`05-governance.md` §1) owns this policy, the ISMS, and every decision recorded in it. Where a single person cannot satisfy a control that assumes several — segregation of duties, independent audit — the limitation is written down (`15` A.5.3, `16` §1) and compensating controls are named. It is never scored away.

## 6. Compliance with this policy

Conformance is checked by the internal audit programme (`16`) and reported to management review (`17`). A breach of this policy by a person is handled under `18`; a breach by the system is an incident under `11`.

## 7. Related documents

`00-PROGRAM.md` (claims and legal register) · `01`–`04` (inventory, flows, residency, control set) · `05`–`12` (governance, notice, DPA, ROPA, retention, minors, breach, vulnerabilities) · `13`–`18` (ISMS scope, risk, SoA, audit, review, training) · `SECURITY.md`.

## 8. Approval and review

| Version | Date | Approved by | Change |
|---|---|---|---|
| 1.0 | 2026-09-22 | Abdul Nafay Sarmad, Founder | First issue; closes audit finding A-07 |

Next review: 2026 Q4 management review, and at any change of scope, ownership or applicable law.
