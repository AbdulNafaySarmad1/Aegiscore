# Codex Handoff — AegisCore Deployment Prompts

Updated: 2026-09-08. Repository: `/home/ubuntu/Aegiscore`, branch `main`.

## Prompt status

Completed prompts: **P1–P17**.

Current prompt: **P18–P19 — code complete and pushed (`bcf49bf0`); the 18A/18B capture
artifacts are the only outstanding item and are blocked on a live install with real
role credentials.**

Remaining execution order: run `capture:screenshots` + `capture:demo` against a live
`APP_URL` with `ADMIN/FACULTY/STUDENT_*` + `AEGISCORE_SETUP_TOKEN` +
`DEMO_COURSE_ID`/`DEMO_STUDENT_NUMBER`/`DEMO_MARKS` (see `docs/CAPTURE_SUITE.md`), then
validate the private-client deployment against the published image.

## Completed prompt commits

- P1: `57e4b56b` (compile fixes), `a1032a24` (schema-aligned tests)
- P2: `6a6ecf05`
- P11: `187970a2`
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
- P17 is verified in `dacb1435`; P11 is verified in `187970a2`.

## Exact next action

P1–P17 remain complete. P18–P19 changes are uncommitted and must not be marked complete:

- User approved adding `client-release.yml` because `release.yml` already exists and existing workflows must remain unchanged. Also approved production login, missing UI routes, and redundant client deployment work.
- Added a cookie-based Portal API proxy, production session loading, students/admissions/reports/settings/dashboard routes, grade submission limited to edited cells, and an admin geolocation check-in control.
- Extended the existing Playwright config with screenshot/demo modes. See `docs/CAPTURE_SUITE.md` for environment inputs and real fixture requirements. No role credentials or APP_URL were available; `screenshots/` and `recordings/` have not been generated.
- Current wizard order remains identity (including branding/templates), database, SSO, permissions, AI, import, review. This differs from the requested conceptual ordering; captures faithfully use the implemented seven-step wizard.
- Added `docker-compose.client.yml`, combined runtime packaging, health-checked ingress, two application replicas, Watchtower peer checks/draining/readiness waits, and client registry/deployment docs. Database/Redis/TLS are client-owned prerequisites prepared by Nocturne; schema changes and ingress maintenance are not automatically orchestrated by Watchtower.
- Licensing now validates on startup and every 24 hours, isolates installation lookup, restores valid status, consumes Basic/Standard/Premium tiers, and gates AI/proctoring/plagiarism. Middleware permits reads during lockdown; an EF interceptor blocks business SaveChanges from requests and background work. Existing offline validation/overrides are preserved.
- Final backend verification: 638 passed / 1 skipped / 0 failed (`dotnet test AegisCore.slnx -c Release --no-build -v quiet`); focused licensing 18 passed. Portal typecheck and lint pass. Client compose parses, has zero build directives, and existing workflows have no diff. DEPLOY.md prints on one A4 page at 11pt.
- Final Docker image build: PASS (`aegiscore-client:verification`). Image runs as non-root, contains API and standalone Portal, and inspection found no first-party C# source, PDBs, source maps, or installed .NET SDK. HAProxy configuration validates. Packaged Portal smoke checks: login 200, readiness without backend 503, cross-origin mutation 403. The isolated smoke container was removed.
- Final combined browser check: PASS, 10/10 with no retries (`portal-auth`, `keyboard-accessibility`, `grade-entry`, CLI `--timeout=120000`). The preceding run passed 9/10 but cold dashboard compilation exceeded 60 seconds on this 4 GB host; the final dashboard check completed in 2.6 seconds. This is isolated mocked UI regression evidence, not the requested real-role capture evidence.
- Graphify code refresh: PASS, 26,830 nodes, 52,085 edges, 1,332 communities. Existing community labels may need a separate semantic relabel; no LLM relabel was performed. Final `git diff --check` passes.
- Do not claim successful screenshots, a sub-four-minute demo, private registry publication/pull, or uninterrupted live updates until those artifacts/checks actually exist. As of `bcf49bf0` the P18–P19 code is pushed to `origin/main`; no registry publication (tagged image build/push) has been performed.

### P18–P19 follow-up fixes (session 01S3NT3haQuhMacUnpd7XTfe)

