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

const MODULES = ["Students", "Grades", "Attendance", "Finance", "Timetable", "Admissions", "Reports", "Settings", "System"];
const ACTIONS = ["Read", "Write", "Delete", "Approve", "Export"];
const DEFAULT_ROLES: RoleConfig[] = [
  { name: "Admin", permissions: MODULES.flatMap(module => ACTIONS.map(action => `${module}.${action}`)) },
  { name: "Faculty", permissions: ["Students.Read", "Grades.Read", "Grades.Write", "Attendance.Read", "Attendance.Write"] },
  { name: "Student", permissions: ["Grades.Read", "Attendance.Read", "Timetable.Read"] },
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

export function WizardShell() {
  const [token, setToken] = useState("");
  const [tokenAccepted, setTokenAccepted] = useState(false);
  const [session, setSession] = useState<Session | null>(null);
  const [step, setStep] = useState(1);
  const [data, setData] = useState<Record<string, WizardData>>({});
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState("");
  const [activated, setActivated] = useState(false);

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

  const current = useMemo(() => data[`step${step}`] ?? {}, [data, step]);
  const canContinue = (step !== 3 || text(current, "provider", "local") === "local" || current.connectionTested === true)
    && (step !== 5 || current.connectionTested === true);
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
      setActivated(true);
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

  if (activated) {
    return (
      <main id="main-content" className="wizard-success" tabIndex={-1}>
        <Check size={54} />
        <p>AEGISCORE IS READY</p>
        <h1>Your university instance is live.</h1>
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
        {step === 3 && <SsoStep value={current} update={update} />}
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
        host: text(value, "host"), port: Number(value.port || 5432), databaseName: text(value, "databaseName"),
        username: text(value, "username"), password: text(value, "password"), sslEnabled: value.sslEnabled !== false,
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
        <SelectField label="Database" value={text(value, "dialect", "postgres")} onChange={dialect => update({ dialect, connectionTested: false })} options={[{ value: "postgres", label: "PostgreSQL" }]} />
        <Field label="Host" value={text(value, "host")} onChange={host => update({ host, connectionTested: false })} />
        <Field label="Port" value={String(value.port ?? 5432)} onChange={port => update({ port: Number(port), connectionTested: false })} type="number" />
        <Field label="Database name" value={text(value, "databaseName")} onChange={databaseName => update({ databaseName, connectionTested: false })} />
        <Field label="Username" value={text(value, "username")} onChange={username => update({ username, connectionTested: false })} />
        <Field label="Password" value={text(value, "password")} onChange={password => update({ password, connectionTested: false })} type="password" />
      </div>
      <button className="wizard-secondary" onClick={testConnection} disabled={testing}>{testing ? "Testing..." : "Connect, test and migrate"}</button>
      {message && <p aria-live="polite">{message}</p>}
    </div>
  );
}

function SsoStep({ value, update }: { value: WizardData; update: (patch: WizardData) => void }) {
  const provider = text(value, "provider", "local");
  const [testing, setTesting] = useState(false);
  const [message, setMessage] = useState("");
  async function testConnection() {
    setTesting(true); setMessage(""); update({ connectionTested: false });
    try {
      await apiPost("/api/wizard/step3/configure", {
        provider, tenantId: text(value, "tenantId"), clientId: text(value, "clientId"), clientSecret: text(value, "clientSecret"),
        domain: text(value, "domain"), metadataUrl: text(value, "metadataUrl"), metadataXml: text(value, "metadataXml"),
        entityId: text(value, "entityId"), acsUrl: text(value, "acsUrl"),
      });
      const result = await apiPost<HandshakeResult>("/api/wizard/step3/test");
      update({ connectionTested: result.success, clientSecret: "" });
      setMessage(result.success ? `SSO handshake successful — ${provider} connected` : `${result.error ?? "Handshake failed."} ${result.fixHint ?? ""}`);
    } catch (error) { setMessage(error instanceof Error ? error.message : "SSO handshake failed."); }
    finally { setTesting(false); }
  }
  return (
    <div className="wizard-sections"><div className="wizard-form-grid">
      <SelectField label="SSO provider" value={provider} onChange={next => update({ provider: next, connectionTested: next === "local" })} options={[
        { value: "local", label: "Local email and password" }, { value: "entra", label: "Microsoft Entra ID" },
        { value: "google", label: "Google Workspace" }, { value: "saml", label: "SAML 2.0" },
      ]} />
      {provider === "entra" && <><Field label="Tenant ID" value={text(value, "tenantId")} onChange={tenantId => update({ tenantId, connectionTested: false })} /><Field label="Client ID" value={text(value, "clientId")} onChange={clientId => update({ clientId, connectionTested: false })} /><Field label="Client secret" value={text(value, "clientSecret")} onChange={clientSecret => update({ clientSecret, connectionTested: false })} type="password" /></>}
      {provider === "google" && <><Field label="Client ID" value={text(value, "clientId")} onChange={clientId => update({ clientId, connectionTested: false })} /><Field label="Client secret" value={text(value, "clientSecret")} onChange={clientSecret => update({ clientSecret, connectionTested: false })} type="password" /><Field label="Workspace domain" value={text(value, "domain")} onChange={domain => update({ domain, connectionTested: false })} /></>}
      {provider === "saml" && <><Field label="Metadata URL" required={false} value={text(value, "metadataUrl")} onChange={metadataUrl => update({ metadataUrl, connectionTested: false })} /><label className="wizard-field"><span>Or metadata XML</span><textarea rows={6} value={text(value, "metadataXml")} onChange={event => update({ metadataXml: event.target.value, connectionTested: false })} /></label><label className="wizard-field"><span>Upload metadata XML</span><input type="file" accept=".xml,text/xml,application/xml" onChange={event => { const file = event.target.files?.[0]; if (file) void file.text().then(metadataXml => update({ metadataXml, metadataUrl: "", connectionTested: false })); }} /></label><Field label="Entity ID" value={text(value, "entityId")} onChange={entityId => update({ entityId, connectionTested: false })} /><Field label="ACS URL" value={text(value, "acsUrl")} onChange={acsUrl => update({ acsUrl, connectionTested: false })} /></>}
    </div><button className="wizard-secondary" type="button" disabled={testing} onClick={testConnection}>{testing ? "Testing..." : "Test Connection"}</button>{message && <p aria-live="polite">{message}</p>}</div>
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
        { value: "ollama-local", label: "Ollama Local" }, { value: "ollama-cloud", label: "Ollama Cloud" }, { value: "openai", label: "OpenAI" },
        { value: "granite", label: "IBM Granite" }, { value: "custom", label: "Custom endpoint" },
      ]} />
      <Field label="Model" value={text(value, "model", defaultModel)} onChange={model => update({ model, connectionTested: false })} />
      {(provider === "ollama-cloud" || provider === "custom") && <Field label="Endpoint URL" value={text(value, "endpoint")} onChange={endpoint => update({ endpoint, connectionTested: false })} />}
      {(provider === "openai" || provider === "granite" || provider === "custom" || provider === "ollama-cloud") && <Field label={provider === "granite" ? "IBM IAM API key" : "API key"} required={provider !== "custom"} value={text(value, "apiKey")} onChange={apiKey => update({ apiKey, connectionTested: false })} type="password" />}
    </div><button className="wizard-secondary" type="button" disabled={testing} onClick={testConnection}>{testing ? "Testing..." : "Test Connection"}</button>{message && <p aria-live="polite">{message}</p>}</div>
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
