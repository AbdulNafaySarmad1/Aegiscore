"use client";

import { useEffect } from "react";
import { ErrorDisplay, toWizardError } from "@/components/ErrorDisplay";

export default function GlobalError({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) {
  useEffect(() => { console.error("AegisCore root error", error); }, [error]);
  return <html lang="en"><body><main className="error-page"><ErrorDisplay error={toWizardError(error, "Something went wrong")} onRetry={reset} /></main></body></html>;
}
