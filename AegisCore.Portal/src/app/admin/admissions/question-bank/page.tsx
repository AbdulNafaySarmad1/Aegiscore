"use client";

import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { Panel, Mark } from "@/components/register";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Subject = { id: string; name: string };
type Question = {
  id: string; subjectId: string; questionText: string;
  optionA: string; optionB: string; optionC: string; optionD: string;
  correctOption: string; difficultyLevel: string; status: string;
};

const LETTERS = ["A", "B", "C", "D"] as const;

/** Questions waiting to be approved before they can be put in front of a candidate. */
export default function QuestionBankPage() {
  const [subjects, setSubjects] = useState<Subject[]>([]);
  const [subjectId, setSubjectId] = useState("");
  const [status, setStatus] = useState("Draft");
  const { state, reload } = useModuleData<Question[]>(
    signal => {
      const params = new URLSearchParams();
      if (subjectId) params.set("subjectId", subjectId);
      if (status) params.set("status", status);
      return apiGet<Question[]>(`/api/admissions/question-bank?${params}`, signal);
    },
    `${subjectId}|${status}`
  );
  const action = useAction();

  useEffect(() => { apiGet<Subject[]>("/api/admissions/subjects").then(setSubjects).catch(() => {}); }, []);

  async function review(q: Question, approve: boolean) {
    await action.run(q.id, async () => {
      await apiPost(`/api/admissions/question-bank/${q.id}/review`, { approve });
      return approve ? "Approved. It can now appear in a test." : "Rejected. It will not be put to a candidate.";
    }, reload);
  }

  const subjectName = (id: string) => subjects.find(s => s.id === id)?.name ?? "Unassigned";

  return (
    <ModulePage
      title="Question bank"
      crumbs={[{ label: "Admissions", href: "/admin/admissions" }, { label: "Test configuration", href: "/admin/admissions/test-config" }, { label: "Question bank" }]}
      meta={<span>A question is put to candidates only once someone has approved it. Answers are shown here because this is the screen on which they are checked.</span>}
      ident={(qs: Question[]) => <><strong>{qs.length}</strong><br />{qs.length === 1 ? "question" : "questions"}</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {questions => (
        <>
          <Panel title="Show">
            <div className="filter-row">
              <label><span>Subject</span>
                <select value={subjectId} onChange={e => setSubjectId(e.target.value)}>
                  <option value="">Every subject</option>
                  {subjects.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                </select>
              </label>
              <label><span>Status</span>
                <select value={status} onChange={e => setStatus(e.target.value)}>
                  <option value="Draft">Awaiting approval</option>
                  <option value="Approved">Approved</option>
                  <option value="Rejected">Rejected</option>
                  <option value="">Any status</option>
                </select>
              </label>
            </div>
          </Panel>

          {questions.length === 0 ? (
            <Panel>
              <p>
                {status === "Draft"
                  ? "Nothing is waiting for approval. Questions written for this subject appear here as soon as they are drafted."
                  : "No question matches what you asked for."}
              </p>
            </Panel>
          ) : questions.map(q => (
            <Panel
              key={q.id}
              title={q.questionText}
              tone={q.status === "Rejected" ? "oxblood" : q.status === "Draft" ? "brass" : undefined}
              aside={<><Mark tone={q.status === "Approved" ? "laurel" : q.status === "Rejected" ? "oxblood" : "brass"}>{q.status === "Draft" ? "Awaiting approval" : q.status}</Mark><span>{subjectName(q.subjectId)}</span><span>{q.difficultyLevel}</span></>}
            >
              <ol className="options">
                {LETTERS.map(k => (
                  <li key={k} data-correct={q.correctOption === k ? "true" : undefined}>
                    <b>{k}</b>
                    <span>{q[`option${k}` as "optionA"]}</span>
                    {q.correctOption === k && <em>the answer</em>}
                  </li>
                ))}
              </ol>
              {q.status === "Draft" && (
                <div className="btn-row">
                  <button type="button" className={`btn btn-primary btn-sm${action.busy === q.id ? " btn-loading" : ""}`} disabled={action.busy !== null} onClick={() => review(q, true)}>Approve</button>
                  <button type="button" className="btn btn-danger btn-sm" disabled={action.busy !== null} onClick={() => review(q, false)}>Reject</button>
                </div>
              )}
            </Panel>
          ))}
        </>
      )}
    </ModulePage>
  );
}
