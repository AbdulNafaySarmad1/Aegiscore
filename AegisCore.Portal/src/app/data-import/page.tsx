"use client";

import { useState, type FormEvent } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel, Mark, Notice } from "@/components/register";
import { Ledger, toneOf } from "@/components/register/ledger";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Connection = { id: string; name: string; dbType: string; isActive: boolean; lastTestSuccessful: boolean; lastTestedAt: string | null };
type TableInfo = { name: string };
type ColumnInfo = { name: string; dataType: string; isNullable: boolean; maxLength: number | null };
type ImportJob = { id: string; entityType: string; status: string; totalRows: number; successRows: number; errorRows: number; skippedRows: number; startedAt: string; completedAt: string | null };

const ENTITIES = ["Student", "Faculty", "Course", "FeePayment", "Grade", "Attendance"];
const DATABASES = ["PostgreSQL", "MySQL", "MSSQL", "Oracle", "SQLite"];

/**
 * Bringing a previous system's records in. Nothing here writes to the old database: a connection is
 * read from, one table at a time, under a query the server checks is a plain SELECT.
 */
export default function DataImportPage() {
  const { dateTime } = useTenantFormatter();
  const { state, reload } = useModuleData<{ connections: Connection[]; jobs: ImportJob[]; templates: string[] }>(async signal => {
    const [connections, jobs, templates] = await Promise.all([
      apiGet<Connection[]>("/api/data-import/connections", signal).catch(() => []),
      apiGet<{ jobs: ImportJob[] }>("/api/data-import/jobs", signal).then(r => r.jobs).catch(() => []),
      apiGet<{ formats: string[] }>("/api/data-import/formats", signal).then(r => r.formats).catch(() => []),
    ]);
    return { connections, jobs, templates };
  });
  const action = useAction();

  const [connection, setConnection] = useState({ name: "", dbType: "PostgreSQL", connectionString: "", schema: "public" });
  const [browsing, setBrowsing] = useState<string | null>(null);
  const [table, setTable] = useState("");
  const [entityType, setEntityType] = useState("Student");
  const [mappingText, setMappingText] = useState("");

  const tables = useModuleData<TableInfo[]>(
    signal => browsing ? apiGet<{ tables: TableInfo[] }>(`/api/data-import/connections/${browsing}/tables`, signal).then(r => r.tables) : Promise.resolve([]),
    browsing ?? ""
  );
  const columns = useModuleData<ColumnInfo[]>(
    signal => browsing && table ? apiGet<{ columns: ColumnInfo[] }>(`/api/data-import/connections/${browsing}/tables/${table}/columns`, signal).then(r => r.columns) : Promise.resolve([]),
    `${browsing}|${table}`
  );

  async function addConnection(e: FormEvent) {
    e.preventDefault();
    await action.run("connect", async () => {
      await apiPost<Connection>("/api/data-import/connections", connection);
      const name = connection.name;
      setConnection({ ...connection, name: "", connectionString: "" });
      return `${name} is saved. Test it before importing from it.`;
    }, reload);
  }

  async function test(c: Connection) {
    await action.run(c.id, async () => {
      const result = await apiPost<{ success: boolean }>(`/api/data-import/connections/${c.id}/test`);
      return result.success
        ? `${c.name} answered.`
        : `${c.name} did not answer. Check the host, the credentials and that this container can reach it.`;
    }, reload);
  }

  async function startImport(e: FormEvent) {
    e.preventDefault();
    await action.run("import", async () => {
      const mappings: Record<string, string> = {};
      for (const line of mappingText.split("\n")) {
        const [source, destination] = line.split("→").map(part => part.trim());
        if (source && destination) mappings[source] = destination;
      }
      if (Object.keys(mappings).length === 0) {
        throw new Error("No mappings were read. Write one per line, as source_column → PropertyName.");
      }
      const job = await apiPost<ImportJob>("/api/data-import/jobs", {
        connectionId: browsing,
        entityType,
        sourceQuery: `SELECT * FROM ${table}`,
        // The server reads the mappings from a JSON string; sending an object meant they were
        // dropped without a word and every import ran with no mapping at all.
        fieldMappingsJson: JSON.stringify(mappings),
      });
      return `Import ${job.id.slice(0, 8)} started: ${Object.keys(mappings).length} columns mapped onto ${entityType}.`;
    }, reload);
  }

  return (
    <ModulePage
      title="Data import"
      crumbs={[{ label: "Operations" }, { label: "Data import" }]}
      meta={<span>Reading records out of a previous system. The old database is only ever read from.</span>}
      ident={d => <><strong>{d.jobs.length}</strong><br />imports run</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {({ connections, jobs, templates }) => (
        <div className="record">
          <div className="record-body">
            <Panel title="Databases this installation can read" flush>
              <Ledger rows={connections} keyOf={c => c.id} minWidth={700}
                status={c => c.lastTestedAt ? (c.lastTestSuccessful ? "laurel" : "oxblood") : "brass"}
                empty={{ title: "No database connected", detail: "Add the connection details of the system you are migrating from. A read-only account is enough, and safer." }}
                columns={[
                  { key: "name", label: "Connection", render: c => c.name },
                  { key: "type", label: "Kind", render: c => c.dbType },
                  {
                    key: "tested", label: "Last tested", render: c => c.lastTestedAt
                      ? <>{dateTime(c.lastTestedAt)}<br /><Mark tone={c.lastTestSuccessful ? "laurel" : "oxblood"}>{c.lastTestSuccessful ? "answered" : "did not answer"}</Mark></>
                      : <Mark tone="brass">never tested</Mark>,
                  },
                  {
                    key: "act", label: "", nowrap: true, render: c => (
                      <span className="btn-row">
                        <button type="button" className="btn btn-secondary btn-sm" onClick={() => { setBrowsing(c.id); setTable(""); }}>Browse</button>
                        <button type="button" className={`btn btn-ghost btn-sm${action.busy === c.id ? " btn-loading" : ""}`} disabled={action.busy !== null} onClick={() => test(c)}>Test</button>
                      </span>
                    ),
                  },
                ]} />
            </Panel>

            {browsing && (
              <Panel
                title={`Tables in ${connections.find(c => c.id === browsing)?.name ?? "that database"}`}
                aside={<button type="button" className="btn btn-ghost btn-sm" onClick={() => { setBrowsing(null); setTable(""); }}>Close</button>}
              >
                {tables.state.kind === "loading" ? <p className="field-hint">Asking the database what it holds…</p>
                  : tables.state.kind === "error" ? <p className="field-hint">{tables.state.message}</p>
                    : tables.state.data.length === 0 ? <p className="field-hint">That database reported no tables in this schema.</p>
                      : (
                        <div className="chip-row">
                          {tables.state.data.map(t => (
                            <button key={t.name} type="button" className={`chip${table === t.name ? " chip-on" : ""}`} onClick={() => setTable(t.name)}>{t.name}</button>
                          ))}
                        </div>
                      )}
              </Panel>
            )}

            {browsing && table && (
              <Panel title={`Columns in ${table}`} flush>
                {columns.state.kind === "ready" && (
                  <Ledger rows={columns.state.data} keyOf={c => c.name} minWidth={560}
                    empty={{ title: "That table reported no columns" }}
                    columns={[
                      { key: "name", label: "Column", render: c => c.name, mono: true },
                      { key: "type", label: "Type", render: c => c.dataType },
                      { key: "null", label: "Empty allowed", render: c => c.isNullable ? "yes" : "no" },
                      { key: "len", label: "Longest", render: c => c.maxLength ?? "—", num: true },
                    ]} />
                )}
              </Panel>
            )}

            <Panel title="Imports run" flush>
              <Ledger rows={jobs} keyOf={j => j.id} minWidth={760} status={j => toneOf(j.status)}
                empty={{ title: "Nothing imported yet", detail: "Each import is recorded here with how many rows came through and how many did not." }}
                columns={[
                  { key: "id", label: "Import", render: j => j.id.slice(0, 8), mono: true, nowrap: true },
                  { key: "entity", label: "Into", render: j => j.entityType },
                  { key: "status", label: "Status", render: j => <Mark tone={toneOf(j.status)}>{j.status}</Mark> },
                  { key: "rows", label: "Rows", render: j => j.totalRows, num: true },
                  { key: "ok", label: "Brought in", render: j => j.successRows, num: true },
                  { key: "bad", label: "Rejected", render: j => j.errorRows, num: true },
                  { key: "when", label: "Started", render: j => dateTime(j.startedAt), nowrap: true },
                ]} />
            </Panel>
          </div>

          <aside className="record-margin">
            <Panel title="Connect a database">
              <form className="panel-form" onSubmit={addConnection}>
                <label><span>Name it</span><input value={connection.name} onChange={e => setConnection({ ...connection, name: e.target.value })} placeholder="Legacy Oracle" required /></label>
                <label><span>Kind</span>
                  <select value={connection.dbType} onChange={e => setConnection({ ...connection, dbType: e.target.value })}>
                    {DATABASES.map(d => <option key={d} value={d}>{d}</option>)}
                  </select>
                </label>
                <label><span>Connection string</span><input value={connection.connectionString} onChange={e => setConnection({ ...connection, connectionString: e.target.value })} placeholder="Host=…;Database=…;Username=readonly;Password=…" required /></label>
                <label><span>Schema</span><input value={connection.schema} onChange={e => setConnection({ ...connection, schema: e.target.value })} placeholder="public" /></label>
                <p className="field-hint">Use a read-only account. Nothing here writes to the old system, and a read-only account makes that true rather than merely intended.</p>
                <button type="submit" className={`btn btn-primary${action.busy === "connect" ? " btn-loading" : ""}`} disabled={action.busy !== null}>Save the connection</button>
              </form>
            </Panel>

            {browsing && table && (
              <Panel title="Import this table">
                <form className="panel-form" onSubmit={startImport}>
                  <label><span>Bring the rows in as</span>
                    <select value={entityType} onChange={e => setEntityType(e.target.value)}>
                      {ENTITIES.map(t => <option key={t} value={t}>{t}</option>)}
                    </select>
                  </label>
                  <label><span>Which column becomes which field</span>
                    <textarea value={mappingText} onChange={e => setMappingText(e.target.value)} rows={7} className="mono"
                      placeholder={"student_id → StudentId\nfirst_name → FirstName\nlast_name → LastName\nemail_address → Email"} />
                  </label>
                  <p className="field-hint">One per line, written as <code>source_column → FieldName</code>. A column you do not map is not brought in.</p>
                  <button type="submit" className={`btn btn-primary${action.busy === "import" ? " btn-loading" : ""}`} disabled={action.busy !== null || !mappingText.trim()}>
                    Import from {table}
                  </button>
                </form>
              </Panel>
            )}

            <Panel title="Templates">
              {templates.length === 0 ? (
                <p>No CSV templates are installed with this build.</p>
              ) : (
                <>
                  <p className="field-hint">The shape each kind of record has to be in. Fill one in, save it as UTF-8 CSV, and import it.</p>
                  <ul className="notice-list">
                    {templates.map(name => (
                      <li key={name}><a href={`/api/data-import/formats/${name}`} download>{name.replace(/_/g, " ").replace(/\.csv$/, "")}</a></li>
                    ))}
                  </ul>
                </>
              )}
            </Panel>

            <Notice tone="ink">
              An import is recorded against the person who started it, with the number of rows that
              came through and the number rejected. Rejected rows are not silently dropped — they are
              counted, and the reason is on the import record.
            </Notice>
          </aside>
        </div>
      )}
    </ModulePage>
  );
}
