"use client";

import { useState, type FormEvent } from "react";
import { apiGet, apiPost, apiPut } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel, Figures, Mark, Notice } from "@/components/register";
import { Ledger, toneOf, words } from "@/components/register/ledger";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type ReplicaHealth = { id: string; name: string; status: string; priority: number; weight: number; currentLagSeconds: number | null; maxLagSeconds: number; isLagAcceptable: boolean; lastHealthCheckAt: string | null };
type Dashboard = { primaryHealthy: boolean; autoScalingEnabled: boolean; readRoutingEnabled: boolean; onlineReplicas: number; totalReplicas: number; maxReplicas: number; maxLagSeconds: number; criticalLagSeconds: number; activeAlertsCount: number; replicas: ReplicaHealth[] };
type ScalingConfig = { id: string; minReplicas: number; maxReplicas: number; defaultMaxLagSeconds: number; criticalLagSeconds: number; scaleUpThresholdConnections: number; scaleDownThresholdConnections: number; scaleDownCooldownMinutes: number; autoScalingEnabled: boolean; readRoutingEnabled: boolean; readDistributionStrategy: string };
type ScaleEvent = { id: string; action: string; previousReplicaCount: number; newReplicaCount: number; triggerReason: string; triggeredBy: string; wasSuccessful: boolean; triggeredAt: string };
type Alert = { id: string; message: string; lagSeconds: number; isCritical: boolean; triggeredAt: string };
type Everything = { dashboard: Dashboard | null; config: ScalingConfig | null; events: ScaleEvent[]; alerts: Alert[] };

