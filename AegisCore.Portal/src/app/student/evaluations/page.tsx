"use client";
import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { ApiError, apiGet } from "@/lib/api-client";
import { RegisterHead, Panel, ErrorNotice, RuledLoading, Empty, Mark } from "@/components/register";

type Evaluation = { surveyId: string; title: string; semester: string; courseId: string; courseCode: string; courseTitle: string; instructor: string | null; completed: boolean; completedAt: string | null; questions: number };

/** The course evaluations open to the signed-in student: one per course they are enrolled in. */
export default function StudentEvaluationsPage() {
  const [rows, setRows] = useState<Evaluation[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [retry, setRetry] = useState(0);

  const load = useCallback(async () => {
    try { setRows(await apiGet<Evaluation[]>("/api/obe/v2/surveys/mine")); setError(null); }
    catch (reason) { setError(reason instanceof ApiError ? reason.message : "Your course evaluations could not be read."); }
  }, []);
  useEffect(() => { Promise.resolve().then(load); }, [load, retry]);

  const crumbs = [{ label: "Student" }, { label: "Course evaluations" }];
  if (error && !rows) return (
    <div className="page">
      <RegisterHead title="Course evaluations" crumbs={crumbs} reveal={false} />
      <ErrorNotice message={error} onRetry={() => setRetry(n => n + 1)} />
    </div>
  );
  if (!rows) return <div className="page"><RuledLoading label="Reading your course evaluations" rows={3} /></div>;

  const pending = rows.filter(r => !r.completed).length;
  const surveys = [...new Set(rows.map(r => r.surveyId))];
  return (
    <div className="page">
      <RegisterHead
        title="Course evaluations"
        crumbs={crumbs}
        ident={<><strong>{pending} to complete</strong><br />{rows.length - pending} done</>}
        meta={<span>Tell the university how each of your courses went, and how well you met its learning outcomes. Your answers are anonymous: they are reported without your name, and your instructor never sees who said what.</span>}
      />
      {rows.length === 0 && <Panel title="Nothing to evaluate"><Empty title="No course evaluation is open right now.">When the university opens one, it appears here for each of your courses.</Empty></Panel>}
      {surveys.map(id => {
        const group = rows.filter(r => r.surveyId === id);
        return (
          <Panel key={id} title={group[0].title} aside={<span className="text-muted">{group[0].semester}</span>}>
            <div className="ledger-wrap">
              <table className="ledger" aria-label={group[0].title}>
                <thead><tr><th scope="col">Course</th><th scope="col">Instructor</th><th scope="col" className="num">Questions</th><th scope="col">State</th><th scope="col"><span className="sr-only">Action</span></th></tr></thead>
                <tbody>
                  {group.map(r => (
                    <tr key={r.courseId} data-status={r.completed ? "laurel" : "brass"}>
                      <td><span className="mono">{r.courseCode}</span> {r.courseTitle}</td>
                      <td>{r.instructor ?? <span className="text-muted">—</span>}</td>
                      <td className="num">{r.questions}</td>
                      <td>{r.completed ? <Mark tone="laurel">Submitted {r.completedAt ? new Date(r.completedAt).toLocaleDateString() : ""}</Mark> : <Mark tone="brass">Not started</Mark>}</td>
                      <td>{r.completed ? <span className="text-muted">Thank you</span> : <Link className="btn btn-primary btn-sm" href={`/student/evaluations/${r.surveyId}/${r.courseId}`}>Evaluate {r.courseCode}</Link>}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Panel>
        );
      })}
    </div>
  );
}
