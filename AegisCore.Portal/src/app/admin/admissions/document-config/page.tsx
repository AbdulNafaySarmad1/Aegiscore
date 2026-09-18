"use client";

import { useState, type FormEvent } from "react";
import { apiGet, apiPost, release } from "@/lib/api-client";
import { Panel, Mark } from "@/components/register";
import { Ledger, words } from "@/components/register/ledger";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Intake = { id: string; year: number; semester: number; name: string; status: number };
type Requirement = {
  id: string; intakeId: string; programId: string; documentType: string;
  isRequired: boolean; acceptedFormat: string; maxFileSizeMb: number;
};

const SEMESTERS = [{ value: 1, label: "Fall" }, { value: 2, label: "Spring" }, { value: 3, label: "Winter" }];
const STATUS = ["", "Draft", "Open", "Closed"];
const DOCUMENTS = ["Matric", "Intermediate", "OLevels", "ALevels", "IBCC_Equivalency", "NationalID", "DomicileCertificate", "Photo", "Other"];

async function patch(path: string, body: unknown) {
  const response = await fetch(path, { method: "PATCH", credentials: "include", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
  if (!response.ok) throw new Error((await response.json().catch(() => ({}))).error ?? `That change was refused (${response.status}).`);
  await release(response);
}

async function remove(path: string) {
  const response = await fetch(path, { method: "DELETE", credentials: "include" });
  await release(response);
  if (!response.ok) throw new Error(`That could not be removed (${response.status}).`);
}

/**
 * Intakes, and what each programme asks an applicant to upload. An intake that is not open accepts
 * no applications, and a requirement added here appears on the applicant's own screen at once.
 */
export default function DocumentConfigPage() {
  const { state, reload } = useModuleData<Intake[]>(signal => apiGet<Intake[]>("/api/admissions/intakes", signal));
  const action = useAction();
  const [intake, setIntake] = useState({ year: new Date().getFullYear(), semester: 1, name: "" });

  const [intakeId, setIntakeId] = useState("");
  const [programId, setProgramId] = useState("");
  const [programCode, setProgramCode] = useState("");
  const [draft, setDraft] = useState({ documentType: "Matric", isRequired: true, acceptedFormat: "Pdf", maxFileSizeMb: 5 });

  const chosen = Boolean(intakeId && programId);
  const requirements = useModuleData<Requirement[]>(
    signal => chosen
      ? apiGet<Requirement[]>(`/api/admissions/config/documents?intakeId=${intakeId}&programId=${programId}`, signal)
      : Promise.resolve([]),
    `${intakeId}|${programId}`
  );

  async function addIntake(e: FormEvent) {
    e.preventDefault();
    await action.run("intake", async () => {
      await apiPost("/api/admissions/intakes", intake);
      const name = intake.name;
      setIntake({ ...intake, name: "" });
      return `${name} is created as a draft. It accepts applications once you open it.`;
    }, reload);
  }

  async function setStatus(row: Intake, status: "Open" | "Closed") {
    await action.run(row.id, async () => {
      await patch(`/api/admissions/intakes/${row.id}/status`, { status });
      return status === "Open"
        ? `${row.name} is open: applicants can apply to it now.`
        : `${row.name} is closed: no further applications are accepted.`;
    }, reload);
  }

  async function saveCode(e: FormEvent) {
    e.preventDefault();
    await action.run("code", async () => {
      await release(await fetch(`/api/admissions/config/programs/${programId}/code`, {
        method: "PUT", credentials: "include", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ code: programCode })
      }));
      return `This programme is now written as ${programCode} on applicant numbers.`;
    });
  }

  async function addRequirement(e: FormEvent) {
    e.preventDefault();
    await action.run("requirement", async () => {
      await apiPost("/api/admissions/config/documents", { intakeId, programId, ...draft });
      return `${words(draft.documentType)} is now ${draft.isRequired ? "required" : "optional"} for this programme.`;
    }, requirements.reload);
  }

  return (
    <ModulePage
      title="Intakes and documents"
      crumbs={[{ label: "Admissions", href: "/admin/admissions" }, { label: "Intakes and documents" }]}
      meta={<span>Which intakes are open, and what each programme asks an applicant to upload.</span>}
      ident={(rows: Intake[]) => <><strong>{rows.filter(i => STATUS[i.status] === "Open").length}</strong><br />open</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {intakes => (
        <div className="record">
          <div className="record-body">
            <Panel title="Intakes" flush>
              <Ledger rows={intakes} keyOf={i => i.id} minWidth={680}
                status={i => STATUS[i.status] === "Open" ? "laurel" : STATUS[i.status] === "Closed" ? undefined : "brass"}
                empty={{ title: "No intake yet", detail: "An intake is an admission cycle: a year, a term, and the programmes open within it." }}
                columns={[
                  { key: "name", label: "Intake", render: i => i.name },
                  { key: "year", label: "Year", render: i => i.year, num: true, nowrap: true },
                  { key: "semester", label: "Term", render: i => SEMESTERS.find(s => s.value === i.semester)?.label ?? "—" },
                  { key: "status", label: "Status", render: i => <Mark tone={STATUS[i.status] === "Open" ? "laurel" : STATUS[i.status] === "Closed" ? "ink" : "brass"}>{STATUS[i.status] || "Unknown"}</Mark> },
                  {
                    key: "act", label: "", nowrap: true, render: i => (
                      <span className="btn-row">
                        <button type="button" className="btn btn-secondary btn-sm" disabled={action.busy !== null || STATUS[i.status] === "Open"} onClick={() => setStatus(i, "Open")}>Open</button>
                        <button type="button" className="btn btn-ghost btn-sm" disabled={action.busy !== null || STATUS[i.status] === "Closed"} onClick={() => setStatus(i, "Closed")}>Close</button>
                      </span>
                    ),
                  },
                ]} />
            </Panel>

            <Panel title="Documents a programme asks for">
              <div className="filter-row">
                <label><span>Intake</span>
                  <select value={intakeId} onChange={e => setIntakeId(e.target.value)}>
                    <option value="">Choose an intake</option>
                    {intakes.map(i => <option key={i.id} value={i.id}>{i.name}</option>)}
                  </select>
                </label>
                <label><span>Programme</span>
                  <input value={programId} onChange={e => setProgramId(e.target.value)} placeholder="programme identifier" size={38} />
                </label>
              </div>

              {!chosen ? (
                <p className="field-hint">Choose an intake and name a programme to see what it asks for.</p>
              ) : requirements.state.kind === "loading" ? (
                <p className="field-hint">Reading the requirements…</p>
              ) : requirements.state.kind === "error" ? (
                <p className="field-hint">{requirements.state.message}</p>
              ) : (
                <Ledger rows={requirements.state.data} keyOf={r => r.id} minWidth={620}
                  empty={{ title: "Nothing is asked for yet", detail: "Until a document is listed here, an applicant to this programme uploads nothing." }}
                  columns={[
                    { key: "doc", label: "Document", render: r => words(r.documentType) },
                    { key: "required", label: "Required", render: r => r.isRequired ? <Mark tone="brass">Required</Mark> : "Optional" },
                    { key: "format", label: "Format", render: r => r.acceptedFormat === "Pdf" ? "PDF" : "JPEG or PNG" },
                    { key: "size", label: "Largest file", render: r => `${r.maxFileSizeMb} MB`, num: true, nowrap: true },
                    {
                      key: "act", label: "", nowrap: true, render: r => (
                        <button type="button" className="btn btn-danger btn-sm" disabled={action.busy !== null}
                          onClick={() => action.run(r.id, async () => { await remove(`/api/admissions/config/documents/${r.id}`); return `${words(r.documentType)} is no longer asked for.`; }, requirements.reload)}>
                          Remove
                        </button>
                      ),
                    },
                  ]} />
              )}
            </Panel>
          </div>

          <aside className="record-margin">
            <Panel title="Add an intake">
              <form className="panel-form" onSubmit={addIntake}>
                <label><span>Name</span><input value={intake.name} onChange={e => setIntake({ ...intake, name: e.target.value })} placeholder="Fall 2026 Admissions" required /></label>
                <label><span>Year</span><input type="number" value={intake.year} onChange={e => setIntake({ ...intake, year: +e.target.value })} required /></label>
                <label><span>Term</span>
                  <select value={intake.semester} onChange={e => setIntake({ ...intake, semester: +e.target.value })}>
                    {SEMESTERS.map(s => <option key={s.value} value={s.value}>{s.label}</option>)}
                  </select>
                </label>
                <p className="field-hint">A new intake starts as a draft and accepts nobody until it is opened.</p>
                <button type="submit" className={`btn btn-primary${action.busy === "intake" ? " btn-loading" : ""}`} disabled={action.busy !== null || !intake.name.trim()}>Add intake</button>
              </form>
            </Panel>

            {chosen && (
              <>
                <Panel title="Programme code">
                  <form className="panel-form" onSubmit={saveCode}>
                    <label><span>Code</span><input value={programCode} onChange={e => setProgramCode(e.target.value.toUpperCase())} placeholder="BCS" /></label>
                    <p className="field-hint">The code appears in every applicant number for this programme, so changing it changes what new applicants are given.</p>
                    <button type="submit" className={`btn btn-secondary${action.busy === "code" ? " btn-loading" : ""}`} disabled={action.busy !== null || !programCode.trim()}>Save the code</button>
                  </form>
                </Panel>

                <Panel title="Ask for a document">
                  <form className="panel-form" onSubmit={addRequirement}>
                    <label><span>Document</span>
                      <select value={draft.documentType} onChange={e => setDraft({ ...draft, documentType: e.target.value })}>
                        {DOCUMENTS.map(d => <option key={d} value={d}>{words(d)}</option>)}
                      </select>
                    </label>
                    <label><span>Accepted as</span>
                      <select value={draft.acceptedFormat} onChange={e => setDraft({ ...draft, acceptedFormat: e.target.value })}>
                        <option value="Pdf">PDF</option>
                        <option value="Image">JPEG or PNG</option>
                      </select>
                    </label>
                    <label><span>Largest file (MB)</span><input type="number" min={1} max={50} value={draft.maxFileSizeMb} onChange={e => setDraft({ ...draft, maxFileSizeMb: +e.target.value })} /></label>
                    <label className="check-row"><input type="checkbox" checked={draft.isRequired} onChange={e => setDraft({ ...draft, isRequired: e.target.checked })} /><span>An applicant cannot continue without it</span></label>
                    <button type="submit" className={`btn btn-primary${action.busy === "requirement" ? " btn-loading" : ""}`} disabled={action.busy !== null}>Add or update</button>
                  </form>
                </Panel>
              </>
            )}
          </aside>
        </div>
      )}
    </ModulePage>
  );
}
