"use client";

import { Check, ChevronLeft, ChevronRight, Plus, ShieldCheck, Trash2, Upload } from "lucide-react";
import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { apiGet, apiPost, apiPostForm, setSetupToken } from "@/lib/api-client";

type WizardData = Record<string, unknown>;
type Session = { sessionId: string; currentStep: number; licenseMode: "demo" | "paid"; data: Record<string, WizardData> };
type RoleConfig = { name: string; permissions: string[] };
type ImportResult = { isValid: boolean; totalRows: number; errors: string[]; warnings: string[] };
type DatabaseResult = { success: boolean; message: string; error?: { userMessage?: string } };
type HandshakeResult = { success: boolean; error?: string; fixHint?: string; loginUrl?: string };
type SsoProviderEntry = {
  provider: string; tenantId?: string; clientId?: string; clientSecret?: string; domain?: string; authority?: string; scopes?: string;
  groupRoleMapping?: Record<string, string>; defaultRole?: string; hasSecret?: boolean; connectionTested?: boolean; clientSecretName?: string;
};
const SSO_METHODS: Array<{ type: string; label: string; note: string }> = [
  { type: "local", label: "Email and password", note: "Accounts held in this register. Keep it for a few administrators even when SSO is on." },
  { type: "entra", label: "Microsoft Entra ID", note: "Staff and students sign in with their Microsoft 365 account." },
  { type: "google", label: "Google Workspace", note: "Sign in with a Google account in your Workspace domain." },
  { type: "okta", label: "Okta", note: "Sign in through your Okta org (OpenID Connect)." },
];
function ssoProviders(value: WizardData): SsoProviderEntry[] {
  const list = value.providers;
  if (Array.isArray(list)) return list as SsoProviderEntry[];
  // A session saved before step 3 held a list: one provider object at the top level.
  return typeof value.provider === "string" ? [value as unknown as SsoProviderEntry] : [];
}
function ssoReady(value: WizardData) {
  const list = ssoProviders(value);
  return list.length > 0 && list.every(entry => entry.provider === "local" || entry.connectionTested === true);
}
type AiConnectionResult = { success: boolean; response?: string; error?: string; fixHint?: string };
type TemplateResult = { id: string; name: string; previewHtml: string; extractedPalette: string[]; extractedFonts: string[]; logoUrl?: string };

const STEPS = [
  "University identity",
  "Database connection",
  "SSO configuration",
  "Permission tree",
  "AI model configuration",
  "Data import",
  "Review and go-live",
] as const;

// Staff, Payroll and Welfare are their own resources so that HR, pay and counselling records are
// not opened by the generic System permission (docs/compliance/04, AC-29).
const MODULES = ["Students", "Grades", "Attendance", "Finance", "Timetable", "Admissions", "Reports", "Settings", "System", "Staff", "Payroll", "Welfare", "Graduation", "Integrations"];
const ACTIONS = ["Read", "Write", "Delete", "Approve", "Export"];
const DEFAULT_ROLES: RoleConfig[] = [
  { name: "Admin", permissions: MODULES.flatMap(module => ACTIONS.map(action => `${module}.${action}`)) },
  { name: "Faculty", permissions: ["Students.Read", "Grades.Read", "Grades.Write", "Attendance.Read", "Attendance.Write"] },
  { name: "Student", permissions: ["Grades.Read", "Attendance.Read", "Timetable.Read"] },
  { name: "HR", permissions: ["Staff.Read", "Staff.Write", "Staff.Approve", "Payroll.Read", "Payroll.Write", "Payroll.Export"] },
  { name: "Counsellor", permissions: ["Students.Read", "Welfare.Read", "Welfare.Write"] },
];

function text(value: WizardData, key: string, fallback = "") {
  return typeof value[key] === "string" ? String(value[key]) : fallback;
}

function Field({ label, value, onChange, type = "text", required = true, placeholder }: {
  label: string; value: string; onChange: (value: string) => void; type?: string; required?: boolean; placeholder?: string;
}) {
  return (
    <label className="wizard-field">
      <span>{label}{required ? " *" : ""}</span>
      <input type={type} value={value} required={required} placeholder={placeholder} onChange={event => onChange(event.target.value)} />
    </label>
  );
}

