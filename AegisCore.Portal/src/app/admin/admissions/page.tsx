"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { apiGet, apiPost } from "@/lib/api-client";

type Intake = { id: string; year: number; semester: number; name: string; status: number };
type Applicant = {
  id: string; applicantId: string; fullName: string | null; email: string;
  programId: string; intakeId: string; status: string;
  submittedAt: string | null; createdAt: string;
};

const STATUSES = ["Draft", "Submitted", "UnderReview", "TestScheduled", "InterviewScheduled", "Admitted", "Rejected", "Withdrawn"];

export default function AdmissionsAdminPage() {
  const [intakes, setIntakes] = useState<Intake[]>([]);
  const [rows, setRows] = useState<Applicant[]>([]);
  const [intakeId, setIntakeId] = useState("");
  const [status, setStatus] = useState("");
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setError(null);
    const params = new URLSearchParams();
    if (intakeId) params.set("intakeId", intakeId);
    if (status) params.set("status", status);
    try {
      setRows(await apiGet<Applicant[]>(`/api/admissions/applicants?${params}`));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load applicants.");
    }
  }, [intakeId, status]);

  useEffect(() => { apiGet<Intake[]>("/api/admissions/intakes").then(setIntakes).catch(() => {}); }, []);
  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- reload applicants when filters change.
    load();
  }, [load]);

  const toggle = (id: string) => setSelected(s => {
    const n = new Set(s);
    if (n.has(id)) n.delete(id); else n.add(id);
    return n;
  });

  const bulk = async (target: string) => {
    if (selected.size === 0) return;
    setBusy(true);
    try {
      await apiPost("/api/admissions/applicants/bulk-status", { applicantIds: [...selected], status: target });
      setSelected(new Set());
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Bulk action failed.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="content-page">
      <div className="page-header">
        <div><h1>Admissions</h1><p>{rows.length} applicants</p></div>
        <Link className="btn btn-secondary" href="/admin/admissions/document-config">Configure intakes &amp; documents</Link>
      </div>

      {error && <div className="error-state">{error}</div>}

      <div className="card">
        <div className="card-body" style={{ display: "flex", gap: 12, flexWrap: "wrap", alignItems: "end" }}>
          <label>Intake
            <select value={intakeId} onChange={e => setIntakeId(e.target.value)}>
              <option value="">All intakes</option>
              {intakes.map(i => <option key={i.id} value={i.id}>{i.name}</option>)}
            </select>
          </label>
          <label>Status
            <select value={status} onChange={e => setStatus(e.target.value)}>
              <option value="">All statuses</option>
              {STATUSES.map(s => <option key={s} value={s}>{s}</option>)}
            </select>
          </label>
          {selected.size > 0 && (
            <div style={{ marginLeft: "auto", display: "flex", gap: 8 }}>
              <span style={{ alignSelf: "center", fontSize: 13 }}>{selected.size} selected</span>
              <button className="btn btn-sm btn-secondary" disabled={busy} onClick={() => bulk("UnderReview")}>Shortlist</button>
              <button className="btn btn-sm btn-secondary" disabled={busy} onClick={() => bulk("TestScheduled")}>Schedule test</button>
              <button className="btn btn-sm btn-danger" disabled={busy} onClick={() => bulk("Rejected")}>Reject</button>
            </div>
          )}
        </div>
      </div>

      <table style={{ width: "100%", marginTop: 16, borderCollapse: "collapse" }}>
        <thead>
          <tr style={{ textAlign: "left", borderBottom: "1px solid var(--border,#e2e4e9)" }}>
            <th style={{ width: 32 }}></th>
            <th>APP ID</th><th>Name</th><th>Email</th><th>Status</th><th>Submitted</th>
          </tr>
        </thead>
        <tbody>
          {rows.map(r => (
            <tr key={r.id} style={{ borderBottom: "1px solid var(--border,#eee)" }}>
              <td><input type="checkbox" style={{ minHeight: "auto" }} checked={selected.has(r.id)} onChange={() => toggle(r.id)} /></td>
              <td><Link href={`/admin/admissions/${r.id}`}>{r.applicantId}</Link></td>
              <td>{r.fullName ?? "—"}</td>
              <td>{r.email}</td>
              <td><span className="badge badge-neutral">{r.status}</span></td>
              <td>{r.submittedAt ? new Date(r.submittedAt).toLocaleDateString() : "—"}</td>
            </tr>
          ))}
          {rows.length === 0 && <tr><td colSpan={6} style={{ padding: 24, textAlign: "center", color: "var(--muted,#667)" }}>No applicants match these filters.</td></tr>}
        </tbody>
      </table>
    </div>
  );
}
