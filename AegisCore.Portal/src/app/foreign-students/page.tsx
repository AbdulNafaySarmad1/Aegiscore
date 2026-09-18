"use client";

import { useState, type FormEvent } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel, Figures, Mark } from "@/components/register";
import { Ledger, toneOf, words } from "@/components/register/ledger";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Registry = { id: string; studentId: string; studentName: string; nationality: string; program: string; yearOfStudy: number; visaType: string; visaExpiry: string | null; passportExpiry: string | null; moraStatus: string; compliance: string; expectedGraduation: string | null };
type Renewal = { id: string; studentName: string; nationality: string; status: string; applicationDeadline: string | null; submittedAt: string | null; approvedAt: string | null; rejectionReason: string | null };
type Breach = { id: string; breachType: string; status: string; occurredAt: string; resolutionType: string | null; studentId: string | null };
type Data = { registry: Registry[]; renewals: Renewal[]; breaches: Breach[] };

const daysUntil = (value: string | null) => value === null ? null : Math.ceil((new Date(value).getTime() - Date.now()) / 86_400_000);

/** Foreign students: the visa register, renewals in flight and compliance breaches. */
export default function ForeignStudentsPage() {
  const { date } = useTenantFormatter();
  const { state, reload } = useModuleData<Data>(async signal => {
    const [registry, renewals, breaches] = await Promise.all([
      apiGet<Registry[]>("/api/foreign-students", signal),
      apiGet<Renewal[]>("/api/foreign-students/visa-renewals", signal),
      apiGet<Breach[]>("/api/foreign-students/compliance-breaches", signal),
    ]);
    return { registry, renewals, breaches };
  });
  const action = useAction();
  const [semester, setSemester] = useState("");

  async function submitMora(e: FormEvent) {
    e.preventDefault();
    await action.run("mora", async () => {
      await apiPost(`/api/foreign-students/mora/submit?semester=${encodeURIComponent(semester.trim())}`);
      return `MORA submission recorded for ${semester.trim()}.`;
    }, reload);
  }

  return (
    <ModulePage
      title="Foreign students"
      crumbs={[{ label: "Student welfare" }, { label: "Foreign students" }]}
      meta={<span>Visa standing, renewals and compliance, as recorded on the register.</span>}
      ident={(d: Data) => <><strong>{d.registry.length}</strong><br />on the register</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {({ registry, renewals, breaches }) => (
        <div className="record">
          <div className="record-body">
            <Figures aria-label="Foreign student figures" items={[
              { label: "On the register", value: registry.length, note: `${new Set(registry.map(r => r.nationality)).size} nationalities` },
              { label: "Visas expiring", value: registry.filter(r => { const d = daysUntil(r.visaExpiry); return d !== null && d <= 90; }).length, tone: "brass", note: "within 90 days" },
              { label: "Renewals in flight", value: renewals.filter(r => !r.approvedAt).length, note: `${renewals.length} started` },
              { label: "Open breaches", value: breaches.filter(b => b.status !== "resolved").length, tone: breaches.some(b => b.status !== "resolved") ? "oxblood" : "ink", note: `${breaches.length} recorded` },
            ]} />

            <Panel title="Visa register" flush>
              <Ledger rows={registry} keyOf={r => r.id} minWidth={900} status={r => toneOf(r.compliance)}
                empty={{ title: "No foreign student registered", detail: "Each international student's passport, visa and MORA standing is recorded here." }}
                columns={[
                  { key: "student", label: "Student", render: r => r.studentName },
                  { key: "nationality", label: "Nationality", render: r => r.nationality },
                  { key: "program", label: "Programme", render: r => `${r.program} (year ${r.yearOfStudy})` },
                  { key: "visa", label: "Visa", render: r => words(r.visaType) },
                  { key: "visaExpiry", label: "Visa expires", render: r => { const d = daysUntil(r.visaExpiry); return r.visaExpiry ? <>{date(r.visaExpiry)}{d !== null && d <= 90 ? <><br /><span className="text-muted">{d < 0 ? "expired" : `in ${d} days`}</span></> : null}</> : "—"; }, nowrap: true },
                  { key: "passport", label: "Passport expires", render: r => r.passportExpiry ? date(r.passportExpiry) : "—", nowrap: true },
                  { key: "mora", label: "MORA", render: r => words(r.moraStatus) },
                  { key: "compliance", label: "Compliance", render: r => <Mark tone={toneOf(r.compliance)}>{words(r.compliance)}</Mark> },
                ]} />
            </Panel>

            <Panel title="Visa renewals" aside={<span className="text-muted">{renewals.length} started</span>} flush>
              <Ledger rows={renewals} keyOf={r => r.id} minWidth={720} status={r => toneOf(r.status)}
                empty={{ title: "No renewal in flight", detail: "A renewal is opened for a student whose visa is approaching expiry." }}
                columns={[
                  { key: "student", label: "Student", render: r => r.studentName },
                  { key: "nationality", label: "Nationality", render: r => r.nationality },
                  { key: "status", label: "Status", render: r => <Mark tone={toneOf(r.status)}>{words(r.status)}</Mark> },
                  { key: "deadline", label: "Deadline", render: r => r.applicationDeadline ? date(r.applicationDeadline) : "—", nowrap: true },
                  { key: "submitted", label: "Submitted", render: r => r.submittedAt ? date(r.submittedAt) : "—", nowrap: true },
                  { key: "approved", label: "Approved", render: r => r.approvedAt ? date(r.approvedAt) : r.rejectionReason ?? "—", nowrap: true },
                ]} />
            </Panel>

            <Panel title="Compliance breaches" aside={<span className="text-muted">{breaches.filter(b => b.status !== "resolved").length} open</span>} flush>
              <Ledger rows={breaches} keyOf={b => b.id} minWidth={560} status={b => b.status === "resolved" ? "laurel" : "oxblood"}
                empty={{ title: "No breach recorded", detail: "A breach is recorded when a visa condition — attendance, employment, registration — is not met." }}
                columns={[
                  { key: "type", label: "Breach", render: b => words(b.breachType) },
                  { key: "when", label: "Occurred", render: b => date(b.occurredAt), nowrap: true },
                  { key: "status", label: "Status", render: b => <Mark tone={b.status === "resolved" ? "laurel" : "oxblood"}>{words(b.status)}</Mark> },
                  { key: "resolution", label: "Resolution", render: b => b.resolutionType ?? "—" },
                ]} />
            </Panel>
          </div>

          <aside className="record-margin">
            <Panel title="MORA submission">
              <form className="panel-form" onSubmit={submitMora}>
                <label><span>Semester</span><input value={semester} onChange={e => setSemester(e.target.value)} placeholder="e.g. Fall 2026" required /></label>
                <p className="field-hint">Records the semester&apos;s submission to the Ministry of Religious Affairs for every registered foreign student.</p>
                <button type="submit" className={`btn btn-primary${action.busy === "mora" ? " btn-loading" : ""}`} disabled={action.busy !== null}>Submit</button>
              </form>
            </Panel>
          </aside>
        </div>
      )}
    </ModulePage>
  );
}