function SelectField({ label, value, onChange, options }: {
  label: string; value: string; onChange: (value: string) => void; options: Array<{ value: string; label: string }>;
}) {
  return (
    <label className="wizard-field">
      <span>{label} *</span>
      <select value={value} onChange={event => onChange(event.target.value)}>
        {options.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
      </select>
    </label>
  );
}

/**
 * Poll until the application answers as ready again. `/api/runtime/ready` is served by the portal
 * and asks the API's own readiness check, so a 200 means the process that was restarted is back and
 * every startup check passed — not merely that a port is open. It gives up after two minutes so the
 * screen can say so instead of spinning for ever.
 */
async function waitUntilAnswering(timeoutMs = 120_000): Promise<boolean> {
  const deadline = Date.now() + timeoutMs;
  // Give the supervisor time to begin the restart; answering "ready" from the process that is about
  // to go down would be the same lie by a different route.
  await new Promise(resolve => setTimeout(resolve, 5_000));
  while (Date.now() < deadline) {
    try {
      const response = await fetch("/api/runtime/ready", { cache: "no-store", signal: AbortSignal.timeout(4_000) });
      if (response.ok) {
        await response.body?.cancel();
        return true;
      }
      await response.body?.cancel();
    } catch { /* still down */ }
    await new Promise(resolve => setTimeout(resolve, 2_000));
  }
  return false;
}

export function WizardShell() {
  const [token, setToken] = useState("");
  const [tokenAccepted, setTokenAccepted] = useState(false);
  const [session, setSession] = useState<Session | null>(null);
  const [step, setStep] = useState(1);
  const [data, setData] = useState<Record<string, WizardData>>({});
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState("");
  const [activated, setActivated] = useState(false);
  const [restarting, setRestarting] = useState(false);
  const [restartNote, setRestartNote] = useState("");

  const loadSession = useCallback(async () => {
    setBusy(true);
    setNotice("");
    try {
      const value = await apiGet<Session>("/api/wizard/session");
      setSession(value);
      setData(value.data ?? {});
      setStep(Math.max(1, Math.min(value.currentStep || 1, 7)));
      setTokenAccepted(true);
    } catch (error) {
      setTokenAccepted(false);
      setNotice(error instanceof Error ? error.message : "The setup token could not be verified.");
    } finally {
      setBusy(false);
    }
  }, []);

  useEffect(() => {
    const saved = window.sessionStorage.getItem("aegiscore_setup_token");
    if (saved) {
      // eslint-disable-next-line react-hooks/set-state-in-effect -- restore persisted bootstrap token on mount.
      setToken(saved);
      void loadSession();
    }
  }, [loadSession]);

  const current = useMemo<WizardData>(() => ({
    ...(step === 1 ? { timezone: Intl.DateTimeFormat().resolvedOptions().timeZone }
      : step === 3 ? { providers: [] } : step === 4 ? { roles: DEFAULT_ROLES } : {}),
    ...data[`step${step}`],
  }), [data, step]);
  const canContinue = (step !== 3 || ssoReady(current))
    && (step !== 5 || current.provider === "disabled" || current.connectionTested === true);
  const update = (patch: WizardData) => setData(existing => ({
    ...existing,
    [`step${step}`]: { ...existing[`step${step}`], ...patch },
  }));

  async function unlock(event: React.FormEvent) {
    event.preventDefault();
    setSetupToken(token);
    await loadSession();
  }

  async function saveAndContinue() {
    if (!session) return;
    setBusy(true);
    setNotice("");
    try {
      if (step === 1) {
        await apiPost("/api/wizard/mode", {
          sessionId: session.sessionId,
          mode: text(current, "licenseMode", session.licenseMode),
          licenseKey: text(current, "licenseKey"),
        });
      }
      if (step === 4) {
        const roles = (current.roles as RoleConfig[] | undefined) ?? DEFAULT_ROLES;
        const definitions = MODULES.flatMap(resource => ACTIONS.map(action => ({ resource, action })));
        await apiPost("/api/wizard/step4/roles", { roles: roles.map(role => role.name) });
        await apiPost("/api/wizard/step4/permissions", {
          permissions: definitions,
        });
        await apiPost("/api/wizard/step4/matrix", {
          entries: roles.flatMap(role => definitions.map(permission => ({
            role: role.name,
            ...permission,
            allowed: role.permissions.includes(`${permission.resource}.${permission.action}`),
          }))),
        });
      }
      const result = await apiPost<{ nextStep: number }>(`/api/wizard/step/${step}/save`, {
        sessionId: session.sessionId,
        data: current,
      });
      const scrubbed = step === 7
        ? { ...current, users: ((current.users as WizardData[] | undefined) ?? []).map(user => ({ ...user, password: undefined })) }
        : current;
      const nextData = { ...data, [`step${step}`]: scrubbed };
      setData(nextData);
      setSession({ ...session, currentStep: result.nextStep, data: nextData });
      setStep(Math.min(result.nextStep, 7));
    } catch (error) {
      setNotice(error instanceof Error ? error.message : "The step could not be saved.");
    } finally {
      setBusy(false);
    }
  }

  async function goLive() {
    if (!session) return;
    setBusy(true);
    setNotice("");
    try {
      await apiPost("/api/wizard/step/7/save", { sessionId: session.sessionId, data: current });
      const users = (current.users as Array<{ email?: string }> | undefined) ?? [];
      await apiPost("/api/wizard/complete", {
        sessionId: session.sessionId,
        confirmedBy: users[0]?.email ?? "",
        timestamp: new Date().toISOString(),
      });
      window.sessionStorage.removeItem("aegiscore_setup_token");
      // Activation changes how the application routes requests, so the container restarts itself a
      // few seconds after this answer (docker/client/application.cjs). Until it is answering again,
      // every request — including the first sign-in — fails. Wait for it rather than handing the
      // person a link that errors.
      setRestarting(true);
      setBusy(false);
      const ready = await waitUntilAnswering();
      setRestarting(false);
      if (!ready) {
        setRestartNote("The register has not finished restarting. It is applying the installation; wait a moment and reload this page. If it does not come back, check the container log.");
      }
      setActivated(true);
      return;
    } catch (error) {
      setNotice(error instanceof Error ? error.message : "Activation failed.");
    } finally {
      setBusy(false);
    }
  }

  if (!tokenAccepted) {
    return (
      <main id="main-content" className="wizard-loading" tabIndex={-1}>
        <ShieldCheck size={38} />
        <h1>Secure installation</h1>
        <p>Enter the setup token printed in the AegisCore container log.</p>
        <form onSubmit={unlock} className="wizard-card" style={{ width: "min(34rem, 92vw)" }}>
          <Field label="Setup token" value={token} onChange={setToken} type="password" placeholder="64-character token" />
          {notice && <p className="wizard-error" role="alert">{notice}</p>}
          <button className="wizard-primary" disabled={busy || !token.trim()}>{busy ? "Verifying..." : "Begin installation"}</button>
        </form>
      </main>
    );
  }

  if (restarting) {
    return (
      <main id="main-content" className="wizard-loading" tabIndex={-1}>
        <ShieldCheck size={38} />
        <h1>Applying the installation</h1>
        <p aria-live="polite">The register is restarting so that it comes up configured. This takes a few seconds; nothing is lost and there is nothing to do. You will be able to sign in when it finishes.</p>
      </main>
    );
  }

  if (activated) {
    return (
      <main id="main-content" className="wizard-success" tabIndex={-1}>
        <Check size={54} />
        <p>AEGISCORE IS READY</p>
        <h1>Your university instance is live.</h1>
        {restartNote
          ? <p className="wizard-error" role="alert">{restartNote}</p>
          : <p>Sign in with the administrator email and password you set in the last step. There is no other account.</p>}
        <Link className="wizard-primary" href="/">Open dashboard <ChevronRight size={18} /></Link>
      </main>
    );
  }

  return (
    <main id="main-content" className="wizard-shell" tabIndex={-1}>
      <aside className="wizard-rail">
        <div className="wizard-brand"><ShieldCheck size={24} /><strong>AegisCore</strong></div>
        <div>
          <p className="wizard-kicker">SECURE INSTALLATION</p>
          <h1>Configure your institution.</h1>
          <p className="wizard-muted">Each step is encrypted and saved. You can safely resume after a restart.</p>
        </div>
        <ol className="wizard-steps">
          {STEPS.map((label, index) => {
            const number = index + 1;
            return (
              <li key={label} className={number === step ? "active" : number < step ? "complete" : ""}>
                <i>{number < step ? <Check size={15} /> : number}</i><span>{label}</span>
              </li>
            );
          })}
        </ol>
      </aside>

      <section className="wizard-content">
        <header>
          <p className="wizard-kicker">STEP {step} OF 7</p>
          <h2>{STEPS[step - 1]}</h2>
        </header>
        {step === 1 && <IdentityStep value={current} update={update} />}
        {step === 2 && <DatabaseStep value={current} update={update} />}
        {step === 3 && <SsoStep value={current} update={update} roleNames={((data.step4?.roles as RoleConfig[] | undefined) ?? DEFAULT_ROLES).map(role => role.name).filter(Boolean)} />}
        {step === 4 && <PermissionStep value={current} update={update} />}
        {step === 5 && <AiStep value={current} update={update} />}
        {step === 6 && <ImportStep value={current} update={update} />}
        {step === 7 && <ReviewStep data={data} value={current} update={update} />}
        {notice && <p className="wizard-error" role="alert">{notice}</p>}
        <footer className="wizard-footer">
          <button className="wizard-secondary" disabled={step === 1 || busy} onClick={() => setStep(value => value - 1)}>
            <ChevronLeft size={18} /> Back
          </button>
          <button className="wizard-primary" disabled={busy || !canContinue} onClick={step === 7 ? goLive : saveAndContinue}>
            {busy ? "Saving..." : step === 7 ? "Review and go live" : <>Save and continue <ChevronRight size={18} /></>}
          </button>
        </footer>
      </section>
    </main>
  );
}

function IdentityStep({ value, update }: { value: WizardData; update: (patch: WizardData) => void }) {
  const country = text(value, "country");
  const [regions, setRegions] = useState<string[]>([]);
    // eslint-disable-next-line react-hooks/set-state-in-effect -- clear dependent region options.
  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- clear dependent region options.
    if (!country) { setRegions([]); return; }
    apiGet<{ regions: string[] }>(`/api/wizard/options/regions?country=${encodeURIComponent(country)}`)
      .then(result => setRegions(result.regions)).catch(() => setRegions([]));
  }, [country]);
  return (
    <div className="wizard-sections">
    <div className="wizard-form-grid">
      <SelectField label="License mode" value={text(value, "licenseMode", "demo")} onChange={licenseMode => update({ licenseMode })} options={[
        { value: "demo", label: "Demo" }, { value: "paid", label: "Licensed" },
      ]} />
      {text(value, "licenseMode", "demo") === "paid" && <Field label="License key" type="password" value={text(value, "licenseKey")} onChange={licenseKey => update({ licenseKey })} />}
      <Field label="University name" value={text(value, "name")} onChange={name => update({ name })} />
      <Field label="Short name" value={text(value, "shortName")} onChange={shortName => update({ shortName })} />
      <Field label="Logo URL" required={false} value={text(value, "logoUrl")} onChange={logoUrl => update({ logoUrl })} />
      <Field label="Timezone" value={text(value, "timezone", Intl.DateTimeFormat().resolvedOptions().timeZone)} onChange={timezone => update({ timezone })} />
      <Field label="Country" required={false} value={country} onChange={next => update({ country: next, region: "" })} />
      {regions.length > 0 ? (
        <SelectField label="Region" value={text(value, "region", regions[0])} onChange={region => update({ region })} options={regions.map(region => ({ value: region, label: region }))} />
      ) : <Field label="Region / state" required={false} value={text(value, "region")} onChange={region => update({ region })} />}
      <Field label="City" required={false} value={text(value, "city")} onChange={city => update({ city })} />
      <Field label="Street address" required={false} value={text(value, "street")} onChange={street => update({ street })} />
      <Field label="Primary contact" value={text(value, "contactName")} onChange={contactName => update({ contactName })} />
      <Field label="Contact email" type="email" value={text(value, "contactEmail")} onChange={contactEmail => update({ contactEmail })} />
    </div>
    <DocumentTemplateDesigner value={value} update={update} />
    </div>
  );
}

