"use client";

import { use, useCallback, useEffect, useState } from "react";
import { loadSession } from "@/lib/session";

type Progress = { studentName: string; program: string | null; cgpa: number; earnedCredits: number; requiredCredits: number; approved: boolean; blockers: string[] };

export default function GraduationPage({ params }: { params: Promise<{ studentId: string }> }) {
  const { studentId } = use(params);
  const [progress, setProgress] = useState<Progress | null>(null);
  const [canApprove, setCanApprove] = useState(false);
  const [message, setMessage] = useState("Loading graduation requirements...");
  const [busy, setBusy] = useState(false);
  const refresh = useCallback(async () => {
    const [response, session] = await Promise.all([
      fetch(`/api/graduation/${encodeURIComponent(studentId)}`, { credentials: "include", cache: "no-store" }),
      loadSession()
    ]);
    if (!response.ok) throw new Error(response.status === 403 ? "You do not have access to this student's graduation record." : "Unable to load graduation requirements. Sign in and try again.");
    setProgress(await response.json());
    setCanApprove(session.roles.some(role => ["admin", "superadmin"].includes(role.toLowerCase())));
    setMessage("");
  }, [studentId]);
  useEffect(() => { refresh().catch(error => setMessage(error.message)); }, [refresh]);
  async function submit(action: string) {
    setBusy(true); setMessage("");
    try {
      const response = await fetch(`/api/graduation/${encodeURIComponent(studentId)}/${action}`, { method: "POST", credentials: "include" });
      if (!response.ok) {
        const body = await response.json().catch(() => ({}));
        throw new Error(response.status === 403 ? "Only an authorized university approver can perform this action." : body.error ?? "The action could not be completed.");
      }
      await refresh();
    } catch (error) { setMessage(error instanceof Error ? error.message : "The action failed."); }
    finally { setBusy(false); }
  }
  return <main style={{ minHeight: "100vh", padding: "56px 28px", background: "linear-gradient(145deg,#f4f7fb,#e8eef8)", color: "#10213d" }}>
    <section style={{ maxWidth: 980, margin: "0 auto", background: "white", borderRadius: 20, boxShadow: "0 18px 50px rgba(16,33,61,.12)", overflow: "hidden" }}>
      <header style={{ padding: "34px 40px", background: "linear-gradient(135deg,#0d2345,#174984)", color: "white" }}>
        <div style={{ textTransform: "uppercase", letterSpacing: ".16em", fontSize: 12, opacity: .75 }}>Academic records · graduation office</div>
        <h1 style={{ margin: "10px 0 4px", fontSize: 38 }}>Graduation review</h1>
        <p role="status" style={{ margin: 0, opacity: .85 }}>{message}</p>
      </header>
      {progress && <div style={{ padding: 40 }}>
        <h2 style={{ margin: 0, fontSize: 30 }}>{progress.studentName}</h2><p style={{ margin: "5px 0 26px", color: "#60708a" }}>{progress.program}</p>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(3,minmax(0,1fr))", gap: 14, marginBottom: 28 }}>
          {[["CGPA", progress.cgpa.toFixed(2)], ["Earned credits", progress.earnedCredits], ["Required credits", progress.requiredCredits]].map(([label, value]) => <div key={label} style={{ padding: 18, border: "1px solid #dce5f2", borderRadius: 14, background: "#f8fafd" }}><small style={{ color: "#6b7890" }}>{label}</small><strong style={{ display: "block", marginTop: 5, fontSize: 25 }}>{value}</strong></div>)}
        </div>
        {progress.approved ? <div style={{ padding: 22, borderRadius: 14, background: "#e8f7ef", color: "#17653a" }}><strong>Graduation approved.</strong> <a href="/alumni">View your alumni profile</a></div> : <>
          <h2 style={{ marginBottom: 12 }}>Requirements remaining</h2>
          {progress.blockers.length ? <ul style={{ display: "grid", gap: 9, margin: 0, padding: 0, listStyle: "none" }}>{progress.blockers.map(item => <li key={item} style={{ padding: "13px 16px", borderRadius: 10, background: "#fff5e6", color: "#7a4a00" }}>○ {item}</li>)}</ul> : <p style={{ padding: 16, borderRadius: 10, background: "#e8f7ef", color: "#17653a" }}>All configured requirements are satisfied. University approval is still required.</p>}
          {canApprove ? <fieldset disabled={busy} style={{ marginTop: 26, padding: 22, border: "1px solid #dce5f2", borderRadius: 14 }}><legend style={{ padding: "0 8px", fontWeight: 700 }}>University approval</legend>
            <p style={{ color: "#60708a" }}>Clearance decisions are attributed to the signed-in approver.</p>
            <div style={{ display: "flex", flexWrap: "wrap", gap: 10 }}>{["Library", "Finance", "Department"].map(department => <button key={department} onClick={() => submit(`clearances/${department}`)} style={{ padding: "10px 14px", border: "1px solid #aebbd0", borderRadius: 9, background: "white", cursor: "pointer" }}>Approve {department} clearance</button>)}
              <button disabled={progress.blockers.length > 0} onClick={() => submit("approve")} style={{ padding: "10px 16px", border: 0, borderRadius: 9, background: progress.blockers.length ? "#b8c0cc" : "#2467d6", color: "white" }}>Approve graduation</button></div>
          </fieldset> : <p style={{ marginTop: 24, color: "#60708a" }}>University staff must complete the listed clearances before graduation can be approved.</p>}
        </>}
      </div>}
    </section>
  </main>;
}
