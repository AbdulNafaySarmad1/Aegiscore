"use client";

import type { AuditDashboardData, SodRule } from "@/app/audit/page";
import { ApiError, apiPost, apiPut } from "@/lib/api-client";
import { Clipboard, FileArchive, KeyRound, RefreshCw, ShieldAlert, ShieldCheck, UsersRound } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormEvent, useState } from "react";

type EngagementResult = { id: string; auditorName: string; auditorEmail: string; expiresAt: string; token: string };

export function AuditDashboard({ dashboard, rules }: { dashboard: AuditDashboardData | null; rules: SodRule[] }) {
  const router = useRouter();
  const [notice, setNotice] = useState<string | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const latest = dashboard?.latestRun;

  async function createEngagement(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setNotice(null); setToken(null);
    const form = new FormData(event.currentTarget);
    try {
      const result = await apiPost<EngagementResult>("/api/finance/v2/audit/engagements", {
        auditorName: form.get("auditorName"), auditorEmail: form.get("auditorEmail"), expiresAt: new Date(String(form.get("expiresAt"))).toISOString(),
        scopes: form.getAll("scopes")
      });
      setToken(result.token); setNotice("Auditor access created. Store the token now; it will not be shown again."); router.refresh();
    } catch (error: unknown) { setNotice(message(error)); } finally { setBusy(false); }
  }

  async function generateEvidence(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setNotice(null);
    const form = new FormData(event.currentTarget);
    try {
      await apiPost("/api/finance/v2/audit/soc2-evidence", { periodFrom: new Date(String(form.get("periodFrom"))).toISOString(), periodTo: new Date(String(form.get("periodTo"))).toISOString(), auditorEngagementId: form.get("engagementId") || null });
      setNotice("SOC 2 evidence package generated and PQC-signed."); router.refresh();
    } catch (error: unknown) { setNotice(message(error)); } finally { setBusy(false); }
  }

  async function saveRule(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setNotice(null);
    const form = new FormData(event.currentTarget);
    try {
      await apiPut("/api/finance/v2/audit/sod-rules", { code: form.get("code"), description: form.get("description"), incompatibleActions: String(form.get("actions")).split(",").map(value => value.trim()).filter(Boolean), isActive: true });
      setNotice("Segregation-of-duties rule saved."); event.currentTarget.reset(); router.refresh();
    } catch (error: unknown) { setNotice(message(error)); } finally { setBusy(false); }
  }

  if (!dashboard) return <main className="finance-page"><div className="finance-alert" role="alert"><ShieldAlert />Audit data is unavailable or your role is not authorized.</div></main>;
  return <main className="finance-page audit-page">
    <header className="finance-heading"><div><p>Assurance workspace</p><h1>Audit and compliance</h1><span>Continuous controls, independent access, and signed evidence.</span></div><button className="icon-button" onClick={() => router.refresh()} aria-label="Refresh audit dashboard"><RefreshCw /></button><Link className="wizard-secondary" href="/">Workspace</Link></header>
    {notice && <div className="form-notice info" role="status">{notice}</div>}
    <section className="finance-metrics" aria-label="Audit metrics">
      <Metric icon={<ShieldCheck />} label="Control library" value={String(dashboard.controlCount)} detail="Automated controls" />
      <Metric icon={<ShieldAlert />} label="Latest flags" value={String(latest?.flaggedCount ?? 0)} detail={latest ? new Date(latest.evaluatedAt).toLocaleString() : "No posting evaluated"} />
      <Metric icon={<UsersRound />} label="Auditor access" value={String(dashboard.activeEngagements)} detail="Active engagements" />
      <Metric icon={<FileArchive />} label="Evidence packages" value={String(dashboard.packages.length)} detail="PQC-signed archives" />
    </section>
    <section className="audit-layout">
      <article className="surface"><div className="surface-heading"><div><h2>Continuous control runs</h2><p>Most recent voucher-triggered evaluations</p></div></div><div className="audit-table" role="region" aria-label="Continuous control runs" tabIndex={0}><table><thead><tr><th>Evaluated</th><th>Reference</th><th>Passed</th><th>Flagged</th><th>Hash</th></tr></thead><tbody>{dashboard.runs.slice(0, 25).map(run => <tr key={run.id}><td>{new Date(run.evaluatedAt).toLocaleString()}</td><td>{run.referenceType}<small>{run.referenceId ?? "Batch"}</small></td><td><span className="status-ok">{run.passedCount}/{run.controlCount}</span></td><td><span className={run.flaggedCount ? "status-danger" : "status-ok"}>{run.flaggedCount}</span></td><td><code>{run.contentHash.slice(0, 16)}...</code></td></tr>)}</tbody></table></div></article>
      <article className="surface audit-form-panel"><div className="surface-heading"><div><h2>External auditor access</h2><p>Time-limited, scope-restricted token</p></div><KeyRound /></div><form onSubmit={createEngagement}><label>Name<input name="auditorName" required minLength={2} maxLength={200} /></label><label>Email<input name="auditorEmail" type="email" required maxLength={320} /></label><label>Expires at<input name="expiresAt" type="datetime-local" required /></label><fieldset><legend>Scopes</legend>{[["controls:read", "Controls"], ["ledger:read", "Ledger"], ["samples:write", "Sampling"], ["evidence:read", "Evidence"]].map(([value, label]) => <label className="check-row" key={value}><input type="checkbox" name="scopes" value={value} defaultChecked />{label}</label>)}</fieldset><button className="wizard-primary" disabled={busy}>Create access</button></form>{token && <div className="one-time-token" role="status"><strong>One-time token</strong><code>{token}</code><button type="button" onClick={() => void navigator.clipboard.writeText(token)}><Clipboard />Copy token</button></div>}</article>
    </section>
    <section className="audit-layout">
      <article className="surface audit-form-panel"><div className="surface-heading"><div><h2>SOC 2 evidence export</h2><p>Immutable control activity package</p></div><FileArchive /></div><form onSubmit={generateEvidence}><label>Period start<input name="periodFrom" type="datetime-local" required /></label><label>Period end<input name="periodTo" type="datetime-local" required /></label><label>Restrict to engagement<select name="engagementId"><option value="">All authorized auditors</option>{dashboard.engagements.filter(item => !item.revokedAt).map(item => <option key={item.id} value={item.id}>{item.auditorName}</option>)}</select></label><button className="wizard-primary" disabled={busy}>Generate signed package</button></form></article>
      <article className="surface audit-form-panel"><div className="surface-heading"><div><h2>Segregation of duties</h2><p>Configurable incompatible actions</p></div><ShieldCheck /></div><form onSubmit={saveRule}><label>Rule code<input name="code" required minLength={3} maxLength={50} placeholder="PROCURE-001" /></label><label>Description<input name="description" required minLength={5} maxLength={500} /></label><label>Actions, comma separated<input name="actions" required placeholder="PO_CREATE, PO_APPROVE, GOODS_RECEIVE, PAYMENT_CREATE" /></label><button className="wizard-primary" disabled={busy}>Save rule</button></form><ul className="rule-list">{rules.map(rule => <li key={rule.id}><span><strong>{rule.code}</strong><small>{rule.description}</small></span><i className={rule.isActive ? "status-ok" : "status-warn"}>{rule.isActive ? "Active" : "Inactive"}</i></li>)}</ul></article>
    </section>
  </main>;
}

function Metric({ icon, label, value, detail }: { icon: React.ReactNode; label: string; value: string; detail: string }) { return <article className="finance-metric"><div className="metric-icon blue">{icon}</div><span>{label}</span><strong>{value}</strong><small>{detail}</small></article>; }
function message(error: unknown) { return error instanceof ApiError ? error.message : "The request could not be completed."; }
