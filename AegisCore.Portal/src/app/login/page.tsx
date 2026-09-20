"use client";

import { useEffect, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { apiPost } from "@/lib/api-client";
import { loadLoginMethods, type LoginMethods } from "@/lib/session";
import { Eye, EyeOff, KeyRound } from "lucide-react";
import { Notice, InstitutionMark } from "@/components/register";
import { useInstitution } from "@/components/TenantPreferencesProvider";

// Demo accounts exist only when NEXT_PUBLIC_DEMO_MODE is set at build time; they never reach production.
const demoAccounts = [
  { email: "student@iobm.demo", password: "Koenigsegg$One1", label: "Student", initial: "S", desc: "Ali Hassan" },
  { email: "faculty@iobm.demo", password: "Pagani$Huayra3", label: "Faculty", initial: "F", desc: "Dr. Imran" },
  { email: "finance@iobm.demo", password: "Porsche$GT3RS", label: "Finance", initial: "£", desc: "Khalid Hussain" },
  { email: "registrar@iobm.demo", password: "Ferrari$Roma22", label: "Registrar", initial: "R", desc: "Dr. Sana" },
  { email: "vc@iobm.demo", password: "Lambo$Aventador1", label: "Vice Chancellor", initial: "V", desc: "Prof. Waseem" },
];

const offices = [
  { office: "Admissions", note: "Intakes, documents, entry tests, offers" },
  { office: "Examinations", note: "Results, transcripts, appeals" },
  { office: "Finance", note: "Fee demands, receipts, clearances" },
  { office: "Registrar", note: "Enrolment, graduation, alumni" },
  { office: "Compliance", note: "HEC and NBEAC evidence, audit trail" },
];

export default function LoginPage() {
  const isDemo = process.env.NEXT_PUBLIC_DEMO_MODE === "true";
  const router = useRouter();
  const [showPassword, setShowPassword] = useState(false);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  // Second step of a password sign-in for accounts with an authenticator app.
  const [challenge, setChallenge] = useState<string | null>(null);
  const [code, setCode] = useState("");
  // Which doors this university opened in the setup wizard. Until the answer arrives — and if it
  // never does — the password form is shown, as it always was.
  const [methods, setMethods] = useState<LoginMethods>({ localEnabled: true, providers: [] });
  const [methodsLoaded, setMethodsLoaded] = useState(isDemo);

  useEffect(() => {
    // A sign-in that went through an identity provider and failed comes back here with the reason.
    const params = new URLSearchParams(window.location.search);
    const ssoError = params.get("sso_error");
    if (ssoError) {
      const carried = params.get("message") || "Sign-in through your identity provider did not complete. Try again.";
      window.history.replaceState(null, "", "/login");
      // eslint-disable-next-line react-hooks/set-state-in-effect -- the reason arrives in the URL, not from a render
      setError(carried);
    }
    if (isDemo) return;
    loadLoginMethods()
      .then(value => setMethods(value))
      .catch(() => setMethods({ localEnabled: true, providers: [] }))
      .finally(() => setMethodsLoaded(true));
  }, [isDemo]);

  const institution = useInstitution();
  const today = new Intl.DateTimeFormat("en-GB", { weekday: "long", day: "numeric", month: "long", year: "numeric" }).format(new Date());

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(""); setBusy(true);
    try {
      if (!isDemo) {
        if (challenge) {
          await apiPost("/api/auth/login/mfa", { challengeToken: challenge, code });
        } else {
          const result = await apiPost<{ mfaRequired?: boolean; challengeToken?: string }>("/api/auth/login", { email, password });
          if (result?.mfaRequired && result.challengeToken) { setChallenge(result.challengeToken); setCode(""); return; }
        }
        localStorage.removeItem("aegiscore_demo_session");
        router.push("/"); router.refresh();
        return;
      }
      const account = demoAccounts.find(a => a.email === email && a.password === password);
      if (!account) { setError("That email and password do not match a demo account. Choose a role below instead."); return; }
      localStorage.setItem("aegiscore_demo_session", JSON.stringify(account));
      router.push("/");
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Sign-in did not complete. Check your email and password, then try again.");
    } finally { setBusy(false); }
  };

  const fillDemo = (acc: typeof demoAccounts[0]) => {
    localStorage.setItem("aegiscore_demo_session", JSON.stringify(acc));
    router.push("/");
  };

  return (
    <main id="main-content" className="gate" tabIndex={-1}>
      <aside className="gate-register" aria-label="About this register">
        <div className="gate-brand">
          <InstitutionMark imgSize={36} />
        </div>

        <div className="gate-folio">
          <span className="gate-folio-date">{today}</span>
          <h1>One register for the whole university.</h1>
          <p>Admissions, results, fees, clearances and graduation are recorded once, attributed to the person who recorded them, and trusted by every office.</p>
          <div className="gate-ledger" role="list">
            {offices.map(o => <div key={o.office} role="listitem"><strong>{o.office}</strong><em>{o.note}</em></div>)}
          </div>
        </div>

        <div className="gate-foot">
          <span>Records are held on the university&rsquo;s own infrastructure.</span>
          <strong>HEC and NBEAC ready</strong>
        </div>
      </aside>

      <section className="gate-form-panel">
        <div className="gate-form">
          <div className="gate-form-head">
            <h1>Sign in to the register</h1>
            <p>{methods.localEnabled
              ? methods.providers.length > 0 ? "Use your university account, or your email and password." : "Use your university email and password."
              : "Use your university account."}</p>
          </div>

          {methods.providers.length > 0 && (
            <div className="gate-sso" aria-label="Sign in with your university account">
              {methods.providers.map(provider => (
                // A plain link, not a fetch: the API answers with a redirect to the identity
                // provider, and the browser has to follow it.
                <a key={provider.type} className="btn btn-secondary btn-lg btn-block gate-sso-button" href={provider.startPath}>
                  <KeyRound size={18} aria-hidden="true" /> Continue with {provider.displayName}
                </a>
              ))}
              {methods.localEnabled && <div className="gate-sso-divider" aria-hidden="true"><span>or</span></div>}
            </div>
          )}

          {!methods.localEnabled && error && <div id="login-error"><Notice tone="oxblood" title={error} /></div>}

          {(methods.localEnabled || !methodsLoaded) && (
          <form onSubmit={handleSubmit} noValidate={false} aria-describedby={error ? "login-error" : undefined}>
            {challenge ? (
              <div className="field">
                <label htmlFor="login-code" className="field-label">Code from your authenticator app</label>
                <input id="login-code" inputMode="numeric" autoComplete="one-time-code" required autoFocus value={code}
                  onChange={e => setCode(e.target.value.trim())} aria-invalid={error ? true : undefined}
                  placeholder="6-digit code, or a backup code" />
                <p className="field-hint">Signed in as {email}. <button type="button" className="btn-link" onClick={() => { setChallenge(null); setCode(""); setError(""); }}>Use a different account</button></p>
              </div>
            ) : (<>
            <div className="field">
              <label htmlFor="login-email" className="field-label">University email</label>
              <input id="login-email" type="email" placeholder="name@university.edu.pk" autoComplete="email" required value={email} onChange={e => setEmail(e.target.value)} aria-invalid={error ? true : undefined} />
            </div>
            <div className="field">
              <label htmlFor="login-password" className="field-label">Password</label>
              <div className="password-field">
                <input id="login-password" type={showPassword ? "text" : "password"} autoComplete="current-password" required value={password} onChange={e => setPassword(e.target.value)} aria-invalid={error ? true : undefined} />
                <button type="button" className="password-toggle" onClick={() => setShowPassword(v => !v)} aria-label={showPassword ? "Hide password" : "Show password"} aria-pressed={showPassword}>
                  {showPassword ? <EyeOff size={18} aria-hidden="true" /> : <Eye size={18} aria-hidden="true" />}
                </button>
              </div>
            </div>

            <div className="gate-options">
              <label className="check-row"><input type="checkbox" name="remember" /><span>Keep me signed in on this device</span></label>
              <Link href="/activate">Activate a new account</Link>
            </div>
            </>)}

            {error && <div id="login-error"><Notice tone="oxblood" title={error} /></div>}

            <button type="submit" className={`btn btn-primary btn-lg btn-block${busy ? " btn-loading" : ""}`} disabled={busy}>{challenge ? "Verify code" : "Sign in"}</button>
          </form>
          )}

          {isDemo && (
            <div className="gate-demo">
              <div className="gate-demo-title"><strong>Demo roles</strong><span>Pilot instance. Choose a role to enter without a password.</span></div>
              <div className="gate-demo-grid">
                {demoAccounts.map(a => (
                  <button key={a.email} type="button" className="gate-demo-card" onClick={() => fillDemo(a)}>
                    <i aria-hidden="true">{a.initial}</i>
                    <span><strong>{a.label}</strong><small>{a.desc}</small></span>
                  </button>
                ))}
              </div>
            </div>
          )}

          <div className="gate-form-foot">
            <span>{institution.name ? `${institution.name}, on AegisCore` : "AegisCore Portal"}</span>
            <span>Need access? Ask your registrar&rsquo;s office.</span>
          </div>
        </div>
      </section>
    </main>
  );
}
