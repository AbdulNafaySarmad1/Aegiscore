"use client";

import { CheckCircle2, KeyRound, RefreshCw, ShieldCheck } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { ApiError, apiGet, apiPost, apiPut } from "@/lib/api-client";
import { ErrorDisplay, toWizardError } from "@/components/ErrorDisplay";

import { FocusTrapDialog } from "@/components/FocusTrapDialog";
type Gateway = {
  gatewaySlug: string;
  environment: string;
  isActive: boolean;
  healthStatus: string;
  lastHealthCheckAt: string | null;
  healthError: string | null;
  configuredAt: string | null;
};

const hashAuthenticated = new Set(["payfast"]);
const requiredFields = (slug: string, env: string | undefined) => env === "production" && !hashAuthenticated.has(slug) ? [...(fields[slug] ?? ["ENV"]), "PQC_ALGORITHM", "PQC_PUBLIC_KEY"] : (fields[slug] ?? ["ENV"]);
const secretField = (field: string) => ["SECRET", "PASSWORD", "TOKEN", "SALT"].some(part => field.includes(part)) || field.endsWith("_KEY");

const fields: Record<string, string[]> = {
  // PayFast (Pakistan) hosted checkout. Its IPN is authenticated by a hash keyed on SECURED_KEY, so no WEBHOOK_SECRET or ML-DSA key.
  payfast: ["MERCHANT_ID", "SECURED_KEY", "MERCHANT_NAME", "ENV"],
  raast: ["MERCHANT_ID", "CLIENT_ID", "CLIENT_SECRET", "IBAN", "WEBHOOK_SECRET", "ENV"],
  stripe: ["PUBLISHABLE_KEY", "SECRET_KEY", "WEBHOOK_SECRET", "ACCOUNT_ID"],
  jazzcash: ["MERCHANT_ID", "PASSWORD", "INTEGRITY_SALT", "WEBHOOK_SECRET"],
  easypaisa: ["STORE_ID", "PASSWORD", "WEBHOOK_SECRET"],
  gocardless: ["ACCESS_TOKEN", "WEBHOOK_SECRET"],
  "1link": ["MERCHANT_ID", "CLIENT_SECRET", "WEBHOOK_SECRET"],
  "jazzcash-merchant": ["MERCHANT_ID", "PASSWORD", "INTEGRITY_SALT", "WEBHOOK_SECRET"],
  "hbl-pay": ["MERCHANT_ID", "CLIENT_ID", "CLIENT_SECRET", "WEBHOOK_SECRET"],
  "mcb-corporate": ["MERCHANT_ID", "CLIENT_ID", "CLIENT_SECRET", "WEBHOOK_SECRET"],
  "ubl-omni": ["MERCHANT_ID", "CLIENT_ID", "CLIENT_SECRET", "WEBHOOK_SECRET"],
  "network-international": ["MERCHANT_ID", "API_KEY", "API_SECRET", "WEBHOOK_SECRET"],
  telr: ["STORE_ID", "AUTH_KEY", "WEBHOOK_SECRET"],
  "plaid-ach": ["CLIENT_ID", "SECRET_KEY", "WEBHOOK_SECRET"],
  "billplz-fpx": ["API_KEY", "X_SIGNATURE", "WEBHOOK_SECRET"],
  "duitnow-qr": ["MERCHANT_ID", "CLIENT_ID", "CLIENT_SECRET", "WEBHOOK_SECRET"],
  "swift-mt103": ["BIC", "SIGNING_KEY_ID"]
};

