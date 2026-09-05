"use client";

import { AlertTriangle, CircleAlert, RotateCw } from "lucide-react";
import { useState } from "react";

export type WizardError = {
  code: string;
  userMessage: string;
  howToFix: string;
  fixCommand?: string | null;
  technicalDetail?: string | null;
  severity?: "warning" | "error";
};

export function toWizardError(error: unknown, fallback = "Something went wrong — try again"): WizardError {
  if (typeof error === "object" && error !== null) {
    const candidate = error as Partial<WizardError> & { message?: string };
    if (candidate.userMessage) {
      return {
        code: candidate.code ?? "REQUEST_FAILED",
        userMessage: candidate.userMessage,
        howToFix: candidate.howToFix ?? "Check your connection and try again.",
        fixCommand: candidate.fixCommand,
        technicalDetail: candidate.technicalDetail ?? candidate.message,
        severity: candidate.severity ?? "error",
      };
    }
    if (candidate.message) {
      return {
        code: "REQUEST_FAILED",
        userMessage: fallback,
        howToFix: "Check your connection and try again. If the problem continues, contact your administrator.",
        technicalDetail: candidate.message,
        severity: "error",
      };
    }
  }

  return {
    code: "REQUEST_FAILED",
    userMessage: fallback,
    howToFix: "Check your connection and try again. If the problem continues, contact your administrator.",
    technicalDetail: typeof error === "string" ? error : undefined,
    severity: "error",
  };
}

export function ErrorDisplay({ error, onRetry }: { error: WizardError; onRetry?: () => void }) {
  const [showTechnical, setShowTechnical] = useState(false);
  const Icon = error.severity === "warning" ? AlertTriangle : CircleAlert;

  return (
    <section className={`error-display ${error.severity === "warning" ? "warning" : "error"}`} role="alert" aria-live="assertive">
      <Icon aria-hidden="true" />
      <div className="error-display-copy">
        <h2>{error.userMessage}</h2>
        <p>{error.howToFix}</p>
        {error.fixCommand && <code>{error.fixCommand}</code>}
        {error.technicalDetail && (
          <details open={showTechnical} onToggle={event => setShowTechnical(event.currentTarget.open)}>
            <summary>{showTechnical ? "Hide technical details" : "Show technical details"}</summary>
            <pre>{error.technicalDetail}</pre>
          </details>
        )}
      </div>
      {onRetry && <button className="btn btn-secondary" type="button" onClick={onRetry}><RotateCw aria-hidden="true" />Retry</button>}
    </section>
  );
}

export function PageErrorDisplay({ message, onRetry }: { message: string; onRetry?: () => void }) {
  const retry = onRetry ?? (() => window.location.reload());
  return <main className="error-page"><ErrorDisplay error={toWizardError(message, "This page could not be loaded.")} onRetry={retry} /></main>;
}
