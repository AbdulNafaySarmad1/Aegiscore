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

/**
 * The admission test terminal. A candidate sits at this screen under invigilation, so it is the one
 * surface in the portal with no navigation, no density and no colour beyond what a candidate needs
 * to read: one question, its options, and the time left. It uses the register's type and paper, at
 * the size a stranger can read across a desk.
 */
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
  const [starting, setStarting] = useState(false);
  const [timeUp, setTimeUp] = useState(false);

  const submittedRef = useRef(false);

  const submit = useCallback(async (auto: boolean) => {
    if (submittedRef.current || !test) return;
    submittedRef.current = true;
    try {
      const r = await api<{ passed: boolean }>(`/api/terminal/${test.applicantTestId}/submit`, { auto });
      setPassed(r.passed);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Your answers could not be submitted. Call the invigilator before you leave this screen.");
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

  // The server refused an answer because the time ran out: hand the paper in at once.
  useEffect(() => { if (timeUp) submitLatest.current(true); }, [timeUp]);

  // Fullscreen guard
  useEffect(() => {
    if (phase !== "running") return;
    const onChange = () => setFsWarning(!document.fullscreenElement);
    document.addEventListener("fullscreenchange", onChange);
    return () => document.removeEventListener("fullscreenchange", onChange);
  }, [phase]);

  const begin = async () => {
    setError(null);
    setStarting(true);
    try {
      const r = await api<StartResult>("/api/terminal/start", { applicantId: appId.trim().toUpperCase() });
      setTest(r);
      setPhase("running");
      setCurrent(0);
      try { await document.documentElement.requestFullscreen(); } catch { /* kiosk may already be fullscreen */ }
    } catch (e) {
      setError(e instanceof Error ? e.message : "The test could not be started. Check the applicant number with the invigilator.");
    } finally {
      setStarting(false);
    }
  };

  const choose = async (q: Question, key: string) => {
    setAnswers(a => ({ ...a, [q.questionId]: key }));
    try {
      await api(`/api/terminal/${test!.applicantTestId}/answer`, { questionId: q.questionId, optionKey: key });
    } catch (e) {
      // A time-up server response ends the test; the effect below hands the paper in.
      if (e instanceof Error && e.message.toLowerCase().includes("time is up")) setTimeUp(true);
    }
  };

  const mm = String(Math.floor(remaining / 60)).padStart(2, "0");
  const ss = String(remaining % 60).padStart(2, "0");
  const answered = test ? test.questions.filter(q => answers[q.questionId]).length : 0;

  return (
    <main className="terminal">
      <style>{`
        .terminal { min-height: 100vh; background: var(--stone); color: var(--ink); display: grid; place-items: center; padding: var(--s-8) var(--s-6); }
        .terminal-sheet { width: 100%; max-width: 840px; background: var(--paper); border-top: 2px solid var(--ink); border-bottom: 1px solid var(--rule); padding: var(--s-8); display: grid; gap: var(--s-6); }
        .terminal-rule { height: 2px; background: var(--brass); }
        .terminal-head { display: flex; align-items: baseline; justify-content: space-between; gap: var(--s-4); flex-wrap: wrap; }
        .terminal-who { font-family: var(--font-serif); font-size: var(--t-20); }
        .terminal-clock { font-family: var(--font-mono); font-size: var(--t-33); font-variant-numeric: tabular-nums; letter-spacing: 0.02em; }
        .terminal-clock[data-low="true"] { color: var(--oxblood); }
        .terminal-count { font-size: var(--t-15); color: var(--ink-2); }
        .terminal-gauge { height: 3px; background: var(--rule); }
        .terminal-gauge > div { height: 100%; background: var(--ink); transition: width 200ms var(--ease-out); }
        .terminal-question { font-family: var(--font-serif); font-size: var(--t-26); line-height: 1.45; margin: 0; }
        .terminal-options { display: grid; gap: var(--s-3); }
        .terminal-option { display: grid; grid-template-columns: 34px minmax(0, 1fr); align-items: baseline; gap: var(--s-3); width: 100%; text-align: left; padding: var(--s-4) var(--s-5); background: var(--paper); border: 1px solid var(--rule); border-radius: var(--r-panel); font-size: var(--t-20); color: var(--ink); cursor: pointer; }
        .terminal-option:hover { border-color: var(--ink-3); }
        .terminal-option[aria-pressed="true"] { border-color: var(--ink); box-shadow: inset 3px 0 0 var(--brass); }
        .terminal-option b { font-family: var(--font-mono); font-size: var(--t-15); font-weight: 500; color: var(--ink-2); }
        .terminal-option[aria-pressed="true"] b { color: var(--ink); }
        .terminal-foot { display: flex; align-items: center; justify-content: space-between; gap: var(--s-4); flex-wrap: wrap; }
        .terminal-note { font-size: var(--t-15); color: var(--ink-2); margin: 0; }
        .terminal-warn { border-left: 3px solid var(--oxblood); background: var(--paper-2); padding: var(--s-3) var(--s-4); font-size: var(--t-15); }
        .terminal-verdict { font-family: var(--font-serif); font-size: var(--t-33); margin: 0; }
        .terminal-field { width: 100%; padding: var(--s-4) var(--s-5); border: 1px solid var(--rule); border-radius: var(--r-panel); background: var(--paper); color: var(--ink); font-family: var(--font-mono); font-size: var(--t-20); letter-spacing: 0.08em; text-transform: uppercase; }
        .terminal-field:focus-visible { outline: 2px solid var(--brass); outline-offset: 2px; }
        .terminal .btn { min-height: 52px; padding: 0 var(--s-8); font-size: var(--t-17); }
        @media (max-width: 640px) {
          .terminal-sheet { padding: var(--s-5); }
          .terminal-question { font-size: var(--t-20); }
          .terminal-option { font-size: var(--t-17); padding: var(--s-3) var(--s-4); }
        }
      `}</style>

      <div className="terminal-sheet">
        {phase === "idle" && (
          <>
            <div>
              <p className="terminal-count">Admissions</p>
              <h1 className="terminal-who" style={{ fontSize: "var(--t-33)" }}>Admission test</h1>
              <div className="terminal-rule" style={{ marginTop: "var(--s-3)" }} />
            </div>
            <p className="terminal-note">
              Enter the applicant number printed on your slip. Once the test begins the clock runs
              without pause, the screen goes full-screen, and you cannot return to a question you
              have answered.
            </p>
            {error && <div className="terminal-warn" role="alert">{error}</div>}
            <label>
              <span className="terminal-count">Applicant number</span>
              <input className="terminal-field" value={appId} onChange={e => setAppId(e.target.value)} placeholder="APP-2025F-BCS-XXXXXXXX" autoFocus />
            </label>
            <div className="terminal-foot">
              <p className="terminal-note">Call the invigilator if the number is not accepted.</p>
              <button className={`btn btn-primary${starting ? " btn-loading" : ""}`} disabled={!appId.trim() || starting} onClick={begin}>
                {starting ? "Starting" : "Begin the test"}
              </button>
            </div>
          </>
        )}

        {phase === "running" && test && (() => {
          const q = test.questions[current];
          return (
            <>
              {fsWarning && (
                <div className="terminal-warn" role="alert">
                  You have left full-screen. The clock is still running — return to full-screen and
                  carry on, and tell the invigilator that it happened.
                </div>
              )}
              <div className="terminal-head">
                <div>
                  <div className="terminal-who">{test.applicantName}</div>
                  <div className="terminal-count">Question {current + 1} of {test.totalQuestions} · {answered} answered</div>
                </div>
                <div style={{ textAlign: "right" }}>
                  <div className="terminal-clock" data-low={remaining < 60} aria-live="off">{mm}:{ss}</div>
                  <div className="terminal-count">{remaining < 60 ? "under a minute left" : "time remaining"}</div>
                </div>
              </div>
              <div className="terminal-gauge"><div style={{ width: `${((current + 1) / test.totalQuestions) * 100}%` }} /></div>

              <h1 className="terminal-question">{q.questionText}</h1>
              <div className="terminal-options">
                {q.options.map(o => (
                  <button
                    key={o.key}
                    type="button"
                    className="terminal-option"
                    aria-pressed={answers[q.questionId] === o.key}
                    onClick={() => choose(q, o.key)}
                  >
                    <b>{o.key}</b>
                    <span>{o.text}</span>
                  </button>
                ))}
              </div>

              <div className="terminal-foot">
                <p className="terminal-note">Your answer is recorded as you choose it. You cannot return to this question.</p>
                {current < test.totalQuestions - 1 ? (
                  <button className="btn btn-primary" disabled={!answers[q.questionId]} onClick={() => setCurrent(c => c + 1)}>Next question</button>
                ) : (
                  <button className="btn btn-primary" disabled={!answers[q.questionId]} onClick={() => submit(false)}>Finish and hand in</button>
                )}
              </div>
            </>
          );
        })()}

        {phase === "done" && (
          <>
            <div>
              <p className="terminal-count">Admissions</p>
              <h1 className="terminal-who" style={{ fontSize: "var(--t-33)" }}>Test complete</h1>
              <div className="terminal-rule" style={{ marginTop: "var(--s-3)" }} />
            </div>
            {error && <div className="terminal-warn" role="alert">{error}</div>}
            {passed !== null && (
              <p className="terminal-verdict" style={{ color: passed ? "var(--laurel)" : "var(--oxblood)" }}>
                {passed ? "Passed" : "Not passed"}
              </p>
            )}
            <p className="terminal-note">
              Your answers are recorded with the admissions office, which will write to you with the
              result in full. You may leave the terminal.
            </p>
          </>
        )}
      </div>
    </main>
  );
}
