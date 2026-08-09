"use client";

// ─── Performance Telemetry (Student) ───
// Student-facing academic telemetry home. All values, statuses, trends and
// comparisons come from GET /api/analytics/v1/students/me/telemetry.
// Distinct deliberate states:
//   403 → student record not linked to this account (handoff §Contract)
//   404 → no telemetry snapshot recorded yet
//   Suppressed metrics → privacy note (never reconstructed client-side)

import { Activity } from "lucide-react";
import { AppShell } from "@/components/layout/AppShell";
import { EmptyState, ErrorState, LoadingState } from "@/components/async/states";
import { isApiError } from "@/lib/api-client";
import { getMyTelemetry } from "@/lib/analytics";
import { useSession } from "@/components/session-provider";
import { useAnalyticsFetch } from "@/components/telemetry/use-analytics";
import { TelemetryView } from "@/components/telemetry/telemetry-view";
import { useState } from "react";

export default function TelemetryPage() {
  const { status: sessionStatus } = useSession();
  const [attempt, setAttempt] = useState(0);
  const state = useAnalyticsFetch(
    sessionStatus === "authenticated" ? `my-telemetry:${attempt}` : null,
    signal => getMyTelemetry(signal)
  );

  return (
    <AppShell>
      <div className="telemetry-page">
        <header className="page-header" style={{ padding: 0 }}>
          <div>
            <div className="page-header-breadcrumb"><span>Student</span><span>/</span><span>Performance</span></div>
            <h1>Performance Telemetry</h1>
            <p>Your academic sectors, trends, and privacy-safe cohort comparisons.</p>
          </div>
        </header>

        {state.status === "loading" && <LoadingState label="Loading your telemetry…" lines={5} />}

        {state.status === "error" && isApiError(state.error) && state.error.status === 403 && (
          <EmptyState
            icon={Activity}
            title="Academic record not linked"
            description="Performance telemetry appears here once the Registrar links your student record to this account. No estimated or placeholder figures are shown."
          />
        )}

        {state.status === "error" && isApiError(state.error) && state.error.status === 404 && (
          <EmptyState
            icon={Activity}
            title="No telemetry recorded yet"
            description="Your academic telemetry will appear here after the first analytics snapshot is recorded for this period."
          />
        )}

        {state.status === "error" && (!isApiError(state.error) || (state.error.status !== 403 && state.error.status !== 404)) && (
          <ErrorState error={state.error} title="Telemetry could not be loaded" onRetry={() => setAttempt(a => a + 1)} />
        )}

        {state.status === "ok" && <TelemetryView telemetry={state.data} />}
      </div>
    </AppShell>
  );
}
