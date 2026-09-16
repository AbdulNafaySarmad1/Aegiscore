"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { apiGet, apiPost } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { RegisterHead, RuledLoading, ErrorNotice, Empty, Mark, Notice, useAnnounce, type Tone } from "@/components/register";

type Intake = { id: string; year: number; semester: number; name: string; status: number };
type Applicant = {
  id: string; applicantId: string; fullName: string | null; email: string;
  programId: string; intakeId: string; status: string | number;
  submittedAt: string | null; createdAt: string;
};

const STATUSES = ["Draft", "Submitted", "UnderReview", "TestScheduled", "InterviewScheduled", "Admitted", "Rejected", "Withdrawn"] as const;
const STATUS_LABEL: Record<string, string> = {
  Draft: "Draft", Submitted: "Submitted", UnderReview: "Under review", TestScheduled: "Test scheduled",
  InterviewScheduled: "Interview scheduled", Admitted: "Admitted", Rejected: "Rejected", Withdrawn: "Withdrawn",
};
const STATUS_TONE: Record<string, Tone> = {
  Draft: "ink", Submitted: "ink", UnderReview: "brass", TestScheduled: "brass", InterviewScheduled: "brass",
  Admitted: "laurel", Rejected: "oxblood", Withdrawn: "oxblood",
};
const statusKey = (value: string | number) => typeof value === "number" ? STATUSES[value - 1] ?? String(value) : value;

