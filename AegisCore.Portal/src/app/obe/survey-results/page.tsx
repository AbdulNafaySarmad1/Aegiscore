"use client";
import { useCallback, useEffect, useState } from "react";
import { ApiError, apiGet } from "@/lib/api-client";
import { RegisterHead, Panel, Figures, ErrorNotice, RuledLoading, Empty } from "@/components/register";
import { Ledger } from "@/components/register/ledger";

type Rating = { answers: number; mean: number; scaleMax: number; favourablePercent: number };
type Report = {
  generatedAt: string; respondents: number; answers: number; lastRecordedAt: string | null;
  channels: { channel: string; respondents: number; answers: number }[];
  courses: {
    courseId: string; code: string; title: string; instructors: string[]; respondents: number; overall: Rating;
    clos: { outcomeId: string; code: string; description: string; rating: Rating }[];
    satisfaction: { category: string; rating: Rating }[];
  }[];
  instructors: { instructor: string; courses: string[]; respondents: number; rating: Rating }[];
  programOutcomes: { outcomeId: string; code: string; description: string; answers: number; percentOfScale: number; contributingClos: string[] }[];
};
type Course = { id: string; code: string | null; title: string };
type Survey = { id: string; title: string; semester: string; status: string };

const CHANNEL_LABEL: Record<string, string> = { native: "AegisCore course evaluations", "external:jotform": "JotForm", "external:generic": "Other form tools" };

function Bar({ percent }: { percent: number }) {
  return <span className="survey-bar" aria-hidden="true"><span style={{ width: `${Math.max(0, Math.min(100, percent))}%` }} /></span>;
}
const mean = (r: Rating) => `${r.mean.toFixed(2)} / ${r.scaleMax}`;

/**
 * The indirect side of outcome-based assessment: what students said, in surveys, about each
 * course's CLOs and its teaching, rolled up to the programme outcomes through the same CLO→PLO
 * mapping (and weights) the direct attainment uses. A response is counted the same whether it was
 * given on an AegisCore form or in an outside tool such as JotForm.
 */