function DocumentTemplateDesigner({ value, update }: { value: WizardData; update: (patch: WizardData) => void }) {
  const [mode, setMode] = useState<"upload" | "write" | "skip">((text(value, "templateMode", "skip") as "upload" | "write" | "skip"));
  const [html, setHtml] = useState(text(value, "templateHtml", "<header><h1>{{university}}</h1></header><main><h2>Academic transcript</h2><p>{{student}}</p>{{records}}</main>"));
  const [result, setResult] = useState<TemplateResult | null>(null);
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);

  function choose(next: "upload" | "write" | "skip") {
    setMode(next); update({ templateMode: next, templateConfirmed: next === "skip" }); setMessage("");
  }
  async function upload(file: File) {
    setBusy(true); setMessage("");
    try {
      const form = new FormData(); form.append("file", file); form.append("name", file.name.replace(/\.[^.]+$/, ""));
      const next = await apiPostForm<TemplateResult>("/api/wizard/step1/template/upload", form);
      setResult(next); update({ templateMode: "upload", templateId: next.id, templateConfirmed: false });
    } catch (error) { setMessage(error instanceof Error ? error.message : "The template could not be uploaded."); }
    finally { setBusy(false); }
  }
  async function write() {
    setBusy(true); setMessage("");
    try {
      const next = await apiPost<TemplateResult>("/api/wizard/step1/template/write", { name: "University document template", rawHtml: html, logoUrl: text(value, "logoUrl") || null });
      setResult(next); update({ templateMode: "write", templateId: next.id, templateHtml: html, templateConfirmed: false });
    } catch (error) { setMessage(error instanceof Error ? error.message : "The HTML template could not be saved."); }
    finally { setBusy(false); }
  }
  async function loadDefault() {
    try { setResult(await apiGet<TemplateResult>("/api/wizard/step1/template/preview")); } catch { /* preview remains optional */ }
  }
  return (
    <section className="wizard-card wizard-template-designer">
      <div><p className="wizard-kicker">DOCUMENT TEMPLATE</p><h3>Design university documents</h3><p className="wizard-muted">Upload an existing design, write HTML, or use the AegisCore default.</p></div>
      <div className="wizard-row" role="tablist" aria-label="Document template source">
        {(["upload", "write", "skip"] as const).map(option => <button type="button" role="tab" aria-selected={mode === option} className={mode === option ? "wizard-primary" : "wizard-secondary"} key={option} onClick={() => { choose(option); if (option === "skip") void loadDefault(); }}>{option === "upload" ? "Upload file" : option === "write" ? "Write HTML" : "Use default"}</button>)}
      </div>
      {mode === "upload" && <label className="wizard-field"><span>HTML, PDF or DOCX (maximum 10 MiB)</span><input type="file" accept=".html,.htm,.pdf,.docx" disabled={busy} onChange={event => { const file = event.target.files?.[0]; if (file) void upload(file); }} /></label>}
      {mode === "write" && <><label className="wizard-field"><span>Template HTML</span><textarea rows={12} value={html} onChange={event => setHtml(event.target.value)} /></label><button type="button" className="wizard-secondary" disabled={busy || !html.trim()} onClick={write}>{busy ? "Building preview..." : "Build preview"}</button></>}
      {result && <>
        <div><strong>Extracted colours</strong><div className="wizard-swatches">{result.extractedPalette.map(color => <span key={color} title={color} style={{ background: color }}><i>{color}</i></span>)}</div></div>
        <p><strong>Fonts:</strong> {result.extractedFonts.join(", ")}</p>
        <div className="wizard-template-compare"><section><h4>Your template</h4><iframe title="Your document template preview" sandbox="" srcDoc={result.previewHtml} /></section><section><h4>Default AegisCore</h4><iframe title="Default AegisCore document preview" sandbox="" srcDoc={'<main style="font-family:system-ui;padding:24px"><h1>Example University</h1><h2>Academic transcript</h2><p>Student: Sample Learner</p><table><tr><th>Course</th><th>Grade</th></tr><tr><td>Systems Design</td><td>A</td></tr></table></main>'} /></section></div>
        <button type="button" className="wizard-primary" onClick={() => { update({ templateConfirmed: true }); setMessage("Document template confirmed."); }}>Confirm template</button>
      </>}
      {mode === "skip" && <p>AegisCore&apos;s accessible default transcript and invoice layout will be used.</p>}
      {message && <p aria-live="polite">{message}</p>}
    </section>
  );
}

