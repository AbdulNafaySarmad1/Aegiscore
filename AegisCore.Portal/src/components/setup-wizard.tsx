"use client";

import {
  ArrowLeft,
  ArrowRight,
  Building2,
  Check,
  CheckCircle2,
  Circle,
  Database,
  Download,
  FileCheck2,
  KeyRound,
  LoaderCircle,
  Network,
  Plus,
  Rocket,
  Server,
  ShieldCheck,
  Trash2,
  Upload
} from "lucide-react";
import Link from "next/link";
import { ChangeEvent, FormEvent, useEffect, useMemo, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";

type Notice = { kind: "success" | "error" | "info"; text: string } | null;
type Template = { key: string; displayName: string; columns: string[]; rules: string[] };
type Department = { id: string; name: string; code: string; hodName: string; facultyCount: number };
type Permission = { role: string; feature: string; allowed: boolean };
type Validation = { fileType: string; isValid: boolean; rowCount: number; errors: string[]; warnings: string[] };
type Review = {
  ready: boolean;
  checks: Array<{ name: string; passed: boolean; detail: string }>;
};

type DemoImportResult = {
  success: boolean;
  connectionId: string;
  tablesDiscovered: number;
  tablesAutoMapped: number;
  autoMappedTables: Array<{ table: string; entityType: string }>;
  results: Array<{
    table: string; entityType: string; status?: string; reason?: string;
    id?: string; totalRows?: number; successRows?: number; errorRows?: number; skippedRows?: number;
  }>;
  note?: string;
};

type DemoStatus = { isDemo: boolean; expiryUtc?: string; remainingDays?: number };

const stepLabels = ["Institution", "Infrastructure", "Templates", "Hierarchy", "Import", "Access", "Review"];
const requiredFiles = ["departments", "students", "faculty", "courses"];
const allFiles = ["departments", "students", "faculty", "employees", "programs", "courses", "enrollments", "attendance", "examinations", "marks", "financial"];

const emptyInstitution = {
  name: "", shortName: "", domain: "", licenseKey: "",
  adminName: "", adminEmail: "", adminPassword: "", adminConfirm: ""
};

const defaultInfrastructure = {
  dbHost: "postgres", dbPort: "5432", dbName: "aegiscore", dbUser: "aegiscore", dbPassword: "", dbSsl: false,
  redisHost: "redis", redisPort: "6379", redisPassword: "", redisTls: false,
  smtpHost: "", smtpPort: "587", smtpUser: "", smtpPassword: "", smtpFrom: "", smtpName: "AegisCore"
};

export function SetupWizard() {
  const [step, setStep] = useState(0);
  const [highestStep, setHighestStep] = useState(0);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<Notice>(null);
  const [installed, setInstalled] = useState(false);
  const [demoMode, setDemoMode] = useState(false);
  const [showDbImport, setShowDbImport] = useState(false);
  const [dbConn, setDbConn] = useState({ connectionString: "", dbType: "PostgreSQL" });
  const [importResult, setImportResult] = useState<DemoImportResult | null>(null);
  const [institution, setInstitution] = useState(emptyInstitution);
  const [infra, setInfra] = useState(defaultInfrastructure);
  const [templates, setTemplates] = useState<Template[]>([]);
  const [viceChancellor, setViceChancellor] = useState("");
  const [departments, setDepartments] = useState<Department[]>([newDepartment()]);
  const [validations, setValidations] = useState<Record<string, Validation>>({});
  const [permissions, setPermissions] = useState<Permission[]>([]);
  const [review, setReview] = useState<Review | null>(null);

  useEffect(() => {
    apiGet<{ isInstalled: boolean; currentStep: number }>("/api/setup/status")
      .then(status => {
        setInstalled(status.isInstalled);
        if (!status.isInstalled) {
          const suggested = Math.min(Math.max(status.currentStep - 1, 0), 6);
          setStep(suggested);
          setHighestStep(suggested);
        }
      })
      .catch(error => setNotice({ kind: "error", text: error instanceof Error ? error.message : "Setup API is unavailable." }));
    apiGet<DemoStatus>("/api/wizard/demo-status")
      .then(s => setDemoMode(s.isDemo))
      .catch(() => {});
  }, []);

  useEffect(() => {
    if (step === 2 && templates.length === 0) {
      apiGet<Template[]>("/api/setup/enhanced/templates")
        .then(setTemplates)
        .catch(error => setNotice(errorNotice(error)));
    }
    if (step === 5 && permissions.length === 0) {
      apiGet<Permission[]>("/api/setup/enhanced/permissions")
        .then(setPermissions)
        .catch(error => setNotice(errorNotice(error)));
    }
    if (step === 6) refreshReview();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [step]);

  const requiredImportsReady = useMemo(
    () => requiredFiles.every(key => validations[key]?.isValid),
    [validations]
  );

  function advance(next = step + 1) {
    const bounded = Math.min(Math.max(next, 0), 6);
    setNotice(null);
    setStep(bounded);
    setHighestStep(current => Math.max(current, bounded));
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  async function run(action: () => Promise<void>) {
    setBusy(true);
    setNotice(null);
    try {
      await action();
    } catch (error) {
      setNotice(errorNotice(error));
    } finally {
      setBusy(false);
    }
  }

  async function saveInstitution(event: FormEvent) {
    event.preventDefault();
    await run(async () => {
      if (institution.adminPassword.length < 12) throw new Error("Use an administrator password of at least 12 characters.");
      if (institution.adminPassword !== institution.adminConfirm) throw new Error("Administrator passwords do not match.");
      const license = await apiPost<{ isValid: boolean; error?: { userMessage?: string } }>("/api/setup/step5/license-info", {
        licenseKey: institution.licenseKey,
        institutionName: institution.name,
        organization: institution.shortName,
        administratorEmail: institution.adminEmail
      });
      if (!license.isValid) throw new Error(license.error?.userMessage ?? "License validation failed.");
      await apiPost("/api/setup/step6/administrator", {
        fullName: institution.adminName,
        email: institution.adminEmail,
        password: institution.adminPassword,
        confirmPassword: institution.adminConfirm
      });
      await apiPost("/api/setup/step9/domain", { domain: institution.domain });
      setNotice({ kind: "success", text: "Institution, license, administrator, and public domain are validated." });
      advance();
    });
  }

  async function testInfrastructure(event: FormEvent) {
    event.preventDefault();
    await run(async () => {
      const postgres = await apiPost<{ success: boolean; message?: string; error?: { userMessage?: string } }>("/api/setup/step3/test-postgres", {
        host: infra.dbHost, port: Number(infra.dbPort), databaseName: infra.dbName,
        username: infra.dbUser, password: infra.dbPassword, sslEnabled: infra.dbSsl
      });
      if (!postgres.success) throw new Error(postgres.error?.userMessage ?? "PostgreSQL test failed.");

      const redis = await apiPost<{ success: boolean; message?: string; error?: { userMessage?: string } }>("/api/setup/step4/test-redis", {
        host: infra.redisHost, port: Number(infra.redisPort), password: infra.redisPassword || null,
        tlsEnabled: infra.redisTls, testConnection: true
      });
      if (!redis.success) throw new Error(redis.error?.userMessage ?? "Redis test failed.");

      if (infra.smtpHost) {
        const smtp = await apiPost<{ success: boolean; error?: { userMessage?: string } }>("/api/setup/step7/test-smtp", {
          host: infra.smtpHost, port: Number(infra.smtpPort), username: infra.smtpUser,
          password: infra.smtpPassword, fromAddress: infra.smtpFrom, fromName: infra.smtpName, testConnection: true
        });
        if (!smtp.success) throw new Error(smtp.error?.userMessage ?? "SMTP test failed.");
      }
      setNotice({ kind: "success", text: "PostgreSQL and Redis are reachable. Infrastructure settings are saved." });
      advance();
    });
  }

  async function saveHierarchy() {
    await run(async () => {
      const saved = await apiPost<{ viceChancellor: string; departments: Department[] }>("/api/setup/enhanced/hierarchy", {
        viceChancellor,
        departments
      });
      setViceChancellor(saved.viceChancellor);
      setDepartments(saved.departments);
      setNotice({ kind: "success", text: "University hierarchy saved with an auditable organization boundary." });
      advance();
    });
  }

  async function validateFile(type: string, event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (!file) return;
    await run(async () => {
      const data = new FormData();
      data.append("file", file);
      const result = await apiPost<Validation>(`/api/setup/enhanced/uploads/${type}/validate`, data);
      setValidations(current => ({ ...current, [type]: result }));
      setNotice({
        kind: result.isValid ? "success" : "error",
        text: result.isValid
          ? `${result.rowCount.toLocaleString()} ${type} rows passed validation.`
          : `${result.errors.length} validation issue${result.errors.length === 1 ? "" : "s"} found in ${type}.`
      });
    });
  }

  async function savePermissions() {
    await run(async () => {
      const saved = await apiPost<Permission[]>("/api/setup/enhanced/permissions", { permissions });
      setPermissions(saved);
      setNotice({ kind: "success", text: "Role permission baseline is saved." });
      advance();
    });
  }

  async function refreshReview() {
    setBusy(true);
    try {
      setReview(await apiGet<Review>("/api/setup/enhanced/review"));
    } catch (error) {
      setNotice(errorNotice(error));
    } finally {
      setBusy(false);
    }
  }

  async function runDbImport() {
    await run(async () => {
      const result = await apiPost<DemoImportResult>("/api/wizard/import-demo-data", {
        connectionString: dbConn.connectionString,
        dbType: dbConn.dbType
      });
      setImportResult(result);
      if (result.success) {
        setNotice({ kind: "success", text: `Imported data from ${result.tablesAutoMapped} auto-mapped tables across ${result.results.filter(r => r.status !== "Skipped").length} entities.` });
      }
    });
  }

  async function goLive() {
    await run(async () => {
      const result = await apiPost<{ success: boolean; erpUrl?: string; error?: { userMessage?: string } }>("/api/setup/step10/install", {
        skipOptionalIntegrations: true
      });
      if (!result.success) throw new Error(result.error?.userMessage ?? "Go-live checks failed.");
      setNotice({ kind: "success", text: `AegisCore is activated${result.erpUrl ? ` at ${result.erpUrl}` : ""}. Restart the API container to enter normal operating mode.` });
      setInstalled(true);
    });
  }

  if (installed) {
    return (
      <main className="setup-complete-screen">
        <div className="completion-orbit"><CheckCircle2 /></div>
        <p>Installation complete</p>
        <h1>Your university workspace is ready.</h1>
        <span>AegisCore has sealed the setup state. Restart the API container once, then enter the portal.</span>
        <Link className="primary-button" href="/">Open AegisCore <ArrowRight /></Link>
      </main>
    );
  }

  return (
    <div className="setup-shell">
      <aside className="setup-rail">
        <Link className="aegis-brand inverse" href="/">
          <span className="brand-glyph"><ShieldCheck /></span>
          <span><strong>AegisCore</strong><small>University launchpad</small></span>
        </Link>
        <div className="setup-intro">
          <span>Guided deployment</span>
          <h1>From infrastructure to a live university.</h1>
          <p>This launchpad validates every critical dependency before AegisCore accepts production traffic.</p>
        </div>
        <ol className="setup-progress">
          {stepLabels.map((label, index) => (
            <li key={label} className={index === step ? "active" : index < step ? "complete" : ""}>
              <button disabled={index > highestStep} onClick={() => advance(index)}>
                <i>{index < step ? <Check /> : index + 1}</i>
                <span><strong>{label}</strong><small>{stepDescription(index)}</small></span>
              </button>
            </li>
          ))}
        </ol>
        <div className="secure-setup-note"><ShieldCheck /><span><strong>Private by design</strong><small>Secrets remain inside this installation.</small></span></div>
      </aside>

      <main className="setup-main">
        <header className="setup-header">
          <span>Step {step + 1} of {stepLabels.length}</span>
          <div><i style={{ width: `${((step + 1) / stepLabels.length) * 100}%` }} /></div>
          <a href="mailto:support@aegiscore.pk">Need help?</a>
        </header>

        <section className="setup-content">
          <div className="setup-title">
            <span className="step-icon">{stepIcon(step)}</span>
            <div><p>{stepLabels[step]}</p><h2>{stepTitle(step)}</h2><span>{stepSubtitle(step)}</span></div>
          </div>

          {notice && <div className={`form-notice ${notice.kind}`} role="status">{notice.kind === "success" ? <CheckCircle2 /> : <Circle />}{notice.text}</div>}

          {step === 0 && (
            <form className="setup-form" onSubmit={saveInstitution}>
              <FormSection title="University identity" description="This identity appears across portals, documents, and official communication.">
                <Field label="Full university name" required><input required value={institution.name} onChange={e => setInstitution({ ...institution, name: e.target.value })} placeholder="e.g., Institute of Business Management" /></Field>
                <Field label="Short name" required><input required value={institution.shortName} onChange={e => setInstitution({ ...institution, shortName: e.target.value })} placeholder="e.g., IOBM" /></Field>
                <Field label="University domain" hint="We will prepare erp.yourdomain.edu.pk" required><input required value={institution.domain} onChange={e => setInstitution({ ...institution, domain: e.target.value })} placeholder="e.g., iobm.edu.pk" /></Field>
                <Field label="AegisCore license key" required><div className="input-with-icon"><KeyRound /><input required value={institution.licenseKey} onChange={e => setInstitution({ ...institution, licenseKey: e.target.value })} placeholder="AE-XXXX-XXXX-XXXX" /></div></Field>
              </FormSection>
              <FormSection title="First administrator" description="Create the accountable owner for this university installation.">
                <Field label="Full name" required><input required value={institution.adminName} onChange={e => setInstitution({ ...institution, adminName: e.target.value })} placeholder="System Administrator" /></Field>
                <Field label="Work email" required><input type="email" required value={institution.adminEmail} onChange={e => setInstitution({ ...institution, adminEmail: e.target.value })} placeholder="admin@university.edu.pk" /></Field>
                <Field label="Password" hint="Minimum 12 characters" required><input type="password" autoComplete="new-password" required value={institution.adminPassword} onChange={e => setInstitution({ ...institution, adminPassword: e.target.value })} /></Field>
                <Field label="Confirm password" required><input type="password" autoComplete="new-password" required value={institution.adminConfirm} onChange={e => setInstitution({ ...institution, adminConfirm: e.target.value })} /></Field>
              </FormSection>
              <WizardActions step={step} busy={busy} onBack={() => advance(step - 1)} />
            </form>
          )}

          {step === 1 && (
            <form className="setup-form" onSubmit={testInfrastructure}>
              <FormSection title="PostgreSQL" description="The authoritative store for university records.">
                <Field label="Host" required><input required value={infra.dbHost} onChange={e => setInfra({ ...infra, dbHost: e.target.value })} /></Field>
                <Field label="Port" required><input type="number" required value={infra.dbPort} onChange={e => setInfra({ ...infra, dbPort: e.target.value })} /></Field>
                <Field label="Database" required><input required value={infra.dbName} onChange={e => setInfra({ ...infra, dbName: e.target.value })} /></Field>
                <Field label="Username" required><input required value={infra.dbUser} onChange={e => setInfra({ ...infra, dbUser: e.target.value })} /></Field>
                <Field label="Password" required><input type="password" required value={infra.dbPassword} onChange={e => setInfra({ ...infra, dbPassword: e.target.value })} /></Field>
                <Toggle label="Require database TLS" checked={infra.dbSsl} onChange={checked => setInfra({ ...infra, dbSsl: checked })} />
              </FormSection>
              <FormSection title="Redis" description="Fast caching, distributed locks, and short-lived workflow state.">
                <Field label="Host" required><input required value={infra.redisHost} onChange={e => setInfra({ ...infra, redisHost: e.target.value })} /></Field>
                <Field label="Port" required><input type="number" required value={infra.redisPort} onChange={e => setInfra({ ...infra, redisPort: e.target.value })} /></Field>
                <Field label="Password"><input type="password" value={infra.redisPassword} onChange={e => setInfra({ ...infra, redisPassword: e.target.value })} /></Field>
                <Toggle label="Require Redis TLS" checked={infra.redisTls} onChange={checked => setInfra({ ...infra, redisTls: checked })} />
              </FormSection>
              <FormSection title="Outgoing email (optional now)" description="Leave the SMTP host empty to configure email after launch.">
                <Field label="SMTP host"><input value={infra.smtpHost} onChange={e => setInfra({ ...infra, smtpHost: e.target.value })} placeholder="smtp.office365.com" /></Field>
                <Field label="Port"><input type="number" value={infra.smtpPort} onChange={e => setInfra({ ...infra, smtpPort: e.target.value })} /></Field>
                <Field label="Username"><input value={infra.smtpUser} onChange={e => setInfra({ ...infra, smtpUser: e.target.value })} /></Field>
                <Field label="Password"><input type="password" value={infra.smtpPassword} onChange={e => setInfra({ ...infra, smtpPassword: e.target.value })} /></Field>
                <Field label="From address"><input type="email" value={infra.smtpFrom} onChange={e => setInfra({ ...infra, smtpFrom: e.target.value })} /></Field>
                <Field label="From name"><input value={infra.smtpName} onChange={e => setInfra({ ...infra, smtpName: e.target.value })} /></Field>
              </FormSection>
              <WizardActions step={step} busy={busy} onBack={() => advance(step - 1)} submitLabel="Test and continue" />
            </form>
          )}

          {step === 2 && (
            <>
              <div className="setup-callout"><Download /><span><strong>Start with clean source files</strong><small>Each workbook includes required headers, formatting rules, and one valid example row.</small></span><a href="/api/setup/enhanced/templates/all">Download all templates</a></div>
              <div className="template-grid">
                {templates.map(template => <article key={template.key}>
                  <div><FileCheck2 /><span><strong>{template.displayName}</strong><small>{template.columns.length} required columns</small></span></div>
                  <ul>{template.rules.slice(0, 3).map(rule => <li key={rule}>{rule}</li>)}</ul>
                  <a href={`/api/setup/enhanced/templates/${encodeURIComponent(template.key)}`}><Download /> Download .xlsx</a>
                </article>)}
              </div>
              <WizardActions step={step} busy={busy} onBack={() => advance(step - 1)} onNext={() => advance()} />
            </>
          )}

          {step === 3 && (
            <>
              <div className="organization-editor">
                <Field label="Vice Chancellor" required><input value={viceChancellor} onChange={e => setViceChancellor(e.target.value)} placeholder="Vice Chancellor name" /></Field>
                <div className="department-heading"><span><strong>Departments</strong><small>Codes must be unique across the university.</small></span><button onClick={() => setDepartments([...departments, newDepartment()])}><Plus /> Add department</button></div>
                <div className="department-list">
                  {departments.map((department, index) => (
                    <div className="department-card" key={department.id}>
                      <i>{department.code || String(index + 1).padStart(2, "0")}</i>
                      <Field label="Department name"><input value={department.name} onChange={e => updateDepartment(index, "name", e.target.value, departments, setDepartments)} placeholder="Computer Science" /></Field>
                      <Field label="Code"><input value={department.code} onChange={e => updateDepartment(index, "code", e.target.value.toUpperCase(), departments, setDepartments)} placeholder="CS" /></Field>
                      <Field label="Head of department"><input value={department.hodName} onChange={e => updateDepartment(index, "hodName", e.target.value, departments, setDepartments)} placeholder="May be assigned later" /></Field>
                      <button className="remove-button" disabled={departments.length === 1} onClick={() => setDepartments(departments.filter((_, itemIndex) => itemIndex !== index))} aria-label={`Remove ${department.name || "department"}`}><Trash2 /></button>
                    </div>
                  ))}
                </div>
              </div>
              <WizardActions step={step} busy={busy} onBack={() => advance(step - 1)} onNext={saveHierarchy} submitLabel="Save hierarchy" />
            </>
          )}

          {step === 4 && (
            <>
              <div className="import-summary">
                <span><strong>{requiredFiles.filter(key => validations[key]?.isValid).length}/{requiredFiles.length}</strong><small>Required datasets valid</small></span>
                <p>Upload completed templates. Validation never imports partial or malformed records.</p>
              </div>

              {demoMode && !showDbImport && (
                <div className="setup-callout">
                  <Database />
                  <span><strong>Pull from your existing database</strong><small>Import live records directly from PostgreSQL, MySQL, MSSQL, or Oracle instead of uploading files.</small></span>
                  <button className="secondary-button" onClick={() => setShowDbImport(true)}>Connect database</button>
                </div>
              )}

              {demoMode && showDbImport && (
                <div className="db-import-panel">
                  <div className="db-import-header">
                    <span><Database /><strong>Database Import</strong></span>
                    <button className="back-button" onClick={() => { setShowDbImport(false); setImportResult(null); }}><ArrowLeft /> Hide</button>
                  </div>
                  <div className="form-grid narrow">
                    <label className="field">
                      <span>Connection string<b> *</b></span>
                      <input value={dbConn.connectionString} onChange={e => setDbConn({ ...dbConn, connectionString: e.target.value })} placeholder="Host=localhost;Database=mydb;Username=user;Password=pass" />
                      <small>Provide the full connection string for your existing database.</small>
                    </label>
                    <label className="field">
                      <span>Database type<b> *</b></span>
                      <select value={dbConn.dbType} onChange={e => setDbConn({ ...dbConn, dbType: e.target.value })}>
                        <option value="PostgreSQL">PostgreSQL</option>
                        <option value="MySQL">MySQL</option>
                        <option value="MSSQL">Microsoft SQL Server</option>
                        <option value="Oracle">Oracle</option>
                      </select>
                    </label>
                    <div className="field-actions">
                      <button className="primary-button" disabled={busy || !dbConn.connectionString} onClick={runDbImport}>
                        {busy ? <LoaderCircle className="spin" /> : <Download />} Run import
                      </button>
                    </div>
                  </div>

                  {importResult?.success && (
                    <div className="import-results">
                      <div className="form-notice success" role="status">
                        <CheckCircle2 />
                        {importResult.tablesDiscovered} tables discovered, {importResult.tablesAutoMapped} auto-mapped across {importResult.results.filter(r => r.status !== "Skipped" && r.status !== "Failed").length} imports
                      </div>
                      {importResult.note && <div className="form-notice info"><Circle />{importResult.note}</div>}
                      {importResult.autoMappedTables.length > 0 && (
                        <div className="mapped-tables">
                          <strong>Auto-mapped tables:</strong>
                          <div className="mapping-chips">
                            {importResult.autoMappedTables.map(m => (
                              <span key={m.table} className="mapping-chip">
                                <small>{m.table}</small> → <b>{m.entityType}</b>
                              </span>
                            ))}
                          </div>
                        </div>
                      )}
                      <div className="import-job-results">
                        {importResult.results.map((r, i) => (
                          <article key={i} className={r.status === "Failed" ? "failed" : r.status === "Skipped" ? "skipped" : "completed"}>
                            <span><strong>{r.table}</strong><small>→ {r.entityType}</small></span>
                            {r.status === "Completed" || r.status === "CompletedWithErrors" ? (
                              <div className="job-stats">
                                <b>{r.successRows ?? 0}</b> rows imported
                                {(r.errorRows ?? 0) > 0 && <span className="err">{(r.errorRows ?? 0)} errors</span>}
                              </div>
                            ) : (
                              <span className="err">{r.reason ?? r.status}</span>
                            )}
                          </article>
                        ))}
                      </div>
                    </div>
                  )}

                  {importResult && !importResult.success && (
                    <div className="form-notice error" role="status"><Circle />Import failed. Check the connection string and database reachability.</div>
                  )}
                </div>
              )}

              <div className="upload-grid">
                {allFiles.map(type => {
                  const result = validations[type];
                  const required = requiredFiles.includes(type);
                  return <article key={type} className={result?.isValid ? "valid" : result && !result.isValid ? "invalid" : ""}>
                    <div><Upload /><span><strong>{titleCase(type)}</strong><small>{required ? "Required for launch" : "Optional migration"}</small></span>{required && <b>Required</b>}</div>
                    <label><input type="file" accept=".xlsx,.csv" disabled={busy} onChange={event => validateFile(type, event)} /><span>{result ? "Choose another file" : "Choose .xlsx or .csv"}</span></label>
                    {result && <div className="validation-result">{result.isValid ? <CheckCircle2 /> : <Circle />}<span><strong>{result.isValid ? `${result.rowCount.toLocaleString()} rows valid` : `${result.errors.length} issues found`}</strong><small>{result.errors[0] ?? result.warnings[0] ?? "Ready for import"}</small></span></div>}
                  </article>;
                })}
              </div>
              <WizardActions step={step} busy={busy} onBack={() => advance(step - 1)} onNext={() => advance()} nextDisabled={!requiredImportsReady} />
            </>
          )}

          {step === 5 && (
            <>
              <div className="permission-table">
                <div className="permission-row permission-head"><span>Capability</span>{["VC", "HOD", "Faculty", "Student"].map(role => <b key={role}>{role}</b>)}</div>
                {[...new Set(permissions.map(item => item.feature))].map(feature => (
                  <div className="permission-row" key={feature}>
                    <strong>{feature}</strong>
                    {["VC", "HOD", "Faculty", "Student"].map(role => {
                      const index = permissions.findIndex(item => item.feature === feature && item.role === role);
                      return <label key={role}><input type="checkbox" checked={permissions[index]?.allowed ?? false} onChange={event => setPermissions(current => current.map((item, itemIndex) => itemIndex === index ? { ...item, allowed: event.target.checked } : item))} /><span><Check /></span></label>;
                    })}
                  </div>
                ))}
              </div>
              <WizardActions step={step} busy={busy} onBack={() => advance(step - 1)} onNext={savePermissions} submitLabel="Apply access baseline" />
            </>
          )}

          {step === 6 && (
            <>
              <div className="review-hero">
                <div className={review?.ready ? "ready" : ""}>{review?.ready ? <Rocket /> : <ShieldCheck />}</div>
                <span><strong>{review?.ready ? "Ready for controlled go-live" : "Complete the remaining launch checks"}</strong><small>AegisCore will not seal installation until every critical dependency passes.</small></span>
                <button onClick={refreshReview} disabled={busy}>{busy ? <LoaderCircle className="spin" /> : <Network />} Run checks</button>
              </div>
              <div className="review-checks">
                {(review?.checks ?? []).map(check => <article key={check.name}><i className={check.passed ? "passed" : ""}>{check.passed ? <Check /> : <Circle />}</i><span><strong>{check.name}</strong><small>{check.detail}</small></span><b>{check.passed ? "Passed" : "Action needed"}</b></article>)}
              </div>
              <div className="go-live-warning"><ShieldCheck /><span><strong>What happens next?</strong><small>AegisCore writes an installation marker, prepares the production configuration, and requires one API restart before normal operation.</small></span></div>
              <WizardActions step={step} busy={busy} onBack={() => advance(step - 1)} onNext={goLive} submitLabel="Seal setup and go live" nextDisabled={!review?.ready} />
            </>
          )}
        </section>
      </main>
    </div>
  );
}

function FormSection({ title, description, children }: { title: string; description: string; children: React.ReactNode }) {
  return <section className="form-section"><div><h3>{title}</h3><p>{description}</p></div><div className="form-grid">{children}</div></section>;
}

function Field({ label, hint, required, children }: { label: string; hint?: string; required?: boolean; children: React.ReactNode }) {
  return <label className="field"><span>{label}{required && <b> *</b>}</span>{children}{hint && <small>{hint}</small>}</label>;
}

function Toggle({ label, checked, onChange }: { label: string; checked: boolean; onChange: (value: boolean) => void }) {
  return <label className="toggle-field"><input type="checkbox" checked={checked} onChange={event => onChange(event.target.checked)} /><span><i /></span><strong>{label}</strong></label>;
}

function WizardActions({ step, busy, onBack, onNext, submitLabel = "Validate and continue", nextDisabled = false }: {
  step: number; busy: boolean; onBack: () => void; onNext?: () => void; submitLabel?: string; nextDisabled?: boolean;
}) {
  return <div className="wizard-actions">
    <button type="button" className="back-button" onClick={onBack} disabled={step === 0 || busy}><ArrowLeft /> Back</button>
    <button type={onNext ? "button" : "submit"} className="primary-button" onClick={onNext} disabled={busy || nextDisabled}>
      {busy ? <LoaderCircle className="spin" /> : null}{submitLabel}<ArrowRight />
    </button>
  </div>;
}

function newDepartment(): Department {
  return { id: crypto.randomUUID(), name: "", code: "", hodName: "", facultyCount: 0 };
}

function updateDepartment(index: number, field: keyof Department, value: string, departments: Department[], setDepartments: (value: Department[]) => void) {
  setDepartments(departments.map((department, itemIndex) => itemIndex === index ? { ...department, [field]: value } : department));
}

function errorNotice(error: unknown): NonNullable<Notice> {
  return { kind: "error", text: error instanceof Error ? error.message : "An unexpected setup error occurred." };
}

function titleCase(value: string) {
  return value.charAt(0).toUpperCase() + value.slice(1);
}

function stepDescription(step: number) {
  return [
    "Identity & licensing", "Data services", "Migration workbooks", "Organization tree",
    "Validate source data", "Permission baseline", "Launch controls"
  ][step];
}

function stepTitle(step: number) {
  return [
    "Tell AegisCore who it serves", "Connect the services that keep it alive",
    "Prepare clean migration workbooks", "Model the university organization",
    "Validate records before import", "Start from a safe access baseline",
    "Prove the installation is ready"
  ][step];
}

function stepSubtitle(step: number) {
  return [
    "Your institutional identity, license, public domain, and accountable administrator.",
    "AegisCore tests PostgreSQL, Redis, and optional email from inside the deployment.",
    "Use canonical templates so every source record can be checked before import.",
    "Department boundaries become authorization and reporting boundaries across the ERP.",
    "Required master data must pass structural and business-rule validation.",
    "Review exactly what institutional roles can see and change.",
    "One final control gate protects the production environment from partial setup."
  ][step];
}

function stepIcon(step: number) {
  return [<Building2 key="building" />, <Server key="server" />, <Download key="download" />, <Network key="network" />, <Upload key="upload" />, <ShieldCheck key="shield" />, <Rocket key="rocket" />][step];
}
