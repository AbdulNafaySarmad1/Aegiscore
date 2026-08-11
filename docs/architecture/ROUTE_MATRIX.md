# AegisCore ROUTE_MATRIX.md

Repository Intelligence artifact. Generated 2026-08-11, branch `agent/prod-hardening`.
Frontend: `AegisCore.Portal` (Next.js 16 App Router, React 19). Legacy `aegiscore-frontend` is a stale Vite/React 18 shell and is NOT the portal.

## 1. Routing model

- App Router under `src/app/**/page.tsx`. No `middleware.ts`, no `loading/error/not-found.tsx`.
- Auth gating is **client-side** (AppShell + pages). Role gating is menu visibility only.
- API proxied via `next.config.ts` rewrites: `/api/*`, `/health/*`, `/hubs/*` → `AEGISCORE_API_INTERNAL_URL ?? "http://api:8080"`.
- Locale: `next-intl`, cookie `AEGISCORE_LOCALE` (en-PK/en-US/en-GB/ar-AE/ms-MY), RTL for ar-AE.

## 2. Route inventory (portal)

| Route | File | Shell | Client API calls |
|---|---|---|---|
| `/` | `src/app/page.tsx` | AppShell | `/api/session` → `/api/student-portal/me` → [attendance/history, finance/v2/student-portal/:id] (3-deep waterfall) |
| `/login` | `src/app/login/page.tsx` | standalone | POST `/api/auth/login`; demo-mode one-click accounts |
| `/setup` | `src/app/setup/page.tsx` | WizardShell | `/api/wizard/*`, `/api/setup/*` |
| `/onboarding`, `/completion`, `/imports` | pages → SetupWizard | none/AppShell | wizard endpoints (stub aliases) |
| `/attendance` | `src/app/attendance/page.tsx` | AppShell | `/api/courses/my`, `/api/attendance/roster/:course`, POST `/api/attendance/mark` |
| `/examinations` | `src/app/examinations/page.tsx` | AppShell | `/api/courses/my`, `/api/faculty-grade-entry/course/:id`, marks PUT, import preview/commit, template.csv |
| `/grade-approvals` | `src/app/grade-approvals/page.tsx` | AppShell | `/api/grade-approvals`, `/api/faculty-grade-entry/approvals/:id/review` |
| `/obe` | `src/app/obe/page.tsx` | AppShell | `/api/obe/faculty/overview`, `/api/obe/v2/cis/*`, signed-report PDF |
| `/finance` | `src/app/finance/page.tsx` | AppShell | serverGet `/api/finance/v2/dashboard/executive`, `/security-posture`; client verify vouchers/:id |
| `/finance/operations` | `src/app/finance/operations/page.tsx` | AppShell | serverGet dashboard/operations; POST voucher decision |
| `/finance/gateways` | `src/app/finance/gateways/page.tsx` | AppShell | gateways CRUD + health-check |
| `/finance/department/[departmentId]` | `.../department/[id]/page.tsx` | AppShell | serverGet dashboard/department/:id |
| `/audit` | `src/app/audit/page.tsx` | AppShell | serverGet audit/dashboard, sod-rules; POST engagements, soc2-evidence |
| `/auditor` | `src/app/auditor/page.tsx` | AppShell | `/api/auditor/*` (opaque token) |
| `/classes` | `src/app/classes/page.tsx` | AppShell | session; faculty: courses/my + attendance/roster; admin: classes/offerings, sections, generate |
| `/grade-appeals` | `src/app/grade-appeals/page.tsx` | AppShell | appeals CRUD/classify/process; FormData upload (hardcoded facultyRespondentId zero-GUID) |
| `/grade-distributions` | `src/app/grade-distributions/page.tsx` | AppShell | courses, anomalies, analyze |
| `/freezes` | `src/app/freezes/page.tsx` | AppShell | `/api/freezes` CRUD, decide, return |
| `/prerequisite-conflicts` | `src/app/prerequisite-conflicts/page.tsx` | AppShell | list/resolve/detect/detect-circular/simulate |
| `/proctoring` | `src/app/proctoring/page.tsx` | AppShell | sessions/integrity/events/face-analysis/lockdown/validate |
| `/convocations` | `src/app/convocations/page.tsx` | AppShell | convocations, attendees, checkin, eligibility, certificates/generate |
| `/document-vault` | `src/app/document-vault/page.tsx` | AppShell | dashboard, seed-tiers, raw POST upload (no auth header — relies on cookies; hardcoded uploadedByUserId zero-GUID) |
| `/data-import` | `src/app/data-import/page.tsx` | AppShell | connections/test/tables/columns/jobs, formats/:name CSV templates |
| `/replication`, `/sync-lag` | pages | AppShell | admin/replication dashboard/config/scale, admin/sync-lag dashboard/config/commit/correct/revert (10s polling) |
| `/catalog`, `/readiness`, `/placement`, `/voice`, `/board`, `/elections`, `/achievements`, `/mental-health`, `/foreign-students`, `/visiting-faculty`, `/defaulter-prediction`, `/cafeteria`, `/transport`, `/plagiarism` | pages | AppShell | respective module endpoints |
| `/profile` | `src/app/profile/page.tsx` | AppShell | `/api/me/profile`, `/api/profile-pictures/upload`, avatar fetch (raw fetch + Bearer) |
| `/student/dashboard` | `src/app/student/dashboard/page.tsx` | AppShell | me, attendance/history, finance/v2/student-portal/:id |
| `/student/attendance` | `.../student/attendance/page.tsx` | AppShell | student-attendance history/check-in (geolocation) |
| `/student/courses` | `.../student/courses/page.tsx` | AppShell | registration GET+POST |
| `/student/examinations` | `.../student/examinations/page.tsx` | AppShell | student-portal/examinations |
| `/student/finance` + `/student/finance/[studentId]` | pages | AppShell | me → finance/v2/student-portal/:id |
| `/student/grades` | `.../student/grades/page.tsx` | AppShell | student-portal/transcript |
| `/student/transcript` | `.../student/transcript/page.tsx` | AppShell | transcript, POST official (PDF), verify |
| `/student/profile` | `.../student/profile/page.tsx` | AppShell | me, me/profile PUT, avatar upload |
| `/student/timetable` | `.../student/timetable/page.tsx` | AppShell | my-timetable |
| `/student/outcomes` `/roadmap` `/survey` `/workflow` | `StudentGeneralPage` | AppShell | me + transcript + timetable (+ obe/attainment for outcomes) — over-fetch on all 4 |
| `/portal-health` | `src/app/portal-health/route.ts` | — | public JSON health |

