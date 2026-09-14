"use client";

import { use, useCallback, useEffect, useState } from "react";

type Progress = { studentName: string; program: string | null; cgpa: number; earnedCredits: number; requiredCredits: number; approved: boolean; blockers: string[] };

export default function GraduationPage({ params }: { params: Promise<{ studentId: string }> }) {
  const { studentId } = use(params);
  const [progress, setProgress] = useState<Progress | null>(null);
  const [message, setMessage] = useState("Loading graduation requirements...");
  const [busy, setBusy] = useState(false);
  const refresh = useCallback(async () => {
    const response = await fetch(`/api/graduation/${encodeURIComponent(studentId)}`, { credentials: "include", cache: "no-store" });
    if (!response.ok) throw new Error(response.status === 403 ? "You do not have access to this student's graduation record." : "Unable to load graduation requirements. Sign in and try again.");
    setProgress(await response.json()); setMessage("");
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
  return <main style={{ maxWidth: 900, margin: "48px auto", padding: 24 }}>
    <h1>Graduation review</h1>
    <p role="status">{message}</p>
    {progress && <>
      <h2>{progress.studentName}</h2><p>{progress.program}</p>
      <p>CGPA: {progress.cgpa.toFixed(2)} · Earned credits: {progress.earnedCredits} / {progress.requiredCredits}</p>
      {progress.approved ? <p>Graduation approved. <a href="/alumni">View your alumni profile</a></p> : <>
        <h2>Requirements remaining</h2>
        {progress.blockers.length ? <ul>{progress.blockers.map(item => <li key={item}>{item}</li>)}</ul> : <p>All configured requirements are satisfied. University approval is still required.</p>}
        <fieldset disabled={busy}><legend>University approval</legend>
          <p>These actions require graduation approval permission. Clearance decisions are attributed to the signed-in approver.</p>
          {["Library", "Finance", "Department"].map(department => <button key={department} onClick={() => submit(`clearances/${department}`)} style={{ margin: 8 }}>Approve {department} clearance</button>)}
          <button disabled={progress.blockers.length > 0} onClick={() => submit("approve")} style={{ margin: 8 }}>Approve graduation</button>
        </fieldset>
      </>}
    </>}
  </main>;
}
