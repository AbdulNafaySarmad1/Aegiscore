"use client";

import { useState } from "react";
import { motion } from "framer-motion";
import { AppShell } from "@/components/layout/AppShell";
import { apiPost } from "@/lib/api-client";
import { FileText, Download, CheckCircle } from "lucide-react";

const semesters = [
  { sem: "Fall 2024", grades: [
    { course: "Programming Fundamentals", grade: "A", credits: 3 },
    { course: "Calculus", grade: "B+", credits: 3 },
    { course: "English Composition", grade: "A-", credits: 3 },
    { course: "Islamic Studies", grade: "A", credits: 2 },
    { course: "Intro to CS", grade: "A-", credits: 3 },
  ], sgpa: 3.71 },
  { sem: "Spring 2025", grades: [
    { course: "Data Structures", grade: "A-", credits: 3 },
    { course: "Discrete Math", grade: "B+", credits: 3 },
    { course: "Digital Logic", grade: "A", credits: 3 },
    { course: "Pakistan Studies", grade: "B", credits: 2 },
    { course: "OOP", grade: "A", credits: 3 },
  ], sgpa: 3.55 },
  { sem: "Fall 2025 (Current)", grades: [
    { course: "Database Systems", grade: "--", credits: 3 },
    { course: "AI", grade: "--", credits: 3 },
    { course: "Operating Systems", grade: "--", credits: 3 },
    { course: "Software Engineering", grade: "--", credits: 3 },
    { course: "ML", grade: "--", credits: 3 },
    { course: "Cyber Security", grade: "--", credits: 3 },
  ], sgpa: null },
];

export default function TranscriptPage() {
  const [requested, setRequested] = useState(false);
  const [downloading, setDownloading] = useState(false);

  const requestTranscript = async () => {
    setDownloading(true);
    try {
      await apiPost("/api/examination/transcripts/00000000-0000-0000-0000-000000000008/request", { requestedBy: "000", reason: "Print" });
      setRequested(true);
    } catch {} finally { setDownloading(false); }
  };

  const totalCredits = semesters.reduce((s, sem) => s + sem.grades.reduce((c, g) => c + g.credits, 0), 0);
  const cgpa = 3.67;

  return (
    <AppShell>
      <div className="content-page">
        <div className="page-header">
          <div>
            <div className="page-header-breadcrumb"><span>Student</span><span>/</span><span>Transcript</span></div>
            <h1>Transcript</h1>
            <p>Official academic transcript — CGPA: {cgpa.toFixed(2)} | Credits: {totalCredits}</p>
          </div>
          <button className="btn btn-primary" onClick={requestTranscript} disabled={downloading || requested}>
            {requested ? <><CheckCircle size={16} />Requested</> : <><Download size={16} />Request Official Copy</>}
          </button>
        </div>

        {semesters.map((sem, idx) => (
          <motion.div key={idx} className="card" style={{ marginBottom: 16 }} initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: idx * 0.08 }}>
            <div className="card-header">
              <h3 style={{ fontSize: 14 }}>{sem.sem}</h3>
              {sem.sgpa && <span className="badge badge-success">SGPA: {sem.sgpa.toFixed(2)}</span>}
            </div>
            <div className="card-body" style={{ padding: 0 }}>
              <table style={{ width: "100%", borderCollapse: "collapse" }}>
                <thead>
                  <tr style={{ background: "var(--surface-muted)" }}>
                    <th style={{ padding: "10px 16px", textAlign: "left", fontSize: 11, color: "var(--muted)", textTransform: "uppercase" }}>Course</th>
                    <th style={{ padding: "10px 16px", textAlign: "center", fontSize: 11, color: "var(--muted)" }}>Credits</th>
                    <th style={{ padding: "10px 16px", textAlign: "center", fontSize: 11, color: "var(--muted)" }}>Grade</th>
                  </tr>
                </thead>
                <tbody>
                  {sem.grades.map((g, i) => (
                    <tr key={i} style={{ borderBottom: i < sem.grades.length - 1 ? "1px solid var(--border)" : "none" }}>
                      <td style={{ padding: "12px 16px", fontSize: 13 }}>{g.course}</td>
                      <td style={{ padding: "12px 16px", textAlign: "center", fontSize: 13 }}>{g.credits}</td>
                      <td style={{ padding: "12px 16px", textAlign: "center" }}>
                        <span className={`badge ${g.grade === "A" || g.grade === "A-" ? "badge-success" : g.grade === "B+" || g.grade === "B" ? "badge-primary" : g.grade === "--" ? "badge-neutral" : "badge-warning"}`}>
                          {g.grade}
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
