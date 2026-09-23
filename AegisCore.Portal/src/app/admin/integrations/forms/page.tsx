"use client";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { ApiError, apiGet, apiPost, apiPut } from "@/lib/api-client";
import { RegisterHead, Panel, Facts, Notice, ErrorNotice, RuledLoading, Empty, Mark, type Tone } from "@/components/register";

type Source = {
  id: string; name: string; provider: string; externalFormId: string | null; scaleMax: number;
  defaultCourseId: string | null; defaultSemester: string | null; isActive: boolean; createdAt: string;
  lastReceivedAt: string | null; tokenHint: string; webhookUrl: string | null;
  fields: number; unmappedFields: number; submissions: number;
};
type Mapping = {
  id: string; externalFieldId: string; questionText: string; target: string;
  cloCode: string | null; programOutcomeId: string | null; category: string | null; sortOrder: number;
};
type Options = {
  targets: string[];
  courses: { id: string; code: string | null; title: string }[];
  cloCodes: { code: string; courses: number }[];
  programOutcomes: { id: string; code: string; description: string }[];
};
type Delivery = {
  id: string; externalSubmissionId: string; receivedAt: string; status: string; fieldCount: number;
  recordedCount: number; unmappedCount: number; instructorName: string | null; semester: string | null;
  course: string | null; problems: string[];
};
type Issued = { token: string; webhookUrl: string; name: string; provider: string };
type Row = Mapping & { dirty?: boolean };

const TARGET_LABEL: Record<string, string> = {
  Unmapped: "Not mapped yet", Ignore: "Ignore (not a rating)", Course: "Course the response is about",
  Instructor: "Instructor", Semester: "Semester", Clo: "Rates a CLO", Plo: "Rates a PLO directly",
  Satisfaction: "Teaching / satisfaction item",
};
const PROVIDER_LABEL: Record<string, string> = { jotform: "JotForm", generic: "Any tool (AegisCore JSON contract)", "aegiscore-forms": "AegisCore form" };
const DELIVERY_TONE: Record<string, Tone> = { Recorded: "laurel", Partial: "brass", Held: "oxblood" };

function when(value: string | null) { return value ? new Date(value).toLocaleString() : "Never"; }
function tokenUrl(issued: Issued) { return `${issued.webhookUrl}?token=${encodeURIComponent(issued.token)}`; }

/**
 * Where an institution connects a survey that runs in an outside form tool (JotForm today; any tool
 * that can post the documented JSON contract tomorrow) and says what each of its questions means:
 * which field names the course, which rate a CLO or a PLO, which are teaching items. Built for a
 * form of 77 questions or more — filter, select many, map them in one move, or paste the question
 * list before the first response arrives.
 *
 * Access: gated on the Integrations permission, which administrators hold and which the role
 * wizard can grant to an IT Lead role. Whether the Registrar should own the mapping day to day is
 * an open question recorded in docs/integrations/external-forms.md.
 */
