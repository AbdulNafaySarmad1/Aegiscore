"use client";

import { AlertCircle, ArrowDownRight, ArrowUpRight, CheckCircle2, CreditCard, FileCheck2, Landmark, RefreshCw, ShieldCheck } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { ApiError, apiGet } from "@/lib/api-client";

type Summary = {
  openReceivables: number;
  postedThisPeriod: number;
  pendingApprovalAmount: number;
  openDemandCount: number;
  pendingVoucherCount: number;
};

const money = (value: number) => new Intl.NumberFormat("en-PK", { style: "currency", currency: "PKR", maximumFractionDigits: 0 }).format(value);

export default function FinancePage() {
  const [summary, setSummary] = useState<Summary | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(() => {
    setLoading(true);
    setError(null);
    apiGet<Summary>("/api/finance/v2/dashboard")
      .then(setSummary)
      .catch((reason: unknown) => setError(reason instanceof ApiError ? reason.message : "Finance data could not be loaded."))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    let cancelled = false;
    apiGet<Summary>("/api/finance/v2/dashboard")
      .then(data => { if (!cancelled) setSummary(data); })
      .catch((reason: unknown) => { if (!cancelled) setError(reason instanceof ApiError ? reason.message : "Finance data could not be loaded."); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, []);

  return (
    <main className="finance-page">
      <header className="finance-heading">
        <div>
          <p>Finance department</p>
          <h1>Institutional finance</h1>
          <span>Live ledger activity, fee collections, and maker-checker controls.</span>
        </div>
        <button className="icon-button" onClick={load} aria-label="Refresh finance dashboard" title="Refresh"><RefreshCw /></button>
      </header>

      {error && <div className="finance-alert" role="alert"><AlertCircle /><span>{error}</span></div>}

      <section className="finance-metrics" aria-label="Finance metrics">
        <Metric icon={<Landmark />} label="Open receivables" value={loading ? "Loading" : money(summary?.openReceivables ?? 0)} tone="blue" />
        <Metric icon={<ArrowUpRight />} label="Posted, last 30 days" value={loading ? "Loading" : money(summary?.postedThisPeriod ?? 0)} tone="green" />
        <Metric icon={<FileCheck2 />} label="Awaiting approval" value={loading ? "Loading" : money(summary?.pendingApprovalAmount ?? 0)} tone="amber" />
        <Metric icon={<CreditCard />} label="Open fee demands" value={loading ? "Loading" : String(summary?.openDemandCount ?? 0)} tone="violet" />
      </section>

      <section className="finance-grid">
        <article className="surface finance-panel">
          <div className="surface-heading"><div><h2>Control posture</h2><p>Posting controls applied to this tenant</p></div><ShieldCheck /></div>
          <div className="finance-control-list">
            <Control label="Tenant-scoped finance records" detail="PostgreSQL RLS enabled" />
            <Control label="Double-entry posting" detail="Database balance trigger" />
            <Control label="Maker-checker separation" detail="API enforced" />
            <Control label="PQC voucher signature" detail="Awaiting ML-DSA provider" pending />
          </div>
        </article>
        <article className="surface finance-panel">
          <div className="surface-heading"><div><h2>Approval queue</h2><p>Items requiring finance action</p></div><span className="finance-count">{summary?.pendingVoucherCount ?? 0}</span></div>
          <div className="finance-empty">
            {summary?.pendingVoucherCount ? <><ArrowDownRight /><strong>{summary.pendingVoucherCount} voucher(s) awaiting approval</strong><span>Open the voucher queue to review maker submissions.</span></> : <><CheckCircle2 /><strong>No vouchers awaiting approval</strong><span>The maker-checker queue is clear.</span></>}
          </div>
        </article>
      </section>
    </main>
  );
}

function Metric({ icon, label, value, tone }: { icon: React.ReactNode; label: string; value: string; tone: string }) {
  return <article className="finance-metric"><div className={`metric-icon ${tone}`}>{icon}</div><span>{label}</span><strong>{value}</strong><small>Tenant-scoped live value</small></article>;
}

function Control({ label, detail, pending = false }: { label: string; detail: string; pending?: boolean }) {
  return <div className="finance-control"><span className={pending ? "control-dot pending" : "control-dot"}>{pending ? "!" : "✓"}</span><span><strong>{label}</strong><small>{detail}</small></span></div>;
}
