"use client";

import { Check, ChevronLeft, ChevronRight, Circle, LockKeyhole, Plus, ShieldCheck, Trash2 } from "lucide-react";
import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";

type Mode = "demo" | "paid";
type Row = { name: string; code: string };
type User = { name: string; email: string; role: "Super Admin" | "Finance Controller"; password: string };
type WizardData = Record<string, unknown>;
type Session = { sessionId: string; currentStep: number; licenseMode: Mode; data: Record<string, WizardData> };

const steps = ["License plan", "University information", "Academic structure", "Payment gateways", "Finance configuration", "Fee structure", "Admin accounts", "Review & activate"];
const gateways = ["Raast RTP", "1Link", "JazzCash Merchant", "EasyPaisa", "HBL Pay", "MCB Corporate", "UBL Omni", "Stripe", "Network International", "Telr", "GoCardless", "Plaid ACH", "Billplz FPX", "DuitNow QR", "SWIFT MT103"];

export function WizardShell() {
  const [session, setSession] = useState<Session | null>(null);
  const [step, setStep] = useState(0);
  const [data, setData] = useState<Record<string, WizardData>>({});
  const [busy, setBusy] = useState(true);
  const [notice, setNotice] = useState("");
  const [activated, setActivated] = useState(false);

  useEffect(() => {
    apiGet<Session>("/api/wizard/session").then(value => {
      setSession(value);
      setData(value.data ?? {});
      setStep(Math.min(value.currentStep, 7));
    }).catch(() => {
      const saved = localStorage.getItem("aegiscore_wizard_session");
      if (saved) {
        const value = JSON.parse(saved) as Session;
        setSession(value);
        setData(value.data ?? {});
        setStep(Math.min(value.currentStep, 7));
      }
    }).finally(() => setBusy(false));
  }, []);

  const current = useMemo(() => data[`step${step}`] ?? {}, [data, step]);
  const update = (patch: WizardData) => setData(value => ({ ...value, [`step${step}`]: { ...value[`step${step}`], ...patch } }));
  const valid = useMemo(() => isValid(step, current), [step, current]);

  async function changeMode(mode: Mode, licenseKey?: string) {
    if (!session) return;
    setBusy(true); setNotice("");
    try {
      const result = await apiPost<{ success: boolean; mode: Mode; currentStep: number; data: Record<string, WizardData> }>("/api/wizard/mode", { sessionId: session.sessionId, mode, licenseKey });
      const nextSession = { ...session, currentStep: result.currentStep, licenseMode: result.mode, data: result.data };
      setSession(nextSession); setData(result.data); setStep(Math.min(result.currentStep, 7));
      localStorage.setItem("aegiscore_wizard_session", JSON.stringify(nextSession));
    } catch (error) { setNotice(error instanceof Error ? error.message : "The license mode could not be changed."); }
    finally { setBusy(false); }
  }

  async function next() {
    if (!session || !valid) return;
    setBusy(true); setNotice("");
    try {
      const result = await apiPost<{ success: boolean; nextStep: number }>(`/api/wizard/step/${step}/save`, { sessionId: session.sessionId, data: current });
      const nextStep = Math.min(result.nextStep, 7);
      const storedData = step === 6
        ? { ...data, step6: { ...current, users: Array.isArray(current.users) ? (current.users as User[]).map(user => ({ name: user.name, email: user.email, role: user.role })) : [] } }
        : { ...data };
      const nextSession = { ...session, currentStep: nextStep, licenseMode: (data.step0?.mode as Mode) ?? session.licenseMode, data: storedData };
      setSession(nextSession); setStep(nextStep); localStorage.setItem("aegiscore_wizard_session", JSON.stringify(nextSession));
    } catch (error) { setNotice(error instanceof Error ? error.message : "The step could not be saved."); }
    finally { setBusy(false); }
  }

  async function activate() {
    if (!session || !valid) return;
    setBusy(true); setNotice("");
    try {
      await apiPost("/api/wizard/step/7/save", { sessionId: session.sessionId, data: current });
      await apiPost("/api/wizard/complete", { sessionId: session.sessionId, confirmedBy: String((data.step6?.users as User[])[0]?.email ?? ""), timestamp: new Date().toISOString() });
      localStorage.removeItem("aegiscore_wizard_session"); setActivated(true);
    } catch (error) { setNotice(error instanceof Error ? error.message : "Activation failed."); }
    finally { setBusy(false); }
  }

  if (busy && !session) return <main className="wizard-loading"><ShieldCheck size={34} /><p>Loading your secure setup session...</p></main>;
  if (activated) return <main className="wizard-success"><Check size={54} /><p>AEGISCORE IS READY</p><h1>Your university instance is live.</h1><Link className="wizard-primary" href="/">Open dashboard <ChevronRight size={18} /></Link></main>;

  return <main className="wizard-shell">
    <aside className="wizard-rail">
      <div className="wizard-brand"><span><ShieldCheck size={24} /></span><strong>AegisCore</strong></div>
      <div><p className="wizard-kicker">UNIVERSITY ERP</p><h1>Configure your institution.</h1><p className="wizard-muted">Every step is saved securely. You can return to this wizard at any time before activation.</p></div>
      <ol className="wizard-steps">{steps.map((label, index) => <li key={label} className={index === step ? "active" : index < step ? "complete" : ""}><i>{index < step ? <Check size={15} /> : index === step ? <Circle size={10} fill="currentColor" /> : index + 1}</i><span>{label}</span></li>)}</ol>
      <p className="wizard-help">Need help? <a href="mailto:admin@aegiscore.pk">admin@aegiscore.pk</a></p>
    </aside>
    <section className="wizard-content">
      {((data.step0?.mode as Mode) ?? session?.licenseMode) === "demo" && step > 0 && <div className="wizard-demo-pill">DEMO MODE <span>30 days</span></div>}
      <header><p className="wizard-kicker">STEP {step}</p><h2>{steps[step]}</h2><p className="wizard-muted">{stepDescription(step)}</p></header>
      {step === 0 && <LicenseStep value={current} update={update} changeMode={changeMode} busy={busy} />}
      {step === 1 && <UniversityStep value={current} update={update} />}
      {step === 2 && <AcademicStep value={current} update={update} />}
      {step === 3 && <GatewayStep value={current} update={update} mode={(data.step0?.mode as Mode) ?? "demo"} />}
      {step === 4 && <FinanceStep value={current} update={update} />}
      {step === 5 && <FeesStep value={current} update={update} />}
      {step === 6 && <UsersStep value={current} update={update} />}
      {step === 7 && <ReviewStep data={data} value={current} update={update} />}
      {notice && <p className="wizard-error">{notice}</p>}
      <footer className="wizard-footer"><button className="wizard-secondary" disabled={step === 0 || busy} onClick={() => setStep(value => value - 1)}><ChevronLeft size={17} /> Back</button>{step === 7 ? <button className="wizard-primary" disabled={!valid || busy} onClick={activate}>{busy ? "Activating..." : "Activate instance"} <ChevronRight size={17} /></button> : <button className="wizard-primary" disabled={!valid || busy} onClick={next}>{busy ? "Saving..." : "Next"} <ChevronRight size={17} /></button>}</footer>
    </section>
  </main>;
}

