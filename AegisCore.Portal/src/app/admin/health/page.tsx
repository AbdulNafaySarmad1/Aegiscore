"use client";

import { useMemo, useState } from "react";
import { apiGet, ApiError } from "@/lib/api-client";
import { Panel, Figures, Mark, Notice } from "@/components/register";
import { Ledger, toneOf, words } from "@/components/register/ledger";
import { ModulePage, useModuleData } from "@/components/register/module-page";

type Infrastructure = { name: string; status: string; detail: string };
type Module = { module: string; status: string; problems: string[]; dependencies: number; tables: number };
type Report = { status: string; summary: string; infrastructure: Infrastructure[]; modules: Module[] };
type Schema = { status: string; summary: string; pendingMigrations: string[]; missingTables: string[]; missingColumns: string[]; mistypedColumns: string[]; modelTables: number; modelColumns: number };
type Everything = { report: Report; schema: Schema | null; readAt: Date };

/**
 * The report answers 503 when something it depends on is down, with the same body it would have
 * sent for 200; the body is the point, so it is read whichever way the status went.
 */
async function readReport<T>(path: string, signal: AbortSignal): Promise<T> {
  try {
    return await apiGet<T>(path, signal);
  } catch (error) {
    if (error instanceof ApiError && error.status === 503 && error.payload && typeof error.payload === "object") return error.payload as T;
    throw error;
  }
}

const infrastructureNames: Record<string, string> = {
  database: "Database",
  redis: "Cache (Redis)",
  smtp: "Outgoing mail",
  storage: "Document storage",
  "license-authority": "Licence authority",
};

