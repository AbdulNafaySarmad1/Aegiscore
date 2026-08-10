"use client";

import { apiDownload, apiPost, ApiError } from "@/lib/api-client";
import { CreditCard, FileDown } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";

type PaymentRequest = {
  success: boolean;
  isSandbox: boolean;
  rtpRequestId: string;
  expiresAt: string;
  deepLink: string | null;
};

export function StudentPaymentAction({ demandId, currency, outstanding }: { demandId: string; currency: string; outstanding: number }) {
  const t = useTranslations("finance");
  const [result, setResult] = useState<PaymentRequest | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [challanBusy, setChallanBusy] = useState(false);
  const gateway = currency === "PKR" ? "raast" : "stripe";

  async function requestPayment() {
    setBusy(true);
    setError(null);
    setResult(null);
    try {
      const response = await apiPost<PaymentRequest>(
        `/api/finance/v2/payments/${gateway}/request-to-pay`,
        { feeDemandId: demandId, feeInstallmentId: null, currency, payerReference: "registered" }
      );
      setResult(response);
      if (response.deepLink) window.location.assign(response.deepLink);
    } catch (caught: unknown) {
      setError(caught instanceof ApiError ? caught.message : t("paymentRequestFailed"));
    } finally {
      setBusy(false);
    }
  }

  async function downloadChallan() {
    setChallanBusy(true);
    setError(null);
    try {
      await apiDownload(
        `/api/student-portal/finance/challan/${encodeURIComponent(demandId)}`,
        { method: "POST", body: "{}" },
        `fee-challan-${demandId}.pdf`
      );
    } catch (caught: unknown) {
      setError(caught instanceof ApiError ? caught.message : "Failed to generate fee challan.");
    } finally {
      setChallanBusy(false);
    }
  }

  return <div className="student-payment-action" style={{ display: "grid", gap: 8 }}>
    <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
      <button className="wizard-primary" type="button" disabled={busy || outstanding <= 0} onClick={() => void requestPayment()}>
        <CreditCard />{busy ? t("requestingPayment") : gateway === "raast" ? t("payRaast") : t("payGateway")}
      </button>
      <button className="wizard-secondary" type="button" disabled={challanBusy} onClick={() => void downloadChallan()}>
        <FileDown />{challanBusy ? "Generating challan..." : "Print / Download Challan"}
      </button>
    </div>
    <div aria-live="polite">
      {error && <p className="finance-alert" role="alert">{error}</p>}
      {result && <p className="payment-request-status"><strong>{t("paymentRequested")}</strong>{result.isSandbox && <b className="sandbox-badge" title={t("sandboxHelp")}>{t("sandbox")}</b>}</p>}
    </div>
  </div>;
}
