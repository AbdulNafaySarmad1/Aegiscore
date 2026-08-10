"use client";

import { useEffect, useState } from "react";
import { motion } from "framer-motion";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet, apiPost } from "@/lib/api-client";
import { FileText, Download, CheckCircle } from "lucide-react";

type Semester = { semesterIndex: number; courses: { courseTitle: string; letterGrade: string; gradePoints: number }[]; sgpa: number };

type OfficialTranscript = {
  hash: string; downloadUrl: string; verificationUrl: string;
  algorithm: string; keyId: string; signature: string; publicKey: string;
};

export default function TranscriptPage() {
  const [semesters, setSemesters] = useState<Semester[]>([]);
  const [cgpa, setCgpa] = useState<number>(0);
  const [studentName, setStudentName] = useState("");
  const [loading, setLoading] = useState(true);
  const [official, setOfficial] = useState<OfficialTranscript | null>(null);
  const [requesting, setRequesting] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    apiGet<{ semesters: Semester[]; cgpa: number; studentName: string }>("/api/student-portal/transcript")
      .then(d => { setSemesters(d.semesters ?? []); setCgpa(d.cgpa); setStudentName(d.studentName); })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const requestOfficial = async () => {
    setRequesting(true); setError("");
    try {
      const res = await apiPost<OfficialTranscript>("/api/student-portal/transcript/official");
      setOfficial(res);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to generate official transcript.");
    } finally { setRequesting(false); }
  };

  const downloadOfficialPdf = () => {
    if (!official?.downloadUrl) return;

    // downloadUrl is an API route. The portal proxies /api requests to
    // the backend, preserving the authenticated student session.
    const a = document.createElement("a");
    a.href = official.downloadUrl;
    a.download = `AegisCore-Official-Transcript-${official.hash.slice(0, 12)}.pdf`;
    document.body.appendChild(a);
    a.click();
    a.remove();
  };


  return (
    <AppShell>
      <div className="content-page">
        <div className="page-header">
          <div>
            <div className="page-header-breadcrumb"><span>Student</span><span>/</span><span>Transcript</span></div>
            <h1>Transcript</h1>
            <p>Official academic transcript — CGPA: {cgpa.toFixed(2)}</p>
          </div>
          <button className="btn btn-primary" onClick={requestOfficial} disabled={requesting || official !== null}>
            {official ? <><CheckCircle size={16} />Generated</> : requesting ? "Generating..." : <><Download size={16} />Request Official Copy</>}
          </button>
        </div>

        {error && <div className="error-state" style={{marginBottom:12}}><p>{error}</p></div>}

        {official && (
          <div className="card" style={{marginBottom:16,border:"1px solid var(--primary)"}}>
            <div className="card-header"><h3 style={{fontSize:14}}>Official Transcript Generated</h3></div>
            <div className="card-body">
              <div style={{display:"grid",gap:8,fontSize:13}}>
                <p><strong>Hash:</strong> {official.hash}</p>
                <p><strong>Algorithm:</strong> {official.algorithm}</p>
                <p><strong>Key ID:</strong> {official.keyId}</p>
                <button
                  type="button"
                  className="btn btn-primary"
                  onClick={downloadOfficialPdf}
                  style={{ marginBottom: 12 }}
                >
                  <Download size={16} />
                  Download Official PDF
                </button>
                <p><strong>Verify at:</strong> <code>{official.verificationUrl}</code></p>
                <div style={{marginTop:8,padding:8,background:"var(--surface-muted)",borderRadius:8,overflow:"auto",maxHeight:80}}>
                  <small style={{fontFamily:"monospace",fontSize:10,wordBreak:"break-all"}}>
                    SIG: {official.signature?.slice(0,64)}...
                  </small>
                </div>
              </div>
            </div>
          </div>
        )}

        {loading ? <div className="skeleton" style={{height:120}} /> :
         semesters.length === 0 ? <p style={{color:"var(--muted)"}}>No grades published yet.</p> :
         semesters.map((sem, idx) => (
          <motion.div key={idx} className="card" style={{ marginBottom: 16 }} initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: idx * 0.08 }}>
            <div className="card-header">
              <h3 style={{ fontSize: 14 }}>Semester {sem.semesterIndex}</h3>
              {sem.sgpa > 0 && <span className="badge badge-success">SGPA: {sem.sgpa.toFixed(2)}</span>}
            </div>
            <div className="card-body" style={{ padding: 0 }}>
              <table style={{ width: "100%", borderCollapse: "collapse" }}>
                <thead>
                  <tr style={{ background: "var(--surface-muted)" }}>
                    <th style={{ padding: "10px 16px", textAlign: "left", fontSize: 11, color: "var(--muted)" }}>Course</th>
                    <th style={{ padding: "10px 16px", textAlign: "center", fontSize: 11, color: "var(--muted)" }}>Grade</th>
                  </tr>
                </thead>
                <tbody>
                  {sem.courses.map((g, i) => (
                    <tr key={i} style={{ borderBottom: i < sem.courses.length - 1 ? "1px solid var(--border)" : "none" }}>
                      <td style={{ padding: "12px 16px", fontSize: 13 }}>{g.courseTitle}</td>
                      <td style={{ padding: "12px 16px", textAlign: "center" }}>
                        <span className={`badge ${g.letterGrade === "A" || g.letterGrade === "A-" ? "badge-success" : g.letterGrade === "B+" || g.letterGrade === "B" ? "badge-primary" : "badge-warning"}`}>
                          {g.letterGrade}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </motion.div>
        ))}
      </div>
    </AppShell>
  );
}
