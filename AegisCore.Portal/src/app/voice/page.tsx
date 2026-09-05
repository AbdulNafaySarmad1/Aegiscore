"use client";

import { PageErrorDisplay } from "@/components/ErrorDisplay";
import { useEffect, useState, useRef } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { usePersonalityTranslations } from "@/lib/personality-translator";
import {
  AlertCircle, CheckCircle2, Clock, FileAudio, Globe, Languages, Mic,
  RefreshCw, Send, Sparkles, Upload, Volume2
} from "lucide-react";

type Language = "urdu" | "roman-urdu" | "english" | "code-switched";

type SessionEntry = {
  id: string; language: Language; transcript: string;
  intent: string | null; response: string | null; timestamp: string;
  confidence: number;
};

type IntentResult = {
  intent: string; description: string; confidence: number;
};

const langLabel = (l: Language): string => {
  const map: Record<Language, string> = {
    urdu: "Urdu",
    "roman-urdu": "Roman Urdu",
    english: "English",
    "code-switched": "Code-Switched"
  };
  return map[l];
};

const confidenceBadge = (c: number) => {
  const color = c > 0.85 ? "var(--green)" : c > 0.6 ? "var(--amber)" : "var(--red)";
  const bg = c > 0.85 ? "var(--green-soft)" : c > 0.6 ? "var(--amber-soft)" : "var(--red-soft)";
  return <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: bg, color }}>{(c * 100).toFixed(0)}%</span>;
};

const inputS: React.CSSProperties = { minHeight: 42, padding: "0 11px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--ink)", background: "var(--canvas)", fontSize: "0.85rem", width: "100%", boxSizing: "border-box" };
const labelS: React.CSSProperties = { display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 };

