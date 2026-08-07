"use client";

import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { usePersonalityTranslations } from "@/lib/personality-translator";
import { Database, FileDown, Link2, Play, RefreshCw, Search, Table2, Trash2, Upload, X } from "lucide-react";

type Connection = { id: string; name: string; dbType: string; isActive: boolean; lastTestSuccessful: boolean; lastTestedAt: string | null };
type TableInfo = { name: string };
type ColumnInfo = { name: string; dataType: string; isNullable: boolean; maxLength: number | null };
type ImportJob = { id: string; entityType: string; status: string; totalRows: number; successRows: number; errorRows: number; skippedRows: number; startedAt: string; completedAt: string | null };

const inputS = { padding: "0.5rem 0.75rem", borderRadius: 8, border: "1px solid var(--line)", background: "var(--canvas)", color: "var(--ink)", fontSize: "0.85rem", width: "100%", boxSizing: "border-box" as const };
const btn = (bg: string) => ({ padding: "0.5rem 1rem", borderRadius: 8, border: "none", cursor: "pointer", fontWeight: 600, fontSize: "0.825rem", background: bg, color: bg === "var(--red)" ? "#fff" : "#fff", display: "flex", alignItems: "center", gap: "0.375rem" });

const container = { maxWidth: 1100, margin: "0 auto", padding: "1.5rem" };
const card = { background: "var(--surface)", borderRadius: 12, border: "1px solid var(--line)", padding: "1.5rem", marginBottom: "1.25rem" };
const cardH = { color: "var(--ink)", fontSize: "1.1rem", fontWeight: 600, margin: "0 0 0.75rem 0" };
const grid2 = { display: "grid", gridTemplateColumns: "1fr 1fr", gap: "0.75rem" };
const grid3 = { display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(180px, 1fr))", gap: "0.5rem" };
const badge = (c: string) => ({ display: "inline-block", padding: "0.15rem 0.55rem", borderRadius: 20, fontSize: "0.7rem", fontWeight: 600, background: c === "Completed" ? "var(--green-soft)" : c === "Failed" ? "var(--red-soft)" : c === "Running" ? "var(--blue-light)" : "var(--amber-soft)", color: c === "Completed" ? "var(--green)" : c === "Failed" ? "var(--red)" : c === "Running" ? "var(--blue)" : "var(--amber)" });
const labelS = { fontSize: "0.75rem", color: "var(--muted)", marginBottom: "0.2rem", display: "block" };

interface FormValues {
  name: string; dbType: string; connString: string; schema: string;
  entityType: string; tableName: string; fieldMapText: string;
}

const emptyForm: FormValues = { name: "", dbType: "PostgreSQL", connString: "", schema: "public", entityType: "Student", tableName: "", fieldMapText: "" };

const formatFiles: { name: string; label: string }[] = [
  { name: "students.csv", label: "Students" },
  { name: "faculty.csv", label: "Faculty" },
  { name: "courses.csv", label: "Courses" },
  { name: "student_enrollments.csv", label: "Enrollments" },
  { name: "grades.csv", label: "Grades" },
  { name: "fee_structure.csv", label: "Fee Structure" },
  { name: "fee_payments.csv", label: "Fee Payments" },
  { name: "timetable.csv", label: "Timetable" },
  { name: "attendance.csv", label: "Attendance" },
  { name: "exam_schedule.csv", label: "Exam Schedule" },
  { name: "placement_records.csv", label: "Placement Records" },
  { name: "alumni.csv", label: "Alumni" },
  { name: "transcripts.csv", label: "Transcripts" },
  { name: "library_books.csv", label: "Library Books" },
  { name: "visitors.csv", label: "Visitors" },
];

