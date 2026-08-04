"use client";

import type { ExecutiveDashboard, SecurityPosture } from "@/app/finance/page";
import { FinanceRealtimeRefresh } from "@/components/finance/finance-realtime-refresh";
import { ApiError, apiGet } from "@/lib/api-client";
import { AlertCircle, CheckCircle2, CreditCard, FileCheck2, Landmark, RefreshCw, Search, ShieldCheck } from "lucide-react";
import { geoGraticule10, geoNaturalEarth1, geoPath } from "d3-geo";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { Area, AreaChart, Bar, BarChart, Brush, CartesianGrid, Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { useLocale, useTranslations } from "next-intl";

type VoucherProof = { voucherNumber: string; status: string; pqcAlgorithm: string | null; pqcKeyId: string | null; signature: string | null; publicKey: string | null; signedPayload: string | null; verified: boolean; clientVerified: boolean };

function base64Bytes(value: string): Uint8Array {
  return Uint8Array.from(atob(value), character => character.charCodeAt(0));
}

export function FinanceDashboard({ dashboard, security }: { dashboard: ExecutiveDashboard | null; security: SecurityPosture | null }) {
  const t = useTranslations("finance");
  const locale = useLocale();
  const router = useRouter();
  const [voucherId, setVoucherId] = useState("");
  const [proof, setProof] = useState<VoucherProof | null>(null);
  const [proofError, setProofError] = useState<string | null>(null);
  const net = (dashboard?.kpis.revenue ?? 0) - (dashboard?.kpis.expenses ?? 0);
  const money = (value: number) => new Intl.NumberFormat(locale, { style: "currency", currency: "PKR", maximumFractionDigits: 0 }).format(value);
  const dateTime = (value: string) => new Intl.DateTimeFormat(locale, { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));

  async function verifyVoucher() {
    setProof(null); setProofError(null);
    try {
      const response = await apiGet<Omit<VoucherProof, "clientVerified">>(`/api/finance/v2/vouchers/${voucherId.trim()}/verification`);
      let clientVerified = false;
      if (response.pqcAlgorithm === "ML-DSA-65" && response.signature && response.publicKey && response.signedPayload) {
        const { ml_dsa65 } = await import("@noble/post-quantum/ml-dsa.js");
        clientVerified = ml_dsa65.verify(base64Bytes(response.signature), base64Bytes(response.signedPayload), base64Bytes(response.publicKey));
      }
      setProof({ ...response, clientVerified });
    }
    catch (error: unknown) { setProofError(error instanceof ApiError ? error.message : t("proofLoadError")); }
  }

  if (!dashboard) return <main className="finance-page"><div className="finance-alert" role="alert"><AlertCircle />{t("unavailable")}</div></main>;

  return <main className="finance-page"><FinanceRealtimeRefresh />
    <header className="finance-heading"><div><p>{t("department")}</p><h1>{t("executive")}</h1><span>{t("executiveDetail")}</span></div><button className="icon-button" onClick={() => router.refresh()} aria-label={t("refresh")}><RefreshCw /></button><Link className="wizard-secondary" href="/finance/operations">{t("operations")}</Link><Link className="wizard-secondary" href="/finance/gateways">{t("gateways")}</Link></header>
    <section className="finance-metrics" aria-label={t("metrics")}>
      <Metric icon={<Landmark />} label={t("revenue")} value={money(dashboard.kpis.revenue)} tone="blue" />
      <Metric icon={<CreditCard />} label={t("expenses")} value={money(dashboard.kpis.expenses)} tone="amber" />
      <Metric icon={<FileCheck2 />} label={t("net")} value={money(net)} tone={net >= 0 ? "green" : "amber"} />
      <Metric icon={<Landmark />} label={t("cash")} value={money(dashboard.kpis.cashPosition)} tone="green" />
    </section>
    <section className="finance-chart-grid">
      <ChartPanel title={t("revenueExpense")} table={<TrendTable rows={dashboard.trend} money={money} labels={[t("month"), t("revenue"), t("expenses"), t("postings")]} />} tableLabel={t("accessibleTable")}>
        <ResponsiveContainer width="100%" height={320}><AreaChart data={dashboard.trend}><CartesianGrid stroke="var(--line)" /><XAxis dataKey="month" /><YAxis /><Tooltip formatter={value => money(Number(value))} /><Legend /><Area dataKey="revenue" stackId="finance" stroke="var(--blue)" fill="var(--blue-soft)" /><Area dataKey="expenses" stackId="finance" stroke="var(--amber)" fill="var(--amber-soft)" /><Brush dataKey="month" height={24} /></AreaChart></ResponsiveContainer>
      </ChartPanel>
      <ChartPanel title={t("topExpenses")} table={<ExpenseTable rows={dashboard.expenseCategories} money={money} labels={[t("category"), t("amount")]} />} tableLabel={t("accessibleTable")}>
        <ResponsiveContainer width="100%" height={320}><BarChart data={dashboard.expenseCategories} layout="vertical"><CartesianGrid stroke="var(--line)" /><XAxis type="number" /><YAxis dataKey="category" type="category" width={120} /><Tooltip /><Bar dataKey="amount" fill="var(--amber)" /></BarChart></ResponsiveContainer>
      </ChartPanel>
    </section>
    <section className="finance-chart-grid">
      <ChartPanel title={t("cashFlowWaterfall")} table={<SimpleTable rows={dashboard.cashFlow.map(item => [item.category, money(item.amount)])} labels={[t("category"), t("amount")]} />} tableLabel={t("accessibleTable")}>
        <ResponsiveContainer width="100%" height={300}><BarChart data={dashboard.cashFlow}><CartesianGrid stroke="var(--line)" /><XAxis dataKey="category" /><YAxis /><Tooltip formatter={value => money(Number(value))} /><Bar dataKey="amount">{dashboard.cashFlow.map(item => <Cell key={item.category} fill={item.amount >= 0 ? "var(--green)" : "var(--red)"} />)}</Bar></BarChart></ResponsiveContainer>
      </ChartPanel>
      <ChartPanel title={t("budgetBurn")} table={<SimpleTable rows={dashboard.budgetBurn.map(item => [item.name, `${item.consumedPercent}%`, money(item.spent), money(item.allocated)])} labels={[t("departmentLabel"), t("consumed"), t("spent"), t("allocated")]} />} tableLabel={t("accessibleTable")}>
        <ResponsiveContainer width="100%" height={300}><PieChart><Pie data={dashboard.budgetBurn} dataKey="spent" nameKey="name" innerRadius={64} outerRadius={104} label={({ name, percent }) => `${name} ${Math.round((percent ?? 0) * 100)}%`}>{dashboard.budgetBurn.map((item, index) => <Cell key={item.costCenterId} fill={["var(--blue)", "var(--green)", "var(--amber)", "var(--red)"][index % 4]} />)}</Pie><Tooltip formatter={value => money(Number(value))} /></PieChart></ResponsiveContainer>
      </ChartPanel>
    </section>
    <section className="surface finance-panel"><div className="surface-heading"><div><h2>{t("currencyExposure")}</h2><p>{t("currencyExposureDetail")}</p></div></div><CurrencyExposureMap rows={dashboard.currencyExposure} money={money} /><details className="chart-table"><summary>{t("accessibleTable")}</summary><SimpleTable rows={dashboard.currencyExposure.map(item => [item.currency, money(item.foreignAmount), money(item.functionalAmount)])} labels={[t("currency"), t("foreignAmount"), t("functionalAmount")]} /></details></section>
    <section className="surface finance-panel"><div className="surface-heading"><div><h2>{t("feeFunnel")}</h2><p>{t("feeFunnelDetail")}</p></div><CreditCard /></div><div className="finance-funnel"><span><strong>{money(dashboard.feeFunnel.demanded)}</strong>{t("demanded")}</span><span><strong>{money(dashboard.feeFunnel.collected)}</strong>{t("collected")}</span><span><strong>{money(dashboard.feeFunnel.overdue)}</strong>{t("overdue")}</span><span><strong>{dashboard.feeFunnel.open}</strong>{t("openDemands")}</span></div></section>
    <section className="surface finance-panel" aria-labelledby="security-heading"><div className="surface-heading"><div><h2 id="security-heading">{t("securityPosture")}</h2><p>{t("securityDetail")}</p></div><ShieldCheck /></div><div className="security-posture-grid"><div className={`security-status ${security?.chainIntegrity ?? "amber"}`}><CheckCircle2 /><strong>{security?.chainIntegrity ?? t("unavailableValue")}</strong></div><div><small>{t("lastSeal")}</small><strong>{security?.lastSealTimestamp ? dateTime(security.lastSealTimestamp) : t("notSealed")}</strong></div><div><small>{t("merkleRoot")}</small><strong className="security-mono">{security?.merkleRoot ?? "-"}</strong></div><div><small>{t("key")}</small><strong>{security?.signatureAlgorithm ?? "-"} {security?.keyId ?? ""}</strong></div><div><small>{t("verificationAlerts")}</small><strong>{security?.failedVerificationAlerts.length ?? 0}</strong></div></div>{security && <div className="security-detail-grid"><details className="chart-table"><summary>{t("keySchedule")}</summary><table><thead><tr><th>{t("key")}</th><th>{t("purpose")}</th><th>{t("algorithm")}</th><th>{t("daysToExpiry")}</th></tr></thead><tbody>{security.keys.map(key => <tr key={key.keyId}><td className="security-mono">{key.keyId}</td><td>{key.purpose}</td><td>{key.algorithm}</td><td>{key.daysToExpiry}</td></tr>)}</tbody></table></details><details className="chart-table"><summary>{t("verificationAlerts")}</summary><table><thead><tr><th>{t("entry")}</th><th>{t("reason")}</th></tr></thead><tbody>{security.failedVerificationAlerts.map(alert => <tr key={`${alert.entryId}-${alert.reason}`}><td>{alert.entryNumber}</td><td>{alert.reason}</td></tr>)}</tbody></table></details></div>}</section>
    <section className="surface finance-panel"><div className="surface-heading"><div><h2>{t("verifySignature")}</h2><p>{t("verifyDetail")}</p></div><Search /></div><div className="voucher-proof-form"><label htmlFor="voucher-id">{t("voucherId")}</label><input id="voucher-id" value={voucherId} onChange={event => setVoucherId(event.target.value)} /><button className="wizard-primary" disabled={!voucherId.trim()} onClick={verifyVoucher}>{t("verify")}</button></div>{proofError && <p role="alert" className="finance-alert">{proofError}</p>}{proof && <div className="security-posture-grid"><div><small>{t("voucher")}</small><strong>{proof.voucherNumber}</strong></div><div><small>{t("status")}</small><strong>{proof.verified && proof.clientVerified ? t("proofVerified") : t("proofFailed")}</strong></div><div><small>{t("algorithm")}</small><strong>{proof.pqcAlgorithm ?? "-"}</strong></div><div><small>{t("key")}</small><strong>{proof.pqcKeyId ?? "-"}</strong></div></div>}</section>
  </main>;
}

function Metric({ icon, label, value, tone }: { icon: React.ReactNode; label: string; value: string; tone: string }) { return <article className="finance-metric"><div className={`metric-icon ${tone}`}>{icon}</div><span>{label}</span><strong>{value}</strong></article>; }
function ChartPanel({ title, children, table, tableLabel }: { title: string; children: React.ReactNode; table: React.ReactNode; tableLabel: string }) { return <article className="surface finance-panel"><div className="surface-heading"><h2>{title}</h2></div><div role="img" aria-label={title}>{children}</div><details className="chart-table"><summary>{tableLabel}</summary>{table}</details></article>; }
function TrendTable({ rows, money, labels }: { rows: ExecutiveDashboard["trend"]; money: (value: number) => string; labels: string[] }) { return <table><thead><tr>{labels.map(label => <th key={label}>{label}</th>)}</tr></thead><tbody>{rows.map(row => <tr key={row.month}><td>{row.month}</td><td>{money(row.revenue)}</td><td>{money(row.expenses)}</td><td>{row.postings}</td></tr>)}</tbody></table>; }
function ExpenseTable({ rows, money, labels }: { rows: ExecutiveDashboard["expenseCategories"]; money: (value: number) => string; labels: string[] }) { return <table><thead><tr>{labels.map(label => <th key={label}>{label}</th>)}</tr></thead><tbody>{rows.map(row => <tr key={row.category}><td>{row.category}</td><td>{money(row.amount)}</td></tr>)}</tbody></table>; }
function SimpleTable({ rows, labels }: { rows: Array<Array<string | number>>; labels: string[] }) { return <table><thead><tr>{labels.map(label => <th key={label}>{label}</th>)}</tr></thead><tbody>{rows.map((row, index) => <tr key={index}>{row.map((value, column) => <td key={column}>{value}</td>)}</tr>)}</tbody></table>; }

const currencyCoordinates: Record<string, [number, number]> = { PKR: [69.3, 30.4], USD: [-98.6, 39.8], GBP: [-3.4, 55.4], EUR: [10.5, 50.1], AED: [54.4, 24.4], SGD: [103.8, 1.35], MYR: [102, 4.2], AUD: [133.8, -25.3] };
function CurrencyExposureMap({ rows, money }: { rows: ExecutiveDashboard["currencyExposure"]; money: (value: number) => string }) {
  const width = 960, height = 360;
  const projection = geoNaturalEarth1().fitExtent([[10, 10], [width - 10, height - 10]], { type: "Sphere" });
  const path = geoPath(projection);
  const max = Math.max(1, ...rows.map(row => row.functionalAmount));
  return <div className="currency-map" role="img" aria-label="Currency exposure by transaction volume"><svg viewBox={`0 0 ${width} ${height}`}><path d={path({ type: "Sphere" }) ?? ""} className="map-sphere" /><path d={path(geoGraticule10()) ?? ""} className="map-grid" />{rows.map(row => { const point = currencyCoordinates[row.currency]; if (!point) return null; const projected = projection(point); if (!projected) return null; return <g key={row.currency} transform={`translate(${projected[0]} ${projected[1]})`}><circle r={7 + 24 * Math.sqrt(row.functionalAmount / max)}><title>{row.currency}: {money(row.functionalAmount)}</title></circle><text y="4">{row.currency}</text></g>; })}</svg></div>;
}
