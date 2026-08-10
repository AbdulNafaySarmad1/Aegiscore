"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiDownload, apiGet, apiPost } from "@/lib/api-client";
import {
  BarChart3,
  BookOpenCheck,
  CheckCircle2,
  Download,
  FileCheck2,
  FileUp,
  Link2,
  Loader2,
  QrCode,
  RefreshCw,
  ShieldCheck,
  Users,
} from "lucide-react";

type Mapping = { clo: string; plo: string; alignment: string };
type ObeCourse = {
  id: string;
  code: string;
  title: string;
  creditHours: number;
  enrollmentCount: number;
  assessmentRecords: number;
  averageAssessmentPercentage: number | null;
  attendanceRecords: number;
  mappedClos: number;
  mappedPlos: number;
  mappings: Mapping[];
};
type Overview = {
  faculty: { id: string; name: string; email: string };
  tenantId: string;
  courses: ObeCourse[];
  reportSecurity: { pqc: string; qrVerification: boolean; auditMetadata: string[] };
};

export default function FacultyObePage() {
  const [overview, setOverview] = useState<Overview | null>(null);
  const [selectedCourseId, setSelectedCourseId] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [uploading, setUploading] = useState(false);
  const [downloading, setDownloading] = useState(false);
  const fileRef = useRef<HTMLInputElement>(null);

  const load = async () => {
    setLoading(true);
    setError("");
    try {
      const data = await apiGet<Overview>("/api/obe/faculty/overview");
      setOverview(data);
      setSelectedCourseId(current => current || data.courses[0]?.id || "");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Unable to load the OBE workspace.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, []);

  const selected = useMemo(
    () => overview?.courses.find(course => course.id === selectedCourseId) ?? null,
    [overview, selectedCourseId]
  );

  const uploadCis = async (file: File) => {
    if (!selected || !overview) return;
    setUploading(true);
    setError("");
    setMessage("");
    try {
      const form = new FormData();
      form.append("file", file);
      const uploadId = await apiPost<string>(
        `/api/obe/v2/cis/${selected.id}/upload?facultyId=${encodeURIComponent(overview.faculty.id)}`,
        form
      );
      if (uploadId) {
        await apiPost(`/api/obe/v2/cis/${encodeURIComponent(uploadId)}/process`, {});
      }
      setMessage(`${file.name} uploaded and queued for OBE/CIS extraction.`);
    } catch (e) {
      setError(e instanceof Error ? e.message : "CIS upload failed.");
    } finally {
      setUploading(false);
      if (fileRef.current) fileRef.current.value = "";
    }
  };

  const downloadSignedReport = async () => {
    if (!selected) return;
    setDownloading(true);
    setError("");
    setMessage("");
    try {
      await apiDownload(
        `/api/obe/faculty/course/${selected.id}/signed-report`,
        { method: "POST" },
        `AegisCore-OBE-${selected.code}.pdf`
      );
      setMessage("PQC-signed OBE report generated. The PDF contains a QR verification link and full audit metadata.");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Unable to generate the signed report.");
    } finally {
      setDownloading(false);
    }
  };

  return (
    <AppShell>
      <div className="content-page">
        <div className="page-header">
          <div>
            <div className="page-header-breadcrumb"><span>Academic</span><span>/</span><span>OBE</span></div>
            <h1>Outcome Based Education</h1>
            <p>CLO/PLO mapping, CIS evidence, attainment context, and PQC-verifiable course reports.</p>
          </div>
          <button className="btn btn-secondary" onClick={() => void load()} disabled={loading}>
            <RefreshCw size={16} /> Refresh
          </button>
        </div>

        {error && <div className="error-state" style={{ marginBottom: 16 }}><p>{error}</p></div>}
        {message && (
          <div style={{ marginBottom: 16, padding: "12px 14px", border: "1px solid var(--success)", borderRadius: 10, color: "var(--success)", background: "var(--success-soft)", display: "flex", alignItems: "center", gap: 8 }}>
            <CheckCircle2 size={18} /> {message}
          </div>
        )}

        {loading ? (
          <div className="card"><div className="card-body" style={{ display: "flex", alignItems: "center", gap: 10 }}><Loader2 size={18} className="animate-spin" /> Loading faculty OBE workspace…</div></div>
        ) : !overview ? null : (
          <>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit,minmax(190px,1fr))", gap: 12, marginBottom: 18 }}>
              <Metric icon={<BookOpenCheck size={19} />} label="Assigned Courses" value={overview.courses.length} />
              <Metric icon={<Link2 size={19} />} label="Mapped CLOs" value={overview.courses.reduce((s, c) => s + c.mappedClos, 0)} />
              <Metric icon={<BarChart3 size={19} />} label="Mapped PLOs" value={overview.courses.reduce((s, c) => s + c.mappedPlos, 0)} />
              <Metric icon={<ShieldCheck size={19} />} label="Report Signing" value="PQC" />
            </div>

            <div className="card" style={{ marginBottom: 16 }}>
              <div className="card-body">
                <label htmlFor="obe-course" style={{ display: "block", fontWeight: 700, fontSize: 13, marginBottom: 7 }}>Course workspace</label>
                <select id="obe-course" value={selectedCourseId} onChange={e => setSelectedCourseId(e.target.value)} style={{ width: "100%", minHeight: 44, border: "1px solid var(--border)", borderRadius: 10, background: "var(--surface)", color: "var(--ink)", padding: "0 12px" }}>
                  {overview.courses.length === 0 && <option value="">No assigned courses found</option>}
                  {overview.courses.map(course => <option key={course.id} value={course.id}>{course.code} — {course.title}</option>)}
                </select>
              </div>
            </div>

            {selected && (
              <div style={{ display: "grid", gridTemplateColumns: "minmax(0,1.4fr) minmax(280px,.8fr)", gap: 16 }}>
                <div style={{ display: "grid", gap: 16 }}>
                  <div className="card">
                    <div className="card-header">
                      <div>
                        <strong>{selected.code} — {selected.title}</strong>
                        <div style={{ color: "var(--muted)", fontSize: 12, marginTop: 3 }}>{selected.creditHours} credit hours</div>
                      </div>
                    </div>
                    <div className="card-body">
                      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit,minmax(145px,1fr))", gap: 10, marginBottom: 18 }}>
                        <Mini label="Students" value={selected.enrollmentCount} icon={<Users size={16} />} />
                        <Mini label="Assessments" value={selected.assessmentRecords} icon={<FileCheck2 size={16} />} />
                        <Mini label="Average" value={selected.averageAssessmentPercentage == null ? "—" : `${selected.averageAssessmentPercentage.toFixed(1)}%`} icon={<BarChart3 size={16} />} />
                        <Mini label="Attendance rows" value={selected.attendanceRecords} icon={<CheckCircle2 size={16} />} />
                      </div>

                      <h3 style={{ margin: "0 0 10px", fontSize: 15 }}>CLO → PLO Mapping</h3>
                      {selected.mappings.length === 0 ? (
                        <div style={{ color: "var(--muted)", fontSize: 13, padding: "14px 0" }}>No CLO/PLO mappings configured yet. Add mappings through the OBE coordinator workflow.</div>
                      ) : (
                        <div style={{ overflowX: "auto" }}>
                          <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 13 }}>
                            <thead><tr><Th>CLO</Th><Th>PLO</Th><Th>Alignment</Th></tr></thead>
                            <tbody>{selected.mappings.map((m, i) => <tr key={`${m.clo}-${m.plo}-${i}`}><Td>{m.clo}</Td><Td>{m.plo}</Td><Td><span className="badge badge-info">{m.alignment}</span></Td></tr>)}</tbody>
                          </table>
                        </div>
                      )}
                    </div>
                  </div>
                </div>

                <div style={{ display: "grid", gap: 16, alignContent: "start" }}>
                  <div className="card">
                    <div className="card-header"><strong>Evidence / CIS Upload</strong></div>
                    <div className="card-body">
                      <p style={{ margin: "0 0 12px", color: "var(--muted)", fontSize: 13 }}>Upload the course information sheet or OBE evidence for extraction. PDF/JPG/PNG, maximum 10 MB.</p>
                      <input ref={fileRef} type="file" accept="application/pdf,image/png,image/jpeg" style={{ display: "none" }} onChange={e => { const file = e.target.files?.[0]; if (file) void uploadCis(file); }} />
                      <button className="btn btn-primary" style={{ width: "100%" }} onClick={() => fileRef.current?.click()} disabled={uploading}>
                        {uploading ? <><Loader2 size={16} className="animate-spin" /> Processing…</> : <><FileUp size={16} /> Upload CIS / Evidence</>}
                      </button>
                    </div>
                  </div>

                  <div className="card">
                    <div className="card-header"><strong>Verified Report</strong></div>
                    <div className="card-body">
                      <div style={{ display: "flex", alignItems: "center", gap: 9, marginBottom: 10, color: "var(--success)", fontWeight: 700, fontSize: 13 }}><ShieldCheck size={18} /> {overview.reportSecurity.pqc}</div>
                      <p style={{ margin: "0 0 12px", color: "var(--muted)", fontSize: 13 }}>Print-ready PDF with QR verification, UUID, issuer/tenant IDs, UTC timestamp, network/geo audit metadata, report hash, detached PQC signature and public key.</p>
                      <button className="btn btn-primary" style={{ width: "100%" }} onClick={() => void downloadSignedReport()} disabled={downloading}>
                        {downloading ? <><Loader2 size={16} className="animate-spin" /> Signing…</> : <><Download size={16} /> Generate PQC Report</>}
                      </button>
                      <div style={{ display: "flex", alignItems: "center", gap: 7, marginTop: 10, fontSize: 11, color: "var(--muted)" }}><QrCode size={14} /> QR opens the verification endpoint.</div>
                    </div>
                  </div>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </AppShell>
  );
}

function Metric({ icon, label, value }: { icon: React.ReactNode; label: string; value: React.ReactNode }) {
  return <div className="card"><div className="card-body" style={{ minHeight: 92 }}><div style={{ display: "flex", alignItems: "center", gap: 8, color: "var(--muted)", fontSize: 12, fontWeight: 700, textTransform: "uppercase" }}>{icon}{label}</div><div style={{ fontSize: 26, lineHeight: 1.1, fontWeight: 800, marginTop: 12 }}>{value}</div></div></div>;
}
function Mini({ icon, label, value }: { icon: React.ReactNode; label: string; value: React.ReactNode }) {
  return <div style={{ border: "1px solid var(--border)", borderRadius: 10, padding: 11 }}><div style={{ display: "flex", alignItems: "center", gap: 6, color: "var(--muted)", fontSize: 11 }}>{icon}{label}</div><div style={{ fontWeight: 800, fontSize: 18, marginTop: 6 }}>{value}</div></div>;
}
function Th({ children }: { children: React.ReactNode }) { return <th style={{ textAlign: "left", padding: "9px 10px", borderBottom: "1px solid var(--border)", color: "var(--muted)", fontSize: 11, textTransform: "uppercase" }}>{children}</th>; }
function Td({ children }: { children: React.ReactNode }) { return <td style={{ padding: "10px", borderBottom: "1px solid var(--border)" }}>{children}</td>; }
