"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { completeSsoSignIn } from "@/lib/session";
import { Notice, InstitutionMark } from "@/components/register";

/**
 * Where an identity provider's sign-in lands. The API's callback sent the browser here with a
 * one-time code; exchanging it sets the session cookies, and the person continues to the portal.
 * Nothing on this page is interactive unless the exchange fails.
 */
export default function SsoCompletePage() {
  const [error, setError] = useState("");

  useEffect(() => {
    const code = new URLSearchParams(window.location.search).get("code") ?? "";
    window.history.replaceState(null, "", "/login/sso");
    // Resolve rather than set: the missing-code case is an outcome of the same exchange, reported the same way.
    (code ? completeSsoSignIn(code) : Promise.reject(new Error("This page needs a sign-in code from your identity provider. Start from the login page.")))
      .then(() => window.location.assign("/"))
      .catch(reason => setError(reason instanceof Error ? reason.message : "Sign-in did not complete. Start again from the login page."));
  }, []);

  return (
    <main id="main-content" className="gate" tabIndex={-1}>
      <section className="gate-form-panel" style={{ gridColumn: "1 / -1" }}>
        <div className="gate-form">
          <div className="gate-brand"><InstitutionMark imgSize={36} /></div>
          <div className="gate-form-head">
            <h1>{error ? "Sign-in did not complete" : "Signing you in"}</h1>
            <p aria-live="polite">{error ? "Your identity provider vouched for you, but the register could not finish the sign-in." : "One moment while the register opens your session."}</p>
          </div>
          {error && <Notice tone="oxblood" title={error} actions={<Link href="/login" className="btn btn-primary btn-sm">Back to sign-in</Link>} />}
        </div>
      </section>
    </main>
  );
}
