"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { apiGet, apiPost } from "@/lib/api-client";

type Card = { applicantProfileId: string; applicantId: string; name: string | null; programId: string; lastUpdated: string; column: string };
type Board = { columns: string[]; cards: Card[] };

const CONFIRM_COLUMNS = new Set(["Admitted", "Enrolled", "Rejected"]);

export default function PipelinePage() {
  const [board, setBoard] = useState<Board | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [dragId, setDragId] = useState<string | null>(null);

  const load = useCallback(async () => {
    try { setBoard(await apiGet<Board>("/api/admissions/pipeline")); }
    catch (e) { setError(e instanceof Error ? e.message : "Failed to load the board."); }
  }, []);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- initial board hydration.
    load();
  }, [load]);

  const move = async (ids: string[], column: string) => {
    if (ids.length === 0) return;
    if (CONFIRM_COLUMNS.has(column) && !window.confirm(`Move ${ids.length} applicant(s) to "${column}"? This step is significant.`)) return;

    let manualInstitutionalEmail: string | null = null;
    if ((column === "Admitted" || column === "Enrolled") && ids.length === 1) {
      manualInstitutionalEmail = window.prompt("Institutional email (leave blank if provisioning is automated)") || null;
    }

    setError(null);
    try {
      if (ids.length === 1) {
        await apiPost("/api/admissions/pipeline/move", { applicantId: ids[0], column, manualInstitutionalEmail });
      } else {
        await apiPost("/api/admissions/pipeline/bulk-move", { applicantIds: ids, column });
      }
      setSelected(new Set());
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Move failed.");
    }
  };

  if (!board) return <div className="content-page">{error ? <div className="error-state">{error}</div> : <div className="skeleton" style={{ height: 200 }} />}</div>;

  const toggle = (id: string) => setSelected(s => { const n = new Set(s); if (n.has(id)) n.delete(id); else n.add(id); return n; });

  return (
    <div className="content-page">
      <div className="page-header">
        <div><Link href="/admin/admissions">← Applicants</Link><h1>Admissions pipeline</h1></div>
        {selected.size > 0 && (
          <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
            <span style={{ fontSize: 13 }}>{selected.size} selected — move to:</span>
            {board.columns.filter(c => c !== "Test Completed").map(c => (
              <button key={c} className="btn btn-sm btn-secondary" onClick={() => move([...selected], c)}>{c}</button>
            ))}
          </div>
        )}
      </div>
      {error && <div className="error-state">{error}</div>}

      <div style={{ display: "flex", gap: 12, overflowX: "auto", paddingBottom: 12 }}>
        {board.columns.map(col => {
          const cards = board.cards.filter(c => c.column === col);
          return (
            <div key={col}
              onDragOver={e => e.preventDefault()}
              onDrop={() => { if (dragId) { move([dragId], col); setDragId(null); } }}
              style={{ minWidth: 220, flex: "0 0 220px", background: "var(--surface-2,#f2f3f6)", borderRadius: 8, padding: 8 }}>
              <div style={{ fontWeight: 600, fontSize: 13, padding: "4px 6px" }}>{col} <span style={{ color: "var(--muted,#889)" }}>({cards.length})</span></div>
              {cards.map(c => (
                <div key={c.applicantProfileId}
                  draggable
                  onDragStart={() => setDragId(c.applicantProfileId)}
                  style={{ background: "var(--surface,#fff)", border: "1px solid var(--border,#e2e4e9)", borderRadius: 6, padding: 8, margin: "6px 0", cursor: "grab" }}>
                  <label style={{ display: "flex", gap: 6, alignItems: "center", fontSize: 12 }}>
                    <input type="checkbox" style={{ minHeight: "auto" }} checked={selected.has(c.applicantProfileId)} onChange={() => toggle(c.applicantProfileId)} />
                    <strong>{c.applicantId}</strong>
                  </label>
                  <div style={{ fontSize: 13 }}>{c.name ?? "—"}</div>
                  <div style={{ fontSize: 11, color: "var(--muted,#889)" }}>{new Date(c.lastUpdated).toLocaleDateString()}</div>
                </div>
              ))}
            </div>
          );
        })}
      </div>
    </div>
  );
}
