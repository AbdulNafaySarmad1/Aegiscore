"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { apiGet, apiPost } from "@/lib/api-client";

type Intake = { id: string; year: number; semester: number; name: string; status: number };
type Requirement = {
  id: string; intakeId: string; programId: string; documentType: string;
  isRequired: boolean; acceptedFormat: string; maxFileSizeMb: number;
};

const SEMESTERS = [{ v: 1, l: "Fall (F)" }, { v: 2, l: "Spring (S)" }, { v: 3, l: "Winter (W)" }];
const INTAKE_STATUS = ["", "Draft", "Open", "Closed"];
const DOC_TYPES = ["Matric", "Intermediate", "OLevels", "ALevels", "IBCC_Equivalency", "NationalID", "DomicileCertificate", "Photo", "Other"];

async function patch(path: string, body: unknown) {
  const res = await fetch(path, { method: "PATCH", credentials: "include", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
  if (!res.ok) throw new Error((await res.json().catch(() => ({}))).error ?? `Failed (${res.status})`);
}
async function del(path: string) {
  const res = await fetch(path, { method: "DELETE", credentials: "include" });
  if (!res.ok) throw new Error(`Failed (${res.status})`);
}

export default function DocumentConfigPage() {
  const [intakes, setIntakes] = useState<Intake[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [newIntake, setNewIntake] = useState({ year: new Date().getFullYear(), semester: 1, name: "" });

  const [intakeId, setIntakeId] = useState("");
  const [programId, setProgramId] = useState("");
  const [programCode, setProgramCode] = useState("");
  const [reqs, setReqs] = useState<Requirement[]>([]);
  const [draft, setDraft] = useState({ documentType: "Matric", isRequired: true, acceptedFormat: "Pdf", maxFileSizeMb: 5 });

  const loadIntakes = useCallback(async () => {
    try { setIntakes(await apiGet<Intake[]>("/api/admissions/intakes")); }
    catch (e) { setError(e instanceof Error ? e.message : "Failed to load intakes."); }
  }, []);
  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- initial intake list hydration.
    loadIntakes();
  }, [loadIntakes]);

  const loadReqs = useCallback(async () => {
    if (!intakeId || !programId) { setReqs([]); return; }
    try { setReqs(await apiGet<Requirement[]>(`/api/admissions/config/documents?intakeId=${intakeId}&programId=${programId}`)); }
    catch (e) { setError(e instanceof Error ? e.message : "Failed to load requirements."); }
  }, [intakeId, programId]);
  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- reload requirements when the intake/program selection changes.
    loadReqs();
  }, [loadReqs]);

  const guard = async (fn: () => Promise<unknown>) => {
    setError(null);
    try { await fn(); } catch (e) { setError(e instanceof Error ? e.message : "Action failed."); }
  };

  return (
    <div className="content-page">
      <div className="page-header">
        <div><Link href="/admin/admissions">← Applicants</Link><h1>Intakes &amp; document requirements</h1></div>
      </div>
      {error && <div className="error-state">{error}</div>}

      <div className="card">
        <div className="card-header"><h3>Intakes</h3></div>
        <div className="card-body">
          <div style={{ display: "flex", gap: 8, flexWrap: "wrap", alignItems: "end", marginBottom: 12 }}>
            <label>Year<input type="number" value={newIntake.year} onChange={e => setNewIntake({ ...newIntake, year: +e.target.value })} style={{ width: 100 }} /></label>
            <label>Semester
              <select value={newIntake.semester} onChange={e => setNewIntake({ ...newIntake, semester: +e.target.value })}>
                {SEMESTERS.map(s => <option key={s.v} value={s.v}>{s.l}</option>)}
              </select>
            </label>
            <label>Name<input value={newIntake.name} onChange={e => setNewIntake({ ...newIntake, name: e.target.value })} placeholder="Fall 2025 Intake" /></label>
            <button className="btn btn-primary" onClick={() => guard(async () => { await apiPost("/api/admissions/intakes", newIntake); setNewIntake({ ...newIntake, name: "" }); await loadIntakes(); })}>Add intake</button>
          </div>
          <table style={{ width: "100%", borderCollapse: "collapse" }}>
            <thead><tr style={{ textAlign: "left" }}><th>Name</th><th>Year</th><th>Sem</th><th>Status</th><th></th></tr></thead>
            <tbody>
              {intakes.map(i => (
                <tr key={i.id} style={{ borderTop: "1px solid var(--border,#eee)" }}>
                  <td>{i.name}</td><td>{i.year}</td><td>{SEMESTERS.find(s => s.v === i.semester)?.l}</td>
                  <td>{INTAKE_STATUS[i.status]}</td>
                  <td style={{ display: "flex", gap: 6 }}>
                    <button className="btn btn-sm btn-success" onClick={() => guard(async () => { await patch(`/api/admissions/intakes/${i.id}/status`, { status: "Open" }); await loadIntakes(); })}>Open</button>
                    <button className="btn btn-sm btn-ghost" onClick={() => guard(async () => { await patch(`/api/admissions/intakes/${i.id}/status`, { status: "Closed" }); await loadIntakes(); })}>Close</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="card" style={{ marginTop: 16 }}>
        <div className="card-header"><h3>Program document requirements</h3></div>
        <div className="card-body">
          <div style={{ display: "flex", gap: 8, flexWrap: "wrap", alignItems: "end" }}>
            <label>Intake
              <select value={intakeId} onChange={e => setIntakeId(e.target.value)}>
                <option value="">Select…</option>
                {intakes.map(i => <option key={i.id} value={i.id}>{i.name}</option>)}
              </select>
            </label>
            <label>Program ID<input value={programId} onChange={e => setProgramId(e.target.value)} placeholder="GUID of the degree program" style={{ width: 320 }} /></label>
            <label>Program code<input value={programCode} onChange={e => setProgramCode(e.target.value.toUpperCase())} placeholder="BCS" style={{ width: 100 }} /></label>
            <button className="btn btn-secondary" disabled={!programId || !programCode}
              onClick={() => guard(async () => { await fetch(`/api/admissions/config/programs/${programId}/code`, { method: "PUT", credentials: "include", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ code: programCode }) }); })}>
              Save code
            </button>
          </div>

          {intakeId && programId && (
            <>
              <table style={{ width: "100%", marginTop: 16, borderCollapse: "collapse" }}>
                <thead><tr style={{ textAlign: "left" }}><th>Document</th><th>Required</th><th>Format</th><th>Max MB</th><th></th></tr></thead>
                <tbody>
                  {reqs.map(r => (
                    <tr key={r.id} style={{ borderTop: "1px solid var(--border,#eee)" }}>
                      <td>{r.documentType.replace(/_/g, " ")}</td>
                      <td>{r.isRequired ? "Yes" : "Optional"}</td>
                      <td>{r.acceptedFormat}</td>
                      <td>{r.maxFileSizeMb}</td>
                      <td><button className="btn btn-sm btn-danger" onClick={() => guard(async () => { await del(`/api/admissions/config/documents/${r.id}`); await loadReqs(); })}>Remove</button></td>
                    </tr>
                  ))}
                  {reqs.length === 0 && <tr><td colSpan={5} style={{ padding: 16, color: "var(--muted,#667)" }}>No requirements yet for this program.</td></tr>}
                </tbody>
              </table>

              <div style={{ display: "flex", gap: 8, flexWrap: "wrap", alignItems: "end", marginTop: 12 }}>
                <label>Document
                  <select value={draft.documentType} onChange={e => setDraft({ ...draft, documentType: e.target.value })}>
                    {DOC_TYPES.map(d => <option key={d} value={d}>{d}</option>)}
                  </select>
                </label>
                <label>Format
                  <select value={draft.acceptedFormat} onChange={e => setDraft({ ...draft, acceptedFormat: e.target.value })}>
                    <option value="Pdf">PDF</option><option value="Image">JPEG/PNG</option>
                  </select>
                </label>
                <label>Max MB<input type="number" min={1} max={50} value={draft.maxFileSizeMb} onChange={e => setDraft({ ...draft, maxFileSizeMb: +e.target.value })} style={{ width: 80 }} /></label>
                <label style={{ display: "flex", gap: 6, alignItems: "center" }}>
                  <input type="checkbox" style={{ minHeight: "auto" }} checked={draft.isRequired} onChange={e => setDraft({ ...draft, isRequired: e.target.checked })} /> Required
                </label>
                <button className="btn btn-primary"
                  onClick={() => guard(async () => {
                    await apiPost("/api/admissions/config/documents", { intakeId, programId, ...draft });
                    await loadReqs();
                  })}>Add / update</button>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
