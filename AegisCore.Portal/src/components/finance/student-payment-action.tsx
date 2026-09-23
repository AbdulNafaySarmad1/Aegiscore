"use client";

import { apiPost, ApiError } from "@/lib/api-client";
import { CreditCard } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";
import { useTenantPreferences } from "@/components/TenantPreferencesProvider";

type PaymentRequest = { success: boolean; isSandbox: boolean; rtpRequestId: string; expiresAt: string; deepLink: string | null };
type HostedCheckout = { success: boolean; basketId: string; actionUrl: string; fields: Record<string, string>; isSandbox: boolean };

/**
 * Sends the browser to PayFast's own payment page. The form carries order fields only (merchant,
 * basket, amount, one-time token, return URLs); the student types their card details on PayFast,
 * never here, which is what keeps AegisCore out of card-data (PCI DSS) scope.
 */
function submitToPayFast(checkout: HostedCheckout) {
  const action = new URL(checkout.actionUrl);
  if (action.protocol !== "https:" || !/(^|\.)apps\.net\.pk$/.test(action.hostname)) throw new Error("Unexpected payment page address.");
  const form = document.createElement("form");
  form.method = "POST";
  form.action = action.toString();
  for (const [name, value] of Object.entries(checkout.fields)) {
    const input = document.createElement("input");
    input.type = "hidden";
    input.name = name;
    input.value = value;
    form.appendChild(input);
  }
  document.body.appendChild(form);
  form.submit();
}

export function StudentPaymentAction({ demandId, currency, outstanding, status }: { demandId: string; currency: string; outstanding: number; status?: string }) {
  const t = useTranslations("finance");
  const [result, setResult] = useState<PaymentRequest | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const { paymentGateway: gateway } = useTenantPreferences();
  const payfast = gateway === "payfast";

  async function requestPayment() {
    setBusy(true); setError(null); setResult(null);
    try {
      if (payfast) {
        const checkout = await apiPost<HostedCheckout>("/api/payments/payfast/checkout", { feeDemandId: demandId });
        submitToPayFast(checkout);
        return; // The page is navigating away; keep the button disabled.
      }
      const response = await apiPost<PaymentRequest>(`/api/finance/v2/payments/${gateway}/request-to-pay`, { feeDemandId: demandId, feeInstallmentId: null, currency, payerReference: "registered" });
      setResult(response);
      setBusy(false);
    }
    catch (caught: unknown) { setError(caught instanceof ApiError ? caught.message : t("paymentRequestFailed")); setBusy(false); }
  }

  const label = busy ? (payfast ? t("redirectingToPayFast") : t("requestingPayment"))
    : payfast ? (status === "PAYMENT_PENDING" ? t("payFastRetry") : t("payFast"))
    : gateway === "raast" ? t("payRaast") : t("payGateway");

  return <div className="student-payment-action">
    {payfast && status === "PAYMENT_PENDING" && <p className="payment-request-status">{t("payFastPending")}</p>}
    <button className="wizard-primary" type="button" disabled={busy || outstanding <= 0} onClick={() => void requestPayment()}><CreditCard />{label}</button>
    {payfast && outstanding > 0 && <p className="finance-note">{t("payFastNote")}</p>}
    <div aria-live="polite">{error && <p className="finance-alert" role="alert">{error}</p>}{result && <p className="payment-request-status"><strong>{t("paymentRequested")}</strong>{result.isSandbox && <b className="sandbox-badge" title={t("sandboxHelp")}>{t("sandbox")}</b>}</p>}</div>
  </div>;
}
