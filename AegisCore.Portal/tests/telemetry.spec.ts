import { test, expect, Page } from "@playwright/test";

// ─── Academic Telemetry / Explainability E2E ───
// The E2E environment has no backend, so API responses are mocked at the
// network layer. Fixtures are TEST-ONLY contracts mirroring
// docs/ANALYTICS_FRONTEND_HANDOFF.md — they never ship to the product.

const studentSession = {
  userId: "00000000-0000-0000-0000-0000000000a1",
  tenantId: "11111111-1111-1111-1111-111111111111",
  studentId: "22222222-2222-2222-2222-222222222222",
  departmentId: null,
  name: "Test Student",
  roles: ["Student"],
  permissions: [],
};

const adminSession = {
  ...studentSession,
  studentId: null,
  name: "Test Admin",
  roles: ["Admin"],
};

async function mockSession(page: Page, session: unknown) {
  await page.route("**/api/session", route =>
    route.fulfill({ status: 200, contentType: "application/json", json: session as object })
  );
}

function metric(overrides: Record<string, unknown>) {
  return {
    dimension: "Assignments",
    rawValue: null,
    normalizedValue: null,
    percentile: null,
    cohortMean: null,
    cohortMedian: null,
    standardDeviation: null,
    zScore: null,
    personalPreviousValue: null,
    personalDelta: null,
    cohortDelta: null,
    trendDirection: "Unavailable",
    performanceStatus: "Unavailable",
    improvementStatus: "Unavailable",
    sampleSize: 0,
    period: "Fall 2026",
    comparisonPopulation: "BSCS 2023 cohort",
    freshnessTimestamp: "2026-08-02T00:00:00Z",
    availability: { status: "Available", reason: null },
    ...overrides,
  };
}

const telemetryFixture = {
  studentId: studentSession.studentId,
  courseId: null,
  period: "Fall 2026",
  comparisonPopulation: "BSCS 2023 cohort",
  minimumCohortSize: 10,
  metrics: [
    metric({
      dimension: "CoursePerformance",
      normalizedValue: 82.4, rawValue: 82.4, percentile: 71,
      cohortMean: 74.1, cohortMedian: 75, standardDeviation: 8.2, zScore: 1.01,
      personalPreviousValue: 76.2, personalDelta: 6.2, cohortDelta: 8.3,
      trendDirection: "Up", performanceStatus: "Strong", improvementStatus: "Improving",
      sampleSize: 42,
    }),
    metric({
      dimension: "Midterms",
      normalizedValue: 58.0, rawValue: 58.0, percentile: 22,
      cohortMean: 71.5, cohortMedian: 73, standardDeviation: 9.4, zScore: -1.44,
      personalPreviousValue: 51.8, personalDelta: 6.2, cohortDelta: -13.5,
      trendDirection: "Up", performanceStatus: "AtRisk", improvementStatus: "PersonalBest",
      sampleSize: 42,
    }),
    metric({
      dimension: "Finals",
      normalizedValue: 88.0, rawValue: 88.0, percentile: 84,
      cohortMean: 70.2, cohortMedian: 71, standardDeviation: 10.1, zScore: 1.76,
      personalPreviousValue: 91.4, personalDelta: -3.4, cohortDelta: 17.8,
      trendDirection: "Down", performanceStatus: "Strong", improvementStatus: "Declining",
      sampleSize: 42,
    }),
    metric({
      dimension: "Attendance",
      normalizedValue: 96.0, rawValue: 96.0, percentile: 90,
      cohortMean: 82.1, cohortMedian: 84, standardDeviation: 7.7, zScore: 1.81,
      personalPreviousValue: 95.8, personalDelta: 0.2, cohortDelta: 13.9,
      trendDirection: "Flat", performanceStatus: "Exceptional", improvementStatus: "Stable",
      sampleSize: 42,
    }),
    metric({
      dimension: "Labs",
      normalizedValue: 91.0, rawValue: 91.0,
      trendDirection: "Up", performanceStatus: "Exceptional", improvementStatus: "Unavailable",
      sampleSize: 4,
      availability: { status: "Suppressed", reason: "Cohort below the minimum size." },
    }),
    metric({
      dimension: "Projects",
      performanceStatus: "Unavailable", improvementStatus: "Unavailable",
      availability: { status: "Unavailable", reason: "No project marks recorded for this period." },
    }),
  ],
  dataCutoffTimestamp: "2026-08-01T00:00:00Z",
  freshnessTimestamp: "2026-08-02T00:00:00Z",
};