- `19A` — `release.yml` stays untouched (pre-existing push-to-`main` internal build: `aegiscore-api`/`aegiscore-portal` images + GitHub Release). The `v*.*.*` tag publisher is `.github/workflows/client-release.yml`, now hardened: header comment on the deliberate rename, `workflow_dispatch` with a regex-validated `tag` input and an `allow_bootstrap` boolean, one tag-resolution step feeding `:{tag}` + `:latest`, and the "require private package" guard skipped only when `allow_bootstrap=true`. `docs/CLIENT_REGISTRY_ACCESS.md` has a new "Release automation" section mapping this file to 19A and the first-publish bootstrap.
- `18A/18B` routes — `/grades` and `/timetable` have no page root; `next.config.ts` already redirects them to `/examinations` and `/classes` (now commented as intentional, kept in sync with the suite `aliases` map). `screenshots.spec.ts` now positively asserts the alias redirect and records it in an `Alias` column in `INDEX.md`; `demo.spec.ts` asserts `/grades → /examinations` before grade entry (flow otherwise unchanged). `fixture-server.mjs` needed no change — the alias target pages' on-load endpoints are all already stubbed.
- Synthetic capture quarantined — `CAPTURE_MODE=visual-demo` (`visual-demo.spec.ts`) now writes only to `screenshots/_synthetic/` and `recordings/_synthetic/` (`aegiscore-demo-synthetic.mp4`); it no longer overwrites `screenshots/INDEX.md`, `recordings/aegiscore-demo.*`, `run.json`, or `README.md`. `docs/CAPTURE_SUITE.md` documents the alias behaviour and the synthetic/real split. Stale local `screenshots/*` and `recordings/*` artifacts (incl. the old `aegiscore-demo.mp4` and `aegiscore-student-lifecycle.webm`) were deleted; both dirs are gitignored.
- Verification: `client-release.yml` YAML parses; `ci.yml`/`release.yml`/`deploy.yml`/`deploy-cloud.yml` have zero diff; `docker-compose.client.yml` still has zero `build:` directives; no new backend `.cs` changes; Portal `npm run typecheck` passes; `playwright test --list` resolves for `screenshots`, `demo`, and `visual-demo` modes. The full synthetic `visual-demo` e2e and the real role-credential capture run were not executed here.
- Committed and pushed: `bcf49bf0` `feat(p18-p19): capture suite, closed-source client delivery, license tiers` → `origin/main` (`f91b2ba4..bcf49bf0`). 65 files.
- Graphify refreshed with `graphify update .`: **26,862 nodes · 52,114 edges · 1,369 communities**, built from `bcf49bf0`, backup written to `graphify-out/2026-09-08/`. 281 communities were auto-renamed by hub; `graphify label` (LLM relabel) was not run — no `GEMINI_API_KEY`/`GOOGLE_API_KEY` available. This handoff-doc refresh follows in a second commit.

### Completion-checklist status (P18–P19)

| Item | Status |
| --- | --- |
| CODEX_HANDOFF.md read before code | done |
| Graphify queried + summarised | done |
| All paths verified before use | done |
| 18A screenshots `screenshots/{role}/{page}.png` (3 roles × 10+ pages) | **blocked** — suite complete (`tests/capture/screenshots.spec.ts`); needs live `APP_URL` + `ADMIN/FACULTY/STUDENT_*`; run externally ordered |
| 18A `screenshots/INDEX.md` | **blocked** — generated by the same run |
| 18B `recordings/aegiscore-demo.webm` (<4 min) | **blocked** — suite complete (`tests/capture/demo.spec.ts`); same prerequisites |
| 19A tag-triggered release workflow | done **as `client-release.yml`** (documented deviation; `release.yml` frozen per "add only") |
| 19B `docs/CLIENT_REGISTRY_ACCESS.md` | done |
| 19C `docker-compose.client.yml` zero `build:` | done (grep-verified) |
| 19D heartbeat wired (not assumed) + `LicenseTier` | done — `ProductionServicesExtensions.cs:247`, `LicenseService.cs` (heartbeat URL + tier parse), `LicenseLockoutMiddleware.cs` (premium gate), `LicenseContracts.cs` (`LicenseTier` enum) |
| 19E `DEPLOY.md` one page | done |
| No existing CI workflows modified | done (zero diff on `ci`/`release`/`deploy`/`deploy-cloud`) |
| No dev services / source mounts in client compose | done |
