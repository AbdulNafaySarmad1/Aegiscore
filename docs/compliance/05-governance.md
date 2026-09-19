# 05 — Governance: accountable people and decisions

**Phase 2 · 2026-09-20.** Controls AC-01, AC-03. Items marked **[DEFAULT — confirm]** were set by the compliance build-out because no decision existed; they stand until someone changes them here.

## 1. Data protection point of contact (AC-01)

| Role | Holder | Contact | Basis |
|---|---|---|---|
| Data Protection Contact (DPC) — accountable for this programme, DSARs, breach decisions | **Abdul Nafay Sarmad, Founder, Nocturne Systems (Pvt.) Ltd.** [DEFAULT — confirm] | security@ — **mailbox to be created**; until then the founder's address | The brief says "likely you, initially"; the founder is the only named officer in the repository (`docs/06`, `README.md`) |
| Deputy | none — single-person company | | Record when a second person joins |
| Security incident lead | same person | | See `11-breach-notification.md` |

This is not a statutory DPO appointment (no Pakistani law requires one yet). It is a named, accountable person so that a university, HEC, the PDA or the NCCIA has someone to write to.

## 2. Roles and responsibilities (AC-03)

| Responsibility | Who | Evidence it happens |
|---|---|---|
| Approve policies in this folder | DPC | Version line at the top of each document |
| Run the legal re-check at each phase start | DPC | `00-PROGRAM.md` legal section, dated |
| Triage a security report within 2 business days | DPC | `SECURITY.md` (Phase 3) |
| Decide breach notification within the window in `11` | DPC | Incident record |
| Review access rights per tenant | the university's administrator (controller) — Nocturne has no access under Model A | DPA §6 |
| Merge to `main` | DPC. Rule from Phase 3: every change to `AegisCore.Infrastructure/Security`, `Compliance`, `Identity`, `Backup`, `API/Authorization`, `API/Middleware` requires a second look before merge — for a single-person team that means the PR checklist in `.github/pull_request_template.md` completed and CI green, and the PR left open for at least 24 h unless it is a security fix | PR history |
| Management review | DPC, quarterly | `docs/compliance/reviews/` (Phase 4) |

## 3. Decisions taken in Phase 2

| # | Decision | Value | Status |
|---|---|---|---|
| D-A | Hosting region for the vendor-hosted model (Model B) | **Model B is restricted to demonstrations and non-production pilots with synthetic data until a Pakistani hosting region is chosen and recorded in `03-data-residency.md`.** The Hetzner EU references in `docs/06` are not an approved production location. | [DEFAULT — confirm] |
| D-B | Retention periods | As set in `09-retention-policy.md` | [DEFAULT — confirm each period] |
| D-C | GDPR / FERPA posture | "aligned architecture" only; no compliance claim; reassess when an EU or US data subject enters the pipeline | Decided (Phase 1) |
| D-D | AI provider default | `ollama` (on-premise) is the only default; `openai` requires the university to accept the sub-processor disclosure in the wizard (Phase 3 wording) | Decided |
| D-E | Licence heartbeat | Stays on by default; payload fixed at five non-personal fields; offline mode documented | Decided (existing) |
| D-F | Backups | One mechanism: `deploy/scripts/backup.sh` + `restore.sh`, always encrypted, restore drill in CI on every push (Phase 3). The in-app `BackupService` becomes a scheduler around the same scripts or is removed | Decided |

## 4. Documents produced in Phase 2

| Document | Control |
|---|---|
| `06-privacy-notice.md` — notice for the product's actual data practices, per deployment model | AC-08 |
| `07-dpa-template.md` — Data Processing Agreement addendum for university clients | AC-09, AC-06 |
| `08-ropa.md` — Record of Processing Activities | AC-07 |
| `09-retention-policy.md` — retention and deletion with the actual delete path per category | AC-38 |
| `10-minors-and-consent.md` — children's data and consent handling design | AC-39, AC-40 |
| `11-breach-notification.md` — incident and breach procedure | AC-61 |