const discoveryFixture = {
  courses: [{
    courseId: "30000000-0000-0000-0000-000000000001",
    courseCode: "CS-201",
    courseTitle: "Data Structures & Algorithms",
    period: "Fall 2026",
    freshnessTimestamp: "2026-08-02T00:00:00Z",
  }],
  grades: [{
    courseGradeId: "40000000-0000-0000-0000-000000000001",
    courseId: "30000000-0000-0000-0000-000000000001",
    courseCode: "CS-201",
    courseTitle: "Data Structures & Algorithms",
    semesterId: "50000000-0000-0000-0000-000000000001",
    semesterName: "Fall 2026",
    finalScore: 78.4,
    finalGrade: "B+",
    publishedAt: "2026-08-01T00:00:00Z",
  }],
  predictions: [],
};

// ─── Student telemetry home ───

test.describe("Student telemetry", () => {
  test("discovers only backend-authorized course, grade, and admission references", async ({ page }) => {
    await mockSession(page, studentSession);
    await page.route("**/api/analytics/v1/students/me/telemetry", route =>
      route.fulfill({ status: 200, contentType: "application/json", json: telemetryFixture })
    );
    await page.route("**/api/analytics/v1/students/me/discovery", route =>
      route.fulfill({ status: 200, contentType: "application/json", json: discoveryFixture })
    );
    await page.route("**/api/analytics/v1/admissions/me/discovery", route =>
      route.fulfill({ status: 200, contentType: "application/json", json: [{
        applicationId: "60000000-0000-0000-0000-000000000001",
        programId: "70000000-0000-0000-0000-000000000001",
        programName: "BS Computer Science",
        applicationStatus: "offered",
        submittedAt: "2022-06-01T00:00:00Z",
        hasPublishedDecision: true,
      }] })
    );

    await page.goto("/telemetry");

    await expect(page.getByRole("link", { name: /CS-201.*Data Structures/ })).toHaveAttribute(
      "href", "/telemetry/courses/30000000-0000-0000-0000-000000000001"
    );
    await expect(page.getByRole("link", { name: "Why did I get B+ in CS-201?" })).toHaveAttribute(
      "href", "/telemetry/grades/40000000-0000-0000-0000-000000000001"
    );
    await expect(page.getByRole("link", { name: "Explain my BS Computer Science decision" })).toHaveAttribute(
      "href", "/telemetry/admissions/60000000-0000-0000-0000-000000000001"
    );
    await expect(page.getByText("No genuine model prediction snapshot is recorded for this account.")).toBeVisible();
  });

  test("shows owned admission discovery when no telemetry snapshot exists", async ({ page }) => {
    await mockSession(page, studentSession);
    await page.route("**/api/analytics/v1/students/me/telemetry", route =>
      route.fulfill({ status: 404, contentType: "application/json", json: { error: "Not found" } })
    );
    await page.route("**/api/analytics/v1/students/me/discovery", route =>
      route.fulfill({ status: 403, contentType: "application/json", json: { error: "Forbidden" } })
    );
    await page.route("**/api/analytics/v1/admissions/me/discovery", route =>
      route.fulfill({ status: 200, contentType: "application/json", json: [{
        applicationId: "60000000-0000-0000-0000-000000000001",
        programId: "70000000-0000-0000-0000-000000000001",
        programName: "BS Computer Science",
        applicationStatus: "offered",
        submittedAt: "2022-06-01T00:00:00Z",
        hasPublishedDecision: true,
      }] })
    );

    await page.goto("/telemetry");

    await expect(page.getByText("No telemetry recorded yet")).toBeVisible({ timeout: 15000 });
    await expect(page.getByRole("link", { name: "Explain my BS Computer Science decision" })).toBeVisible();
  });

  test("gives admissions staff navigable published decision references", async ({ page }) => {
    await mockSession(page, adminSession);
    await page.route("**/api/analytics/v1/students/me/discovery", route =>
      route.fulfill({ status: 403, contentType: "application/json", json: { error: "Forbidden" } })
    );
    await page.route("**/api/analytics/v1/admissions/discovery", route =>
      route.fulfill({ status: 200, contentType: "application/json", json: [{
        applicationId: "60000000-0000-0000-0000-000000000001",
        programId: "70000000-0000-0000-0000-000000000001",
        programName: "BS Computer Science",
        applicationStatus: "offered",
        submittedAt: "2022-06-01T00:00:00Z",
        hasPublishedDecision: true,
      }] })
    );

    await page.goto("/telemetry");

    await expect(page.getByRole("heading", { name: "Admissions Decision Discovery" })).toBeVisible({ timeout: 15000 });
    await expect(page.getByRole("link", { name: "Explain BS Computer Science decision" })).toHaveAttribute(
      "href", "/telemetry/admissions/60000000-0000-0000-0000-000000000001"
    );
  });

  test("renders backend statuses as text, keeps PersonalBest separate from absolute score", async ({ page }) => {
    await mockSession(page, studentSession);
    await page.route("**/api/analytics/v1/students/me/telemetry", route =>
      route.fulfill({ status: 200, contentType: "application/json", json: telemetryFixture })
    );

    await page.goto("/telemetry");
    await expect(page.getByRole("heading", { name: "Performance Telemetry" })).toBeVisible({ timeout: 15000 });

    // Absolute-performance labels render as text (not color alone)
    await expect(page.getByText("Strong", { exact: true }).first()).toBeVisible();
    await expect(page.getByText("At risk", { exact: true })).toBeVisible();
    await expect(page.getByText("Exceptional", { exact: true }).first()).toBeVisible();

    // A low absolute score can still be a Personal Best — both shown together
    const midterms = page.locator(".sector-card", { hasText: "Midterms" });
    await expect(midterms).toContainText("58.0");
    await expect(midterms).toContainText("Personal best");
    await expect(midterms).toContainText("At risk");
    await expect(midterms).toContainText("+6.2");

    // A high absolute score can still be Declining
    const finals = page.locator(".sector-card", { hasText: "Finals" });
    await expect(finals).toContainText("88.0");
    await expect(finals).toContainText("Declining");
    await expect(finals).toContainText("-3.4");

    // Summary strip tallies backend statuses
    await expect(page.locator(".telemetry-summary")).toContainText("improving");
    await expect(page.locator(".telemetry-summary")).toContainText("personal best");
  });

  test("privacy-suppressed cohort shows a deliberate state, never reconstructed values", async ({ page }) => {
    await mockSession(page, studentSession);
    await page.route("**/api/analytics/v1/students/me/telemetry", route =>
      route.fulfill({ status: 200, contentType: "application/json", json: telemetryFixture })
    );

    await page.goto("/telemetry");
    const labs = page.locator(".sector-card", { hasText: "Labs" });
    await expect(labs).toBeVisible({ timeout: 15000 });
    // Personal value remains, comparative values are em dashes, privacy note shown
    await expect(labs).toContainText("91.0");
    await expect(labs).toContainText("privacy");
    const percentileCell = labs.locator(".sector-stat", { hasText: "Percentile" });
    await expect(percentileCell).toContainText("—");
  });

  test("unavailable metric never shows a fabricated zero", async ({ page }) => {
    await mockSession(page, studentSession);
    await page.route("**/api/analytics/v1/students/me/telemetry", route =>
      route.fulfill({ status: 200, contentType: "application/json", json: telemetryFixture })
    );

    await page.goto("/telemetry");
    const projects = page.locator(".sector-card", { hasText: "Projects" });
    await expect(projects).toBeVisible({ timeout: 15000 });
    await expect(projects).toContainText("No project marks recorded for this period.");
    await expect(projects).not.toContainText("0.0 / 100");
  });

  test("403 shows the not-linked state (distinct from an error)", async ({ page }) => {
    await mockSession(page, studentSession);
    await page.route("**/api/analytics/v1/students/me/telemetry", route =>
      route.fulfill({ status: 403, contentType: "application/json", json: { error: "Forbidden" } })
    );

    await page.goto("/telemetry");
    await expect(page.getByText("Academic record not linked")).toBeVisible({ timeout: 15000 });
  });

  test("404 shows the no-snapshot empty state", async ({ page }) => {
    await mockSession(page, studentSession);
    await page.route("**/api/analytics/v1/students/me/telemetry", route =>
      route.fulfill({ status: 404, contentType: "application/json", json: { error: "Not found" } })
    );

    await page.goto("/telemetry");
    await expect(page.getByText("No telemetry recorded yet")).toBeVisible({ timeout: 15000 });
  });

  test("telemetry remains usable at mobile width without horizontal overflow", async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await mockSession(page, studentSession);
    await page.route("**/api/analytics/v1/students/me/telemetry", route =>
      route.fulfill({ status: 200, contentType: "application/json", json: telemetryFixture })
    );

    await page.goto("/telemetry");
    await expect(page.locator(".sector-card").first()).toBeVisible({ timeout: 15000 });
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth
    );
    expect(overflow).toBeLessThanOrEqual(2);
  });

  test("terminal 401 still redirects to login", async ({ page }) => {
    await page.route("**/api/session", route =>
      route.fulfill({ status: 401, contentType: "application/json", json: { error: "Unauthorized" } })
    );
    await page.route("**/api/auth/refresh", route =>
      route.fulfill({ status: 401, contentType: "application/json", json: { error: "Unauthorized" } })
    );

    await page.goto("/telemetry");
    await page.waitForURL("**/login", { timeout: 15000 });
  });
});

