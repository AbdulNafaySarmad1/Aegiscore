"use client";

import { useState, type FormEvent } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel, Figures, Mark } from "@/components/register";
import { Ledger, toneOf, words } from "@/components/register/ledger";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Ceremony = { id: string; title: string; academicYear: string; ceremonyDate: string; venue: string; guestTicketsPerStudent: number; status: string; eligible: number; attendees: number; certificates: number };
type Eligibility = { id: string; studentId: string; studentName: string; convocationId: string; status: string; blockingReason: string | null; creditsCompleted: boolean; cgpaMet: boolean; duesCleared: boolean; thesisApproved: boolean; libraryCleared: boolean; disciplinaryCleared: boolean };
type Certificate = { id: string; serialNumber: string; studentName: string; studentId: string; degreeTitle: string; status: string; attestationStatus: string; hecReference: string | null };
type Attendee = { id: string; studentName: string; studentId: string; status: string; checkInTime: string | null; guestCount: number; certificateCollected: boolean };
type Data = { ceremonies: Ceremony[]; eligibility: Eligibility[]; certificates: Certificate[]; attendees: Attendee[] };

/** Convocations: the ceremony, who may walk, the certificates printed and who arrived. */
export default function ConvocationsPage() {
  const { date } = useTenantFormatter();
  const { state, reload } = useModuleData<Data>(async signal => {
    const [ceremonies, eligibility, certificates, attendees] = await Promise.all([
      apiGet<Ceremony[]>("/api/convocations", signal),
      apiGet<Eligibility[]>("/api/convocations/eligibility", signal),
      apiGet<Certificate[]>("/api/convocations/certificates", signal),
      apiGet<Attendee[]>("/api/convocations/attendees", signal),
    ]);
    return { ceremonies, eligibility, certificates, attendees };
  });
  const action = useAction();
  const [selected, setSelected] = useState("");

  async function check(e: FormEvent) {
    e.preventDefault();
    await action.run("check", async () => {
      const result = await apiPost<{ total?: number }>(`/api/convocations/${selected}/check-eligibility`);
      return `Eligibility checked${result?.total !== undefined ? ` for ${result.total} students` : ""}.`;
    }, reload);
  }
  async function generate(e: FormEvent) {
    e.preventDefault();
    await action.run("generate", async () => {
      const result = await apiPost<{ total?: number }>(`/api/convocations/${selected}/generate-certificates`);
      return `Certificates generated${result?.total !== undefined ? `: ${result.total}` : ""}.`;
    }, reload);
  }

  return (
    <ModulePage
      title="Convocation"
      crumbs={[{ label: "Registrar" }, { label: "Convocation" }]}
      meta={<span>Ceremonies, eligibility, degree certificates and attendance.</span>}
      ident={(d: Data) => <><strong>{d.ceremonies.length}</strong><br />ceremonies</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {({ ceremonies, eligibility, certificates, attendees }) => (
        <>
          <Figures aria-label="Convocation figures" items={[
            { label: "Ceremonies", value: ceremonies.length, note: ceremonies.find(c => c.status !== "Completed") ? `next ${date(ceremonies.filter(c => c.status !== "Completed").sort((a, b) => a.ceremonyDate.localeCompare(b.ceremonyDate))[0].ceremonyDate)}` : "none scheduled" },
            { label: "Eligible to walk", value: eligibility.filter(e => e.status === "Eligible").length, note: `${eligibility.length} assessed` },
            { label: "Certificates", value: certificates.length, note: `${certificates.filter(c => c.status === "Dispatched").length} dispatched` },
            { label: "Checked in", value: attendees.filter(a => a.status === "CheckedIn").length, note: `${attendees.length} registered` },
          ]} />

          <div className="record">
            <div className="record-body">
              <Panel title="Ceremonies" flush>
                <Ledger rows={ceremonies} keyOf={c => c.id} minWidth={680} status={c => toneOf(c.status)}
                  empty={{ title: "No ceremony recorded", detail: "The registrar creates a convocation with its date, venue and guest allowance." }}
                  columns={[
                    { key: "title", label: "Ceremony", render: c => <strong>{c.title}</strong> },
                    { key: "year", label: "Academic year", render: c => c.academicYear },
                    { key: "date", label: "Date", render: c => date(c.ceremonyDate), nowrap: true },
                    { key: "venue", label: "Venue", render: c => c.venue || "—" },
                    { key: "tickets", label: "Guest tickets", render: c => c.guestTicketsPerStudent, num: true },
                    { key: "status", label: "Status", render: c => <Mark tone={toneOf(c.status)}>{words(c.status)}</Mark> },
                  ]} />
              </Panel>

              <Panel title="Eligibility" aside={<span className="text-muted">{eligibility.length} assessed</span>} flush>
                <Ledger rows={eligibility} keyOf={e => e.id} minWidth={760} status={e => toneOf(e.status)}
                  empty={{ title: "No eligibility assessed", detail: "Run the check for a ceremony to test every graduating student against credits, CGPA, dues, thesis, library and discipline." }}
                  columns={[
                    { key: "student", label: "Student", render: e => e.studentName },
                    { key: "id", label: "Student number", render: e => e.studentId, mono: true },
                    { key: "status", label: "Standing", render: e => <Mark tone={toneOf(e.status)}>{words(e.status)}</Mark> },
                    { key: "blockers", label: "Outstanding", render: e => e.blockingReason ?? ([!e.creditsCompleted && "credits", !e.cgpaMet && "CGPA", !e.duesCleared && "dues", !e.thesisApproved && "thesis", !e.libraryCleared && "library", !e.disciplinaryCleared && "discipline"].filter(Boolean).join(", ") || "nothing") },
                  ]} />
              </Panel>

              <Panel title="Degree certificates" aside={<span className="text-muted">{certificates.length} issued</span>} flush>
                <Ledger rows={certificates} keyOf={c => c.id} minWidth={720} status={c => toneOf(c.status)}
                  empty={{ title: "No certificates generated", detail: "Certificates are generated for a ceremony once eligibility is settled." }}
                  columns={[
                    { key: "serial", label: "Serial", render: c => c.serialNumber, mono: true },
                    { key: "student", label: "Student", render: c => c.studentName },
                    { key: "degree", label: "Degree", render: c => c.degreeTitle || "—" },
                    { key: "status", label: "Status", render: c => <Mark tone={toneOf(c.status)}>{words(c.status)}</Mark> },
                    { key: "attestation", label: "Attestation", render: c => words(c.attestationStatus) },
                    { key: "hec", label: "HEC reference", render: c => c.hecReference ?? "—", mono: true },
                  ]} />
              </Panel>

              <Panel title="Attendance" aside={<span className="text-muted">{attendees.length} registered</span>} flush>
                <Ledger rows={attendees} keyOf={a => a.id} minWidth={640} status={a => toneOf(a.status)}
                  empty={{ title: "Nobody registered yet", detail: "Students register for the ceremony and are checked in on the day." }}
                  columns={[
                    { key: "student", label: "Student", render: a => a.studentName },
                    { key: "id", label: "Student number", render: a => a.studentId, mono: true },
                    { key: "status", label: "Status", render: a => <Mark tone={toneOf(a.status)}>{words(a.status)}</Mark> },
                    { key: "checkin", label: "Checked in", render: a => a.checkInTime ? date(a.checkInTime) : "—", nowrap: true },
                    { key: "guests", label: "Guests", render: a => a.guestCount, num: true },
                    { key: "collected", label: "Certificate", render: a => a.certificateCollected ? "collected" : "not collected" },
                  ]} />
              </Panel>
            </div>

            <aside className="record-margin">
              <Panel title="Run a ceremony">
                <form className="panel-form" onSubmit={check}>
                  <label><span>Ceremony</span>
                    <select value={selected} onChange={e => setSelected(e.target.value)} required>
                      <option value="">Choose a ceremony</option>
                      {ceremonies.map(c => <option key={c.id} value={c.id}>{c.title}</option>)}
                    </select>
                  </label>
                  <p className="field-hint">Checks every graduating student against credits, CGPA, dues, thesis, library and discipline.</p>
                  <button type="submit" className={`btn btn-primary${action.busy === "check" ? " btn-loading" : ""}`} disabled={action.busy !== null || !selected}>Check eligibility</button>
                </form>
                <form className="panel-form" onSubmit={generate}>
                  <p className="field-hint">Generates a numbered certificate for every eligible student in the chosen ceremony.</p>
                  <button type="submit" className={`btn btn-secondary${action.busy === "generate" ? " btn-loading" : ""}`} disabled={action.busy !== null || !selected}>Generate certificates</button>
                </form>
              </Panel>
            </aside>
          </div>
        </>
      )}
    </ModulePage>
  );
}