function DatabaseStep({ value, update }: { value: WizardData; update: (patch: WizardData) => void }) {
  const [testing, setTesting] = useState(false);
  const [message, setMessage] = useState("");
  async function testConnection() {
    setTesting(true); setMessage("");
    try {
      const result = await apiPost<DatabaseResult>("/api/wizard/step2/test", {
        dialect: text(value, "dialect", "postgres"), host: text(value, "host"), port: Number(value.port || 5432),
        databaseName: text(value, "databaseName"), username: text(value, "username"), password: text(value, "password"), sslEnabled: value.sslEnabled !== false,
      });
      update({ connectionTested: result.success });
      setMessage(result.message);
    } catch (error) {
      update({ connectionTested: false });
      setMessage(error instanceof Error ? error.message : "Database test failed.");
    } finally { setTesting(false); }
  }
  return (
    <div className="wizard-sections">
      <div className="wizard-form-grid">
        <SelectField label="Database" value={text(value, "dialect", "postgres")} onChange={dialect => update({ dialect, port: ({ postgres: 5432, mysql: 3306, mssql: 1433, oracle: 1521 } as Record<string, number>)[dialect], connectionTested: false })} options={[{ value: "postgres", label: "PostgreSQL" }, { value: "mysql", label: "MySQL / MariaDB" }, { value: "mssql", label: "Microsoft SQL Server" }, { value: "oracle", label: "Oracle Database" }]} />
        <Field label="Host" value={text(value, "host")} onChange={host => update({ host, connectionTested: false })} />
        <Field label="Port" value={String(value.port ?? 5432)} onChange={port => update({ port: Number(port), connectionTested: false })} type="number" />
        <Field label="Database name" value={text(value, "databaseName")} onChange={databaseName => update({ databaseName, connectionTested: false })} />
        <Field label="Username" value={text(value, "username")} onChange={username => update({ username, connectionTested: false })} />
        <Field label="Password" value={text(value, "password")} onChange={password => update({ password, connectionTested: false })} type="password" />
        <label className="wizard-check"><input type="checkbox" checked={value.sslEnabled !== false} onChange={event => update({ sslEnabled: event.target.checked, connectionTested: false })} />Require database TLS</label>
      </div>
      <button className="wizard-secondary" onClick={testConnection} disabled={testing}>{testing ? "Testing..." : "Connect, test and migrate"}</button>
      {message && <p aria-live="polite">{message}</p>}
    </div>
  );
}

