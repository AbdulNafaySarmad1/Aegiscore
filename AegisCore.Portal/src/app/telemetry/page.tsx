"use client";

// ─── Performance Telemetry (Student) ───
// Student-facing academic telemetry home. All values, statuses, trends and
// comparisons come from GET /api/analytics/v1/students/me/telemetry.
// Distinct deliberate states:
//   403 → student record not linked to this account (handoff §Contract)
//   404 → no telemetry snapshot recorded yet
//   Suppressed metrics → privacy note (never reconstructed client-side)

import { Activity, BookOpen, FileQuestion, GraduationCap } from "lucide-react";
import Link from "next/link";
import { AppShell } from "@/components/layout/AppShell";
import { EmptyState, ErrorState, LoadingState } from "@/components/async/states";
import { isApiError } from "@/lib/api-client";
import { getAdmissionsDiscovery, getMyAdmissionDiscovery, getMyAnalyticsDiscovery, getMyTelemetry } from "@/lib/analytics";
import { useSession } from "@/components/session-provider";
import { useAnalyticsFetch } from "@/components/telemetry/use-analytics";
import { TelemetryView } from "@/components/telemetry/telemetry-view";
import { useState } from "react";

export default function TelemetryPage() {
  const { status: sessionStatus, session } = useSession();
  const [attempt, setAttempt] = useState(0);
  const isAdmissionsStaff = session?.roles.some(role => ["Admin", "SuperAdmin", "Registrar", "AdmissionsHead"].includes(role)) ?? false;
  const state = useAnalyticsFetch(
    sessionStatus === "authenticated" && !isAdmissionsStaff ? `my-telemetry:${attempt}` : null,
    signal => getMyTelemetry(signal)
  );
  const discovery = useAnalyticsFetch(
    sessionStatus === "authenticated" ? `my-analytics-discovery:${isAdmissionsStaff}:${attempt}` : null,
    async signal => {
      const [student, applications] = await Promise.all([
        getMyAnalyticsDiscovery(signal).catch(error => {
          if (isApiError(error) && (error.status === 403 || error.status === 404)) {
            return { courses: [], grades: [], predictions: [] };
          }
          throw error;
        }),
        isAdmissionsStaff ? getAdmissionsDiscovery(signal) : getMyAdmissionDiscovery(signal),
      ]);
      return { student, applications };
    }
  );

  return (
    <AppShell>
      <div className="telemetry-page">
        <header className="page-header" style={{ padding: 0 }}>
          <div>
            <div className="page-header-breadcrumb"><span>{isAdmissionsStaff ? "Admissions" : "Student"}</span><span>/</span><span>Performance</span></div>
            <h1>{isAdmissionsStaff ? "Admissions Decision Discovery" : "Performance Telemetry"}</h1>
            <p>{isAdmissionsStaff ? "Open published decision explanations available to admissions staff." : "Your academic sectors, trends, and privacy-safe cohort comparisons."}</p>
          </div>
        </header>

        {!isAdmissionsStaff && state.status === "loading" && <LoadingState label="Loading your telemetry…" lines={5} />}

        {!isAdmissionsStaff && state.status === "error" && isApiError(state.error) && state.error.status === 403 && (
          <EmptyState
            icon={Activity}
            title="Academic record not linked"
            description="Performance telemetry appears here once the Registrar links your student record to this account. No estimated or placeholder figures are shown."
          />
        )}

        {!isAdmissionsStaff && state.status === "error" && isApiError(state.error) && state.error.status === 404 && (
          <EmptyState
            icon={Activity}
            title="No telemetry recorded yet"
            description="Your academic telemetry will appear here after the first analytics snapshot is recorded for this period."
          />
        )}

        {!isAdmissionsStaff && state.status === "error" && (!isApiError(state.error) || (state.error.status !== 403 && state.error.status !== 404)) && (
          <ErrorState error={state.error} title="Telemetry could not be loaded" onRetry={() => setAttempt(a => a + 1)} />
        )}

        {isAdmissionsStaff && discovery.status === "loading" && <LoadingState label="Loading published admissions decisions…" lines={3} />}

        {isAdmissionsStaff && discovery.status === "error" && (
          <ErrorState error={discovery.error} title="Admissions decisions could not be loaded" onRetry={() => setAttempt(a => a + 1)} />
        )}

        {!isAdmissionsStaff && state.status === "ok" && <TelemetryView telemetry={state.data} />}

        {discovery.status === "ok" && (
          <section className="card" aria-labelledby="record-links-heading" style={{ marginTop: "var(--space-6)" }}>
                <div className="card-header">
                  <div>
                    <h2 id="record-links-heading">Explore your records</h2>
                    <p className="text-muted">{isAdmissionsStaff ? "Open published application explanations in your tenant." : "Open only the courses and explanations linked to your account."}</p>
                  </div>
                </div>
                <div className="module-grid">
                  {!isAdmissionsStaff && <div>
                    <h3><BookOpen size={18} aria-hidden="true" /> Course telemetry</h3>
                    {discovery.data.student.courses.length === 0 ? <p className="text-muted">No course snapshots recorded.</p> : (
                      <ul>
                        {discovery.data.student.courses.map(course => (
                          <li key={course.courseId}>
                            <Link href={`/telemetry/courses/${course.courseId}`}>
                              {course.courseCode ? `${course.courseCode} · ` : ""}{course.courseTitle}
                            </Link>
                          </li>
                        ))}
                      </ul>
                    )}
                  </div>}
                  {!isAdmissionsStaff && <div>
                    <h3><FileQuestion size={18} aria-hidden="true" /> Grade explanations</h3>
                    {discovery.data.student.grades.length === 0 ? <p className="text-muted">No published grades available.</p> : (
                      <ul>
                        {discovery.data.student.grades.map(grade => (
                          <li key={grade.courseGradeId}>
                            <Link href={`/telemetry/grades/${grade.courseGradeId}`}>
                              Why did I get {grade.finalGrade} in {grade.courseCode ?? grade.courseTitle}?
                            </Link>
                          </li>
                        ))}
                      </ul>
                    )}
                  </div>}
                  <div>
                    <h3><GraduationCap size={18} aria-hidden="true" /> Admissions decisions</h3>
                    {discovery.data.applications.filter(application => application.hasPublishedDecision).length === 0 ? (
                      <p className="text-muted">No published admissions decision recorded.</p>
                    ) : (
                      <ul>
                        {discovery.data.applications.filter(application => application.hasPublishedDecision).map(application => (
                          <li key={application.applicationId}>
                            <Link href={`/telemetry/admissions/${application.applicationId}`}>
                               Explain {isAdmissionsStaff ? application.programName : `my ${application.programName}`} decision
                            </Link>
                          </li>
                        ))}
                      </ul>
                    )}
                  </div>
                </div>
            {!isAdmissionsStaff && (discovery.data.student.predictions.length === 0 ? (
              <p className="text-muted" role="note">No genuine model prediction snapshot is recorded for this account.</p>
            ) : (
              <div>
                <h3><Activity size={18} aria-hidden="true" /> Model prediction explanations</h3>
                <ul>
                  {discovery.data.student.predictions.map(prediction => (
                    <li key={prediction.predictionId}>
                      <Link href={`/telemetry/predictions/${prediction.predictionId}`}>
                        Explain {prediction.modelName} {prediction.modelVersion}
                      </Link>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </section>
        )}
      </div>
    </AppShell>
  );
}