/** What the register depends on, whether each part is actually answering, and every module's wiring. */
export default function HealthPage() {
  const { state, reload } = useModuleData<Everything>(async signal => {
    const [report, schema] = await Promise.all([
      readReport<Report>("/health/modules", signal),
      readReport<Schema>("/health/schema", signal).catch(() => null),
    ]);
    return { report, schema, readAt: new Date() };
  });
  const [showHealthy, setShowHealthy] = useState(false);

  const tone = (status: string) => status === "healthy" || status === "current" ? "laurel" : status === "degraded" ? "brass" : "oxblood";
  const describe = (status: string) =>
    status === "healthy" ? "Everything answers" : status === "degraded" ? "Working, with something unconfigured" : "Something it needs is not answering";

  return (
    <ModulePage
      title="System health"
      crumbs={[{ label: "Operations" }, { label: "System health" }]}
      meta={<span>Proved, not assumed: the database is compared with the code, storage is written and read back, the cache is pinged, and every module&rsquo;s tables and services are checked.</span>}
      ident={(d: Everything) => <><strong>{words(d.report.status)}</strong><br />as of {d.readAt.toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit" })}</>}
      state={state} reload={reload}
    >
      {({ report, schema }) => <HealthBody report={report} schema={schema} tone={tone} describe={describe} showHealthy={showHealthy} setShowHealthy={setShowHealthy} reload={reload} />}
    </ModulePage>
  );
}

function HealthBody({ report, schema, tone, describe, showHealthy, setShowHealthy, reload }: {
  report: Report; schema: Schema | null;
  tone: (s: string) => "laurel" | "brass" | "oxblood"; describe: (s: string) => string;
  showHealthy: boolean; setShowHealthy: (v: boolean) => void; reload: () => void;
}) {
  const unwell = useMemo(() => report.modules.filter(m => m.status !== "healthy"), [report]);
  const listed = showHealthy ? report.modules : unwell;
  const down = report.infrastructure.filter(i => i.status === "unavailable");
  const degraded = report.infrastructure.filter(i => i.status === "degraded");

  return (
    <div className="record">
      <div className="record-body">
        <Figures aria-label="Health figures" items={[
          { label: "Overall", value: words(report.status), tone: tone(report.status), note: describe(report.status) },
          { label: "Modules", value: `${report.modules.length - unwell.length} of ${report.modules.length}`, tone: unwell.length === 0 ? "laurel" : "oxblood", note: unwell.length === 0 ? "wired and answering" : `${unwell.length} with a problem` },
          { label: "Schema", value: schema ? words(schema.status) : "Not read", tone: schema ? tone(schema.status) : "ink", note: schema ? `${schema.modelTables} tables, ${schema.modelColumns} columns` : "the schema check did not answer" },
          { label: "Dependencies", value: `${report.infrastructure.length - down.length} of ${report.infrastructure.length}`, tone: down.length > 0 ? "oxblood" : degraded.length > 0 ? "brass" : "laurel", note: down.length > 0 ? `${down.length} not answering` : degraded.length > 0 ? `${degraded.length} not configured` : "all answering" },
        ]} />

        {report.status !== "healthy" && (
          <Notice tone={tone(report.status)} title={report.status === "unhealthy" ? "Something the register needs is not answering." : "The register is working, with something still to configure."} role="alert">
            {report.summary || "The lines below say which."}
          </Notice>
        )}

        <Panel title="What it depends on" flush>
          <Ledger rows={report.infrastructure} keyOf={i => i.name} minWidth={640} status={i => tone(i.status)}
            empty={{ title: "Nothing was checked" }}
            columns={[
              { key: "name", label: "Dependency", render: i => infrastructureNames[i.name] ?? words(i.name), nowrap: true },
              { key: "status", label: "Status", render: i => <Mark tone={tone(i.status)}>{i.status === "unavailable" ? "Not answering" : i.status === "degraded" ? "Not configured" : "Answering"}</Mark>, nowrap: true },
              { key: "detail", label: "What was found", render: i => i.detail },
            ]} />
        </Panel>

        {schema && schema.status !== "current" && (
          <Panel tone="oxblood" title="The database does not match the code">
            <>
              <p>{schema.summary}</p>
              {schema.pendingMigrations.length > 0 && <p><strong>Pending migrations:</strong> {schema.pendingMigrations.join(", ")}</p>}
              {schema.missingTables.length > 0 && <p><strong>Missing tables:</strong> {schema.missingTables.join(", ")}</p>}
              {schema.missingColumns.length > 0 && <p><strong>Missing columns:</strong> {schema.missingColumns.join(", ")}</p>}
              {schema.mistypedColumns.length > 0 && <p><strong>Columns of the wrong type:</strong> {schema.mistypedColumns.join(", ")}</p>}
            </>
          </Panel>
        )}

        <Panel title={showHealthy ? "Every module" : "Modules with a problem"} flush
          aside={<button type="button" className="btn btn-ghost btn-sm" onClick={() => setShowHealthy(!showHealthy)}>{showHealthy ? "Show only problems" : `Show all ${report.modules.length}`}</button>}>
          <Ledger rows={listed} keyOf={m => m.module} minWidth={720} status={m => toneOf(m.status)}
            empty={{ title: "Every module is wired and its tables exist", detail: `${report.modules.length} modules were checked: each one's services resolve and each table it reads is present.` }}
            columns={[
              { key: "module", label: "Module", render: m => words(m.module), nowrap: true },
              { key: "status", label: "Status", render: m => <Mark tone={toneOf(m.status)}>{m.status === "healthy" ? "Wired" : words(m.status)}</Mark>, nowrap: true },
              { key: "wiring", label: "Wiring", render: m => `${m.dependencies} service${m.dependencies === 1 ? "" : "s"}, ${m.tables} table${m.tables === 1 ? "" : "s"}`, nowrap: true },
              { key: "problems", label: "Problems", render: m => m.problems.length === 0 ? "none" : m.problems.join(" ") },
            ]} />
        </Panel>
      </div>

      <aside className="record-margin">
        <Panel title="Read again">
          <div className="panel-form">
            <p className="field-hint">The report is cached for ten seconds, so a monitor may poll it as often as it likes.</p>
            <button type="button" className="btn btn-secondary" onClick={reload}>Check now</button>
          </div>
        </Panel>
        <Panel title="From the command line">
          <p className="field-hint">The same report, for a script or a monitor, signed in with the browser&rsquo;s cookie jar:</p>
          <pre className="code-block">{`curl -b jar.txt ${typeof window !== "undefined" ? window.location.origin : ""}/health/modules`}</pre>
          <p className="field-hint">It answers 503 whenever anything is not answering, with the same body.</p>
        </Panel>
      </aside>
    </div>
  );
}
