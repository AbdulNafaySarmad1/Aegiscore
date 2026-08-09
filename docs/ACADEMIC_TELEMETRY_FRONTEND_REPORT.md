# AegisCore Academic Telemetry Frontend Report

## Baseline

Branch:
agent/telemetry-frontend

Starting analytics commit:
66dfccb2

Original frontend milestone:
481310a1

## Analytics API Integration

- New typed client `AegisCore.Portal/src/lib/analytics.ts` mirrors
  `docs/ANALYTICS_FRONTEND_HANDOFF.md` exactly: `StudentTelemetry`,
  `TelemetryMetric`, `AnalyticsEnvelope<T>`, `GradeExplanation`,
  `AdmissionDecisionExplanation`, `PredictionExplanation`, `ModelQuality`,
  `GlobalModelExplanation`, and all backend enums serialized as names.
- All calls go through the existing `api-client.ts` (`apiGet`) — HttpOnly
  cookie credentials, single-flight transparent refresh on 401, terminal-401
  `aegiscore:auth-expired` event, and 403 never conflated with 401. No
  parallel auth or fetch layer was created.
- The frontend never computes grades, percentiles, z-scores, cohort
  statistics, statuses, trends, SHAP values, or risk scores. The only
  client-side arithmetic is display formatting (decimals, signs, ordinals,
  percent unit conversion of a backend calibrated probability) and tallies
  of backend-assigned statuses for the summary strip.

## Student Telemetry

- `/telemetry` — student telemetry home
  (`GET /api/analytics/v1/students/me/telemetry`): period/comparison/data
  freshness context line, "lap" summary strip, and sector cards grouped
  into Overall, Assessment sectors, Learning outcomes, and Additional
  dimensions.
- Distinct deliberate states: 403 → "Academic record not linked" (the
  known `student_id` claim limitation), 404 → "No telemetry recorded yet",
  per-metric `Suppressed`/`Unavailable` handling, retryable error state.
- `/telemetry/courses/[courseId]` — course drill-down via
  `GET /students/me/courses/{courseId}`, reusing the same telemetry view.

## F1-Style Sector Semantics

- `src/lib/telemetry-semantics.ts` maps backend statuses to tone + label +
  symbol: Exceptional/PersonalBest → purple ◆/★, Strong/Improving → green,
  Developing → yellow ▲, AtRisk/Declining → red, Unavailable → gray —.
- Color is never the only indicator: every chip carries a text label and a
  symbol; trend arrows differ by direction with sr-only text; sector bars
  have `role="img"` aria-labels.
- Absolute performance, personal progress, and trend are rendered as three
  independent facets on every sector card (a 58% AtRisk sector can show
  "Personal best"; an 88% Strong sector can show "Declining").

## Comparison Modes

