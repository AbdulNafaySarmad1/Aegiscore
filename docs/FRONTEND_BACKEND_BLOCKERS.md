# Frontend ↔ Backend Blockers

Contract gaps found during the frontend integration pass (branch `agent/frontend-kimi`,
backend baseline `344afa70` / `backend-demo-ready-20260809`). Each entry was verified
against the backend source. Frontend pages degrade to deliberate empty/unavailable
states rather than fabricating data.

## 1. Faculty course feed (blocks Attendance + Grade Entry)

- **Frontend area:** `/attendance`, `/examinations`
- **Frontend calls:** `GET /api/courses/my`, `GET /api/attendance/roster/{courseId}`
- **Expected:** list of courses assigned to the signed-in faculty member; roster per course.
- **Actual:** neither route exists. `AttendanceController` exposes only `POST /api/attendance/mark`,
  `GET /api/attendance/report/course/{courseId}`, `GET /api/attendance/report/student/{studentId}`.
- **Impact:** faculty cannot reach attendance marking or grade entry with real data.
- **Also:** `PUT /api/examination/marks` (used by grade entry) has no matching backend route;
  `S3ExaminationController` exposes schemes/moderation/grade-calculation routes instead.

## 2. Class generation read endpoints (blocks Classes page)

- **Frontend area:** `/classes`
- **Frontend calls:** `GET /api/classes/offerings`, `GET /api/classes/sections`,
  `GET /api/classes/sync-logs`, `POST /api/classes/lms/provision`, `POST /api/classes/lms/sync-roster`
- **Actual backend (`ClassGenerationController`):** `POST /api/classes/generate`,
  `GET /api/classes/jobs/{jobId}`, `GET /api/classes/offering/{offeringId}`,
  `POST /api/classes/{classId}/provision-lms`, `POST /api/classes/provisions/{provisionId}/sync-roster`,
  `GET /api/classes/{classId}/provisions`. No list endpoints for offerings/sections/sync-logs.
- **Impact:** classes workspace renders empty even when generation jobs succeed.

## 3. Election discovery (blocks Elections page)

- **Frontend area:** `/elections`
- **Frontend calls:** `GET /api/elections`, `GET /api/elections/candidates`, `GET /api/elections/results`
- **Actual:** no list/read endpoints exist on the legacy `ElectionsController`
  (mutations + tally only) or the v2 group (`/api/campus/v2/elections` has per-election
  `/{id}/candidates`, `/{id}/results`, `/{id}/participation` but no "list elections" route).
- **Frontend reconciliation done:** nomination, token issuance (v2 one-time `voterToken`,
  displayed once), ballot casting, tally envelope (`{total, results}`), and certification
  (`/api/elections/results/{resultId}/certify`) now match the real route shapes.
- **Impact:** without a list endpoint the page cannot discover election IDs; forms work only
  when an election ID is otherwise known.

## 4. Student-self academic/finance feeds (blocks student KPIs)

- **Frontend area:** `/student/dashboard`, `/student/profile`, `/student/transcript`, role KPIs
- **Expected:** student CGPA/attendance/fee summaries for the signed-in student.
- **Actual:** all student-grade routes require `{studentId:guid}`
  (`/api/examination/grades/{studentId}/cgpa`, `/api/attendance/report/student/{studentId}`,
  `/api/finance/v2/student-portal/{studentId}`), but `GET /api/session` returns
  `studentId: null` for local accounts (documented in FRONTEND_API_HANDOFF.md §Session).
- **Impact:** student dashboards deliberately show no academic metrics until the
  user-to-student link exists. Transcript page wires the real request + public
  hash-verification endpoints only.

## 5. Cafeteria read endpoints (blocks Cafeteria page)

- **Frontend calls:** `GET /api/cafeteria/menu-items`, `GET /api/cafeteria/orders`,
  `GET /api/cafeteria/inventory-alerts`, `GET /api/cafeteria/vendor-scorecard`, `POST /api/cafeteria/waste-log`
- **Actual (`CafeteriaController`):** creation endpoints plus
  `GET /api/cafeteria/units/{unitId}/orders` and `GET /api/cafeteria/units/{unitId}/alerts`
  (unit-scoped). No tenant-wide list routes. V2 equivalents live under
  `/api/campus/v2/cafeteria/*` with different shapes.
- **Impact:** cafeteria page shows empty states.

## 6. Voice session history (blocks Voice page history panel)

- **Frontend calls:** `GET /api/voice/sessions`
- **Actual (`VoiceController`):** `POST /api/voice/audio`, `POST /api/voice/text`,
  `GET /api/voice/sessions/{id}`, `POST /api/voice/sessions/{id}/execute`. No list route.

## 7. Placement module (blocks Placement page)

- **Frontend calls:** `GET /api/placement/active`, `/api/placement/analytics`,
  `/api/placement/partners`, `/api/placement/readiness/{id}`
- **Actual:** no `api/placement` controller exists in the backend.

## 8. KPI summary endpoints (documented in FRONTEND_API_HANDOFF.md §KPI Contracts)

- `/api/classes?summary`, `/api/obe/attainment`, `/api/grade-appeals?status=submitted`,
  `/api/readiness`, `/api/defaulter-prediction?summary` — route-shape mismatches.
- `/api/grades/cgpa`, `/api/student/finance`, `/api/placement?summary`, `/api/portal-health`,
  `/api/imports?status=failed` — no backend route.
- **Frontend handling:** role KPI definitions keep only the verified live source
  (`GET /api/finance/v2/dashboard/executive`); everything else renders an explicit
  "Not available" state. No inferred or fabricated values.

## 9. Grade appeals list (partially reconciled)

- **Frontend area:** `/grade-appeals`
- **Was:** `GET /api/grade-appeals` (does not exist), `POST /{id}/brief`, `POST /{id}/process`.
- **Now:** uses `GET /api/grade-appeals/pending` (envelope `{total, appeals}`),
  `POST /{id}/committee-brief`, `POST /{id}/decision?decision=Upheld|Dismissed`.
- **Remaining gap:** appeal submission requires `studentId`/`courseId` GUIDs and a
  `facultyRespondentId` GUID that the session cannot supply; there is no
  student-self "my appeals" view without a student link (see #4).

## 10. Proctoring read routes (unverified sub-routes)

- **Frontend calls:** `GET /api/proctoring/integrity`, `/api/proctoring/events`,
  `/api/proctoring/sessions`, `POST /api/proctoring/face-analysis`,
  `POST /api/proctoring/lockdown/validate`
- **Actual:** `ProctoringIntegrationController` exposes `/api/proctoring-integration/*`
  (`face-analyze`, `lockdown/{sessionId}`, `multi-face`, `gaze`). The legacy
  `ProctoringController` exists but its exact sub-routes were not fully reconciled.
- **Impact:** proctoring dashboards may render empty until routes are aligned.

## Non-blockers (verified working contracts)

- Auth/session: `POST /api/auth/login|refresh|logout`, `GET /api/session` — fully wired.
- Finance: `GET /api/finance/v2/dashboard/executive`, voucher decision, gateway config,
  student portal — verified per FRONTEND_API_HANDOFF.md.
- Transcript verify: `GET /api/examination/transcripts/verify/{hash}` — wired.
- Notifications: `GET /api/notifications/deliveries` — wired to the shell bell.
