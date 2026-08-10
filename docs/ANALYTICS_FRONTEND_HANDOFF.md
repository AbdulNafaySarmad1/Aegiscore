# Analytics Frontend Handoff

## Contract Version

- Base namespace: `/api/analytics/v1`
- Authentication: existing HttpOnly cookie or Bearer token flow.
- All object identifiers are authorized server-side. A changed ID returns `404` when the caller cannot see that object.
- Student-self routes require a non-empty `student_id` claim. Local JWT issuance derives it only from the persisted, same-tenant `Users.StudentId` relationship and fails closed when that relationship is absent or invalid.
- Enums in analytics DTOs serialize as their names, not numeric values.

## Explanation Types

`explanationType` is one of:

- `DeterministicDecision`: exact rule, policy, or arithmetic.
- `StatisticalObservation`: contextual descriptive/inferential analytics.
- `MlPrediction`: a genuine predictive model and its stored explanation.
- `HumanDecision`: recorded human rubric, reason, or audit evidence.

`decisionSource` is one of `Rule`, `Statistic`, `Model`, `Human`, or `Mixed`.

SHAP is never used for a deterministic grade or admissions formula. SHAP labels use **Factors influencing this model prediction** and always include a non-causal disclaimer.

The shared `HumanDecisionExplanation` contract supports decision/subject IDs, rubric version and criteria, reason codes, recorded reason, audit evidence references, audit ID, decision timestamp, and explanation version. No generic human-decision read endpoint is exposed until a source workflow persists that evidence and defines resource authorization.

## Endpoints

### Scoped Discovery

- `GET /api/analytics/v1/students/me/discovery?limit=50` returns only the linked student's telemetry-course, published-grade, and genuine stored prediction references.
- `GET /api/analytics/v1/admissions/me/discovery?limit=50` returns only applications linked to the authenticated user.
- `GET /api/analytics/v1/admissions/discovery?limit=50` is restricted to admissions oversight roles and remains tenant-scoped.
- `GET /api/analytics/v1/predictions/discovery?limit=50` applies the same student, department, or tenant-wide scope as prediction explanation reads.
- Limits are bounded to 1-100. These are navigation contracts, not unrestricted generic ID listings.

### Student Telemetry

- `GET /api/analytics/v1/students/me/telemetry`
- `GET /api/analytics/v1/students/me/courses/{courseId}`
- `GET /api/analytics/v1/students/{studentId}/telemetry?courseId={courseId}` for `Admin`, `SuperAdmin`, `Registrar`, `ExamController`, `VC`, `HOD`, and `Dean`.

The staff route is tenant-scoped. `HOD` and `Dean` also require a matching authoritative `staff_department_id` claim; the student-derived `department_id` claim is never accepted as staff scope. Faculty access is not exposed until an authoritative faculty-to-course assignment claim/query is available.

`StudentTelemetry` fields:

- `studentId`, nullable `courseId`
- `period`, `comparisonPopulation`, `minimumCohortSize`
- `dataCutoffTimestamp`, `freshnessTimestamp`
- `metrics[]`

Each `TelemetryMetric` contains:

- `dimension`
- `rawValue`, `normalizedValue`
- `percentile`, `cohortMean`, `cohortMedian`, `standardDeviation`, `zScore`
- `personalPreviousValue`, `personalDelta`, `cohortDelta`
- `trendDirection`, `performanceStatus`, `improvementStatus`
- `sampleSize`, `period`, `comparisonPopulation`, `freshnessTimestamp`
- `availability`

Supported dimension names are producer-defined machine keys. Initial contracts cover `CoursePerformance`, `Assignments`, `Quizzes`, `Midterms`, `Finals`, `Labs`, `Projects`, `Attendance`, `CLOAttainment`, `PLOAttainment`, `Consistency`, `SemesterTrajectory`, and grounded academic skill domains.

The backend owns all values and classifications. The frontend must not reclassify a metric.

## Semantic Statuses

Absolute performance:

- `Exceptional`: normalized value meets the configured exceptional threshold.
- `Strong`: below exceptional and at/above the strong threshold.
- `Developing`: below strong and at/above the developing threshold.
- `AtRisk`: below the developing threshold.
- `Unavailable`: no valid authoritative value.

Personal progress is separate:

- `PersonalBest`: exceeds the student's prior valid history by the configured tolerance.
- `Improving`: positive personal delta meets the configured threshold.
- `Stable`: change remains within policy tolerance.
- `Declining`: negative personal delta meets the configured threshold.
- `Unavailable`: no valid comparison.

Trend direction is `Up`, `Flat`, `Down`, or `Unavailable`. A metric may be both `AtRisk` and `PersonalBest`.

## Privacy Suppression

- Default minimum cohort size: `10`, configurable under `Analytics:MinimumCohortSize`.
- The server minimum cannot be lowered by a telemetry payload.
- Below the threshold, `availability.status` is `Suppressed` and all comparative values are `null`.
- Suppressed responses do not include peer rows, member identities, ranks, or a reconstructable distribution.
- An individual raw/personal value can remain available while cohort context is suppressed.

