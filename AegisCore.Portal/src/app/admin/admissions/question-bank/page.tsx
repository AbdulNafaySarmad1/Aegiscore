"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { apiGet, apiPost } from "@/lib/api-client";

type Subject = { id: string; name: string };
type Question = {
  id: string; subjectId: string; questionText: string;
  optionA: string; optionB: string; optionC: string; optionD: string;
  correctOption: string; difficultyLevel: string; status: string;
};

export default function QuestionBankPage() {
  const [subjects, setSubjects] = useState<Subject[]>([]);
  const [subjectId, setSubjectId] = useState("");
  const [status, setStatus] = useState("Draft");
  const [rows, setRows] = useState<Question[]>([]);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    const params = new URLSearchParams();
    if (subjectId) params.set("subjectId", subjectId);
    if (status) params.set("status", status);
    try { setRows(await apiGet<Question[]>(`/api/admissions/question-bank?${params}`)); }
    catch (e) { setError(e instanceof Error ? e.message : "Failed to load questions."); }
  }, [subjectId, status]);

  useEffect(() => { apiGet<Subject[]>("/api/admissions/subjects").then(setSubjects).catch(() => {}); }, []);
  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- reload questions when filters change.
    load();
  }, [load]);

  const review = (id: string, approve: boolean) => (async () => {
    setError(null);
    try { await apiPost(`/api/admissions/question-bank/${id}/review`, { approve }); await load(); }
    catch (e) { setError(e instanceof Error ? e.message : "Review failed."); }
  })();

  return (
    <div className="content-page">
      <div className="page-header">
        <div><Link href="/admin/admissions/test-config">← Test configuration</Link><h1>Question bank</h1></div>
      </div>
      {error && <div className="error-state">{error}</div>}

      <div className="card">
        <div className="card-body" style={{ display: "flex", gap: 12, alignItems: "end" }}>
          <label>Subject
            <select value={subjectId} onChange={e => setSubjectId(e.target.value)}>
              <option value="">All</option>
              {subjects.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
            </select>
          </label>
          <label>Status
            <select value={status} onChange={e => setStatus(e.target.value)}>
              <option value="Draft">Draft</option><option value="Approved">Approved</option><option value="Rejected">Rejected</option><option value="">All</option>
            </select>
          </label>
        </div>
      </div>

      {rows.map(q => (
        <div key={q.id} className="card" style={{ marginTop: 12 }}>
          <div className="card-body">
            <div style={{ display: "flex", justifyContent: "space-between" }}>
              <strong>{q.questionText}</strong>
              <span className={`badge badge-${q.status === "Approved" ? "success" : q.status === "Rejected" ? "danger" : "warning"}`}>{q.status}</span>
            </div>
            <ol type="A" style={{ margin: "8px 0" }}>
              {(["A", "B", "C", "D"] as const).map(k => (
                <li key={k} style={{ fontWeight: q.correctOption === k ? 700 : 400 }}>
                  {q[`option${k}` as "optionA"]} {q.correctOption === k && "✓"}
                </li>
              ))}
            </ol>
            <div style={{ fontSize: 12, color: "var(--muted,#667)" }}>Difficulty: {q.difficultyLevel}</div>
            {q.status === "Draft" && (
              <div style={{ display: "flex", gap: 8, marginTop: 8 }}>
                <button className="btn btn-sm btn-success" onClick={() => review(q.id, true)}>Approve</button>
                <button className="btn btn-sm btn-danger" onClick={() => review(q.id, false)}>Reject</button>
              </div>
            )}
          </div>
        </div>
      ))}
      {rows.length === 0 && <p style={{ color: "var(--muted,#667)", marginTop: 16 }}>No questions match.</p>}
    </div>
  );
}
