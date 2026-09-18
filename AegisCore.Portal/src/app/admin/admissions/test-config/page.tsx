"use client";

import { useEffect, useState, type FormEvent } from "react";
import Link from "next/link";
import { apiGet, apiPost, apiPut, apiPostForm, release } from "@/lib/api-client";
import { Panel, Mark, Notice } from "@/components/register";
import { Ledger } from "@/components/register/ledger";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Subject = { id: string; name: string; code: string; isActive: boolean };
type Slot = { subjectId: string; questionCount: number; marksPerQuestion: number };
type Profile = { id: string; intakeId: string; programId: string; passMark: number; timeLimitMinutes: number; subjects: Slot[] };

/**
 * What an admission test consists of: the subjects that exist, how many questions each programme's
 * paper draws from them, and where those questions come from in the first place.
 */
export default function TestConfigPage() {
  const { state, reload } = useModuleData<Subject[]>(signal => apiGet<Subject[]>("/api/admissions/subjects?includeInactive=true", signal));
  const action = useAction();
  const [newSubject, setNewSubject] = useState({ name: "", code: "" });

  const [intakeId, setIntakeId] = useState("");
  const [programId, setProgramId] = useState("");

  const [syllabusSubject, setSyllabusSubject] = useState("");
  const [syllabusCount, setSyllabusCount] = useState(20);
  const [prompt, setPrompt] = useState("");

  useEffect(() => { apiGet<{ systemPrompt: string }>("/api/admissions/question-prompt").then(r => setPrompt(r.systemPrompt)).catch(() => {}); }, []);

  // The paper belongs to one intake and programme, so it is read once both are named. A programme
  // with no paper yet answers 404, which is an answer, not a failure.
  const paper = useModuleData<Profile | null>(
    signal => intakeId && programId
      ? apiGet<Profile>(`/api/admissions/test-config?intakeId=${intakeId}&programId=${programId}`, signal).catch(() => null)
      : Promise.resolve(null),
    `${intakeId}|${programId}`
  );

  async function addSubject(e: FormEvent) {
    e.preventDefault();
    await action.run("subject", async () => {
      await apiPost("/api/admissions/subjects", newSubject);
      const name = newSubject.name;
      setNewSubject({ name: "", code: "" });
      return `${name} can now be given questions and put on a paper.`;
    }, reload);
  }

  async function toggle(subject: Subject) {
    await action.run(subject.id, async () => {
      await release(await fetch(`/api/admissions/subjects/${subject.id}/active`, {
        method: "PATCH", credentials: "include", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ isActive: !subject.isActive })
      }));
      return subject.isActive
        ? `${subject.name} is set aside: it cannot go on a new paper, and the questions already written for it are kept.`
        : `${subject.name} is in use again.`;
    }, reload);
  }

  async function ingest(file: File) {
    await action.run("syllabus", async () => {
      const form = new FormData();
      form.append("file", file);
      form.append("questions", String(syllabusCount));
      const r = await apiPostForm<{ questionsGenerated: number; ocrMethod: string }>(`/api/admissions/syllabus/${syllabusSubject}`, form);
      return `${r.questionsGenerated} draft questions were written from that syllabus (its text was read by ${r.ocrMethod}). None can be put to a candidate until someone approves it in the question bank.`;
    });
  }

  async function savePrompt(e: FormEvent) {
    e.preventDefault();
    await action.run("prompt", async () => {
      await apiPut("/api/admissions/question-prompt", { systemPrompt: prompt });
      return "Saved. The next syllabus you upload will be read with these instructions.";
    });
  }

  return (
    <ModulePage
      title="Test configuration"
      crumbs={[{ label: "Admissions", href: "/admin/admissions" }, { label: "Test configuration" }]}
      meta={<span>Subjects, the shape of each programme&apos;s paper, and where its questions come from. <Link href="/admin/admissions/question-bank">Question bank</Link></span>}
      ident={(subjects: Subject[]) => <><strong>{subjects.filter(s => s.isActive).length}</strong><br />subjects in use</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {subjects => (
        <div className="record">
          <div className="record-body">
            <Panel title="Subjects" flush>
              <Ledger rows={subjects} keyOf={s => s.id} minWidth={560} status={s => s.isActive ? undefined : "brass"}
                empty={{ title: "No subject yet", detail: "A subject holds questions, and a paper is made of subjects." }}
                columns={[
                  { key: "name", label: "Subject", render: s => s.name },
                  { key: "code", label: "Code", render: s => s.code, mono: true, nowrap: true },
                  { key: "active", label: "In use", render: s => s.isActive ? <Mark tone="laurel">In use</Mark> : <Mark tone="brass">Set aside</Mark> },
                  {
                    key: "act", label: "", nowrap: true, render: s => (
                      <button type="button" className="btn btn-ghost btn-sm" disabled={action.busy !== null} onClick={() => toggle(s)}>
                        {s.isActive ? "Set aside" : "Use again"}
                      </button>
                    ),
                  },
                ]} />
            </Panel>

            <PaperForm
              key={`${intakeId}|${programId}`}
              intakeId={intakeId} programId={programId}
              onIntake={setIntakeId} onProgramme={setProgramId}
              subjects={subjects}
              paper={paper.state.kind === "ready" ? paper.state.data : null}
              reading={paper.state.kind === "loading"}
              action={action}
            />
          </div>

          <aside className="record-margin">
            <Panel title="Add a subject">
              <form className="panel-form" onSubmit={addSubject}>
                <label><span>Name</span><input value={newSubject.name} onChange={e => setNewSubject({ ...newSubject, name: e.target.value })} placeholder="Mathematics" required /></label>
                <label><span>Code</span><input value={newSubject.code} onChange={e => setNewSubject({ ...newSubject, code: e.target.value.toUpperCase() })} placeholder="MATH" required /></label>
                <button type="submit" className={`btn btn-primary${action.busy === "subject" ? " btn-loading" : ""}`} disabled={action.busy !== null}>Add subject</button>
              </form>
            </Panel>

            <Panel title="Questions from a syllabus">
              <div className="panel-form">
                <label><span>Subject</span>
                  <select value={syllabusSubject} onChange={e => setSyllabusSubject(e.target.value)}>
                    <option value="">Choose a subject</option>
                    {subjects.filter(s => s.isActive).map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                  </select>
                </label>
                <label><span>How many to write</span><input type="number" min={1} max={200} value={syllabusCount} onChange={e => setSyllabusCount(+e.target.value)} /></label>
                <label className={`btn btn-secondary apply-file${action.busy === "syllabus" ? " btn-loading" : ""}`} aria-disabled={!syllabusSubject}>
                  {action.busy === "syllabus" ? "Reading the syllabus" : "Upload a syllabus (PDF)"}
                  <input hidden type="file" accept="application/pdf" disabled={!syllabusSubject || action.busy !== null}
                    onChange={e => { const file = e.target.files?.[0]; if (file) void ingest(file); }} />
                </label>
                <p className="field-hint">
                  The text is read out of the PDF and questions are drafted from it. They arrive unapproved:
                  nothing written this way reaches a candidate until a person has read it in the question bank.
                </p>
              </div>
            </Panel>

            <Panel title="How questions are asked for">
              <form className="panel-form" onSubmit={savePrompt}>
                <label><span>Instructions given to the model</span>
                  <textarea value={prompt} onChange={e => setPrompt(e.target.value)} rows={8} />
                </label>
                <p className="field-hint">Write <code>{"{N}"}</code> where the number of questions should appear.</p>
                <button type="submit" className={`btn btn-secondary${action.busy === "prompt" ? " btn-loading" : ""}`} disabled={action.busy !== null}>Save the instructions</button>
              </form>
            </Panel>

            <Notice tone="ink">
              A generated question is a draft written by a language model from your syllabus. It can be
              wrong, so every one is held for approval before a candidate ever sees it.
            </Notice>
          </aside>
        </div>
      )}
    </ModulePage>
  );
}

/**
 * One programme's paper. It is mounted with a key of the chosen intake and programme, so choosing a
 * different one remounts it with that paper's values and no effect ever writes state during render.
 */
function PaperForm({ intakeId, programId, onIntake, onProgramme, subjects, paper, reading, action }: {
  intakeId: string; programId: string;
  onIntake: (value: string) => void; onProgramme: (value: string) => void;
  subjects: Subject[]; paper: Profile | null; reading: boolean;
  action: ReturnType<typeof useAction>;
}) {
  const [passMark, setPassMark] = useState(paper?.passMark ?? 50);
  const [timeLimit, setTimeLimit] = useState(paper?.timeLimitMinutes ?? 60);
  const [slots, setSlots] = useState<Slot[]>(paper?.subjects ?? []);
  const marks = slots.reduce((n, slot) => n + slot.questionCount * slot.marksPerQuestion, 0);

  async function save(e: FormEvent) {
    e.preventDefault();
    await action.run("profile", async () => {
      await apiPut<Profile>("/api/admissions/test-config", { intakeId, programId, passMark, timeLimitMinutes: timeLimit, subjects: slots });
      return `Saved: ${slots.length} subject${slots.length === 1 ? "" : "s"}, ${marks} marks in all, pass at ${passMark}, ${timeLimit} minutes.`;
    });
  }

  return (
    <Panel
      title="The paper for one programme"
      aside={reading ? <span>reading…</span> : paper ? <span>saved: {paper.subjects.length} subject{paper.subjects.length === 1 ? "" : "s"}</span> : <span>no paper saved yet</span>}
    >
      <form className="panel-form" onSubmit={save}>
        <div className="filter-row">
          <label><span>Intake</span><input value={intakeId} onChange={e => onIntake(e.target.value)} placeholder="intake identifier" size={36} /></label>
          <label><span>Programme</span><input value={programId} onChange={e => onProgramme(e.target.value)} placeholder="programme identifier" size={36} /></label>
        </div>
        <div className="filter-row">
          <label><span>Pass mark</span><input type="number" value={passMark} onChange={e => setPassMark(+e.target.value)} /></label>
          <label><span>Minutes allowed</span><input type="number" value={timeLimit} onChange={e => setTimeLimit(+e.target.value)} /></label>
        </div>

        {slots.length === 0 ? (
          <p className="field-hint">This paper has no subjects on it yet.</p>
        ) : (
          <table className="ledger">
            <thead><tr><th scope="col">Subject</th><th scope="col" className="num">Questions</th><th scope="col" className="num">Marks each</th><th scope="col" /></tr></thead>
            <tbody>
              {slots.map((slot, i) => (
                <tr key={i}>
                  <td>
                    <select value={slot.subjectId} onChange={e => setSlots(s => s.map((x, j) => j === i ? { ...x, subjectId: e.target.value } : x))} aria-label={`Subject ${i + 1}`}>
                      <option value="">Choose a subject</option>
                      {subjects.filter(s => s.isActive).map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                    </select>
                  </td>
                  <td className="num"><input type="number" min={1} value={slot.questionCount} onChange={e => setSlots(s => s.map((x, j) => j === i ? { ...x, questionCount: +e.target.value } : x))} aria-label="Questions" /></td>
                  <td className="num"><input type="number" min={1} value={slot.marksPerQuestion} onChange={e => setSlots(s => s.map((x, j) => j === i ? { ...x, marksPerQuestion: +e.target.value } : x))} aria-label="Marks each" /></td>
                  <td className="nowrap"><button type="button" className="btn btn-ghost btn-sm" onClick={() => setSlots(s => s.filter((_, j) => j !== i))}>Remove</button></td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        <div className="btn-row">
          <button type="button" className="btn btn-secondary btn-sm" onClick={() => setSlots(s => [...s, { subjectId: "", questionCount: 20, marksPerQuestion: 1 }])}>Add a subject to the paper</button>
        </div>
        <p className="field-hint">
          {slots.length === 0
            ? "A candidate sits nothing until at least one subject is on the paper."
            : `As it stands: ${slots.reduce((n, slot) => n + slot.questionCount, 0)} questions, ${marks} marks, pass at ${passMark}.`}
        </p>
        <button type="submit" className={`btn btn-primary${action.busy === "profile" ? " btn-loading" : ""}`} disabled={action.busy !== null || !intakeId || !programId || slots.length === 0}>
          Save the paper
        </button>
      </form>
    </Panel>
  );
}
