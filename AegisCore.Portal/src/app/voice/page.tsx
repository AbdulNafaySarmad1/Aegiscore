"use client";

import { useState, type FormEvent } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel, Figures, Mark, Notice } from "@/components/register";
import { Ledger, words } from "@/components/register/ledger";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Session = { id: string; userRole: string; detectedLanguage: string; transcript: string | null; intent: string | null; responseText: string | null; asrConfidence: number | null; wasEscalated: boolean; escalationReason: string | null; createdAt: string };

/** The voice assistant: what was asked in Urdu or English, what it understood, and what it answered. */
export default function VoicePage() {
  const { date } = useTenantFormatter();
  const { state, reload } = useModuleData<Session[]>(signal => apiGet<Session[]>("/api/voice/sessions", signal));
  const action = useAction();
  const [text, setText] = useState("");
  const [language, setLanguage] = useState("English");
  const [answer, setAnswer] = useState<Session | null>(null);

  async function ask(e: FormEvent) {
    e.preventDefault();
    await action.run("ask", async () => {
      const session = await apiPost<Session>(`/api/voice/text?language=${encodeURIComponent(language)}`, text.trim());
      setAnswer(session);
      setText("");
      return session.responseText ?? "Answered.";
    }, reload);
  }

  return (
    <ModulePage
      title="Voice assistant"
      crumbs={[{ label: "Student services" }, { label: "Voice assistant" }]}
      meta={<span>Typed questions in Urdu or English. Audio transcription needs a speech provider, which this installation does not have.</span>}
      ident={(d: Session[]) => <><strong>{d.length}</strong><br />sessions</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {sessions => (
        <div className="record">
          <div className="record-body">
            <Figures aria-label="Voice figures" items={[
              { label: "Sessions", value: sessions.length, note: sessions.length === 0 ? "none yet" : `latest ${date(sessions[0].createdAt)}` },
              { label: "Escalated", value: sessions.filter(s => s.wasEscalated).length, tone: sessions.some(s => s.wasEscalated) ? "brass" : "ink", note: "handed to a person" },
              { label: "Languages", value: new Set(sessions.map(s => s.detectedLanguage)).size || "—", note: sessions.length === 0 ? "no sessions yet" : [...new Set(sessions.map(s => s.detectedLanguage))].join(", ") },
              { label: "Intents recognised", value: sessions.filter(s => s.intent).length, note: `of ${sessions.length} asked` },
            ]} />

            {answer && (
              <Notice tone="laurel" title={`Understood as: ${words(answer.intent)}`} role="status">
                {answer.responseText}
              </Notice>
            )}

            <Panel title="Sessions" aside={<span className="text-muted">most recent first</span>} flush>
              <Ledger rows={sessions} keyOf={s => s.id} minWidth={800} status={s => s.wasEscalated ? "brass" : "ink"}
                empty={{ title: "No question asked yet", detail: "Ask a question in the panel to the right; each session is recorded with what was understood and answered." }}
                columns={[
                  { key: "when", label: "Asked", render: s => date(s.createdAt), nowrap: true },
                  { key: "language", label: "Language", render: s => words(s.detectedLanguage) },
                  { key: "transcript", label: "Question", render: s => s.transcript ?? "—" },
                  { key: "intent", label: "Understood as", render: s => s.intent ? <Mark tone="ink">{words(s.intent)}</Mark> : "not recognised" },
                  { key: "response", label: "Answered", render: s => s.responseText ?? "—" },
                  { key: "escalated", label: "Escalated", render: s => s.wasEscalated ? (s.escalationReason ?? "yes") : "—" },
                ]} />
            </Panel>
          </div>

          <aside className="record-margin">
            <Panel title="Ask a question">
              <form className="panel-form" onSubmit={ask}>
                <label><span>Language</span>
                  <select value={language} onChange={e => setLanguage(e.target.value)}>
                    <option value="English">English</option>
                    <option value="Urdu">Urdu</option>
                    <option value="RomanUrdu">Roman Urdu</option>
                  </select>
                </label>
                <label><span>Question</span><textarea value={text} onChange={e => setText(e.target.value)} rows={3} placeholder="e.g. mera result kya hai?" required /></label>
                <button type="submit" className={`btn btn-primary${action.busy === "ask" ? " btn-loading" : ""}`} disabled={action.busy !== null || !text.trim()}>Ask</button>
              </form>
            </Panel>
          </aside>
        </div>
      )}
    </ModulePage>
  );
}
