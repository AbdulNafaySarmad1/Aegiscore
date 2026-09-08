"use client";

import { useCallback, useEffect, useRef, useState } from "react";

type Option = { key: string; text: string };
type Question = { index: number; questionId: string; questionText: string; options: Option[] };
type StartResult = {
  applicantTestId: string;
  applicantName: string;
  timeLimitMinutes: number;
  expiresAt: string;
  totalQuestions: number;
  questions: Question[];
};

async function api<T>(path: string, body: unknown): Promise<T> {
  const res = await fetch(path, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body)
  });
  const data = res.headers.get("content-type")?.includes("json") ? await res.json() : await res.text();
  if (!res.ok) throw new Error((data as { error?: string })?.error ?? `Request failed (${res.status})`);
  return data as T;
}

export default function TerminalPage() {
  const [phase, setPhase] = useState<"idle" | "running" | "done">("idle");
  const [appId, setAppId] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [test, setTest] = useState<StartResult | null>(null);
  const [current, setCurrent] = useState(0);
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const [remaining, setRemaining] = useState(0);
  const [passed, setPassed] = useState<boolean | null>(null);
  const [fsWarning, setFsWarning] = useState(false);

  const submittedRef = useRef(false);

  const submit = useCallback(async (auto: boolean) => {
    if (submittedRef.current || !test) return;
    submittedRef.current = true;
    try {
      const r = await api<{ passed: boolean }>(`/api/terminal/${test.applicantTestId}/submit`, { auto });
      setPassed(r.passed);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Submission failed.");
    } finally {
      setPhase("done");
      if (document.fullscreenElement) document.exitFullscreen().catch(() => {});
    }
  }, [test]);

  const submitLatest = useRef(submit);
  useEffect(() => { submitLatest.current = submit; }, [submit]);

  // Countdown
  useEffect(() => {
    if (phase !== "running" || !test) return;
    const deadline = new Date(test.expiresAt).getTime();
    const tick = () => {
      const secs = Math.max(0, Math.round((deadline - Date.now()) / 1000));
      setRemaining(secs);
      if (secs <= 0) submitLatest.current(true);
    };
    tick();
    const id = window.setInterval(tick, 1000);
    return () => window.clearInterval(id);
  }, [phase, test]);

  // Fullscreen guard
  useEffect(() => {
    if (phase !== "running") return;
    const onChange = () => setFsWarning(!document.fullscreenElement);
    document.addEventListener("fullscreenchange", onChange);
    return () => document.removeEventListener("fullscreenchange", onChange);
  }, [phase]);

  const begin = async () => {
    setError(null);
    try {
      const r = await api<StartResult>("/api/terminal/start", { applicantId: appId.trim().toUpperCase() });
      setTest(r);
      setPhase("running");
      setCurrent(0);
      try { await document.documentElement.requestFullscreen(); } catch { /* kiosk may already be fullscreen */ }
    } catch (e) {
      setError(e instanceof Error ? e.message : "Could not start the test.");
    }
  };

  const choose = async (q: Question, key: string) => {
    setAnswers(a => ({ ...a, [q.questionId]: key }));
    try {
      await api(`/api/terminal/${test!.applicantTestId}/answer`, { questionId: q.questionId, optionKey: key });
    } catch (e) {
      // A time-up server response ends the test.
      if (e instanceof Error && e.message.toLowerCase().includes("time is up")) submitLatest.current(true);
    }
  };

  const mm = String(Math.floor(remaining / 60)).padStart(2, "0");
  const ss = String(remaining % 60).padStart(2, "0");

  return (
    <main className="terminal-root">
      <style>{`
        .terminal-root{min-height:100vh;background:#0f1420;color:#e8ecf4;display:flex;align-items:center;justify-content:center;font-size:16px;padding:24px}
        .terminal-card{width:100%;max-width:820px;background:#161d2e;border:1px solid #26314a;border-radius:12px;padding:28px}
        .terminal-timer{font-variant-numeric:tabular-nums;font-size:26px;font-weight:700;letter-spacing:1px}
        .terminal-opt{display:block;width:100%;text-align:left;margin:10px 0;padding:14px 16px;border-radius:8px;border:1px solid #2c3a58;background:#101725;color:inherit;cursor:pointer}
        .terminal-opt.sel{border-color:#4c8dff;background:#16233c}
        .terminal-progress{height:6px;background:#22304c;border-radius:999px;overflow:hidden;margin:14px 0}
        .terminal-progress > div{height:100%;background:#4c8dff}
        .terminal-warn{background:#3a1d1d;border:1px solid #7a2e2e;color:#ffb4b4;padding:10px 14px;border-radius:8px;margin-bottom:14px}
        .terminal-btn{padding:12px 20px;border-radius:8px;border:0;background:#4c8dff;color:#fff;font-weight:600;cursor:pointer}
        .terminal-btn[disabled]{opacity:.5;cursor:not-allowed}
        .terminal-input{width:100%;padding:14px 16px;border-radius:8px;border:1px solid #2c3a58;background:#101725;color:inherit;letter-spacing:2px;text-transform:uppercase}
      `}</style>

      <div className="terminal-card">
        {phase === "idle" && (
          <>
            <h1>Admission Test Terminal</h1>
            <p style={{ color: "#9fb0cc" }}>Enter your Applicant ID to begin. The test runs full-screen and cannot be paused.</p>
            {error && <div className="terminal-warn">{error}</div>}
            <input className="terminal-input" value={appId} onChange={e => setAppId(e.target.value)} placeholder="APP-2025F-BCS-XXXXXXXX" />
            <div style={{ marginTop: 16 }}>
              <button className="terminal-btn" disabled={!appId.trim()} onClick={begin}>Begin test</button>
            </div>
          </>
        )}

        {phase === "running" && test && (
          <>
            {fsWarning && <div className="terminal-warn">You exited full-screen. Return to full-screen to continue — the timer is still running.</div>}
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
              <strong>{test.applicantName}</strong>
              <span className="terminal-timer" style={{ color: remaining < 60 ? "#ff7a7a" : undefined }}>{mm}:{ss}</span>
            </div>
            <div className="terminal-progress"><div style={{ width: `${((current + 1) / test.totalQuestions) * 100}%` }} /></div>
            <p style={{ color: "#9fb0cc" }}>Question {current + 1} of {test.totalQuestions}</p>

            {(() => {
              const q = test.questions[current];
              return (
                <>
                  <h2 style={{ lineHeight: 1.4 }}>{q.questionText}</h2>
                  {q.options.map(o => (
                    <button key={o.key} className={`terminal-opt ${answers[q.questionId] === o.key ? "sel" : ""}`} onClick={() => choose(q, o.key)}>
                      {o.text}
                    </button>
                  ))}
                  <div style={{ display: "flex", justifyContent: "flex-end", marginTop: 18 }}>
                    {current < test.totalQuestions - 1 ? (
                      <button className="terminal-btn" disabled={!answers[q.questionId]} onClick={() => setCurrent(c => c + 1)}>Next</button>
                    ) : (
                      <button className="terminal-btn" disabled={!answers[q.questionId]} onClick={() => submitLatest.current(false)}>Finish &amp; submit</button>
                    )}
                  </div>
                  <p style={{ color: "#6c7a96", fontSize: 13, marginTop: 10 }}>You cannot return to a previous question.</p>
                </>
              );
            })()}
          </>
        )}

        {phase === "done" && (
          <>
            <h1>Test complete</h1>
            {error && <div className="terminal-warn">{error}</div>}
            {passed !== null && (
              <p style={{ fontSize: 22, fontWeight: 700, color: passed ? "#5ad19a" : "#ff7a7a" }}>
                {passed ? "PASS" : "FAIL"}
              </p>
            )}
            <p style={{ color: "#9fb0cc" }}>You may now leave the terminal. Results are recorded with the admissions office.</p>
          </>
        )}
      </div>
    </main>
  );
}
