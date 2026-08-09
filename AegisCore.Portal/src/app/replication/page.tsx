"use client";

import { useEffect, useState } from "react";
import { apiGet, apiPost, apiPut } from "@/lib/api-client";
import { usePersonalityTranslations } from "@/lib/personality-translator";

type ReplicaHealth = { id: string; name: string; status: string; priority: number; weight: number; currentLagSeconds: number | null; maxLagSeconds: number; isLagAcceptable: boolean; lastHealthCheckAt: string | null };
type Dashboard = { primaryHealthy: boolean; autoScalingEnabled: boolean; readRoutingEnabled: boolean; onlineReplicas: number; totalReplicas: number; maxReplicas: number; maxLagSeconds: number; criticalLagSeconds: number; activeAlertsCount: number; replicas: ReplicaHealth[] };
type ScalingConfig = { id: string; minReplicas: number; maxReplicas: number; defaultMaxLagSeconds: number; criticalLagSeconds: number; scaleUpThresholdConnections: number; scaleDownThresholdConnections: number; scaleDownCooldownMinutes: number; autoScalingEnabled: boolean; readRoutingEnabled: boolean; readDistributionStrategy: string };
type ScaleEvent = { id: string; action: string; previousReplicaCount: number; newReplicaCount: number; triggerReason: string; triggeredBy: string; wasSuccessful: boolean; triggeredAt: string };
type Alert = { id: string; message: string; lagSeconds: number; isCritical: boolean; triggeredAt: string };

const s = (o: Record<string,string>) => o;
const cardS = { background: "var(--surface)", borderRadius: 12, border: "1px solid var(--line)", padding: "1.5rem", marginBottom: "1.25rem" };
const h3 = { color: "var(--ink)", fontSize: "1.1rem", fontWeight: 600, margin: "0 0 0.75rem 0" };
const btn = (bg: string) => ({ padding: "0.5rem 1rem", borderRadius: 8, border: "none", cursor: "pointer", fontWeight: 600, fontSize: "0.825rem", background: bg, color: "#fff", display: "inline-flex", alignItems: "center", gap: "0.375rem" });
const inp = { padding: "0.5rem 0.75rem", borderRadius: 8, border: "1px solid var(--line)", background: "var(--canvas)", color: "var(--ink)", fontSize: "0.85rem", width: "100%", boxSizing: "border-box" as const };
const badge = (ok: boolean) => ({ display: "inline-block", padding: "0.15rem 0.55rem", borderRadius: 20, fontSize: "0.7rem", fontWeight: 600, background: ok ? "var(--green-soft)" : "var(--red-soft)", color: ok ? "var(--green)" : "var(--red)" });
const lagBadge = (lag: number | null | undefined, max: number) => {
  if (lag == null) return badge(false);
  const pct = lag / (max || 1);
  const bg = pct < 0.3 ? "var(--green-soft)" : pct < 0.7 ? "var(--amber-soft)" : "var(--red-soft)";
  const fg = pct < 0.3 ? "var(--green)" : pct < 0.7 ? "var(--amber)" : "var(--red)";
  return { display: "inline-block", padding: "0.15rem 0.55rem", borderRadius: 20, fontSize: "0.7rem", fontWeight: 600, background: bg, color: fg };
};