export default function SurveyResultsPage() {
  const [report, setReport] = useState<Report | null>(null);
  const [courses, setCourses] = useState<Course[]>([]);
  const [courseId, setCourseId] = useState("");
  const [surveys, setSurveys] = useState<Survey[]>([]);
  // A link from the course evaluations page lands on that survey's results.
  const [surveyId, setSurveyId] = useState(() => typeof window === "undefined" ? "" : new URLSearchParams(window.location.search).get("survey") ?? "");
  const [error, setError] = useState<string | null>(null);
  const [retry, setRetry] = useState(0);
  const [live, setLive] = useState(true);

  const load = useCallback(async () => {
    try {
      const params = new URLSearchParams();
      if (courseId) params.set("courseId", courseId);
      if (surveyId) params.set("surveyId", surveyId);
      const query = params.size ? `?${params}` : "";
      setReport(await apiGet<Report>(`/api/obe/v2/indirect${query}`));
      setError(null);
    } catch (reason) { setError(reason instanceof ApiError ? reason.message : "The survey results could not be read."); }
  }, [courseId, surveyId]);

  useEffect(() => { Promise.resolve().then(load); }, [load, retry]);
  useEffect(() => {
    apiGet<{ courses: Course[] }>("/api/obe/v2/outcomes").then(data => setCourses(data.courses)).catch(() => setCourses([]));
    apiGet<Survey[]>("/api/obe/v2/surveys").then(setSurveys).catch(() => setSurveys([]));
  }, []);
  // The report re-reads itself while it is open, so a response submitted now is visible in seconds.
  useEffect(() => {
    if (!live) return;
    const timer = window.setInterval(() => { if (!document.hidden) void load(); }, 5000);
    return () => window.clearInterval(timer);
  }, [live, load]);

  const crumbs = [{ label: "Outcome-based education" }, { label: "Survey results" }];
  if (error && !report) return (
    <div className="page">
      <RegisterHead title="Survey results" crumbs={crumbs} reveal={false} />
      <ErrorNotice message={error} hint="You need the Grades read permission to open this page." onRetry={() => setRetry(n => n + 1)} />
    </div>
  );
  if (!report) return <div className="page"><RuledLoading label="Reading the survey results" rows={5} /></div>;

  return (
    <div className="page survey-results">
      <RegisterHead
        title="Survey results"
        crumbs={crumbs}
        ident={<><strong>{report.respondents} response{report.respondents === 1 ? "" : "s"}</strong><br />{report.answers} rated answers</>}
        meta={
          <div className="toolbar" role="group" aria-label="Filter results">
            <label>Survey
              <select value={surveyId} onChange={e => setSurveyId(e.target.value)}>
                <option value="">All surveys and sources</option>
                {surveys.map(s => <option key={s.id} value={s.id}>{s.title} ({s.status.toLowerCase()})</option>)}
              </select>
            </label>
            <label>Course
              <select value={courseId} onChange={e => setCourseId(e.target.value)}>
                <option value="">All courses</option>
                {courses.map(c => <option key={c.id} value={c.id}>{c.code} {c.title}</option>)}
              </select>
            </label>
            <label className="check-row"><input type="checkbox" checked={live} onChange={e => setLive(e.target.checked)} /><span>Update live</span></label>
            <span className="survey-live text-muted" data-testid="survey-updated">Read at {new Date(report.generatedAt).toLocaleTimeString()}</span>
          </div>
        }
      />

      <Figures aria-label="Totals" items={[
        { label: "Responses", value: report.respondents },
        { label: "Rated answers", value: report.answers },
        { label: "Latest response", value: report.lastRecordedAt ? new Date(report.lastRecordedAt).toLocaleTimeString() : "None yet", note: report.lastRecordedAt ? new Date(report.lastRecordedAt).toLocaleDateString() : undefined },
        ...report.channels.map(c => ({ label: `From ${CHANNEL_LABEL[c.channel] ?? c.channel}`, value: c.respondents, note: `${c.answers} answers` })),
      ]} />

      {report.courses.length === 0 && (
        <Panel title="No survey answers yet"><Empty title="Nothing to report.">Answers appear here once a mapped survey response is received.</Empty></Panel>
      )}

      {report.courses.map(course => (
        <Panel key={course.courseId} title={`${course.code} ${course.title}`}
          aside={<span className="text-muted">{course.instructors.join(", ") || "No instructor recorded"} · {course.respondents} response{course.respondents === 1 ? "" : "s"}</span>}>
          <Ledger caption={`${course.code} course learning outcomes`} rows={course.clos} keyOf={r => r.outcomeId}
            empty={{ title: "No question on the survey rates a CLO of this course." }}
            columns={[
              { key: "code", label: "CLO", mono: true, nowrap: true, render: r => r.code },
              { key: "description", label: "Outcome", render: r => r.description },
              { key: "answers", label: "Answers", num: true, render: r => r.rating.answers },
              { key: "mean", label: "Mean", num: true, nowrap: true, render: r => mean(r.rating) },
              { key: "favourable", label: "Agree or better", nowrap: true, render: r => <><Bar percent={r.rating.favourablePercent} />{r.rating.favourablePercent.toFixed(1)}%</> },
            ]} />
          {course.satisfaction.length > 0 && (
            <Ledger caption={`${course.code} teaching and satisfaction`} rows={course.satisfaction} keyOf={r => r.category}
              empty={{ title: "No teaching items." }}
              columns={[
                { key: "category", label: "Teaching and satisfaction", render: r => r.category },
                { key: "answers", label: "Answers", num: true, render: r => r.rating.answers },
                { key: "mean", label: "Mean", num: true, nowrap: true, render: r => mean(r.rating) },
                { key: "favourable", label: "Agree or better", nowrap: true, render: r => <><Bar percent={r.rating.favourablePercent} />{r.rating.favourablePercent.toFixed(1)}%</> },
              ]} />
          )}
        </Panel>
      ))}

      {report.programOutcomes.length > 0 && (
        <Panel title="Programme outcomes (indirect)">
          <p className="text-muted">Each PLO combines answers that rate it directly with answers about the CLOs mapped to it, weighted by mapping strength (high 1, medium 0.66, low 0.33) — the same weights as the direct attainment.</p>
          <Ledger caption="Programme outcomes" rows={report.programOutcomes} keyOf={r => r.outcomeId}
            empty={{ title: "No programme outcome is reached by the survey yet." }}
            columns={[
              { key: "code", label: "PLO", mono: true, nowrap: true, render: r => r.code },
              { key: "description", label: "Outcome", render: r => r.description },
              { key: "answers", label: "Answers", num: true, render: r => r.answers },
              { key: "percent", label: "Mean as % of scale", nowrap: true, render: r => <><Bar percent={r.percentOfScale} />{r.percentOfScale.toFixed(1)}%</> },
              { key: "from", label: "From", render: r => r.contributingClos.join(", ") || "Direct questions" },
            ]} />
        </Panel>
      )}

      {report.instructors.length > 0 && (
        <Panel title="Instructors">
          <Ledger caption="Instructors" rows={report.instructors} keyOf={r => r.instructor}
            empty={{ title: "No instructor recorded." }}
            columns={[
              { key: "instructor", label: "Instructor", render: r => r.instructor },
              { key: "courses", label: "Courses", mono: true, render: r => r.courses.join(", ") },
              { key: "respondents", label: "Responses", num: true, render: r => r.respondents },
              { key: "mean", label: "Mean (teaching items)", num: true, nowrap: true, render: r => mean(r.rating) },
              { key: "favourable", label: "Agree or better", nowrap: true, render: r => <><Bar percent={r.rating.favourablePercent} />{r.rating.favourablePercent.toFixed(1)}%</> },
            ]} />
        </Panel>
      )}
    </div>
  );
}