function SsoStep({ value, update, roleNames }: { value: WizardData; update: (patch: WizardData) => void; roleNames: string[] }) {
  const providers = ssoProviders(value);
  const [redirectUri, setRedirectUri] = useState("");
  const [busyProvider, setBusyProvider] = useState("");
  const [messages, setMessages] = useState<Record<string, string>>({});
  const [loaded, setLoaded] = useState(false);

  const setProviders = (next: SsoProviderEntry[]) => update({ providers: next });
  const patch = (type: string, changes: Partial<SsoProviderEntry>) =>
    setProviders(providers.map(entry => entry.provider === type ? { ...entry, ...changes, connectionTested: type === "local" } : entry));
  const say = (type: string, message: string) => setMessages(existing => ({ ...existing, [type]: message }));

  async function enable(type: string) {
    if (providers.some(entry => entry.provider === type)) return;
    const entry: SsoProviderEntry = { provider: type, connectionTested: type === "local", hasSecret: false, scopes: type === "okta" ? "openid profile email groups" : "openid profile email" };
    setProviders([...providers, entry]);
    if (type === "local") {
      try { await apiPost("/api/wizard/step3/configure", { provider: "local" }); }
      catch (error) { say("local", error instanceof Error ? error.message : "Could not enable password sign-in."); }
    }
  }

  async function disable(type: string) {
    setProviders(providers.filter(entry => entry.provider !== type));
    try { await apiPost("/api/wizard/step3/remove", { provider: type }); }
    catch (error) { say(type, error instanceof Error ? error.message : "Could not remove this sign-in method."); }
  }

  async function saveAndTest(entry: SsoProviderEntry) {
    setBusyProvider(entry.provider); say(entry.provider, "");
    try {
      await apiPost("/api/wizard/step3/configure", {
        provider: entry.provider, tenantId: entry.tenantId, clientId: entry.clientId, clientSecret: entry.clientSecret || undefined,
        domain: entry.domain, authority: entry.authority, scopes: entry.scopes, defaultRole: entry.defaultRole,
        groupRoleMapping: Object.fromEntries(Object.entries(entry.groupRoleMapping ?? {}).filter(([group, role]) => group.trim() && role.trim())),
      });
      const result = await apiPost<HandshakeResult>("/api/wizard/step3/test", { provider: entry.provider });
      setProviders(providers.map(item => item.provider === entry.provider
        ? { ...item, clientSecret: "", hasSecret: item.hasSecret || Boolean(entry.clientSecret), connectionTested: result.success }
        : item));
      say(entry.provider, result.success
        ? `Connected. People will be sent to ${result.loginUrl ?? "the provider"} to sign in. ${result.fixHint ?? ""}`
        : `${result.error ?? "The connection test failed."} ${result.fixHint ?? ""}`);
    } catch (error) {
      setProviders(providers.map(item => item.provider === entry.provider ? { ...item, connectionTested: false } : item));
      say(entry.provider, error instanceof Error ? error.message : "The connection test failed.");
    } finally { setBusyProvider(""); }
  }

  // The server's list is the one that gets saved — it holds what was actually tested and where each
  // secret lives — so the screen starts from it rather than from whatever the browser remembered.
  useEffect(() => {
    if (loaded) return;
    apiGet<{ redirectUri: string; providers: SsoProviderEntry[] }>("/api/wizard/step3/providers")
      .then(result => {
        setRedirectUri(result.redirectUri);
        if (result.providers.length > 0) update({ providers: result.providers });
        else void enable("local");
      })
      .catch(() => { /* the step still works; the list is empty until a method is enabled */ })
      .finally(() => setLoaded(true));
    // eslint-disable-next-line react-hooks/exhaustive-deps -- runs once on mount
  }, []);

  const localEnabled = providers.some(entry => entry.provider === "local");
  const roleOptions = Array.from(new Set([...roleNames, ...DEFAULT_ROLES.map(role => role.name)]));

  return (
    <div className="wizard-sections">
      <h3>How will people sign in?</h3>
      <p className="wizard-muted">Choose one or more. Each provider is tested against its live discovery document before you can continue, and its client secret is kept in the installation&rsquo;s secret vault, never in this page.</p>
      {redirectUri && (
        <div className="wizard-card" style={{ cursor: "default" }}>
          <strong>Redirect URI to register at each provider</strong>
          <code style={{ userSelect: "all", wordBreak: "break-all" }}>{redirectUri}</code>
          <p>Paste this exactly into the app registration (Entra: Authentication, Web redirect URI. Google: Authorised redirect URIs. Okta: Sign-in redirect URIs).</p>
        </div>
      )}
      <div className="wizard-form-grid">
        {SSO_METHODS.map(method => {
          const enabled = providers.some(entry => entry.provider === method.type);
          return (
            <label key={method.type} className="check-row" style={{ alignItems: "flex-start" }}>
              <input type="checkbox" checked={enabled} onChange={event => { if (event.target.checked) void enable(method.type); else void disable(method.type); }} />
              <span><strong>{method.label}</strong><br /><small className="wizard-muted">{method.note}</small></span>
            </label>
          );
        })}
      </div>
      {!localEnabled && providers.length > 0 && (
        <p className="wizard-error" role="alert">Password sign-in is off. The administrator account you set in the last step will not be able to sign in with its password; map the administrator&rsquo;s group to the Admin role below, or keep &ldquo;Email and password&rdquo; on.</p>
      )}
      {messages.local && <p className="wizard-error" role="alert">{messages.local}</p>}
      {providers.filter(entry => entry.provider !== "local").map(entry => (
        <SsoProviderCard key={entry.provider} entry={entry} roleOptions={roleOptions} busy={busyProvider === entry.provider}
          message={messages[entry.provider] ?? ""} patch={changes => patch(entry.provider, changes)} test={() => saveAndTest(entry)} />
      ))}
    </div>
  );
}

