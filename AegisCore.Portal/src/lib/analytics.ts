// ─── AegisCore Analytics Client ─── src/lib/analytics.ts ───
// Typed contracts + API access for the versioned analytics platform
// (docs/ANALYTICS_FRONTEND_HANDOFF.md — the source of truth).
//
// The backend owns every value and classification. This layer never
// computes grades, percentiles, cohort statistics, SHAP values, risk
// scores, or statuses — it only transports and types what the
// backend has already determined.

import { apiGet } from "@/lib/api-client";

// ─── Enums (serialized as names by the backend) ───

export type ExplanationSource = "Rule" | "Statistic" | "Model" | "Human" | "Mixed";

export type ExplanationKind =
  | "DeterministicDecision"
  | "StatisticalObservation"
  | "MlPrediction"
  | "HumanDecision";

export type AvailabilityStatus = "Available" | "Partial" | "Unavailable" | "Suppressed";

export type PerformanceStatus = "Exceptional" | "Strong" | "Developing" | "AtRisk" | "Unavailable";

export type ImprovementStatus = "PersonalBest" | "Improving" | "Stable" | "Declining" | "Unavailable";

export type TrendDirection = "Up" | "Flat" | "Down" | "Unavailable";

export type ModelKind = "GenuinePredictiveModel" | "DeterministicRule" | "Heuristic" | "LlmEstimate";

// ─── Shared shapes ───

export type Availability = {
  status: AvailabilityStatus;
  reason?: string | null;
};

export type AnalyticsEnvelope<T> = {
  explanationType: ExplanationKind;
  decisionSource: ExplanationSource;
  availability: Availability;
  data: T | null;
  explanationVersion: string;
  generatedAt: string;
  auditId?: string | null;
};

export type HistogramBucket = {
  lowerBound: number;
  upperBound: number;
  count: number;
  includesUpperBound: boolean;
};

// ─── Student telemetry ───

export type TelemetryMetric = {
  dimension: string;
  rawValue: number | null;
  normalizedValue: number | null;
  percentile: number | null;
  cohortMean: number | null;
  cohortMedian: number | null;
  standardDeviation: number | null;
  zScore: number | null;
  personalPreviousValue: number | null;
  personalDelta: number | null;
  cohortDelta: number | null;
  trendDirection: TrendDirection;
  performanceStatus: PerformanceStatus;
  improvementStatus: ImprovementStatus;
  sampleSize: number;
  period: string;
  comparisonPopulation: string;
  freshnessTimestamp: string;
  availability: Availability;
};

export type StudentTelemetry = {
  studentId: string;
  courseId: string | null;
  period: string;
  comparisonPopulation: string;
  minimumCohortSize: number;
  metrics: TelemetryMetric[];
  dataCutoffTimestamp: string;
  freshnessTimestamp: string;
};

// ─── Grade explanation ───

export type GradeComponentExplanation = {
  assessmentComponentId: string;
  componentType: string;
  rawScore: number;
  maximumScore: number;
  normalizedScore: number;
  weight: number;
  weightedContribution: number;
};

export type GradeBoundaryExplanation = {
  grade: string;
  minimumInclusive: number;
  maximumInclusive: number;
  gpaPoints: number;
};

export type GradeStatisticalContext = {
  sampleSize: number;
  cohortMean: number | null;
  cohortMedian: number | null;
  standardDeviation: number | null;
  percentile: number | null;
  zScore: number | null;
  distribution: HistogramBucket[];
  availability: Availability;
};

export type GradeExplanation = {
  courseGradeId: string;
  studentId: string;
  courseId: string;
  semesterId: string;
  finalScore: number;
  finalGrade: string;
  gradingPolicyVersion: string;
  assessmentComponents: GradeComponentExplanation[];
  totalWeightedScore: number;
  appliedGradeBoundary: GradeBoundaryExplanation;
  nextGradeBoundary: GradeBoundaryExplanation | null;
  distanceToNextGrade: number | null;
  roundingPolicy: string;
  moderationOrAdjustment: string | null;
  statisticalContext: GradeStatisticalContext;
  decisionTimestamp: string;
  snapshotTimestamp: string;
};

