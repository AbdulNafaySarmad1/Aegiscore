# 17 — Management review

**Version 1.0 · 2026-09-22.** ISO/IEC 27001:2022 clause 9.3.

## 1. Cadence and participants

Quarterly, and after any S1 incident or failed audit. Chaired by the DPC (`05-governance.md`), who is currently also the management — the review is still held and still minuted, because a certification body checks that it happens and what it decided, and because the discipline of sitting down with the numbers is the point.

Records: one file per review in `docs/compliance/reviews/<yyyy-qn>.md`, never deleted.

## 2. Fixed agenda (clause 9.3.2 — every item, every time)

| # | Input | Source |
|---|---|---|
| 1 | Status of actions from the previous review | previous minute |
| 2 | Changes in external and internal issues relevant to the ISMS | `00-PROGRAM.md` legal register; headcount; new deployment models |
| 3 | Changes in interested-party needs | `13-isms-scope.md` §6; client and HEC/PDA developments |
| 4 | Feedback on information security performance: nonconformities and corrective actions; monitoring and measurement results; audit results; fulfilment of objectives | `16` audit reports, `12` register, `SecurityAlerts`, CI history, §3 metrics |
| 5 | Feedback from interested parties | client correspondence, vulnerability reports, procurement questionnaires |
| 6 | Risk assessment results and risk treatment status | `14` |
| 7 | Opportunities for continual improvement | anywhere |
| 8 | Decisions: changes to the ISMS, to controls, to resources | minuted with owners and dates |

## 3. Metrics reviewed each quarter

Measured, not estimated. Each has a source that can be re-run.

| Metric | Source | Target |
|---|---|---|
| Open Critical/High risks | `14` | 0 Critical; High only with written acceptance |
| Open Major audit findings | `16` reports | 0 beyond 90 days |
| Vulnerabilities open past their window | `12` | 0 |
| Days since the last successful restore drill | CI history / `exercises/` | ≤ 7 (CI runs every push) |
| Security alerts raised, and how many were reviewed | `SecurityAlerts` table | 100 % reviewed |
| Incidents, and notification windows met | `incidents/` | 100 % within the `11` clock |
| Controls Implemented / Partial / Planned in the SoA | `15` | Implemented rising; no regression |
| NIST CSF function scores | `04` scorecard | no function regressing |
| Tabletop exercises in the last 12 months | `exercises/` | ≥ 2 |
| Training completed | `18` | 100 % of people |

## 4. Information security objectives (clause 6.2)

| # | Objective | Measure | Target date |
|---|---|---|---|
| O-1 | No shipped release carries a known Critical or High dependency advisory | `12` register; CI gate | Continuous, from 2026-09-20 |
| O-2 | A restore is proven at least weekly | CI drill | Continuous, from 2026-09-20 |
| O-3 | Every sensitive read and write is audited | `15` A.8.15 evidence | Achieved 2026-09-20; hold |
| O-4 | Master-key rotation exists | `rotate-master-key` shipped | 2026-12-22 |
| O-5 | Certification-ready: no Major finding open against the SoA | full-scope internal audit | End of Phase 5 |
| O-6 | ISO/IEC 27001 Stage 1 completed | certification body report | Phase 6, subject to auditor scheduling |

## 5. First review

Held 2026-09-22 — minute in `reviews/2026-q3.md`.