function SsoProviderCard({ entry, roleOptions, busy, message, patch, test }: {
  entry: SsoProviderEntry; roleOptions: string[]; busy: boolean; message: string;
  patch: (changes: Partial<SsoProviderEntry>) => void; test: () => void;
}) {
  const label = SSO_METHODS.find(method => method.type === entry.provider)?.label ?? entry.provider;
  const mapping = Object.entries(entry.groupRoleMapping ?? {});
  const setMapping = (rows: Array<[string, string]>) => patch({ groupRoleMapping: Object.fromEntries(rows) });
  const hint = entry.provider === "entra"
    ? "A group's object ID or display name, as it appears in the ID token's groups claim (or an app role value)."
    : entry.provider === "google"
      ? "A group's email address or name from the Workspace directory (needs the admin.directory.group.readonly scope)."
      : "A group name as your Okta authorization server puts it in the groups claim.";
  return (
    <section className="wizard-card" style={{ cursor: "default" }}>
      <div className="wizard-row"><h3>{label}</h3>{entry.connectionTested && <small><Check size={14} /> Connection tested</small>}</div>
      <div className="wizard-form-grid">
        {entry.provider === "entra" && <>
          <Field label="Directory (tenant) ID" value={entry.tenantId ?? ""} onChange={tenantId => patch({ tenantId })} placeholder="00000000-0000-0000-0000-000000000000" />
          <Field label="Application (client) ID" value={entry.clientId ?? ""} onChange={clientId => patch({ clientId })} />
        </>}
        {entry.provider === "google" && <>
          <Field label="OAuth client ID" value={entry.clientId ?? ""} onChange={clientId => patch({ clientId })} placeholder="....apps.googleusercontent.com" />
          <Field label="Workspace domain" value={entry.domain ?? ""} onChange={domain => patch({ domain })} placeholder="university.edu.pk" />
        </>}
        {entry.provider === "okta" && <>
          <Field label="Okta org URL (authority)" value={entry.authority ?? ""} onChange={authority => patch({ authority })} placeholder="https://your-org.okta.com/oauth2/default" />
          <Field label="Client ID" value={entry.clientId ?? ""} onChange={clientId => patch({ clientId })} />
        </>}
        <Field label={entry.hasSecret ? "Client secret (stored; enter to replace)" : "Client secret"} value={entry.clientSecret ?? ""} onChange={clientSecret => patch({ clientSecret })} type="password" required={!entry.hasSecret} />
        <Field label="Scopes" value={entry.scopes ?? ""} onChange={scopes => patch({ scopes })} required={false} placeholder="openid profile email" />
        <label className="wizard-field">
          <span>Default role (for people in no mapped group)</span>
          <input list="sso-role-options" value={entry.defaultRole ?? ""} onChange={event => patch({ defaultRole: event.target.value })} placeholder="e.g. Faculty" />
        </label>
      </div>
      <h4>Group to role mapping</h4>
      <p className="wizard-muted">{hint} Roles are the ones you define in the next step.</p>
      {mapping.map(([group, role], index) => (
        <div className="wizard-row" key={index}>
          <Field label="Provider group" value={group} onChange={next => setMapping(mapping.map((row, i) => i === index ? [next, row[1]] : row))} />
          <label className="wizard-field"><span>AegisCore role *</span>
            <input list="sso-role-options" value={role} onChange={event => setMapping(mapping.map((row, i) => i === index ? [row[0], event.target.value] : row))} />
          </label>
          <button className="icon-button" type="button" aria-label="Remove mapping" onClick={() => setMapping(mapping.filter((_, i) => i !== index))}><Trash2 size={16} /></button>
        </div>
      ))}
      <datalist id="sso-role-options">{roleOptions.map(role => <option key={role} value={role} />)}</datalist>
      <button className="link-button" type="button" onClick={() => setMapping([...mapping, ["", ""]])}><Plus size={16} /> Add mapping</button>
      <button className="wizard-secondary" type="button" disabled={busy} onClick={test}>{busy ? "Testing..." : "Save and test connection"}</button>
      {message && <p aria-live="polite">{message}</p>}
    </section>
  );
}

