"use client";

import { useEffect, useState, useCallback } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { usePersonalityTranslations } from "@/lib/personality-translator";
import {
  AlertCircle, BookOpen, CheckCircle2, Clock, CloudUpload, FileSearch,
  FileText, GraduationCap, Pencil, RefreshCw, Send, Upload, XCircle, Zap
} from "lucide-react";

type IngestionStatus = "Received" | "Classifying" | "Extracting" | "Validating" | "Reviewing" | "Inserting" | "Completed" | "Failed";

type IngestionJob = {
  id: string; fileName: string; status: IngestionStatus; coursesExtracted: number;
  coursesInserted: number; requiringReview: number; warnings: number; errors: number;
  createdAt: string;
};

type ExtractedCourse = {
  id: string; jobId: string; courseCode: string; title: string; credits: number;
  semester: string; type: string; confidence: number; status: "PendingReview" | "Approved" | "Rejected";
};

const statusBadge = (s: IngestionStatus) => {
  const map: Record<IngestionStatus, { c: string; bg: string }> = {
    Received: { c: "var(--muted)", bg: "var(--surface-soft)" },
    Classifying: { c: "var(--amber)", bg: "var(--amber-soft)" },
    Extracting: { c: "var(--amber)", bg: "var(--amber-soft)" },
    Validating: { c: "var(--amber)", bg: "var(--amber-soft)" },
    Reviewing: { c: "var(--blue-600)", bg: "var(--blue-100)" },
    Inserting: { c: "var(--blue-600)", bg: "var(--blue-100)" },
    Completed: { c: "var(--green)", bg: "var(--green-soft)" },
    Failed: { c: "var(--red)", bg: "var(--red-soft)" }
  };
  const v = map[s];
  return <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: v.bg, color: v.c }}>{s}</span>;
};

const confidenceColor = (c: number) => c > 90 ? "var(--green)" : c >= 70 ? "var(--amber)" : "var(--red)";
const confidenceBg = (c: number) => c > 90 ? "var(--green-soft)" : c >= 70 ? "var(--amber-soft)" : "var(--red-soft)";

const statusProgress = (s: IngestionStatus): number => {
  const order: IngestionStatus[] = ["Received", "Classifying", "Extracting", "Validating", "Reviewing", "Inserting", "Completed"];
  const idx = order.indexOf(s);
  return idx === -1 ? 0 : Math.round(((idx + 1) / order.length) * 100);
};

const inputS: React.CSSProperties = { minHeight: 42, padding: "0 11px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--ink)", background: "var(--canvas)", fontSize: "0.85rem", width: "100%", boxSizing: "border-box" };
const labelS: React.CSSProperties = { display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 };

