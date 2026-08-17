"use client";

import { useEffect, useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet, apiPost } from "@/lib/api-client";
import { CheckCircle2, Loader2, RefreshCw, ShieldCheck, XCircle } from "lucide-react";

type RequestRow = {
  id: string;
  courseId: string;
  course: { code: string; title: string };
  student: { id: string; universityId: string; name: string };
  component: { id: string; name: string; totalMarks: number; passingMarks: number | null };
  previousMark: number;
  requestedMark: number;
  delta: number;
  crossesPassBoundary: boolean;
  passMark: number;
  submittedBy: { id: string; name: string; email?: string | null };
  submittedAt?: string | null;
  reason: string;
};

export default function GradeApprovalsPage() {
  const [rows, setRows] = useState<RequestRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [working, setWorking] = useState<string | null>(null);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  const load = async () => {
    setLoading(true); setError("");
    try { setRows(await apiGet<RequestRow[]>("/api/grade-approvals")); }
    catch (e) { setError(e instanceof Error ? e.message : "Unable to load approval queue."); }
    finally { setLoading(false); }
  };

  useEffect(() => { void load(); }, []);

  const review = async (row: RequestRow, decision: "approved" | "rejected") => {
    const comments = window.prompt(decision === "approved" ? "Approval comment (optional):" : "Reason for rejection:", "") ?? "";
    if (decision === "rejected" && !comments.trim()) return;
    setWorking(row.id); setError(""); setMessage("");
    try {
      await apiPost(`/api/faculty-grade-entry/approvals/${row.id}/review`, { decision, comments });
      setMessage(decision === "approved" ? `Approved ${row.student.universityId}. The requested mark has been applied.` : `Rejected ${row.student.universityId}. The existing mark remains unchanged.`);
      await load();
    } catch (e) { setError(e instanceof Error ? e.message : "Review failed."); }
    finally { setWorking(null); }
  };

  return (
    <AppShell>
      <div className="content-page">
        <div className="page-header">
          <div>
            <div className="page-header-breadcrumb"><span>Academic</span><span>/</span><span>Grade Approvals</span></div>
            <h1>Grade Change Approvals</h1>
            <p>Department-head review for material mark changes and pass/fail boundary changes.</p>
          </div>
          <button className="btn btn-secondary" onClick={() => void load()} disabled={loading}><RefreshCw size={16}/> Refresh</button>
        </div>

        <div style={{ display:"flex",gap:9,alignItems:"flex-start",padding:12,border:"1px solid var(--border)",borderRadius:10,background:"var(--surface-2)",marginBottom:16,color:"var(--muted)",fontSize:12 }}>
          <ShieldCheck size={18}/><span>HOD users only see requests from courses in their own department. Exam Controller/Admin roles can review across departments.</span>
        </div>
        {error && <div className="error-state" style={{marginBottom:14}}><p>{error}</p></div>}
        {message && <div style={{marginBottom:14,padding:12,border:"1px solid var(--success)",borderRadius:10,background:"var(--success-soft)",color:"var(--success)"}}>{message}</div>}

        {loading ? <div className="card"><div className="card-body"><Loader2 size={18} className="animate-spin"/> Loading approvals…</div></div> : rows.length === 0 ? (
          <div className="card"><div className="card-body" style={{textAlign:"center",padding:36}}><CheckCircle2 size={32} style={{margin:"0 auto 10px",color:"var(--success)"}}/><strong>Approval queue is clear.</strong><p style={{color:"var(--muted)",fontSize:13}}>No sensitive grade changes are waiting for review.</p></div></div>
        ) : (
          <div style={{display:"grid",gap:12}}>{rows.map(row => (
            <div key={row.id} className="card"><div className="card-body">
              <div style={{display:"flex",justifyContent:"space-between",gap:16,alignItems:"flex-start",flexWrap:"wrap"}}>
                <div>
                  <div style={{fontWeight:850,fontSize:16}}>{row.course.code} — {row.course.title}</div>
                  <div style={{marginTop:5,fontSize:13}}><strong>{row.student.name}</strong> · {row.student.universityId}</div>
                  <div style={{marginTop:5,color:"var(--muted)",fontSize:12}}>{row.component.name} · submitted by {row.submittedBy.name}{row.submittedAt ? ` · ${new Date(row.submittedAt).toLocaleString()}` : ""}</div>
                </div>
                <span className={row.crossesPassBoundary ? "badge badge-danger" : "badge badge-warning"}>{row.reason}</span>
              </div>
              <div style={{display:"grid",gridTemplateColumns:"repeat(4,minmax(100px,1fr))",gap:10,marginTop:16}}>
                <Metric label="Current" value={`${row.previousMark}/${row.component.totalMarks}`}/>
                <Metric label="Requested" value={`${row.requestedMark}/${row.component.totalMarks}`}/>
                <Metric label="Change" value={`${row.requestedMark > row.previousMark ? "+" : ""}${row.requestedMark-row.previousMark}`}/>
                <Metric label="Pass mark" value={row.passMark}/>
              </div>
              <div style={{display:"flex",gap:10,marginTop:16}}>
                <button className="btn btn-primary" disabled={working===row.id} onClick={() => void review(row,"approved")}><CheckCircle2 size={16}/>{working===row.id ? " Working…" : " Approve & Apply"}</button>
                <button className="btn btn-secondary" disabled={working===row.id} onClick={() => void review(row,"rejected")}><XCircle size={16}/> Reject</button>
              </div>
            </div></div>
          ))}</div>
        )}
      </div>
    </AppShell>
  );
}

function Metric({label,value}:{label:string;value:React.ReactNode}) {
  return <div style={{border:"1px solid var(--border)",borderRadius:9,padding:10}}><div style={{color:"var(--muted)",fontSize:10,textTransform:"uppercase",fontWeight:800}}>{label}</div><div style={{fontSize:18,fontWeight:850,marginTop:4}}>{value}</div></div>;
}
