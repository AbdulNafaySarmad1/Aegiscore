"use client";

import { useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet, ApiError } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { RegisterHead, Panel, Notice, Mark, Figures } from "@/components/register";
import { Ledger } from "@/components/register/ledger";

type AgingLine = { feeDemandId: string; studentId: string; demandNumber: string; currency: string; dueDate: string; daysOverdue: number; bucket: string; outstandingAmount: number };
type Aging = { asOf: string; lines: AgingLine[]; bucketTotals: Record<string, number> };
type CashLine = { accountCode?: string; accountName?: string; balance?: number; currency?: string };
type Cash = { asOf: string; lines: CashLine[]; totalBalance: number };

const REPORTS = [
  { value: "receivables-aging", label: "Receivables ageing", detail: "What is owed to the university, by how overdue it is." },
  { value: "cash-position", label: "Cash position", detail: "What is in each account as at a moment." },
  { value: "statement-of-financial-position", label: "Statement of financial position", detail: "Assets, liabilities and funds under the accounting standard this institution follows." },
] as const;

type ReportName = typeof REPORTS[number]["value"];

/** Finance reports, drawn as they would be read rather than printed as JSON. */
export default function ReportsPage() {
  const { money, date, dateTime } = useTenantFormatter();
  const [report, setReport] = useState<ReportName>("receivables-aging");
  const [result, setResult] = useState<unknown>();
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function draw() {
    setBusy(true); setError(null); setResult(undefined);
    try { setResult(await apiGet(`/api/finance/v2/reports/${report}`)); }
    catch (reason) { setError(reason instanceof ApiError ? reason.message : "That report could not be drawn."); }
    finally { setBusy(false); }
  }

  function download() {
    const url = URL.createObjectURL(new Blob([JSON.stringify(result, null, 2)], { type: "application/json" }));
    const link = document.createElement("a");
    link.href = url; link.download = `${report}-${new Date().toISOString().slice(0, 10)}.json`;
    link.click();
    URL.revokeObjectURL(url);
  }

  const chosen = REPORTS.find(r => r.value === report)!;
  const aging = report === "receivables-aging" ? result as Aging | undefined : undefined;
  const cash = report === "cash-position" ? result as Cash | undefined : undefined;

  return (
    <AppShell>
      <div className="page">
        <RegisterHead
          title="Finance reports"
          crumbs={[{ label: "Finance" }, { label: "Reports" }]}
          meta={<span>Drawn from the ledger at the moment you ask. Nothing here is cached.</span>}
        />

        <Panel title="Which report">
          <div className="panel-form">
            <label><span>Report</span>
              <select value={report} onChange={e => { setReport(e.target.value as ReportName); setResult(undefined); setError(null); }}>
                {REPORTS.map(r => <option key={r.value} value={r.value}>{r.label}</option>)}
              </select>
            </label>
            <p className="field-hint">{chosen.detail}</p>
            <div className="btn-row">
              <button type="button" className={`btn btn-primary${busy ? " btn-loading" : ""}`} disabled={busy} onClick={draw}>
                {busy ? "Drawing" : "Draw the report"}
              </button>
              {result !== undefined && <button type="button" className="btn btn-ghost" onClick={download}>Download as JSON</button>}
            </div>
          </div>
        </Panel>

        {error && <Notice tone="oxblood" role="alert">{error}</Notice>}

        {aging && (
          <>
            <Figures aria-label="Ageing totals" items={Object.entries(aging.bucketTotals ?? {}).map(([bucket, total]) => ({
              label: bucket === "0-30" ? "Not yet 30 days" : `${bucket} days`,
              value: money(total),
              tone: bucket === "0-30" ? "ink" : bucket.startsWith("90") ? "oxblood" : "brass",
            }))} />
            <Panel title="Receivables" aside={<span>as at {dateTime(aging.asOf)}</span>} flush>
              <Ledger rows={aging.lines} keyOf={l => l.feeDemandId} minWidth={780}
                status={l => l.daysOverdue > 90 ? "oxblood" : l.daysOverdue > 0 ? "brass" : undefined}
                empty={{ title: "Nothing is owed", detail: "Every fee demand on the ledger is settled." }}
                columns={[
                  { key: "demand", label: "Demand", render: l => l.demandNumber, mono: true },
                  { key: "due", label: "Due", render: l => date(l.dueDate), nowrap: true },
                  { key: "overdue", label: "Overdue by", render: l => l.daysOverdue <= 0 ? "not yet due" : `${l.daysOverdue} days`, nowrap: true },
                  { key: "bucket", label: "Bucket", render: l => <Mark tone={l.daysOverdue > 90 ? "oxblood" : l.daysOverdue > 0 ? "brass" : "ink"}>{l.bucket}</Mark> },
                  { key: "amount", label: "Outstanding", render: l => money(l.outstandingAmount, l.currency), num: true, nowrap: true },
                ]} />
            </Panel>
          </>
        )}

        {cash && (
          <Panel title="Cash position" aside={<span>as at {dateTime(cash.asOf)}</span>} flush>
            <Ledger rows={cash.lines} keyOf={l => l.accountCode ?? String(l.accountName)} minWidth={560}
              empty={{ title: "No cash account is on the ledger", detail: "Nothing has been posted to a cash account yet, so there is no position to report." }}
              columns={[
                { key: "code", label: "Account", render: l => l.accountCode ?? "—", mono: true },
                { key: "name", label: "", render: l => l.accountName ?? "" },
                { key: "balance", label: "Balance", render: l => money(l.balance ?? 0, l.currency), num: true, nowrap: true },
              ]} />
            <div className="panel-foot"><span>Total</span><strong>{money(cash.totalBalance)}</strong></div>
          </Panel>
        )}

        {result !== undefined && !aging && !cash && (
          <Panel title={chosen.label} aside={<span>as drawn</span>}>
            <pre className="report-raw">{JSON.stringify(result, null, 2)}</pre>
          </Panel>
        )}
      </div>
    </AppShell>
  );
}
