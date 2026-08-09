"use client";

// ─── Telemetry View ───
// Renders a backend StudentTelemetry payload: context line, "lap" summary
// strip, and dimension sectors grouped by meaning. Counts in the summary
// are tallies of backend-assigned statuses — presentation only, never a
// reclassification.

import { CalendarDays, Database, Users } from "lucide-react";
import { formatDateTime, type StudentTelemetry, type TelemetryMetric } from "@/lib/analytics";
import { TelemetrySectorCard } from "./sector-card";
import { EmptyState } from "@/components/async/states";
import { Inbox } from "lucide-react";

const OVERALL_DIMENSIONS = new Set(["CoursePerformance", "Consistency", "SemesterTrajectory"]);
const ASSESSMENT_DIMENSIONS = new Set(["Assignments", "Quizzes", "Midterms", "Finals", "Labs", "Projects", "Attendance"]);
const OUTCOME_DIMENSIONS = new Set(["CLOAttainment", "PLOAttainment"]);

type MetricGroup = { title: string; sub: string; metrics: TelemetryMetric[] };

function groupMetrics(metrics: TelemetryMetric[]): MetricGroup[] {
  const overall: TelemetryMetric[] = [];
  const assessment: TelemetryMetric[] = [];
  const outcomes: TelemetryMetric[] = [];
  const other: TelemetryMetric[] = [];
  for (const m of metrics) {
    if (OVERALL_DIMENSIONS.has(m.dimension)) overall.push(m);
    else if (ASSESSMENT_DIMENSIONS.has(m.dimension)) assessment.push(m);
    else if (OUTCOME_DIMENSIONS.has(m.dimension)) outcomes.push(m);
    else other.push(m);
  }
  const groups: MetricGroup[] = [];
  if (overall.length) groups.push({ title: "Overall", sub: "Headline academic telemetry for this period.", metrics: overall });
  if (assessment.length) groups.push({ title: "Assessment sectors", sub: "Where marks are gained and lost across assessment types.", metrics: assessment });
  if (outcomes.length) groups.push({ title: "Learning outcomes", sub: "Measured CLO/PLO attainment.", metrics: outcomes });
  if (other.length) groups.push({ title: "Additional dimensions", sub: "Other recorded telemetry dimensions.", metrics: other });
  return groups;
}

/** Tallies of backend-assigned statuses (presentation counts only). */
function SummaryStrip({ metrics }: { metrics: TelemetryMetric[] }) {
  const improving = metrics.filter(m => m.improvementStatus === "Improving").length;
  const stable = metrics.filter(m => m.improvementStatus === "Stable").length;
  const declining = metrics.filter(m => m.improvementStatus === "Declining").length;
  const personalBests = metrics.filter(m => m.improvementStatus === "PersonalBest").length;
  return (
    <div className="telemetry-summary" aria-label="Progress summary">
      <span className="summary-item"><strong>{improving}</strong> improving</span>
      <span className="summary-item"><strong>{stable}</strong> stable</span>
      <span className="summary-item"><strong>{declining}</strong> declining</span>
      <span className="summary-item"><strong>{personalBests}</strong> personal {personalBests === 1 ? "best" : "bests"}</span>
    </div>
  );
}

export function TelemetryView({ telemetry }: { telemetry: StudentTelemetry }) {
  const groups = groupMetrics(telemetry.metrics ?? []);
  return (
    <>
      <div className="telemetry-context">
        <span><CalendarDays size={12} aria-hidden="true" /> Period: {telemetry.period}</span>
        <span><Users size={12} aria-hidden="true" /> Comparison: {telemetry.comparisonPopulation}</span>
        <span><Database size={12} aria-hidden="true" /> Data as of {formatDateTime(telemetry.dataCutoffTimestamp)}</span>
        <span>Updated {formatDateTime(telemetry.freshnessTimestamp)}</span>
      </div>

      {groups.length === 0 ? (
        <EmptyState
          icon={Inbox}
          title="No telemetry dimensions recorded"
          description="No metrics were present in the latest telemetry snapshot for this period."
        />
      ) : (
        <>
          <SummaryStrip metrics={telemetry.metrics} />
          {groups.map(group => (
            <section key={group.title} className="telemetry-section" aria-labelledby={`telemetry-${group.title.replace(/\s+/g, "-")}`}>
              <h2 id={`telemetry-${group.title.replace(/\s+/g, "-")}`}>{group.title}</h2>
              <p className="section-sub">{group.sub}</p>
              <div className="telemetry-grid">
                {group.metrics.map(metric => (
                  <TelemetrySectorCard key={metric.dimension} metric={metric} />
                ))}
              </div>
            </section>
          ))}
        </>
      )}
    </>
  );
}
