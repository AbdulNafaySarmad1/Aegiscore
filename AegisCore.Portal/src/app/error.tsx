"use client";

import { useEffect } from "react";
import { ErrorDisplay, toWizardError } from "@/components/ErrorDisplay";

export default function ErrorPage({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) {
  useEffect(() => { console.error("AegisCore route error", error); }, [error]);
  return <main id="main-content" className="error-page" tabIndex={-1}><ErrorDisplay error={toWizardError(error, "This page stopped responding.")} onRetry={reset} /></main>;
}
