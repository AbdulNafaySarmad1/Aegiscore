import { serverGet } from "@/lib/server-api";
import Link from "next/link";
import { getPortalLocale } from "@/i18n/server";
import { portalMessages } from "@/i18n/messages";
import { ApprovalQueue } from "@/components/finance/approval-queue";
import { FinanceRealtimeRefresh } from "@/components/finance/finance-realtime-refresh";

type Voucher = { id: string; voucherNumber: string; voucherType: string; status: string; currency: string; totalAmount: number; description: string | null; createdAt: string };
type Operations = {
  pipeline: Array<{ status: string; count: number; total: number; vouchers: Voucher[] }>;
  approvals: Voucher[];
  collections: Array<{ date: string; amount: number; count: number }>;
  liveFeed: Array<{ id: string; eventType: string; gatewaySlug: string; referenceId: string | null; amount: number | null; currency: string | null; status: string; isSandbox: boolean; createdAt: string }>;
};

export default async function FinanceOperationsPage() {
  const locale = await getPortalLocale();
  const t = portalMessages[locale].finance;
  let data: Operations | null = null;
  try { data = await serverGet<Operations>("/api/finance/v2/dashboard/operations"); } catch { /* rendered below */ }
  if (!data) return <main className="finance-page"><p className="finance-alert" role="alert">{t.operationsUnavailable}</p></main>;
  const money = (value: number, currency = "PKR") => new Intl.NumberFormat(locale, { style: "currency", currency, maximumFractionDigits: 0 }).format(value);
  const date = (value: string) => new Intl.DateTimeFormat(locale, { dateStyle: "medium" }).format(new Date(value));
  const maxCollection = Math.max(1, ...data.collections.map(item => item.amount));
  return <main className="finance-page"><FinanceRealtimeRefresh /><header className="finance-heading"><div><p>{t.operations}</p><h1>{t.operationsTitle}</h1><span>{t.operationsDetail}</span></div><Link className="wizard-secondary" href="/finance">{t.executiveDashboard}</Link></header>
    <section className="voucher-kanban" aria-label={t.operationsTitle}>{data.pipeline.map(column => <article className="surface finance-panel" key={column.status}><div className="surface-heading"><h2>{column.status.replaceAll("_", " ")}</h2><strong>{column.count}</strong></div><p>{money(column.total)}</p><ol className="voucher-list">{column.vouchers.map(voucher => <li key={voucher.id}><strong>{voucher.voucherNumber}</strong><span>{voucher.description ?? voucher.voucherType}</span><small>{money(voucher.totalAmount, voucher.currency)} · {date(voucher.createdAt)}</small></li>)}</ol></article>)}</section>
    <section className="surface finance-panel"><div className="surface-heading"><div><h2>{t.collections}</h2><p>{t.collectionVolume}</p></div></div><div className="collection-heatmap" role="img" aria-label={t.collectionVolume}>{data.collections.map(item => <span key={item.date} title={`${date(item.date)}: ${money(item.amount)} (${item.count})`} style={{ "--collection-level": String(Math.max(.08, item.amount / maxCollection)) } as React.CSSProperties} />)}</div><details className="chart-table"><summary>{t.accessibleTable}</summary><table><thead><tr><th>{t.date}</th><th>{t.amount}</th><th>{t.count}</th></tr></thead><tbody>{data.collections.map(item => <tr key={item.date}><td>{date(item.date)}</td><td>{money(item.amount)}</td><td>{item.count}</td></tr>)}</tbody></table></details></section>
    <ApprovalQueue vouchers={data.approvals} locale={locale} labels={{ title: t.approvalQueue, detail: t.priorityAge, approve: t.approve, reject: t.reject, note: t.decisionNote, required: t.rejectionNoteRequired, failed: t.decisionFailed }} />
    <section className="surface finance-panel"><div className="surface-heading"><div><h2>{t.gatewayActivity}</h2><p>{t.recentEvents}</p></div></div><div className="audit-table" role="region" aria-label={t.gatewayActivity} tabIndex={0}><table><tbody>{data.liveFeed.map(event => <tr key={event.id}><th>{event.eventType}</th><td>{event.gatewaySlug}</td><td>{event.amount === null ? "-" : money(event.amount, event.currency ?? "PKR")}</td><td>{event.isSandbox && <b className="sandbox-badge" title={t.sandboxHelp}>{t.sandbox}</b>} {event.status}</td></tr>)}</tbody></table></div></section>
  </main>;
}