// ─── Grade explanation ───

const gradeExplanationFixture = {
  explanationType: "DeterministicDecision",
  decisionSource: "Rule",
  availability: { status: "Available", reason: null },
  explanationVersion: "grade-explanation/v1",
  generatedAt: "2026-08-02T00:00:00Z",
  auditId: "33333333-3333-3333-3333-333333333333",
  data: {
    courseGradeId: "44444444-4444-4444-4444-444444444444",
    studentId: studentSession.studentId,
    courseId: "55555555-5555-5555-5555-555555555555",
    semesterId: "66666666-6666-6666-6666-666666666666",
    finalScore: 78.4,
    finalGrade: "B+",
    gradingPolicyVersion: "abcdef1234567890abcdef1234567890",
    assessmentComponents: [
      { assessmentComponentId: "c1c1c1c1-0000-0000-0000-000000000001", componentType: "Assignments", rawScore: 18.2, maximumScore: 20, normalizedScore: 91.0, weight: 20, weightedContribution: 18.2 },
      { assessmentComponentId: "c2c2c2c2-0000-0000-0000-000000000002", componentType: "Quizzes", rawScore: 12.6, maximumScore: 15, normalizedScore: 84.0, weight: 15, weightedContribution: 12.6 },
      { assessmentComponentId: "c3c3c3c3-0000-0000-0000-000000000003", componentType: "Midterm", rawScore: 17.0, maximumScore: 25, normalizedScore: 68.0, weight: 25, weightedContribution: 17.0 },
      { assessmentComponentId: "c4c4c4c4-0000-0000-0000-000000000004", componentType: "Final", rawScore: 27.6, maximumScore: 35, normalizedScore: 78.9, weight: 35, weightedContribution: 27.6 },
      { assessmentComponentId: "c5c5c5c5-0000-0000-0000-000000000005", componentType: "Participation", rawScore: 3.0, maximumScore: 5, normalizedScore: 60.0, weight: 5, weightedContribution: 3.0 },
    ],
    totalWeightedScore: 78.4,
    appliedGradeBoundary: { grade: "B+", minimumInclusive: 77.0, maximumInclusive: 79.99, gpaPoints: 3.3 },
    nextGradeBoundary: { grade: "A-", minimumInclusive: 80.0, maximumInclusive: 84.99, gpaPoints: 3.7 },
    distanceToNextGrade: 1.6,
    roundingPolicy: "Half-up to two decimal places",
    moderationOrAdjustment: null,
    statisticalContext: {
      sampleSize: 42,
      cohortMean: 74.1,
      cohortMedian: 75.0,
      standardDeviation: 8.2,
      percentile: 68,
      zScore: 0.52,
      distribution: [
        { lowerBound: 50, upperBound: 60, count: 4, includesUpperBound: false },
        { lowerBound: 60, upperBound: 70, count: 9, includesUpperBound: false },
        { lowerBound: 70, upperBound: 80, count: 17, includesUpperBound: false },
        { lowerBound: 80, upperBound: 90, count: 10, includesUpperBound: false },
        { lowerBound: 90, upperBound: 100, count: 2, includesUpperBound: true },
      ],
      availability: { status: "Available", reason: null },
    },
    decisionTimestamp: "2026-07-15T00:00:00Z",
    snapshotTimestamp: "2026-08-02T00:00:00Z",
  },
};

