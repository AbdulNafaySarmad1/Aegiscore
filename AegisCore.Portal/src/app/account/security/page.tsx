"use client";
import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError, apiGet, apiPost } from "@/lib/api-client";
import { RegisterHead, Panel, Facts, Notice, ErrorNotice, RuledLoading, type Tone } from "@/components/register";

type MfaStatus = { enrolled: boolean; enabled: boolean; backupCodesLeft: number; requiredForRole: boolean };
type Enrolment = { secret: string; otpAuthUri: string; backupCodes: string[]; qrPngDataUri: string };
type State = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; status: MfaStatus };

/**
 * Where a person sets up the authenticator app on their phone. Administrators are sent here by the
 * API (403 mfa_required) until they have one; everyone else may add one. The secret is shown once,
 * as a QR code and as text; the backup codes are shown once and never again.
 */
export default function AccountSecurityPage() {
  const router = useRouter();
  const [state, setState] = useState<State>({ kind: "loading" });
  const [retry, setRetry] = useState(0);
  const [enrolment, setEnrolment] = useState<Enrolment | null>(null);
  const [code, setCode] = useState("");
  const [busy, setBusy] = useState(false);
  const [outcome, setOutcome] = useState<{ tone: Tone; text: string } | null>(null);

  const load = useCallback(async (): Promise<State> => {
    try { return { kind: "ready", status: await apiGet<MfaStatus>("/api/auth/mfa/status") }; }
    catch (error) { return { kind: "error", message: error instanceof ApiError ? error.message : "Your security settings could not be read." }; }
  }, []);
  useEffect(() => {
    let cancelled = false;
    // eslint-disable-next-line react-hooks/set-state-in-effect -- reset on retry.
    setState({ kind: "loading" });
    load().then(next => { if (!cancelled) setState(next); });
    return () => { cancelled = true; };
  }, [load, retry]);

  async function enroll() {
    setBusy(true); setOutcome(null);
    try { setEnrolment(await apiPost<Enrolment>("/api/auth/mfa/enroll")); setCode(""); }
    catch (error) { setOutcome({ tone: "oxblood", text: error instanceof ApiError ? error.message : "Enrolment could not start." }); }
    finally { setBusy(false); }
  }

  async function confirm() {
    setBusy(true); setOutcome(null);
    try {
      await apiPost("/api/auth/mfa/confirm", { code });
      setOutcome({ tone: "laurel", text: "Authenticator enabled. From now on, sign-in asks for a code from your app." });
      setState(await load());
      router.refresh();
    } catch (error) { setOutcome({ tone: "oxblood", text: error instanceof ApiError ? error.message : "The code could not be checked." }); }
    finally { setBusy(false); }
  }

  async function disable() {
    setBusy(true); setOutcome(null);
    try {
      await apiPost("/api/auth/mfa/disable", { code });
      setEnrolment(null); setCode("");
      setOutcome({ tone: "brass", text: "Authenticator removed. Sign-in is password-only again." });
      setState(await load());
    } catch (error) { setOutcome({ tone: "oxblood", text: error instanceof ApiError ? error.message : "The authenticator could not be removed." }); }
    finally { setBusy(false); }
  }

  const crumbs = [{ label: "Account" }, { label: "Security" }];
  if (state.kind === "loading") return <div className="page"><RuledLoading label="Reading your security settings" rows={3} /></div>;
  if (state.kind === "error") return (
    <div className="page">
      <RegisterHead title="Account security" crumbs={crumbs} reveal={false} />
      <ErrorNotice message={state.message} hint="Check your connection and try again." onRetry={() => setRetry(n => n + 1)} />
    </div>
  );

  const { status } = state;
  return (
    <div className="page">
      <RegisterHead
        title="Account security"
        crumbs={crumbs}
        ident={<><strong>{status.enabled ? "Authenticator app enabled" : "Password only"}</strong><br />{status.requiredForRole ? "Required for your role" : "Optional for your role"}</>}
        meta={<span>A code from an app on your phone is asked for at every sign-in, in addition to your password.</span>}
      />

      {status.requiredForRole && !status.enabled && (
        <Notice tone="oxblood" role="alert" title="Your role requires an authenticator app.">
          Until one is set up, the rest of the register is closed to this account. It takes about a minute.
        </Notice>
      )}
      {outcome && <Notice tone={outcome.tone} role="status">{outcome.text}</Notice>}

      <Panel title="Standing">
        <Facts items={[
          { label: "Authenticator app", value: status.enabled ? "Enabled" : status.enrolled ? "Set up, not yet confirmed" : "Not set up" },
          { label: "Backup codes left", value: status.enabled ? String(status.backupCodesLeft) : "—" },
          { label: "Required for your role", value: status.requiredForRole ? "Yes" : "No" },
        ]} />
      </Panel>

      {!status.enabled && !enrolment && (
        <Panel title="Set up an authenticator app">
          <p>Install any authenticator app (Microsoft Authenticator, Google Authenticator, Aegis, 1Password). Then:</p>
          <button type="button" className={`btn btn-primary${busy ? " btn-loading" : ""}`} disabled={busy} onClick={enroll}>Start set-up</button>
        </Panel>
      )}

      {enrolment && !status.enabled && (
        <Panel title="Scan, then confirm">
          <div style={{ display: "flex", gap: "var(--s-4)", flexWrap: "wrap", alignItems: "flex-start" }}>
            {/* Drawn by the API, not by a third-party QR service: the image holds the secret. */}
            {/* eslint-disable-next-line @next/next/no-img-element -- data URI, not an optimisable asset */}
            <img src={enrolment.qrPngDataUri} width={180} height={180} alt="QR code for your authenticator app" />
            <div>
              <p>If you cannot scan, enter this key by hand:</p>
              <code style={{ wordBreak: "break-all" }}>{enrolment.secret}</code>
            </div>
          </div>
          <p><strong>Backup codes</strong> — each works once if your phone is lost. Write them down now; they are not shown again.</p>
          <ul style={{ columns: 2, fontFamily: "var(--mono, monospace)" }}>
            {enrolment.backupCodes.map(c => <li key={c}>{c}</li>)}
          </ul>
          <label><span>Code shown in the app now</span>
            <input inputMode="numeric" autoComplete="one-time-code" value={code} onChange={e => setCode(e.target.value.trim())} placeholder="000000" />
          </label>
          <button type="button" className={`btn btn-primary${busy ? " btn-loading" : ""}`} disabled={busy || code.length < 6} onClick={confirm}>Confirm and enable</button>
        </Panel>
      )}

      {status.enabled && !status.requiredForRole && (
        <Panel title="Remove the authenticator">
          <label><span>Current code from the app, or a backup code</span>
            <input inputMode="numeric" autoComplete="one-time-code" value={code} onChange={e => setCode(e.target.value.trim())} />
          </label>
          <button type="button" className={`btn btn-secondary${busy ? " btn-loading" : ""}`} disabled={busy || code.length < 6} onClick={disable}>Remove</button>
        </Panel>
      )}
    </div>
  );
}