// ─── Admissions explanation ───

export type AdmissionRequirementExplanation = {
  requirement: string;
  passed: boolean | null;
  evidence: string | null;
  failureReason: string | null;
};

export type AdmissionMeritComponent = {
  name: string;
  rawValue: number | null;
  normalizedValue: number | null;
  weight: number | null;
  contribution: number;
};

export type AdmissionDecisionExplanation = {
  applicationId: string;
  meritListId: string;
  decision: string;
  eligibilityRequirements: AdmissionRequirementExplanation[];
  meritComponents: AdmissionMeritComponent[];
  aggregateScore: number;
  rank: number;
  seatsAvailable: number;
  closingMerit: number | null;
  distanceFromClosingMerit: number | null;
  waitlistPosition: number | null;
  failedPrerequisiteOrReason: string | null;
  policyVersion: string | null;
  decisionTimestamp: string;
  auditReference: string;
  limitations: string[];
};

// ─── Prediction (ML) explanation ───

export type ModelFeatureContribution = {
  featureName: string;
  featureValue: number;
  shapValue: number;
  rank: number;
};

export type PredictionExplanation = {
  predictionId: string;
  subjectId: string;
  subjectType: string;
  modelKind: ModelKind;
  modelName: string;
  modelVersion: string;
  featureSchemaVersion: string;
  predictionTimestamp: string;
  dataCutoffTimestamp: string;
  rawPrediction: number;
  calibratedProbability: number | null;
  baselineExpectedValue: number;
  featureValues: Record<string, number>;
  shapValues: Record<string, number>;
  rankedLocalContributions: ModelFeatureContribution[];
  explanationAlgorithm: string;
  explanationAlgorithmVersion: string;
  label: string;
  requiresHumanReview: boolean;
  disclaimer: string;
};

// ─── Model quality / global explanations (authorized oversight roles) ───

export type ModelQuality = {
  modelName: string;
  modelVersion: string;
  trainingPeriodStart: string | null;
  trainingPeriodEnd: string | null;
  trainingSampleSize: number | null;
  calibrationError: number | null;
  auRoc: number | null;
  precision: number | null;
  recall: number | null;
  specificity: number | null;
  f1: number | null;
  truePositive: number | null;
  trueNegative: number | null;
  falsePositive: number | null;
  falseNegative: number | null;
  threshold: number | null;
  driftIndicator: number | null;
  evaluationTimestamp: string;
  sensitiveOrProxyFeatureReview: string[];
  fairnessMonitoringMetrics: Record<string, number>;
  evidenceStatement: string;
};

export type GlobalModelExplanation = {
  snapshotId: string;
  modelName: string;
  modelVersion: string;
  featureSchemaVersion: string;
  authorizedPopulation: string;
  sampleSize: number;
  populationStart: string;
  populationEnd: string;
  meanAbsoluteShapValues: Record<string, number>;
  explanationAlgorithm: string;
  explanationAlgorithmVersion: string;
  calculatedAt: string;
  disclaimer: string;
};

// ─── API access (all through the shared api-client: cookies, refresh, 401/403) ───

const BASE = "/api/analytics/v1";

/** GET /students/me/telemetry — Student role; 403 until the student link exists, 404 when no snapshot. */
export function getMyTelemetry(signal?: AbortSignal) {
  return apiGet<StudentTelemetry>(`${BASE}/students/me/telemetry`, signal);
}

/** GET /students/me/courses/{courseId} — per-course drill-down for the owning student. */
export function getMyCourseTelemetry(courseId: string, signal?: AbortSignal) {
  return apiGet<StudentTelemetry>(`${BASE}/students/me/courses/${encodeURIComponent(courseId)}`, signal);
}

/** GET /students/{studentId}/telemetry — scoped staff roles only (backend authorized). */
export function getStudentTelemetry(studentId: string, courseId?: string, signal?: AbortSignal) {
  const query = courseId ? `?courseId=${encodeURIComponent(courseId)}` : "";
  return apiGet<StudentTelemetry>(`${BASE}/students/${encodeURIComponent(studentId)}/telemetry${query}`, signal);
}

