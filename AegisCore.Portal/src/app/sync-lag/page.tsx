"use client";

import { useEffect, useState } from "react";
import { apiGet, apiPost, apiPut } from "@/lib/api-client";
import { usePersonalityTranslations } from "@/lib/personality-translator";
import { AlertTriangle, CheckCircle, Clock, Edit3, Lock, RotateCcw, Send, Settings, X } from "lucide-react";

type SyncEntry = { id: string; entityType: string; fieldName: string | null; originalValue: string | null; writtenValue: string | null; status: string; writtenAt: string; graceExpiresAt: string; secondsRemaining: number; isLocked: boolean; canCorrect: boolean };
type Dashboard = { totalPending: number; expiringWithin5Min: number; committedToday: number; correctedToday: number; defaultGraceSeconds: number; autoCommitEnabled: boolean; recentEntries: SyncEntry[] };
type SyncConfig = { id: string; defaultGracePeriodSeconds: number; maxGracePeriodSeconds: number; autoCommitOnGraceExpiry: boolean; requireAdminApprovalForCorrections: boolean; correctionPermission: string; notifyOwnerOnCorrection: boolean };

const cardS = { background: "var(--surface)", borderRadius: 12, border: "1px solid var(--line)", padding: "1.5rem", marginBottom: "1.25rem" };
const h3 = { color: "var(--ink)", fontSize: "1.1rem", fontWeight: 600, margin: "0 0 0.75rem 0" };
const btn = (bg: string) => ({ padding: "0.5rem 1rem", borderRadius: 8, border: "none", cursor: "pointer", fontWeight: 600, fontSize: "0.82rem", background: bg, color: "#fff", display: "inline-flex", alignItems: "center", gap: "0.35rem" });
const inp = { padding: "0.5rem 0.75rem", borderRadius: 8, border: "1px solid var(--line)", background: "var(--canvas)", color: "var(--ink)", fontSize: "0.85rem", width: "100%", boxSizing: "border-box" as const };
const badge = (s: string) => ({
  display: "inline-block", padding: "0.15rem 0.55rem", borderRadius: 20, fontSize: "0.7rem", fontWeight: 600,
  background: s === "PendingSync" ? "var(--amber-soft)" : s === "Committed" ? "var(--green-soft)" : s === "Corrected" ? "var(--blue-light)" : s === "Reverted" ? "var(--red-soft)" : "var(--gray-100)",
  color: s === "PendingSync" ? "var(--amber)" : s === "Committed" ? "var(--green)" : s === "Corrected" ? "var(--blue)" : s === "Reverted" ? "var(--red)" : "var(--muted)"
});

