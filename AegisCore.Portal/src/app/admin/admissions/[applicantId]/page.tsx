"use client";

import { use, useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { apiGet, apiPost } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { RegisterHead, Panel, Facts, Figures, Mark, RuledLoading, ErrorNotice, Empty, PromptDialog, useAnnounce, type Tone } from "@/components/register";

type Doc = {
  id: string; documentType: string | number; mimeType: string; fileSize: number;
  validationStatus: string | number; rejectionReason: string | null;
  validationConfidence: number | null; validationMethod: string | null; uploadedAt: string;
};
type Detail = {
  applicant: {
    id: string; applicantId: string; fullName: string | null; email: string;
    status: string | number; submittedAt: string | null; createdAt: string;
  };
  documents: Doc[];
  test: { score: number | null; passed: boolean | null; status: string | number; completedAt: string | null } | null;
};

const PIPELINE = ["Submitted", "UnderReview", "TestScheduled", "InterviewScheduled", "Admitted", "Rejected", "Withdrawn"] as const;
const APPLICANT_STATUSES = ["Draft", ...PIPELINE];
const STATUS_LABEL: Record<string, string> = {
  Draft: "Draft", Submitted: "Submitted", UnderReview: "Under review", TestScheduled: "Test scheduled",
  InterviewScheduled: "Interview scheduled", Admitted: "Admitted", Rejected: "Rejected", Withdrawn: "Withdrawn",
};
const STATUS_TONE: Record<string, Tone> = {
  Draft: "ink", Submitted: "ink", UnderReview: "brass", TestScheduled: "brass", InterviewScheduled: "brass",
  Admitted: "laurel", Rejected: "oxblood", Withdrawn: "oxblood",
};
const MOVE_LABEL: Record<string, string> = {
  Submitted: "Return to submitted", UnderReview: "Shortlist for review", TestScheduled: "Schedule entry test",
  InterviewScheduled: "Schedule interview", Admitted: "Admit applicant", Rejected: "Reject application", Withdrawn: "Record withdrawal",
};
const TEST_STATUSES = ["Scheduled", "In progress", "Submitted", "Expired", "Force submitted"];
const DOCUMENT_TYPES = ["Matric", "Intermediate", "O Levels", "A Levels", "IBCC Equivalency", "National ID", "Domicile Certificate", "Photo", "Other"];
const DOCUMENT_STATUSES = ["Pending", "Approved", "Rejected", "Manual review"];
const DOC_TONE: Record<string, Tone> = { Pending: "ink", Approved: "laurel", Rejected: "oxblood", "Manual review": "brass" };
const label = (value: string | number, values: readonly string[]) => typeof value === "number" ? values[value - 1] ?? String(value) : value;
const kb = (bytes: number) => bytes >= 1048576 ? `${(bytes / 1048576).toFixed(1)} MB` : `${Math.max(1, Math.round(bytes / 1024))} KB`;

type Pending = { kind: "reject-doc"; docId: string; name: string } | { kind: "move"; status: string };

export default function ApplicantDetailPage({ params }: { params: Promise<{ applicantId: string }> }) {
  const { applicantId } = use(params);
  const { date, dateTime } = useTenantFormatter();
  const [detail, setDetail] = useState<Detail | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [busy, setBusy] = useState<string | null>(null);
  const [pending, setPending] = useState<Pending | null>(null);
  const [retry, setRetry] = useState(0);
  const { announce, region } = useAnnounce();

  const load = useCallback(async () => {
    setLoadError(null);
    try { setDetail(await apiGet<Detail>(`/api/admissions/applicants/${applicantId}`)); }
    catch (e) { setLoadError(e instanceof Error ? e.message : "The applicant record could not be loaded."); }
  }, [applicantId]);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- initial applicant-detail hydration.
    load();
  }, [load, retry]);

  const act = async (key: string, fn: () => Promise<unknown>, done: string) => {
    setBusy(key); setActionError(null);
    try { await fn(); await load(); announce(done); }
    catch (e) { setActionError(e instanceof Error ? e.message : "The action did not complete."); }
    finally { setBusy(null); setPending(null); }
  };

  const approveDoc = (d: Doc) => act(`doc-${d.id}`, () =>
    apiPost(`/api/admissions/applicants/${applicantId}/documents/${d.id}/review`, { approve: true }),
    `${label(d.documentType, DOCUMENT_TYPES)} approved.`);

  const rejectDoc = (docId: string, reason: string) => act(`doc-${docId}`, () =>
    apiPost(`/api/admissions/applicants/${applicantId}/documents/${docId}/review`, { approve: false, rejectionReason: reason }),
    "Document rejected.");

  const move = (status: string, note: string) => act(`move-${status}`, async () => {
    const res = await fetch(`/api/admissions/applicants/${applicantId}/status`, {
      method: "PATCH", credentials: "include",
      headers: { "Content-Type": "application/json" }, body: JSON.stringify({ status, note: note || undefined })
    });
    if (!res.ok) throw new Error((await res.json().catch(() => ({}))).error ?? `The status change was refused (${res.status}).`);
  }, `Applicant moved to ${STATUS_LABEL[status] ?? status}.`);

  if (loadError) return (
    <div className="page">
      <RegisterHead title="Applicant record" crumbs={[{ label: "Admissions office", href: "/admin/admissions" }, { label: "Applicant" }]} reveal={false} />
      <ErrorNotice message={loadError} hint="The record may have been withdrawn, or the university API is not reachable." onRetry={() => setRetry(n => n + 1)} />
    </div>
  );
  if (!detail) return <div className="page"><RuledLoading label="Opening applicant record" rows={5} /></div>;

  const a = detail.applicant;
  const status = label(a.status, APPLICANT_STATUSES);
  const approvedDocs = detail.documents.filter(d => label(d.validationStatus, DOCUMENT_STATUSES) === "Approved").length;

  return (
    <div className="page">
      {region}
      <RegisterHead
        title={a.fullName ?? a.applicantId}
        crumbs={[{ label: "Admissions office", href: "/admin/admissions" }, { label: "Applicant" }]}
        ident={<><strong>{a.applicantId}</strong><br />Application</>}
        meta={<><Mark tone={STATUS_TONE[status] ?? "ink"}>{STATUS_LABEL[status] ?? status}</Mark><span>{a.email}</span>{a.submittedAt && <span>Submitted {date(new Date(a.submittedAt))}</span>}</>}
      />

      {actionError && <ErrorNotice message={actionError} hint="The record was not changed. Try the action again." />}

      <div className="record">
        <div className="record-body">
          <Panel title="Documents" aside={<span>{approvedDocs} of {detail.documents.length} approved</span>} flush>
            {detail.documents.length === 0 ? (
              <Empty title="No documents uploaded yet.">The applicant can upload certificates and identity documents from their application portal.</Empty>
            ) : (
              <div className="ledger-wrap">
                <table className="ledger" aria-label="Uploaded documents">
                  <thead><tr><th scope="col">Document</th><th scope="col">Automatic check</th><th scope="col">Status</th><th scope="col"><span className="sr-only">Actions</span></th></tr></thead>
                  <tbody>
                    {detail.documents.map(d => {
                      const s = label(d.validationStatus, DOCUMENT_STATUSES);
                      const name = label(d.documentType, DOCUMENT_TYPES).replace(/_/g, " ");
                      const rowBusy = busy === `doc-${d.id}`;
                      return (
                        <tr key={d.id} data-status={DOC_TONE[s] ?? "ink"}>
                          <td><strong>{name}</strong><span className="cell-sub nowrap">{kb(d.fileSize)} {d.mimeType.split("/")[1]?.toUpperCase()}, uploaded {date(new Date(d.uploadedAt))}</span></td>
                          <td className="nowrap">{d.validationMethod ? <>{d.validationMethod}{d.validationConfidence != null && <span className="cell-sub">{Math.round(d.validationConfidence * 100)}% confidence</span>}</> : <span className="text-muted">Not checked</span>}</td>
                          <td><Mark tone={DOC_TONE[s] ?? "ink"}>{s}</Mark>{d.rejectionReason && <span className="cell-sub">{d.rejectionReason}</span>}</td>
                          <td className="actions">
                            <div className="cell-actions">
                              <a className="btn btn-quiet btn-sm" href={`/api/admissions/applicants/${applicantId}/documents/${d.id}`} target="_blank" rel="noreferrer">Open<span className="sr-only"> {name} in a new tab</span></a>
                              {s !== "Approved" && <button type="button" className={`btn btn-laurel btn-sm${rowBusy ? " btn-loading" : ""}`} disabled={busy !== null} onClick={() => approveDoc(d)}>Approve<span className="sr-only"> {name}</span></button>}
                              {s !== "Rejected" && <button type="button" className="btn btn-danger btn-sm" disabled={busy !== null} onClick={() => setPending({ kind: "reject-doc", docId: d.id, name })}>Reject<span className="sr-only"> {name}</span></button>}
                            </div>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </Panel>

          <Panel title="Entry test" aside={detail.test ? <Mark tone={detail.test.passed ? "laurel" : detail.test.passed === false ? "oxblood" : "ink"}>{detail.test.passed ? "Passed" : detail.test.passed === false ? "Not passed" : "Awaiting result"}</Mark> : undefined} flush>
            {detail.test ? (
              <Figures aria-label="Entry test result" items={[
                { label: "Score", value: detail.test.score != null ? `${detail.test.score}%` : "—", tone: detail.test.passed ? "laurel" : detail.test.passed === false ? "oxblood" : undefined, note: detail.test.passed ? "meets the admission threshold" : detail.test.passed === false ? "below the admission threshold" : "awaiting marking" },
                { label: "Test status", value: label(detail.test.status, TEST_STATUSES), note: detail.test.completedAt ? `Completed ${dateTime(new Date(detail.test.completedAt))}` : "Not yet completed" },
              ]} />
            ) : (
              <Empty title="No entry test on record.">Schedule the test from the applicant&rsquo;s actions once documents are approved.</Empty>
            )}
          </Panel>
        </div>

        <aside className="record-margin" aria-label="Applicant record details">
          <Panel title="Record">
            <Facts items={[
              { label: "Application", value: a.applicantId, mono: true },
              { label: "Status", value: <Mark tone={STATUS_TONE[status] ?? "ink"}>{STATUS_LABEL[status] ?? status}</Mark> },
              { label: "Email", value: a.email },
              { label: "Created", value: date(new Date(a.createdAt)), mono: true },
              { label: "Submitted", value: a.submittedAt ? date(new Date(a.submittedAt)) : "Not submitted", mono: Boolean(a.submittedAt) },
              { label: "Documents", value: `${approvedDocs} of ${detail.documents.length} approved` },
            ]} />
          </Panel>

          <Panel title="Move applicant" foot={<span>Each move is recorded with your name and an optional note.</span>}>
            <div className="btn-row" style={{ flexDirection: "column", alignItems: "stretch" }}>
              {PIPELINE.map(s => {
                const current = s === status;
                const danger = s === "Rejected" || s === "Withdrawn";
                return (
                  <button key={s} type="button" className={`btn ${danger ? "btn-danger" : s === "Admitted" ? "btn-primary" : "btn-secondary"}${busy === `move-${s}` ? " btn-loading" : ""}`} disabled={busy !== null || current} aria-disabled={current || undefined} onClick={() => setPending({ kind: "move", status: s })} title={current ? "The applicant is already at this stage" : undefined}>
                    {MOVE_LABEL[s]}
                  </button>
                );
              })}
            </div>
          </Panel>
          <p className="text-muted" style={{ fontSize: "var(--t-13)" }}><Link href="/admin/admissions">Back to all applicants</Link></p>
        </aside>
      </div>

      {pending?.kind === "reject-doc" && (
        <PromptDialog
          title={`Reject ${pending.name}`}
          description="The applicant will see this reason and can upload a replacement."
          label="Reason for rejection" placeholder="For example: the scan is unreadable, or the name does not match the application."
          required confirmLabel="Record rejection" tone="oxblood" busy={busy !== null}
          onConfirm={reason => rejectDoc(pending.docId, reason)} onCancel={() => setPending(null)} />
      )}
      {pending?.kind === "move" && (
        <PromptDialog
          title={MOVE_LABEL[pending.status] ?? pending.status}
          description={<>This moves <strong>{a.fullName ?? a.applicantId}</strong> from {STATUS_LABEL[status] ?? status} to {STATUS_LABEL[pending.status] ?? pending.status}.</>}
          label="Note for the file" placeholder="Anything the next reviewer should know."
          confirmLabel={MOVE_LABEL[pending.status] ?? "Confirm"} tone={pending.status === "Rejected" || pending.status === "Withdrawn" ? "oxblood" : "ink"} busy={busy !== null}
          onConfirm={note => move(pending.status, note)} onCancel={() => setPending(null)} />
      )}
    </div>
  );
}
