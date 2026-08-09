"use client";

// ─── Transcript ───
// No fabricated grades: the semester grade feed requires an authoritative
// studentId that the session model does not yet provide
// (docs/FRONTEND_API_HANDOFF.md). What IS real and wired here:
//   - POST /api/examination/transcripts/{studentId}/request (when linked)
//   - GET  /api/examination/transcripts/verify/{hash} (public verification)

import { useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet, apiPost, isApiError } from "@/lib/api-client";
import { useSession } from "@/components/session-provider";
import { EmptyState } from "@/components/async/states";
import { FileText, Download, CheckCircle, ShieldCheck, XCircle } from "lucide-react";

export default function TranscriptPage() {
  const { session } = useSession();
  const [requested, setRequested] = useState(false);
  const [requesting, setRequesting] = useState(false);
  const [requestError, setRequestError] = useState("");

  const [hash, setHash] = useState("");
  const [verifying, setVerifying] = useState(false);
  const [verifyResult, setVerifyResult] = useState<boolean | null>(null);

  const requestTranscript = async () => {
    if (!session?.studentId) return;
    setRequesting(true); setRequestError("");
    try {
      await apiPost(`/api/examination/transcripts/${encodeURIComponent(session.studentId)}/request`, {
        requestedBy: session.userId,
        reason: "Student portal request",
      });
      setRequested(true);
    } catch (e) {
      setRequestError(isApiError(e) ? e.message : "The request could not be submitted.");
    } finally {
      setRequesting(false);
    }
  };

  const verify = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!hash.trim()) return;
    setVerifying(true); setVerifyResult(null);
    try {
      setVerifyResult(await apiGet<boolean>(`/api/examination/transcripts/verify/${encodeURIComponent(hash.trim())}`));
    } catch {
      setVerifyResult(false);
    } finally {
      setVerifying(false);
    }
  };

  return (
    <AppShell>
      <div className="content-page">
        <div className="page-header">
          <div>
            <div className="page-header-breadcrumb"><span>Student</span><span>/</span><span>Transcript</span></div>
            <h1>Transcript</h1>
            <p>Official academic record requests and verification.</p>
          </div>
          {session?.studentId && (
            <button className="btn btn-primary" onClick={requestTranscript} disabled={requesting || requested}>
              {requested ? <><CheckCircle size={16} />Requested</> : <><Download size={16} />{requesting ? "Submitting…" : "Request Official Copy"}</>}
            </button>
          )}
        </div>

        {requestError && <div className="error-state" role="alert" style={{ marginBottom: 16 }}><XCircle size={18} /><p>{requestError}</p></div>}
        {requested && <div className="form-notice info" role="status" style={{ marginBottom: 16 }}>Your transcript request was submitted. The Registrar&rsquo;s office will process it.</div>}

        {!session?.studentId ? (
          <EmptyState
            icon={FileText}
            title="Academic record not linked"
            description="Your semester grades and transcript appear here once the Registrar links your student record to this account. No placeholder records are shown."
          />
        ) : (
          <EmptyState
            icon={FileText}
            title="No published transcript yet"
            description="Your transcript will appear here once semester results are published by the Examination office."
          />
        )}

        <div className="card" style={{ maxWidth: 720, marginTop: 8 }}>
          <div className="card-header">
            <h3 style={{ fontSize: 14, display: "flex", alignItems: "center", gap: 8 }}>
              <ShieldCheck size={16} /> Verify a Transcript
            </h3>
          </div>
          <div className="card-body">
            <form onSubmit={verify} style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
              <label className="sr-only" htmlFor="verify-hash">Transcript verification hash</label>
              <input
                id="verify-hash"
                value={hash}
                onChange={e => { setHash(e.target.value); setVerifyResult(null); }}
                placeholder="Paste the verification hash printed on the transcript"
                style={{ flex: 1, minWidth: 240 }}
              />
              <button type="submit" className="btn btn-secondary" disabled={verifying || !hash.trim()}>
                {verifying ? "Verifying…" : "Verify"}
              </button>
            </form>
            {verifyResult !== null && (
              <p role="status" style={{ marginTop: 12, fontSize: 13, display: "flex", alignItems: "center", gap: 6, color: verifyResult ? "var(--success)" : "var(--danger)" }}>
                {verifyResult ? <><CheckCircle size={16} /> This transcript hash is authentic.</> : <><XCircle size={16} /> This hash could not be verified.</>}
              </p>
            )}
          </div>
        </div>
      </div>
    </AppShell>
  );
}