function stepDescription(step: number) { return ["Choose the license mode for this tenant.", "Tell us about the university and its primary contact.", "Build the academic hierarchy and calendar.", "Connect payment accounts or continue in sandbox mode.", "Set the accounting and reporting baseline.", "Define fee heads and payment defaults.", "Create the first real administrators.", "Confirm the setup and seal the tenant."][step]; }
function text(value: WizardData, key: string) { return typeof value[key] === "string" ? String(value[key]) : ""; }
function Field({ label, value, onChange, type = "text", required = true }: { label: string; value: string; onChange: (value: string) => void; type?: string; required?: boolean }) { return <label className="wizard-field"><span>{label}{required ? " *" : ""}</span><input type={type} value={value} onChange={event => onChange(event.target.value)} /></label>; }
function LicenseStep({ value, update, changeMode, busy }: { value: WizardData; update: (value: WizardData) => void; changeMode: (mode: Mode, licenseKey?: string) => Promise<void>; busy: boolean }) {
  const selectedMode = value.mode as Mode | undefined;
  const mode = selectedMode ?? "demo";
  return <div className="wizard-grid two">
    <div className={`wizard-card ${mode === "demo" ? "selected" : ""}`}><b>FREE</b><h3>Demo</h3><strong>PKR 0 / 30 days</strong><p>Full access to all modules with sandbox payments.</p><small>Real money never moves in demo mode.</small><button className="wizard-mode-switch" type="button" disabled={busy || selectedMode === "demo"} onClick={() => void changeMode("demo")}><span aria-hidden="true" /> {selectedMode === "demo" ? "Demo mode selected" : mode === "paid" ? "Switch to demo mode" : "Select demo mode"}</button></div>
    <div className={`wizard-card ${mode === "paid" ? "selected" : ""}`}><b>RECOMMENDED</b><h3>Live / Licensed</h3><strong>Contact Sales</strong><p>Unlimited capacity and real gateway integrations.</p>
      <Field label="License key" value={text(value, "licenseKey")} onChange={licenseKey => update({ licenseKey, licenseValidated: false })} required={false} />
      <button className="wizard-mode-switch" type="button" role="switch" aria-checked={mode === "paid"} disabled={busy || (mode === "demo" && !text(value, "licenseKey").trim())} onClick={() => void changeMode(mode === "paid" ? "demo" : "paid", text(value, "licenseKey").trim() || undefined)}>
        <span aria-hidden="true" /> {mode === "paid" ? "Live mode enabled" : "Switch to live mode"}
      </button>
      {mode === "paid" && <p className="wizard-success-text">Live mode is server-validated. Real gateways can be configured in Step 3.</p>}
    </div>
  </div>;
}
function UniversityStep({ value, update }: { value: WizardData; update: (value: WizardData) => void }) { const fields = [["name", "University full name"], ["shortName", "Short name / acronym"], ["type", "University type"], ["country", "Country"], ["city", "City"], ["language", "Primary language"], ["timezone", "Timezone"], ["street", "Street address"], ["province", "Province / state"], ["contactName", "Primary contact name"], ["contactEmail", "Primary contact email"], ["contactPhone", "Primary contact phone"]]; return <div className="wizard-form-grid">{fields.map(([key, label]) => <Field key={key} label={label} value={text(value, key)} onChange={field => update({ [key]: field })} type={key.includes("Email") || key === "contactEmail" ? "email" : "text"} />)}</div>; }
function AcademicStep({ value, update }: { value: WizardData; update: (value: WizardData) => void }) { const faculties = (value.faculties as Row[] | undefined) ?? []; const departments = (value.departments as Row[] | undefined) ?? []; const programmes = (value.programmes as Row[] | undefined) ?? []; const add = (key: string, list: Row[]) => update({ [key]: [...list, { name: "", code: "" }] }); const edit = (key: string, list: Row[], index: number, field: keyof Row, fieldValue: string) => update({ [key]: list.map((row, item) => item === index ? { ...row, [field]: fieldValue } : row) }); const list = (key: string, rows: Row[]) => <div className="wizard-list">{rows.map((row, index) => <div className="wizard-row" key={`${key}-${index}`}><input aria-label={`${key} name`} value={row.name} onChange={event => edit(key, rows, index, "name", event.target.value)} placeholder="Name" /><input aria-label={`${key} code`} value={row.code} onChange={event => edit(key, rows, index, "code", event.target.value)} placeholder="Code" /><button aria-label={`Remove ${key}`} className="icon-button" onClick={() => update({ [key]: rows.filter((_, item) => item !== index) })}><Trash2 size={16} /></button></div>)}</div>; return <div className="wizard-sections"><section><h3>Faculties / schools</h3>{list("faculties", faculties)}<button className="link-button" onClick={() => add("faculties", faculties)}><Plus size={16} /> Add faculty</button></section><section><h3>Departments</h3>{list("departments", departments)}<button className="link-button" onClick={() => add("departments", departments)}><Plus size={16} /> Add department</button></section><section><h3>Programmes</h3>{list("programmes", programmes)}<button className="link-button" onClick={() => add("programmes", programmes)}><Plus size={16} /> Add programme</button></section><div className="wizard-form-grid"><Field label="Calendar type" value={text(value, "calendarType")} onChange={calendarType => update({ calendarType })} /><Field label="Academic year" value={text(value, "academicYear")} onChange={academicYear => update({ academicYear })} /><Field label="Semester start" value={text(value, "semesterStart")} onChange={semesterStart => update({ semesterStart })} type="date" /><Field label="Semester end" value={text(value, "semesterEnd")} onChange={semesterEnd => update({ semesterEnd })} type="date" /></div></div>; }
function GatewayStep({ value, update, mode }: { value: WizardData; update: (value: WizardData) => void; mode: Mode }) {
  const enabled = (value.enabledGateways as string[] | undefined) ?? [];
  const configured = (value.configuredGateways as string[] | undefined) ?? [];
  const [editing, setEditing] = useState<string | null>(null);
  const [fields, setFields] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [health, setHealth] = useState<Record<string, string>>({});
  const fieldNames = editing === "Raast RTP"
    ? ["MERCHANT_ID", "CLIENT_ID", "CLIENT_SECRET", "IBAN", "WEBHOOK_SECRET", "ENV"]
    : ["CLIENT_ID", "CLIENT_SECRET", "WEBHOOK_SECRET", "ENV"];
  const visibleFields = fields.ENV === "production" ? [...fieldNames, "PQC_ALGORITHM", "PQC_PUBLIC_KEY"] : fieldNames;
  const slug = (name: string) => name === "Raast RTP" ? "raast" : name.toLowerCase().replaceAll(" ", "-");
  const open = (name: string) => { setEditing(name); setFields({ ENV: "sandbox" }); setError(""); };
  const save = async () => {
    if (!editing || visibleFields.some(field => !fields[field]?.trim())) { setError("Complete every required gateway field."); return; }
    setSaving(true); setError("");
    try {
      const result = await apiPost<{ success: boolean; healthStatus?: string; latencyMs?: number }>(`/api/wizard/gateways/${slug(editing)}/configure`, fields);
      if (!result.success) { setError(`Gateway health check failed${result.healthStatus ? `: ${result.healthStatus}` : "."}`); return; }
      setHealth(current => ({ ...current, [editing]: `Connected${typeof result.latencyMs === "number" ? ` · ${result.latencyMs}ms` : ""}` }));
      update({ enabledGateways: enabled.includes(editing) ? enabled : [...enabled, editing], configuredGateways: configured.includes(editing) ? configured : [...configured, editing] });
      setEditing(null);
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Gateway configuration failed."); }
    finally { setSaving(false); }
  };
  const test = async (name: string) => {
    setHealth(current => ({ ...current, [name]: "Checking..." }));
    try {
      const result = await apiPost<{ success: boolean; healthStatus?: string; latencyMs?: number; error?: string }>(`/api/wizard/gateways/${slug(name)}/health-check`, {});
      setHealth(current => ({ ...current, [name]: result.success ? `Connected${typeof result.latencyMs === "number" ? ` · ${result.latencyMs}ms` : ""}` : result.error ?? result.healthStatus ?? "Health check failed" }));
    } catch (cause) { setHealth(current => ({ ...current, [name]: cause instanceof Error ? cause.message : "Health check failed" })); }
  };
  const toggle = (name: string) => update({ enabledGateways: enabled.includes(name) ? enabled.filter(item => item !== name) : [...enabled, name] });
  const skipDemo = () => update({ skippedInDemo: true, enabledGateways: enabled.length > 0 ? enabled : ["Sandbox Gateway"] });

  return (
    <div>
      {mode === "demo" ? (
        <div style={{ backgroundColor: "#fffbeb", border: "1px solid #fcd34d", borderRadius: "0.75rem", padding: "1rem", color: "#92400e", fontSize: "0.875rem", marginBottom: "1.25rem", display: "flex", flexDirection: "column", gap: "0.5rem" }}>
          <div style={{ display: "flex", alignItems: "center", gap: "0.5rem", fontWeight: "700" }}>
            🧪 SANDBOX MODE ACTIVE
          </div>
          <div>
            Payment gateway credentials are not required in Demo. All payment operations will be simulated automatically. Configure real credentials after upgrading to Licensed.
          </div>
          <button
            type="button"
            className="wizard-secondary"
            style={{ width: "fit-content", marginTop: "0.5rem", fontSize: "0.8rem", padding: "4px 12px", minHeight: "32px" }}
            onClick={skipDemo}
          >
            Skip — Configure After Upgrade
          </button>
        </div>
      ) : (
        <p className="wizard-muted" style={{ marginBottom: "1rem" }}>
          At least one payment gateway must be configured to proceed.
        </p>
      )}

      <div className="wizard-grid two">
        {gateways.map(name => {
          const isConfigured = configured.includes(name);
          const isEnabled = enabled.includes(name);
          return (
            <div className={`wizard-gateway ${mode === "demo" ? "locked" : ""}`} key={name}>
              <div>
                <div style={{ display: "flex", alignItems: "center", gap: "6px" }}>
                  {mode === "demo" && <LockKeyhole size={14} style={{ color: "#9ca3af" }} />}
                  <b>{name}</b>
                </div>
                <div style={{ marginTop: "4px" }}>
                  <span style={{ fontSize: "0.75rem", padding: "2px 6px", borderRadius: "4px", fontWeight: "600", backgroundColor: isConfigured ? "#dcfce7" : "#f3f4f6", color: isConfigured ? "#166534" : "#4b5563" }}>
                    {isConfigured ? "CONFIGURED ✓" : "NOT CONFIGURED"}
                  </span>
                </div>
                <small>{health[name] ?? (isConfigured ? "Configured" : "Not configured")}</small>
              </div>
              <div className="wizard-gateway-actions">
                <button disabled={mode === "demo"} className="toggle" onClick={() => toggle(name)}>
                  {isEnabled ? "ON" : "OFF"}
                </button>
                <button disabled={mode === "demo"} className="link-button" onClick={() => open(name)}>
                  Configure
                </button>
                {isConfigured && (
                  <button disabled={mode === "demo" || health[name] === "Checking..."} className="link-button" onClick={() => test(name)} aria-label={`Test ${name} connection`}>
                    Test connection
                  </button>
                )}
              </div>
            </div>
          );
        })}
      </div>

      {editing && (
        <div className="wizard-modal" role="dialog" aria-modal="true" aria-label={`Configure ${editing}`}>
          <div className="wizard-card">
            <h3>Configure {editing}</h3>
            {visibleFields.map(field => (
              <Field key={field} label={field.replaceAll("_", " ")} value={fields[field] ?? ""} onChange={fieldValue => setFields(current => ({ ...current, [field]: fieldValue }))} type={field.includes("SECRET") || field === "PQC_PUBLIC_KEY" ? "password" : "text"} />
            ))}
            {fields.ENV === "production" && <div className="wizard-error">Live mode requires an ML-DSA-65 public key for webhook verification.</div>}
            {error && <p className="wizard-error">{error}</p>}
            <div className="wizard-footer">
              <button className="wizard-secondary" onClick={() => setEditing(null)}>Cancel</button>
              <button className="wizard-primary" disabled={saving || visibleFields.some(field => !fields[field]?.trim())} onClick={save}>
                {saving ? "Saving..." : "Save gateway"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
function FinanceStep({ value, update }: { value: WizardData; update: (value: WizardData) => void }) { return <div className="wizard-form-grid"><Field label="Primary accounting standard" value={text(value, "standard")} onChange={standard => update({ standard })} /><Field label="Fiscal year label" value={text(value, "fiscalYear")} onChange={fiscalYear => update({ fiscalYear })} /><Field label="Fiscal year start month" value={text(value, "fiscalYearStartMonth")} onChange={fiscalYearStartMonth => update({ fiscalYearStartMonth })} type="number" /><Field label="Base currency" value={text(value, "baseCurrency").toUpperCase()} onChange={baseCurrency => update({ baseCurrency: baseCurrency.toUpperCase() })} /><Field label="Exchange rate source" value={text(value, "rateSource")} onChange={rateSource => update({ rateSource })} /><Field label="Open Exchange Rates API key" value={text(value, "apiKey")} onChange={apiKey => update({ apiKey })} type="password" required={false} /></div>; }
function FeesStep({ value, update }: { value: WizardData; update: (value: WizardData) => void }) { const heads = (value.feeHeads as Row[] | undefined) ?? []; return <div className="wizard-sections"><section><h3>Fee heads</h3>{heads.map((head, index) => <div className="wizard-row" key={index}><input value={head.name} placeholder="Tuition fee" onChange={event => update({ feeHeads: heads.map((item, i) => i === index ? { ...item, name: event.target.value } : item) })} /><input value={head.code} placeholder="TF" onChange={event => update({ feeHeads: heads.map((item, i) => i === index ? { ...item, code: event.target.value } : item) })} /></div>)}<button className="link-button" onClick={() => update({ feeHeads: [...heads, { name: "", code: "" }] })}><Plus size={16} /> Add fee head</button></section><Field label="Programme assignment amount" value={text(value, "assignmentAmount")} onChange={assignmentAmount => update({ assignmentAmount, assignments: [{ programme: "default", amount: assignmentAmount }] })} type="number" /></div>; }
function UsersStep({ value, update }: { value: WizardData; update: (value: WizardData) => void }) { const users = (value.users as User[] | undefined) ?? []; return <div className="wizard-sections">{users.map((user, index) => <div className="wizard-row" key={index}><input value={user.name} placeholder="Full name" onChange={event => update({ users: users.map((item, i) => i === index ? { ...item, name: event.target.value } : item) })} /><input value={user.email} placeholder="Email" onChange={event => update({ users: users.map((item, i) => i === index ? { ...item, email: event.target.value } : item) })} /><input type="password" value={user.password ?? ""} placeholder="Temporary password" onChange={event => update({ users: users.map((item, i) => i === index ? { ...item, password: event.target.value } : item) })} /><select value={user.role} onChange={event => update({ users: users.map((item, i) => i === index ? { ...item, role: event.target.value as User["role"] } : item) })}><option>Super Admin</option><option>Finance Controller</option></select></div>)}<button className="link-button" onClick={() => update({ users: [...users, { name: "", email: "", password: "", role: users.some(user => user.role === "Super Admin") ? "Finance Controller" : "Super Admin" }] })}><Plus size={16} /> Add administrator</button></div>; }
function ReviewStep({ data, value, update }: { data: Record<string, WizardData>; value: WizardData; update: (value: WizardData) => void }) { const legal = value.legalAccepted === true; const terms = value.termsAccepted === true; return <div className="wizard-review"><div className="wizard-review-grid">{steps.slice(0, 7).map((label, index) => <article key={label}><span>STEP {index}</span><h3>{label}</h3><p>{Object.keys(data[`step${index}`] ?? {}).length} configured fields</p></article>)}</div><label className="wizard-check"><input type="checkbox" checked={legal} onChange={event => update({ legalAccepted: event.target.checked, confirmed: event.target.checked && terms })} /> I confirm the information is accurate and I am authorised to set up AegisCore ERP.</label><label className="wizard-check"><input type="checkbox" checked={terms} onChange={event => update({ termsAccepted: event.target.checked, confirmed: legal && event.target.checked })} /> I accept the Terms of Service and Privacy Policy.</label></div>; }
function isValid(step: number, value: WizardData) { if (step === 0) return value.mode === "demo" || (value.mode === "paid" && value.licenseValidated === true); if (step === 1) return ["name", "shortName", "type", "country", "city", "language", "timezone", "street", "province", "contactName", "contactEmail", "contactPhone"].every(key => Boolean(value[key])); if (step === 2) return ["faculties", "departments", "programmes"].every(key => Array.isArray(value[key]) && (value[key] as unknown[]).length > 0) && ["calendarType", "academicYear", "semesterStart", "semesterEnd"].every(key => Boolean(value[key])); if (step === 3) return value.mode === "demo" || (Array.isArray(value.enabledGateways) && Array.isArray(value.configuredGateways) && (value.enabledGateways as unknown[]).some(item => (value.configuredGateways as unknown[]).includes(item))); if (step === 4) { const month = Number(value.fiscalYearStartMonth); const source = text(value, "rateSource").trim().toUpperCase().replaceAll(" ", "_"); return ["standard", "fiscalYear", "baseCurrency", "rateSource"].every(key => Boolean(value[key])) && Number.isInteger(month) && month >= 1 && month <= 12 && (source !== "OPEN_EXCHANGE_RATES" || Boolean(value.apiKey)); } if (step === 5) return Array.isArray(value.feeHeads) && (value.feeHeads as unknown[]).length >= 2 && Boolean(value.assignmentAmount); if (step === 6) return Array.isArray(value.users) && (value.users as User[]).every(user => Boolean(user.name && user.email && (user.password ?? "").length >= 12)) && (value.users as User[]).some(user => user.role === "Super Admin") && (value.users as User[]).some(user => user.role === "Finance Controller"); return value.confirmed === true; }
