"use client";
import { useEffect, useState, type FormEvent } from "react";
import Link from "next/link";
import { Notice, InstitutionMark } from "@/components/register";

export default function ActivatePage() {
  const [token, setToken] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [result, setResult] = useState<{ tone: "laurel" | "oxblood"; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    // The activation token arrives in the URL fragment; it is read once and removed from history.
    // eslint-disable-next-line react-hooks/set-state-in-effect -- one-time read of the fragment.
    setToken(location.hash.slice(1));
    history.replaceState(null, "", location.pathname);
  }, []);

  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setResult(null);
    try {
      const response = await fetch(`/api/auth/activation/${token ? "complete" : "request"}`, {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify(token ? { token, password } : { email }),
      });
      const body = await response.json().catch(() => ({}));
      if (!response.ok) throw new Error(body.error ?? "Activation is not available right now. Try again in a few minutes.");
      setResult({ tone: "laurel", text: token ? "Password saved. Sign in with your institutional email." : body.message ?? "If that address is on file, an activation link has been sent to your verified personal email." });
      setPassword("");
    } catch (error) { setResult({ tone: "oxblood", text: error instanceof Error ? error.message : "Activation did not complete." }); }
    finally { setBusy(false); }
  }

  return (
    <main id="main-content" className="gate" tabIndex={-1}>
      <aside className="gate-register" aria-label="About activation">
        <div className="gate-brand">
          <InstitutionMark imgSize={36} />
        </div>
        <div className="gate-folio">
          <h1>Your account is created when you are admitted.</h1>
          <p>The activation link goes to the personal email you verified during your application. Once you set a password, sign in with your institutional email.</p>
          <div className="gate-ledger" role="list">
            <div role="listitem"><strong>Request a link</strong><em>Enter the personal or institutional email on your application</em></div>
            <div role="listitem"><strong>Open the link</strong><em>It is valid for a limited time and works once</em></div>
            <div role="listitem"><strong>Set a password</strong><em>At least 16 characters; use a passphrase you can remember</em></div>
          </div>
        </div>
        <div className="gate-foot"><span>Links are sent from the university&rsquo;s own mail domain.</span></div>
      </aside>

      <section className="gate-form-panel">
        <div className="gate-form">
          <div className="gate-form-head">
            <h1>{token ? "Set your password" : "Activate your student account"}</h1>
            <p>{token ? "Choose a password for your institutional account." : "Request an activation link using the email on your application."}</p>
          </div>

          <form onSubmit={submit}>
            {token ? (
              <div className="field">
                <label htmlFor="activate-password" className="field-label">New password</label>
                <input id="activate-password" type="password" autoComplete="new-password" minLength={16} maxLength={128} required value={password} onChange={e => setPassword(e.target.value)} aria-describedby="activate-password-hint" />
                <span id="activate-password-hint" className="field-hint">16 to 128 characters. A sentence you will remember works better than symbols.</span>
              </div>
            ) : (
              <div className="field">
                <label htmlFor="activate-email" className="field-label">Email</label>
                <input id="activate-email" type="email" autoComplete="email" required value={email} onChange={e => setEmail(e.target.value)} placeholder="name@example.com" />
                <span className="field-hint">Personal or institutional email from your application.</span>
              </div>
            )}
            {result && <Notice tone={result.tone} title={result.text} />}
            <button type="submit" className={`btn btn-primary btn-lg btn-block${busy ? " btn-loading" : ""}`} disabled={busy}>{token ? "Set password" : "Send activation link"}</button>
          </form>

          <div className="gate-form-foot">
            <span>Already activated?</span>
            <Link href="/login">Sign in</Link>
          </div>
        </div>
      </section>
    </main>
  );
}
