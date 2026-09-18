"use client";

import { useState, type FormEvent } from "react";
import { apiPost, ApiError } from "@/lib/api-client";
import { RegisterHead, Panel, Figures, Notice, Mark } from "@/components/register";

/**
 * Text comparison. A similarity score is evidence in a misconduct case, so this compares two texts
 * you supply and reports the overlap it actually measured — it does not scan one submission against
 * a corpus this installation does not hold.
 */
export default function PlagiarismPage() {
  const [a, setA] = useState("");
  const [b, setB] = useState("");
  const [busy, setBusy] = useState(false);
  const [result, setResult] = useState<{ similarity: number; words: number } | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function compare(e: FormEvent) {
    e.preventDefault();
    setBusy(true); setError(null); setResult(null);
    try {
      const similarity = await apiPost<number>(`/api/plagiarism-engine/similarity?textA=${encodeURIComponent(a.trim())}&textB=${encodeURIComponent(b.trim())}`);
      setResult({ similarity: Number(similarity), words: new Set(a.trim().toLowerCase().split(/\s+/)).size });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "The comparison did not complete.");
    } finally { setBusy(false); }
  }

  const tone = result === null ? "ink" : result.similarity >= 0.6 ? "oxblood" : result.similarity >= 0.3 ? "brass" : "laurel";
  return (
    <div className="page">
      <RegisterHead
        title="Text comparison"
        crumbs={[{ label: "Academic" }, { label: "Text comparison" }]}
        ident={result === null ? undefined : <><strong>{(result.similarity * 100).toFixed(1)}%</strong><br />word overlap</>}
        meta={<span>Measures the shared vocabulary between two texts. It is a starting point for a human reading, not a finding of misconduct.</span>}
      />

      <Notice tone="ink" title="What this does and does not do.">
        It compares the two texts you paste and reports the proportion of vocabulary they share.
        It does not search the internet, a paper mill, or past submissions — this installation keeps
        no corpus — so a low figure here is not a clearance and a high one is not a verdict.
      </Notice>

      {error && <Notice tone="oxblood" role="alert">{error}</Notice>}

      {result !== null && (
        <>
          <Figures aria-label="Comparison result" items={[
            { label: "Shared vocabulary", value: `${(result.similarity * 100).toFixed(1)}%`, tone, note: "of the words in both texts" },
            { label: "Distinct words compared", value: result.words, note: "in the first text" },
            { label: "Reading", value: result.similarity >= 0.6 ? "High overlap" : result.similarity >= 0.3 ? "Some overlap" : "Little overlap", tone, note: "for a person to judge" },
          ]} />
          <Notice tone={tone} role="status">
            {result.similarity >= 0.6
              ? "These texts share most of their vocabulary. Read both before drawing any conclusion — shared subject matter and quoted material raise this figure too."
              : result.similarity >= 0.3
                ? "These texts share some vocabulary, which is ordinary for two answers to the same question."
                : "These texts share little vocabulary."}
          </Notice>
        </>
      )}

      <Panel title="Compare two texts">
        <form className="panel-form" onSubmit={compare}>
          <label><span>First text</span><textarea value={a} onChange={e => setA(e.target.value)} rows={8} required /></label>
          <label><span>Second text</span><textarea value={b} onChange={e => setB(e.target.value)} rows={8} required /></label>
          <div className="btn-row">
            <button type="submit" className={`btn btn-primary${busy ? " btn-loading" : ""}`} disabled={busy || !a.trim() || !b.trim()}>{busy ? "Comparing" : "Compare"}</button>
            <button type="button" className="btn btn-ghost" onClick={() => { setA(""); setB(""); setResult(null); setError(null); }} disabled={busy}>Clear</button>
          </div>
        </form>
      </Panel>

      <Panel title="Scanning against a corpus">
        <p>
          Scanning one submission against everything a cohort has ever written needs a stored corpus of
          submissions, which this installation does not keep, or an external integrity service.{" "}
          <Mark tone="brass">Not available here</Mark> — until one is configured, a scan would answer with a
          number that means nothing, so the endpoint refuses instead.
        </p>
      </Panel>
    </div>
  );
}
