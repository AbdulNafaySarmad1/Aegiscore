# Telemetry Backend Blockers

> Integration status (2026-08-10): blockers 1 and 2 are resolved on `agent/demo-integration`. Blockers 3 and 4 remain deliberate, truthful product constraints rather than defects: comparison population is snapshot-defined and no trained model exists.

Verified contract constraints encountered while building the academic
telemetry / decision-transparency frontend. None of these are frontend
defects; each item lists the evidence and the user impact. No backend
analytics semantics were altered from the frontend pass.

## 1. Student-self analytics routes return 403 until the identity link exists

- **Frontend feature:** `/telemetry` (student telemetry home),
  `/telemetry/courses/{courseId}`, `/telemetry/grades/{courseGradeId}`.
- **Endpoints:** `GET /api/analytics/v1/students/me/*`.
- **Expected contract:** a Student-role caller can read their own telemetry
  and grade explanations.
- **Actual contract:** the routes require a non-empty `student_id` JWT claim.
  The current local JWT issuer does not emit that claim, so the routes return
  `403` for every locally issued student token.
- **Evidence:** `docs/ANALYTICS_FRONTEND_HANDOFF.md` §Contract Version;
  `AegisCore.API/Controllers/AnalyticsController.cs` (`StudentId()` gate);
  `docs/ANALYTICS_BACKEND_REPORT.md` §Limitations.
- **User impact:** students see the deliberate "Academic record not linked"
  state until an authoritative user→student mapping is issued. No placeholder
  data is shown.

## 2. No discovery surface for explanation identifiers

- **Frontend feature:** grade explanation, course telemetry, prediction
  explanation, admissions explanation, model analytics.
- **Endpoints:** all of `/api/analytics/v1/*` read routes.
- **Expected contract:** (frontend need) a way to reach an explanation from a
  list — e.g. published grades with `courseGradeId`, enrolled courses with
  `courseId`, recorded predictions with `predictionId`, applications with
  `applicationId`, governed model names.
- **Actual contract:** every analytics read route is ID-addressed and no list
  endpoint exists for any of these resources.
- **Evidence:** handoff §Endpoints (route inventory only);
  `AnalyticsController.cs` (no list actions).
- **User impact:** the new explanation pages work when linked with a real ID
  (e.g. from a future transcript or merit-list surface) but cannot be
  discovered in-app today. Deliberately, no ID-probing pickers were added.

## 3. Comparison population is snapshot-defined, not selectable

- **Frontend feature:** comparison modes (Myself previously / Class / Cohort /
  Program).
- **Endpoint:** `GET /api/analytics/v1/students/me/telemetry`.
- **Expected contract:** (product ambition) the student can switch the
  comparison population.
- **Actual contract:** the endpoint accepts no population parameter; each
  metric carries a backend-chosen `comparisonPopulation`.
- **Evidence:** handoff §Student Telemetry; `AnalyticsContracts.cs`
  (`StudentTelemetry`, `TelemetryMetric`).
- **User impact:** the UI displays the recorded comparison basis explicitly
  and offers no selector. A population parameter would be a backend feature.

## 4. No trained model exists yet, so SHAP surfaces have no live data

- **Frontend feature:** prediction explanation (SHAP), model quality, global
  explanations.
- **Endpoints:** `/predictions/{id}/explanation`, `/models/{name}/quality`,
  `/models/{name}/global-explanation`.
- **Expected contract:** (future) populated prediction/quality snapshots.
- **Actual contract:** the immutable snapshot infrastructure exists and is
  fully served, but no genuine trained model pipeline writes to it yet, so
  all three routes return `404` in practice.
- **Evidence:** handoff §Prediction Explanation; backend report §SHAP And
  Models ("no existing heuristic is exposed as SHAP").
- **User impact:** the SHAP/model-quality UI is complete and tested against
  the contract, and currently renders the deliberate "no snapshot recorded"
  states.
