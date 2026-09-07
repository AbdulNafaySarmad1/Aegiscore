"use client";

import { use, useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { apiGet, apiPost } from "@/lib/api-client";

type Doc = {
  id: string; documentType: string; mimeType: string; fileSize: number;
  validationStatus: string; rejectionReason: string | null;
  validationConfidence: number | null; validationMethod: string | null; uploadedAt: string;
};
type Detail = {
  applicant: {
    id: string; applicantId: string; fullName: string | null; email: string;
    status: string; submittedAt: string | null; createdAt: string;
  };
  documents: Doc[];
};

const PIPELINE = ["Submitted", "UnderReview", "TestScheduled", "InterviewScheduled", "Admitted", "Rejected", "Withdrawn"];

export default function ApplicantDetailPage({ params }: { params: Promise<{ applicantId: string }> }) {
  const { applicantId } = use(params);
  const [detail, setDetail] = useState<Detail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    try { setDetail(await apiGet<Detail>(`/api/admissions/applicants/${applicantId}`)); }
    catch (e) { setError(e instanceof Error ? e.message : "Failed to load."); }
  }, [applicantId]);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- initial applicant-detail hydration.
    load();
  }, [load]);

  const act = async (fn: () => Promise<unknown>) => {
    setBusy(true); setError(null);
    try { await fn(); await load(); }
    catch (e) { setError(e instanceof Error ? e.message : "Action failed."); }
    finally { setBusy(false); }
  };

  const reviewDoc = (docId: string, approve: boolean) => act(async () => {
    const reason = approve ? undefined : window.prompt("Rejection reason") ?? "Rejected";
    await apiPost(`/api/admissions/applicants/${applicantId}/documents/${docId}/review`, { approve, rejectionReason: reason });
  });

  const transition = (status: string) => act(async () => {
    const note = window.prompt(`Note for moving to ${status} (optional)`) ?? undefined;
    const res = await fetch(`/api/admissions/applicants/${applicantId}/status`, {
      method: "PATCH", credentials: "include",
      headers: { "Content-Type": "application/json" }, body: JSON.stringify({ status, note })
    });
    if (!res.ok) throw new Error((await res.json().catch(() => ({}))).error ?? `Failed (${res.status})`);
  });

  if (!detail) return <div className="content-page">{error ? <div className="error-state">{error}</div> : <div className="skeleton" style={{ height: 200 }} />}</div>;

  const a = detail.applicant;
  return (
    <div className="content-page">
      <div className="page-header">
        <div>
          <Link href="/admin/admissions">← All applicants</Link>
          <h1>{a.fullName ?? a.applicantId}</h1>
          <p>{a.applicantId} · {a.email} · <span className="badge badge-neutral">{a.status}</span></p>
        </div>
      </div>

      {error && <div className="error-state">{error}</div>}

      <div className="card">
        <div className="card-header"><h3>Documents</h3></div>
        <div className="card-body">
          {detail.documents.length === 0 && <p>No documents uploaded yet.</p>}
          {detail.documents.map(d => (
            <div key={d.id} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 12, padding: "10px 0", borderBottom: "1px solid var(--border,#eee)" }}>
              <div>
                <strong>{d.documentType.replace(/_/g, " ")}</strong>{" "}
                <span className={`badge badge-${d.validationStatus === "Approved" ? "success" : d.validationStatus === "Rejected" ? "danger" : "warning"}`}>{d.validationStatus}</span>
                <div style={{ fontSize: 12, color: "var(--muted,#667)" }}>
                  {(d.fileSize / 1024).toFixed(0)} KB · {d.mimeType}
                  {d.validationMethod && <> · check: {d.validationMethod}</>}
                  {d.validationConfidence != null && <> · {Math.round(d.validationConfidence * 100)}%</>}
                  {d.rejectionReason && <> · {d.rejectionReason}</>}
                </div>
              </div>
              <div style={{ display: "flex", gap: 8 }}>
                <a className="btn btn-sm btn-ghost" href={`/api/admissions/applicants/${applicantId}/documents/${d.id}`} target="_blank" rel="noreferrer">View</a>
                <button className="btn btn-sm btn-success" disabled={busy} onClick={() => reviewDoc(d.id, true)}>Approve</button>
                <button className="btn btn-sm btn-danger" disabled={busy} onClick={() => reviewDoc(d.id, false)}>Reject</button>
              </div>
            </div>
          ))}
        </div>
      </div>

      <div className="card" style={{ marginTop: 16 }}>
        <div className="card-header"><h3>Move applicant</h3></div>
        <div className="card-body" style={{ display: "flex", flexWrap: "wrap", gap: 8 }}>
          {PIPELINE.map(s => (
            <button key={s} className={`btn btn-sm ${s === "Rejected" ? "btn-danger" : "btn-secondary"}`} disabled={busy || s === a.status} onClick={() => transition(s)}>
              {s}
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}
