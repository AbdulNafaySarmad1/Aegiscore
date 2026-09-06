# Codex Handoff — AegisCore Deployment Prompts

Updated: 2026-09-06. Repository: `/home/ubuntu/Aegiscore`, branch `main`.

## Prompt status

Completed prompts: **P1–P17**.

Current prompt: **None — all required prompts are implemented and verified**.

Remaining execution order: **None**.

## Completed prompt commits

- P1: `57e4b56b` (compile fixes), `a1032a24` (schema-aligned tests)
- P2: `6a6ecf05`
- P11: this completion change set (multi-dialect adapters, migrations, CI, and verification)
- P10: `dd657541`
- P3A: `e5daf47a`
- P3B: `b7b314bf`
- P5: `6480fba1`
- P4: `3a2f1014`
- P6: `f32f7d16`
- P7: `318ac5df`
- P8: `56f397b4`
- P9: `16e6dc7f`
- P12: `b9d16168`
- P13: `9ffd7caf`
- P14: `cdfe8c90`
- P15: `bf8779ec`
- P16: `c5be80d4` (core compliance)
- P16 appendix: `202008ce`
- P17: `dacb1435`

## P11 verified implementation

- Added `DB_DIALECT` and PostgreSQL/MySQL/SQL Server/Oracle adapters for runtime registration, design-time context creation, enum/JSON/binary/text mapping, health/readiness, and wizard connections.
- Implemented per-dialect migration assemblies and stable generated baselines for MySQL, SQL Server, and Oracle; PostgreSQL retains its existing migration history.
- Added the four-provider wizard selector, persisted dialect/connection formats, exact provider version queries, validation, environment/deployment settings, unit tests, and a real-container CI migration matrix.
- All 46 historical Finance migrations and all 40 historical RLS migrations contain the P11 portability note. PostgreSQL RLS remains defense in depth through a PostgreSQL-only adapter interceptor; global `ITenantScoped` filters are active for every provider.
- All non-PostgreSQL snapshots contain zero `jsonb` and zero `bytea`; all four providers report no pending model changes. SQL Server cascades use `NO ACTION`, and Oracle default decimals have stable canonical metadata.
- Focused dialect/environment coverage passes 21/21, including adapters, exact version queries, model mappings, aliases, connection formats, SQL Server cascades, Oracle decimals, and MySQL non-TLS authentication.
- Real-engine verification passes: PostgreSQL 17 applies the complete historical chain; MySQL 8.4, SQL Server 2022, and Oracle Free 23 apply their baselines from empty databases.
- The PostgreSQL run exposed and verified a repair to `AddTenantId_PolicyConfig`: add nullable, backfill system-template rows to `Guid.Empty`, then enforce non-null.
- A security gate rejected generating a migration that would remove already-deployed PostgreSQL RLS. Do not bypass that gate. The implemented defense-in-depth compatibility preserves security and cross-provider behavior.

No P11 implementation work remains.

P13 includes tenant-defined wizard roles/permission matrix, two-argument `RequirePermission`, tenant-scoped Casbin policy sync with five-minute cache, dynamic JWT lifetime, shared read-only SQL allow-list, and tests. There are zero `[Authorize(Roles=...)]` controller attributes and 105 `RequirePermission(...)` controller attributes after P16.

## P14 verified implementation

- Added root/page error infrastructure:
  - `src/components/ErrorBoundary.tsx`
  - `src/components/ErrorDisplay.tsx` with `WizardError`, retry, fix guidance, and technical-details toggle
  - `src/app/error.tsx`, `global-error.tsx`, `not-found.tsx`
  - high-risk boundaries under `finance/`, `examinations/`, `admissions/`, and `grades/`
  - root layout wraps all 43 page roots in `ErrorBoundary`
- Added `TenantPreferencesProvider`/formatter, backed by authenticated `GET /api/config/portal-preferences`.
- Extended `TenantConfigService` defaults/getters for locale, currency, payment gateway, accreditor name, and identity-document name.
- Added a 768–1023px tablet CSS tier: persistent collapsed sidebar, two-column dashboards, one-column forms, card-style tables, 90%-width dialogs, and 44px touch targets. Mobile behavior now starts below 768px. `AppShell` defaults the tablet sidebar to collapsed and exposes a mobile menu button.
- Replaced tenant-specific formatting/configuration in finance dashboard, department finance, finance operations, student finance, payment gateway selection, cafeteria, placement, freezes, transport, defaulter forecast, and visiting-faculty labels/amounts.
- Removed the literal student fee balance and replaced the transcript's country-specific sample course.
- Replaced generic page-load failures with `ErrorDisplay`, actionable retry behavior, module-specific copy, descriptive loading messages, and informative empty states that point to their associated page action.
- Added `tests/tablet-responsive.spec.ts` and `docs/P14_VIEWPORT_RESULTS.md`; all three required viewport checks pass.
- Portal lint now has zero errors. Generated Playwright reports are excluded from the lint surface.

## P15 verified implementation