/** Read replicas: how far behind the primary each one is, and what the installation does about it. */
export default function ReplicationPage() {
  const { dateTime } = useTenantFormatter();
  const { state, reload } = useModuleData<Everything>(async signal => {
    const [dashboard, config, events, alerts] = await Promise.all([
      apiGet<Dashboard>("/api/admin/replication/dashboard", signal).catch(() => null),
      apiGet<ScalingConfig>("/api/admin/replication/config", signal).catch(() => null),
      apiGet<ScaleEvent[]>("/api/admin/replication/scale-history?limit=20", signal).catch(() => []),
      apiGet<{ alerts: Alert[] }>("/api/admin/replication/alerts", signal).then(r => r.alerts ?? []).catch(() => []),
    ]);
    return { dashboard, config, events: Array.isArray(events) ? events : [], alerts };
  });
  const action = useAction();
  const [replica, setReplica] = useState({ name: "", connectionString: "" });

  async function run(name: string, path: string, said: string) {
    await action.run(name, async () => { await apiPost(path); return said; }, reload);
  }

  async function addReplica(e: FormEvent) {
    e.preventDefault();
    await action.run("add", async () => {
      const query = new URLSearchParams({ name: replica.name, connectionString: replica.connectionString, priority: "1", weight: "1" });
      await apiPost(`/api/admin/replication/replicas?${query}`);
      const name = replica.name;
      setReplica({ name: "", connectionString: "" });
      return `${name} is registered. Run a health check to see how far behind it is.`;
    }, reload);
  }

  async function saveConfig(config: ScalingConfig) {
    await action.run("config", async () => {
      await apiPut("/api/admin/replication/config", config);
      return "Saved.";
    }, reload);
  }

  return (
    <ModulePage
      title="Replication"
      crumbs={[{ label: "Operations" }, { label: "Replication" }]}
      meta={<span>Read replicas of the university database, how far behind each has fallen, and what happens when one falls too far.</span>}
      ident={(d: Everything) => <><strong>{d.dashboard?.onlineReplicas ?? 0}</strong><br />online</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {({ dashboard, config, events, alerts }) => !dashboard ? (
        <Notice tone="oxblood" title="The replication service did not answer." role="alert">
          Nothing on this screen can be trusted until it does. Check the application log.
        </Notice>
      ) : (
        <div className="record">
          <div className="record-body">
            <Figures aria-label="Replication figures" items={[
              { label: "Primary", value: dashboard.primaryHealthy ? "Answering" : "Silent", tone: dashboard.primaryHealthy ? "laurel" : "oxblood", note: "asked just now" },
              { label: "Replicas online", value: `${dashboard.onlineReplicas} of ${dashboard.totalReplicas}`, tone: dashboard.totalReplicas === 0 ? "ink" : dashboard.onlineReplicas === dashboard.totalReplicas ? "laurel" : "brass", note: `at most ${dashboard.maxReplicas}` },
              { label: "Alerts", value: dashboard.activeAlertsCount, tone: dashboard.activeAlertsCount > 0 ? "oxblood" : "laurel", note: dashboard.activeAlertsCount === 0 ? "none open" : "lag past the limit" },
              { label: "Lag allowed", value: `${dashboard.maxLagSeconds}s`, note: `critical past ${dashboard.criticalLagSeconds}s` },
            ]} />

            {!dashboard.primaryHealthy && (
              <Notice tone="oxblood" title="The primary database did not answer." role="alert">
                Every replica is a copy of a database that is not responding. Attend to the primary first.
              </Notice>
            )}

            {alerts.length > 0 && (
              <Panel tone="oxblood" title="Open alerts" flush>
                <Ledger rows={alerts} keyOf={a => a.id} minWidth={640} status={a => a.isCritical ? "oxblood" : "brass"}
                  empty={{ title: "No alerts" }}
                  columns={[
                    { key: "when", label: "Raised", render: a => dateTime(a.triggeredAt), nowrap: true },
                    { key: "what", label: "What happened", render: a => a.message },
                    { key: "lag", label: "Behind by", render: a => `${a.lagSeconds}s`, num: true, nowrap: true },
                    { key: "severity", label: "", render: a => <Mark tone={a.isCritical ? "oxblood" : "brass"}>{a.isCritical ? "Critical" : "Warning"}</Mark> },
                  ]} />
              </Panel>
            )}

            <Panel title="Replicas" flush>
              <Ledger rows={dashboard.replicas} keyOf={r => r.id} minWidth={760}
                status={r => r.isLagAcceptable ? toneOf(r.status) : "oxblood"}
                empty={{
                  title: "No read replica is registered",
                  detail: "Every read goes to the primary. That is a supported way to run — a replica is worth adding when reporting starts to slow the primary down.",
                }}
                columns={[
                  { key: "name", label: "Replica", render: r => r.name },
                  { key: "status", label: "Status", render: r => <Mark tone={toneOf(r.status)}>{words(r.status)}</Mark> },
                  { key: "lag", label: "Behind by", render: r => r.currentLagSeconds === null ? "not measured" : `${r.currentLagSeconds}s`, num: true, nowrap: true },
                  { key: "limit", label: "Allowed", render: r => `${r.maxLagSeconds}s`, num: true, nowrap: true },
                  { key: "share", label: "Share of reads", render: r => `weight ${r.weight}, priority ${r.priority}`, nowrap: true },
                  { key: "checked", label: "Last checked", render: r => r.lastHealthCheckAt ? dateTime(r.lastHealthCheckAt) : "never", nowrap: true },
                ]} />
            </Panel>

            <Panel title="Scaling decisions" flush>
              <Ledger rows={events} keyOf={e => e.id} minWidth={780} status={e => e.wasSuccessful ? "laurel" : "oxblood"}
                empty={{ title: "No replica has been added or removed", detail: "Every decision to scale up or down is recorded here with the reason for it." }}
                columns={[
                  { key: "when", label: "When", render: e => dateTime(e.triggeredAt), nowrap: true },
                  { key: "action", label: "Decision", render: e => words(e.action) },
                  { key: "count", label: "Replicas", render: e => `${e.previousReplicaCount} → ${e.newReplicaCount}`, nowrap: true },
                  { key: "why", label: "Because", render: e => e.triggerReason },
                  { key: "who", label: "Decided by", render: e => words(e.triggeredBy) },
                  { key: "ok", label: "", render: e => <Mark tone={e.wasSuccessful ? "laurel" : "oxblood"}>{e.wasSuccessful ? "Done" : "Failed"}</Mark> },
                ]} />
            </Panel>
          </div>

          <aside className="record-margin">
            <Panel title="Check now">
              <div className="panel-form">
                <button type="button" className={`btn btn-secondary${action.busy === "health" ? " btn-loading" : ""}`} disabled={action.busy !== null}
                  onClick={() => run("health", "/api/admin/replication/health-check-all", "Every replica was asked how far behind it is.")}>
                  Measure lag on every replica
                </button>
                <button type="button" className={`btn btn-secondary${action.busy === "scale" ? " btn-loading" : ""}`} disabled={action.busy !== null}
                  onClick={() => run("scale", "/api/admin/replication/auto-scale", "The scaling rules were applied to the current load.")}>
                  Apply the scaling rules now
                </button>
              </div>
            </Panel>

            <Panel title="Register a replica">
              <form className="panel-form" onSubmit={addReplica}>
                <label><span>Name</span><input value={replica.name} onChange={e => setReplica({ ...replica, name: e.target.value })} placeholder="replica-1" required /></label>
                <label><span>Connection string</span><input value={replica.connectionString} onChange={e => setReplica({ ...replica, connectionString: e.target.value })} placeholder="Host=…;Database=…" required /></label>
                <p className="field-hint">The replica must already exist and be replicating; this only tells the installation where to find it.</p>
                <button type="submit" className={`btn btn-primary${action.busy === "add" ? " btn-loading" : ""}`} disabled={action.busy !== null}>Register it</button>
              </form>
            </Panel>

            {config && (
              <Panel title="Rules">
                <form className="panel-form" onSubmit={e => { e.preventDefault(); saveConfig(config); }}>
                  <label className="check-row"><input type="checkbox" checked={config.autoScalingEnabled} onChange={e => saveConfig({ ...config, autoScalingEnabled: e.target.checked })} /><span>Add and remove replicas automatically</span></label>
                  <label className="check-row"><input type="checkbox" checked={config.readRoutingEnabled} onChange={e => saveConfig({ ...config, readRoutingEnabled: e.target.checked })} /><span>Send reads to replicas</span></label>
                  <dl className="facts">
                    <div><dt>Replicas</dt><dd>{config.minReplicas} to {config.maxReplicas}</dd></div>
                    <div><dt>Lag allowed</dt><dd>{config.defaultMaxLagSeconds}s, critical at {config.criticalLagSeconds}s</dd></div>
                    <div><dt>Add one past</dt><dd>{config.scaleUpThresholdConnections} connections</dd></div>
                    <div><dt>Remove one below</dt><dd>{config.scaleDownThresholdConnections} connections</dd></div>
                    <div><dt>Wait before removing</dt><dd>{config.scaleDownCooldownMinutes} minutes</dd></div>
                    <div><dt>Reads are spread</dt><dd>{words(config.readDistributionStrategy)}</dd></div>
                  </dl>
                </form>
              </Panel>
            )}
          </aside>
        </div>
      )}
    </ModulePage>
  );
}
