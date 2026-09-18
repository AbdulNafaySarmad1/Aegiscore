"use client";

import { useState } from "react";
import { apiGet } from "@/lib/api-client";
import { Panel, Figures, Mark } from "@/components/register";
import { Ledger, toneOf, words } from "@/components/register/ledger";
import { ModulePage, useModuleData } from "@/components/register/module-page";

type Job = { id: string; documentName: string; documentType: string | null; status: string; extractionConfidence: number; coursesExtracted: number; coursesInserted: number; coursesUpdated: number };
type Course = { id: string; jobId: string; degreeProgram: string; semesterSequence: number; semesterLabel: string | null; courseCode: string; courseTitle: string; creditHoursTotal: number; courseType: string | null; prerequisitesJson: string };
type Data = { jobs: Job[]; courses: Course[] };

const prerequisites = (json: string) => { try { const v = JSON.parse(json); return Array.isArray(v) ? v.join(", ") : ""; } catch { return ""; } };

/** Catalogue ingestion: the documents read, and the courses extracted from them awaiting review. */
export default function CatalogPage() {
  const [job, setJob] = useState("");
  const { state, reload } = useModuleData<Data>(async signal => {
    const [jobs, courses] = await Promise.all([
      apiGet<Job[]>("/api/catalog/ingest/jobs", signal),
      apiGet<Course[]>(`/api/catalog/courses${job ? `?jobId=${job}` : ""}`, signal),
    ]);
    return { jobs, courses };
  }, job);

  return (
    <ModulePage
      title="Catalogue ingestion"
      crumbs={[{ label: "Academic" }, { label: "Catalogue" }]}
      meta={<span>Courses extracted from an uploaded catalogue document, for review before they enter the register.</span>}
      ident={(d: Data) => <><strong>{d.courses.length}</strong><br />courses extracted</>}
      state={state} reload={reload}
    >
      {({ jobs, courses }) => (
        <div className="panel-stack">
          <Figures aria-label="Ingestion figures" items={[
            { label: "Documents read", value: jobs.length, note: jobs.length === 0 ? "none uploaded" : `${jobs.filter(j => /complete/i.test(j.status)).length} complete` },
            { label: "Courses extracted", value: jobs.reduce((n, j) => n + j.coursesExtracted, 0), note: "across all documents" },
            { label: "Inserted", value: jobs.reduce((n, j) => n + j.coursesInserted, 0), note: `${jobs.reduce((n, j) => n + j.coursesUpdated, 0)} updated` },
            { label: "Programmes", value: new Set(courses.map(c => c.degreeProgram)).size, note: "in the extracted courses" },
          ]} />

          <Panel title="Documents" flush>
            <Ledger rows={jobs} keyOf={j => j.id} minWidth={700} status={j => toneOf(j.status)}
              empty={{ title: "No document ingested", detail: "Upload a catalogue document; its courses are extracted for review before anything enters the register." }}
              columns={[
                { key: "name", label: "Document", render: j => <strong>{j.documentName}</strong> },
                { key: "type", label: "Type", render: j => j.documentType ?? "—" },
                { key: "status", label: "Status", render: j => <Mark tone={toneOf(j.status)}>{words(j.status)}</Mark> },
                { key: "confidence", label: "Confidence", render: j => `${(j.extractionConfidence * 100).toFixed(0)}%`, num: true },
                { key: "extracted", label: "Extracted", render: j => j.coursesExtracted, num: true },
                { key: "inserted", label: "Inserted", render: j => j.coursesInserted, num: true },
                { key: "updated", label: "Updated", render: j => j.coursesUpdated, num: true },
              ]} />
          </Panel>

          <Panel title="Extracted courses" aside={
            <label className="toolbar-inline"><span className="sr-only">Document</span>
              <select value={job} onChange={e => setJob(e.target.value)}>
                <option value="">All documents</option>
                {jobs.map(j => <option key={j.id} value={j.id}>{j.documentName}</option>)}
              </select>
            </label>} flush>
            <Ledger rows={courses} keyOf={c => c.id} minWidth={800}
              empty={{ title: "Nothing extracted", detail: "Courses appear here once a catalogue document has been read." }}
              columns={[
                { key: "programme", label: "Programme", render: c => c.degreeProgram },
                { key: "semester", label: "Semester", render: c => c.semesterLabel ?? `Semester ${c.semesterSequence}` },
                { key: "code", label: "Code", render: c => c.courseCode, mono: true },
                { key: "title", label: "Title", render: c => c.courseTitle },
                { key: "credits", label: "Credits", render: c => c.creditHoursTotal, num: true },
                { key: "type", label: "Type", render: c => words(c.courseType) },
                { key: "prerequisites", label: "Prerequisites", render: c => prerequisites(c.prerequisitesJson) || "none" },
              ]} />
          </Panel>
        </div>
      )}
    </ModulePage>
  );
}