- The backend supplies one `comparisonPopulation` per snapshot; the UI
  displays it explicitly on every sector footer ("n = … · population") and
  in the page context line. No selector exists because the contract has no
  population parameter (blocker #3).
- Percentile / z-score / cohort delta are labeled as cohort context and
  never presented as grades.

## Personal Bests

- `PersonalBest` is rendered as its own purple "★ Personal best" chip,
  independent of the absolute-performance chip, and counted separately in
  the summary strip. Personal bests are never manufactured client-side —
  only the backend status is rendered.

## Course Telemetry

- `/telemetry/courses/[courseId]` renders the same grouped sector view
  scoped to one course, with deliberate 403/404 states. No skill categories
  or dimensions are invented; unknown dimension keys land in "Additional
  dimensions" with a humanized label.

## Grade Explanation

- `/telemetry/grades/[courseGradeId]` — "Why did I get this grade?":
  - Final grade + score hero.
  - Exact arithmetic table: component, raw/max, normalized, weight,
    weighted contribution, total row (`totalWeightedScore / 100`).
  - Applied boundary rule, next grade boundary, distance to next grade,
    rounding policy, optional moderation note, decision/snapshot
    timestamps, truncated policy-version hash.
  - Provenance panel: explanation type, decision source (Rule),
    "AI involved: No", explanation version, audit reference.
  - Statistical context in a clearly separate card ("How you compare") with
    its own availability handling — mean, median, std dev, percentile,
    z-score, cohort size, and a zero-based histogram with the student's
    position and median markers (plus an accessible data table).
  - Envelope-level `Unavailable` with null data renders the deliberate
    "cannot be explained" state with the backend reason — no prose
    invention. SHAP never appears on this page.

## Admissions Explanation

- `/telemetry/admissions/[applicationId]` (backend roles: Admin,
  SuperAdmin, Registrar, AdmissionsHead): decision, eligibility
  requirements (pass/fail/not-recorded icons + text), merit component
  arithmetic with aggregate total, rank, seats, closing merit, signed
  distance, waitlist position, policy version (explicit "Not recorded" when
  null), recorded reason, limitations list, audit reference, timestamps.
- The closing-merit gap is rendered with the required non-promissory
  wording ("Against the recorded closing merit for this intake…") and an
  explicit non-promissory note. Rule provenance + "AI involved: No" shown
  from the envelope.

## SHAP / Model Explainability

- `/telemetry/predictions/[predictionId]`: label, model output, calibrated
  probability (when supplied), baseline expected value, human-review badge,
  model name/version/kind, feature schema version, explainer + version,
  prediction/data-cutoff timestamps, prediction ID, input feature values,
  and ranked SHAP contributions.
- Language discipline enforced: "Factors influencing this model prediction"
  split into increasing/decreasing groups, a fixed non-causal caption
  ("explains the model's output, not real-world causality"), plus the
  backend disclaimer verbatim. The word "causes" never appears.
- Provenance panel shows "Decision source: Model" / "AI involved: Yes"
  from the envelope — deterministic decisions are never labeled as AI and
  vice versa.

## Global Analytics

- `/analytics` (oversight roles): model name/version lookup form (no list
  endpoint exists — blocker #2), then the global explanation snapshot:
  top model drivers as mean-|SHAP| contribution bars with an accessible
  table, authorized population, window, sample size, algorithm/version,
  calculation timestamp, aggregate-only note, and backend disclaimer.

## Model Quality

- Same `/analytics` surface renders the quality snapshot: only non-null
  metrics (AUROC, precision, recall, specificity, F1, threshold,
  calibration error, drift), confusion counts, training window/sample,
  fairness monitoring metrics, sensitive/proxy feature review evidence,
  evaluation timestamp, and the evidence statement. Nulls are omitted —
  never zero-filled; nothing claims "bias free".

## Privacy / Cohort Suppression

- `Suppressed` availability renders "Comparison unavailable because the
  cohort is too small to preserve privacy" (page level) / a per-sector
  privacy note, with comparative values as "—". Personal values remain
  shown when the backend supplies them. No reconstruction, no peer data, no
  hidden fetches.

## RBAC

- `roles.ts`: Student gains `/telemetry` (sidebar "Performance" module,
  academic domain). Registrar, ExamController, HOD, Dean gain `/telemetry`
  + `/analytics` with a "Model Analytics" module; Admin/VC (and inherited
  SuperAdmin) gain the "Model Analytics" module (`/*` routes already).
- No ID-based probing selectors were added; route gating remains UX-only
  with the backend authoritative (403 surfaces render ForbiddenState).

## Accessibility

- Every status = tone + label + symbol; sr-only trend labels; `role="img"`
  chart summaries; `<details>` data-table equivalents for both charts;
  `role="note"` on privacy/unavailability notes; semantic headings per
  section; visible focus retained via the existing design system;
  reduced-motion media query already global; new CSS uses logical
  properties (`border-inline-start`, `inset-inline-start`,
  `padding-inline-start`) for RTL.

## Responsiveness

- Sector grid auto-fills ≥300px cards; stats collapse 4→2 columns on
  tablet; contribution rows and histogram heights shrink on mobile; single
  column layout <640px; no horizontal scrolling required except the
  existing table-wrap pattern.

## Performance

- All analytics surfaces are single-fetch snapshot reads (one request per
  route, abort-on-unmount, retry only on user action). No polling, no
  client-side percentile/distribution computation, no new dependencies —
  charts are CSS/DOM with text equivalents.

## Tests Added

`AegisCore.Portal/tests/telemetry.spec.ts` (16 tests, API-mocked fixtures
mirroring the handoff contract):

- status text rendering; PersonalBest separate from absolute score;
  Improving/Declining separate from grade; summary tallies
- privacy-suppressed cohort state (no reconstructed values)
- unavailable metric never shows a fabricated zero
- 403 not-linked state; 404 empty state; terminal-401 → /login redirect;
  mobile-width usability without horizontal overflow
- grade explanation: exact arithmetic, applied boundary, next-grade
  distance, Rule provenance, "AI involved: No", no SHAP, separate
  statistical context; 404 state; non-reproducible unavailable state
- prediction explanation: Model provenance, "AI involved: Yes", non-causal
  language (and absence of "causes"), signed +/- contributions, model
  version metadata, human-review badge
- admissions: merit arithmetic, non-promissory gap wording, limitations,
  Rule provenance; student 403 → restricted state
- model analytics: quality metrics shown, null specificity never
  zero-filled, evidence statement; global drivers with aggregate-only
  framing; no-snapshot empty state; student gated away from /analytics

## Validation

TypeScript:
PASS (`tsc --noEmit`, 0 errors)

Lint:
PASS (`eslint`, 0 errors; 122 pre-existing warnings unchanged)

Production build:
PASS (`next build`, 48 routes; new: `/telemetry`,
`/telemetry/courses/[courseId]`, `/telemetry/grades/[courseGradeId]`,
`/telemetry/admissions/[applicationId]`,
`/telemetry/predictions/[predictionId]`, `/analytics`)

Frontend E2E:
PASS — 37/37 (21 baseline + 16 new telemetry tests)

Backend Release build:
PASS — 0 errors, 20 pre-existing warnings

Backend tests:
PASS — 558 passed, 1 pre-existing skip (matches the 66dfccb2 floor)

## Backend Blockers

See `docs/TELEMETRY_BACKEND_BLOCKERS.md`:

1. Student-self analytics routes return 403 until the local JWT issuer
   emits a `student_id` claim (known handoff limitation; deliberate
   not-linked UI state shipped).
2. No list/discovery endpoints for grade/course/prediction/application IDs
   or model names — explanation pages are reachable by direct link only.
3. No comparison-population parameter; population is snapshot-defined.
4. No trained model pipeline yet — SHAP/quality/global surfaces render
   their deliberate no-snapshot states.

## Remaining Known Issues

- Student-facing telemetry cannot be exercised end-to-end against a live
  backend until blocker #1 is resolved; UI correctness is covered by
  contract-mocked E2E tests.
- Grade/prediction/admissions explanation pages have no in-app entry point
  until a list surface (transcript, merit lists, prediction registry)
  supplies IDs.

## Recommended Next Actions

1. Backend: emit/link the `student_id` claim (or an authoritative
   user→student mapping) to unlock all student-self analytics routes.
2. Backend: add list endpoints (published grades, enrolled courses,
   recorded predictions, merit-list applications, governed models) so
   explanation pages become discoverable.
3. Frontend: deep-link "Why did I get this grade?" from transcript/result
   surfaces once `courseGradeId` is exposed.
4. Backend: when a genuine governed model pipeline lands, the SHAP,
   model-quality, and global-explanation UI is already contract-complete.
