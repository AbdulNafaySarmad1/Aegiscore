"use client";

import { useState } from "react";
import { apiPost } from "@/lib/api-client";
import { useTranslations } from "next-intl";

export function GradeAppealForm() {
  const t = useTranslations("academic");
  const [studentId, setStudentId] = useState("");
  const [courseId, setCourseId] = useState("");
  const [semester, setSemester] = useState("");
  const [grade, setGrade] = useState("");
  const [grounds, setGrounds] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState<string | null>(null);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    try {
      await apiPost("/api/grade-appeals", { studentId, courseId, semester, submittedGrade: grade, appealGrounds: grounds, facultyRespondentId: "00000000-0000-0000-0000-000000000000" });
      setResult("Appeal submitted successfully");
    } catch { setResult("Submission failed"); }
    finally { setSubmitting(false); }
  }

  return (
    <div style={{ background: "var(--surface)", borderRadius: 12, border: "1px solid var(--line)", padding: "1.5rem", maxWidth: 600 }}>
      <h2 style={{ color: "var(--ink)", margin: "0 0 1rem 0" }}>Submit Grade Appeal</h2>
      {result && <div style={{ background: result.includes("failed") ? "var(--red-soft)" : "var(--green-soft)", padding: "0.75rem", borderRadius: 8, marginBottom: "1rem", color: "var(--ink)", fontSize: "0.875rem" }}>{result}</div>}
      <form onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: "0.75rem" }}>
        <input placeholder="Student ID" value={studentId} onChange={e => setStudentId(e.target.value)} style={inputStyle} required />
        <input placeholder="Course ID" value={courseId} onChange={e => setCourseId(e.target.value)} style={inputStyle} required />
        <input placeholder="Semester (e.g. Fall 2025)" value={semester} onChange={e => setSemester(e.target.value)} style={inputStyle} required />
        <input placeholder="Submitted Grade (e.g. C)" value={grade} onChange={e => setGrade(e.target.value)} style={inputStyle} required />
        <textarea placeholder="Explain your grounds for appeal..." value={grounds} onChange={e => setGrounds(e.target.value)} rows={4} style={{ ...inputStyle, resize: "vertical" }} required />
        <button type="submit" disabled={submitting} style={{ padding: "0.75rem", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 600, opacity: submitting ? 0.7 : 1 }}>{submitting ? "Submitting..." : "Submit Appeal"}</button>
      </form>
    </div>
  );
}

const inputStyle: React.CSSProperties = {
  padding: "0.625rem 0.75rem", borderRadius: 8, border: "1px solid var(--line)", background: "var(--canvas)", color: "var(--ink)", fontSize: "0.875rem", width: "100%", boxSizing: "border-box"
};