test.describe("Grade explanation", () => {
  const gradeId = gradeExplanationFixture.data.courseGradeId;

  test("shows exact deterministic arithmetic, boundary, distance, and Rule provenance", async ({ page }) => {
    await mockSession(page, studentSession);
    await page.route(`**/api/analytics/v1/students/me/grades/${gradeId}/explanation`, route =>
      route.fulfill({ status: 200, contentType: "application/json", json: gradeExplanationFixture })
    );

    await page.goto(`/telemetry/grades/${gradeId}`);
    await expect(page.getByRole("heading", { name: "Why did I get this grade?" })).toBeVisible({ timeout: 15000 });

    // Deterministic provenance — never AI
    await expect(page.getByText("Decision source: Rule")).toBeVisible();
    await expect(page.getByText("AI involved: No")).toBeVisible();

    // Exact arithmetic: components and total
    await expect(page.getByText("18.20 / 20.00")).toBeVisible();
    await expect(page.getByText("78.40 / 100.00").first()).toBeVisible();

    // Applied boundary + next-grade distance
    await expect(page.getByText(/77\.00 ≤ score ≤ 79\.99 → B\+/)).toBeVisible();
    await expect(page.getByText(/begins at/)).toContainText("80.00");
    await expect(page.getByText(/distance/)).toContainText("1.60");

    // SHAP must never appear for a deterministic grade
    await expect(page.getByText(/SHAP/i)).toHaveCount(0);

    // Statistical context is clearly separate
    await expect(page.getByText("How you compare — statistical context")).toBeVisible();
    await expect(page.getByText("68th")).toBeVisible();
    await expect(page.getByText("+0.52")).toBeVisible();
  });

  test("unpublished/unknown grade returns a deliberate 404 state", async ({ page }) => {
    await mockSession(page, studentSession);
    await page.route("**/api/analytics/v1/students/me/grades/*/explanation", route =>
      route.fulfill({ status: 404, contentType: "application/json", json: { error: "Not found" } })
    );

    await page.goto(`/telemetry/grades/${gradeId}`);
    await expect(page.getByText("Grade explanation not available")).toBeVisible({ timeout: 15000 });
  });

  test("non-reproducible grade renders the unavailable explanation state without prose invention", async ({ page }) => {
    await mockSession(page, studentSession);
    await page.route(`**/api/analytics/v1/students/me/grades/${gradeId}/explanation`, route =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        json: {
          ...gradeExplanationFixture,
          availability: { status: "Unavailable", reason: "Current records cannot reproduce the persisted result." },
          data: null,
        },
      })
    );

    await page.goto(`/telemetry/grades/${gradeId}`);
    await expect(page.getByText("This grade cannot be explained from current records")).toBeVisible({ timeout: 15000 });
    await expect(page.getByText("Current records cannot reproduce the persisted result.")).toBeVisible();
  });
});

