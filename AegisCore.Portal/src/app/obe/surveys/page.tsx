"use client";
import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { ApiError, apiDelete, apiGet, apiPost, apiPut } from "@/lib/api-client";
import { RegisterHead, Panel, Facts, Notice, ErrorNotice, RuledLoading, Empty, Mark, ProgressRule, type Tone } from "@/components/register";

type Summary = { id: string; title: string; semester: string; status: string; questions: number; askCourseClos: boolean; eligible: number; completed: number; createdAt: string; openedAt: string | null; closedAt: string | null };
type Question = { section: string; text: string; kind: "Rating" | "Comment"; category: string | null; programOutcomeId: string | null; required: boolean };
type Detail = {
  id: string; title: string; instructions: string | null; semester: string; status: string; scaleMax: number; askCourseClos: boolean;
  cloSection: string; cloPrompt: string; openedAt: string | null; closedAt: string | null; allCourses: boolean;
  questions: (Question & { id: string; sortOrder: number; programOutcomeCode: string | null })[];
  courses: { courseId: string; code: string; title: string; instructor: string | null; enrolled: number; completed: number; clos: string[] }[];
};
type Outcomes = { courses: { id: string; code: string | null; title: string; clos: { code: string }[] }[]; programs: { id: string; name: string; outcomes: { id: string; code: string; description: string }[] }[] };
type Draft = { title: string; semester: string; instructions: string; scaleMax: number; askCourseClos: boolean; cloSection: string; cloPrompt: string; questions: Question[]; courseIds: string[] };

const STATUS_TONE: Record<string, Tone> = { Draft: "brass", Open: "laurel", Closed: "ink" };
const blank: Draft = { title: "", semester: "", instructions: "", scaleMax: 5, askCourseClos: true, cloSection: "Course learning outcomes", cloPrompt: "By the end of this course I am able to:", questions: [], courseIds: [] };

/**
 * One section per "# heading" line, one question per line under it. A line ending in "[comment]"
 * is a free-text question; "[PLO2]" makes a rating count directly towards that programme outcome.
 */
function parseQuestions(text: string, plos: { id: string; code: string }[]): { questions: Question[]; problems: string[] } {
  const questions: Question[] = [];
  const problems: string[] = [];
  let section = "General";
  text.split(/\r?\n/).forEach((raw, index) => {
    const line = raw.trim();
    if (!line) return;
    if (line.startsWith("#")) { section = line.replace(/^#+\s*/, "") || "General"; return; }
    let body = line.replace(/^\d+[.)]\s+/, "");
    let kind: Question["kind"] = "Rating";
    let programOutcomeId: string | null = null;
    const tag = body.match(/\s*\[([^\]]+)\]\s*$/);
    if (tag) {
      body = body.slice(0, tag.index).trim();
      const value = tag[1].trim();
      if (/^comment$/i.test(value)) kind = "Comment";
      else {
        const plo = plos.find(p => p.code.toLowerCase() === value.toLowerCase());
        if (plo) programOutcomeId = plo.id; else problems.push(`Line ${index + 1}: “${value}” is not a programme outcome code.`);
      }
    }
    questions.push({ section, text: body, kind, category: null, programOutcomeId, required: kind === "Rating" });
  });
  return { questions, problems };
}

