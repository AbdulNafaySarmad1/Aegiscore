"use client";

import { apiPost, ApiError } from "@/lib/api-client";
import { CreditCard } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";

type PaymentRequest = { success: boolean; isSandbox: boolean; rtpRequestId: string; expiresAt: string; deepLink: string | null };

export function StudentPaymentAction({ demandId, currency, outstanding }: { demandId: string; currency: string; outstanding: number }) {
  const t = useTranslations("finance");
  const [result, setResult] = useState<PaymentRequest | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const gateway = currency === "PKR" ? "raast" : "stripe";

  async function requestPayment() {
    setBusy(true); setError(null); setResult(null);
    try {
      const response = await apiPost<PaymentRequest>(`/api/finance/v2/payments/${gateway}/request-to-pay`, { feeDemandId: demandId, feeInstallmentId: null, currency, payerReference: "registered" });
      setResult(response);
    }
    catch (caught: unknown) { setError(caught instanceof ApiError ? caught.message : t("paymentRequestFailed")); }
    finally { setBusy(false); }
  }

  return <div className="student-payment-action">
    <button className="wizard-primary" type="button" disabled={busy || outstanding <= 0} onClick={() => void requestPayment()}><CreditCard />{busy ? t("requestingPayment") : gateway === "raast" ? t("payRaast") : t("payGateway")}</button>
    <div aria-live="polite">{error && <p className="finance-alert" role="alert">{error}</p>}{result && <p className="payment-request-status"><strong>{t("paymentRequested")}</strong>{result.isSandbox && <b className="sandbox-badge" title={t("sandboxHelp")}>{t("sandbox")}</b>}</p>}</div>
  </div>;
}