export default function GatewayManagementPage() {
  const [gateways, setGateways] = useState<Gateway[]>([]);
  const [selected, setSelected] = useState<string | null>(null);
  const [values, setValues] = useState<Record<string, string>>({ ENV: "production" });
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try { setGateways(await apiGet<Gateway[]>("/api/finance/v2/gateways")); setError(null); }
    catch (reason: unknown) { setError(reason instanceof ApiError ? reason.message : "Gateway configuration could not be loaded."); }
  }, []);

  useEffect(() => { Promise.resolve().then(load); }, [load]);

  const configure = async () => {
    if (!selected) return;
    setBusy(true); setMessage(null); setError(null);
    try {
      const result = await apiPut<{ success: boolean; healthStatus: string; latencyMs: number }>(`/api/finance/v2/gateways/${selected}`, values);
      setMessage(`${selected} saved. Health status: ${result.healthStatus} (${result.latencyMs}ms).`);
      setSelected(null); await load();
    } catch (reason: unknown) { setError(reason instanceof ApiError ? reason.message : "Gateway configuration failed."); }
    finally { setBusy(false); }
  };

  const check = async (slug: string) => {
    setBusy(true); setMessage(null); setError(null);
    try { const result = await apiPost<{ success: boolean; healthStatus: string; latencyMs: number }>(`/api/finance/v2/gateways/${slug}/health-check`); setMessage(`${slug} health status: ${result.healthStatus} (${result.latencyMs}ms).`); await load(); }
    catch (reason: unknown) { setError(reason instanceof ApiError ? reason.message : "Health check failed."); }
    finally { setBusy(false); }
  };

  return <main className="finance-page">
    <header className="finance-heading"><div><p>Finance settings</p><h1>Payment gateways</h1><span>Credentials are write-only and stored in the tenant vault. They are never returned or logged.</span></div><button className="icon-button" onClick={() => void load()} aria-label="Refresh gateway configuration"><RefreshCw /></button></header>
    {message && <div className="finance-alert" role="status"><CheckCircle2 /><span>{message}</span></div>}
    {error && <ErrorDisplay error={toWizardError(error, "Payment gateway settings could not be loaded.")} onRetry={() => void load()} />}
    <section className="finance-grid" aria-label="Configured payment gateways">
      {gateways.map(gateway => <article className="surface finance-panel" key={gateway.gatewaySlug}>
        <div className="surface-heading"><div><h2>{gateway.gatewaySlug.toUpperCase()}</h2><p>{gateway.environment} · {gateway.healthStatus}</p></div><ShieldCheck /></div>
        <div className="finance-control-list"><Control label={gateway.isActive ? "Active" : "Inactive"} detail={gateway.healthError ?? (gateway.lastHealthCheckAt ? `Checked ${new Date(gateway.lastHealthCheckAt).toLocaleString()}` : "Not checked")} /></div>
        <div className="wizard-footer"><button className="wizard-secondary" onClick={() => { setSelected(gateway.gatewaySlug); setValues({ ENV: gateway.environment }); }}><KeyRound size={16} /> Configure</button><button className="wizard-primary" disabled={busy || !gateway.isActive} onClick={() => void check(gateway.gatewaySlug)}>Test connection</button></div>
      </article>)}
      {!gateways.some(gateway => gateway.gatewaySlug === "payfast") && <article className="surface finance-panel">
        <div className="surface-heading"><div><h2>PAYFAST</h2><p>Card payments on PayFast's hosted page · not configured</p></div><ShieldCheck /></div>
        <div className="wizard-footer"><button className="wizard-secondary" onClick={() => { setSelected("payfast"); setValues({ ENV: "sandbox" }); }}><KeyRound size={16} /> Configure</button></div>
      </article>}
      {gateways.length === 0 && <div className="finance-empty"><KeyRound /><strong>No gateways configured</strong><span>Configure a gateway during setup or after activating a licensed tenant.</span></div>}
    </section>
    {selected && <FocusTrapDialog label={`Configure ${selected}`} onClose={() => setSelected(null)}><div className="wizard-card"><h2>Configure {selected.toUpperCase()}</h2><p className="wizard-muted">Values are write-only and stored in the tenant vault. {hashAuthenticated.has(selected) ? "Use the sandbox (UAT) credentials PayFast issued until live credentials are approved. ENV is sandbox or production." : "Production webhooks also require ML-DSA verification."}</p>{requiredFields(selected, values.ENV).map(field => <label className="wizard-field" key={field}><span>{field.replaceAll("_", " ")} *</span><input type={secretField(field) ? "password" : "text"} value={values[field] ?? ""} onChange={event => setValues(current => ({ ...current, [field]: event.target.value }))} /></label>)}<div className="wizard-footer"><button className="wizard-secondary" onClick={() => setSelected(null)}>Cancel</button><button className="wizard-primary" disabled={busy || requiredFields(selected, values.ENV).some(field => !values[field]?.trim())} onClick={() => void configure()}>{busy ? "Saving..." : "Save and verify"}</button></div></div></FocusTrapDialog>}
  </main>;
}

function Control({ label, detail }: { label: string; detail: string }) { return <div className="finance-control"><span className="control-dot">✓</span><span><strong>{label}</strong><small>{detail}</small></span></div>; }
