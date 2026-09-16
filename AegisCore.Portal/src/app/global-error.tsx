"use client";

import { useEffect } from "react";
import { ErrorDisplay, toWizardError } from "@/components/ErrorDisplay";
import { fontClassName } from "@/lib/fonts";
import "./globals.css";

export default function GlobalError({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) {
  useEffect(() => { console.error("AegisCore root error", error); }, [error]);
  return (
    <html lang="en" className={fontClassName}>
      <body>
        <main id="main-content" className="error-page" tabIndex={-1}>
          <ErrorDisplay error={toWizardError(error, "AegisCore could not start.")} onRetry={reset} />
        </main>
      </body>
    </html>
  );
}
