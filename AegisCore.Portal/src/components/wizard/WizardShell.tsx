"use client";

import { Check, ChevronLeft, ChevronRight, Circle, Clock, Eye, EyeOff, Plus, ShieldCheck, Trash2 } from "lucide-react";
import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";

// ─── Types ────────────────────────────────────────────────────────────────────
type Mode = "demo" | "paid";
type Row = { name: string; code: string };
type User = { name: string; email: string; role: "Super Admin" | "Finance Controller" | "Head of Department" | "Admissions Head" | "Academic Head" | "HR Head" | "IT Head"; password: string };
type WizardData = Record<string, unknown>;
type Session = { sessionId: string; currentStep: number; licenseMode: Mode; data: Record<string, WizardData> };
type DemoStatus = { isDemo: boolean; expiryUtc: string | null; remainingDays: number; remainingHours: number; isExpired: boolean };
type VaultProvider = "FILE" | "INFISICAL" | "HASHICORP_VAULT" | "AZURE_KEYVAULT" | "AWS_SECRETS_MANAGER" | "GCP_SECRET_MANAGER";

// ─── Static data ──────────────────────────────────────────────────────────────
const steps = [
  "License plan", "University information", "Academic structure",
  "Payment gateways", "Finance configuration", "Fee structure",
  "Admin accounts", "Review & activate", "Infrastructure secrets",
];

const gateways = [
  "Raast RTP", "1Link", "JazzCash Merchant", "EasyPaisa",
  "HBL Pay", "MCB Corporate", "UBL Omni", "Stripe",
  "Network International", "Telr", "GoCardless", "Plaid ACH",
  "Billplz FPX", "DuitNow QR", "SWIFT MT103",
];

const demoDayPresets = [30, 60, 90, 120, 180, 365];

/** Full IANA timezone list — all zones globally */
const TIMEZONES: string[] = Intl.supportedValuesOf("timeZone");

/** ISO 4217 currency list */
const CURRENCIES: { code: string; name: string }[] = [
  { code: "PKR", name: "Pakistani Rupee" },
  { code: "USD", name: "US Dollar" },
  { code: "EUR", name: "Euro" },
  { code: "GBP", name: "British Pound" },
  { code: "AED", name: "UAE Dirham" },
  { code: "SAR", name: "Saudi Riyal" },
  { code: "INR", name: "Indian Rupee" },
  { code: "BDT", name: "Bangladeshi Taka" },
  { code: "MYR", name: "Malaysian Ringgit" },
  { code: "IDR", name: "Indonesian Rupiah" },
  { code: "NGN", name: "Nigerian Naira" },
  { code: "EGP", name: "Egyptian Pound" },
  { code: "TRY", name: "Turkish Lira" },
  { code: "CAD", name: "Canadian Dollar" },
  { code: "AUD", name: "Australian Dollar" },
  { code: "JPY", name: "Japanese Yen" },
  { code: "CNY", name: "Chinese Yuan" },
  { code: "KWD", name: "Kuwaiti Dinar" },
  { code: "BHD", name: "Bahraini Dinar" },
  { code: "QAR", name: "Qatari Riyal" },
  { code: "OMR", name: "Omani Rial" },
  { code: "JOD", name: "Jordanian Dinar" },
  { code: "ZAR", name: "South African Rand" },
  { code: "KES", name: "Kenyan Shilling" },
  { code: "GHS", name: "Ghanaian Cedi" },
  { code: "BRL", name: "Brazilian Real" },
  { code: "MXN", name: "Mexican Peso" },
  { code: "CHF", name: "Swiss Franc" },
  { code: "SEK", name: "Swedish Krona" },
  { code: "NOK", name: "Norwegian Krone" },
  { code: "DKK", name: "Danish Krone" },
  { code: "SGD", name: "Singapore Dollar" },
  { code: "HKD", name: "Hong Kong Dollar" },
  { code: "NZD", name: "New Zealand Dollar" },
  { code: "THB", name: "Thai Baht" },
  { code: "PHP", name: "Philippine Peso" },
  { code: "VND", name: "Vietnamese Dong" },
  { code: "IQD", name: "Iraqi Dinar" },
  { code: "IRR", name: "Iranian Rial" },
  { code: "LKR", name: "Sri Lankan Rupee" },
  { code: "NPR", name: "Nepalese Rupee" },
  { code: "AFN", name: "Afghan Afghani" },
];

const CALENDAR_TYPES = [
  "Semester (Fall/Spring)",
  "Trimester",
  "Quarter",
  "Annual / Yearly",
  "Islamic Calendar (Hijri)",
  "Custom",
];

/** Generate academic year options: current-1 through current+3 */
function academicYearOptions(): string[] {
  const now = new Date().getFullYear();
  return Array.from({ length: 6 }, (_, i) => {
    const y = now - 1 + i;
    return `${y}-${String(y + 1).slice(2)}`;
  });
}

/** Province/state options keyed by country name */
const PROVINCES: Record<string, string[]> = {
  Pakistan: ["Punjab", "Sindh", "Khyber Pakhtunkhwa", "Balochistan", "Islamabad Capital Territory", "Azad Jammu & Kashmir", "Gilgit-Baltistan"],
  India: ["Andhra Pradesh", "Assam", "Bihar", "Delhi", "Gujarat", "Karnataka", "Kerala", "Maharashtra", "Punjab", "Rajasthan", "Tamil Nadu", "Uttar Pradesh", "West Bengal"],
  "United States": ["Alabama", "Alaska", "Arizona", "California", "Colorado", "Florida", "Georgia", "Illinois", "New York", "Ohio", "Pennsylvania", "Texas", "Washington"],
  "United Kingdom": ["England", "Scotland", "Wales", "Northern Ireland"],
  Canada: ["Alberta", "British Columbia", "Manitoba", "New Brunswick", "Nova Scotia", "Ontario", "Quebec", "Saskatchewan"],
  Australia: ["New South Wales", "Queensland", "South Australia", "Tasmania", "Victoria", "Western Australia"],
  UAE: ["Abu Dhabi", "Dubai", "Sharjah", "Ajman", "Fujairah", "Ras Al Khaimah", "Umm Al Quwain"],
  "Saudi Arabia": ["Riyadh", "Mecca", "Medina", "Eastern Province", "Jizan", "Najran", "Asir"],
  Malaysia: ["Johor", "Kedah", "Kelantan", "Kuala Lumpur", "Penang", "Perak", "Sabah", "Sarawak", "Selangor"],
  Bangladesh: ["Dhaka", "Chattogram", "Khulna", "Rajshahi", "Sylhet", "Barisal", "Rangpur", "Mymensingh"],
  Nigeria: ["Lagos", "Abuja FCT", "Kano", "Rivers", "Oyo", "Kaduna", "Anambra", "Enugu"],
  Egypt: ["Cairo", "Alexandria", "Giza", "Aswan", "Luxor", "Mansoura", "Port Said", "Suez"],
  Turkey: ["Istanbul", "Ankara", "Izmir", "Bursa", "Antalya", "Adana", "Konya"],
  Kenya: ["Nairobi", "Mombasa", "Kisumu", "Nakuru", "Eldoret", "Thika"],
  Indonesia: ["Jakarta", "East Java", "West Java", "Central Java", "Bali", "North Sumatra", "South Sulawesi"],
};

