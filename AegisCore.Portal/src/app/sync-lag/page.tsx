"use client";

import { useEffect, useState } from "react";
import { apiGet, apiPost, apiPut } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel, Figures, Mark, Notice } from "@/components/register";
import { Ledger, toneOf, words } from "@/components/register/ledger";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type SyncEntry = {
  id: string; entityType: string; fieldName: string | null; originalValue: string | null; writtenValue: string | null;
  status: string; writtenAt: string; graceExpiresAt: string; secondsRemaining: number; isLocked: boolean; canCorrect: boolean;
};
type Dashboard = { totalPending: number; expiringWithin5Min: number; committedToday: number; correctedToday: number; defaultGraceSeconds: number; autoCommitEnabled: boolean; recentEntries: SyncEntry[] };
type SyncConfig = { id: string; defaultGracePeriodSeconds: number; maxGracePeriodSeconds: number; autoCommitOnGraceExpiry: boolean; requireAdminApprovalForCorrections: boolean; correctionPermission: string; notifyOwnerOnCorrection: boolean };

const remaining = (seconds: number) => seconds <= 0 ? "expired" : seconds < 60 ? `${seconds}s left` : `${Math.floor(seconds / 60)} min left`;

/**
 * Writes made through a replica wait here before they reach the central database. While one waits it
 * can still be corrected or withdrawn; once it commits it is part of the record.
 */
