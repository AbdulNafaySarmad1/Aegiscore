# Analytics Backend Report

## Architecture

- Added a dedicated `/api/analytics/v1` controller and `AegisCore.Application/Analytics` contracts.
- Added pure reusable statistical and telemetry-classification engines.
- Added an infrastructure intelligence service for tenant/subject authorization, deterministic explanations, snapshot writes, and snapshot reads.
- Added five append-only snapshot tables for decisions, telemetry, local predictions, model quality, and global explanations.
- Added tenant-leading indexes, EF global filters, PostgreSQL forced RLS, and database triggers that reject snapshot update/delete. `AegisDbContext` also rejects mutation for SQLite/tests and application paths.
- Migration `20260809120000_AddAnalyticsIntelligencePlatform` is explicitly discoverable by EF. The repository's pre-existing model-snapshot drift remains; this migration follows the existing manually attributed additive-migration pattern rather than regenerating the large drifted snapshot.

## Statistics

Implemented finite/missing-aware:

- count, valid count, missing count, min/max/range
- arithmetic mean, median, repeated-value modes
- sample variance and standard deviation
- type-7 quartiles, IQR, configurable percentiles
- midrank percentile rank and z-score
- bounded histograms and Tukey outlier indicators
- adjusted sample skewness
- period deltas/directions and rolling means
- privacy-suppressed cohort comparison
- pairwise-complete Pearson correlation
- one-way ANOVA F statistic, degrees of freedom, and eta-squared effect size
- mean confidence intervals for zero-variance samples and sufficiently large samples; nonzero tiny samples are withheld rather than using an unreliable approximation

Invalid, missing, zero-variance, and insufficient samples return explicit null/availability semantics.

## Telemetry Contracts

- Contracts support course performance, assessment categories, attendance, CLO/PLO, consistency, semester trajectory, and grounded skill dimensions.
- Metrics separate raw/normalized value, absolute status, relative context, personal delta, trend, and grade.
- Server-owned statuses: `Exceptional`, `Strong`, `Developing`, `AtRisk`, `Unavailable`.
- Independent progress statuses: `PersonalBest`, `Improving`, `Stable`, `Declining`, `Unavailable`.
- Persisted payloads must enforce the configured minimum cohort size; suppressed metrics cannot carry comparative values.

## Decision Explanations

- Grade explanations reproduce persisted published grades from component marks, maxima, weights, weighted contributions, exact tenant grade boundary, next boundary, distance, and the production rounding rule.
- A content-hashed policy version captures the assessment scheme and tenant grade scale used by the explanation.
- Explanations fail unavailable if records are missing, ambiguous, out of bounds, unpublished, or do not exactly reproduce the final score.
- Admissions explanations use persisted published merit contributions, aggregate, rank, seat allocation, closing merit, and waitlist position.
- Closing-merit deltas are explicitly non-promissory.
- Legacy admissions records have no persisted policy version or structured eligibility evaluation; the API reports those omissions rather than inventing them.

## SHAP And Models

- Repository review found no genuine trained predictive model or SHAP implementation. Existing defaulter/readiness services are heuristics, fixed formulas, placeholders, or LLM generation.
- No SHAP explanation was added to those deterministic/heuristic paths.
- Added immutable local/global SHAP snapshot infrastructure for a future genuine model pipeline.
- Writes require model/version/schema/explainer versions, immutable input values, data cutoff, expected value, and additivity validation.
- SHAP is rejected for `DeterministicRule`, `Heuristic`, and `LlmEstimate` model kinds.
- Protected-characteristic feature keys are rejected. Quality contracts retain proxy-review and aggregate fairness evidence without claiming “bias free.”
- Prediction outputs are labeled non-causal and require human review by default.

## Scalability

- Telemetry, prediction, quality, and global SHAP are precomputed snapshot reads.
- Local SHAP is written once per prediction ID/model version and served repeatedly.
- Decision explanations write immutable evidence once and then serve the first recorded snapshot.
- Tenant-leading latest-read and evidence-deduplication indexes bound portal queries.
- Global SHAP is a scheduled/offline writer contract; no interactive endpoint calculates it.
- No Redis dependency was introduced because the existing application has no shared cache abstraction. PostgreSQL snapshots are the durable read model and source of truth.

## Privacy And Security

- Every new entity implements `ITenantScoped`; tenant context is required and assigned server-side.
- PostgreSQL RLS uses `app.tenant_id` and fails closed when absent.
- Student self-service requires `student_id` and never falls back to user `sub`.
- Staff telemetry is tenant-scoped; HOD/Dean require department scope.
- Prediction reads enforce owner, department, or explicit tenant-wide institutional scope.
- Academic legacy tables are constrained by joins through tenant-owned departments/programs.
- Student cohort responses never include identifiable peer rows.
- Cohort statistics are null and marked `Suppressed` below the server minimum (default 10).
- Published-state checks prevent early grade/merit disclosure.

## Tests

Added 19 focused tests covering:

- mean, median, sample variance/stddev, quartiles, percentiles, missing data, zero variance, histogram totals
- percentile boundaries, rolling means, correlation, ANOVA
- tiny-cohort suppression and server telemetry leakage rejection
- independent absolute/performance and personal-best classification
- exact grade arithmetic, inconsistency refusal, grade boundary inclusivity, peer denial
- admissions merit/cutoff arithmetic and non-promissory limitations
- SHAP additivity, genuine-model restriction, protected-feature rejection
- tenant/subject isolation, missing tenant failure, controller role metadata
- model/version linkage, quality/global snapshots, historical immutability, and snapshot deduplication

## Limitations

- Current local JWT issuance has no authoritative user-to-student link, so student-self APIs require an SSO/external `student_id` claim until identity mapping is implemented.
- Faculty-to-course authorization is not authoritative in current claims; no broad faculty analytics route was added.
- Existing published grade/admissions rows predate decision snapshots. Their first authorized explanation read creates immutable backfill evidence from reproducible current records; true decision-time capture should be added transactionally to the publication/allocation workflows after those legacy schemas are reconciled.
- Admissions policies/eligibility checks were not historically versioned, so the API leaves those fields unavailable.
- No trained model artifact exists, so the SHAP tables remain empty until a genuine governed model pipeline is introduced.
- PostgreSQL RLS/trigger behavior is migration-defined; the automated suite uses SQLite and validates the equivalent application guard. A deployment migration smoke test against PostgreSQL remains recommended.
- The repository's known pre-existing EF model drift remains intentionally unresolved.

## Frontend Handoff

The frontend contract source of truth is `docs/ANALYTICS_FRONTEND_HANDOFF.md`. No `AegisCore.Portal` file was modified.
