"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { apiGet, apiPost } from "@/lib/api-client";

type Row = {
  applicantTestId: string; applicantId: string; applicantName: string;
  startedAt: string | null; expiresAt: string; status: string; proctorNote: string | null;
};

export default function ProctorPage() {
  const [rows, setRows] = useState<Row[]>([]);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try { setRows(await apiGet<Row[]>("/api/admissions/proctor")); }
    catch (e) { setError(e instanceof Error ? e.message : "Failed to load."); }
  }, []);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- poll the live proctor list.
    load();
    const id = window.setInterval(load, 10000);
    return () => window.clearInterval(id);
  }, [load]);

  const act = (path: string, body?: unknown) => (async () => {
    setError(null);
    try { await apiPost(path, body ?? {}); await load(); }
    catch (e) { setError(e instanceof Error ? e.message : "Action failed."); }
  })();

  return (
    <div className="content-page">
      <div className="page-header">
        <div><Link href="/admin/admissions">← Applicants</Link><h1>Proctor — live tests</h1><p>{rows.length} in progress</p></div>
      </div>
      {error && <div className="error-state">{error}</div>}
      <p style={{ color: "var(--muted,#667)" }}>Questions and answers are never shown here.</p>

      <table style={{ width: "100%", borderCollapse: "collapse", marginTop: 12 }}>
        <thead><tr style={{ textAlign: "left", borderBottom: "1px solid var(--border,#e2e4e9)" }}>
          <th>APP ID</th><th>Name</th><th>Started</th><th>Deadline</th><th>Flag</th><th></th>
        </tr></thead>
        <tbody>
          {rows.map(r => (
            <tr key={r.applicantTestId} style={{ borderBottom: "1px solid var(--border,#eee)" }}>
              <td>{r.applicantId}</td>
              <td>{r.applicantName}</td>
              <td>{r.startedAt ? new Date(r.startedAt).toLocaleTimeString() : "—"}</td>
              <td>{new Date(r.expiresAt).toLocaleTimeString()}</td>
              <td>{r.proctorNote ?? "—"}</td>
              <td style={{ display: "flex", gap: 6 }}>
                <button className="btn btn-sm btn-ghost" onClick={() => {
                  const note = window.prompt("Flag note");
                  if (note) act(`/api/admissions/proctor/${r.applicantTestId}/flag`, { note });
                }}>Flag</button>
                <button className="btn btn-sm btn-danger" onClick={() => act(`/api/admissions/proctor/${r.applicantTestId}/force-submit`)}>Force submit</button>
              </td>
            </tr>
          ))}
          {rows.length === 0 && <tr><td colSpan={6} style={{ padding: 24, textAlign: "center", color: "var(--muted,#667)" }}>No tests are currently in progress.</td></tr>}
        </tbody>
      </table>
    </div>
  );
}