// ─── Prediction (SHAP) explanation ───

const predictionFixture = {
  explanationType: "MlPrediction",
  decisionSource: "Model",
  availability: { status: "Available", reason: null },
  explanationVersion: "prediction-explanation/v1",
  generatedAt: "2026-08-02T00:00:00Z",
  auditId: "77777777-7777-7777-7777-777777777777",
  data: {
    predictionId: "88888888-8888-8888-8888-888888888888",
    subjectId: studentSession.studentId,
    subjectType: "Student",
    modelKind: "GenuinePredictiveModel",
    modelName: "academic-risk",
    modelVersion: "2.1.0",
    featureSchemaVersion: "fs-7",
    predictionTimestamp: "2026-08-01T00:00:00Z",
    dataCutoffTimestamp: "2026-07-31T00:00:00Z",
    rawPrediction: 0.63,
    calibratedProbability: 0.61,
    baselineExpectedValue: 0.35,
    featureValues: { "Attendance trend": 0.8, "Midterm performance": 0.55, "Lab performance": 0.9, "Recent improvement": 0.4 },
    shapValues: { "Attendance trend": 0.18, "Midterm performance": 0.14, "Lab performance": -0.1, "Recent improvement": -0.05 },
    rankedLocalContributions: [
      { featureName: "Attendance trend", featureValue: 0.8, shapValue: 0.18, rank: 1 },
      { featureName: "Midterm performance", featureValue: 0.55, shapValue: 0.14, rank: 2 },
      { featureName: "Lab performance", featureValue: 0.9, shapValue: -0.1, rank: 3 },
      { featureName: "Recent improvement", featureValue: 0.4, shapValue: -0.05, rank: 4 },
    ],
    explanationAlgorithm: "SHAP",
    explanationAlgorithmVersion: "0.46",
    label: "Academic risk",
    requiresHumanReview: true,
    disclaimer: "This explanation describes model behavior only and is not a causal claim.",
  },
};