function PermissionStep({ value, update }: { value: WizardData; update: (patch: WizardData) => void }) {
  const roles = (value.roles as RoleConfig[] | undefined) ?? DEFAULT_ROLES;
  const replace = (next: RoleConfig[]) => update({ roles: next });
  return (
    <div className="wizard-sections">
      <h3>What roles exist at your institution?</h3>
      {roles.map((role, roleIndex) => (
        <section className="wizard-card" key={roleIndex}>
          <div className="wizard-row">
            <Field label="Role name" value={role.name} onChange={name => replace(roles.map((item, index) => index === roleIndex ? { ...item, name } : item))} />
            <button className="icon-button" aria-label={`Remove ${role.name}`} onClick={() => replace(roles.filter((_, index) => index !== roleIndex))}><Trash2 size={16} /></button>
          </div>
          <div style={{ overflowX: "auto" }}>
            <table><thead><tr><th scope="col">Module</th>{ACTIONS.map(action => <th scope="col" key={action}>{action}</th>)}</tr></thead>
              <tbody>{MODULES.map(module => <tr key={module}><th scope="row">{module}</th>{ACTIONS.map(action => {
                const key = `${module}.${action}`; const checked = role.permissions.includes(key);
                return <td key={action}><input aria-label={`${role.name} ${key}`} type="checkbox" checked={checked} onChange={() => replace(roles.map((item, index) => index !== roleIndex ? item : { ...item, permissions: checked ? item.permissions.filter(permission => permission !== key) : [...item.permissions, key] }))} /></td>;
              })}</tr>)}</tbody>
            </table>
          </div>
        </section>
      ))}
      <button className="link-button" onClick={() => replace([...roles, { name: "", permissions: [] }])}><Plus size={16} /> Add role</button>
    </div>
  );
}

function AiStep({ value, update }: { value: WizardData; update: (patch: WizardData) => void }) {
  const provider = text(value, "provider", "ollama-local");
  const [testing, setTesting] = useState(false);
  const [message, setMessage] = useState("");
  const defaultModel = provider === "ollama-local" ? "mistral" : provider === "openai" ? "gpt-4o-mini" : provider === "granite" ? "ibm/granite-13b-chat-v2" : "";
  async function testConnection() {
    setTesting(true); setMessage(""); update({ connectionTested: false });
    try {
      await apiPost("/api/wizard/step5/configure", { provider, model: text(value, "model", defaultModel), endpoint: text(value, "endpoint") || null, apiKey: text(value, "apiKey") || null });
      const result = await apiPost<AiConnectionResult>("/api/wizard/step5/test");
      update({ connectionTested: result.success, apiKey: "" });
      setMessage(result.success ? `Model responded — ${provider} connected` : `${result.error ?? "Model test failed."} ${result.fixHint ?? ""}`);
    } catch (error) { setMessage(error instanceof Error ? error.message : "AI model test failed."); }
    finally { setTesting(false); }
  }
  return (
    <div className="wizard-sections"><div className="wizard-form-grid">
      <SelectField label="AI provider" value={provider} onChange={next => update({ provider: next, model: next === "ollama-local" ? "mistral" : next === "openai" ? "gpt-4o-mini" : next === "granite" ? "ibm/granite-13b-chat-v2" : "", connectionTested: false })} options={[
        { value: "disabled", label: "Disabled — configure later" },
        { value: "ollama-local", label: "Ollama Local" }, { value: "ollama-cloud", label: "Ollama Cloud" }, { value: "openai", label: "OpenAI" },
        { value: "granite", label: "IBM Granite" }, { value: "custom", label: "Custom endpoint" },
      ]} />
      {provider !== "disabled" && <Field label="Model" value={text(value, "model", defaultModel)} onChange={model => update({ model, connectionTested: false })} />}
      {(provider === "ollama-cloud" || provider === "custom") && <Field label="Endpoint URL" value={text(value, "endpoint")} onChange={endpoint => update({ endpoint, connectionTested: false })} />}
      {(provider === "openai" || provider === "granite" || provider === "custom" || provider === "ollama-cloud") && <Field label={provider === "granite" ? "IBM IAM API key" : "API key"} required={provider !== "custom"} value={text(value, "apiKey")} onChange={apiKey => update({ apiKey, connectionTested: false })} type="password" />}
    </div>{provider === "disabled" ? <p>AI features will remain unavailable until a provider is configured. No model connection test is claimed.</p> : <button className="wizard-secondary" type="button" disabled={testing} onClick={testConnection}>{testing ? "Testing..." : "Test Connection"}</button>}{message && <p aria-live="polite">{message}</p>}</div>
  );
}