## Grade Explanation

`GET /api/analytics/v1/students/me/grades/{courseGradeId}/explanation`

Only the owning student can read a published grade. Draft/calculated but unpublished grades return `404`.

The response is `AnalyticsEnvelope<GradeExplanation>`:

- Envelope: `explanationType`, `decisionSource`, `availability`, `data`, `explanationVersion`, `generatedAt`, `auditId`.
- Core: `courseGradeId`, `studentId`, `courseId`, `semesterId`, `finalScore`, `finalGrade`.
- Policy: `gradingPolicyVersion` is a SHA-256 content version of the tenant grade scale and assessment scheme.
- Components: `assessmentComponentId`, `componentType`, `rawScore`, `maximumScore`, `normalizedScore`, `weight`, `weightedContribution`.
- Arithmetic: `totalWeightedScore`, `appliedGradeBoundary`, optional `nextGradeBoundary`, optional `distanceToNextGrade`.
- Evidence: `roundingPolicy`, optional `moderationOrAdjustment`, `decisionTimestamp`, `snapshotTimestamp`.
- Context: sample size, mean, median, standard deviation, percentile, z-score, histogram, and its own availability.

The endpoint returns `availability.status = Unavailable` with `data = null` if current source records cannot exactly reproduce the persisted result, if marks are incomplete, or if the tenant's applied grade boundary is missing/ambiguous. Do not manufacture prose in that state.

## Admissions Explanation

`GET /api/analytics/v1/admissions/{applicationId}/decision-explanation`

Roles: `Admin`, `SuperAdmin`, `Registrar`, `AdmissionsHead`.

`AdmissionDecisionExplanation` fields:

- `applicationId`, `meritListId`, `decision`
- `eligibilityRequirements[]`: requirement, nullable pass result, evidence, failure reason
- `meritComponents[]`: name, raw value, normalized value, recoverable weight, contribution
- `aggregateScore`, `rank`, `seatsAvailable`
- nullable `closingMerit`, signed `distanceFromClosingMerit`, nullable `waitlistPosition`
- nullable `failedPrerequisiteOrReason`, nullable `policyVersion`
- `decisionTimestamp`, `auditReference`, `limitations[]`

Only published merit lists and persisted seat allocations define a cutoff. A negative `distanceFromClosingMerit` means the score was below that historical cutoff. It is non-promissory and must never be rendered as a guarantee that changing one component would produce admission.

The legacy admissions schema did not persist policy versions or structured eligibility outcomes. Those fields remain null/empty and the limitation is explicit.

## Prediction Explanation

`GET /api/analytics/v1/predictions/{predictionId}/explanation`

Students can read only their own student prediction. Department-scoped staff can read only student predictions in their department. Tenant-wide institutional roles can read tenant predictions.

`PredictionExplanation` fields:

- `predictionId`, `subjectId`, `subjectType`
- `modelKind`, `modelName`, `modelVersion`, `featureSchemaVersion`
- `predictionTimestamp`, `dataCutoffTimestamp`
- `rawPrediction`, nullable `calibratedProbability`, `baselineExpectedValue`
- `featureValues`, `shapValues`, `rankedLocalContributions`
- `explanationAlgorithm`, `explanationAlgorithmVersion`
- `label`, `requiresHumanReview`, `disclaimer`

Snapshots pass an additivity check: raw model output must equal expected value plus SHAP contributions within tolerance. Protected-characteristic feature keys are rejected. Historical rows cannot be updated or deleted.

No current AegisCore risk service is a genuine trained model, so no existing heuristic is exposed as SHAP. This endpoint becomes populated only by a versioned predictive-model pipeline.

## Model Quality

- `GET /api/analytics/v1/models/{modelName}/quality?modelVersion={version}`
- `GET /api/analytics/v1/models/{modelName}/global-explanation?modelVersion={version}`

Management/academic oversight roles only.

Quality fields are nullable where inappropriate: training period/sample, calibration error, AUROC, precision, recall, specificity, F1, confusion counts, threshold, drift indicator, evaluation timestamp, sensitive/proxy review evidence, aggregate fairness metrics, and an evidence statement. The frontend must not replace null metrics with zero and must not render “bias free.”

Global explanations include authorized population, population window, sample size, mean absolute SHAP by feature, algorithm/version, timestamp, and a non-causal disclaimer. They are scheduled/precomputed snapshots, never interactive recomputations.

## Availability And Errors

Availability statuses:

- `Available`: authoritative payload is present.
- `Partial`: authoritative subset is present and limitations explain omissions.
- `Unavailable`: source data cannot support a valid answer.
- `Suppressed`: privacy threshold prevents comparative disclosure.

HTTP behavior:

- `200`: available or structured unavailable/suppressed envelope where the object is authorized.
- `400`: invalid bounded input.
- `401`: unauthenticated.
- `403`: authenticated identity lacks required role/claim scope.
- `404`: resource absent, unpublished, cross-tenant, or not owned/authorized.

Never infer missing values, peer identities, causal claims, model quality, or policy versions in the frontend.
