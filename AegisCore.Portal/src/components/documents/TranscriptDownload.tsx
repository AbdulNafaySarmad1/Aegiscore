"use client";

import { useState } from "react";
import { FileDown } from "lucide-react";
import type { TranscriptData } from "@/lib/pdf/transcript";

type Status = "idle" | "preparing" | "done" | "error";

/** Builds the transcript PDF in the browser on demand; the renderer is loaded only when clicked. */
export function TranscriptDownload({ data, fileName, className = "btn btn-primary" }: { data: () => Omit<TranscriptData, "generatedAt" | "reference">; fileName: string; className?: string }) {
  const [status, setStatus] = useState<Status>("idle");
  const [message, setMessage] = useState<string | null>(null);

  async function download() {
    setStatus("preparing"); setMessage(null);
    try {
      const [{ pdf }, { TranscriptDocument, registerFonts }] = await Promise.all([
        import("@react-pdf/renderer"),
        import("@/lib/pdf/transcript"),
      ]);
      registerFonts(window.location.origin);
      const generatedAt = new Date();
      const reference = `TR-${generatedAt.getFullYear()}${String(generatedAt.getMonth() + 1).padStart(2, "0")}${String(generatedAt.getDate()).padStart(2, "0")}-${Math.random().toString(36).slice(2, 8).toUpperCase()}`;
      const blob = await pdf(<TranscriptDocument data={{ ...data(), generatedAt, reference }} />).toBlob();
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url; link.download = fileName; link.rel = "noopener";
      document.body.appendChild(link); link.click(); link.remove();
      setTimeout(() => URL.revokeObjectURL(url), 10_000);
      setStatus("done");
      setMessage("Transcript saved as a PDF.");
    } catch (error) {
      setStatus("error");
      setMessage(error instanceof Error ? `The PDF could not be prepared: ${error.message}` : "The PDF could not be prepared.");
    }
  }

  return (
    <span style={{ display: "inline-grid", gap: 4, justifyItems: "end" }}>
      <button type="button" className={`${className}${status === "preparing" ? " btn-loading" : ""}`} disabled={status === "preparing"} onClick={download} aria-describedby={message ? "transcript-download-status" : undefined}>
        <FileDown aria-hidden="true" />
        {status === "preparing" ? "Preparing PDF" : "Download transcript (PDF)"}
      </button>
      {message && <span id="transcript-download-status" role="status" className={status === "error" ? "field-error" : "field-hint"}>{message}</span>}
    </span>
  );
}