test.describe("Prediction explanation", () => {
  const predictionId = predictionFixture.data.predictionId;

  test("renders SHAP with non-causal language, signed contributions, and model metadata", async ({ page }) => {
    await mockSession(page, studentSession);
    await page.route(`**/api/analytics/v1/predictions/${predictionId}/explanation`, route =>
      route.fulfill({ status: 200, contentType: "application/json", json: predictionFixture })
    );

    await page.goto(`/telemetry/predictions/${predictionId}`);
    await expect(page.getByRole("heading", { name: "Model prediction explanation" })).toBeVisible({ timeout: 15000 });

    // Honest provenance: this IS a model output
    await expect(page.getByText("Decision source: Model")).toBeVisible();
    await expect(page.getByText("AI involved: Yes")).toBeVisible();

    // Non-causal headings and labels
    await expect(page.getByText("Factors influencing this model prediction")).toBeVisible();
    await expect(page.getByText("Factors increasing this model prediction")).toBeVisible();
    await expect(page.getByText("Factors decreasing this model prediction")).toBeVisible();
    await expect(page.getByText(/not real-world causality/i)).toBeVisible();
    await expect(page.getByText("This explanation describes model behavior only and is not a causal claim.")).toBeVisible();
    await expect(page.getByText(/causes of this outcome/i)).toHaveCount(0);

    // Signed contributions, both directions
    await expect(page.getByText("+0.180").first()).toBeVisible();
    await expect(page.getByText("-0.100").first()).toBeVisible();

    // Model metadata
    await expect(page.getByText("academic-risk")).toBeVisible();
    await expect(page.getByText("2.1.0")).toBeVisible();
    await expect(page.getByText("fs-7")).toBeVisible();
    await expect(page.getByText("Requires human review")).toBeVisible();
    await expect(page.getByText("0.630")).toBeVisible();
    await expect(page.getByText("61.0%")).toBeVisible();
  });
});