export default function AdmissionsAdminPage() {
  const { date } = useTenantFormatter();
  const [intakes, setIntakes] = useState<Intake[]>([]);
  const [rows, setRows] = useState<Applicant[] | null>(null);
  const [intakeId, setIntakeId] = useState("");
  const [status, setStatus] = useState("");
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [retry, setRetry] = useState(0);
  const headerCheck = useRef<HTMLInputElement>(null);
  const { announce, region } = useAnnounce();

  const load = useCallback(async (signal?: AbortSignal) => {
    setError(null);
    const params = new URLSearchParams();
    if (intakeId) params.set("intakeId", intakeId);
    if (status) params.set("status", status);
    try {
      const data = await apiGet<Applicant[]>(`/api/admissions/applicants?${params}`, signal);
      setRows(data);
    } catch (e) {
      if (signal?.aborted) return;
      setRows([]);
      setError(e instanceof Error ? e.message : "The applicant list could not be loaded.");
    }
  }, [intakeId, status]);

  useEffect(() => { apiGet<Intake[]>("/api/admissions/intakes").then(setIntakes).catch(() => {}); }, []);
  useEffect(() => {
    const abort = new AbortController();
    // eslint-disable-next-line react-hooks/set-state-in-effect -- reload applicants when filters change.
    setRows(null);
    void load(abort.signal);
    return () => abort.abort();
  }, [load, retry]);

  const visibleIds = useMemo(() => (rows ?? []).map(r => r.id), [rows]);
  const allSelected = visibleIds.length > 0 && visibleIds.every(id => selected.has(id));
  const someSelected = visibleIds.some(id => selected.has(id));
  useEffect(() => { if (headerCheck.current) headerCheck.current.indeterminate = someSelected && !allSelected; }, [someSelected, allSelected]);

  const toggle = (id: string) => setSelected(s => { const n = new Set(s); if (n.has(id)) n.delete(id); else n.add(id); return n; });
  const toggleAll = () => setSelected(allSelected ? new Set() : new Set(visibleIds));

  const bulk = async (target: string, verb: string) => {
    if (selected.size === 0) return;
    setBusy(target); setActionError(null);
    const count = selected.size;
    try {
      await apiPost("/api/admissions/applicants/bulk-status", { applicantIds: [...selected], status: target });
      setSelected(new Set());
      announce(`${count} applicant${count === 1 ? "" : "s"} moved to ${STATUS_LABEL[target] ?? target}.`);
      await load();
    } catch (e) {
      setActionError(e instanceof Error ? `${verb} did not complete: ${e.message}` : `${verb} did not complete.`);
    } finally {
      setBusy(null);
    }
  };

  const filtered = Boolean(intakeId || status);
  const count = rows?.length ?? 0;
  const intakeName = intakes.find(i => i.id === intakeId)?.name;

  return (
    <div className="page">
      {region}
      <RegisterHead
        title="Admissions"
        crumbs={[{ label: "Admissions office" }, { label: "Applicants" }]}
        ident={error ? "Not loaded" : rows ? <><strong>{count} applicant{count === 1 ? "" : "s"}</strong>{intakeName ? <><br />{intakeName}</> : null}</> : "Counting…"}
        meta={
          <div className="toolbar" role="group" aria-label="Filter applicants">
            <label>Intake
              <select value={intakeId} onChange={e => setIntakeId(e.target.value)}>
                <option value="">All intakes</option>
                {intakes.map(i => <option key={i.id} value={i.id}>{i.name}</option>)}
              </select>
            </label>
            <label>Status
              <select value={status} onChange={e => setStatus(e.target.value)}>
                <option value="">All statuses</option>
                {STATUSES.map(s => <option key={s} value={s}>{STATUS_LABEL[s]}</option>)}
              </select>
            </label>
            {filtered && <button type="button" className="btn btn-quiet btn-sm" onClick={() => { setIntakeId(""); setStatus(""); }}>Clear filters</button>}
          </div>
        }
        actions={<>
          <Link className="btn btn-secondary" href="/admin/admissions/pipeline">Pipeline board</Link>
          <Link className="btn btn-secondary" href="/admin/admissions/test-config">Entry test</Link>
          <Link className="btn btn-secondary" href="/admin/admissions/document-config">Intakes and documents</Link>
        </>}
      />

      {actionError && <ErrorNotice message={actionError} hint="Nothing was changed. Try the action again." />}

      {selected.size > 0 && (
        <div className="toolbar-selection" role="region" aria-label="Bulk actions">
          <strong>{selected.size} selected</strong>
          <button type="button" className={`btn btn-secondary btn-sm${busy === "UnderReview" ? " btn-loading" : ""}`} disabled={busy !== null} onClick={() => bulk("UnderReview", "Shortlisting")}>Shortlist for review</button>
          <button type="button" className={`btn btn-secondary btn-sm${busy === "TestScheduled" ? " btn-loading" : ""}`} disabled={busy !== null} onClick={() => bulk("TestScheduled", "Scheduling")}>Schedule entry test</button>
          <button type="button" className={`btn btn-danger btn-sm${busy === "Rejected" ? " btn-loading" : ""}`} disabled={busy !== null} onClick={() => bulk("Rejected", "Rejection")}>Reject applications</button>
          <button type="button" className="btn btn-quiet btn-sm" disabled={busy !== null} onClick={() => setSelected(new Set())}>Clear selection</button>
        </div>
      )}

      {rows === null ? (
        <RuledLoading label="Loading applicants" rows={8} head={false} />
      ) : error ? (
        <ErrorNotice message={error} hint="Check that the university API is reachable, then try again." onRetry={() => setRetry(n => n + 1)} />
      ) : rows.length === 0 ? (
        <div className="panel">
          <Empty title={filtered ? "No applicants match these filters." : "No applications in the register yet."} action={filtered ? <button type="button" className="btn btn-secondary" onClick={() => { setIntakeId(""); setStatus(""); }}>Clear filters</button> : undefined}>
            {filtered ? "Widen the intake or status filter to see more of the register." : "Applications appear here as soon as candidates submit them."}
          </Empty>
        </div>
      ) : (
        <div className="ledger-wrap">
          <table className="ledger" aria-label="Applicants">
            <thead>
              <tr>
                <th scope="col" className="check"><input ref={headerCheck} type="checkbox" checked={allSelected} onChange={toggleAll} aria-label="Select all applicants on this page" /></th>
                <th scope="col">Application</th>
                <th scope="col">Applicant</th>
                <th scope="col">Email</th>
                <th scope="col">Status</th>
                <th scope="col">Submitted</th>
              </tr>
            </thead>
            <tbody>
              {rows.map(r => {
                const key = statusKey(r.status);
                return (
                  <tr key={r.id} data-status={STATUS_TONE[key] ?? "ink"} aria-selected={selected.has(r.id)}>
                    <td className="check"><input type="checkbox" checked={selected.has(r.id)} onChange={() => toggle(r.id)} aria-label={`Select ${r.fullName ?? r.applicantId}`} /></td>
                    <td className="mono"><Link href={`/admin/admissions/${r.id}`}>{r.applicantId}</Link></td>
                    <td>{r.fullName ?? <span className="text-muted">Name not yet provided</span>}</td>
                    <td className="text-muted">{r.email}</td>
                    <td><Mark tone={STATUS_TONE[key] ?? "ink"}>{STATUS_LABEL[key] ?? key}</Mark></td>
                    <td className="mono">{r.submittedAt ? date(new Date(r.submittedAt)) : <span className="text-muted">Not submitted</span>}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          <div className="ledger-foot">
            <span>{count} row{count === 1 ? "" : "s"}{filtered ? " matching filters" : ""}</span>
            <span>Select rows to shortlist, schedule the entry test, or reject in one step.</span>
          </div>
        </div>
      )}

      {rows && rows.length > 0 && rows.length >= 200 && (
        <Notice tone="ink">Showing the first {rows.length} applicants. Narrow by intake or status to see the rest.</Notice>
      )}
    </div>
  );
}