## 3. Layouts

- Root: `src/app/layout.tsx` (locale, RTL, `NextIntlClientProvider`, Personality wrapper, meta).
- ~30 route layouts are byte-identical `RouteLayout → <AppShell>{children}</AppShell>`.
- `src/components/layout/AppShell.tsx` — session bootstrap, role-aware nav (`roles.ts`), theme/accessibility toggles, logout, non-functional notification bell/search.

## 4. Data-fetching hygiene findings

- ~40/52 pages are `"use client"` fetching in `useEffect`; no react-query/SWR; failures often `.catch(()=>{})`.
- Waterfall: `/` page session→me→[attendance,finance] (`page.tsx:81-103`); `classes` session→courses; `examinations` course→grade-entry.
- `StudentGeneralPage.tsx:44-49` fetches 4 endpoints on every one of its 4 routes regardless of need.
- Full refetch after mutation: `student/courses` register→load(), `elections` after each vote.

## 5. Legacy frontend (aegiscore-frontend — IGNORE for implementation)

- Vite 8 + React 18 + Redux Toolkit; single-page shell with hardcoded role toggle and mock arrays; cookie-session assumption; only substantive page is CurriculumBuilder. Superseded by `AegisCore.Portal`.

## 6. Route risks

- Hardcoded zero-GUIDs in requests: `grade-appeals` facultyRespondentId, `attendance` markedByFacultyId, `document-vault` uploadedByUserId/role, `sync-lag` correctedByUserId/revertedByUserId. These send a bogus-but-parseable actor identity; backend must not trust them for audit (it should use JWT nameidentifier instead).
- No auth header on `document-vault` upload and `personality-context` PUT — relies on cookie forwarding; verify server accepts cookies-only auth (it does via JwtBearer cookie? — JWT is in localStorage, so these raw fetches may 401 unless cookie auth path exists). NEEDS verification at runtime.
