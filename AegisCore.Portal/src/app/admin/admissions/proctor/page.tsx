"use client";

import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel, Notice } from "@/components/register";
import { Ledger } from "@/components/register/ledger";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Row = {
  applicantTestId: string; applicantId: string; applicantName: string;
  startedAt: string | null; expiresAt: string; status: string; proctorNote: string | null;
};

/**
 * What an invigilator sees while tests are running: who is sitting, when their time runs out, and
 * the two things an invigilator can do about it. Questions and answers are deliberately absent.
 */
export default function ProctorPage() {
  const { time } = useTenantFormatter();
  const { state, reload } = useModuleData<Row[]>(signal => apiGet<Row[]>("/api/admissions/proctor", signal));
  const action = useAction();
  const [flagging, setFlagging] = useState<Row | null>(null);
  const [note, setNote] = useState("");
  const [ending, setEnding] = useState<Row | null>(null);

  // The list is what is happening now, so it refreshes itself.
  useEffect(() => {
    const id = window.setInterval(reload, 10000);
    return () => window.clearInterval(id);
  }, [reload]);

  async function flag() {
    const row = flagging!;
    setFlagging(null);
    await action.run("flag", async () => {
      await apiPost(`/api/admissions/proctor/${row.applicantTestId}/flag`, { note: note.trim() });
      setNote("");
      return `${row.applicantName} has been flagged: “${note.trim()}”.`;
    }, reload);
  }

  async function forceSubmit() {
    const row = ending!;
    setEnding(null);
    await action.run("end", async () => {
      await apiPost(`/api/admissions/proctor/${row.applicantTestId}/force-submit`);
      return `${row.applicantName}'s test was handed in as it stood.`;
    }, reload);
  }

  return (
    <ModulePage
      title="Invigilation"
      crumbs={[{ label: "Admissions", href: "/admin/admissions" }, { label: "Invigilation" }]}
      meta={<span>Tests being sat right now. The list refreshes itself every ten seconds.</span>}
      ident={(rows: Row[]) => <><strong>{rows.length}</strong><br />{rows.length === 1 ? "candidate" : "candidates"}</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {rows => (
        <>
          {flagging && (
            <Panel tone="brass" title={`Flag ${flagging.applicantName}?`}>
              <div className="panel-form">
                <p>A flag is recorded against this candidate&apos;s test for the admissions office to read afterwards. It does not end the test.</p>
                <label><span>What did you see?</span><input value={note} onChange={e => setNote(e.target.value)} autoFocus placeholder="e.g. left the desk twice" /></label>
                <div className="btn-row">
                  <button type="button" className="btn btn-primary" disabled={!note.trim()} onClick={flag}>Record the flag</button>
                  <button type="button" className="btn btn-ghost" onClick={() => { setFlagging(null); setNote(""); }}>Cancel</button>
                </div>
              </div>
            </Panel>
          )}

          {ending && (
            <Panel tone="oxblood" title={`End ${ending.applicantName}'s test now?`}>
              <div className="panel-form">
                <p>The test is handed in exactly as it stands and marked. The candidate cannot go back to it.</p>
                <div className="btn-row">
                  <button type="button" className="btn btn-primary" onClick={forceSubmit}>Hand it in</button>
                  <button type="button" className="btn btn-ghost" onClick={() => setEnding(null)}>Let them carry on</button>
                </div>
              </div>
            </Panel>
          )}

          <Panel title="Sitting now" flush>
            <Ledger rows={rows} keyOf={r => r.applicantTestId} minWidth={840} status={r => r.proctorNote ? "brass" : undefined}
              empty={{ title: "No test is being sat", detail: "Candidates appear here from the moment they begin until they hand in." }}
              columns={[
                { key: "id", label: "Applicant", render: r => r.applicantId, mono: true },
                { key: "name", label: "Name", render: r => r.applicantName },
                { key: "started", label: "Began", render: r => r.startedAt ? time(r.startedAt) : "not yet", nowrap: true },
                { key: "deadline", label: "Time runs out", render: r => time(r.expiresAt), nowrap: true },
                { key: "flag", label: "Flagged", render: r => r.proctorNote ?? "—" },
                {
                  key: "act", label: "", nowrap: true, render: r => (
                    <span className="btn-row">
                      <button type="button" className="btn btn-ghost btn-sm" disabled={action.busy !== null} onClick={() => { setFlagging(r); setNote(r.proctorNote ?? ""); }}>Flag</button>
                      <button type="button" className="btn btn-danger btn-sm" disabled={action.busy !== null} onClick={() => setEnding(r)}>End test</button>
                    </span>
                  ),
                },
              ]} />
          </Panel>

          <Notice tone="ink">
            Questions and answers are never shown on this screen. An invigilator can see that someone
            is sitting a test and how long is left, not what they are answering.
          </Notice>
        </>
      )}
    </ModulePage>
  );
}