export default function SurveysPage() {
  const [list, setList] = useState<Summary[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [retry, setRetry] = useState(0);
  const [selected, setSelected] = useState<Detail | null>(null);
  const [editing, setEditing] = useState<{ id: string | null; draft: Draft } | null>(null);
  const [pasted, setPasted] = useState("");
  const [plos, setPlos] = useState<{ id: string; code: string; description: string }[]>([]);
  const [courses, setCourses] = useState<Outcomes["courses"]>([]);
  const [busy, setBusy] = useState<string | null>(null);
  const [outcome, setOutcome] = useState<{ tone: Tone; text: string } | null>(null);

  const load = useCallback(async () => {
    try { setList(await apiGet<Summary[]>("/api/obe/v2/surveys")); setError(null); }
    catch (reason) { setError(reason instanceof ApiError ? reason.message : "The surveys could not be read."); }
  }, []);
  useEffect(() => { Promise.resolve().then(load); }, [load, retry]);
  useEffect(() => {
    apiGet<Outcomes>("/api/obe/v2/outcomes").then(o => { setPlos(o.programs.flatMap(p => p.outcomes)); setCourses(o.courses); }).catch(() => setPlos([]));
  }, []);
  // An open survey's response rate is watched while it is on screen.
  useEffect(() => {
    if (!selected || selected.status !== "Open") return;
    const timer = window.setInterval(() => {
      if (!document.hidden) apiGet<Detail>(`/api/obe/v2/surveys/${selected.id}`).then(setSelected).catch(() => undefined);
    }, 5000);
    return () => window.clearInterval(timer);
  }, [selected]);

  async function run(label: string, action: () => Promise<void>) {
    setBusy(label); setOutcome(null);
    try { await action(); }
    catch (reason) { setOutcome({ tone: "oxblood", text: reason instanceof ApiError ? reason.message : "That did not work; nothing was changed." }); }
    finally { setBusy(null); }
  }

  const open = (id: string) => run("load", async () => { setEditing(null); setSelected(await apiGet<Detail>(`/api/obe/v2/surveys/${id}`)); });

  const edit = (detail: Detail | null) => {
    setSelected(null); setPasted("");
    setEditing(detail ? {
      id: detail.id, draft: {
        title: detail.title, semester: detail.semester, instructions: detail.instructions ?? "", scaleMax: detail.scaleMax, askCourseClos: detail.askCourseClos,
        cloSection: detail.cloSection, cloPrompt: detail.cloPrompt,
        questions: detail.questions.map(q => ({ section: q.section, text: q.text, kind: q.kind, category: q.category, programOutcomeId: q.programOutcomeId, required: q.required })),
        courseIds: detail.allCourses ? [] : detail.courses.map(c => c.courseId),
      },
    } : { id: null, draft: blank });
  };

  const save = () => run("save", async () => {
    if (!editing) return;
    const body = { ...editing.draft, instructions: editing.draft.instructions || null };
    const saved = editing.id ? await apiPut<Detail>(`/api/obe/v2/surveys/${editing.id}`, body) : await apiPost<Detail>("/api/obe/v2/surveys", body);
    setEditing(null); setSelected(saved); await load();
    setOutcome({ tone: "laurel", text: `“${saved.title}” saved as a draft with ${saved.questions.length} question${saved.questions.length === 1 ? "" : "s"}${saved.askCourseClos ? ", plus each course's own CLOs" : ""}.` });
  });

  const setStatus = (status: "open" | "close") => run(status, async () => {
    if (!selected) return;
    if (status === "close" && !window.confirm("Close this survey? Students will no longer be able to answer it.")) return;
    const updated = await apiPost<Detail>(`/api/obe/v2/surveys/${selected.id}/${status}`);
    setSelected(updated); await load();
    setOutcome({ tone: "laurel", text: status === "open" ? "The survey is open. Every enrolled student now sees it under Course evaluations." : "The survey is closed." });
  });

  const remove = () => run("delete", async () => {
    if (!selected || !window.confirm(`Delete “${selected.title}” and every answer given in it? This cannot be undone.`)) return;
    const result = await apiDelete<{ answers: number }>(`/api/obe/v2/surveys/${selected.id}`);
    setSelected(null); await load();
    setOutcome({ tone: "laurel", text: `Survey deleted, with ${result.answers} recorded answer${result.answers === 1 ? "" : "s"}.` });
  });

  const crumbs = [{ label: "Outcome-based education" }, { label: "Course evaluations" }];
  if (error && !list) return (
    <div className="page">
      <RegisterHead title="Course evaluations" crumbs={crumbs} reveal={false} />
      <ErrorNotice message={error} hint="You need the Grades permission to open this page." onRetry={() => setRetry(n => n + 1)} />
    </div>
  );
  if (!list) return <div className="page"><RuledLoading label="Reading the surveys" rows={4} /></div>;

  const d = editing?.draft;
  const setDraft = (change: Partial<Draft>) => setEditing(current => current ? { ...current, draft: { ...current.draft, ...change } } : current);
  const ratings = d?.questions.filter(q => q.kind === "Rating").length ?? 0;
  const parsed = pasted.trim() ? parseQuestions(pasted, plos) : null;

  return (
    <div className="page course-evaluations">
      <RegisterHead
        title="Course evaluations"
        crumbs={crumbs}
        ident={<><strong>{list.length} survey{list.length === 1 ? "" : "s"}</strong><br />{list.filter(s => s.status === "Open").length} open</>}
        meta={<span>Build the end-of-semester course and teacher evaluation, open it, and every enrolled student answers it once per course — with that course&apos;s own CLOs. Results go straight to the CLO/PLO survey report.</span>}
        actions={<>
          <Link className="btn btn-secondary" href="/obe/survey-results">Survey results</Link>
          <button type="button" className="btn btn-primary" onClick={() => edit(null)}>New survey</button>
        </>}
      />
      {outcome && <Notice tone={outcome.tone} role="status">{outcome.text}</Notice>}

      {editing && d && (
        <Panel title={editing.id ? "Edit survey" : "New survey"}>
          <label><span>Title</span><input value={d.title} onChange={e => setDraft({ title: e.target.value })} placeholder="Course & Teacher Evaluation — Fall 2026" /></label>
          <label><span>Semester</span><input value={d.semester} onChange={e => setDraft({ semester: e.target.value })} placeholder="Fall 2026" /></label>
          <label><span>Instructions to students</span><textarea rows={2} value={d.instructions} onChange={e => setDraft({ instructions: e.target.value })} /></label>
          <label><span>Rating scale</span>
            <select value={d.scaleMax} onChange={e => setDraft({ scaleMax: Number(e.target.value) })}>
              <option value={5}>1 to 5 — Strongly disagree … Strongly agree</option>
              <option value={4}>1 to 4</option>
              <option value={7}>1 to 7</option>
              <option value={10}>1 to 10</option>
            </select>
          </label>
          <fieldset className="course-evaluations-courses">
            <legend>Courses this survey covers</legend>
            <label className="check-row"><input type="checkbox" checked={d.courseIds.length === 0} onChange={e => setDraft({ courseIds: e.target.checked ? [] : courses.slice(0, 1).map(c => c.id) })} /><span>Every course with enrolled students</span></label>
            {d.courseIds.length > 0 && <div className="course-evaluations-course-grid">
              {courses.map(c => (
                <label key={c.id} className="check-row">
                  <input type="checkbox" checked={d.courseIds.includes(c.id)} onChange={e => setDraft({ courseIds: e.target.checked ? [...d.courseIds, c.id] : d.courseIds.filter(x => x !== c.id) })} />
                  <span><span className="mono">{c.code}</span> {c.title}{c.clos.length ? <small className="text-muted"> · {c.clos.length} CLOs</small> : null}</span>
                </label>
              ))}
            </div>}
          </fieldset>
          <label className="check-row">
            <input type="checkbox" checked={d.askCourseClos} onChange={e => setDraft({ askCourseClos: e.target.checked })} />
            <span>Ask each student to rate the CLOs of the course they are evaluating, in that course&apos;s own words</span>
          </label>
          {d.askCourseClos && <>
            <label><span>CLO section heading</span><input value={d.cloSection} onChange={e => setDraft({ cloSection: e.target.value })} /></label>
            <label><span>CLO prompt</span><input value={d.cloPrompt} onChange={e => setDraft({ cloPrompt: e.target.value })} /></label>
          </>}

          <h3 className="course-evaluations-sub">Questions <small className="text-muted">({d.questions.length} — {ratings} rated, {d.questions.length - ratings} comment)</small></h3>
          <details className="form-integrations-import" open={d.questions.length === 0}>
            <summary>Paste the question list</summary>
            <p className="text-muted">One question per line. Start a section with <code># Heading</code> — ratings are reported under their section&apos;s heading. End a line with <code>[comment]</code> for a free-text answer, or with a PLO code such as <code>[PLO2]</code> to count it towards that programme outcome.</p>
            <textarea rows={8} value={pasted} onChange={e => setPasted(e.target.value)} aria-label="Question list" placeholder={"# The instructor\nThe instructor came to class prepared\nThe instructor explained concepts clearly\n# Overall\nOverall, I am satisfied with this course\nAny other comments? [comment]"} />
            {parsed && <p className="text-muted" data-testid="paste-preview">{parsed.questions.length} question{parsed.questions.length === 1 ? "" : "s"} in {new Set(parsed.questions.map(q => q.section)).size} section{new Set(parsed.questions.map(q => q.section)).size === 1 ? "" : "s"}{parsed.problems.length ? ` — ${parsed.problems.join(" ")}` : ""}</p>}
            <button type="button" className="btn btn-secondary" disabled={!parsed || parsed.questions.length === 0 || parsed.problems.length > 0}
              onClick={() => { if (parsed) { setDraft({ questions: [...d.questions, ...parsed.questions] }); setPasted(""); } }}>Add these questions</button>
          </details>

          {d.questions.length > 0 && d.askCourseClos && <p className="text-muted">Students see the CLO questions of their course first, then these {d.questions.length}.</p>}
          {d.questions.length > 0 && (
            <div className="ledger-wrap form-integrations-mapping">
              <table className="ledger" aria-label="Survey questions">
                <thead><tr><th scope="col" className="num">#</th><th scope="col">Section</th><th scope="col">Question</th><th scope="col">Kind</th><th scope="col">Counts towards</th><th scope="col"><span className="sr-only">Remove</span></th></tr></thead>
                <tbody>
                  {d.questions.map((q, i) => (
                    <tr key={i}>
                      <td className="num">{i + 1}</td>
                      <td><input aria-label={`Section of question ${i + 1}`} value={q.section} onChange={e => setDraft({ questions: d.questions.map((x, j) => j === i ? { ...x, section: e.target.value } : x) })} /></td>
                      <td><input aria-label={`Text of question ${i + 1}`} value={q.text} style={{ minWidth: 360 }} onChange={e => setDraft({ questions: d.questions.map((x, j) => j === i ? { ...x, text: e.target.value } : x) })} /></td>
                      <td>
                        <select aria-label={`Kind of question ${i + 1}`} value={q.kind} onChange={e => setDraft({ questions: d.questions.map((x, j) => j === i ? { ...x, kind: e.target.value as Question["kind"] } : x) })}>
                          <option value="Rating">Rating</option><option value="Comment">Comment</option>
                        </select>
                      </td>
                      <td>
                        {q.kind === "Rating" ? (
                          <select aria-label={`What question ${i + 1} counts towards`} value={q.programOutcomeId ?? ""} onChange={e => setDraft({ questions: d.questions.map((x, j) => j === i ? { ...x, programOutcomeId: e.target.value || null } : x) })}>
                            <option value="">“{q.category || q.section}” heading</option>
                            {plos.map(p => <option key={p.id} value={p.id}>{p.code} (programme outcome)</option>)}
                          </select>
                        ) : <span className="text-muted">Not scored</span>}
                      </td>
                      <td><button type="button" className="btn btn-quiet btn-sm" aria-label={`Remove question ${i + 1}`} onClick={() => setDraft({ questions: d.questions.filter((_, j) => j !== i) })}>Remove</button></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <div className="toolbar">
            <button type="button" className={`btn btn-primary${busy === "save" ? " btn-loading" : ""}`} disabled={busy !== null || !d.title.trim() || !d.semester.trim()} onClick={save}>Save draft</button>
            <button type="button" className="btn btn-quiet" onClick={() => setEditing(null)}>Cancel</button>
          </div>
        </Panel>
      )}

      {selected && !editing && (
        <Panel title={selected.title} aside={<Mark tone={STATUS_TONE[selected.status] ?? "ink"}>{selected.status}</Mark>}>
          <Facts inline items={[
            { label: "Semester", value: selected.semester },
            { label: "Fixed questions", value: String(selected.questions.length) },
            { label: "Plus", value: selected.askCourseClos ? "each course's own CLOs" : "no CLO questions" },
            { label: "Courses", value: selected.allCourses ? "Every course" : selected.courses.map(c => c.code).join(", ") },
            { label: "Scale", value: `1 to ${selected.scaleMax}` },
            { label: "Opened", value: selected.openedAt ? new Date(selected.openedAt).toLocaleString() : "Not yet" },
          ]} />
          <div className="toolbar">
            {selected.status === "Draft" && <button type="button" className="btn btn-secondary" onClick={() => edit(selected)}>Edit</button>}
            {selected.status === "Draft" && <button type="button" className={`btn btn-primary${busy === "open" ? " btn-loading" : ""}`} disabled={busy !== null} onClick={() => setStatus("open")}>Open to students</button>}
            {selected.status === "Open" && <button type="button" className={`btn btn-secondary${busy === "close" ? " btn-loading" : ""}`} disabled={busy !== null} onClick={() => setStatus("close")}>Close survey</button>}
            {selected.status !== "Open" && <button type="button" className={`btn btn-danger${busy === "delete" ? " btn-loading" : ""}`} disabled={busy !== null} onClick={remove}>Delete survey</button>}
          </div>
          <h3 className="course-evaluations-sub">Response rate by course {selected.status === "Open" && <small className="text-muted">(updates every 5 seconds)</small>}</h3>
          {selected.courses.length === 0 ? <Empty title="No course has enrolled students." /> : (
            <div className="ledger-wrap">
              <table className="ledger" aria-label="Response rate by course">
                <thead><tr><th scope="col">Course</th><th scope="col">Instructor</th><th scope="col">CLOs asked</th><th scope="col" className="num">Answered</th><th scope="col">Response rate</th></tr></thead>
                <tbody>
                  {selected.courses.map(c => (
                    <tr key={c.courseId}>
                      <td><span className="mono">{c.code}</span> {c.title}</td>
                      <td>{c.instructor ?? <span className="text-muted">—</span>}</td>
                      <td className="mono">{selected.askCourseClos ? (c.clos.length ? c.clos.join(", ") : <span className="text-muted">None defined</span>) : "—"}</td>
                      <td className="num">{c.completed} of {c.enrolled}</td>
                      <td style={{ minWidth: 200 }}><ProgressRule value={c.completed} max={Math.max(1, c.enrolled)} label={`${Math.round(c.completed * 100 / Math.max(1, c.enrolled))}%`} /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <details className="form-integrations-import">
            <summary>The {selected.questions.length} fixed questions</summary>
            <ol className="course-evaluations-list">{selected.questions.map(q => <li key={q.id}><strong>{q.section}:</strong> {q.text} <small className="text-muted">{q.kind === "Comment" ? "(comment)" : q.programOutcomeCode ? `(${q.programOutcomeCode})` : ""}</small></li>)}</ol>
          </details>
        </Panel>
      )}

      <Panel title="Surveys">
        {list.length === 0 ? <Empty title="No survey yet." action={<button type="button" className="btn btn-primary" onClick={() => edit(null)}>New survey</button>}>Start with the question list of your current evaluation form.</Empty> : (
          <div className="ledger-wrap">
            <table className="ledger" aria-label="Surveys">
              <thead><tr><th scope="col">Survey</th><th scope="col">Semester</th><th scope="col">State</th><th scope="col" className="num">Questions</th><th scope="col" className="num">Answered</th><th scope="col"><span className="sr-only">Open</span></th></tr></thead>
              <tbody>
                {list.map(s => (
                  <tr key={s.id} data-status={STATUS_TONE[s.status]}>
                    <td>{s.title}</td><td>{s.semester}</td><td><Mark tone={STATUS_TONE[s.status] ?? "ink"}>{s.status}</Mark></td>
                    <td className="num">{s.questions}{s.askCourseClos ? " + CLOs" : ""}</td>
                    <td className="num">{s.completed} of {s.eligible}</td>
                    <td><button type="button" className="btn btn-secondary btn-sm" onClick={() => open(s.id)}>View</button></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Panel>
    </div>
  );
}
