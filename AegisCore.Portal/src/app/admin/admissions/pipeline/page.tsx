"use client";

import { useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel } from "@/components/register";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Card = { applicantProfileId: string; applicantId: string; name: string | null; programId: string; lastUpdated: string; column: string };
type Board = { columns: string[]; cards: Card[] };

/** Moving into one of these decides someone's admission, so it is confirmed rather than dropped. */
const DECIDING = new Set(["Admitted", "Enrolled", "Rejected"]);

/**
 * The admissions board. Applicants sit in a column each; moving one is a decision about a person,
 * so the columns that decide an outcome ask first and say what they are about to do.
 */
export default function PipelinePage() {
  const { date } = useTenantFormatter();
  const { state, reload } = useModuleData<Board>(signal => apiGet<Board>("/api/admissions/pipeline", signal));
  const action = useAction();
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [dragging, setDragging] = useState<string | null>(null);
  const [pending, setPending] = useState<{ ids: string[]; column: string } | null>(null);
  const [email, setEmail] = useState("");

  function request(ids: string[], column: string) {
    if (ids.length === 0) return;
    if (DECIDING.has(column)) { setPending({ ids, column }); setEmail(""); return; }
    void move(ids, column, null);
  }

  async function move(ids: string[], column: string, institutionalEmail: string | null) {
    setPending(null);
    await action.run("move", async () => {
      if (ids.length === 1) await apiPost("/api/admissions/pipeline/move", { applicantId: ids[0], column, manualInstitutionalEmail: institutionalEmail });
      else await apiPost("/api/admissions/pipeline/bulk-move", { applicantIds: ids, column });
      setSelected(new Set());
      return ids.length === 1 ? `One applicant moved to ${column}.` : `${ids.length} applicants moved to ${column}.`;
    }, reload);
  }

  const toggle = (id: string) => setSelected(s => { const n = new Set(s); if (n.has(id)) n.delete(id); else n.add(id); return n; });

  return (
    <ModulePage
      title="Admissions board"
      crumbs={[{ label: "Admissions", href: "/admin/admissions" }, { label: "Board" }]}
      meta={<span>Every applicant in the cycle, in the column they have reached. Drag one across, or select several and move them together.</span>}
      ident={(b: Board) => <><strong>{b.cards.length}</strong><br />applicants</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {board => (
        <>
          {pending && (
            <Panel
              tone={pending.column === "Rejected" ? "oxblood" : "brass"}
              title={`Move ${pending.ids.length === 1 ? "this applicant" : `${pending.ids.length} applicants`} to ${pending.column}?`}
            >
              <div className="panel-form">
                <p>
                  {pending.column === "Rejected"
                    ? "A rejection is recorded against the applicant and the applicant is told. It cannot be undone from this board."
                    : pending.column === "Enrolled"
                      ? "Enrolment is recorded, the applicant is told, and their student record is created."
                      : "Admission is recorded and the applicant is told."}
                </p>
                {(pending.column === "Admitted" || pending.column === "Enrolled") && pending.ids.length === 1 && (
                  <label>
                    <span>Institutional email</span>
                    <input value={email} onChange={e => setEmail(e.target.value)} placeholder="leave blank if provisioning is automatic" />
                  </label>
                )}
                <div className="btn-row">
                  <button type="button" className={`btn btn-primary${action.busy === "move" ? " btn-loading" : ""}`} onClick={() => move(pending.ids, pending.column, email.trim() || null)}>
                    Move to {pending.column}
                  </button>
                  <button type="button" className="btn btn-ghost" onClick={() => setPending(null)}>Leave as it is</button>
                </div>
              </div>
            </Panel>
          )}

          {selected.size > 0 && (
            <Panel title={`${selected.size} selected`} aside={<button type="button" className="btn btn-ghost btn-sm" onClick={() => setSelected(new Set())}>Clear</button>}>
              <div className="btn-row">
                {board.columns.filter(c => c !== "Test Completed").map(c => (
                  <button key={c} type="button" className="btn btn-secondary btn-sm" disabled={action.busy !== null} onClick={() => request([...selected], c)}>Move to {c}</button>
                ))}
              </div>
            </Panel>
          )}

          <div className="board" role="list">
            {board.columns.map(col => {
              const cards = board.cards.filter(c => c.column === col);
              return (
                <section
                  key={col}
                  role="listitem"
                  className="board-column"
                  data-drop={dragging ? "true" : undefined}
                  onDragOver={e => e.preventDefault()}
                  onDrop={() => { if (dragging) { request([dragging], col); setDragging(null); } }}
                >
                  <h2 className="board-column-head">{col}<span>{cards.length}</span></h2>
                  {cards.length === 0
                    ? <p className="board-empty">None</p>
                    : cards.map(c => (
                      <article
                        key={c.applicantProfileId}
                        className="board-card"
                        draggable
                        onDragStart={() => setDragging(c.applicantProfileId)}
                        onDragEnd={() => setDragging(null)}
                        data-selected={selected.has(c.applicantProfileId) ? "true" : undefined}
                      >
                        <label className="board-card-id">
                          <input type="checkbox" checked={selected.has(c.applicantProfileId)} onChange={() => toggle(c.applicantProfileId)} aria-label={`Select ${c.applicantId}`} />
                          <span className="mono">{c.applicantId}</span>
                        </label>
                        <p className="board-card-name">{c.name ?? "Name not yet given"}</p>
                        <p className="board-card-when">{date(c.lastUpdated)}</p>
                      </article>
                    ))}
                </section>
              );
            })}
          </div>
        </>
      )}
    </ModulePage>
  );
}
