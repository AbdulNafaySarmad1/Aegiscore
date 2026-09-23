"use client";

import { Suspense, useCallback, useEffect, useRef, useState } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { apiGet, apiPost, ApiError } from "@/lib/api-client";
import { loadSession } from "@/lib/session";
import { usePersonalityTranslations } from "@/lib/personality-translator";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";

type Checkout = {
  sessionId: string; basketId: string; feeDemandId: string; demandNumber: string; amount: number; currency: string;
  status: "PENDING" | "PAID" | "FAILED" | "CANCELLED" | "EXPIRED" | "SUPERSEDED" | "NEEDS_REVIEW";
  demandStatus: string; transactionId: string | null; gatewayMessage: string | null; isSandbox: boolean;
};

const copy: Record<Checkout["status"], [string, string, string]> = {
  PAID: ["checkoutPaid", "checkoutPaidDetail", "paid"],
  PENDING: ["checkoutPending", "checkoutPendingDetail", "pending"],
  FAILED: ["checkoutFailed", "checkoutFailedDetail", "failed"],
  CANCELLED: ["checkoutCancelled", "checkoutCancelledDetail", "failed"],
  EXPIRED: ["checkoutExpired", "checkoutExpiredDetail", "failed"],
  SUPERSEDED: ["checkoutSuperseded", "checkoutSupersededDetail", "failed"],
  NEEDS_REVIEW: ["checkoutReview", "checkoutReviewDetail", "pending"],
};

/**
 * Where PayFast sends the student back (SUCCESS_URL / FAILURE_URL). What PayFast appended to the
 * URL is handed to the API, which checks it exactly as it checks the IPN — this page decides
 * nothing itself and shows only what the API then reports.
 */
function CheckoutReturn() {
  const params = useSearchParams();
  const basket = params.get("basket") ?? "";
  const t = usePersonalityTranslations("finance");
  const tenant = useTenantFormatter();
  const [checkout, setCheckout] = useState<Checkout | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [studentId, setStudentId] = useState<string | null>(null);
  const confirmed = useRef(false);

  const refresh = useCallback(async () => {
    try { setCheckout(await apiGet<Checkout>(`/api/payments/payfast/checkout/${encodeURIComponent(basket)}`)); }
    catch (caught) { setError(caught instanceof ApiError && caught.status !== 404 ? caught.message : t("checkoutMissing")); }
  }, [basket, t]);

  useEffect(() => { loadSession().then(s => setStudentId(s.studentId)).catch(() => undefined); }, []);

  useEffect(() => {
    if (!basket || confirmed.current) return;
    confirmed.current = true;
    const returned: Record<string, string> = {};
    params.forEach((value, key) => { if (key !== "basket" && key !== "result") returned[key] = value; });
    const run = returned.validation_hash
      ? apiPost<{ verification: string; checkout: Checkout }>(`/api/payments/payfast/checkout/${encodeURIComponent(basket)}/confirm`, returned).then(r => setCheckout(r.checkout))
      : refresh();
    run.catch(caught => setError(caught instanceof ApiError && caught.status !== 404 ? caught.message : t("checkoutMissing")));
  }, [basket, params, refresh, t]);

  // While PayFast's IPN may still be on its way, look again for up to two minutes.
  useEffect(() => {
    if (checkout?.status !== "PENDING") return;
    let tries = 0;
    const timer = window.setInterval(() => { if (++tries > 24) window.clearInterval(timer); else void refresh(); }, 5000);
    return () => window.clearInterval(timer);
  }, [checkout?.status, refresh]);

  async function cancel() {
    setBusy(true);
    try { setCheckout(await apiPost<Checkout>(`/api/payments/payfast/checkout/${encodeURIComponent(basket)}/cancel`)); }
    catch (caught) { setError(caught instanceof ApiError ? caught.message : t("checkoutMissing")); }
    finally { setBusy(false); }
  }

  const back = studentId ? `/student/finance/${encodeURIComponent(studentId)}` : "/student/finance";
  const money = (value: number, currency: string) => new Intl.NumberFormat(tenant.locale, { style: "currency", currency }).format(value);
  const [title, detail, tone] = checkout ? copy[checkout.status] : ["checkoutTitle", "checkoutChecking", "pending"];

  return <main id="main-content" className="finance-page" tabIndex={-1}>
    <header className="finance-heading"><div><p>{t("checkoutTitle")}</p><h1>{t(title)}</h1></div></header>
    <section className="surface finance-panel" aria-live="polite">
      {error ? <p className="finance-alert" role="alert">{error}</p> : <>
        <p className={`status-pill ${tone}`}>{t(detail)}</p>
        {checkout && <dl className="checkout-facts">
          <div><dt>{t("checkoutChallan")}</dt><dd>{checkout.demandNumber}</dd></div>
          <div><dt>{t("checkoutAmount")}</dt><dd>{money(checkout.amount, checkout.currency)}</dd></div>
          {checkout.transactionId && <div><dt>{t("checkoutReference")}</dt><dd><code>{checkout.transactionId}</code></dd></div>}
          {checkout.gatewayMessage && checkout.status !== "PAID" && <div><dt>PayFast</dt><dd>{checkout.gatewayMessage}</dd></div>}
        </dl>}
        {checkout?.isSandbox && <b className="sandbox-badge" title={t("sandboxHelp")}>{t("sandbox")}</b>}
      </>}
      <div className="student-payment-action">
        {checkout?.status === "PENDING" && <>
          <button className="wizard-secondary" type="button" disabled={busy} onClick={() => void refresh()}>{t("checkoutRefresh")}</button>
          <button className="wizard-secondary" type="button" disabled={busy} onClick={() => void cancel()}>{t("checkoutCancel")}</button>
        </>}
        <Link className="wizard-primary" href={back}>{t("checkoutBack")}</Link>
      </div>
    </section>
  </main>;
}

export default function CheckoutReturnPage() {
  return <Suspense fallback={<main id="main-content" className="finance-page" tabIndex={-1}><p>…</p></main>}><CheckoutReturn /></Suspense>;
}