export default function CatalogPage() {
  const t = usePersonalityTranslations("academic");

  const [jobs, setJobs] = useState<IngestionJob[]>([]);
  const [courses, setCourses] = useState<ExtractedCourse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const [file, setFile] = useState<File | null>(null);
  const [dragOver, setDragOver] = useState(false);
  const [ingesting, setIngesting] = useState(false);
  const [publishing, setPublishing] = useState(false);
  const [computingTree, setComputingTree] = useState(false);
  const [reviewingId, setReviewingId] = useState<string | null>(null);

  useEffect(() => {
    const ctrl = new AbortController();
    const fetch = async () => {
      try {
        const [j, c] = await Promise.all([
          apiGet<IngestionJob[]>("/api/catalog/ingest/jobs", ctrl.signal).catch(() => [] as IngestionJob[]),
          apiGet<ExtractedCourse[]>("/api/catalog/courses", ctrl.signal).catch(() => [] as ExtractedCourse[])
        ]);
        setJobs(j); setCourses(c);
      } catch { setError("Failed to load catalog data"); }
      finally { setLoading(false); }
    };
    fetch();
    return () => { ctrl.abort(); };
  }, []);

  async function startIngestion() {
    if (!file) return;
    setIngesting(true); setMessage(null);
    try {
      const form = new FormData();
      form.append("file", file);
      await apiPost("/api/catalog/ingest", form);
      setMessage("success: Ingestion started");
      setFile(null);
      const ctrl = new AbortController();
      const [j, c] = await Promise.all([
        apiGet<IngestionJob[]>("/api/catalog/ingest/jobs", ctrl.signal),
        apiGet<ExtractedCourse[]>("/api/catalog/courses", ctrl.signal)
      ]);
      setJobs(j); setCourses(c);
    } catch { setMessage("error: Ingestion failed"); }
    finally { setIngesting(false); }
  }

  async function reviewCourse(jobId: string, courseId: string, approved: boolean) {
    setReviewingId(courseId);
    try {
      await apiPost(`/api/catalog/ingest/jobs/${jobId}/review`, { courseId, approved });
      setMessage(`success: Course ${approved ? "approved" : "rejected"}`);
      setCourses(prev => prev.map(c => c.id === courseId ? { ...c, status: approved ? "Approved" as const : "Rejected" as const } : c));
    } catch { setMessage("error: Review failed"); }
    finally { setReviewingId(null); }
  }

  async function publishCatalog() {
    setPublishing(true);
    try {
      await apiPost("/api/catalog/publish");
      setMessage("success: Catalog published");
    } catch { setMessage("error: Publication failed"); }
    finally { setPublishing(false); }
  }

  async function computeDegreeTree() {
    setComputingTree(true);
    try {
      await apiPost("/api/catalog/degree-tree/compute");
      setMessage("success: Degree tree computed");
    } catch { setMessage("error: Degree tree computation failed"); }
    finally { setComputingTree(false); }
  }

  const latestJob = jobs[0];
  const reviewQueue = courses.filter(c => c.status === "PendingReview");

  if (loading) return (
    <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", gap: 16, color: "var(--muted)" }}>
      <div style={{ width: 40, height: 4, borderRadius: 99, overflow: "hidden", background: "var(--line)" }}>
        <div style={{ width: "50%", height: "100%", borderRadius: "inherit", background: "var(--blue-500)", animation: "launch 1s ease-in-out infinite alternate" }} />
      </div>
      Loading course catalog...
    </main>
  );

  if (error) return (
    <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", alignContent: "center", gap: 12, color: "var(--red)" }}>
      <AlertCircle style={{ width: 32 }} />
      <strong>{error}</strong>
      <button onClick={() => window.location.reload()} style={{ minHeight: 40, padding: "0 18px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--blue-600)", background: "var(--surface)", cursor: "pointer", fontWeight: 700 }}>Retry</button>
    </main>
  );

  return (
    <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
      <header style={{ display: "flex", alignItems: "end", justifyContent: "space-between", gap: 20, marginBottom: 28 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>Academic Records</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>{t("catalog") ?? "Course Catalog Ingestion"}</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>Intelligent document-to-database pipeline with OCR, validation, and versioning</span>
        </div>
      </header>

      {message && (
        <div style={{ display: "flex", alignItems: "center", gap: 9, padding: "11px 14px", marginBottom: 18, border: message.startsWith("error") ? "1px solid #ecc0c6" : "1px solid #aaddc9", borderRadius: 10, color: message.startsWith("error") ? "var(--red)" : "var(--green)", background: message.startsWith("error") ? "var(--red-soft)" : "var(--green-soft)", fontSize: 11 }}>
          {message.startsWith("error") ? <AlertCircle /> : <CheckCircle2 />}{message.replace(/^[^:]+: /, "")}
        </div>
      )}

      <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 1.3fr) minmax(360px, 0.8fr)", gap: 18, marginBottom: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Document Upload</h2><p>Upload PDF, Excel, or image files for ingestion</p></div><CloudUpload /></div>
          <div style={{ padding: 18 }}>
            <div
              onDragOver={e => { e.preventDefault(); setDragOver(true); }}
              onDragLeave={() => setDragOver(false)}
              onDrop={e => { e.preventDefault(); setDragOver(false); const f = e.dataTransfer.files[0]; if (f) setFile(f); }}
              style={{ minHeight: 150, display: "grid", placeItems: "center", alignContent: "center", gap: 10, padding: 24, border: `2px dashed ${dragOver ? "var(--blue-600)" : "var(--line-strong)"}`, borderRadius: 12, background: dragOver ? "var(--blue-100)" : "var(--surface-soft)", cursor: "pointer", transition: "all .2s" }}>
              <Upload style={{ width: 32, color: "var(--blue-600)", opacity: dragOver ? 1 : 0.6 }} />
              {file ? <strong style={{ fontSize: 12 }}>{file.name}</strong> : <><strong style={{ fontSize: 12 }}>Drag & drop a file here</strong><small style={{ color: "var(--muted)" }}>or click to browse (PDF, Excel, Images)</small></>}
              <input type="file" accept=".pdf,.xlsx,.xls,.jpg,.png,.jpeg" style={{ position: "absolute", opacity: 0, cursor: "pointer", width: "100%", height: "100%" }} onChange={e => setFile(e.target.files?.[0] ?? null)} />
            </div>
            <button onClick={startIngestion} disabled={ingesting || !file}
              style={{ marginTop: 14, width: "100%", padding: "0.7rem", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, display: "flex", alignItems: "center", justifyContent: "center", gap: 7, opacity: ingesting || !file ? 0.6 : 1 }}>
              <Send style={{ width: 14 }} /> {ingesting ? "Starting Ingestion..." : "Start Ingestion"}
            </button>
          </div>
        </div>

        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Actions</h2><p>Catalog management operations</p></div><Zap /></div>
          <div style={{ padding: 18, display: "grid", gap: 10 }}>
            <button onClick={publishCatalog} disabled={publishing}
              style={{ padding: "0.7rem", background: "var(--green)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, opacity: publishing ? 0.6 : 1 }}>
              {publishing ? "Publishing..." : "Publish Catalog"}
            </button>
            <button onClick={computeDegreeTree} disabled={computingTree}
              style={{ padding: "0.7rem", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, opacity: computingTree ? 0.6 : 1 }}>
              {computingTree ? "Computing..." : "Compute Degree Tree"}
            </button>
          </div>
        </div>
      </section>

      {latestJob && (
        <section className="surface" style={{ overflow: "hidden", marginBottom: 18 }}>
          <div className="surface-heading"><div><h2>Ingestion Job Status</h2><p>Job ID: {latestJob.id.slice(0, 8)}...</p></div><FileSearch /></div>
          <div style={{ padding: 18 }}>
            <div style={{ display: "flex", alignItems: "center", gap: 12, marginBottom: 16 }}>
              {statusBadge(latestJob.status)}
              <div style={{ flex: 1, height: 6, borderRadius: 99, overflow: "hidden", background: "var(--line)" }}>
                <div style={{ width: `${statusProgress(latestJob.status)}%`, height: "100%", borderRadius: 99, background: latestJob.status === "Failed" ? "var(--red)" : "var(--blue-600)", transition: "width .4s" }} />
              </div>
              <span style={{ fontSize: 10, fontWeight: 700, minWidth: 36, textAlign: "right" }}>{statusProgress(latestJob.status)}%</span>
            </div>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(140px, 1fr))", gap: 10 }}>
              <div style={{ padding: 12, borderRadius: 8, background: "var(--surface-soft)", textAlign: "center" }}>
                <strong style={{ display: "block", fontSize: 22, color: "var(--blue-600)" }}>{latestJob.coursesExtracted}</strong>
                <small style={{ color: "var(--muted)", fontSize: 10 }}>Courses Extracted</small>
              </div>
              <div style={{ padding: 12, borderRadius: 8, background: "var(--green-soft)", textAlign: "center" }}>
                <strong style={{ display: "block", fontSize: 22, color: "var(--green)" }}>{latestJob.coursesInserted}</strong>
                <small style={{ color: "var(--green)", fontSize: 10 }}>Inserted</small>
              </div>
              <div style={{ padding: 12, borderRadius: 8, background: "var(--amber-soft)", textAlign: "center" }}>
                <strong style={{ display: "block", fontSize: 22, color: "var(--amber)" }}>{latestJob.requiringReview}</strong>
                <small style={{ color: "var(--amber)", fontSize: 10 }}>Requiring Review</small>
              </div>
              <div style={{ padding: 12, borderRadius: 8, background: "var(--red-soft)", textAlign: "center" }}>
                <strong style={{ display: "block", fontSize: 22, color: "var(--red)" }}>{latestJob.errors}</strong>
                <small style={{ color: "var(--red)", fontSize: 10 }}>Errors</small>
              </div>
            </div>
          </div>
        </section>
      )}

      <div style={{ display: "grid", gridTemplateColumns: "minmax(0, 1fr) minmax(360px, 0.7fr)", gap: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Extracted Courses</h2><p>{courses.length} courses processed</p></div><BookOpen /></div>
          {courses.length === 0 ? (
            <div style={{ minHeight: 180, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
              <GraduationCap style={{ width: 28, opacity: 0.5 }} />
              <strong>No courses extracted</strong>
              <span>Upload a document and start ingestion to see results</span>
            </div>
          ) : (
            <div style={{ overflow: "auto" }}>
              <table style={{ width: "100%", minWidth: 700, borderCollapse: "collapse" }}>
                <thead>
                  <tr style={{ background: "var(--surface-soft)" }}>
                    {["Course Code", "Title", "Credits", "Semester", "Type", "Confidence"].map(h => (
                      <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {courses.map(c => (
                    <tr key={c.id} style={{ borderBottom: "1px solid var(--line)" }}>
                      <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600, fontFamily: "monospace" }}>{c.courseCode}</td>
                      <td style={{ padding: "10px 14px", fontSize: 11 }}>{c.title}</td>
                      <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{c.credits}</td>
                      <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{c.semester}</td>
                      <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{c.type}</td>
                      <td style={{ padding: "10px 14px" }}>
                        <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: confidenceBg(c.confidence), color: confidenceColor(c.confidence) }}>{c.confidence}%</span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Review Queue</h2><p>{reviewQueue.length} courses awaiting review</p></div><Pencil /></div>
          {reviewQueue.length === 0 ? (
            <div style={{ minHeight: 180, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
              <CheckCircle2 style={{ width: 28, opacity: 0.5 }} />
              <strong>No pending reviews</strong>
              <span>All courses have been reviewed</span>
            </div>
          ) : (
            <div style={{ padding: 12, display: "grid", gap: 8, maxHeight: 500, overflow: "auto" }}>
              {reviewQueue.map(c => (
                <div key={c.id} style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 8, padding: "10px 12px", border: "1px solid var(--line)", borderRadius: 8, background: "var(--surface-soft)" }}>
                  <div style={{ display: "grid", gap: 2, minWidth: 0 }}>
                    <strong style={{ fontSize: 11 }}>{c.courseCode} — {c.title}</strong>
                    <small style={{ color: "var(--muted)", fontSize: 10 }}>{c.credits} credits · {c.semester}</small>
                  </div>
                  <div style={{ display: "flex", gap: 6, flexShrink: 0 }}>
                    <button onClick={() => reviewCourse(c.jobId, c.id, true)} disabled={reviewingId === c.id}
                      style={{ padding: "4px 10px", border: "1px solid #aaddc9", borderRadius: 6, color: "var(--green)", background: "var(--green-soft)", cursor: "pointer", fontSize: 10, fontWeight: 700 }}>Approve</button>
                    <button onClick={() => reviewCourse(c.jobId, c.id, false)} disabled={reviewingId === c.id}
                      style={{ padding: "4px 10px", border: "1px solid #ecc0c6", borderRadius: 6, color: "var(--red)", background: "var(--red-soft)", cursor: "pointer", fontSize: 10, fontWeight: 700 }}>Reject</button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </main>
  );
}