// ─── Admissions decision explanation ───

const admissionFixture = {
  explanationType: "DeterministicDecision",
  decisionSource: "Rule",
  availability: { status: "Available", reason: null },
  explanationVersion: "admission-explanation/v1",
  generatedAt: "2026-08-02T00:00:00Z",
  auditId: "99999999-9999-9999-9999-999999999999",
  data: {
    applicationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    meritListId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    decision: "Not admitted",
    eligibilityRequirements: [
      { requirement: "Academic requirement", passed: true, evidence: "FSc 82%", failureReason: null },
      { requirement: "Admission test", passed: true, evidence: "Score 71/100", failureReason: null },
      { requirement: "Documents", passed: true, evidence: "Verified", failureReason: null },
    ],
    meritComponents: [
      { name: "FSc marks", rawValue: 82, normalizedValue: 82, weight: 0.5, contribution: 41.0 },
      { name: "Admission test", rawValue: 71, normalizedValue: 71, weight: 0.5, contribution: 35.42 },
    ],
    aggregateScore: 76.42,
    rank: 418,
    seatsAvailable: 350,
    closingMerit: 78.15,
    distanceFromClosingMerit: -1.73,
    waitlistPosition: 12,
    failedPrerequisiteOrReason: null,
    policyVersion: "Fall 2026 BSCS Merit Policy",
    decisionTimestamp: "2026-07-20T00:00:00Z",
    auditReference: "cccccccc-cccc-cccc-cccc-cccccccccccc",
    limitations: ["Legacy admissions records do not persist structured policy versions."],
  },
};

test.describe("Admissions explanation", () => {
  const applicationId = admissionFixture.data.applicationId;

  test("shows merit arithmetic, non-promissory closing-merit gap, and Rule provenance", async ({ page }) => {
    await mockSession(page, adminSession);
    await page.route(`**/api/analytics/v1/admissions/${applicationId}/decision-explanation`, route =>
      route.fulfill({ status: 200, contentType: "application/json", json: admissionFixture })
    );

    await page.goto(`/telemetry/admissions/${applicationId}`);
    await expect(page.getByRole("heading", { name: "Admissions decision explanation" })).toBeVisible({ timeout: 15000 });

    await expect(page.getByText("Decision source: Rule")).toBeVisible();
    await expect(page.getByText("AI involved: No")).toBeVisible();
    await expect(page.getByText("Not admitted").first()).toBeVisible();
    await expect(page.getByText("76.42").first()).toBeVisible();
    await expect(page.getByText("78.15")).toBeVisible();
    await expect(page.getByText(/1\.73 points lower/)).toBeVisible();
    await expect(page.getByText(/non-promissory/)).toBeVisible();
    await expect(page.getByText("Fall 2026 BSCS Merit Policy")).toBeVisible();
    await expect(page.getByText("418")).toBeVisible();
    await expect(page.getByText("350")).toBeVisible();
    await expect(page.getByText(/Legacy admissions records/)).toBeVisible();
  });

  test("an unowned application is hidden by the backend and shows the restricted state", async ({ page }) => {
    await mockSession(page, studentSession);
    await page.route(`**/api/analytics/v1/admissions/${applicationId}/decision-explanation`, route =>
      route.fulfill({ status: 404, contentType: "application/json", json: { error: "Not found" } })
    );

    await page.goto(`/telemetry/admissions/${applicationId}`);
    await expect(page.getByText("Decision explanation not available")).toBeVisible({ timeout: 15000 });
  });
});

// ─── Model analytics (oversight) ───