export default function VoicePage() {
  const t = usePersonalityTranslations("academic");
  const audioRef = useRef<HTMLInputElement>(null);

  const [history, setHistory] = useState<SessionEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const [language, setLanguage] = useState<Language>("english");
  const [textInput, setTextInput] = useState("");
  const [sendingText, setSendingText] = useState(false);

  const [audioFile, setAudioFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);

  const [transcript, setTranscript] = useState<string | null>(null);
  const [intent, setIntent] = useState<IntentResult | null>(null);
  const [response, setResponse] = useState<string | null>(null);

  useEffect(() => {
    const ctrl = new AbortController();
    apiGet<SessionEntry[]>("/api/voice/sessions", ctrl.signal)
      .then(d => { setHistory(d); setError(null); })
      .catch(() => setError("Voice session history is temporarily unavailable."))
      .finally(() => setLoading(false));
    return () => { ctrl.abort(); };
  }, []);

  async function sendText(e: React.FormEvent) {
    e.preventDefault();
    if (!textInput.trim()) return;
    setSendingText(true); setMessage(null); setTranscript(null); setIntent(null); setResponse(null);
    try {
      const res = await apiPost<{ transcript: string; intent: IntentResult; response: string }>("/api/voice/text", { text: textInput, language });
      setTranscript(res.transcript);
      setIntent(res.intent);
      setResponse(res.response);
      const ctrl = new AbortController();
      const h = await apiGet<SessionEntry[]>("/api/voice/sessions", ctrl.signal);
      setHistory(h);
    } catch { setMessage("error: Text processing failed"); }
    finally { setSendingText(false); }
  }

  async function uploadAudio(e: React.FormEvent) {
    e.preventDefault();
    if (!audioFile) return;
    setUploading(true); setMessage(null); setTranscript(null); setIntent(null); setResponse(null);
    try {
      const form = new FormData();
      form.append("file", audioFile);
      form.append("language", language);
      const res = await apiPost<{ transcript: string; intent: IntentResult; response: string }>("/api/voice/audio", form);
      setTranscript(res.transcript);
      setIntent(res.intent);
      setResponse(res.response);
      setAudioFile(null);
      if (audioRef.current) audioRef.current.value = "";
      const ctrl = new AbortController();
      const h = await apiGet<SessionEntry[]>("/api/voice/sessions", ctrl.signal);
      setHistory(h);
    } catch { setMessage("error: Audio upload failed"); }
    finally { setUploading(false); }
  }

  if (loading) return (
    <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", gap: 16, color: "var(--muted)" }}>
      <div style={{ width: 40, height: 4, borderRadius: 99, overflow: "hidden", background: "var(--line)" }}>
        <div style={{ width: "50%", height: "100%", borderRadius: "inherit", background: "var(--blue-500)", animation: "launch 1s ease-in-out infinite alternate" }} />
      </div>
      Loading NidaBot voice assistant...
    </main>
  );

  if (error) return <PageErrorDisplay message={error} />;

  return (
    <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
      <header style={{ display: "flex", alignItems: "end", justifyContent: "space-between", gap: 20, marginBottom: 28 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>AI Interface</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>Voice Assistant (NidaBot)</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>Multilingual voice interface supporting Urdu, Roman Urdu, English, and Code-Switched for ERP operations</span>
        </div>
      </header>

      {message && (
        <div style={{ display: "flex", alignItems: "center", gap: 9, padding: "11px 14px", marginBottom: 18, border: message.startsWith("error") ? "1px solid #ecc0c6" : "1px solid #aaddc9", borderRadius: 10, color: message.startsWith("error") ? "var(--red)" : "var(--green)", background: message.startsWith("error") ? "var(--red-soft)" : "var(--green-soft)", fontSize: 11 }}>
          {message.startsWith("error") ? <AlertCircle /> : <CheckCircle2 />}{message.replace(/^[^:]+: /, "")}
        </div>
      )}

      <div style={{ display: "grid", gridTemplateColumns: "repeat(4, minmax(0, 1fr))", gap: 10, marginBottom: 18 }}>
        {(["urdu", "roman-urdu", "english", "code-switched"] as Language[]).map(l => (
          <button key={l} onClick={() => setLanguage(l)}
            style={{
              minHeight: 44, padding: "0 10px", border: language === l ? "2px solid var(--blue-600)" : "1px solid var(--line)",
              borderRadius: 10, background: language === l ? "var(--blue-100)" : "var(--surface)",
              color: language === l ? "var(--blue-600)" : "var(--ink)",
              cursor: "pointer", fontWeight: 700, fontSize: 11, display: "flex", alignItems: "center", justifyContent: "center", gap: 6
            }}>
            <Languages style={{ width: 14, opacity: language === l ? 1 : 0.5 }} />
            {langLabel(l)}
          </button>
        ))}
      </div>

      <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 1.2fr) minmax(360px, 0.8fr)", gap: 18, marginBottom: 18 }}>
        <div style={{ display: "grid", gap: 18, alignContent: "start" }}>
          <div className="surface" style={{ overflow: "hidden" }}>
            <div className="surface-heading"><div><h2>Text Input</h2><p>Type your query in {langLabel(language)}</p></div><Mic /></div>
            <form onSubmit={sendText} style={{ display: "flex", gap: 10, padding: 18 }}>
              <input
                placeholder={`Type in ${langLabel(language)}...`}
                value={textInput}
                onChange={e => setTextInput(e.target.value)}
                style={{ ...inputS, flex: 1 }}
              />
              <button type="submit" disabled={sendingText || !textInput.trim()}
                style={{ minHeight: 42, padding: "0 18px", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, display: "flex", alignItems: "center", gap: 6, opacity: sendingText || !textInput.trim() ? 0.6 : 1, whiteSpace: "nowrap" }}>
                <Send style={{ width: 14 }} /> {sendingText ? "Sending..." : "Send"}
              </button>
            </form>
          </div>

          <div className="surface" style={{ overflow: "hidden" }}>
            <div className="surface-heading"><div><h2>Audio Upload</h2><p>Upload .wav or .mp3 voice recording</p></div><FileAudio /></div>
            <form onSubmit={uploadAudio} style={{ padding: 18, display: "grid", gap: 12 }}>
              <label style={{ minHeight: 100, display: "grid", placeItems: "center", alignContent: "center", gap: 8, border: `2px dashed ${audioFile ? "var(--green)" : "var(--line-strong)"}`, borderRadius: 12, background: audioFile ? "var(--green-soft)" : "var(--surface-soft)", cursor: "pointer", transition: "all .2s" }}>
                <Volume2 style={{ width: 28, color: audioFile ? "var(--green)" : "var(--blue-600)", opacity: audioFile ? 1 : 0.5 }} />
                {audioFile ? <strong style={{ fontSize: 12 }}>{audioFile.name}</strong> : <><strong style={{ fontSize: 12 }}>Click to select audio</strong><small style={{ color: "var(--muted)", fontSize: 10 }}>.wav, .mp3 supported</small></>}
                <input ref={audioRef} type="file" accept=".wav,.mp3" style={{ display: "none" }} onChange={e => setAudioFile(e.target.files?.[0] ?? null)} />
              </label>
              <button type="submit" disabled={uploading || !audioFile}
                style={{ padding: "0.7rem", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, display: "flex", alignItems: "center", justifyContent: "center", gap: 7, opacity: uploading || !audioFile ? 0.6 : 1 }}>
                <Upload style={{ width: 14 }} /> {uploading ? "Uploading..." : "Upload & Process"}
              </button>
            </form>
          </div>

          {transcript && (
            <div className="surface" style={{ overflow: "hidden" }}>
              <div className="surface-heading"><div><h2>Transcript</h2><p>Detected speech-to-text output</p></div><Globe /></div>
              <div style={{ padding: 18 }}>
                <div style={{ padding: 14, borderRadius: 10, border: "1px solid var(--line)", background: "var(--surface-soft)", fontSize: 14, lineHeight: 1.6, fontStyle: "italic" }}>
                  {transcript}
                </div>
              </div>
            </div>
          )}
        </div>

        <div style={{ display: "grid", gap: 18, alignContent: "start" }}>
          {intent && (
            <div className="surface" style={{ overflow: "hidden" }}>
              <div className="surface-heading"><div><h2>Intent Classification</h2><p>Classified intent: {intent.intent}</p></div><Sparkles /></div>
              <div style={{ padding: 18, display: "grid", gap: 10 }}>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                  <span style={{ display: "grid", gap: 3 }}>
                    <strong style={{ fontSize: 14 }}>{intent.intent}</strong>
                    <small style={{ color: "var(--muted)", fontSize: 10 }}>{intent.description}</small>
                  </span>
                  {confidenceBadge(intent.confidence)}
                </div>
                {response && (
                  <div style={{ marginTop: 8, padding: 14, borderRadius: 10, border: "1px solid #c4d1f2", background: "var(--blue-100)" }}>
                    <div style={{ display: "flex", alignItems: "center", gap: 6, marginBottom: 6 }}>
                      <Sparkles style={{ width: 14, color: "var(--blue-600)" }} />
                      <strong style={{ fontSize: 11, color: "var(--blue-600)" }}>NidaBot Response</strong>
                    </div>
                    <p style={{ margin: 0, fontSize: 13, lineHeight: 1.6 }}>{response}</p>
                  </div>
                )}
              </div>
            </div>
          )}

          <div className="surface" style={{ overflow: "hidden" }}>
            <div className="surface-heading"><div><h2>Session History</h2><p>{history.length} previous interactions</p></div><Clock /></div>
            <div style={{ maxHeight: 500, overflow: "auto" }}>
              {history.length === 0 ? (
                <div style={{ minHeight: 160, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
                  <Mic style={{ width: 24, opacity: 0.5 }} />
                  <strong>No session history</strong>
                  <span>Send a text or audio query to start interacting with NidaBot</span>
                </div>
              ) : (
                <div style={{ padding: 6 }}>
                  {history.map(entry => (
                    <div key={entry.id} style={{ padding: "12px 10px", borderBottom: "1px solid var(--line)" }}>
                      <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 4 }}>
                        <span style={{ display: "inline-flex", padding: "2px 7px", borderRadius: 99, fontSize: 9, fontWeight: 700, background: "var(--blue-100)", color: "var(--blue-600)" }}>{langLabel(entry.language)}</span>
                        {entry.intent && <span style={{ display: "inline-flex", padding: "2px 7px", borderRadius: 99, fontSize: 9, fontWeight: 700, background: "var(--surface-soft)", color: "var(--muted)" }}>{entry.intent}</span>}
                        {confidenceBadge(entry.confidence)}
                      </div>
                      <p style={{ margin: "0 0 4px", fontSize: 11, color: "var(--ink)", fontStyle: "italic" }}>{entry.transcript}</p>
                      {entry.response && <p style={{ margin: 0, fontSize: 10, color: "var(--blue-600)" }}>{entry.response}</p>}
                      <small style={{ display: "block", marginTop: 4, color: "var(--subtle)", fontSize: 9 }}>{new Date(entry.timestamp).toLocaleString()}</small>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      </section>
    </main>
  );
}