const VAULT_PROVIDERS: { value: VaultProvider; label: string }[] = [
  { value: "FILE", label: "Local file vault (default)" },
  { value: "INFISICAL", label: "Infisical" },
  { value: "HASHICORP_VAULT", label: "HashiCorp Vault" },
  { value: "AZURE_KEYVAULT", label: "Azure Key Vault" },
  { value: "AWS_SECRETS_MANAGER", label: "AWS Secrets Manager" },
  { value: "GCP_SECRET_MANAGER", label: "GCP Secret Manager" },
];

// ─── Helpers ──────────────────────────────────────────────────────────────────
function text(value: WizardData, key: string) {
  return typeof value[key] === "string" ? String(value[key]) : "";
}

// ─── Field components ─────────────────────────────────────────────────────────
function Field({
  label, value, onChange, type = "text", required = true, placeholder,
}: {
  label: string; value: string; onChange: (value: string | number) => void;
  type?: string; required?: boolean; placeholder?: string;
}) {
  const [show, setShow] = useState(false);
  const isPassword = type === "password";
  return (
    <label className="wizard-field">
      <span>{label}{required ? " *" : ""}</span>
      <div style={{ position: "relative", display: "flex", alignItems: "center" }}>
        <input
          type={isPassword && show ? "text" : type}
          value={value}
          placeholder={placeholder}
          style={{ width: "100%", paddingRight: isPassword ? "2.25rem" : undefined }}
          onChange={e => {
            const v = e.target.value;
            onChange(type === "number" ? (v === "" ? "" : Number(v)) : v);
          }}
        />
        {isPassword && (
          <button
            type="button"
            onClick={() => setShow(s => !s)}
            style={{
              position: "absolute", right: "0.5rem", background: "none",
              border: "none", cursor: "pointer", color: "#6b7280", padding: 0,
              display: "flex", alignItems: "center",
            }}
            aria-label={show ? "Hide" : "Show"}
          >
            {show ? <EyeOff size={15} /> : <Eye size={15} />}
          </button>
        )}
      </div>
    </label>
  );
}

