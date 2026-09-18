"use client";

import { useState, type FormEvent } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel, Figures, Mark } from "@/components/register";
import { Ledger, toneOf, words } from "@/components/register/ledger";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Freeze = {
  id: string; studentId: string; studentName: string; type: string; reasonCategory: string; reasonDescription: string | null;
  status: string; currentSemester: string; totalSemesters: number; refundAmount: number; outstandingBalance: number;
  freezesUsed: number; maxFreezeSemesters: number; requestDate: string;
};

/** Semester freezes: who has asked to pause, on what ground, and what the decision was. */
export default function FreezesPage() {
  const { date, money } = useTenantFormatter();
  const { state, reload } = useModuleData<Freeze[]>(signal => apiGet<Freeze[]>("/api/freezes", signal));
  const action = useAction();
  const [selected, setSelected] = useState("");
  const [reason, setReason] = useState("");

  async function decide(approved: boolean, e: FormEvent) {
    e.preventDefault();
    await action.run(approved ? "approve" : "reject", async () => {
      await apiPost(`/api/freezes/${selected}/approve?approved=${approved}${approved ? "" : `&rejectionReason=${encodeURIComponent(reason.trim())}`}`);
      return approved ? "Freeze approved." : "Freeze rejected with the reason recorded.";
    }, reload);
  }

  return (
    <ModulePage
      title="Semester freezes"
      crumbs={[{ label: "Registrar" }, { label: "Semester freezes" }]}
      meta={<span>Requests to pause enrolment, their grounds and the decision recorded against each.</span>}
      ident={(d: Freeze[]) => <><strong>{d.filter(f => /pending|requested/i.test(f.status)).length}</strong><br />awaiting decision</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {freezes => (
        <div className="record">
          <div className="record-body">
            <Figures aria-label="Freeze figures" items={[
              { label: "Requests", value: freezes.length, note: freezes.length === 0 ? "none recorded" : `latest ${date(freezes[0].requestDate)}` },
              { label: "Awaiting decision", value: freezes.filter(f => /pending|requested/i.test(f.status)).length, tone: "brass", note: "with the registrar" },
              { label: "Approved", value: freezes.filter(f => /approved/i.test(f.status)).length, tone: "laurel", note: "currently paused" },
              { label: "Refunds due", value: money(freezes.reduce((s, f) => s + Number(f.refundAmount), 0)), note: "across approved freezes" },
            ]} />

            <Panel title="Requests" flush>
              <Ledger rows={freezes} keyOf={f => f.id} minWidth={900} status={f => toneOf(f.status)}
                empty={{ title: "No freeze requested", detail: "A student applies to freeze a semester; the request records the ground, the timing rule and the balance." }}
                columns={[
                  { key: "student", label: "Student", render: f => f.studentName },
                  { key: "id", label: "Student number", render: f => f.studentId, mono: true },
                  { key: "type", label: "Type", render: f => words(f.type) },
                  { key: "ground", label: "Ground", render: f => <>{words(f.reasonCategory)}{f.reasonDescription ? <><br /><span className="text-muted">{f.reasonDescription}</span></> : null}</> },
                  { key: "semester", label: "Semester", render: f => `${f.currentSemester} of ${f.totalSemesters}` },
                  { key: "used", label: "Freezes used", render: f => `${f.freezesUsed}/${f.maxFreezeSemesters}`, num: true },
                  { key: "refund", label: "Refund", render: f => money(Number(f.refundAmount)), num: true },
                  { key: "balance", label: "Outstanding", render: f => money(Number(f.outstandingBalance)), num: true },
                  { key: "status", label: "Status", render: f => <Mark tone={toneOf(f.status)}>{words(f.status)}</Mark> },
                  { key: "when", label: "Requested", render: f => date(f.requestDate), nowrap: true },
                ]} />
            </Panel>
          </div>

          <aside className="record-margin">
            <Panel title="Decide a request">
              <form className="panel-form" onSubmit={e => decide(true, e)}>
                <label><span>Request</span>
                  <select value={selected} onChange={e => setSelected(e.target.value)} required>
                    <option value="">Choose a request</option>
                    {freezes.filter(f => /pending|requested/i.test(f.status)).map(f => <option key={f.id} value={f.id}>{f.studentName} — {words(f.reasonCategory)}</option>)}
                  </select>
                </label>
                <button type="submit" className={`btn btn-primary${action.busy === "approve" ? " btn-loading" : ""}`} disabled={action.busy !== null || !selected}>Approve</button>
              </form>
              <form className="panel-form" onSubmit={e => decide(false, e)}>
                <label><span>Reason for rejection</span><input value={reason} onChange={e => setReason(e.target.value)} placeholder="recorded against the request" /></label>
                <button type="submit" className={`btn btn-secondary${action.busy === "reject" ? " btn-loading" : ""}`} disabled={action.busy !== null || !selected || !reason.trim()}>Reject</button>
              </form>
            </Panel>
          </aside>
        </div>
      )}
    </ModulePage>
  );
}
