"use client";

// ─── Course Telemetry (Student drill-down) ───
// Per-course telemetry from GET /api/analytics/v1/students/me/courses/{id}.
// Answers: where am I strong, where am I losing performance, how am I
// trending, how do I compare — using only backend-supplied dimensions.

import { use, useState } from "react";
import Link from "next/link";
import { Activity, ArrowLeft } from "lucide-react";
import { AppShell } from "@/components/layout/AppShell";
import { EmptyState, ErrorState, LoadingState } from "@/components/async/states";
import { isApiError } from "@/lib/api-client";
import { getMyCourseTelemetry } from "@/lib/analytics";
import { useAnalyticsFetch } from "@/components/telemetry/use-analytics";
import { TelemetryView } from "@/components/telemetry/telemetry-view";

export default function CourseTelemetryPage({ params }: { params: Promise<{ courseId: string }> }) {
  const { courseId } = use(params);
  const [attempt, setAttempt] = useState(0);
  const state = useAnalyticsFetch(`course-telemetry:${courseId}:${attempt}`, signal =>
    getMyCourseTelemetry(courseId, signal)
  );

  return (
    <AppShell>
      <div className="telemetry-page">
        <header className="page-header" style={{ padding: 0 }}>
          <div>
            <div className="page-header-breadcrumb">
              <Link href="/telemetry">Performance</Link><span>/</span><span>Course</span>
            </div>
            <h1>Course Telemetry</h1>
            <p>Assessment, outcome, and attendance telemetry for this course.</p>
          </div>
          <div className="page-header-actions">
            <Link href="/telemetry" className="btn btn-secondary btn-sm"><ArrowLeft size={14} /> All telemetry</Link>
          </div>
        </header>

        {state.status === "loading" && <LoadingState label="Loading course telemetry…" lines={5} />}

        {state.status === "error" && isApiError(state.error) && state.error.status === 403 && (
          <EmptyState
            icon={Activity}
            title="Academic record not linked"
            description="Course telemetry appears here once the Registrar links your student record to this account."
          />
        )}

        {state.status === "error" && isApiError(state.error) && state.error.status === 404 && (
          <EmptyState
            icon={Activity}
            title="No telemetry for this course"
            description="No analytics snapshot has been recorded for you in this course, or the course is not part of your record."
          />
        )}

        {state.status === "error" && (!isApiError(state.error) || (state.error.status !== 403 && state.error.status !== 404)) && (
          <ErrorState error={state.error} title="Course telemetry could not be loaded" onRetry={() => setAttempt(a => a + 1)} />
        )}

        {state.status === "ok" && <TelemetryView telemetry={state.data} />}
      </div>
    </AppShell>
  );
}
