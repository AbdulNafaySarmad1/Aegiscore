"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { apiGet, apiPost, apiPut, apiPostForm, release } from "@/lib/api-client";

type Subject = { id: string; name: string; code: string; isActive: boolean };
type Slot = { subjectId: string; questionCount: number; marksPerQuestion: number };
type Profile = { id: string; intakeId: string; programId: string; passMark: number; timeLimitMinutes: number; subjects: Slot[] };

export default function TestConfigPage() {
  const [subjects, setSubjects] = useState<Subject[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [newSubject, setNewSubject] = useState({ name: "", code: "" });

  const [intakeId, setIntakeId] = useState("");
  const [programId, setProgramId] = useState("");
  const [profile, setProfile] = useState<Profile | null>(null);
  const [passMark, setPassMark] = useState(50);
  const [timeLimit, setTimeLimit] = useState(60);
  const [slots, setSlots] = useState<Slot[]>([]);

  const [syllabusSubject, setSyllabusSubject] = useState("");
  const [syllabusCount, setSyllabusCount] = useState(20);
  const [ingestMsg, setIngestMsg] = useState<string | null>(null);

  const [prompt, setPrompt] = useState("");

  const guard = async (fn: () => Promise<unknown>) => {
    setError(null);
    try { await fn(); } catch (e) { setError(e instanceof Error ? e.message : "Action failed."); }
  };

  const loadSubjects = useCallback(async () => {
    setSubjects(await apiGet<Subject[]>("/api/admissions/subjects?includeInactive=true"));
  }, []);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- initial hydration of subjects and prompt.
    loadSubjects().catch(() => setError("Failed to load subjects."));
    apiGet<{ systemPrompt: string }>("/api/admissions/question-prompt").then(r => setPrompt(r.systemPrompt)).catch(() => {});
  }, [loadSubjects]);

  const loadProfile = useCallback(async () => {
    if (!intakeId || !programId) return;
    try {
      const p = await apiGet<Profile>(`/api/admissions/test-config?intakeId=${intakeId}&programId=${programId}`);
      setProfile(p); setPassMark(p.passMark); setTimeLimit(p.timeLimitMinutes); setSlots(p.subjects);
    } catch {
      setProfile(null); setSlots([]);
    }
  }, [intakeId, programId]);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- reload the test profile when the selection changes.
    loadProfile();
  }, [loadProfile]);

  return (
    <div className="content-page">
      <div className="page-header">
        <div><Link href="/admin/admissions">← Applicants</Link><h1>Test configuration</h1></div>
        <Link className="btn btn-secondary" href="/admin/admissions/question-bank">Question bank</Link>
      </div>
      {error && <div className="error-state">{error}</div>}

      <div className="card">
        <div className="card-header"><h3>Master subject list</h3></div>
        <div className="card-body">
          <div style={{ display: "flex", gap: 8, alignItems: "end", marginBottom: 12 }}>
            <label>Name<input value={newSubject.name} onChange={e => setNewSubject({ ...newSubject, name: e.target.value })} /></label>
            <label>Code<input value={newSubject.code} onChange={e => setNewSubject({ ...newSubject, code: e.target.value.toUpperCase() })} style={{ width: 100 }} /></label>
            <button className="btn btn-primary" onClick={() => guard(async () => { await apiPost("/api/admissions/subjects", newSubject); setNewSubject({ name: "", code: "" }); await loadSubjects(); })}>Add subject</button>
          </div>
          <table style={{ width: "100%", borderCollapse: "collapse" }}>
            <thead><tr style={{ textAlign: "left" }}><th>Name</th><th>Code</th><th>Active</th><th></th></tr></thead>
            <tbody>
              {subjects.map(s => (
                <tr key={s.id} style={{ borderTop: "1px solid var(--border,#eee)" }}>
                  <td>{s.name}</td><td>{s.code}</td><td>{s.isActive ? "Yes" : "No"}</td>
                  <td><button className="btn btn-sm btn-ghost" onClick={() => guard(async () => {
                    await release(await fetch(`/api/admissions/subjects/${s.id}/active`, { method: "PATCH", credentials: "include", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ isActive: !s.isActive }) }));
                    await loadSubjects();
                  })}>{s.isActive ? "Deactivate" : "Activate"}</button></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="card" style={{ marginTop: 16 }}>
        <div className="card-header"><h3>Per-program test profile</h3></div>
        <div className="card-body">
          <div style={{ display: "flex", gap: 8, alignItems: "end", flexWrap: "wrap" }}>
            <label>Intake ID<input value={intakeId} onChange={e => setIntakeId(e.target.value)} style={{ width: 300 }} /></label>
            <label>Program ID<input value={programId} onChange={e => setProgramId(e.target.value)} style={{ width: 300 }} /></label>
            <label>Pass mark<input type="number" value={passMark} onChange={e => setPassMark(+e.target.value)} style={{ width: 90 }} /></label>
            <label>Time limit (min)<input type="number" value={timeLimit} onChange={e => setTimeLimit(+e.target.value)} style={{ width: 90 }} /></label>
          </div>

          <table style={{ width: "100%", marginTop: 12, borderCollapse: "collapse" }}>
            <thead><tr style={{ textAlign: "left" }}><th>Subject</th><th>Questions</th><th>Marks each</th><th></th></tr></thead>
            <tbody>
              {slots.map((slot, i) => (
                <tr key={i} style={{ borderTop: "1px solid var(--border,#eee)" }}>
                  <td>
                    <select value={slot.subjectId} onChange={e => setSlots(s => s.map((x, j) => j === i ? { ...x, subjectId: e.target.value } : x))}>
                      <option value="">Select subject…</option>
                      {subjects.filter(s => s.isActive).map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                    </select>
                  </td>
                  <td><input type="number" value={slot.questionCount} onChange={e => setSlots(s => s.map((x, j) => j === i ? { ...x, questionCount: +e.target.value } : x))} style={{ width: 80 }} /></td>
                  <td><input type="number" value={slot.marksPerQuestion} onChange={e => setSlots(s => s.map((x, j) => j === i ? { ...x, marksPerQuestion: +e.target.value } : x))} style={{ width: 80 }} /></td>
                  <td><button className="btn btn-sm btn-ghost" onClick={() => setSlots(s => s.filter((_, j) => j !== i))}>Remove</button></td>
                </tr>
              ))}
            </tbody>
          </table>
          <button className="btn btn-sm btn-secondary" onClick={() => setSlots(s => [...s, { subjectId: "", questionCount: 20, marksPerQuestion: 1 }])}>Add subject slot</button>

          <div style={{ marginTop: 12 }}>
            <button className="btn btn-primary" disabled={!intakeId || !programId || slots.length === 0}
              onClick={() => guard(async () => {
                await apiPut("/api/admissions/test-config", { intakeId, programId, passMark, timeLimitMinutes: timeLimit, subjects: slots });
                await loadProfile();
              })}>Save test profile</button>
            {profile && <span style={{ marginLeft: 12, color: "var(--muted,#667)" }}>Saved profile has {profile.subjects.length} subject(s).</span>}
          </div>
        </div>
      </div>

      <div className="card" style={{ marginTop: 16 }}>
        <div className="card-header"><h3>Syllabus ingestion → question bank</h3></div>
        <div className="card-body" style={{ display: "flex", gap: 8, alignItems: "end", flexWrap: "wrap" }}>
          <label>Subject
            <select value={syllabusSubject} onChange={e => setSyllabusSubject(e.target.value)}>
              <option value="">Select…</option>
              {subjects.filter(s => s.isActive).map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
            </select>
          </label>
          <label>Questions to generate<input type="number" min={1} max={200} value={syllabusCount} onChange={e => setSyllabusCount(+e.target.value)} style={{ width: 90 }} /></label>
          <label className="btn btn-primary" style={{ cursor: syllabusSubject ? "pointer" : "not-allowed" }}>
            Upload syllabus PDF
            <input hidden type="file" accept="application/pdf" disabled={!syllabusSubject}
              onChange={e => {
                const file = e.target.files?.[0];
                if (!file) return;
                setIngestMsg("Extracting text and generating questions…");
                const form = new FormData();
                form.append("file", file);
                form.append("questions", String(syllabusCount));
                guard(async () => {
                  const r = await apiPostForm<{ questionsGenerated: number; ocrMethod: string }>(`/api/admissions/syllabus/${syllabusSubject}`, form);
                  setIngestMsg(`Generated ${r.questionsGenerated} draft questions (OCR: ${r.ocrMethod}). Review them in the question bank.`);
                });
              }} />
          </label>
          {ingestMsg && <span style={{ color: "var(--muted,#667)" }}>{ingestMsg}</span>}
        </div>
      </div>

      <div className="card" style={{ marginTop: 16 }}>
        <div className="card-header"><h3>Question generation prompt</h3></div>
        <div className="card-body">
          <p style={{ color: "var(--muted,#667)", marginTop: 0 }}>Editable LLM system prompt. Use <code>{"{N}"}</code> where the question count should go.</p>
          <textarea value={prompt} onChange={e => setPrompt(e.target.value)} rows={6} style={{ width: "100%" }} />
          <button className="btn btn-primary" style={{ marginTop: 8 }}
            onClick={() => guard(async () => { await apiPut("/api/admissions/question-prompt", { systemPrompt: prompt }); })}>Save prompt</button>
        </div>
      </div>
    </div>
  );
}
