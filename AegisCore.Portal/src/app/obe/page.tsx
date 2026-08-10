"use client";

import { BookOpenCheck } from "lucide-react";
import { AppShell } from "@/components/layout/AppShell";
import { EmptyState, ErrorState, LoadingState } from "@/components/async/states";
import { useAnalyticsFetch } from "@/components/telemetry/use-analytics";
import { getObeEvidence } from "@/lib/obe";
import { useState } from "react";

export default function ObeEvidencePage() {
  const [attempt, setAttempt] = useState(0);
  const state = useAnalyticsFetch(`obe-evidence:${attempt}`, signal => getObeEvidence(signal));

  return (
    <AppShell>
      <div className="telemetry-page">
        <header className="page-header" style={{ padding: 0 }}>
          <div>
            <div className="page-header-breadcrumb"><span>Academic Quality</span><span>/</span><span>OBE Evidence</span></div>
            <h1>Outcome Evidence</h1>
            <p>Recorded course-learning outcomes and their program-outcome mappings.</p>
          </div>
        </header>

        {state.status === "loading" && <LoadingState label="Loading OBE evidence…" lines={3} />}
        {state.status === "error" && <ErrorState error={state.error} title="OBE evidence could not be loaded" onRetry={() => setAttempt(value => value + 1)} />}
        {state.status === "ok" && state.data.length === 0 && (
          <EmptyState icon={BookOpenCheck} title="No OBE evidence recorded" description="CLO-to-PO mappings will appear here after they are persisted." />
        )}
        {state.status === "ok" && state.data.length > 0 && (
          <div className="module-grid" aria-label="CLO to PO mappings">
            {state.data.map(item => (
              <article className="card" key={`${item.courseCode}:${item.cloCode}:${item.poCode}`}>
                <div className="card-header">
                  <div>
                    <h2>{item.courseCode} · {item.courseTitle}</h2>
                    <p className="text-muted">{item.programName}</p>
                  </div>
                  <span className="badge badge-success">{item.strength}</span>
                </div>
                <div className="card-body">
                  <p><strong>{item.cloCode}</strong> · {item.cloDescription}</p>
                  <p><strong>{item.poCode}</strong> · {item.poDescription}</p>
                </div>
              </article>
            ))}
          </div>
        )}
      </div>
    </AppShell>
  );
}
