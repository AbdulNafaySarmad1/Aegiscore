"use client";

import { Download, Eye, EyeOff, FileCheck2, KeyRound, ListChecks, Search, ShieldCheck } from "lucide-react";
import { FormEvent, useState } from "react";

type Session = { id: string; auditorName: string; auditorEmail: string; expiresAt: string; scopes: string[] };
type Control = { code: string; title: string; category: string; severity: string };
type ControlResponse = { library: Control[]; runs: Array<{ id: string; passedCount: number; flaggedCount: number; evaluatedAt: string }> };
type LedgerEntry = { id: string; entryNumber: string; entryDate: string; description: string; totalDebit: number; totalCredit: number; entryHash: string };
type Sample = { id: string; populationCount: number; sampleSize: number; selectedReferencesJson: string; createdAt: string };
type Evidence = { id: string; framework: string; periodFrom: string; periodTo: string; contentHash: string; pqcAlgorithm: string; pqcKeyId: string; generatedAt: string; download: string };

export function AuditorPortal() {
  const [token, setToken] = useState("");
  const [showToken, setShowToken] = useState(false);
  const [session, setSession] = useState<Session | null>(null);
  const [controls, setControls] = useState<ControlResponse | null>(null);
  const [ledger, setLedger] = useState<LedgerEntry[]>([]);
  const [samples, setSamples] = useState<Sample[]>([]);
  const [evidence, setEvidence] = useState<Evidence[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function authenticate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setError(null);
    try {
      const nextSession = await request<Session>("/api/auditor/session", token);
      setSession(nextSession);
      const [nextControls, nextSamples, nextEvidence] = await Promise.all([
        nextSession.scopes.includes("controls:read") ? request<ControlResponse>("/api/auditor/controls", token) : Promise.resolve(null),
        nextSession.scopes.includes("controls:read") ? request<Sample[]>("/api/auditor/samples", token) : Promise.resolve([]),
        nextSession.scopes.includes("evidence:read") ? request<Evidence[]>("/api/auditor/evidence", token) : Promise.resolve([])
      ]);
      setControls(nextControls); setSamples(nextSamples); setEvidence(nextEvidence);
    } catch (caught: unknown) { setSession(null); setError(caught instanceof Error ? caught.message : "Access denied."); } finally { setBusy(false); }
  }

  async function loadLedger(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setError(null); const form = new FormData(event.currentTarget);
    try { const result = await request<{ entries: LedgerEntry[] }>(`/api/auditor/ledger?from=${encodeURIComponent(String(form.get("from")))}&to=${encodeURIComponent(String(form.get("to")))}`, token); setLedger(result.entries); }
    catch (caught: unknown) { setError(caught instanceof Error ? caught.message : "Ledger could not be loaded."); } finally { setBusy(false); }
  }

  async function createSample(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setError(null); const form = new FormData(event.currentTarget);
    try {
      await request("/api/auditor/samples", token, { method: "POST", body: JSON.stringify({ populationType: "VOUCHERS", periodFrom: new Date(String(form.get("from"))).toISOString(), periodTo: new Date(String(form.get("to"))).toISOString(), sampleSize: Number(form.get("sampleSize")), seed: form.get("seed") }) });
      setSamples(await request<Sample[]>("/api/auditor/samples", token));
    } catch (caught: unknown) { setError(caught instanceof Error ? caught.message : "Sample could not be created."); } finally { setBusy(false); }
  }

  async function download(item: Evidence) {
    setError(null);
    const response = await fetch(item.download, { headers: { "X-AegisCore-Auditor-Token": token }, cache: "no-store" });
    if (!response.ok) { setError("Evidence download was denied."); return; }
    const url = URL.createObjectURL(await response.blob()); const anchor = document.createElement("a"); anchor.href = url; anchor.download = `aegiscore-soc2-${item.id}.json`; anchor.click(); URL.revokeObjectURL(url);
  }

  if (!session) return <main className="auditor-login"><div className="auditor-login-panel"><div className="brand-glyph"><ShieldCheck /></div><p>Independent assurance</p><h1>AegisCore auditor portal</h1><span>Enter the time-limited token issued by the institution. It remains in this browser tab only.</span><form onSubmit={authenticate}><label htmlFor="auditor-token">Access token</label><div className="secret-field"><input id="auditor-token" type={showToken ? "text" : "password"} value={token} onChange={event => setToken(event.target.value)} required autoComplete="off" /><button type="button" onClick={() => setShowToken(value => !value)} aria-label={showToken ? "Hide access token" : "Show access token"}>{showToken ? <EyeOff /> : <Eye />}</button></div><button className="wizard-primary" disabled={busy || token.length < 20}><KeyRound />Open engagement</button></form>{error && <p className="finance-alert" role="alert">{error}</p>}</div></main>;

  return <main className="finance-page audit-page"><header className="finance-heading"><div><p>External assurance</p><h1>{session.auditorName}</h1><span>Access expires {new Date(session.expiresAt).toLocaleString()} · {session.scopes.join(", ")}</span></div><ShieldCheck /></header>{error && <div className="finance-alert" role="alert">{error}</div>}
    <section className="audit-layout"><article className="surface"><div className="surface-heading"><div><h2>Control results</h2><p>{controls?.library.length ?? 0} controls in scope</p></div><ListChecks /></div><div className="audit-table" role="region" aria-label="Control library" tabIndex={0}><table><thead><tr><th>Control</th><th>Category</th><th>Severity</th></tr></thead><tbody>{controls?.library.map(control => <tr key={control.code}><td><strong>{control.code}</strong><small>{control.title}</small></td><td>{control.category}</td><td>{control.severity}</td></tr>)}</tbody></table></div></article>
      {session.scopes.includes("ledger:read") && <article className="surface audit-form-panel"><div className="surface-heading"><div><h2>Ledger inspection</h2><p>Read-only, bounded to 366 days</p></div><Search /></div><form onSubmit={loadLedger}><label>From<input name="from" type="date" required /></label><label>To<input name="to" type="date" required /></label><button className="wizard-primary" disabled={busy}>Load ledger</button></form><div className="audit-table compact" role="region" aria-label="Ledger entries" tabIndex={0}><table><thead><tr><th>Entry</th><th>Date</th><th>Debit</th><th>Credit</th></tr></thead><tbody>{ledger.map(entry => <tr key={entry.id}><td><strong>{entry.entryNumber}</strong><small>{entry.description}</small></td><td>{new Date(entry.entryDate).toLocaleDateString()}</td><td>{entry.totalDebit.toFixed(2)}</td><td>{entry.totalCredit.toFixed(2)}</td></tr>)}</tbody></table></div></article>}
    </section>
    <section className="audit-layout">{session.scopes.includes("samples:write") && <article className="surface audit-form-panel"><div className="surface-heading"><div><h2>Audit sampling</h2><p>Deterministic reproducible selection</p></div><ListChecks /></div><form onSubmit={createSample}><label>From<input name="from" type="date" required /></label><label>To<input name="to" type="date" required /></label><label>Sample size<input name="sampleSize" type="number" min={1} max={500} defaultValue={25} required /></label><label>Private seed<input name="seed" minLength={16} maxLength={200} required autoComplete="off" /></label><button className="wizard-primary" disabled={busy}>Create sample</button></form><ul className="rule-list">{samples.map(sample => <li key={sample.id}><span><strong>{sample.sampleSize} of {sample.populationCount}</strong><small>{new Date(sample.createdAt).toLocaleString()}</small></span></li>)}</ul></article>}
      {session.scopes.includes("evidence:read") && <article className="surface"><div className="surface-heading"><div><h2>Signed evidence</h2><p>Offline-verifiable SOC 2 packages</p></div><FileCheck2 /></div><ul className="evidence-list">{evidence.map(item => <li key={item.id}><span><strong>{item.framework}</strong><small>{new Date(item.periodFrom).toLocaleDateString()} – {new Date(item.periodTo).toLocaleDateString()}</small><code>{item.contentHash.slice(0, 24)}...</code></span><button className="icon-button" onClick={() => void download(item)} aria-label={`Download evidence generated ${new Date(item.generatedAt).toLocaleDateString()}`}><Download /></button></li>)}</ul></article>}
    </section>
  </main>;
}

async function request<T>(path: string, token: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, { ...init, cache: "no-store", headers: { "Content-Type": "application/json", "X-AegisCore-Auditor-Token": token, ...init?.headers } });
  const payload: unknown = await response.json().catch(() => null);
  if (!response.ok) throw new Error(response.status === 401 ? "The token is invalid, expired, revoked, or lacks this scope." : `Request failed (${response.status}).`);
  return payload as T;
}
