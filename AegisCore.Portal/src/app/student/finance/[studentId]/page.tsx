"use client";

import { use, useEffect, useState } from "react";
import { apiGet } from "@/lib/api-client";
import { usePersonalityTranslations } from "@/lib/personality-translator";
import { StudentPaymentAction } from "@/components/finance/student-payment-action";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";

type StudentFinance = {
  studentId: string;
  demands: Array<{ id: string; demandNumber: string; currency: string; totalAmount: number; paidAmount: number; outstandingAmount: number; status: string; issuedAt: string; dueDate: string }>;
  installments: Array<{ id: string; feeDemandId: string; sequence: number; amount: number; paidAmount: number; penaltyAmount: number; dueDate: string; status: string }>;
  payments: Array<{ id: string; feeDemandId: string; amount: number; method: string; gatewaySlug: string | null; externalReference: string | null; status: string; createdAt: string; settledAt: string | null }>;
  awards: Array<{ id: string; scholarshipType: string; coverageType: string; amount: number; disbursementMethod: string; status: string; awardedAt: string; renewalReviewAt: string | null }>;
};

export default function StudentFinancePage({ params }: { params: Promise<{ studentId: string }> }) {
  const { studentId } = use(params);
  const tenant = useTenantFormatter();
  const t = usePersonalityTranslations("finance");
  const [data, setData] = useState<StudentFinance | null>(null);
  const [error, setError] = useState<boolean>(false);

  useEffect(() => {
    apiGet<StudentFinance>(`/api/finance/v2/student-portal/${encodeURIComponent(studentId)}`)
      .then(setData)
      .catch(() => setError(true));
  }, [studentId]);

  const money = (value: number, currency: string) => new Intl.NumberFormat(tenant.locale, { style: "currency", currency }).format(value);
  const date = (value: string) => new Intl.DateTimeFormat(tenant.locale, { dateStyle: "medium" }).format(new Date(value));

  if (error) return <main className="finance-page"><p className="finance-alert" role="alert">{t("studentUnavailable")}</p></main>;
  if (!data) return <main className="finance-page"><p>{t("feesStatusLoading")}</p></main>;
  return <main className="finance-page"><header className="finance-heading"><div><p>{t("studentPortal")}</p><h1>{t("feesAid")}</h1><span>{t("studentDetail")}</span></div></header>
    <section className="statement-timeline" aria-label={t("feeStatement")}>{data.demands.map(demand => <article className="surface finance-panel" key={demand.id}><div className="surface-heading"><div><h2>{demand.demandNumber}</h2><p>{t("due", { date: date(demand.dueDate) })}</p></div><b className={`status-pill ${demand.status.toLowerCase()}`}>{demand.status}</b></div><div className="finance-funnel"><span><strong>{money(demand.totalAmount, demand.currency)}</strong>{t("total")}</span><span><strong>{money(demand.paidAmount, demand.currency)}</strong>{t("paid")}</span><span><strong>{money(demand.outstandingAmount, demand.currency)}</strong>{t("outstanding")}</span></div>{data.installments.filter(x => x.feeDemandId === demand.id).map(item => <div className="installment-row" key={item.id}><span>{t("installment", { sequence: item.sequence })}</span><progress max={item.amount} value={item.paidAmount} aria-label={t("installmentProgress", { sequence: item.sequence })} /><strong>{money(item.amount + item.penaltyAmount, demand.currency)}</strong><time>{date(item.dueDate)}</time></div>)}<StudentPaymentAction demandId={demand.id} currency={demand.currency} outstanding={demand.outstandingAmount} /></article>)}{data.demands.length === 0 && <p className="finance-empty">{t("noDemands")}</p>}</section>
    <section className="surface finance-panel"><div className="surface-heading"><div><h2>{t("payments")}</h2><p>{t("receiptDetail")}</p></div></div>{data.payments.map(payment => <div className="installment-row" key={payment.id}><span>{payment.method}</span><strong>{money(payment.amount, data.demands.find(x => x.id === payment.feeDemandId)?.currency ?? tenant.currency)}</strong><span>{payment.status === "SETTLED" ? t("receiptAvailable") : payment.status}</span><time>{date(payment.createdAt)}</time></div>)}{data.payments.length === 0 && <p className="finance-empty">{t("noPayments")}</p>}</section>
    <section className="surface finance-panel"><div className="surface-heading"><div><h2>{t("scholarships")}</h2><p>{t("scholarshipsDetail")}</p></div></div>{data.awards.map(award => <div className="installment-row" key={award.id}><strong>{award.scholarshipType}</strong><span>{award.coverageType}</span><span>{money(award.amount, tenant.currency)}</span><span>{award.status}</span></div>)}{data.awards.length === 0 && <p className="finance-empty">{t("noAwards")}</p>}</section>
  </main>;
}
