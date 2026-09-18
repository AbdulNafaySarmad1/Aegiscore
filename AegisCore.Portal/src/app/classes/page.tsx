"use client";

import { useState, type FormEvent } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel, Figures, Mark } from "@/components/register";
import { Ledger, toneOf, words } from "@/components/register/ledger";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Offering = { id: string; courseCode: string; courseTitle: string; academicYear: string; semester: string; sectionNumber: number; capacity: number; enrolledCount: number; status: string };
type Section = { id: string; courseCode: string; courseTitle: string; section: string; academicYear: string; semester: string; faculty: string; capacity: number; enrolledCount: number; status: string };
type Sync = { id: string; courseCode: string; section: string; provider: string; status: string; externalUrl: string | null; lastSyncAt: string | null; retryCount: number };
type Data = { offerings: Offering[]; sections: Section[]; syncs: Sync[] };

/** Class generation: what is offered, the sections cut from it, and how they reached the LMS. */
export default function ClassesPage() {
  const { date } = useTenantFormatter();
  const { state, reload } = useModuleData<Data>(async signal => {
    const [offerings, sections, syncs] = await Promise.all([
      apiGet<Offering[]>("/api/classes/offerings", signal),
      apiGet<Section[]>("/api/classes/sections", signal),
      apiGet<Sync[]>("/api/classes/sync-logs", signal),
    ]);
    return { offerings, sections, syncs };
  });
  const action = useAction();
  const [year, setYear] = useState("");
  const [semester, setSemester] = useState("");

  async function generate(e: FormEvent) {
    e.preventDefault();
    await action.run("generate", async () => {
      const job = await apiPost<{ sectionsGenerated?: number; studentsDistributed?: number }>(`/api/classes/generate?academicYear=${encodeURIComponent(year.trim())}&semester=${encodeURIComponent(semester.trim())}`);
      return `Generated ${job?.sectionsGenerated ?? 0} section(s), ${job?.studentsDistributed ?? 0} student(s) distributed.`;
    }, reload);
  }

  return (
    <ModulePage
      title="Classes"
      crumbs={[{ label: "Academic" }, { label: "Classes" }]}
      meta={<span>Course offerings, the sections generated from them, and their LMS provisioning.</span>}
      ident={(d: Data) => <><strong>{d.sections.length}</strong><br />sections</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {({ offerings, sections, syncs }) => (
        <div className="record">
          <div className="record-body">
            <Figures aria-label="Class figures" items={[
              { label: "Offerings", value: offerings.length, note: offerings.length === 0 ? "none yet" : `${offerings.reduce((n, o) => n + o.capacity, 0)} seats` },
              { label: "Sections", value: sections.length, note: `${sections.filter(s => s.faculty).length} with faculty assigned` },
              { label: "Students placed", value: sections.reduce((n, s) => n + s.enrolledCount, 0), note: "across all sections" },
              { label: "LMS provisioned", value: syncs.filter(s => /provisioned|synced|active/i.test(s.status)).length, note: `${syncs.length} attempted` },
            ]} />

            <Panel title="Offerings" flush>
              <Ledger rows={offerings} keyOf={o => o.id} minWidth={700} status={o => toneOf(o.status)}
                empty={{ title: "Nothing offered", detail: "An offering is a course made available for a semester, with its capacity." }}
                columns={[
                  { key: "course", label: "Course", render: o => <><strong>{o.courseCode}</strong> {o.courseTitle}</> },
                  { key: "term", label: "Term", render: o => `${o.semester} ${o.academicYear}` },
                  { key: "section", label: "Section", render: o => o.sectionNumber, num: true },
                  { key: "capacity", label: "Capacity", render: o => o.capacity, num: true },
                  { key: "enrolled", label: "Enrolled", render: o => o.enrolledCount, num: true },
                  { key: "status", label: "Status", render: o => <Mark tone={toneOf(o.status)}>{words(o.status)}</Mark> },
                ]} />
            </Panel>

            <Panel title="Sections" flush>
              <Ledger rows={sections} keyOf={s => s.id} minWidth={740} status={s => toneOf(s.status)}
                empty={{ title: "No sections generated", detail: "Generation cuts sections from the offerings and distributes the enrolled students across them." }}
                columns={[
                  { key: "course", label: "Course", render: s => <><strong>{s.courseCode}</strong> {s.courseTitle}</> },
                  { key: "section", label: "Section", render: s => s.section },
                  { key: "term", label: "Term", render: s => `${s.semester} ${s.academicYear}` },
                  { key: "faculty", label: "Faculty", render: s => s.faculty || "unassigned" },
                  { key: "enrolled", label: "Enrolled", render: s => `${s.enrolledCount}/${s.capacity}`, num: true },
                  { key: "status", label: "Status", render: s => <Mark tone={toneOf(s.status)}>{words(s.status)}</Mark> },
                ]} />
            </Panel>

            <Panel title="LMS provisioning" aside={<span className="text-muted">{syncs.length} attempts</span>} flush>
              <Ledger rows={syncs} keyOf={s => s.id} minWidth={680} status={s => toneOf(s.status)}
                empty={{ title: "No LMS provisioning attempted", detail: "A section can be provisioned into the university's LMS; each attempt is recorded here." }}
                columns={[
                  { key: "course", label: "Course", render: s => `${s.courseCode} §${s.section}` },
                  { key: "provider", label: "Provider", render: s => words(s.provider) },
                  { key: "status", label: "Status", render: s => <Mark tone={toneOf(s.status)}>{words(s.status)}</Mark> },
                  { key: "synced", label: "Last synced", render: s => s.lastSyncAt ? date(s.lastSyncAt) : "never", nowrap: true },
                  { key: "retries", label: "Retries", render: s => s.retryCount, num: true },
                ]} />
            </Panel>
          </div>

          <aside className="record-margin">
            <Panel title="Generate sections">
              <form className="panel-form" onSubmit={generate}>
                <label><span>Academic year</span><input value={year} onChange={e => setYear(e.target.value)} placeholder="e.g. 2026-27" required /></label>
                <label><span>Semester</span><input value={semester} onChange={e => setSemester(e.target.value)} placeholder="e.g. Fall" required /></label>
                <p className="field-hint">Cuts sections from the term&apos;s offerings and distributes enrolled students across them.</p>
                <button type="submit" className={`btn btn-primary${action.busy === "generate" ? " btn-loading" : ""}`} disabled={action.busy !== null}>Generate</button>
              </form>
            </Panel>
          </aside>
        </div>
      )}
    </ModulePage>
  );
}
