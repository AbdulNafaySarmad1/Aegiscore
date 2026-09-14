"use client";
import { useEffect, useState } from "react";

export default function ActivatePage() {
  const [token, setToken] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  useEffect(() => {
    setToken(location.hash.slice(1));
    history.replaceState(null, "", location.pathname);
  }, []);
  async function submit(event: React.FormEvent) {
    event.preventDefault(); setBusy(true);
    try {
      const response = await fetch(`/api/auth/activation/${token ? "complete" : "request"}`, {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify(token ? { token, password } : { email }),
      });
      const body = await response.json();
      if (!response.ok) throw new Error(body.error ?? "Activation is unavailable. Please retry.");
      setMessage(token ? "Password saved. Sign in with your institutional email." : body.message);
      setPassword("");
    } catch (error) { setMessage(error instanceof Error ? error.message : "Activation failed."); }
    finally { setBusy(false); }
  }
  return <main style={{ maxWidth: 520, margin: "64px auto", padding: 24 }}>
    <h1>Activate your student account</h1>
    <p>After admission, request a link using your personal or institutional email. The link is delivered to your verified personal email.</p>
    <form onSubmit={submit}>
      {token ? <label>New password<input type="password" autoComplete="new-password" minLength={16} maxLength={128} required value={password} onChange={e => setPassword(e.target.value)} /></label>
        : <label>Email<input type="email" autoComplete="email" required value={email} onChange={e => setEmail(e.target.value)} /></label>}
      <button disabled={busy}>{token ? "Set password" : "Send activation link"}</button>
    </form>
    <p role="status">{message}</p><a href="/login">Sign in</a>
  </main>;
}
