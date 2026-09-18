"use client";
import { useCallback, useEffect, useState } from "react";
import { ApiError, apiPost } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { RegisterHead, Panel, Facts, Notice, ErrorNotice, RuledLoading, type Tone } from "@/components/register";
import { activateLicense, loadLicenseStatus, tierName, type LicenseStatus } from "@/lib/license";

type State = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; status: LicenseStatus };

const toneOf = (severity: LicenseStatus["severity"]): Tone => severity === "locked" ? "oxblood" : severity === "warning" ? "brass" : "laurel";

/**
 * The page an IT administrator lands on when the installation says it is read-only. It shows what
 * happened in plain words, exactly what to do, and takes the offline licence key the vendor issues.
 */
export default function LicensePage() {
  const { date } = useTenantFormatter();
  const [state, setState] = useState<State>({ kind: "loading" });
  const [retry, setRetry] = useState(0);
  const [key, setKey] = useState("");
  const [busy, setBusy] = useState<"activate" | "heartbeat" | null>(null);
  const [outcome, setOutcome] = useState<{ tone: Tone; text: string } | null>(null);

  const load = useCallback(async (signal?: AbortSignal): Promise<State> => {
    try { return { kind: "ready", status: await loadLicenseStatus(signal) }; }
    catch (error) { return { kind: "error", message: error instanceof ApiError ? error.message : "The licence status could not be read." }; }
  }, []);
  useEffect(() => {
    const abort = new AbortController();
    // eslint-disable-next-line react-hooks/set-state-in-effect -- reset on retry.
    setState({ kind: "loading" });
    load(abort.signal).then(next => { if (!abort.signal.aborted) setState(next); });
    return () => abort.abort();
  }, [load, retry]);

  async function activate() {
    if (state.kind !== "ready" || !state.status.universityId) return;
    setBusy("activate"); setOutcome(null);
    try {
      const result = await activateLicense(key, state.status.universityId);
      setState({ kind: "ready", status: result.status });
      setKey("");
      setOutcome({ tone: result.status.isLocked ? "oxblood" : "laurel", text: result.status.isLocked ? result.message : `Licence accepted. ${result.message}` });
    } catch (error) {
      setOutcome({ tone: "oxblood", text: error instanceof ApiError ? (error.status === 403 ? "Your role cannot activate a licence; a system administrator has to." : error.message) : "The key could not be submitted." });
    } finally { setBusy(null); }
  }

  async function checkNow() {
    setBusy("heartbeat"); setOutcome(null);
    try {
      await apiPost("/api/admin/license/heartbeat");
      const next = await load();
      setState(next);
      if (next.kind === "ready") setOutcome({ tone: toneOf(next.status.severity), text: next.status.message ?? "Checked." });
    } catch (error) {
      setOutcome({ tone: "oxblood", text: error instanceof ApiError ? error.message : "The check did not complete." });
    } finally { setBusy(null); }
  }

  const crumbs = [{ label: "Administration" }, { label: "Licence" }];
  if (state.kind === "loading") return <div className="page"><RuledLoading label="Reading the licence" rows={4} /></div>;
  if (state.kind === "error") return (
    <div className="page">
      <RegisterHead title="Installation licence" crumbs={crumbs} reveal={false} />
      <ErrorNotice message={state.message} hint="Check your connection and try again." onRetry={() => setRetry(n => n + 1)} />
    </div>
  );

  const { status } = state;
  const standing = status.isLocked ? "Read-only" : status.severity === "warning" ? "Writable, at risk" : "Active";
  return (
    <div className="page">
      <RegisterHead
        title="Installation licence"
        crumbs={crumbs}
        ident={<><strong>{standing}</strong><br />{status.universityName ?? "This installation"}</>}
        meta={<span>{status.isOffline ? "Offline licence, verified on this server." : "Licence verified with the vendor authority by a daily heartbeat."}</span>}
      />

      <Notice tone={toneOf(status.severity)} title={status.message ?? undefined} role={status.isLocked ? "alert" : "status"}>
        {status.nextStep}
      </Notice>

      {outcome && <Notice tone={outcome.tone} role="status">{outcome.text}</Notice>}

      <Panel title="Standing">
        <Facts items={[
          { label: "University id", value: status.universityId ?? "—", mono: true },
          { label: "Tier", value: tierName(status.tier) },
          { label: "Expires", value: status.expiryDate ? date(status.expiryDate) : "—" },
          { label: "Last successful heartbeat", value: status.lastHeartbeatAt ? date(status.lastHeartbeatAt) : "Never" },
          { label: "Consecutive failures", value: String(status.failedHeartbeatCount) },
          { label: "Read-only from", value: status.graceUntil ? date(status.graceUntil) : status.isLocked && status.lockedAt ? date(status.lockedAt) : "—" },
        ]} />
        <p className="text-muted">Quote the university id when asking the vendor for an offline licence key; a key is issued for one university id and one expiry date.</p>
      </Panel>

      <Panel title="Enter an offline licence key" aside={!status.isOffline && <button type="button" className={`btn btn-secondary btn-sm${busy === "heartbeat" ? " btn-loading" : ""}`} disabled={busy !== null} onClick={checkNow}>Check now</button>}>
        <label><span>Licence key from the vendor</span>
          <textarea value={key} onChange={e => setKey(e.target.value)} rows={4} spellCheck={false} placeholder="AEGIS-OFFLINE.…" aria-describedby="license-key-hint" />
        </label>
        <p id="license-key-hint" className="field-hint">Paste the whole key exactly as sent. It is verified on this server against the vendor&apos;s public key; nothing is transmitted.</p>
        <button type="button" className={`btn btn-primary${busy === "activate" ? " btn-loading" : ""}`} disabled={busy !== null || !key.trim() || !status.universityId} onClick={activate}>
          {busy === "activate" ? "Verifying key" : "Activate key"}
        </button>
      </Panel>
    </div>
  );
}
