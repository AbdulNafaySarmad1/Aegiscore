"use client";

import { FinanceRealtimeRefresh } from "@/components/finance/finance-realtime-refresh";
import { AlertCircle, Banknote, ClipboardCheck, WalletCards } from "lucide-react";
import { useTranslations } from "next-intl";
import { Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip, Treemap } from "recharts";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";

export type DepartmentDashboard = {
  department: { id: string; name: string; code: string };
  budget: { allocated: number; spent: number; committed: number; available: number };
  pettyCash: { currency: string; floatAmount: number; currentBalance: number } | null;
  approvals: Array<{ id: string; voucherNumber: string; voucherType: string; currency: string; totalAmount: number; description: string | null; createdAt: string }>;
  categories: Array<{ category: string; amount: number }>;
};

export function DepartmentFinanceDashboard({ dashboard }: { dashboard: DepartmentDashboard | null }) {
  const t = useTranslations("finance");
  const { money } = useTenantFormatter();
  if (!dashboard) return <main className="finance-page"><p className="finance-alert" role="alert"><AlertCircle />{t("departmentUnavailable")}</p></main>;
  const budget = [{ name: t("spent"), value: dashboard.budget.spent }, { name: t("committed"), value: dashboard.budget.committed }, { name: t("available"), value: Math.max(0, dashboard.budget.available) }];
  const pettyPercent = dashboard.pettyCash && dashboard.pettyCash.floatAmount > 0 ? dashboard.pettyCash.currentBalance / dashboard.pettyCash.floatAmount * 100 : 0;
  return <main className="finance-page"><FinanceRealtimeRefresh /><header className="finance-heading"><div><p>{dashboard.department.code}</p><h1>{dashboard.department.name}</h1><span>{t("departmentDashboardDetail")}</span></div></header>
    <section className="finance-chart-grid">
      <article className="surface finance-panel"><div className="surface-heading"><h2>{t("myBudget")}</h2><WalletCards /></div><div role="img" aria-label={t("myBudget")}><ResponsiveContainer width="100%" height={280}><PieChart><Pie data={budget} dataKey="value" nameKey="name" innerRadius={62} outerRadius={100}>{budget.map((item, index) => <Cell key={item.name} fill={["var(--amber)", "var(--blue)", "var(--green)"][index]} />)}</Pie><Tooltip formatter={value => money(Number(value))} /><Legend /></PieChart></ResponsiveContainer></div><details className="chart-table"><summary>{t("accessibleTable")}</summary><table><tbody>{budget.map(item => <tr key={item.name}><th>{item.name}</th><td>{money(item.value)}</td></tr>)}</tbody></table></details></article>
      <article className="surface finance-panel"><div className="surface-heading"><h2>{t("pettyCash")}</h2><Banknote /></div>{dashboard.pettyCash ? <div className="petty-gauge"><strong>{money(dashboard.pettyCash.currentBalance, dashboard.pettyCash.currency)}</strong><progress value={Math.max(0, dashboard.pettyCash.currentBalance)} max={dashboard.pettyCash.floatAmount} aria-label={t("pettyCashBalance")} /><span>{Math.round(pettyPercent)}% · {t("imprestThreshold")}: 20%</span></div> : <p className="finance-empty">{t("noPettyCash")}</p>}</article>
    </section>
    <section className="finance-chart-grid"><article className="surface finance-panel"><div className="surface-heading"><h2>{t("spendByCategory")}</h2></div><div role="img" aria-label={t("spendByCategory")}><ResponsiveContainer width="100%" height={300}><Treemap data={dashboard.categories} dataKey="amount" nameKey="category" stroke="var(--surface)" fill="var(--blue)" /></ResponsiveContainer></div><details className="chart-table"><summary>{t("accessibleTable")}</summary><table><tbody>{dashboard.categories.map(item => <tr key={item.category}><th>{item.category}</th><td>{money(item.amount)}</td></tr>)}</tbody></table></details></article>
      <article className="surface finance-panel"><div className="surface-heading"><h2>{t("myPendingApprovals")}</h2><ClipboardCheck /></div><ol className="voucher-list">{dashboard.approvals.map(voucher => <li key={voucher.id}><strong>{voucher.voucherNumber}</strong><span>{voucher.description ?? voucher.voucherType}</span><small>{money(voucher.totalAmount, voucher.currency)}</small></li>)}</ol>{dashboard.approvals.length === 0 && <p className="finance-empty">{t("noPendingApprovals")}</p>}</article></section>
  </main>;
}