function ImportStep({ value, update }: { value: WizardData; update: (patch: WizardData) => void }) {
  const [results, setResults] = useState<Record<string, ImportResult>>({});
  async function upload(fileType: string, file: File) {
    const form = new FormData(); form.append("file", file);
    const result = await apiPostForm<ImportResult>(`/api/wizard/step6/import/${fileType}/validate`, form);
    const next = { ...results, [fileType]: result }; setResults(next);
    update({ skipImport: false, validatedFiles: Object.entries(next).filter(([, item]) => item.isValid).map(([name]) => name) });
  }
  return (
    <div className="wizard-sections">
      <p>Upload students, faculty, and courses as CSV or XLSX. Validation runs before anything is imported.</p>
      {(["students", "faculty", "courses"] as const).map(fileType => (
        <label className="wizard-card" key={fileType}>
          <strong>{fileType[0].toUpperCase() + fileType.slice(1)}</strong>
          <span><Upload size={16} /> Choose CSV or XLSX</span>
          <input type="file" accept=".csv,.xlsx" onChange={event => event.target.files?.[0] && void upload(fileType, event.target.files[0])} />
          {results[fileType] && <small>{results[fileType].isValid ? `${results[fileType].totalRows} rows validated` : results[fileType].errors.join(" ")}</small>}
        </label>
      ))}
      <label className="wizard-check"><input type="checkbox" checked={value.skipImport === true} onChange={event => update({ skipImport: event.target.checked, validatedFiles: [] })} /> Skip data import for now</label>
    </div>
  );
}

function ReviewStep({ data, value, update }: { data: Record<string, WizardData>; value: WizardData; update: (patch: WizardData) => void }) {
  const configuredRoles = ((data.step4?.roles as RoleConfig[] | undefined) ?? DEFAULT_ROLES).filter(role => role.name.trim());
  const defaultRole = configuredRoles.find(role => role.permissions.includes("System.Write"))?.name ?? configuredRoles[0]?.name ?? "Admin";
  const users = (value.users as Array<{ name: string; email: string; password: string; role: string }> | undefined) ?? [
    { name: "", email: "", password: "", role: defaultRole },
  ];
  return (
    <div className="wizard-sections">
      <div className="wizard-review-grid">{STEPS.slice(0, 6).map((label, index) => <article key={label}><span>STEP {index + 1}</span><h3>{label}</h3><p>{Object.keys(data[`step${index + 1}`] ?? {}).length} configured fields</p></article>)}</div>
      <h3>Initial administrators</h3>
      {users.map((user, index) => <div className="wizard-row" key={index}>
        <Field label={`${user.role} name`} value={user.name} onChange={name => update({ users: users.map((item, i) => i === index ? { ...item, name } : item) })} />
        <Field label={`${user.role} email`} type="email" value={user.email} onChange={email => update({ users: users.map((item, i) => i === index ? { ...item, email } : item) })} />
        <Field label={`${user.role} password`} type="password" value={user.password} onChange={password => update({ users: users.map((item, i) => i === index ? { ...item, password } : item) })} />
        <label className="wizard-field"><span>Role *</span><select value={user.role} onChange={event => update({ users: users.map((item, i) => i === index ? { ...item, role: event.target.value } : item) })}>{configuredRoles.map(role => <option key={role.name} value={role.name}>{role.name}</option>)}</select></label>
        {users.length > 1 && <button type="button" className="icon-button" aria-label={`Remove administrator ${index + 1}`} onClick={() => update({ users: users.filter((_, i) => i !== index) })}><Trash2 size={16} /></button>}
      </div>)}
      <button type="button" className="link-button" onClick={() => update({ users: [...users, { name: "", email: "", password: "", role: defaultRole }] })}><Plus size={16} /> Add administrator</button>
      <label className="wizard-check"><input type="checkbox" checked={value.confirmed === true} onChange={event => update({ confirmed: event.target.checked })} /> I confirm this configuration and am authorised to activate this installation.</label>
    </div>
  );
}