const modelQualityFixture = {
  modelName: "academic-risk",
  modelVersion: "2.1.0",
  trainingPeriodStart: "2026-01-01T00:00:00Z",
  trainingPeriodEnd: "2026-06-30T00:00:00Z",
  trainingSampleSize: 1842,
  calibrationError: 0.04,
  auRoc: 0.87,
  precision: 0.74,
  recall: 0.69,
  specificity: null,
  f1: 0.71,
  truePositive: 120,
  trueNegative: 1450,
  falsePositive: 42,
  falseNegative: 55,
  threshold: 0.5,
  driftIndicator: 0.03,
  evaluationTimestamp: "2026-08-01T00:00:00Z",
  sensitiveOrProxyFeatureReview: ["No protected-characteristic features are present in schema fs-7."],
  fairnessMonitoringMetrics: {},
  evidenceStatement: "Evaluated against the held-out Spring 2026 cohort within the authorized tenant.",
};

const globalExplanationFixture = {
  snapshotId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
  modelName: "academic-risk",
  modelVersion: "2.1.0",
  featureSchemaVersion: "fs-7",
  authorizedPopulation: "Enrolled students, Spring 2026",
  sampleSize: 1842,
  populationStart: "2026-01-01T00:00:00Z",
  populationEnd: "2026-06-30T00:00:00Z",
  meanAbsoluteShapValues: {
    "Attendance deterioration": 0.21,
    "Midterm performance": 0.17,
    "Missing assignments": 0.12,
  },
  explanationAlgorithm: "SHAP",
  explanationAlgorithmVersion: "0.46",
  calculatedAt: "2026-08-01T00:00:00Z",
  disclaimer: "Global explanations describe aggregate model behavior and are not causal evidence.",
};

test.describe("Model analytics", () => {
  test("quality snapshot shows supplied metrics and never zero-fills nulls", async ({ page }) => {
    await mockSession(page, adminSession);
    await page.route("**/api/analytics/v1/models/academic-risk/quality**", route =>
      route.fulfill({ status: 200, contentType: "application/json", json: modelQualityFixture })
    );
    await page.route("**/api/analytics/v1/models/academic-risk/global-explanation**", route =>
      route.fulfill({ status: 200, contentType: "application/json", json: globalExplanationFixture })
    );

    await page.goto("/analytics");
    await page.getByLabel("Model name").fill("academic-risk");
    await page.getByRole("button", { name: /Load snapshots/ }).click();

    await expect(page.getByText(/Model quality — academic-risk/)).toBeVisible({ timeout: 15000 });
    await expect(page.getByText("0.870")).toBeVisible();
    await expect(page.getByText("0.740")).toBeVisible();
    // Null specificity is omitted entirely — never rendered as zero
    await expect(page.getByText("Specificity")).toHaveCount(0);
    await expect(page.getByText(/held-out Spring 2026 cohort/)).toBeVisible();

    // Global explanation: aggregate drivers with non-causal framing
    await expect(page.getByText("Top model drivers — global explanation")).toBeVisible();
    await expect(page.locator(".contribution-name", { hasText: "Attendance deterioration" })).toBeVisible();
    await expect(page.getByText(/not causal evidence/)).toBeVisible();
    await expect(page.getByText(/no individual student data/i)).toBeVisible();
  });

  test("no recorded snapshot shows a deliberate empty state", async ({ page }) => {
    await mockSession(page, adminSession);
    await page.route("**/api/analytics/v1/models/**", route =>
      route.fulfill({ status: 404, contentType: "application/json", json: { error: "Not found" } })
    );

    await page.goto("/analytics");
    await page.getByLabel("Model name").fill("unknown-model");
    await page.getByRole("button", { name: /Load snapshots/ }).click();

    await expect(page.getByText("No snapshots recorded for this model")).toBeVisible({ timeout: 15000 });
  });

  test("student role is gated away from the oversight surface", async ({ page }) => {
    await mockSession(page, studentSession);
    await page.goto("/analytics");
    await expect(page.getByText("Access restricted")).toBeVisible({ timeout: 15000 });
  });
});
