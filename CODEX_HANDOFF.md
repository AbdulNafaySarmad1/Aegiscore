# Codex Handoff — AegisCore Deployment Prompts

Updated: 2026-09-05. Repository: `/home/ubuntu/Aegiscore`, branch `main`.

## Prompt status

Fully completed through **P15** (15/17); P15 is recorded in the commit containing this handoff.

Next prompt: **P16 — Accessibility**.

Remaining execution order: **P16 → P17 → P11 (last)**. Do not start P11 before the other prerequisites/prompts are complete.

## Completed prompt commits

- P1: `57e4b56b` (compile fixes), `a1032a24` (schema-aligned tests)
- P2: `6a6ecf05`
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
- P15: current commit

P13 includes tenant-defined wizard roles/permission matrix, two-argument `RequirePermission`, tenant-scoped Casbin policy sync with five-minute cache, dynamic JWT lifetime, shared read-only SQL allow-list, and tests. There are zero `[Authorize(Roles=...)]` controller attributes and 104 `RequirePermission(...)` controller attributes after P15.

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

## Current verification status

- `dotnet build AegisCore.slnx -c Release --no-restore`: **PASS**, 0 errors, 20 warnings (existing analyzer/package warnings).
- `dotnet test AegisCore.Tests/AegisCore.Tests.csproj -c Release --no-build`: **PASS**, 603 passed, 1 skipped, 0 failed. The skip is `SecurityHardeningTests.OversizedFileUpload_RejectedAtBoundary`.
- Portal `npm run typecheck`: **PASS**.
- Portal `npm run build`: **PASS**, 46 generated routes including `/admin/campus` and `/student/attendance`.
- Portal `npm run lint -- --quiet`: **PASS**, 0 errors.
- Portal tablet Playwright suite: **PASS**, 3/3 required viewport tests.
- `git diff --check`: **PASS**.
- EF `migrations has-pending-model-changes`: **PASS**, no pending model changes.

## Architecture and workflow constraints

- Root `AegisCore.slnx` is canonical; `aegiscore-backend/` was deleted in P10.
- Read/query `graphify-out/` before each prompt. The verified installed-CLI refresh command is `graphify update .`.
- Finish one prompt, run Release build/tests, refresh Graphify, commit, and push before starting the next.
- Preserve client-owned infrastructure and introduce no Nocturne-hosted dependency.
- `AEGISCORE_FINAL_17_PROMPTS.md` is user-confirmed source of truth and must be committed with the repository.
- P15 is verified and included in the current commit; the worktree should be clean before beginning P16.

## Exact next action

Query the refreshed graph for P16 accessibility implementation, read P16 in full, then implement and verify it before committing and pushing.