/** GET /students/me/grades/{courseGradeId}/explanation — deterministic grade explanation. */
export function getMyGradeExplanation(courseGradeId: string, signal?: AbortSignal) {
  return apiGet<AnalyticsEnvelope<GradeExplanation>>(
    `${BASE}/students/me/grades/${encodeURIComponent(courseGradeId)}/explanation`,
    signal
  );
}

/** GET /admissions/{applicationId}/decision-explanation — admissions oversight roles. */
export function getAdmissionDecisionExplanation(applicationId: string, signal?: AbortSignal) {
  return apiGet<AnalyticsEnvelope<AdmissionDecisionExplanation>>(
    `${BASE}/admissions/${encodeURIComponent(applicationId)}/decision-explanation`,
    signal
  );
}

/** GET /predictions/{predictionId}/explanation — stored ML prediction + SHAP snapshot. */
export function getPredictionExplanation(predictionId: string, signal?: AbortSignal) {
  return apiGet<AnalyticsEnvelope<PredictionExplanation>>(
    `${BASE}/predictions/${encodeURIComponent(predictionId)}/explanation`,
    signal
  );
}

/** GET /models/{modelName}/quality — management/academic oversight roles. */
export function getModelQuality(modelName: string, modelVersion?: string, signal?: AbortSignal) {
  const query = modelVersion ? `?modelVersion=${encodeURIComponent(modelVersion)}` : "";
  return apiGet<ModelQuality>(`${BASE}/models/${encodeURIComponent(modelName)}/quality${query}`, signal);
}

/** GET /models/{modelName}/global-explanation — management/academic oversight roles. */
export function getGlobalModelExplanation(modelName: string, modelVersion?: string, signal?: AbortSignal) {
  const query = modelVersion ? `?modelVersion=${encodeURIComponent(modelVersion)}` : "";
  return apiGet<GlobalModelExplanation>(
    `${BASE}/models/${encodeURIComponent(modelName)}/global-explanation${query}`,
    signal
  );
}

// ─── Presentation helpers (formatting only — never computation of analytics) ───

/** Humanize a producer-defined machine key, e.g. "CLOAttainment" → "CLO Attainment". */
export function humanizeDimension(dimension: string): string {
  const known: Record<string, string> = {
    CoursePerformance: "Course Performance",
    CLOAttainment: "CLO Attainment",
    PLOAttainment: "PLO Attainment",
  };
  if (known[dimension]) return known[dimension];
  return dimension
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
    .replace(/([A-Z]+)([A-Z][a-z])/g, "$1 $2")
    .trim();
}

/** Format a backend decimal for display. No value invention: null → em dash by caller. */
export function formatValue(value: number | null | undefined, digits = 1): string {
  if (value === null || value === undefined || Number.isNaN(value)) return "—";
  return value.toFixed(digits);
}

/** Signed delta, e.g. "+6.2" / "-3.1". */
export function formatDelta(value: number | null | undefined, digits = 1): string {
  if (value === null || value === undefined || Number.isNaN(value)) return "—";
  const fixed = value.toFixed(digits);
  return value > 0 ? `+${fixed}` : fixed;
}

/** Ordinal suffix for a backend-supplied percentile, e.g. 71 → "71st". */
export function formatPercentile(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) return "—";
  const rounded = Math.round(value);
  const mod100 = rounded % 100;
  const suffix =
    mod100 >= 11 && mod100 <= 13
      ? "th"
      : rounded % 10 === 1
        ? "st"
        : rounded % 10 === 2
          ? "nd"
          : rounded % 10 === 3
            ? "rd"
            : "th";
  return `${rounded}${suffix}`;
}

/** Signed z-score, e.g. "+0.72". */
export function formatZScore(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) return "—";
  const fixed = value.toFixed(2);
  return value > 0 ? `+${fixed}` : fixed;
}

export function formatDateTime(iso: string | null | undefined): string {
  if (!iso) return "—";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return "—";
  return date.toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" });
}