function SelectField({
  label, value, onChange, options, required = true, placeholder,
}: {
  label: string; value: string; onChange: (v: string) => void;
  options: { value: string; label: string }[]; required?: boolean; placeholder?: string;
}) {
  return (
    <label className="wizard-field">
      <span>{label}{required ? " *" : ""}</span>
      <select value={value} onChange={e => onChange(e.target.value)}>
        {placeholder && <option value="">{placeholder}</option>}
        {options.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
      </select>
    </label>
  );
}

// ─── Step descriptions ────────────────────────────────────────────────────────
function stepDescription(step: number) {
  return [
    "Choose the license mode for this tenant.",
    "Tell us about the university and its primary contact.",
    "Build the academic hierarchy and calendar.",
    "Connect payment accounts or continue in sandbox mode.",
    "Set the accounting and reporting baseline.",
    "Define fee heads and payment defaults.",
    "Create the first real administrators.",
    "Confirm the setup and seal the tenant.",
    "Provide infrastructure secrets and API keys.",
  ][step] ?? "";
}

// ─── Validation ───────────────────────────────────────────────────────────────
function isValid(step: number, value: WizardData, mode: Mode): boolean {
  if (step === 0) return mode === "demo" || (mode === "paid" && value.licenseValidated === true);
  if (step === 1) return ["name", "shortName", "type", "country", "city", "language", "timezone", "street", "province", "contactName", "contactEmail", "contactPhone"].every(k => Boolean(value[k]));
  if (step === 2) return (
    ["faculties", "departments", "programmes"].every(k => Array.isArray(value[k]) && (value[k] as unknown[]).length > 0) &&
    ["calendarType", "academicYear", "semesterStart", "semesterEnd"].every(k => Boolean(value[k]))
  );
  if (step === 3) return mode === "demo" || (
    Array.isArray(value.enabledGateways) && Array.isArray(value.configuredGateways) &&
    (value.enabledGateways as unknown[]).some(item => (value.configuredGateways as unknown[]).includes(item))
  );
  if (step === 4) {
    const month = Number(value.fiscalYearStartMonth);
    const source = text(value, "rateSource").trim().toUpperCase().replaceAll(" ", "_");
    return (
      ["standard", "fiscalYear", "baseCurrency", "rateSource"].every(k => Boolean(value[k])) &&
      Number.isInteger(month) && month >= 1 && month <= 12 &&
      (source !== "OPEN_EXCHANGE_RATES" || Boolean(value.apiKey))
    );
  }
  if (step === 5) return Array.isArray(value.feeHeads) && (value.feeHeads as unknown[]).length >= 2 && Boolean(value.assignmentAmount);
  if (step === 6) return (
    Array.isArray(value.users) &&
    (value.users as User[]).every(u => Boolean(u.name && u.email && (u.password ?? "").length >= 12)) &&
    (value.users as User[]).some(u => u.role === "Super Admin") &&
    (value.users as User[]).some(u => u.role === "Finance Controller")
  );
  if (step === 7) return value.confirmed === true;
  // Step 8 (secrets): postgres + redis required; banking keys optional
  if (step === 8) return Boolean(value.dbConnectionString) && Boolean(value.redisConnectionString);
  return false;
}

// ─── Main shell ───────────────────────────────────────────────────────────────
export function WizardShell() {
  const [session, setSession] = useState<Session | null>(null);
  const [step, setStep] = useState(0);
  const [data, setData] = useState<Record<string, WizardData>>({});
  const [busy, setBusy] = useState(true);
  const [notice, setNotice] = useState("");
  const [activated, setActivated] = useState(false);
  const [demoStatus, setDemoStatus] = useState<DemoStatus>({
    isDemo: false, expiryUtc: null, remainingDays: 30, remainingHours: 720, isExpired: false,
  });

  useEffect(() => {
    Promise.all([
      apiGet<Session>("/api/wizard/session"),
      apiGet<DemoStatus>("/api/wizard/demo-status").catch(() => null as DemoStatus | null),
    ]).then(([value, demo]) => {
      setSession(value);
      setData(value.data ?? {});
      setStep(Math.min(value.currentStep, steps.length - 1));
      if (demo) setDemoStatus(demo);
    }).catch(() => {
      const saved = localStorage.getItem("aegiscore_wizard_session");
      if (saved) {
        const value = JSON.parse(saved) as Session;
        setSession(value);
        setData(value.data ?? {});
        setStep(Math.min(value.currentStep, steps.length - 1));
      }
    }).finally(() => setBusy(false));
  }, []);

  const current = useMemo(() => data[`step${step}`] ?? {}, [data, step]);
  const update = (patch: WizardData) =>
    setData(v => ({ ...v, [`step${step}`]: { ...v[`step${step}`], ...patch } }));
  const mode = useMemo(() => (data.step0?.mode as Mode) ?? session?.licenseMode ?? "demo", [data, session]);
  const valid = useMemo(() => isValid(step, current, mode), [step, current, mode]);

  async function changeMode(mode: Mode, licenseKey?: string) {
    if (!session) return;
    setBusy(true); setNotice("");
    try {
      const result = await apiPost<{ success: boolean; mode: Mode; currentStep: number; data: Record<string, WizardData> }>(
        "/api/wizard/mode", { sessionId: session.sessionId, mode, licenseKey }
      );
      const nextSession = { ...session, currentStep: result.currentStep, licenseMode: result.mode, data: result.data };
      setSession(nextSession); setData(result.data);
      setStep(Math.min(result.currentStep, steps.length - 1));
      localStorage.setItem("aegiscore_wizard_session", JSON.stringify(nextSession));
    } catch (e) { setNotice(e instanceof Error ? e.message : "The license mode could not be changed."); }
    finally { setBusy(false); }
  }

  async function activateDemo(days: number) {
    if (!session) return;
    setBusy(true); setNotice("");
    try {
      await apiPost(`/api/wizard/activate-demo?expiryDays=${days}`, {});
      const demo = await apiGet<DemoStatus>("/api/wizard/demo-status");
      setDemoStatus(demo);
      await changeMode("demo");
    } catch (e) { setNotice(e instanceof Error ? e.message : "Demo activation failed."); }
    finally { setBusy(false); }
  }

  async function next() {
    if (!session || !valid) return;
    setBusy(true); setNotice("");
    try {
      const payload = { ...current, mode: (data.step0?.mode as Mode) ?? session.licenseMode ?? "demo" };
      const result = await apiPost<{ success: boolean; nextStep: number }>(
        `/api/wizard/step/${step}/save`, { sessionId: session.sessionId, data: payload }
      );
      const nextStep = Math.min(result.nextStep, steps.length - 1);
      const storedData = step === 6
        ? { ...data, step6: { ...current, users: Array.isArray(current.users) ? (current.users as User[]).map(u => ({ name: u.name, email: u.email, role: u.role })) : [] } }
        : { ...data };
      const nextSession = { ...session, currentStep: nextStep, licenseMode: (data.step0?.mode as Mode) ?? session.licenseMode, data: storedData };
      setSession(nextSession); setStep(nextStep);
      localStorage.setItem("aegiscore_wizard_session", JSON.stringify(nextSession));
    } catch (e) { setNotice(e instanceof Error ? e.message : "The step could not be saved."); }
    finally { setBusy(false); }
  }

  async function activate() {
    if (!session || !valid) return;
    setBusy(true); setNotice("");
    try {
      await apiPost("/api/wizard/step/7/save", { sessionId: session.sessionId, data: current });
      await apiPost("/api/wizard/complete", {
        sessionId: session.sessionId,
        confirmedBy: String((data.step6?.users as User[])[0]?.email ?? ""),
        timestamp: new Date().toISOString(),
      });
      localStorage.removeItem("aegiscore_wizard_session");
      setActivated(true);
    } catch (e) { setNotice(e instanceof Error ? e.message : "Activation failed."); }
    finally { setBusy(false); }
  }

  async function submitSecrets() {
    if (!session || !valid) return;
    setBusy(true); setNotice("");
    try {
      await apiPost("/api/wizard/step/8/secrets", { sessionId: session.sessionId, data: current });
      localStorage.removeItem("aegiscore_wizard_session");
      setActivated(true);
    } catch (e) { setNotice(e instanceof Error ? e.message : "Secrets could not be saved."); }
    finally { setBusy(false); }
  }

  if (busy && !session) return (
    <main className="wizard-loading"><ShieldCheck size={34} /><p>Loading your secure setup session...</p></main>
  );
  if (activated) return (
    <main className="wizard-success">
      <Check size={54} />
      <p>AEGISCORE IS READY</p>
      <h1>Your university instance is live.</h1>
      <Link className="wizard-primary" href="/">Open dashboard <ChevronRight size={18} /></Link>
    </main>
  );

  const isLastStep = step === steps.length - 1;

  return (
    <main className="wizard-shell">
      <aside className="wizard-rail">
        <div className="wizard-brand"><span><ShieldCheck size={24} /></span><strong>AegisCore</strong></div>
        <div>
          <p className="wizard-kicker">UNIVERSITY ERP</p>
          <h1>Configure your institution.</h1>
          <p className="wizard-muted">Every step is saved securely. You can return to this wizard at any time before activation.</p>
        </div>
        <ol className="wizard-steps">
          {steps.map((label, index) => (
            <li key={label} className={index === step ? "active" : index < step ? "complete" : ""}>
              <i>{index < step ? <Check size={15} /> : index === step ? <Circle size={10} fill="currentColor" /> : index + 1}</i>
              <span>{label}</span>
            </li>
          ))}
        </ol>
        <p className="wizard-help">Need help? <a href="mailto:admin@aegiscore.pk">admin@aegiscore.pk</a></p>
      </aside>

      <section className="wizard-content">
        {mode === "demo" && step > 0 && demoStatus.expiryUtc && (
          <div className={`wizard-demo-pill${demoStatus.isExpired ? " expired" : ""}`}>
            {demoStatus.isExpired ? "DEMO EXPIRED" : "DEMO MODE"}
            <span>{demoStatus.isExpired ? "Upgrade now" : `${Math.max(0, Math.ceil(demoStatus.remainingDays))} day${Math.ceil(demoStatus.remainingDays) === 1 ? "" : "s"} left`}</span>
          </div>
        )}

        <header>
          <p className="wizard-kicker">STEP {step + 1} OF {steps.length}</p>
          <h2>{steps[step]}</h2>
          <p className="wizard-muted">{stepDescription(step)}</p>
        </header>

        {step === 0 && <LicenseStep value={current} update={update} changeMode={changeMode} activateDemo={activateDemo} busy={busy} demoStatus={demoStatus} />}
        {step === 1 && <UniversityStep value={current} update={update} />}
        {step === 2 && <AcademicStep value={current} update={update} />}
        {step === 3 && <GatewayStep value={current} update={update} mode={mode} />}
        {step === 4 && <FinanceStep value={current} update={update} />}
        {step === 5 && <FeesStep value={current} update={update} />}
        {step === 6 && <UsersStep value={current} update={update} />}
        {step === 7 && <ReviewStep data={data} value={current} update={update} />}
        {step === 8 && <SecretsStep value={current} update={update} mode={mode} />}

        {notice && <p className="wizard-error">{notice}</p>}

        <footer className="wizard-footer">
          <button className="wizard-secondary" disabled={step === 0 || busy} onClick={() => setStep(v => v - 1)}>
            <ChevronLeft size={17} /> Back
          </button>
          {step === 7 ? (
            <button className="wizard-primary" disabled={!valid || busy} onClick={activate}>
              {busy ? "Activating..." : "Activate instance"} <ChevronRight size={17} />
            </button>
          ) : isLastStep ? (
            <button className="wizard-primary" disabled={!valid || busy} onClick={submitSecrets}>
              {busy ? "Sealing..." : "Submit secrets & go live"} <ChevronRight size={17} />
            </button>
          ) : (
            <button className="wizard-primary" disabled={!valid || busy} onClick={next}>
              {busy ? "Saving..." : "Next"} <ChevronRight size={17} />
            </button>
          )}
        </footer>
      </section>
    </main>
  );
}

// ─── Step 0: License ──────────────────────────────────────────────────────────
function LicenseStep({
  value, update, changeMode, activateDemo, busy, demoStatus,
}: {
  value: WizardData; update: (v: WizardData) => void;
  changeMode: (mode: Mode, licenseKey?: string) => Promise<void>;
  activateDemo: (days: number) => Promise<void>;
  busy: boolean; demoStatus: DemoStatus;
}) {
  const selectedMode = value.mode as Mode | undefined;
  const mode = selectedMode ?? "demo";
  const [demoDays, setDemoDays] = useState(30);
  const [customDays, setCustomDays] = useState("");
  const effectiveDays = customDays ? parseInt(customDays, 10) || 30 : demoDays;
  const licenseKey = text(value, "licenseKey");

  return (
    <div>
      {demoStatus.isExpired && !selectedMode && (
        <div className="wizard-expired-banner">
          <Clock size={18} />
          <div>
            <strong>Your demo period has ended</strong>
            <p>Switch to a production license to continue using AegisCore.</p>
          </div>
        </div>
      )}
      <div className="wizard-grid two">
        <div className={`wizard-card ${mode === "demo" ? "selected" : ""}${demoStatus.isExpired ? " expired" : ""}`}>
          <b>FREE</b>
          <h3>Demo</h3>
          {demoStatus.isDemo && demoStatus.expiryUtc
            ? <strong>PKR 0 / {Math.max(0, demoStatus.remainingDays)} day{demoStatus.remainingDays === 1 ? "" : "s"} left</strong>
            : <strong>PKR 0 / {effectiveDays} day{effectiveDays === 1 ? "" : "s"}</strong>}
          <p>Full access to all modules with sandbox payments.</p>
          <small>Real money never moves in demo mode.</small>
          {selectedMode === "demo" ? (
            <button className="wizard-mode-switch active" type="button" disabled>
              <span aria-hidden="true" /> Demo mode active
            </button>
          ) : demoStatus.isExpired ? (
            <div>
              <p className="wizard-error" style={{ marginTop: 8 }}>Demo expired</p>
              <button className="wizard-mode-switch" type="button" disabled style={{ opacity: 0.5 }}>
                <span aria-hidden="true" /> No longer available
              </button>
            </div>
          ) : (
            <div className="wizard-demo-days">
              <p className="wizard-muted" style={{ fontSize: "0.8rem", marginBottom: 6 }}>Choose trial duration:</p>
              <div className="wizard-day-presets">
                {demoDayPresets.map(d => (
                  <button key={d} className={`wizard-day-chip${demoDays === d && !customDays ? " active" : ""}`} type="button"
                    onClick={() => { setDemoDays(d); setCustomDays(""); update({ mode: "demo", demoDays: d }); }}>
                    {d}d
                  </button>
                ))}
                <input className="wizard-day-custom" type="number" min={1} max={365} placeholder="Custom" value={customDays}
                  onChange={e => { setCustomDays(e.target.value); if (e.target.value) setDemoDays(0); update({ mode: "demo", demoDays: parseInt(e.target.value, 10) || 30 }); }}
                  style={{ width: 72, padding: "2px 6px", fontSize: "0.8rem", borderRadius: 6, border: "1px solid #d1d5db" }} />
              </div>
              <button className="wizard-primary" type="button"
                disabled={busy || effectiveDays < 1 || effectiveDays > 365}
                onClick={() => activateDemo(effectiveDays)}
                style={{ marginTop: 12, width: "100%", fontSize: "0.85rem", padding: "8px 12px" }}>
                {busy ? "Activating..." : `Start ${effectiveDays}-day trial`}
              </button>
            </div>
          )}
        </div>

        <div className={`wizard-card ${mode === "paid" ? "selected" : ""}`}>
          <b>RECOMMENDED</b>
          <h3>Live / Licensed</h3>
          <strong>Contact Sales</strong>
          <p>Unlimited capacity and real gateway integrations.</p>
          <Field label="License key" value={licenseKey}
            onChange={v => update({ licenseKey: v, licenseValidated: false })} required={false} />
          <button className="wizard-mode-switch" type="button" role="switch" aria-checked={mode === "paid"}
            disabled={busy || (mode !== "paid" && !licenseKey.trim())}
            onClick={() => void changeMode(mode === "paid" ? "demo" : "paid", licenseKey.trim() || undefined)}>
            <span aria-hidden="true" /> {mode === "paid" ? "Live mode enabled" : "Switch to live mode"}
          </button>
          {mode === "paid" && <p className="wizard-success-text">Live mode is server-validated. Real gateways can be configured in Step 3.</p>}
        </div>
      </div>
    </div>
  );
}

// ─── Step 1: University ───────────────────────────────────────────────────────
function UniversityStep({ value, update }: { value: WizardData; update: (v: WizardData) => void }) {
  const country = text(value, "country");
  const provinceOptions = PROVINCES[country] ?? [];

  return (
    <div className="wizard-form-grid">
      <Field label="University full name" value={text(value, "name")} onChange={v => update({ name: v })} />
      <Field label="Short name / acronym" value={text(value, "shortName")} onChange={v => update({ shortName: v })} />
      <Field label="University type" value={text(value, "type")} onChange={v => update({ type: v })} />
      <Field label="Country" value={country} onChange={v => update({ country: v, province: "" })} />
      <Field label="City" value={text(value, "city")} onChange={v => update({ city: v })} />
      <Field label="Street address" value={text(value, "street")} onChange={v => update({ street: v })} />

      {/* Province/State — dropdown if known country, text fallback */}
      {provinceOptions.length > 0 ? (
        <SelectField
          label="Province / state"
          value={text(value, "province")}
          onChange={v => update({ province: v })}
          options={provinceOptions.map(p => ({ value: p, label: p }))}
          placeholder="Select province / state"
        />
      ) : (
        <Field label="Province / state" value={text(value, "province")} onChange={v => update({ province: v })} />
      )}

      {/* Timezone — full IANA dropdown */}
      <SelectField
        label="Timezone"
        value={text(value, "timezone")}
        onChange={v => update({ timezone: v })}
        options={TIMEZONES.map(tz => ({ value: tz, label: tz }))}
        placeholder="Select timezone"
      />

      <Field label="Primary language" value={text(value, "language")} onChange={v => update({ language: v })} />
      <Field label="Primary contact name" value={text(value, "contactName")} onChange={v => update({ contactName: v })} />
      <Field label="Primary contact email" value={text(value, "contactEmail")} onChange={v => update({ contactEmail: v })} type="email" />
      <Field label="Primary contact phone" value={text(value, "contactPhone")} onChange={v => update({ contactPhone: v })} />
    </div>
  );
}

// ─── Step 2: Academic structure ───────────────────────────────────────────────
function AcademicStep({ value, update }: { value: WizardData; update: (v: WizardData) => void }) {
  const faculties = (value.faculties as Row[] | undefined) ?? [];
  const departments = (value.departments as Row[] | undefined) ?? [];
  const programmes = (value.programmes as Row[] | undefined) ?? [];

  const add = (key: string, list: Row[]) => update({ [key]: [...list, { name: "", code: "" }] });
  const edit = (key: string, list: Row[], index: number, field: keyof Row, fieldValue: string) =>
    update({ [key]: list.map((row, i) => i === index ? { ...row, [field]: String(fieldValue) } : row) });
  const remove = (key: string, list: Row[], index: number) =>
    update({ [key]: list.filter((_, i) => i !== index) });

  const list = (key: string, rows: Row[]) => (
    <div className="wizard-list">
      {rows.map((row, index) => (
        <div className="wizard-row" key={`${key}-${index}`}>
          <input aria-label={`${key} name`} value={row.name} onChange={e => edit(key, rows, index, "name", e.target.value)} placeholder="Name" />
          <input aria-label={`${key} code`} value={row.code} onChange={e => edit(key, rows, index, "code", e.target.value)} placeholder="Code" />
          <button aria-label={`Remove ${key}`} className="icon-button" onClick={() => remove(key, rows, index)}><Trash2 size={16} /></button>
        </div>
      ))}
    </div>
  );

  return (
    <div className="wizard-sections">
      <section>
        <h3>Faculties / schools</h3>
        {list("faculties", faculties)}
        <button className="link-button" onClick={() => add("faculties", faculties)}><Plus size={16} /> Add faculty</button>
      </section>
      <section>
        <h3>Departments</h3>
        {list("departments", departments)}
        <button className="link-button" onClick={() => add("departments", departments)}><Plus size={16} /> Add department</button>
      </section>
      <section>
        <h3>Programmes</h3>
        {list("programmes", programmes)}
        <button className="link-button" onClick={() => add("programmes", programmes)}><Plus size={16} /> Add programme</button>
      </section>

      <div className="wizard-form-grid">
        {/* Calendar type — dropdown */}
        <SelectField
          label="Calendar type"
          value={text(value, "calendarType")}
          onChange={v => update({ calendarType: v })}
          options={CALENDAR_TYPES.map(c => ({ value: c, label: c }))}
          placeholder="Select calendar type"
        />

        {/* Academic year — dropdown */}
        <SelectField
          label="Academic year"
          value={text(value, "academicYear")}
          onChange={v => update({ academicYear: v })}
          options={academicYearOptions().map(y => ({ value: y, label: y }))}
          placeholder="Select academic year"
        />

        <Field label="Semester start" value={text(value, "semesterStart")} onChange={v => update({ semesterStart: v })} type="date" />
        <Field label="Semester end" value={text(value, "semesterEnd")} onChange={v => update({ semesterEnd: v })} type="date" />
      </div>
    </div>
  );
}

// ─── Step 3: Gateways (unchanged logic, kept in full) ─────────────────────────
function GatewayStep({ value, update, mode }: { value: WizardData; update: (v: WizardData) => void; mode: Mode }) {
  const enabled = (value.enabledGateways as string[] | undefined) ?? [];
  const configured = (value.configuredGateways as string[] | undefined) ?? [];
  const [editing, setEditing] = useState<string | null>(null);
  const [fields, setFields] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [health, setHealth] = useState<Record<string, string>>({});
  const fieldNames = mode === "demo" ? ["ENV"] : editing === "Raast RTP"
    ? ["MERCHANT_ID", "CLIENT_ID", "CLIENT_SECRET", "IBAN", "WEBHOOK_SECRET", "ENV"]
    : ["CLIENT_ID", "CLIENT_SECRET", "WEBHOOK_SECRET", "ENV"];
  const visibleFields = fields.ENV === "production" ? [...fieldNames, "PQC_ALGORITHM", "PQC_PUBLIC_KEY"] : fieldNames;
  const slug = (name: string) => name === "Raast RTP" ? "raast" : name.toLowerCase().replaceAll(" ", "-");
  const open = (name: string) => { setEditing(name); setFields({ ENV: "sandbox" }); setError(""); };

  const save = async () => {
    if (!editing || visibleFields.some(f => !fields[f]?.trim())) { setError("Complete every required gateway field."); return; }
    setSaving(true); setError("");
    try {
      const result = await apiPost<{ success: boolean; healthStatus?: string; latencyMs?: number }>(
        `/api/wizard/gateways/${slug(editing)}/configure`, fields
      );
      if (!result.success) { setError(`Gateway health check failed${result.healthStatus ? `: ${result.healthStatus}` : "."}`); return; }
      setHealth(cur => ({ ...cur, [editing!]: `Connected${typeof result.latencyMs === "number" ? ` · ${result.latencyMs}ms` : ""}` }));
      update({
        enabledGateways: enabled.includes(editing) ? enabled : [...enabled, editing],
        configuredGateways: configured.includes(editing) ? configured : [...configured, editing],
      });
      setEditing(null);
    } catch (e) { setError(e instanceof Error ? e.message : "Gateway configuration failed."); }
    finally { setSaving(false); }
  };

  const test = async (name: string) => {
    setHealth(cur => ({ ...cur, [name]: "Checking..." }));
    try {
      const result = await apiPost<{ success: boolean; healthStatus?: string; latencyMs?: number; error?: string }>(
        `/api/wizard/gateways/${slug(name)}/health-check`, {}
      );
      setHealth(cur => ({ ...cur, [name]: result.success ? `Connected${typeof result.latencyMs === "number" ? ` · ${result.latencyMs}ms` : ""}` : result.error ?? result.healthStatus ?? "Health check failed" }));
    } catch (e) { setHealth(cur => ({ ...cur, [name]: e instanceof Error ? e.message : "Health check failed" })); }
  };

  const toggle = (name: string) => update({ enabledGateways: enabled.includes(name) ? enabled.filter(i => i !== name) : [...enabled, name] });
  const skipDemo = () => update({ skippedInDemo: true, enabledGateways: enabled.length > 0 ? enabled : ["Sandbox Gateway"] });

  return (
    <div>
      {mode === "demo" ? (
        <div style={{ backgroundColor: "#fffbeb", border: "1px solid #fcd34d", borderRadius: "0.75rem", padding: "1rem", color: "#92400e", fontSize: "0.875rem", marginBottom: "1.25rem" }}>
          <div style={{ fontWeight: 700, marginBottom: "0.5rem" }}>SANDBOX MODE ACTIVE</div>
          <div>Payment gateways run with simulated sandbox credentials in Demo. Real money never moves.</div>
          <button type="button" className="wizard-secondary" style={{ marginTop: "0.5rem", fontSize: "0.8rem", padding: "4px 12px", minHeight: "32px" }} onClick={skipDemo}>
            Use Sandbox Gateway
          </button>
        </div>
      ) : (
        <p className="wizard-muted" style={{ marginBottom: "1rem" }}>At least one payment gateway must be configured to proceed.</p>
      )}
      <div className="wizard-grid two">
        {gateways.map(name => {
          const isConfigured = configured.includes(name);
          const isEnabled = enabled.includes(name);
          return (
            <div className="wizard-gateway" key={name}>
              <div>
                <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
                  {mode === "demo" && <ShieldCheck size={14} style={{ color: "#16a34a" }} />}
                  <b>{name}</b>
                </div>
                <span style={{ fontSize: "0.75rem", padding: "2px 6px", borderRadius: 4, fontWeight: 600, backgroundColor: isConfigured ? "#dcfce7" : "#f3f4f6", color: isConfigured ? "#166534" : "#4b5563" }}>
                  {isConfigured ? "CONFIGURED ✓" : "NOT CONFIGURED"}
                </span>
                <small>{health[name] ?? (isConfigured ? "Configured" : "Not configured")}</small>
              </div>
              <div className="wizard-gateway-actions">
                <button className="toggle" onClick={() => toggle(name)}>{isEnabled ? "ON" : "OFF"}</button>
                <button className="link-button" onClick={() => open(name)}>Configure</button>
                {isConfigured && (
                  <button disabled={health[name] === "Checking..."} className="link-button" onClick={() => test(name)}>Test connection</button>
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
            {mode === "demo" && <p className="wizard-muted">Demo saves sandbox credentials automatically.</p>}
            {visibleFields.map(f => (
              <Field key={f} label={f.replaceAll("_", " ")} value={fields[f] ?? ""}
                onChange={v => setFields(cur => ({ ...cur, [f]: String(v) }))}
                type={f.includes("SECRET") || f === "PQC_PUBLIC_KEY" ? "password" : "text"} />
            ))}
            {fields.ENV === "production" && <div className="wizard-error">Live mode requires an ML-DSA-65 public key for webhook verification.</div>}
            {error && <p className="wizard-error">{error}</p>}
            <div className="wizard-footer">
              <button className="wizard-secondary" onClick={() => setEditing(null)}>Cancel</button>
              <button className="wizard-primary" disabled={saving || visibleFields.some(f => !fields[f]?.trim())} onClick={save}>
                {saving ? "Saving..." : "Save gateway"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ─── Step 4: Finance ──────────────────────────────────────────────────────────
function FinanceStep({ value, update }: { value: WizardData; update: (v: WizardData) => void }) {
  const standard = text(value, "standard") || "IFRS";
  const source = text(value, "rateSource") || "ECB";
  const baseCurrency = text(value, "baseCurrency") || "PKR";

  return (
    <div className="wizard-form-grid">
      <SelectField
        label="Primary accounting standard"
        value={standard}
        onChange={v => update({ standard: v })}
        options={[
          { value: "IFRS", label: "IFRS" },
          { value: "US_GAAP", label: "US GAAP" },
          { value: "FRS_102", label: "FRS 102" },
          { value: "IPSAS", label: "IPSAS" },
          { value: "AAOIFI", label: "AAOIFI" },
        ]}
      />

      <Field label="Fiscal year label" value={text(value, "fiscalYear")} onChange={v => update({ fiscalYear: v })} />
      <Field label="Fiscal year start month" value={text(value, "fiscalYearStartMonth")} onChange={v => update({ fiscalYearStartMonth: v })} type="number" />

      {/* Base currency — full ISO 4217 dropdown, PKR default */}
      <SelectField
        label="Base currency"
        value={baseCurrency}
        onChange={v => update({ baseCurrency: v })}
        options={CURRENCIES.map(c => ({ value: c.code, label: `${c.code} — ${c.name}` }))}
      />

      <SelectField
        label="Exchange rate source"
        value={source}
        onChange={v => update({ rateSource: v })}
        options={[
          { value: "ECB", label: "ECB" },
          { value: "SBP", label: "SBP" },
          { value: "OPEN_EXCHANGE_RATES", label: "Open Exchange Rates" },
          { value: "MANUAL", label: "Manual" },
        ]}
      />

      {source === "OPEN_EXCHANGE_RATES" && (
        <Field label="Open Exchange Rates API key" value={text(value, "apiKey")} onChange={v => update({ apiKey: v })} type="password" />
      )}
    </div>
  );
}

// ─── Step 5: Fees ─────────────────────────────────────────────────────────────
function FeesStep({ value, update }: { value: WizardData; update: (v: WizardData) => void }) {
  const heads = (value.feeHeads as Row[] | undefined) ?? [];
  return (
    <div className="wizard-sections">
      <section>
        <h3>Fee heads</h3>
        {heads.map((head, index) => (
          <div className="wizard-row" key={index}>
            <input value={head.name} placeholder="Tuition fee"
              onChange={e => update({ feeHeads: heads.map((item, i) => i === index ? { ...item, name: e.target.value } : item) })} />
            <input value={head.code} placeholder="TF"
              onChange={e => update({ feeHeads: heads.map((item, i) => i === index ? { ...item, code: e.target.value } : item) })} />
          </div>
        ))}
        <button className="link-button" onClick={() => update({ feeHeads: [...heads, { name: "", code: "" }] })}>
          <Plus size={16} /> Add fee head
        </button>
      </section>
      <Field label="Programme assignment amount" value={text(value, "assignmentAmount")}
        onChange={v => update({ assignmentAmount: v, assignments: [{ programme: "default", amount: v }] })} type="number" />
    </div>
  );
}

// ─── Step 6: Users ────────────────────────────────────────────────────────────
function UsersStep({ value, update }: { value: WizardData; update: (v: WizardData) => void }) {
  const users = (value.users as User[] | undefined) ?? [];
  const roles = ["Super Admin", "Finance Controller", "Head of Department", "Admissions Head", "Academic Head", "HR Head", "IT Head"];
  const hasRole = (role: string) => users.some(u => u.role === role);

  return (
    <div className="wizard-sections">
      {users.map((user, index) => (
        <div className="wizard-row" key={index}>
          <input value={user.name} placeholder="Full name"
            onChange={e => update({ users: users.map((item, i) => i === index ? { ...item, name: e.target.value } : item) })} />
          <input value={user.email} placeholder="Email"
            onChange={e => update({ users: users.map((item, i) => i === index ? { ...item, email: e.target.value } : item) })} />
          {/* Password with eye toggle */}
          <PasswordInput
            value={user.password ?? ""}
            placeholder="Temporary password (min 12 chars)"
            onChange={v => update({ users: users.map((item, i) => i === index ? { ...item, password: v } : item) })}
          />
          <select value={user.role}
            onChange={e => update({ users: users.map((item, i) => i === index ? { ...item, role: e.target.value as User["role"] } : item) })}>
            {roles.map(role => <option key={role}>{role}</option>)}
          </select>
          <button aria-label="Remove user" className="icon-button"
            onClick={() => update({ users: users.filter((_, i) => i !== index) })}>
            <Trash2 size={16} />
          </button>
        </div>
      ))}
      <p className="wizard-muted" style={{ fontSize: "0.8rem", margin: "4px 0" }}>
        Super Admin and Finance Controller are required. Add department heads for scoped access.
      </p>
      <button className="link-button"
        onClick={() => update({ users: [...users, { name: "", email: "", password: "", role: roles.find(r => !hasRole(r)) ?? "Head of Department" }] })}>
        <Plus size={16} /> Add administrator
      </button>
    </div>
  );
}

// ─── Step 7: Review ───────────────────────────────────────────────────────────
function ReviewStep({ data, value, update }: { data: Record<string, WizardData>; value: WizardData; update: (v: WizardData) => void }) {
  const legal = value.legalAccepted === true;
  const terms = value.termsAccepted === true;
  const stepNames = steps.slice(0, 8);
  return (
    <div className="wizard-review">
      <div className="wizard-review-grid">
        {stepNames.slice(0, 7).map((label, index) => (
          <article key={label}>
            <span>STEP {index + 1}</span>
            <h3>{label}</h3>
            <p>{Object.keys(data[`step${index}`] ?? {}).length} configured fields</p>
          </article>
        ))}
      </div>
      <label className="wizard-check">
        <input type="checkbox" checked={legal}
          onChange={e => update({ legalAccepted: e.target.checked, confirmed: e.target.checked && terms })} />
        I confirm the information is accurate and I am authorised to set up AegisCore ERP.
      </label>
      <label className="wizard-check">
        <input type="checkbox" checked={terms}
          onChange={e => update({ termsAccepted: e.target.checked, confirmed: legal && e.target.checked })} />
        I accept the Terms of Service and Privacy Policy.
      </label>
    </div>
  );
}

// ─── Step 8: Secrets ─────────────────────────────────────────────────────────
function SecretsStep({ value, update, mode }: { value: WizardData; update: (v: WizardData) => void; mode: Mode }) {
  const isDemo = mode === "demo";
  const vaultProvider = (value.vaultProvider as VaultProvider | undefined) ?? "FILE";

  return (
    <div className="wizard-sections">
      {/* Vault provider selector */}
      <section>
        <h3>Secrets vault</h3>
        <p className="wizard-muted" style={{ marginBottom: "0.75rem", fontSize: "0.875rem" }}>
          Choose where AegisCore reads and writes runtime secrets. All secrets are encrypted with ML-KEM-768 (PQC) before storage and passwords are hashed with Argon2id.
        </p>
        <SelectField
          label="Vault provider"
          value={vaultProvider}
          onChange={v => update({ vaultProvider: v as VaultProvider })}
          options={VAULT_PROVIDERS.map(p => ({ value: p.value, label: p.label }))}
        />
        {vaultProvider === "INFISICAL" && (
          <div className="wizard-form-grid" style={{ marginTop: "1rem" }}>
            <Field label="Infisical URL" value={text(value, "infisicalUrl")} onChange={v => update({ infisicalUrl: v })} placeholder="https://app.infisical.com" required />
            <Field label="Service token" value={text(value, "infisicalToken")} onChange={v => update({ infisicalToken: v })} type="password" required />
            <Field label="Workspace ID" value={text(value, "infisicalWorkspaceId")} onChange={v => update({ infisicalWorkspaceId: v })} required />
            <Field label="Environment" value={text(value, "infisicalEnvironment")} onChange={v => update({ infisicalEnvironment: v })} placeholder="production" required />
          </div>
        )}
        {vaultProvider === "HASHICORP_VAULT" && (
          <div className="wizard-form-grid" style={{ marginTop: "1rem" }}>
            <Field label="Vault address" value={text(value, "vaultAddr")} onChange={v => update({ vaultAddr: v })} placeholder="https://vault.example.com" required />
            <Field label="Vault token" value={text(value, "vaultToken")} onChange={v => update({ vaultToken: v })} type="password" required />
            <Field label="Mount path" value={text(value, "vaultMount")} onChange={v => update({ vaultMount: v })} placeholder="secret" required />
            <Field label="Secret path" value={text(value, "vaultPath")} onChange={v => update({ vaultPath: v })} placeholder="aegiscore" required />
          </div>
        )}
        {vaultProvider === "AZURE_KEYVAULT" && (
          <div className="wizard-form-grid" style={{ marginTop: "1rem" }}>
            <Field label="Key Vault URL" value={text(value, "azureVaultUrl")} onChange={v => update({ azureVaultUrl: v })} placeholder="https://myvault.vault.azure.net/" required />
            <Field label="Tenant ID" value={text(value, "azureTenantId")} onChange={v => update({ azureTenantId: v })} required />
            <Field label="Client ID" value={text(value, "azureClientId")} onChange={v => update({ azureClientId: v })} required />
            <Field label="Client secret" value={text(value, "azureClientSecret")} onChange={v => update({ azureClientSecret: v })} type="password" required />
          </div>
        )}
        {vaultProvider === "AWS_SECRETS_MANAGER" && (
          <div className="wizard-form-grid" style={{ marginTop: "1rem" }}>
            <Field label="AWS region" value={text(value, "awsRegion")} onChange={v => update({ awsRegion: v })} placeholder="ap-southeast-1" required />
            <Field label="Access key ID" value={text(value, "awsAccessKeyId")} onChange={v => update({ awsAccessKeyId: v })} required />
            <Field label="Secret access key" value={text(value, "awsSecretAccessKey")} onChange={v => update({ awsSecretAccessKey: v })} type="password" required />
          </div>
        )}
        {vaultProvider === "GCP_SECRET_MANAGER" && (
          <div className="wizard-form-grid" style={{ marginTop: "1rem" }}>
            <Field label="GCP project ID" value={text(value, "gcpProjectId")} onChange={v => update({ gcpProjectId: v })} required />
            <Field label="Service account JSON (base64)" value={text(value, "gcpCredentials")} onChange={v => update({ gcpCredentials: v })} type="password" required />
          </div>
        )}
      </section>

      {/* Infrastructure */}
      <section>
        <h3>Infrastructure connections</h3>
        <div className="wizard-form-grid">
          <Field
            label="PostgreSQL connection string"
            value={text(value, "dbConnectionString")}
            onChange={v => update({ dbConnectionString: v })}
            type="password"
          />
          <Field
            label="Redis connection string"
            value={text(value, "redisConnectionString")}
            onChange={v => update({ redisConnectionString: v })}
            type="password"
          />
        </div>
      </section>

      {/* License key — blurred/disabled in demo */}
      <section>
        <h3>License key</h3>
        {isDemo ? (
          <div style={{ position: "relative" }}>
            <input
              type="text"
              value="AE-DEMO-XXXX-XXXX-XXXX"
              readOnly
              style={{ filter: "blur(4px)", userSelect: "none", pointerEvents: "none", width: "100%", padding: "0.5rem 0.75rem", borderRadius: 6, border: "1px solid #d1d5db" }}
            />
            <div style={{ position: "absolute", inset: 0, display: "flex", alignItems: "center", justifyContent: "center" }}>
              <span style={{ background: "#fef3c7", color: "#92400e", fontSize: "0.75rem", fontWeight: 700, padding: "2px 10px", borderRadius: 20, border: "1px solid #fcd34d" }}>
                NOT REQUIRED IN DEMO
              </span>
            </div>
          </div>
        ) : (
          <Field label="AegisCore license key" value={text(value, "licenseKey")} onChange={v => update({ licenseKey: v })} type="password" />
        )}
      </section>

      {/* Banking / payment API keys — optional, system works without them */}
      <section>
        <h3>Payment gateway API keys <span style={{ fontWeight: 400, fontSize: "0.8rem", color: "#6b7280" }}>(optional — system works without these)</span></h3>
        <p className="wizard-muted" style={{ fontSize: "0.85rem", marginBottom: "0.75rem" }}>
          Banking integrations degrade gracefully. AegisCore will operate in manual payment mode if keys are not provided.
        </p>
        <div className="wizard-form-grid">
          {isDemo ? (
            <div style={{ gridColumn: "1 / -1", opacity: 0.4, pointerEvents: "none", filter: "blur(2px)" }}>
              <Field label="Raast RTP — Merchant ID" value="" onChange={() => {}} required={false} />
              <Field label="Raast RTP — Client secret" value="" onChange={() => {}} type="password" required={false} />
              <Field label="Stripe secret key" value="" onChange={() => {}} type="password" required={false} />
            </div>
          ) : (
            <>
              <Field label="Raast RTP — Merchant ID" value={text(value, "raastMerchantId")} onChange={v => update({ raastMerchantId: v })} required={false} />
              <Field label="Raast RTP — Client ID" value={text(value, "raastClientId")} onChange={v => update({ raastClientId: v })} required={false} />
              <Field label="Raast RTP — Client secret" value={text(value, "raastClientSecret")} onChange={v => update({ raastClientSecret: v })} type="password" required={false} />
              <Field label="JazzCash Merchant ID" value={text(value, "jazzcashMerchantId")} onChange={v => update({ jazzcashMerchantId: v })} required={false} />
              <Field label="JazzCash password" value={text(value, "jazzcashPassword")} onChange={v => update({ jazzcashPassword: v })} type="password" required={false} />
              <Field label="EasyPaisa store ID" value={text(value, "easypaisaStoreId")} onChange={v => update({ easypaisaStoreId: v })} required={false} />
              <Field label="EasyPaisa hash key" value={text(value, "easypaisaHashKey")} onChange={v => update({ easypaisaHashKey: v })} type="password" required={false} />
              <Field label="Stripe secret key" value={text(value, "stripeSecretKey")} onChange={v => update({ stripeSecretKey: v })} type="password" required={false} />
              <Field label="1Link credentials" value={text(value, "oneLinkCredentials")} onChange={v => update({ oneLinkCredentials: v })} type="password" required={false} />
            </>
          )}
        </div>
        {isDemo && (
          <p style={{ fontSize: "0.78rem", color: "#92400e", marginTop: "0.5rem" }}>
            Banking API keys are not configurable in demo mode. Upgrade to a licensed instance to activate live payment integrations.
          </p>
        )}
      </section>

      <div style={{ background: "#f0fdf4", border: "1px solid #bbf7d0", borderRadius: 8, padding: "0.75rem 1rem", fontSize: "0.8rem", color: "#166534", display: "flex", gap: "0.5rem", alignItems: "flex-start" }}>
        <ShieldCheck size={15} style={{ marginTop: 1, flexShrink: 0 }} />
        <span>All secrets are encrypted with ML-KEM-768 before writing to the vault. Passwords are hashed with Argon2id (t=3, m=65536, p=4). Keys never leave this deployment.</span>
      </div>
    </div>
  );
}

// ─── Password input with eye toggle (standalone) ──────────────────────────────
function PasswordInput({ value, onChange, placeholder }: { value: string; onChange: (v: string) => void; placeholder?: string }) {
  const [show, setShow] = useState(false);
  return (
    <div style={{ position: "relative", display: "flex", alignItems: "center" }}>
      <input
        type={show ? "text" : "password"}
        value={value}
        placeholder={placeholder}
        style={{ width: "100%", paddingRight: "2.25rem" }}
        onChange={e => onChange(e.target.value)}
      />
      <button
        type="button"
        onClick={() => setShow(s => !s)}
        style={{ position: "absolute", right: "0.5rem", background: "none", border: "none", cursor: "pointer", color: "#6b7280", padding: 0, display: "flex", alignItems: "center" }}
        aria-label={show ? "Hide password" : "Show password"}
      >
        {show ? <EyeOff size={15} /> : <Eye size={15} />}
      </button>
    </div>
  );
}