export default function FormIntegrationsPage() {
  const [sources, setSources] = useState<Source[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [retry, setRetry] = useState(0);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [options, setOptions] = useState<Options | null>(null);
  const [rows, setRows] = useState<Row[] | null>(null);
  const [deliveries, setDeliveries] = useState<Delivery[] | null>(null);
  const [outcome, setOutcome] = useState<{ tone: Tone; text: string } | null>(null);
  const [issued, setIssued] = useState<Issued | null>(null);
  const [busy, setBusy] = useState<string | null>(null);

  // Filtering and bulk mapping.
  const [search, setSearch] = useState("");
  const [show, setShow] = useState("all");
  const [checked, setChecked] = useState<Set<string>>(new Set());
  const [bulkTarget, setBulkTarget] = useState("Clo");
  const [bulkClo, setBulkClo] = useState("");
  const [bulkPlo, setBulkPlo] = useState("");
  const [bulkCategory, setBulkCategory] = useState("Instructor");
  const [pasted, setPasted] = useState("");
  const headerCheck = useRef<HTMLInputElement>(null);

  // A new connection.
  const [draft, setDraft] = useState({ name: "", provider: "jotform", externalFormId: "", scaleMax: 5, defaultCourseId: "", defaultSemester: "" });

  const loadSources = useCallback(async () => {
    try {
      const [list, opts] = await Promise.all([apiGet<Source[]>("/api/integrations/forms/sources"), apiGet<Options>("/api/integrations/forms/options")]);
      setSources(list); setOptions(opts); setError(null);
      setSelectedId(current => current ?? list[0]?.id ?? null);
    } catch (reason) { setError(reason instanceof ApiError ? reason.message : "The form connections could not be read."); }
  }, []);

  const loadSource = useCallback(async (id: string) => {
    const [mappings, recent] = await Promise.all([
      apiGet<Mapping[]>(`/api/integrations/forms/sources/${id}/mappings`),
      apiGet<Delivery[]>(`/api/integrations/forms/sources/${id}/submissions?take=25`),
    ]);
    setRows(mappings); setDeliveries(recent);
  }, []);

  useEffect(() => { Promise.resolve().then(loadSources); }, [loadSources, retry]);
  useEffect(() => {
    if (!selectedId) return;
    // eslint-disable-next-line react-hooks/set-state-in-effect -- a new source starts with a clean slate.
    setRows(null); setDeliveries(null); setChecked(new Set());
    loadSource(selectedId).catch(reason => setError(reason instanceof ApiError ? reason.message : "The mapping could not be read."));
  }, [selectedId, loadSource]);

  // Deliveries are watched while the page is open, so a response submitted in the form tool shows up here.
  useEffect(() => {
    if (!selectedId) return;
    const timer = window.setInterval(async () => {
      if (document.hidden) return;
      try {
        setDeliveries(await apiGet<Delivery[]>(`/api/integrations/forms/sources/${selectedId}/submissions?take=25`));
        const fresh = await apiGet<Mapping[]>(`/api/integrations/forms/sources/${selectedId}/mappings`);
        // New fields discovered by a delivery join the list; edits in progress are kept.
        setRows(current => {
          if (!current) return fresh;
          const known = new Set(current.map(r => r.externalFieldId));
          const added = fresh.filter(m => !known.has(m.externalFieldId));
          return added.length === 0 ? current : [...current, ...added];
        });
      } catch { /* the next tick tries again */ }
    }, 5000);
    return () => window.clearInterval(timer);
  }, [selectedId]);

  const source = sources?.find(s => s.id === selectedId) ?? null;
  const visible = useMemo(() => {
    if (!rows) return [];
    const needle = search.trim().toLowerCase();
    return rows.filter(r =>
      (show === "all" || (show === "unmapped" ? r.target === "Unmapped" : show === "dirty" ? r.dirty : r.target === show))
      && (!needle || r.externalFieldId.toLowerCase().includes(needle) || r.questionText.toLowerCase().includes(needle)));
  }, [rows, search, show]);
  const dirty = rows?.filter(r => r.dirty) ?? [];
  const unmapped = rows?.filter(r => r.target === "Unmapped").length ?? 0;
  const allVisibleChecked = visible.length > 0 && visible.every(r => checked.has(r.externalFieldId));
  useEffect(() => {
    if (headerCheck.current) headerCheck.current.indeterminate = !allVisibleChecked && visible.some(r => checked.has(r.externalFieldId));
  }, [allVisibleChecked, visible, checked]);

  function edit(field: string, change: Partial<Mapping>) {
    setRows(current => current?.map(r => r.externalFieldId === field ? { ...r, ...change, dirty: true } : r) ?? null);
  }

  function applyBulk() {
    if (checked.size === 0) return;
    const change: Partial<Mapping> = {
      target: bulkTarget,
      cloCode: bulkTarget === "Clo" ? bulkClo || null : null,
      programOutcomeId: bulkTarget === "Plo" ? bulkPlo || null : null,
      category: bulkTarget === "Satisfaction" ? bulkCategory || "General" : null,
    };
    setRows(current => current?.map(r => checked.has(r.externalFieldId) ? { ...r, ...change, dirty: true } : r) ?? null);
    setOutcome({ tone: "ink", text: `${checked.size} field(s) set to “${TARGET_LABEL[bulkTarget]}”. Save to keep the change.` });
    setChecked(new Set());
  }

  function toggleAllVisible() {
    setChecked(current => {
      const next = new Set(current);
      if (allVisibleChecked) visible.forEach(r => next.delete(r.externalFieldId));
      else visible.forEach(r => next.add(r.externalFieldId));
      return next;
    });
  }

  async function run(label: string, action: () => Promise<void>) {
    setBusy(label); setOutcome(null);
    try { await action(); }
    catch (reason) {
      const detail = reason instanceof ApiError ? reason.message : "The change could not be made.";
      setOutcome({ tone: "oxblood", text: detail });
    } finally { setBusy(null); }
  }

  const save = () => run("save", async () => {
    if (!selectedId || dirty.length === 0) return;
    const body = dirty.map(r => ({ externalFieldId: r.externalFieldId, questionText: r.questionText, target: r.target, cloCode: r.cloCode, programOutcomeId: r.programOutcomeId, category: r.category, sortOrder: r.sortOrder }));
    const result = await apiPut<{ saved: number; unmapped: number }>(`/api/integrations/forms/sources/${selectedId}/mappings`, body);
    const replay = await apiPost<{ submissions: number; recorded: number; partial: number; held: number; answers: number }>(`/api/integrations/forms/sources/${selectedId}/reprocess`);
    await loadSource(selectedId); await loadSources();
    setOutcome({ tone: "laurel", text: `${result.saved} mapping(s) saved; ${result.unmapped} field(s) still unmapped. Re-applied to ${replay.submissions} stored response(s): ${replay.answers} answer(s) now recorded.` });
  });

  const importList = () => run("import", async () => {
    if (!selectedId) return;
    // One question per line: field id, question text[, target[, CLO code or category]] — comma or tab separated.
    const parsed = pasted.split(/\r?\n/).map(line => line.trim()).filter(Boolean).map((line, index) => {
      const cells = (line.includes("\t") ? line.split("\t") : line.split(",")).map(c => c.trim());
      const target = cells[2] && TARGET_LABEL[cells[2]] ? cells[2] : "Unmapped";
      return {
        externalFieldId: cells[0], questionText: cells[1] || cells[0], target, sortOrder: index,
        cloCode: target === "Clo" ? cells[3] || null : null,
        category: target === "Satisfaction" ? cells[3] || "General" : null,
        programOutcomeId: target === "Plo" ? options?.programOutcomes.find(p => p.code.toLowerCase() === (cells[3] ?? "").toLowerCase())?.id ?? null : null,
      };
    }).filter(r => r.externalFieldId && !/^field\s*id$/i.test(r.externalFieldId));
    if (parsed.length === 0) throw new ApiError("Paste at least one line: field id, question text.", 400);
    const result = await apiPut<{ saved: number; created: number; unmapped: number }>(`/api/integrations/forms/sources/${selectedId}/mappings`, parsed);
    setPasted("");
    await loadSource(selectedId); await loadSources();
    setOutcome({ tone: "laurel", text: `${result.saved} question(s) loaded (${result.created} new); ${result.unmapped} still to map.` });
  });

  const create = () => run("create", async () => {
    const created = await apiPost<{ id: string; name: string; provider: string; token: string; webhookUrl: string }>("/api/integrations/forms/sources", {
      name: draft.name, provider: draft.provider, externalFormId: draft.externalFormId || null, scaleMax: draft.scaleMax,
      defaultCourseId: draft.defaultCourseId || null, defaultSemester: draft.defaultSemester || null,
    });
    setIssued({ token: created.token, webhookUrl: created.webhookUrl, name: created.name, provider: created.provider });
    setDraft({ name: "", provider: "jotform", externalFormId: "", scaleMax: 5, defaultCourseId: "", defaultSemester: "" });
    setSelectedId(created.id);
    await loadSources();
  });

  const rotate = () => run("rotate", async () => {
    if (!source || !window.confirm(`Issue a new token for “${source.name}”? The form tool stops delivering until its webhook URL is updated.`)) return;
    const result = await apiPost<{ token: string; webhookUrl: string }>(`/api/integrations/forms/sources/${source.id}/token`);
    setIssued({ ...result, name: source.name, provider: source.provider });
    await loadSources();
  });

  const crumbs = [{ label: "Administration" }, { label: "Integrations" }, { label: "Form connections" }];
  if (error && !sources) return (
    <div className="page">
      <RegisterHead title="Form connections" crumbs={crumbs} reveal={false} />
      <ErrorNotice message={error} hint="You need the Integrations permission to open this page." onRetry={() => setRetry(n => n + 1)} />
    </div>
  );
  if (!sources || !options) return <div className="page"><RuledLoading label="Reading the form connections" rows={5} /></div>;

  return (
    <div className="page form-integrations">
      <RegisterHead
        title="Form connections"
        crumbs={crumbs}
        ident={<><strong>{sources.length} form{sources.length === 1 ? "" : "s"} connected</strong><br />{source ? source.name : "None selected"}</>}
        meta={<span>Surveys that run in an outside form tool send each response here by webhook. The mapping below says what every question means, so CLO and PLO results reach the outcome report with no re-keying.</span>}
        actions={sources.length > 1 ? (
          <label className="toolbar-inline"><span className="sr-only">Form</span>
            <select value={selectedId ?? ""} onChange={e => setSelectedId(e.target.value)} aria-label="Form connection">
              {sources.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
            </select>
          </label>
        ) : undefined}
      />

      {issued && (
        <Notice tone="brass" role="status" title={`Webhook for “${issued.name}” — copy it now`}
          actions={<button type="button" className="btn btn-secondary btn-sm" onClick={() => setIssued(null)}>I have copied it</button>}>
          <p>The token is shown once; only a fingerprint of it is kept.</p>
          {issued.provider === "jotform" ? (
            <p>In JotForm open <em>Settings → Integrations → WebHooks</em> and paste this URL. JotForm&apos;s webhook settings take only a URL, so the token travels in it — keep the URL private and use HTTPS.</p>
          ) : (
            <p>Send the token as <code>Authorization: Bearer …</code> (or <code>X-AegisCore-Form-Token</code>) and post the JSON contract to the webhook URL.</p>
          )}
          <p className="mono form-integrations-secret" data-testid="issued-webhook">{issued.provider === "jotform" ? tokenUrl(issued) : issued.webhookUrl}</p>
          {issued.provider !== "jotform" && <p className="mono form-integrations-secret" data-testid="issued-token">{issued.token}</p>}
        </Notice>
      )}
      {outcome && <Notice tone={outcome.tone} role="status">{outcome.text}</Notice>}

      {source ? (
        <Panel title={source.name} aside={<Mark tone={source.isActive ? "laurel" : "oxblood"}>{source.isActive ? "Receiving" : "Paused"}</Mark>}>
          <Facts inline items={[
            { label: "Form tool", value: PROVIDER_LABEL[source.provider] ?? source.provider },
            { label: "Form id in the tool", value: source.externalFormId ?? "Any", mono: true },
            { label: "Webhook", value: source.webhookUrl ?? "Not applicable", mono: true },
            { label: "Token", value: `…${source.tokenHint}`, mono: true },
            { label: "Rating scale", value: `1 to ${source.scaleMax}` },
            { label: "Last response", value: when(source.lastReceivedAt) },
            { label: "Questions", value: `${source.fields} (${source.unmappedFields} unmapped)` },
            { label: "Responses received", value: String(source.submissions) },
          ]} />
          <div className="toolbar">
            <button type="button" className={`btn btn-secondary btn-sm${busy === "rotate" ? " btn-loading" : ""}`} disabled={busy !== null || source.provider === "aegiscore-forms"} onClick={rotate}>Issue a new token</button>
          </div>
        </Panel>
      ) : (
        <Panel title="No form connected yet"><Empty title="Connect the first form below.">Each connection gets its own webhook token.</Empty></Panel>
      )}

      {source && rows && (
        <Panel title="What each question means" aside={<span className="text-muted" data-testid="mapping-summary">{rows.length} question{rows.length === 1 ? "" : "s"} · {unmapped} unmapped{dirty.length ? ` · ${dirty.length} unsaved` : ""}</span>}>
          <div className="toolbar" role="group" aria-label="Filter questions">
            <label>Search
              <input type="search" value={search} placeholder="Field id or question text" onChange={e => setSearch(e.target.value)} />
            </label>
            <label>Show
              <select value={show} onChange={e => setShow(e.target.value)}>
                <option value="all">All questions</option>
                <option value="unmapped">Not mapped yet</option>
                <option value="dirty">Changed, not saved</option>
                {options.targets.filter(t => t !== "Unmapped").map(t => <option key={t} value={t}>{TARGET_LABEL[t]}</option>)}
              </select>
            </label>
            <span className="toolbar-spacer" />
            <button type="button" className={`btn btn-primary${busy === "save" ? " btn-loading" : ""}`} disabled={busy !== null || dirty.length === 0} onClick={save}>
              Save {dirty.length || ""} change{dirty.length === 1 ? "" : "s"} and re-apply
            </button>
          </div>

          {checked.size > 0 && (
            <div className="toolbar-selection" role="region" aria-label="Map selected questions">
              <strong>{checked.size} selected</strong>
              <label className="toolbar-inline"><span className="sr-only">Map selected to</span>
                <select value={bulkTarget} onChange={e => setBulkTarget(e.target.value)} aria-label="Map selected to">
                  {options.targets.map(t => <option key={t} value={t}>{TARGET_LABEL[t]}</option>)}
                </select>
              </label>
              {bulkTarget === "Clo" && <CloPicker value={bulkClo} onChange={setBulkClo} codes={options.cloCodes} label="CLO for selected" />}
              {bulkTarget === "Plo" && <PloPicker value={bulkPlo} onChange={setBulkPlo} outcomes={options.programOutcomes} label="PLO for selected" />}
              {bulkTarget === "Satisfaction" && <input aria-label="Category for selected" value={bulkCategory} onChange={e => setBulkCategory(e.target.value)} placeholder="Instructor, Course, Resources" />}
              <button type="button" className="btn btn-secondary btn-sm" onClick={applyBulk} disabled={(bulkTarget === "Clo" && !bulkClo) || (bulkTarget === "Plo" && !bulkPlo)}>Apply to selected</button>
              <button type="button" className="btn btn-quiet btn-sm" onClick={() => setChecked(new Set())}>Clear selection</button>
            </div>
          )}

          {rows.length === 0 ? (
            <Empty title="No questions known yet.">Paste the form&apos;s question list below, or submit one test response in the form tool — every field it sends appears here to be mapped.</Empty>
          ) : (
            <div className="ledger-wrap form-integrations-mapping">
              <table className="ledger" aria-label="Question mapping">
                <thead>
                  <tr>
                    <th scope="col" className="check"><input ref={headerCheck} type="checkbox" checked={allVisibleChecked} onChange={toggleAllVisible} aria-label={`Select all ${visible.length} shown questions`} /></th>
                    <th scope="col">Field</th>
                    <th scope="col">Question</th>
                    <th scope="col">Means</th>
                    <th scope="col">Outcome or heading</th>
                  </tr>
                </thead>
                <tbody>
                  {visible.map(r => (
                    <tr key={r.externalFieldId} data-status={r.dirty ? "brass" : r.target === "Unmapped" ? "oxblood" : "laurel"} aria-selected={checked.has(r.externalFieldId)}>
                      <td className="check"><input type="checkbox" checked={checked.has(r.externalFieldId)} aria-label={`Select ${r.externalFieldId}`}
                        onChange={() => setChecked(current => { const next = new Set(current); if (next.has(r.externalFieldId)) next.delete(r.externalFieldId); else next.add(r.externalFieldId); return next; })} /></td>
                      <td className="mono nowrap">{r.externalFieldId}</td>
                      <td>{r.questionText}</td>
                      <td>
                        <select value={r.target} aria-label={`Meaning of ${r.externalFieldId}`} onChange={e => edit(r.externalFieldId, { target: e.target.value })}>
                          {options.targets.map(t => <option key={t} value={t}>{TARGET_LABEL[t]}</option>)}
                        </select>
                      </td>
                      <td>
                        {r.target === "Clo" && <CloPicker value={r.cloCode ?? ""} onChange={code => edit(r.externalFieldId, { cloCode: code || null })} codes={options.cloCodes} label={`CLO for ${r.externalFieldId}`} />}
                        {r.target === "Plo" && <PloPicker value={r.programOutcomeId ?? ""} onChange={id => edit(r.externalFieldId, { programOutcomeId: id || null })} outcomes={options.programOutcomes} label={`PLO for ${r.externalFieldId}`} />}
                        {r.target === "Satisfaction" && <input aria-label={`Heading for ${r.externalFieldId}`} value={r.category ?? ""} placeholder="General" onChange={e => edit(r.externalFieldId, { category: e.target.value })} />}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {visible.length === 0 && <Empty title="No question matches this filter." />}
            </div>
          )}

          <details className="form-integrations-import">
            <summary>Load the question list in one go</summary>
            <p className="text-muted">One question per line: <code>field id, question text, meaning, CLO code or heading</code> (comma or tab separated — a spreadsheet column pasted as-is works). Meaning is one of {options.targets.join(", ")}; leave it off to map later. For JotForm, the field id is the question&apos;s key, such as <code>q12_instructorKnowledge</code>.</p>
            <textarea rows={6} value={pasted} onChange={e => setPasted(e.target.value)} aria-label="Question list" placeholder={"q3_course, Which course is this evaluation for?, Course\nq5_clo1, I can write programs to solve problems, Clo, CLO1\nq20_instructorPrepared, The instructor came prepared, Satisfaction, Instructor"} />
            <button type="button" className={`btn btn-secondary${busy === "import" ? " btn-loading" : ""}`} disabled={busy !== null || !pasted.trim()} onClick={importList}>Load questions</button>
          </details>
        </Panel>
      )}

      {source && deliveries && (
        <Panel title="Responses received" aside={<span className="text-muted">Checked every 5 seconds</span>}>
          {deliveries.length === 0 ? (
            <Empty title="Nothing received yet.">Submit a test response in the form tool; it appears here within seconds.</Empty>
          ) : (
            <div className="ledger-wrap">
              <table className="ledger" aria-label="Responses received">
                <thead><tr><th scope="col">Received</th><th scope="col">Response</th><th scope="col">State</th><th scope="col">Course</th><th scope="col">Instructor</th><th scope="col" className="num">Answers recorded</th><th scope="col">Needs attention</th></tr></thead>
                <tbody>
                  {deliveries.map(d => (
                    <tr key={d.id} data-status={DELIVERY_TONE[d.status] ?? "ink"}>
                      <td className="mono nowrap">{new Date(d.receivedAt).toLocaleTimeString()}<br /><small className="text-muted">{new Date(d.receivedAt).toLocaleDateString()}</small></td>
                      <td className="mono">{d.externalSubmissionId}</td>
                      <td><Mark tone={DELIVERY_TONE[d.status] ?? "ink"}>{d.status}</Mark></td>
                      <td>{d.course ?? <span className="text-muted">Unknown</span>}</td>
                      <td>{d.instructorName ?? <span className="text-muted">—</span>}</td>
                      <td className="num">{d.recordedCount} of {d.fieldCount}</td>
                      <td>{d.problems.length === 0 ? <span className="text-muted">Nothing</span> : <ul className="form-integrations-problems">{d.problems.slice(0, 3).map(p => <li key={p}>{p}</li>)}{d.problems.length > 3 && <li>and {d.problems.length - 3} more</li>}</ul>}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Panel>
      )}

      <Panel title="Connect another form">
        <p className="text-muted">One connection per form. The token issued for it lets that form — and only that form — deliver responses to this institution.</p>
        <label><span>Name</span><input value={draft.name} placeholder="Course and teacher evaluation, Fall 2026" onChange={e => setDraft({ ...draft, name: e.target.value })} /></label>
        <label><span>Form tool</span>
          <select value={draft.provider} onChange={e => setDraft({ ...draft, provider: e.target.value })}>
            <option value="jotform">JotForm</option>
            <option value="generic">Any tool (AegisCore JSON contract)</option>
          </select>
        </label>
        <label><span>Form id in the tool <small className="text-muted">(optional; responses from any other form are refused)</small></span><input value={draft.externalFormId} onChange={e => setDraft({ ...draft, externalFormId: e.target.value })} /></label>
        <label><span>Top of the rating scale</span><input type="number" min={2} max={10} value={draft.scaleMax} onChange={e => setDraft({ ...draft, scaleMax: Number(e.target.value) || 5 })} /></label>
        <label><span>Course <small className="text-muted">(only if the whole form is about one course; otherwise map the question that names it)</small></span>
          <select value={draft.defaultCourseId} onChange={e => setDraft({ ...draft, defaultCourseId: e.target.value })}>
            <option value="">A question on the form names the course</option>
            {options.courses.map(c => <option key={c.id} value={c.id}>{c.code} {c.title}</option>)}
          </select>
        </label>
        <label><span>Semester <small className="text-muted">(optional)</small></span><input value={draft.defaultSemester} placeholder="Fall 2026" onChange={e => setDraft({ ...draft, defaultSemester: e.target.value })} /></label>
        <button type="button" className={`btn btn-primary${busy === "create" ? " btn-loading" : ""}`} disabled={busy !== null || !draft.name.trim()} onClick={create}>Connect and issue token</button>
      </Panel>
    </div>
  );
}

function CloPicker({ value, onChange, codes, label }: { value: string; onChange: (code: string) => void; codes: Options["cloCodes"]; label: string }) {
  return (
    <select value={value} onChange={e => onChange(e.target.value)} aria-label={label}>
      <option value="">Choose a CLO</option>
      {codes.map(c => <option key={c.code} value={c.code}>{c.code} (defined in {c.courses} course{c.courses === 1 ? "" : "s"})</option>)}
    </select>
  );
}

function PloPicker({ value, onChange, outcomes, label }: { value: string; onChange: (id: string) => void; outcomes: Options["programOutcomes"]; label: string }) {
  return (
    <select value={value} onChange={e => onChange(e.target.value)} aria-label={label}>
      <option value="">Choose a PLO</option>
      {outcomes.map(o => <option key={o.id} value={o.id}>{o.code} — {o.description.slice(0, 60)}</option>)}
    </select>
  );
}
