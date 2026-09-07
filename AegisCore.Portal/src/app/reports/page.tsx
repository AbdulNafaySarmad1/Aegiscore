"use client";
import { useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet } from "@/lib/api-client";

export default function ReportsPage() {
  const [report, setReport] = useState("cash-position");
  const [result, setResult] = useState<unknown>();
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  async function generate() {
    setBusy(true); setError(""); setResult(undefined);
    try { setResult(await apiGet(`/api/finance/v2/reports/${report}`)); }
    catch (reason) { setError(reason instanceof Error ? reason.message : "Report generation failed."); }
    finally { setBusy(false); }
  }
  function download() {
    const url = URL.createObjectURL(new Blob([JSON.stringify(result, null, 2)], { type: "application/json" }));
    const link = document.createElement("a"); link.href = url; link.download = `${report}.json`; link.click(); URL.revokeObjectURL(url);
  }
  return <AppShell><div className="content-page"><h1>Reports</h1><section className="card" style={{ padding: 24 }}>
    <label>Report <select value={report} onChange={event => setReport(event.target.value)}><option value="cash-position">Cash position</option><option value="receivables-aging">Receivables aging</option><option value="statement-of-financial-position">Statement of financial position</option></select></label>
    <button className="btn btn-primary" disabled={busy} onClick={() => void generate()}>{busy ? "Generating…" : "Generate report"}</button>
    {error && <p role="alert">{error}</p>}
    {result !== undefined && <section aria-label="Generated report"><p role="status">Report generated.</p><button onClick={download}>Download JSON</button><pre style={{ overflowX: "auto", whiteSpace: "pre-wrap" }}>{JSON.stringify(result, null, 2)}</pre></section>}
  </section></div></AppShell>;
}
