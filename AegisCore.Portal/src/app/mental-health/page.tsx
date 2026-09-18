"use client";

import { useState, type FormEvent } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel, Figures, Mark, Notice } from "@/components/register";
import { Ledger, words } from "@/components/register/ledger";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Ticket = { id: string; reference: string; severity: string; concernType: string; preferredLanguage: string | null; status: string; assignedCounselorId: string | null; firstResponseAt: string | null; crisisProtocolsTriggered: boolean; createdAt: string };
type Counselor = { id: string; fullName: string; isOnCall: boolean; currentCaseload: number; maxCaseload: number; languages: string; specializations: string | null };
type Data = { tickets: Ticket[]; counselors: Counselor[] };

const severityTone = (s: string) => /crisis/i.test(s) ? "oxblood" : /urgent/i.test(s) ? "brass" : "ink";

/** The counsellor queue: open cases by urgency, and who is free to take them. */
export default function MentalHealthPage() {
  const { date } = useTenantFormatter();
  const { state, reload } = useModuleData<Data>(signal => apiGet<Data>("/api/mental-health/counselor/queue", signal));
  const action = useAction();
  const [ticket, setTicket] = useState("");
  const [counselor, setCounselor] = useState("");

  async function assign(e: FormEvent) {
    e.preventDefault();
    await action.run("assign", async () => {
      await apiPost(`/api/mental-health/tickets/${ticket}/assign/${counselor}`);
      return "Case assigned.";
    }, reload);
  }

  return (
    <ModulePage
      title="Counsellor queue"
      crumbs={[{ label: "Student welfare" }, { label: "Mental health" }]}
      meta={<span>Cases are held under an anonymous reference; a student&apos;s name never appears here.</span>}
      ident={(d: Data) => <><strong>{d.tickets.length}</strong><br />open cases</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {({ tickets, counselors }) => (
        <div className="record">
          <div className="record-body">
            <Figures aria-label="Counselling figures" items={[
              { label: "Open cases", value: tickets.length, note: `${tickets.filter(t => !t.assignedCounselorId).length} unassigned` },
              { label: "Crisis", value: tickets.filter(t => /crisis/i.test(t.severity)).length, tone: tickets.some(t => /crisis/i.test(t.severity)) ? "oxblood" : "ink", note: "protocols triggered where flagged" },
              { label: "Awaiting first response", value: tickets.filter(t => !t.firstResponseAt).length, tone: "brass", note: "no counsellor has replied yet" },
              { label: "Counsellors on call", value: counselors.filter(c => c.isOnCall).length, note: `${counselors.length} active` },
            ]} />

            {tickets.some(t => /crisis/i.test(t.severity) && !t.assignedCounselorId) && (
              <Notice tone="oxblood" title="A crisis case is unassigned." role="alert">
                Assign it to an on-call counsellor now; the crisis protocol has already been recorded against it.
              </Notice>
            )}

            <Panel title="Queue" aside={<span className="text-muted">most urgent first</span>} flush>
              <Ledger rows={tickets} keyOf={t => t.id} minWidth={780} status={t => severityTone(t.severity)}
                empty={{ title: "The queue is empty", detail: "Cases raised by students appear here, ordered by urgency and then by age." }}
                columns={[
                  { key: "ref", label: "Reference", render: t => t.reference, mono: true },
                  { key: "severity", label: "Urgency", render: t => <Mark tone={severityTone(t.severity)}>{words(t.severity)}</Mark> },
                  { key: "concern", label: "Concern", render: t => words(t.concernType) },
                  { key: "language", label: "Language", render: t => t.preferredLanguage?.toUpperCase() ?? "—" },
                  { key: "assigned", label: "Counsellor", render: t => counselors.find(c => c.id === t.assignedCounselorId)?.fullName ?? "unassigned" },
                  { key: "response", label: "First response", render: t => t.firstResponseAt ? date(t.firstResponseAt) : "awaited", nowrap: true },
                  { key: "raised", label: "Raised", render: t => date(t.createdAt), nowrap: true },
                ]} />
            </Panel>

            <Panel title="Counsellors" aside={<span className="text-muted">{counselors.length} active</span>} flush>
              <Ledger rows={counselors} keyOf={c => c.id} minWidth={620} status={c => c.currentCaseload >= c.maxCaseload ? "oxblood" : c.isOnCall ? "laurel" : "ink"}
                empty={{ title: "No counsellor on the register", detail: "Counsellors are added with their languages, specialisations and caseload limit." }}
                columns={[
                  { key: "name", label: "Counsellor", render: c => c.fullName },
                  { key: "oncall", label: "On call", render: c => c.isOnCall ? <Mark tone="laurel">On call</Mark> : "—" },
                  { key: "caseload", label: "Caseload", render: c => `${c.currentCaseload}/${c.maxCaseload}`, num: true },
                  { key: "languages", label: "Languages", render: c => c.languages },
                  { key: "specialisations", label: "Specialisations", render: c => c.specializations ?? "—" },
                ]} />
            </Panel>
          </div>

          <aside className="record-margin">
            <Panel title="Assign a case">
              <form className="panel-form" onSubmit={assign}>
                <label><span>Case</span>
                  <select value={ticket} onChange={e => setTicket(e.target.value)} required>
                    <option value="">Choose a case</option>
                    {tickets.filter(t => !t.assignedCounselorId).map(t => <option key={t.id} value={t.id}>{t.reference} — {words(t.severity)}</option>)}
                  </select>
                </label>
                <label><span>Counsellor</span>
                  <select value={counselor} onChange={e => setCounselor(e.target.value)} required>
                    <option value="">Choose a counsellor</option>
                    {counselors.filter(c => c.currentCaseload < c.maxCaseload).map(c => <option key={c.id} value={c.id}>{c.fullName} ({c.currentCaseload}/{c.maxCaseload})</option>)}
                  </select>
                </label>
                <p className="field-hint">Only counsellors below their caseload limit are listed.</p>
                <button type="submit" className={`btn btn-primary${action.busy === "assign" ? " btn-loading" : ""}`} disabled={action.busy !== null || !ticket || !counselor}>Assign</button>
              </form>
            </Panel>
          </aside>
        </div>
      )}
    </ModulePage>
  );
}
