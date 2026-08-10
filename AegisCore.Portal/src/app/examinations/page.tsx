"use client";

import { useEffect, useRef, useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiDownload, apiGet, apiPost, apiPut } from "@/lib/api-client";
import { AlertTriangle, CheckCircle2, ClipboardCheck, Download, FileSpreadsheet, FileText, FileUp, Keyboard, Loader2, ShieldCheck } from "lucide-react";

type Course = { id: string; title: string; code: string };
type Component = { id: string; name: string; totalMarks: number; weightPercentage: number; passingMarks: number | null; isMandatory: boolean };
type Student = { enrollmentId: string; studentId: string; name: string; universityId: string; marks: Record<string, number> };
type Snapshot = { scheme: { id: string; semesterId: string; components: Component[] }; students: Student[] };
type PreviewRow = { studentId: string; universityId: string; assessmentComponentId: string; obtainedMarks: number; error?: string | null };
type ImportPreview = { source: string; rows: PreviewRow[]; warnings: string[] };
type MarkResponse = { status: "saved" | "approval_required"; moderationId?: string; message: string };
type Mode = "manual" | "excel" | "pdf";

export default function GradeEntry() {
  const [courses, setCourses] = useState<Course[]>([]);
  const [selectedCourse, setSelectedCourse] = useState("");
  const [snapshot, setSnapshot] = useState<Snapshot | null>(null);
  const [marks, setMarks] = useState<Record<string, Record<string, number>>>({});
  const [mode, setMode] = useState<Mode>("manual");
  const [preview, setPreview] = useState<ImportPreview | null>(null);
  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const fileRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    apiGet<Course[]>("/api/courses/my").then(setCourses).catch(e => setError(e instanceof Error ? e.message : "Unable to load courses.")).finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!selectedCourse) { setSnapshot(null); setPreview(null); return; }
    setLoading(true); setError(""); setMessage(""); setPreview(null);
    apiGet<Snapshot>(`/api/faculty-grade-entry/course/${selectedCourse}`)
      .then(data => {
        setSnapshot(data);
        const initial: Record<string, Record<string, number>> = {};
        for (const student of data.students) {
          initial[student.studentId] = {};
          for (const component of data.scheme.components) {
            initial[student.studentId][component.id] = Number(student.marks?.[component.id] ?? 0);
          }
        }
        setMarks(initial);
      })
      .catch(e => setError(e instanceof Error ? e.message : "Unable to load grade sheet."))
      .finally(() => setLoading(false));
  }, [selectedCourse]);

  const setMark = (studentId: string, componentId: string, value: number) => {
    const max = snapshot?.scheme.components.find(c => c.id === componentId)?.totalMarks ?? 100;
    setMarks(prev => ({ ...prev, [studentId]: { ...prev[studentId], [componentId]: Math.max(0, Math.min(Number.isFinite(value) ? value : 0, max)) } }));
  };

  const submitManual = async () => {
    if (!selectedCourse || !snapshot) return;
    setSaving(true); setError(""); setMessage("");
    let saved = 0; let approval = 0;
    try {
      for (const student of snapshot.students) {
        for (const component of snapshot.scheme.components) {
          const requested = marks[student.studentId]?.[component.id] ?? 0;
          const original = Number(student.marks?.[component.id] ?? 0);
          if (requested === original) continue;
          const result = await apiPut<MarkResponse>("/api/faculty-grade-entry/marks", {
            courseId: selectedCourse,
            studentId: student.studentId,
            assessmentComponentId: component.id,
            obtainedMarks: requested,
          });
          if (result.status === "approval_required") approval++; else saved++;
        }
      }
      setMessage(`${saved} mark change${saved === 1 ? "" : "s"} saved${approval ? ` · ${approval} sensitive change${approval === 1 ? "" : "s"} sent for HOD/Exam approval` : ""}.`);
      const fresh = await apiGet<Snapshot>(`/api/faculty-grade-entry/course/${selectedCourse}`);
      setSnapshot(fresh);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to submit marks.");
    } finally { setSaving(false); }
  };

  const previewFile = async (file: File) => {
    if (!selectedCourse) return;
    setSaving(true); setError(""); setMessage(""); setPreview(null);
    try {
      const form = new FormData(); form.append("file", file);
      const result = await apiPost<ImportPreview>(`/api/faculty-grade-entry/course/${selectedCourse}/import-preview`, form);
      setPreview(result);
      const good = result.rows.filter(r => !r.error).length;
      setMessage(`${file.name}: ${good} valid mark entr${good === 1 ? "y" : "ies"} detected. Review before committing.`);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Unable to read grade sheet.");
    } finally {
      setSaving(false);
      if (fileRef.current) fileRef.current.value = "";
    }
  };

  const commitImport = async () => {
    if (!selectedCourse || !preview) return;
    const entries = preview.rows.filter(r => !r.error && r.studentId && r.assessmentComponentId).map(r => ({ studentId: r.studentId, assessmentComponentId: r.assessmentComponentId, obtainedMarks: r.obtainedMarks }));
    setSaving(true); setError(""); setMessage("");
    try {
      const result = await apiPost<{saved:number;approvalRequired:number;errors:string[]}>("/api/faculty-grade-entry/import-commit", { courseId: selectedCourse, entries });
      setMessage(`${result.saved} imported · ${result.approvalRequired} routed for approval${result.errors.length ? ` · ${result.errors.length} error(s)` : ""}.`);
      if (result.errors.length) setError(result.errors.join(" "));
      setPreview(null);
    } catch (e) { setError(e instanceof Error ? e.message : "Import failed."); }
    finally { setSaving(false); }
  };

  const downloadTemplate = async () => {
    if (!selectedCourse) return;
    try { await apiDownload(`/api/faculty-grade-entry/course/${selectedCourse}/template.csv`, undefined, "grade-sheet.csv"); }
    catch (e) { setError(e instanceof Error ? e.message : "Unable to download template."); }
  };

  const accept = mode === "pdf" ? "application/pdf" : ".xlsx,.csv,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,text/csv";

  return (
    <AppShell>
      <div className="content-page">
        <div className="page-header">
          <div>
            <div className="page-header-breadcrumb"><span>Academic</span><span>/</span><span>Grade Submission</span></div>
            <h1>Grade Submission</h1>
            <p>Enter marks manually, upload an Excel grade sheet, or extract a PDF grade sheet.</p>
          </div>
        </div>

        {error && <div className="error-state" style={{ marginBottom: 14 }}><AlertTriangle size={18}/><p>{error}</p></div>}
        {message && <div style={{ marginBottom: 14, padding: "11px 13px", border: "1px solid var(--success)", borderRadius: 10, color: "var(--success)", background: "var(--success-soft)", display: "flex", gap: 8, alignItems: "center" }}><CheckCircle2 size={17}/>{message}</div>}

        <div className="card" style={{ marginBottom: 16 }}><div className="card-body">
          {loading && courses.length === 0 ? <div className="skeleton" style={{ height: 40 }} /> : (
            <select value={selectedCourse} onChange={e => setSelectedCourse(e.target.value)} style={{ width: "100%", minHeight: 44, borderRadius: 10, border: "1px solid var(--border)", padding: "0 12px", fontSize: 14, background: "var(--surface)", color: "var(--ink)" }}>
              <option value="">Select a subject...</option>
              {courses.map(c => <option key={c.id} value={c.id}>{c.code} — {c.title}</option>)}
            </select>
          )}
        </div></div>

        {selectedCourse && snapshot && (
          <>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(3,minmax(0,1fr))", gap: 10, marginBottom: 16 }}>
              <ModeButton active={mode === "manual"} icon={<Keyboard size={19}/>} title="Manual Entry" text="Type marks directly" onClick={() => { setMode("manual"); setPreview(null); }} />
              <ModeButton active={mode === "excel"} icon={<FileSpreadsheet size={19}/>} title="Excel Sheet" text="Upload .xlsx or .csv" onClick={() => { setMode("excel"); setPreview(null); }} />
              <ModeButton active={mode === "pdf"} icon={<FileText size={19}/>} title="PDF Sheet" text="OCR and review first" onClick={() => { setMode("pdf"); setPreview(null); }} />
            </div>

            <div style={{ display: "flex", alignItems: "flex-start", gap: 9, marginBottom: 16, padding: 12, borderRadius: 10, border: "1px solid var(--border)", background: "var(--surface-2)", color: "var(--muted)", fontSize: 12 }}>
              <ShieldCheck size={18} style={{ flex: "0 0 auto" }}/>
              <span><strong style={{ color: "var(--ink)" }}>Sensitive-change protection:</strong> first-time marks save normally. Editing an existing mark by 5 or more marks, or moving a student across that assessment&apos;s pass/fail threshold, is not applied immediately for Faculty; it is routed to the relevant department HOD / Exam Controller for approval.</span>
            </div>

            {mode === "manual" ? (
              <ManualSheet snapshot={snapshot} marks={marks} setMark={setMark} saving={saving} submit={submitManual} />
            ) : (
              <div className="card">
                <div className="card-header"><strong>{mode === "excel" ? "Excel Grade Sheet" : "PDF Grade Sheet"}</strong></div>
                <div className="card-body">
                  <p style={{ margin: "0 0 12px", color: "var(--muted)", fontSize: 13 }}>
                    {mode === "excel" ? "Upload an .xlsx workbook or CSV. The first row must contain UniversityId, StudentName, then assessment component names exactly as shown in the scheme." : "Upload a text-based or scanned PDF. AegisCore runs the configured OCR provider, converts the extracted table into a preview, and never commits marks until you confirm."}
                  </p>
                  {mode === "excel" && <button className="btn btn-secondary" onClick={() => void downloadTemplate()} style={{ marginBottom: 10 }}><Download size={16}/> Download Excel-compatible template</button>}
                  <input ref={fileRef} type="file" accept={accept} style={{ display: "none" }} onChange={e => { const file = e.target.files?.[0]; if (file) void previewFile(file); }} />
                  <button className="btn btn-primary" onClick={() => fileRef.current?.click()} disabled={saving}><FileUp size={16}/>{saving ? " Reading…" : ` Choose ${mode === "pdf" ? "PDF" : "Excel/CSV"}`}</button>

                  {preview && <ImportPreviewTable preview={preview} components={snapshot.scheme.components} onCommit={commitImport} saving={saving} />}
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </AppShell>
  );
}

function ModeButton({active,icon,title,text,onClick}:{active:boolean;icon:React.ReactNode;title:string;text:string;onClick:()=>void}) {
  return <button onClick={onClick} style={{ textAlign:"left", padding:14, borderRadius:12, border: active ? "2px solid var(--primary)" : "1px solid var(--border)", background: active ? "var(--primary-soft)" : "var(--surface)", color:"var(--ink)", cursor:"pointer", minHeight:84 }}><div style={{ display:"flex", gap:8, alignItems:"center", fontWeight:800 }}>{icon}{title}</div><div style={{ color:"var(--muted)", fontSize:12, marginTop:6 }}>{text}</div></button>;
}

function ManualSheet({snapshot,marks,setMark,saving,submit}:{snapshot:Snapshot;marks:Record<string,Record<string,number>>;setMark:(s:string,c:string,v:number)=>void;saving:boolean;submit:()=>void}) {
  return <>
    <div className="card" style={{ marginBottom: 12 }}><div className="card-header"><strong>Assessment Breakdown</strong></div><div className="card-body" style={{ display:"flex",gap:10,flexWrap:"wrap" }}>{snapshot.scheme.components.map(c => <div key={c.id} className="badge badge-primary" style={{padding:"8px 12px"}}>{c.name}: {c.totalMarks} ({c.weightPercentage}%) · pass {c.passingMarks ?? Math.ceil(c.totalMarks/2)}</div>)}</div></div>
    <div className="table-wrap"><table><thead><tr><th>Student</th><th>ID</th>{snapshot.scheme.components.map(c=><th key={c.id}>{c.name}</th>)}</tr></thead><tbody>{snapshot.students.map(s=><tr key={s.studentId}><td style={{fontWeight:600}}>{s.name}</td><td style={{color:"var(--muted)",fontSize:12}}>{s.universityId}</td>{snapshot.scheme.components.map(c=><td key={c.id}><input type="number" min={0} max={c.totalMarks} value={marks[s.studentId]?.[c.id] ?? 0} onChange={e=>setMark(s.studentId,c.id,Number(e.target.value))} style={{width:74,textAlign:"center",minHeight:36,borderRadius:6,border:"1px solid var(--border)",padding:"2px 8px",fontSize:13,background:"var(--surface)",color:"var(--ink)"}}/><small style={{display:"block",color:"var(--muted)",fontSize:10}}>/ {c.totalMarks}</small></td>)}</tr>)}</tbody></table></div>
    <button className="btn btn-primary btn-lg" style={{width:"100%",marginTop:16}} onClick={submit} disabled={saving}>{saving ? <><Loader2 size={16} className="animate-spin"/> Submitting…</> : <><ClipboardCheck size={16}/> Submit Grade Changes</>}</button>
  </>;
}

function ImportPreviewTable({preview,components,onCommit,saving}:{preview:ImportPreview;components:Component[];onCommit:()=>void;saving:boolean}) {
  const valid = preview.rows.filter(r=>!r.error);
  const name = (id:string)=>components.find(c=>c.id===id)?.name ?? "Unknown";
  return <div style={{marginTop:18}}><div style={{fontWeight:800,marginBottom:8}}>Import preview</div><div className="table-wrap"><table><thead><tr><th>Student ID</th><th>Component</th><th>Mark</th><th>Status</th></tr></thead><tbody>{preview.rows.slice(0,250).map((r,i)=><tr key={`${r.studentId}-${r.assessmentComponentId}-${i}`}><td>{r.universityId || "—"}</td><td>{r.assessmentComponentId ? name(r.assessmentComponentId) : "—"}</td><td>{r.error ? "—" : r.obtainedMarks}</td><td>{r.error ? <span className="badge badge-danger">{r.error}</span> : <span className="badge badge-success">Ready</span>}</td></tr>)}</tbody></table></div><button className="btn btn-primary" style={{width:"100%",marginTop:14}} disabled={saving || valid.length===0} onClick={onCommit}>{saving ? <><Loader2 size={16} className="animate-spin"/> Committing…</> : <>Commit {valid.length} validated entries</>}</button></div>;
}