export default function DataImportPage() {
  const t = usePersonalityTranslations("academic");
  const [connections, setConnections] = useState<Connection[]>([]);
  const [tables, setTables] = useState<TableInfo[]>([]);
  const [columns, setColumns] = useState<ColumnInfo[]>([]);
  const [jobs, setJobs] = useState<ImportJob[]>([]);
  const [form, setForm] = useState<FormValues>(emptyForm);
  const [selectedConn, setSelectedConn] = useState<string | null>(null);
  const [selectedTable, setSelectedTable] = useState<string>("");
  const [msg, setMsg] = useState<{ text: string; ok: boolean } | null>(null);
  const [tab, setTab] = useState<"import" | "formats">("import");
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const ctrl = new AbortController();
    apiGet<Connection[]>("/api/data-import/connections", ctrl.signal).then(setConnections).catch(() => {});
    return () => ctrl.abort();
  }, []);

  useEffect(() => {
    if (!selectedConn) return;
    const ctrl = new AbortController();
    apiGet<{ tables: TableInfo[] }>(`/api/data-import/connections/${selectedConn}/tables`, ctrl.signal)
      .then(r => setTables(r.tables)).catch(() => setTables([]));
    return () => ctrl.abort();
  }, [selectedConn]);

  useEffect(() => {
    if (!selectedConn || !selectedTable) return;
    const ctrl = new AbortController();
    apiGet<{ columns: ColumnInfo[] }>(`/api/data-import/connections/${selectedConn}/tables/${selectedTable}/columns`, ctrl.signal)
      .then(r => setColumns(r.columns)).catch(() => setColumns([]));
    return () => ctrl.abort();
  }, [selectedConn, selectedTable]);

  async function addConnection() {
    setLoading(true);
    try {
      const c = await apiPost<Connection>("/api/data-import/connections", { name: form.name, dbType: form.dbType, connectionString: form.connString, schema: form.schema });
      setConnections(prev => [...prev, c]);
      setMsg({ text: "Connection added", ok: true });
    } catch { setMsg({ text: "Failed to add connection", ok: false }); }
    setLoading(false);
  }

  async function testConnection(id: string) {
    try {
      await apiPost(`/api/data-import/connections/${id}/test`);
      setMsg({ text: "Connection test passed", ok: true });
    } catch { setMsg({ text: "Connection test failed", ok: false }); }
  }

  async function startImport() {
    setLoading(true);
    try {
      const mappings: Record<string,string> = {};
      form.fieldMapText.split("\n").forEach(line => {
        const [src, dest] = line.split("→").map(s => s.trim());
        if (src && dest) mappings[src] = dest;
      });
      const job = await apiPost<ImportJob>("/api/data-import/jobs", {
        connectionId: selectedConn, entityType: form.entityType,
        sourceQuery: form.tableName ? `SELECT * FROM ${form.tableName}` : "",
        fieldMappings: mappings
      });
      setJobs(prev => [job, ...prev]);
      setMsg({ text: `Import started — Job ${job.id.slice(0,8)}`, ok: true });
    } catch { setMsg({ text: "Import failed to start", ok: false }); }
    setLoading(false);
  }

  function setVal(f: keyof FormValues, v: string) { setForm(prev => ({ ...prev, [f]: v })); }

  return (
    <main style={container}>
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: "1.5rem" }}>
        <div>
          <h1 style={{ color: "var(--ink)", fontSize: "1.5rem", fontWeight: 600, margin: 0 }}>Data Import &amp; Migration</h1>
          <p style={{ color: "var(--muted)", fontSize: "0.85rem", margin: "0.25rem 0 0 0" }}>Connect external databases or upload CSV files to migrate into AegisCore</p>
        </div>
      </div>

      <div style={{ display: "flex", gap: "0.5rem", marginBottom: "1.5rem" }}>
        <button onClick={() => setTab("import")} style={{ ...btn(tab === "import" ? "var(--blue)" : "var(--line)"), color: tab === "import" ? "#fff" : "var(--muted)" }}><Database size={16} /> Database Import</button>
        <button onClick={() => setTab("formats")} style={{ ...btn(tab === "formats" ? "var(--blue)" : "var(--line)"), color: tab === "formats" ? "#fff" : "var(--muted)" }}><FileDown size={16} /> Download Templates</button>
      </div>

      {msg && (
        <div style={{ padding: "0.75rem 1rem", borderRadius: 8, marginBottom: "1rem", background: msg.ok ? "var(--green-soft)" : "var(--red-soft)", color: msg.ok ? "var(--green)" : "var(--red)", fontSize: "0.85rem", display: "flex", alignItems: "center", gap: "0.5rem" }}>
          {msg.ok ? "✓" : "✕"} {msg.text}
          <button onClick={() => setMsg(null)} style={{ marginLeft: "auto", background: "none", border: "none", cursor: "pointer", color: "inherit" }}><X size={14} /></button>
        </div>
      )}

      {tab === "import" ? (<>
        <div style={card}>
          <h3 style={cardH}><Link2 size={18} style={{ marginRight: "0.4rem" }} />External Database Connections</h3>
          <div style={grid2}>
            <div><label style={labelS}>Connection Name</label><input style={inputS} value={form.name} onChange={e => setVal("name", e.target.value)} placeholder="e.g. Legacy Oracle DB" /></div>
            <div>
              <label style={labelS}>Database Type</label>
              <select style={inputS} value={form.dbType} onChange={e => setVal("dbType", e.target.value)}>
                <option>PostgreSQL</option><option>MySQL</option><option>MSSQL</option><option>Oracle</option><option>SQLite</option>
              </select>
            </div>
          </div>
          <div style={{ marginTop: "0.75rem" }}>
            <label style={labelS}>Connection String</label>
            <input style={inputS} value={form.connString} onChange={e => setVal("connString", e.target.value)} placeholder="Host=old-db.server.com;Port=5432;Database=legacy_erp;Username=readonly;Password=..." />
          </div>
          <div style={{ marginTop: "0.75rem" }}>
            <label style={labelS}>Schema (optional)</label>
            <input style={inputS} value={form.schema} onChange={e => setVal("schema", e.target.value)} placeholder="public" />
          </div>
          <button onClick={addConnection} disabled={loading} style={{ ...btn("var(--blue)"), marginTop: "0.75rem" }}><Link2 size={14} /> Add Connection</button>
        </div>

        {connections.length > 0 && (
          <div style={card}>
            <h3 style={cardH}>Saved Connections</h3>
            {connections.map(c => (
              <div key={c.id} style={{ padding: "0.75rem", borderRadius: 8, background: "var(--canvas)", marginBottom: "0.5rem", display: "flex", alignItems: "center", gap: "1rem", flexWrap: "wrap" }}>
                <span style={{ fontWeight: 600, color: "var(--ink)", fontSize: "0.9rem" }}>{c.name}</span>
                <span style={{ ...badge(c.dbType === "PostgreSQL" ? "Completed" : "Running"), fontSize: "0.7rem" }}>{c.dbType}</span>
                <button onClick={() => { setSelectedConn(c.id); setSelectedTable(""); }} style={btn("var(--blue)")}><Table2 size={14} /> Browse</button>
                <button onClick={() => testConnection(c.id)} style={btn("var(--green)")}><RefreshCw size={14} /> Test</button>
              </div>
            ))}
          </div>
        )}

        {selectedConn && tables.length > 0 && (
          <div style={card}>
            <h3 style={cardH}><Table2 size={18} style={{ marginRight: "0.4rem" }} />Tables in {connections.find(c => c.id === selectedConn)?.name}</h3>
            <div style={grid3}>
              {tables.map(t => (
                <button key={t.name} onClick={() => setSelectedTable(t.name)} style={{ ...btn(selectedTable === t.name ? "var(--blue)" : "var(--line)"), color: selectedTable === t.name ? "#fff" : "var(--muted)", textAlign: "left" }}>
                  <Table2 size={14} /> {t.name}
                </button>
              ))}
            </div>
          </div>
        )}

        {selectedTable && columns.length > 0 && (
          <div style={card}>
            <h3 style={cardH}>Columns in {selectedTable}</h3>
            <table style={{ width: "100%", borderCollapse: "collapse", fontSize: "0.85rem" }}>
              <thead><tr style={{ borderBottom: "1px solid var(--line)" }}>{["Column Name","Data Type","Nullable","Max Length"].map(h => <th key={h} style={{ textAlign: "left", padding: "0.5rem 0.75rem", color: "var(--muted)", fontWeight: 500, fontSize: "0.75rem" }}>{h}</th>)}</tr></thead>
              <tbody>
                {columns.map(c => <tr key={c.name} style={{ borderBottom: "1px solid var(--gray)" }}><td style={{ padding: "0.4rem 0.75rem", color: "var(--ink)", fontFamily: "monospace", fontSize: "0.82rem" }}>{c.name}</td><td style={{ padding: "0.4rem 0.75rem", color: "var(--muted)", fontSize: "0.82rem" }}>{c.dataType}</td><td style={{ padding: "0.4rem 0.75rem", color: c.isNullable ? "var(--amber)" : "var(--green)", fontSize: "0.82rem" }}>{c.isNullable ? "Yes" : "No"}</td><td style={{ padding: "0.4rem 0.75rem", color: "var(--muted)", fontSize: "0.82rem" }}>{c.maxLength ?? "—"}</td></tr>)}
              </tbody>
            </table>

            <div style={{ marginTop: "1.5rem" }}>
              <h4 style={{ color: "var(--ink)", fontSize: "0.9rem", marginBottom: "0.5rem" }}>Field Mappings</h4>
              <p style={{ color: "var(--muted)", fontSize: "0.8rem", marginBottom: "0.5rem" }}>One per line: <code>source_column → aegiscore_property</code></p>
              <div style={grid2}>
                <div>
                  <label style={labelS}>Entity Type</label>
                  <select style={inputS} value={form.entityType} onChange={e => setVal("entityType", e.target.value)}>
                    <option>Student</option><option>Faculty</option><option>Course</option><option>FeePayment</option><option>Grade</option><option>Attendance</option>
                  </select>
                </div>
                <div></div>
              </div>
              <textarea style={{ ...inputS, marginTop: "0.75rem", height: 120, resize: "vertical", fontFamily: "monospace" }} value={form.fieldMapText} onChange={e => setVal("fieldMapText", e.target.value)} placeholder={"student_id → StudentId\nfirst_name → FirstName\nlast_name → LastName\nemail_address → Email"} />
              <button onClick={startImport} disabled={loading} style={{ ...btn("var(--green)"), marginTop: "0.75rem" }}><Play size={14} /> Start Import</button>
            </div>
          </div>
        )}

        {jobs.length > 0 && (
          <div style={card}>
            <h3 style={cardH}>Import History</h3>
            <table style={{ width: "100%", borderCollapse: "collapse", fontSize: "0.85rem" }}>
              <thead><tr style={{ borderBottom: "1px solid var(--line)" }}>{["Job","Entity","Status","Total","Success","Errors","Started"].map(h => <th key={h} style={{ textAlign: "left", padding: "0.5rem 0.75rem", color: "var(--muted)", fontWeight: 500, fontSize: "0.75rem" }}>{h}</th>)}</tr></thead>
              <tbody>
                {jobs.map(j => <tr key={j.id} style={{ borderBottom: "1px solid var(--gray)" }}>
                  <td style={{ padding: "0.4rem 0.75rem", fontFamily: "monospace", fontSize: "0.78rem", color: "var(--blue)" }}>{j.id.slice(0,8)}</td>
                  <td style={{ padding: "0.4rem 0.75rem", color: "var(--ink)" }}>{j.entityType}</td>
                  <td style={{ padding: "0.4rem 0.75rem" }}><span style={badge(j.status)}>{j.status}</span></td>
                  <td style={{ padding: "0.4rem 0.75rem", color: "var(--ink)" }}>{j.totalRows}</td>
                  <td style={{ padding: "0.4rem 0.75rem", color: "var(--green)" }}>{j.successRows}</td>
                  <td style={{ padding: "0.4rem 0.75rem", color: "var(--red)" }}>{j.errorRows}</td>
                  <td style={{ padding: "0.4rem 0.75rem", color: "var(--muted)", fontSize: "0.78rem" }}>{new Date(j.startedAt).toLocaleDateString()}</td>
                </tr>)}
              </tbody>
            </table>
          </div>
        )}
      </>) : (
        <div style={card}>
          <h3 style={cardH}><FileDown size={18} style={{ marginRight: "0.4rem" }} />Download Format Templates</h3>
          <p style={{ color: "var(--muted)", fontSize: "0.85rem", marginBottom: "1rem" }}>Download these CSV templates to see the exact format required for each type of data upload. Fill in your data, save as CSV (UTF-8), then upload through the Data Import page.</p>
          
          <div style={grid2}>
            {formatFiles.map(f => (
              <a key={f.name} href={`/api/data-import/formats/${f.name}`} download
                 style={{
                   display: "flex", alignItems: "center", gap: "0.5rem", padding: "0.75rem 1rem",
                   borderRadius: 8, border: "1px solid var(--line)", background: "var(--canvas)",
                   color: "var(--ink)", textDecoration: "none", fontWeight: 500, fontSize: "0.85rem",
                   transition: "background 0.15s"
                 }}
                 onMouseOver={e => (e.currentTarget.style.background = "var(--blue-light)")}
                 onMouseOut={e => (e.currentTarget.style.background = "var(--canvas)")}
              >
                <FileDown size={16} style={{ color: "var(--blue)" }} /> {f.label} <span style={{ fontSize: "0.7rem", color: "var(--muted)", marginLeft: "auto" }}>.csv</span>
              </a>
            ))}
          </div>
        </div>
      )}
    </main>
  );
}