export default function ReplicationPage() {
  const t = usePersonalityTranslations("academic");
  const [dash, setDash] = useState<Dashboard | null>(null);
  const [config, setConfig] = useState<ScalingConfig | null>(null);
  const [events, setEvents] = useState<ScaleEvent[]>([]);
  const [alerts, setAlerts] = useState<Alert[]>([]);
  const [addName, setAddName] = useState("");
  const [addConn, setAddConn] = useState("");
  const [msg, setMsg] = useState("");

  async function refresh() {
    const [d, c, e, a] = await Promise.all([
      apiGet<Dashboard>("/api/admin/replication/dashboard").catch(() => null),
      apiGet<ScalingConfig>("/api/admin/replication/config").catch(() => null),
      apiGet<ScaleEvent[]>("/api/admin/replication/scale-history?limit=20").catch(() => []),
      apiGet<{ alerts: Alert[] }>("/api/admin/replication/alerts").catch(() => ({ alerts: [] })),
    ]);
    setDash(d); setConfig(c); setEvents(Array.isArray(e) ? e : []);
    setAlerts(a && Array.isArray(a.alerts) ? a.alerts : []);
  }

  useEffect(() => { const ctrl = new AbortController(); queueMicrotask(() => { if (!ctrl.signal.aborted) void refresh(); }); return () => ctrl.abort(); }, []);

  async function doPost(path: string) { try { await apiPost(path); setMsg("Done"); refresh(); } catch { setMsg("Failed"); } }

  async function addReplica() { try { await apiPost(`/api/admin/replication/replicas?name=${addName}&connectionString=${addConn}&priority=1&weight=1`); setMsg("Replica added"); setAddName(""); setAddConn(""); refresh(); } catch { setMsg("Failed"); } }

  async function saveConfig() {
    if (!config) return;
    try { await apiPut("/api/admin/replication/config", config); setMsg("Config saved"); } catch { setMsg("Failed"); }
  }

  if (!dash) return <main style={{ maxWidth: 1100, margin: "0 auto", padding: "1.5rem" }}><p style={{ color: "var(--muted)" }}>Loading...</p></main>;

  return (
    <main style={{ maxWidth: 1100, margin: "0 auto", padding: "1.5rem" }}>
      <h1 style={{ color: "var(--ink)", fontSize: "1.5rem", fontWeight: 600, marginBottom: "0.25rem" }}>Database Replication</h1>
      <p style={{ color: "var(--muted)", fontSize: "0.85rem", marginBottom: "1.5rem" }}>Read replicas, auto-scaling, lag monitoring, and failover management</p>
      {msg && <div onDoubleClick={() => setMsg("")} style={{ padding: "0.5rem 1rem", borderRadius: 8, background: "var(--blue-light)", color: "var(--blue)", marginBottom: "1rem", fontSize: "0.85rem", cursor: "pointer" }} title="Double-click to dismiss">{msg}</div>}

      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(180px, 1fr))", gap: "0.75rem", marginBottom: "1.5rem" }}>
        {[
          ["Online Replicas", `${dash.onlineReplicas} / ${dash.maxReplicas}`, "var(--green)"],
          ["Auto-Scaling", dash.autoScalingEnabled ? "ON" : "OFF", dash.autoScalingEnabled ? "var(--green)" : "var(--muted)"],
          ["Read Routing", dash.readRoutingEnabled ? "ON" : "OFF", dash.readRoutingEnabled ? "var(--blue)" : "var(--muted)"],
          ["Max Lag", `${dash.maxLagSeconds}s`, "var(--amber)"],
          ["Critical Lag", `${dash.criticalLagSeconds}s`, "var(--red)"],
          ["Alerts", String(dash.activeAlertsCount), dash.activeAlertsCount > 0 ? "var(--red)" : "var(--green)"],
        ].map(([label, val, color]) => (
          <div key={label} style={{ background: "var(--surface)", borderRadius: 12, border: "1px solid var(--line)", padding: "1rem" }}>
            <div style={{ fontSize: "0.75rem", color: "var(--muted)", marginBottom: "0.25rem" }}>{label}</div>
            <div style={{ fontSize: "1.5rem", fontWeight: 700, color }}>{val}</div>
          </div>
        ))}
      </div>

      <div style={{ display: "flex", gap: "0.5rem", marginBottom: "1.25rem", flexWrap: "wrap" }}>
        <button onClick={() => doPost("/api/admin/replication/health-check-all")} style={btn("var(--blue)")}>Run Health Check</button>
        <button onClick={() => doPost("/api/admin/replication/auto-scale")} style={btn("var(--green)")}>Evaluate Auto-Scale</button>
      </div>

      <div style={cardS}>
        <h3 style={h3}>Replica Health</h3>
        <table style={{ width: "100%", borderCollapse: "collapse", fontSize: "0.85rem" }}>
          <thead><tr style={{ borderBottom: "1px solid var(--line)" }}>{["Name","Status","Priority","Weight","Lag","Threshold","Acceptable"].map(h => <th key={h} style={{ textAlign: "left", padding: "0.5rem 0.75rem", color: "var(--muted)", fontWeight: 500, fontSize: "0.75rem" }}>{h}</th>)}</tr></thead>
          <tbody>
            {dash.replicas.map(r => (
              <tr key={r.id} style={{ borderBottom: "1px solid var(--gray)" }}>
                <td style={{ padding: "0.4rem 0.75rem", color: "var(--ink)", fontWeight: 500 }}>{r.name}</td>
                <td style={{ padding: "0.4rem 0.75rem" }}><span style={badge(r.status === "Online")}>{r.status}</span></td>
                <td style={{ padding: "0.4rem 0.75rem", color: "var(--muted)" }}>{r.priority}</td>
                <td style={{ padding: "0.4rem 0.75rem", color: "var(--muted)" }}>{r.weight}</td>
                <td style={{ padding: "0.4rem 0.75rem" }}><span style={lagBadge(r.currentLagSeconds, r.maxLagSeconds)}>{r.currentLagSeconds ?? "?"}s</span></td>
                <td style={{ padding: "0.4rem 0.75rem", color: "var(--muted)" }}>{r.maxLagSeconds}s</td>
                <td style={{ padding: "0.4rem 0.75rem" }}><span style={badge(r.isLagAcceptable)}>{r.isLagAcceptable ? "Yes" : "No"}</span></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div style={cardS}>
        <h3 style={h3}>Add Replica</h3>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 2fr auto", gap: "0.75rem", alignItems: "end" }}>
          <div><div style={{ fontSize: "0.75rem", color: "var(--muted)", marginBottom: "0.2rem" }}>Name</div><input style={inp} value={addName} onChange={e => setAddName(e.target.value)} placeholder="replica-us-east" /></div>
          <div><div style={{ fontSize: "0.75rem", color: "var(--muted)", marginBottom: "0.2rem" }}>Connection String</div><input style={inp} value={addConn} onChange={e => setAddConn(e.target.value)} placeholder="Host=replica.db.com;Port=5432;..." /></div>
          <button onClick={addReplica} style={btn("var(--blue)")}>Add</button>
        </div>
      </div>

      <div style={cardS}>
        <h3 style={h3}>Scaling Configuration</h3>
        {config && (
          <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(220px, 1fr))", gap: "0.75rem" }}>
            {[
              ["Min Replicas", "minReplicas", config.minReplicas],
              ["Max Replicas", "maxReplicas", config.maxReplicas],
              ["Default Max Lag (s)", "defaultMaxLagSeconds", config.defaultMaxLagSeconds],
              ["Critical Lag (s)", "criticalLagSeconds", config.criticalLagSeconds],
              ["Scale Up Threshold (conn)", "scaleUpThresholdConnections", config.scaleUpThresholdConnections],
              ["Scale Down Threshold (conn)", "scaleDownThresholdConnections", config.scaleDownThresholdConnections],
              ["Cool Down (min)", "scaleDownCooldownMinutes", config.scaleDownCooldownMinutes],
            ].map(([label, key, value]) => (
              <div key={key}>
                <div style={{ fontSize: "0.75rem", color: "var(--muted)", marginBottom: "0.2rem" }}>{label}</div>
                <input type="number" style={inp} value={value} onChange={e => setConfig({ ...config, [key as string]: parseInt(e.target.value) || 0 })} />
              </div>
            ))}
          </div>
        )}
        <div style={{ marginTop: "1rem", display: "flex", gap: "0.5rem", alignItems: "center" }}>
          <label style={{ display: "flex", alignItems: "center", gap: "0.4rem", fontSize: "0.85rem", color: "var(--ink)", cursor: "pointer" }}>
            <input type="checkbox" checked={config?.autoScalingEnabled ?? false} onChange={e => setConfig(c => c ? { ...c, autoScalingEnabled: e.target.checked } : c)} /> Auto-Scaling
          </label>
          <label style={{ display: "flex", alignItems: "center", gap: "0.4rem", fontSize: "0.85rem", color: "var(--ink)", cursor: "pointer" }}>
            <input type="checkbox" checked={config?.readRoutingEnabled ?? false} onChange={e => setConfig(c => c ? { ...c, readRoutingEnabled: e.target.checked } : c)} /> Read Routing
          </label>
          <select style={{ ...inp, width: "auto" }} value={config?.readDistributionStrategy ?? "round-robin"} onChange={e => setConfig(c => c ? { ...c, readDistributionStrategy: e.target.value } : c)}>
            <option value="round-robin">Round Robin</option>
            <option value="weighted">Weighted</option>
            <option value="least-loaded">Least Loaded</option>
          </select>
          <button onClick={saveConfig} style={btn("var(--green)")}>Save Config</button>
        </div>
      </div>

      {alerts.length > 0 && (
        <div style={cardS}>
          <h3 style={h3}>Active Replication Alerts</h3>
          {alerts.map(a => (
            <div key={a.id} style={{ padding: "0.75rem", borderRadius: 8, marginBottom: "0.5rem", background: a.isCritical ? "var(--red-soft)" : "var(--amber-soft)", border: `1px solid ${a.isCritical ? "var(--red)" : "var(--amber)"}`, fontSize: "0.85rem", color: "var(--ink)" }}>
              <strong>{a.isCritical ? "🚨 CRITICAL" : "⚠️ Warning"}</strong> — {a.message} <span style={{ color: "var(--muted)", marginLeft: "0.5rem" }}>({new Date(a.triggeredAt).toLocaleString()})</span>
            </div>
          ))}
        </div>
      )}

      <div style={cardS}>
        <h3 style={h3}>Scale Event History</h3>
        <table style={{ width: "100%", borderCollapse: "collapse", fontSize: "0.85rem" }}>
          <thead><tr style={{ borderBottom: "1px solid var(--line)" }}>{["Action","Before","After","Reason","Trigger","Time"].map(h => <th key={h} style={{ textAlign: "left", padding: "0.5rem 0.75rem", color: "var(--muted)", fontWeight: 500, fontSize: "0.75rem" }}>{h}</th>)}</tr></thead>
          <tbody>
            {events.map(e => (
              <tr key={e.id} style={{ borderBottom: "1px solid var(--gray)" }}>
                <td style={{ padding: "0.4rem 0.75rem" }}><span style={badge(e.action === "scale-up")}>{e.action}</span></td>
                <td style={{ padding: "0.4rem 0.75rem", color: "var(--ink)" }}>{e.previousReplicaCount}</td>
                <td style={{ padding: "0.4rem 0.75rem", color: "var(--ink)" }}>{e.newReplicaCount}</td>
                <td style={{ padding: "0.4rem 0.75rem", color: "var(--muted)", fontSize: "0.8rem" }}>{e.triggerReason}</td>
                <td style={{ padding: "0.4rem 0.75rem" }}><span style={badge(e.triggeredBy === "auto")}>{e.triggeredBy}</span></td>
                <td style={{ padding: "0.4rem 0.75rem", color: "var(--muted)", fontSize: "0.78rem" }}>{new Date(e.triggeredAt).toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </main>
  );
}