- Added encrypted-at-rest attendance coordinates, tenant-scoped campus polygons, location statuses, manual overrides, and migration `AddGeolocationToAttendance`.
- Added boundary-inclusive ray casting and six focused tests, including a SQLite-backed check-in/roster/override integration path.
- Added permission-protected check-in, teacher override, roster, student-course, and campus-polygon management APIs.
- Added student mobile check-in, teacher status/filter/override UI, and an administrator map-based campus editor.
- Fixed the enum persistence sentinel found by the integration test, plus onboarding template/RBAC regression tests exposed by the full suite.

No P15 implementation work remains.

## P16 verified implementation

- Added tenant-scoped FERPA audit, consent-ledger, and erasure-request schemas plus migration `AddFerpaGdprCompliance`; IP addresses are encrypted at rest.
- Added `RequireFerpaAudit` enforcement on grade, attendance, transcript, and admission record endpoints. It audits allowed and denied access, validates admin/self/assigned-faculty/applicant relationships, and returns the specified 403 denial message.
- Added the permission-protected, filterable, paginated `GET /api/admin/ferpa-audit` report with human-readable summaries.
- Added student directory opt-out enforcement in search results, indexing, and full index rebuilds.
- Added consent record/revoke APIs, user-or-admin erasure requests with 30-day deadlines, transactional cascade anonymization, active-session/profile-photo cleanup, and retained anonymized academic facts.
- Added on-demand five-file DSAR ZIP exports with 24-hour expiry metadata and no location coordinates or encrypted IP values.
- Added pending-erasure processing and tenant-local 02:00 retention with a configurable six-year default.
- Added focused coverage for allowed/denied FERPA access, encrypted IP storage, complete erasure, DSAR contents/expiry, retention windows, and directory opt-out.
- Added `docs/FERPA_GDPR_COMPLIANCE.md` documenting the modules as institutional opt-ins, operational prerequisites, and legal limitations.
- Disclosed the exact licence-heartbeat payload and added locally signed offline validation via `License__OfflineMode`/`LICENSE_OFFLINE_MODE`; verified that offline mode creates no HTTP client.
- Made Cloudflare Tunnel an explicit setup choice that defaults off; direct university ingress/DNS remains supported and no empty tunnel variable is emitted.

No P16 implementation work remains.

## P17 verified implementation

- Added `@axe-core/playwright`, a PR/push CI accessibility job, exact coverage for the ten required high-risk URLs, and uploaded JSON/Playwright evidence.
- Audited all 45 current `page.tsx` roots using WCAG 2.1 A/AA tags; the final no-retry scan passes 45/45 with zero automated violations.
- Documented and fixed 20 pre-remediation page-rule findings: 4 critical and 16 serious findings across 39 affected nodes.
- Added global skip navigation, visible focus styles, reduced-motion handling, landmark/control semantics, table scope, form error relationships, and corrected AA colour tokens.
- Added a reusable modal focus trap with initial focus, Tab/Shift+Tab wrapping, Escape close, and trigger restoration.
- Added six Playwright keyboard workflows covering login, dashboard, grade entry, attendance, all seven wizard steps, and data import.
- Replaced the inaccurate accessibility statement and added complete axe and keyboard audit evidence.

No P17 implementation work remains.

## Current verification status

- `dotnet test AegisCore.slnx -c Release --no-build`: **PASS**, 626 passed, 1 skipped, 0 failed. The skip is `SecurityHardeningTests.OversizedFileUpload_RejectedAtBoundary`.
- P16 focused compliance tests: **PASS**, 4/4.
- P16 appendix licensing/infrastructure tests: **PASS**, 7/7.
- Portal `npm run typecheck`: **PASS**.
- Full no-retry WCAG audit: **PASS**, 45/45 current Portal page roots.
- Exact CI accessibility command: **PASS**, 10/10 required axe routes and 6/6 keyboard workflows.
- `@axe-core/playwright` reports: **PASS**, zero violations after remediation.
- Portal `npm run build`: **PASS**, 46 generated routes including `/admin/campus` and `/student/attendance`.
- Portal `npm run lint -- --quiet`: **PASS**, 0 errors.
- P11 focused dialect/environment tests: **PASS**, 21/21.
- Four real database migration runs: **PASS** (PostgreSQL 17 full history; MySQL 8.4, SQL Server 2022, Oracle Free 23 baselines).
- Four `migrations has-pending-model-changes` checks: **PASS**.
- CI workflow YAML parse: **PASS**, 5 jobs including the four-dialect matrix.
- Graphify refresh/query: **PASS**, 26,714 nodes and 51,852 edges.
- Portal tablet Playwright suite: **PASS**, 3/3 required viewport tests.
- `git diff --check`: **PASS**.
- EF `migrations has-pending-model-changes`: **PASS**, no pending model changes.

## Architecture and workflow constraints

- Root `AegisCore.slnx` is canonical; `aegiscore-backend/` was deleted in P10.
- Read/query `graphify-out/` before each prompt. The verified installed-CLI refresh command is `graphify update .`.
- Finish one prompt, run Release build/tests, refresh Graphify, commit, and push before starting the next.
- Preserve client-owned infrastructure and introduce no Nocturne-hosted dependency.
- `AEGISCORE_FINAL_17_PROMPTS.md` is user-confirmed source of truth and must be committed with the repository.
- P17 is verified in `dacb1435`; P11 changes are uncommitted until all provider checks pass.

## Exact next action

Commit and push this verified P11 completion change set; no prompt implementation remains afterward.
