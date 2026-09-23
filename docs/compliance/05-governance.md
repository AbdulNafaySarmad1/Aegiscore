# 05 — Governance: accountable people and decisions

**Phase 2 · 2026-09-20, revised 2026-09-23.** Controls AC-01, AC-03. The three decisions a supplier cannot make for an institution — its data protection contact, where its data lives, and how long it keeps records — are **settings each installation owns and may change at any time**, not values in this document; see `21-configurable-decisions.md`.

## 1. Data protection point of contact (AC-01)

**This is a setting, not a value in this document.** Each installation names its own contact at
**Administration → Compliance** (`compliance.dpc_*`); see `21-configurable-decisions.md`. The product
ships it empty and reports it as an unmet obligation until the institution fills it in, because
naming an accountable person for someone else's institution is not something a supplier can do.

| Field | Where it lives |
|---|---|
| Name, role, email, phone, postal address, deputy | Compliance settings, editable at any time by anyone holding `Settings.Write` |
| Published to data subjects | `06-privacy-notice.md`, filled from the setting |
| Used in breach correspondence | `11-breach-notification.md` |

**For Nocturne itself** (the supplier's own ISMS, `13-isms-scope.md`), the contact is the founder,
who holds every role in a one-person company. That is a fact about Nocturne, not a default imposed
on any client.

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
| D-A | Hosting region for the vendor-hosted model (Model B) | **A setting per installation** (`compliance.deployment_model`, `compliance.database_location`, …). Nocturne's own position: Model B is restricted to demonstrations and non-production pilots with synthetic data until a region is chosen and recorded. The Hetzner EU references in `docs/06` are not an approved production location. | Vendor position decided; per-installation value is the institution's |
| D-B | Retention periods | **Settings per installation**, with shipped starting values (`21-configurable-decisions.md`). `09-retention-policy.md` documents the mechanism enforcing each | Mechanism decided; periods are the institution's |
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
