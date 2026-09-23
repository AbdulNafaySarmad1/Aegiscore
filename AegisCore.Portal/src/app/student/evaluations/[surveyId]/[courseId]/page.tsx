"use client";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import { ApiError, apiGet, apiPost } from "@/lib/api-client";
import { RegisterHead, Panel, Notice, ErrorNotice, RuledLoading, ProgressRule } from "@/components/register";

type Question = { key: string; number: number; text: string; kind: "Rating" | "Comment"; required: boolean; code: string | null };
type Form = {
  surveyId: string; title: string; instructions: string | null; semester: string; scaleMax: number;
  courseId: string; courseCode: string; courseTitle: string; instructor: string | null;
  sections: { title: string; prompt: string | null; questions: Question[] }[]; questions: number;
};
type Receipt = { completedAt: string; ratings: number; comments: number };

const FIVE = ["Strongly disagree", "Disagree", "Neutral", "Agree", "Strongly agree"];
const labelsFor = (max: number) => max === 5 ? FIVE : Array.from({ length: max }, (_, i) => String(i + 1));

/** One course evaluation, answered by a student. Submitted once; the answers are stored without the student. */
export default function EvaluationFormPage() {
  const { surveyId, courseId } = useParams<{ surveyId: string; courseId: string }>();
  const [form, setForm] = useState<Form | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const [missing, setMissing] = useState<Set<string>>(new Set());
  const [busy, setBusy] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [receipt, setReceipt] = useState<Receipt | null>(null);

  useEffect(() => {
    apiGet<Form>(`/api/obe/v2/surveys/${surveyId}/courses/${courseId}`).then(setForm)
      .catch(reason => setError(reason instanceof ApiError ? reason.message : "The evaluation could not be opened."));
  }, [surveyId, courseId]);

  const required = useMemo(() => form?.sections.flatMap(s => s.questions).filter(q => q.kind === "Rating" && q.required) ?? [], [form]);
  const answered = required.filter(q => answers[q.key]).length;

  async function submit() {
    if (!form) return;
    const gaps = required.filter(q => !answers[q.key]).map(q => q.key);
    if (gaps.length > 0) {
      setMissing(new Set(gaps));
      setSubmitError(`${gaps.length} question${gaps.length === 1 ? " is" : "s are"} not answered yet — marked below.`);
      document.getElementById(`q-${gaps[0]}`)?.scrollIntoView({ behavior: "smooth", block: "center" });
      return;
    }
    setBusy(true); setSubmitError(null);
    try {
      setReceipt(await apiPost<Receipt>(`/api/obe/v2/surveys/${surveyId}/courses/${courseId}`, { answers }));
      window.scrollTo({ top: 0, behavior: "smooth" });
    } catch (reason) { setSubmitError(reason instanceof ApiError ? reason.message : "The evaluation could not be submitted. Your answers are still here; try again."); }
    finally { setBusy(false); }
  }

  const crumbs = [{ label: "Student" }, { label: "Course evaluations", href: "/student/evaluations" }, { label: form?.courseCode ?? "Evaluation" }];
  if (error) return (
    <div className="page">
      <RegisterHead title="Course evaluation" crumbs={crumbs} reveal={false} />
      <ErrorNotice message={error} />
      <p><Link href="/student/evaluations">Back to course evaluations</Link></p>
    </div>
  );
  if (!form) return <div className="page"><RuledLoading label="Opening the evaluation" rows={6} /></div>;

  if (receipt) return (
    <div className="page">
      <RegisterHead title="Thank you" crumbs={crumbs} ident={<><strong>{form.courseCode}</strong><br />Submitted</>} />
      <Notice tone="laurel" role="status" title={`Your evaluation of ${form.courseCode} ${form.courseTitle} has been submitted.`}>
        <p data-testid="evaluation-receipt">{receipt.ratings} ratings{receipt.comments ? ` and ${receipt.comments} comment${receipt.comments === 1 ? "" : "s"}` : ""} recorded at {new Date(receipt.completedAt).toLocaleTimeString()}. They are stored without your name.</p>
      </Notice>
      <p><Link className="btn btn-primary" href="/student/evaluations">Back to course evaluations</Link></p>
    </div>
  );

  const scale = labelsFor(form.scaleMax);
  return (
    <div className="page evaluation-form">
      <RegisterHead
        title={`${form.courseCode} ${form.courseTitle}`}
        crumbs={crumbs}
        ident={<><strong>{form.questions} questions</strong><br />{form.semester}</>}
        meta={<span>{form.title}{form.instructor ? ` · Instructor: ${form.instructor}` : ""}. {form.instructions ?? ""} Your answers are anonymous.</span>}
      />
      <div className="evaluation-progress" aria-live="polite">
        <ProgressRule value={answered} max={Math.max(1, required.length)} label={`${answered} of ${required.length} answered`} />
      </div>
      {form.sections.map(section => (
        <Panel key={section.title} title={section.title}>
          {section.prompt && <p className="evaluation-prompt">{section.prompt}</p>}
          {section.questions.map(q => (
            <fieldset key={q.key} id={`q-${q.key}`} className={`evaluation-question${missing.has(q.key) && !answers[q.key] ? " is-missing" : ""}`}>
              <legend><span className="evaluation-number">{q.number}.</span> {q.text}{q.code && <span className="evaluation-code mono">{q.code}</span>}</legend>
              {q.kind === "Comment" ? (
                <textarea rows={3} aria-label={`Question ${q.number}`} value={answers[q.key] ?? ""} onChange={e => setAnswers(a => ({ ...a, [q.key]: e.target.value }))} />
              ) : (
                <div className="evaluation-scale" role="radiogroup" aria-label={`Question ${q.number}`}>
                  {scale.map((label, i) => (
                    <label key={label} className={answers[q.key] === String(i + 1) ? "is-chosen" : undefined}>
                      <input type="radio" name={q.key} value={i + 1} checked={answers[q.key] === String(i + 1)} onChange={() => setAnswers(a => ({ ...a, [q.key]: String(i + 1) }))} />
                      <span>{label}</span>
                    </label>
                  ))}
                </div>
              )}
            </fieldset>
          ))}
        </Panel>
      ))}
      {submitError && <ErrorNotice message={submitError} />}
      <div className="toolbar">
        <button type="button" className={`btn btn-primary${busy ? " btn-loading" : ""}`} disabled={busy} onClick={submit}>Submit evaluation</button>
        <Link className="btn btn-quiet" href="/student/evaluations">Not now</Link>
        <span className="text-muted">Once submitted, an evaluation cannot be changed.</span>
      </div>
    </div>
  );
}