export default function SyncLagPage() {
  const t = usePersonalityTranslations("academic");
  const [dash, setDash] = useState<Dashboard | null>(null);
  const [config, setConfig] = useState<SyncConfig | null>(null);
  const [msg, setMsg] = useState("");
  const [corrections, setCorrections] = useState<Record<string, { value: string; reason: string }>>({});
  const [showConfig, setShowConfig] = useState(false);

  async function refresh() {
    const [d, c] = await Promise.all([
      apiGet<Dashboard>("/api/admin/sync-lag/dashboard").catch(() => null),
      apiGet<SyncConfig>("/api/admin/sync-lag/config").catch(() => null),
    ]);
    setDash(d); setConfig(c);
  }
  // eslint-disable-next-line react-hooks/set-state-in-effect -- initial dashboard hydration and polling.
  useEffect(() => { refresh(); const i = setInterval(refresh, 10000); return () => clearInterval(i); }, []);

  async function correct(id: string) {
    const c = corrections[id];
    if (!c?.value) return;
    try { await apiPost(`/api/admin/sync-lag/entries/${id}/correct?correctedValue=${c.value}&reason=${c.reason || "Manual correction"}&correctedByUserId=00000000-0000-0000-0000-000000000000`); setMsg("Corrected — will sync to central DB"); setCorrections(prev => { const n = { ...prev }; delete n[id]; return n; }); refresh(); } catch { setMsg("Correction failed"); }
  }

  async function revert(id: string) {
    try { await apiPost(`/api/admin/sync-lag/entries/${id}/revert?revertedByUserId=00000000-0000-0000-0000-000000000000`); setMsg("Reverted"); refresh(); } catch { setMsg("Revert failed"); }
  }

  async function saveConfig() {
    if (!config) return;
    try { await apiPut("/api/admin/sync-lag/config", config); setMsg("Sync-lag configuration saved."); } catch { setMsg("Sync-lag configuration could not be saved — try again."); }
  }

  if (!dash || !config) return <main style={{ maxWidth: 1100, margin: "0 auto", padding: "1.5rem" }}><p style={{ color: "var(--muted)" }}>Loading sync-lag dashboard...</p></main>;

  return (
    <main style={{ maxWidth: 1100, margin: "0 auto", padding: "1.5rem" }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: "1.5rem" }}>
        <div>
          <h1 style={{ color: "var(--ink)", fontSize: "1.5rem", fontWeight: 600, marginBottom: "0.25rem" }}>Sync-Lag Correction Window</h1>
          <p style={{ color: "var(--muted)", fontSize: "0.85rem" }}>Data written through replicas is held in a correction window before committing to the central database. Fix mistakes before they lock.</p>
        </div>
        <button onClick={() => setShowConfig(!showConfig)} style={btn("var(--blue)")}><Settings size={16} /> {showConfig ? "Hide" : "Configure"}</button>
      </div>

      {msg && <div onDoubleClick={() => setMsg("")} style={{ padding: "0.5rem 1rem", borderRadius: 8, background: "var(--blue-light)", color: "var(--blue)", marginBottom: "1rem", fontSize: "0.85rem", cursor: "pointer" }}>{msg}</div>}

      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(180px, 1fr))", gap: "0.75rem", marginBottom: "1.5rem" }}>
        {[
          ["Pending Sync", String(dash.totalPending || 0), "var(--amber)"],
          ["Expiring in 5 min", String(dash.expiringWithin5Min || 0), dash.expiringWithin5Min > 0 ? "var(--red)" : "var(--green)"],
          ["Grace Period", `${config.defaultGracePeriodSeconds}s`, "var(--blue)"],
          ["Auto-Commit", config.autoCommitOnGraceExpiry ? "ON" : "OFF", config.autoCommitOnGraceExpiry ? "var(--green)" : "var(--muted)"],
        ].map(([label, val, color]) => (
          <div key={label} style={{ background: "var(--surface)", borderRadius: 12, border: "1px solid var(--line)", padding: "1rem" }}>
            <div style={{ fontSize: "0.75rem", color: "var(--muted)", marginBottom: "0.25rem" }}>{label}</div>
            <div style={{ fontSize: "1.5rem", fontWeight: 700, color }}>{val}</div>
          </div>
        ))}
      </div>

      <div style={{ display: "flex", gap: "0.5rem", marginBottom: "1.25rem" }}>
        <button onClick={() => apiPost("/api/admin/sync-lag/commit-expired").then(refresh)} style={btn("var(--green)")}><Send size={14} /> Commit Expired</button>
      </div>

      {showConfig && (
        <div style={cardS}>
          <h3 style={h3}><Settings size={16} /> Sync-Lag Configuration</h3>
          <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(220px, 1fr))", gap: "0.75rem" }}>
            <div><div style={{ fontSize: "0.75rem", color: "var(--muted)", marginBottom: "0.2rem" }}>Grace Period (seconds)</div><input type="number" step="1" min="1" style={inp} value={config.defaultGracePeriodSeconds} onChange={e => setConfig({ ...config, defaultGracePeriodSeconds: parseInt(e.target.value) || 300 })} /></div>
            <div><div style={{ fontSize: "0.75rem", color: "var(--muted)", marginBottom: "0.2rem" }}>Max Grace Period (seconds)</div><input type="number" step="1" min="1" style={inp} value={config.maxGracePeriodSeconds} onChange={e => setConfig({ ...config, maxGracePeriodSeconds: parseInt(e.target.value) || 86400 })} /></div>
          </div>
          <div style={{ marginTop: "0.75rem", display: "flex", gap: "1rem", alignItems: "center", flexWrap: "wrap" }}>
            <label style={{ display: "flex", alignItems: "center", gap: "0.4rem", fontSize: "0.85rem", color: "var(--ink)", cursor: "pointer" }}>
              <input type="checkbox" checked={config.autoCommitOnGraceExpiry} onChange={e => setConfig({ ...config, autoCommitOnGraceExpiry: e.target.checked })} /> Auto-Commit
            </label>
            <label style={{ display: "flex", alignItems: "center", gap: "0.4rem", fontSize: "0.85rem", color: "var(--ink)", cursor: "pointer" }}>
              <input type="checkbox" checked={config.requireAdminApprovalForCorrections} onChange={e => setConfig({ ...config, requireAdminApprovalForCorrections: e.target.checked })} /> Require Admin for Corrections
            </label>
            <select style={{ ...inp, width: "auto" }} value={config.correctionPermission} onChange={e => setConfig({ ...config, correctionPermission: e.target.value })}>
              <option value="AdminOnly">Admin Only</option>
              <option value="OwnerOnly">Owner Only</option>
              <option value="Anyone">Anyone</option>
            </select>
            <button onClick={saveConfig} style={btn("var(--green)")}>Save Config</button>
          </div>
        </div>
      )}

      <div style={cardS}>
        <h3 style={h3}>Pending Sync Entries — Correction Window Open</h3>
        <p style={{ color: "var(--muted)", fontSize: "0.8rem", marginBottom: "1rem" }}>
          <Clock size={14} style={{ verticalAlign: "middle", marginRight: "0.25rem" }} />
          These writes are not yet in the central database. You have until the grace period expires to correct mistakes. Once committed, the data is locked.
        </p>
        {dash.recentEntries.length === 0 ? (
          <p style={{ color: "var(--muted)", textAlign: "center", padding: "2rem" }}>No pending sync entries. All replica writes have been committed to the central database.</p>
        ) : (
          <table style={{ width: "100%", borderCollapse: "collapse", fontSize: "0.85rem" }}>
            <thead><tr style={{ borderBottom: "1px solid var(--line)" }}>{["Entity","Field","Was","Written","Status","Expires In",""].map(h => <th key={h} style={{ textAlign: "left", padding: "0.5rem 0.75rem", color: "var(--muted)", fontWeight: 500, fontSize: "0.75rem" }}>{h}</th>)}</tr></thead>
            <tbody>
              {dash.recentEntries.map(e => (
                <tr key={e.id} style={{ borderBottom: "1px solid var(--gray)" }}>
                  <td style={{ padding: "0.4rem 0.75rem", color: "var(--ink)", fontWeight: 500 }}>{e.entityType}</td>
                  <td style={{ padding: "0.4rem 0.75rem", color: "var(--muted)", fontSize: "0.82rem" }}>{e.fieldName || "—"}</td>
                  <td style={{ padding: "0.4rem 0.75rem", color: "var(--muted)", fontSize: "0.82rem" }}>{e.originalValue || "—"}</td>
                  <td style={{ padding: "0.4rem 0.75rem", color: "var(--ink)", fontWeight: 600 }}>{e.writtenValue || "—"}</td>
                  <td style={{ padding: "0.4rem 0.75rem" }}><span style={badge(e.status)}>{e.status}</span></td>
                  <td style={{ padding: "0.4rem 0.75rem", color: e.secondsRemaining < 60 ? "var(--red)" : e.secondsRemaining < 180 ? "var(--amber)" : "var(--muted)", fontWeight: 600 }}>{e.secondsRemaining}s</td>
                  <td style={{ padding: "0.4rem 0.75rem", display: "flex", gap: "0.35rem" }}>
                    {e.canCorrect && (
                      <>
                        <div style={{ display: "flex", gap: "0.25rem", alignItems: "center" }}>
                          <input style={{ ...inp, width: 60, fontSize: "0.8rem", padding: "0.25rem 0.4rem" }} placeholder="New value" value={corrections[e.id]?.value || ""} onChange={ev => setCorrections(prev => ({ ...prev, [e.id]: { ...prev[e.id], value: ev.target.value } }))} />
                          <button onClick={() => correct(e.id)} style={btn("var(--blue)")} title="Correct"><Edit3 size={12} /></button>
                          <button onClick={() => revert(e.id)} style={btn("var(--red)")} title="Revert"><RotateCcw size={12} /></button>
                        </div>
                      </>
                    )}
                    {e.isLocked && <Lock size={14} style={{ color: "var(--muted)" }} />}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </main>
  );
}
