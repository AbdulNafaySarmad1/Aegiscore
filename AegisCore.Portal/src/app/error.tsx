"use client";

import { useEffect } from "react";
import { ErrorDisplay, toWizardError } from "@/components/ErrorDisplay";

export default function ErrorPage({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) {
  useEffect(() => { console.error("AegisCore route error", error); }, [error]);
  return <main className="error-page"><ErrorDisplay error={toWizardError(error)} onRetry={reset} /></main>;
}