export default function SyncLagPage() {
  const { dateTime } = useTenantFormatter();
  const { state, reload } = useModuleData<{ dashboard: Dashboard | null; config: SyncConfig | null }>(async signal => {
    const [dashboard, config] = await Promise.all([
      apiGet<Dashboard>("/api/admin/sync-lag/dashboard", signal).catch(() => null),
      apiGet<SyncConfig>("/api/admin/sync-lag/config", signal).catch(() => null),
    ]);
    return { dashboard, config };
  });
  const action = useAction();
  const [editing, setEditing] = useState<{ entry: SyncEntry; value: string; reason: string } | null>(null);

  // The window closes on its own, so the list is re-read while it is open.
  useEffect(() => {
    const id = window.setInterval(reload, 10000);
    return () => window.clearInterval(id);
  }, [reload]);

  async function correct() {
    const { entry, value, reason } = editing!;
    setEditing(null);
    await action.run("correct", async () => {
      await apiPost(`/api/admin/sync-lag/entries/${entry.id}/correct?correctedValue=${encodeURIComponent(value)}&reason=${encodeURIComponent(reason)}`);
      return `Corrected to “${value}”. That is what will reach the central database.`;
    }, reload);
  }

  async function revert(entry: SyncEntry) {
    await action.run(entry.id, async () => {
      await apiPost(`/api/admin/sync-lag/entries/${entry.id}/revert`);
      return "Withdrawn. Nothing will be sent for that write.";
    }, reload);
  }

  async function saveConfig(config: SyncConfig) {
    await action.run("config", async () => { await apiPut("/api/admin/sync-lag/config", config); return "Saved."; }, reload);
  }

  return (
    <ModulePage
      title="Correction window"
      crumbs={[{ label: "Operations" }, { label: "Correction window" }]}
      meta={<span>Writes made at a replica, waiting to reach the central database. Until they do, they can still be put right.</span>}
      ident={d => <><strong>{d.dashboard?.totalPending ?? 0}</strong><br />waiting</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {({ dashboard, config }) => !dashboard || !config ? (
        <Notice tone="oxblood" title="The correction service did not answer." role="alert">
          Whether any writes are waiting cannot be told from here. Check the application log.
        </Notice>
      ) : (
        <div className="record">
          <div className="record-body">
            <Figures aria-label="Correction window figures" items={[
              { label: "Waiting", value: dashboard.totalPending, tone: dashboard.totalPending > 0 ? "brass" : "laurel", note: "not yet in the central database" },
              { label: "Closing within 5 min", value: dashboard.expiringWithin5Min, tone: dashboard.expiringWithin5Min > 0 ? "oxblood" : "ink", note: "correct these first" },
              { label: "Committed today", value: dashboard.committedToday, note: "now part of the record" },
              { label: "Corrected today", value: dashboard.correctedToday, note: "put right before committing" },
            ]} />

            {editing && (
              <Panel tone="brass" title={`Correct this ${words(editing.entry.entityType).toLowerCase()}`}>
                <div className="panel-form">
                  <dl className="facts">
                    <div><dt>Field</dt><dd>{editing.entry.fieldName ?? "the whole record"}</dd></div>
                    <div><dt>Was</dt><dd className="mono">{editing.entry.originalValue ?? "—"}</dd></div>
                    <div><dt>Written as</dt><dd className="mono">{editing.entry.writtenValue ?? "—"}</dd></div>
                  </dl>
                  <label><span>Correct it to</span><input value={editing.value} onChange={e => setEditing({ ...editing, value: e.target.value })} autoFocus /></label>
                  <label><span>Why</span><input value={editing.reason} onChange={e => setEditing({ ...editing, reason: e.target.value })} placeholder="e.g. the invigilator read the wrong row" /></label>
                  <p className="field-hint">The reason is kept with the entry, against your name.</p>
                  <div className="btn-row">
                    <button type="button" className="btn btn-primary" disabled={!editing.value.trim() || !editing.reason.trim()} onClick={correct}>Correct it</button>
                    <button type="button" className="btn btn-ghost" onClick={() => setEditing(null)}>Leave it</button>
                  </div>
                </div>
              </Panel>
            )}

            <Panel title="Waiting to commit" flush>
              <Ledger rows={dashboard.recentEntries} keyOf={e => e.id} minWidth={880}
                status={e => e.isLocked ? undefined : e.secondsRemaining < 300 ? "oxblood" : toneOf(e.status)}
                empty={{ title: "Nothing is waiting", detail: "Every write made at a replica has reached the central database." }}
                columns={[
                  { key: "what", label: "Record", render: e => <>{words(e.entityType)}<br /><span className="text-muted">{e.fieldName ?? "whole record"}</span></> },
                  { key: "was", label: "Was", render: e => e.originalValue ?? "—", mono: true },
                  { key: "now", label: "Written as", render: e => e.writtenValue ?? "—", mono: true },
                  { key: "status", label: "Status", render: e => <Mark tone={toneOf(e.status)}>{words(e.status)}</Mark> },
                  { key: "written", label: "Written", render: e => dateTime(e.writtenAt), nowrap: true },
                  { key: "left", label: "Window", render: e => e.isLocked ? "closed" : remaining(e.secondsRemaining), nowrap: true },
                  {
                    key: "act", label: "", nowrap: true, render: e => e.canCorrect ? (
                      <span className="btn-row">
                        <button type="button" className="btn btn-secondary btn-sm" disabled={action.busy !== null} onClick={() => setEditing({ entry: e, value: e.writtenValue ?? "", reason: "" })}>Correct</button>
                        <button type="button" className="btn btn-ghost btn-sm" disabled={action.busy !== null} onClick={() => revert(e)}>Withdraw</button>
                      </span>
                    ) : <span className="text-muted">committed</span>,
                  },
                ]} />
            </Panel>
          </div>

          <aside className="record-margin">
            <Panel title="Commit now">
              <div className="panel-form">
                <p className="field-hint">Sends every write whose window has closed to the central database, rather than waiting for the next sweep.</p>
                <button type="button" className={`btn btn-secondary${action.busy === "commit" ? " btn-loading" : ""}`} disabled={action.busy !== null}
                  onClick={() => action.run("commit", async () => {
                    const r = await apiPost<{ committed: number }>("/api/admin/sync-lag/commit-expired");
                    return r.committed === 0 ? "Nothing had expired; nothing was sent." : `${r.committed} write${r.committed === 1 ? "" : "s"} committed and locked.`;
                  }, reload)}>
                  Commit what has expired
                </button>
              </div>
            </Panel>

            <Panel title="How long the window stays open">
              <form className="panel-form" onSubmit={e => { e.preventDefault(); saveConfig(config); }}>
                <label><span>Seconds</span><input type="number" min={1} value={config.defaultGracePeriodSeconds} onChange={e => saveConfig({ ...config, defaultGracePeriodSeconds: parseInt(e.target.value) || 300 })} /></label>
                <label><span>Longest allowed</span><input type="number" min={1} value={config.maxGracePeriodSeconds} onChange={e => saveConfig({ ...config, maxGracePeriodSeconds: parseInt(e.target.value) || 86400 })} /></label>
                <label className="check-row"><input type="checkbox" checked={config.autoCommitOnGraceExpiry} onChange={e => saveConfig({ ...config, autoCommitOnGraceExpiry: e.target.checked })} /><span>Commit automatically when the window closes</span></label>
                <label className="check-row"><input type="checkbox" checked={config.requireAdminApprovalForCorrections} onChange={e => saveConfig({ ...config, requireAdminApprovalForCorrections: e.target.checked })} /><span>A correction needs an administrator</span></label>
                <label><span>Who may correct</span>
                  <select value={config.correctionPermission} onChange={e => saveConfig({ ...config, correctionPermission: e.target.value })}>
                    <option value="AdminOnly">An administrator only</option>
                    <option value="OwnerOnly">Whoever made the write</option>
                    <option value="Anyone">Anyone with access</option>
                  </select>
                </label>
                {!config.autoCommitOnGraceExpiry && (
                  <p className="field-hint">With automatic commit off, writes wait until someone commits them here.</p>
                )}
              </form>
            </Panel>

            <Notice tone="ink">
              A correction is recorded against the person who made it, with the reason given. Once a
              write commits it is part of the record and can only be changed by a further entry.
            </Notice>
          </aside>
        </div>
      )}
    </ModulePage>
  );
}
