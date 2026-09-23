"use client";
import { useCallback, useEffect, useState } from "react";
import { ApiError, apiGet, apiPut } from "@/lib/api-client";
import { RegisterHead, Panel, Facts, Notice, ErrorNotice, RuledLoading, type Tone } from "@/components/register";

type Contact = {
  name: string | null; role: string | null; email: string | null; phone: string | null;
  postalAddress: string | null; deputyName: string | null; deputyEmail: string | null;
};
type Residency = {
  deploymentModel: string | null; databaseLocation: string | null; fileStorageLocation: string | null;
  backupLocation: string | null; replicaLocation: string | null; approvedForProduction: boolean; notes: string | null;
};
type Retention = {
  auditRetentionYears: number; writeAuditRetentionYears: number; loginAuditRetentionDays: number;
  visitorRetentionDays: number; applicantRetentionYears: number; studentRecordRetentionYears: number;
  welfareRetentionYears: number; staffRetentionYears: number; financialRetentionYears: number;
  recruitmentRetentionYears: number;
};
type Settings = { contact: Contact; residency: Residency; retention: Retention };
type Gap = { setting: string; why: string; document: string };
type Payload = { settings: Settings; gaps: Gap[] };
type State = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; data: Payload };

const RETENTION_FIELDS: { key: keyof Retention; label: string; unit: string; hint: string }[] = [
  { key: "studentRecordRetentionYears", label: "Student records after graduation or withdrawal", unit: "years", hint: "The academic register itself is kept permanently; this is the identifiable record around it." },
  { key: "welfareRetentionYears", label: "Counselling, discipline and mental-health records", unit: "years", hint: "From the last session or the closure of the case." },
  { key: "staffRetentionYears", label: "Staff records after leaving", unit: "years", hint: "Employment, contracts, leave, performance." },
  { key: "financialRetentionYears", label: "Fees, payments and payroll", unit: "years", hint: "Usually set by tax and companies legislation rather than by policy." },
  { key: "applicantRetentionYears", label: "Unsuccessful and withdrawn applicants", unit: "years", hint: "Enforced: the worker deletes these profiles and their documents." },
  { key: "recruitmentRetentionYears", label: "Unsuccessful job candidates", unit: "years", hint: "From the hiring decision." },
  { key: "auditRetentionYears", label: "Access audit and consent records", unit: "years", hint: "Enforced nightly." },
  { key: "writeAuditRetentionYears", label: "Change audit", unit: "years", hint: "Enforced nightly." },
  { key: "loginAuditRetentionDays", label: "Sign-in records (IP, location, device)", unit: "days", hint: "Enforced nightly." },
  { key: "visitorRetentionDays", label: "Visitor and gate records", unit: "days", hint: "Enforced nightly. These hold visitors' CNICs." },
];

/**
 * Three decisions the software cannot make for an institution: who is accountable for personal
 * data, where that data physically sits, and how long each category is kept. The product ships
 * starting values for the periods and nothing at all for the first two — a supplier naming a
 * university's data protection officer would be worse than leaving it blank. Changed here at any
 * time; the nightly retention run reads whatever is current.
 */
export default function CompliancePage() {
  const [state, setState] = useState<State>({ kind: "loading" });
  const [retry, setRetry] = useState(0);
  const [contact, setContact] = useState<Contact | null>(null);
  const [residency, setResidency] = useState<Residency | null>(null);
  const [retention, setRetention] = useState<Retention | null>(null);
  const [busy, setBusy] = useState<string | null>(null);
  const [outcome, setOutcome] = useState<{ tone: Tone; text: string } | null>(null);

  const load = useCallback(async (): Promise<State> => {
    try { return { kind: "ready", data: await apiGet<Payload>("/api/admin/compliance-settings") }; }
    catch (error) { return { kind: "error", message: error instanceof ApiError ? error.message : "The compliance settings could not be read." }; }
  }, []);

  useEffect(() => {
    let cancelled = false;
    // eslint-disable-next-line react-hooks/set-state-in-effect -- reset on retry.
    setState({ kind: "loading" });
    load().then(next => {
      if (cancelled) return;
      setState(next);
      if (next.kind === "ready") {
        setContact(next.data.settings.contact);
        setResidency(next.data.settings.residency);
        setRetention(next.data.settings.retention);
      }
    });
    return () => { cancelled = true; };
  }, [load, retry]);

  async function save(section: string, body: object) {
    setBusy(section); setOutcome(null);
    try {
      const data = await apiPut<Payload>("/api/admin/compliance-settings", body);
      setState({ kind: "ready", data });
      setContact(data.settings.contact);
      setResidency(data.settings.residency);
      setRetention(data.settings.retention);
      setOutcome({ tone: "laurel", text: `${section} saved. The nightly run uses these values from tonight.` });
    } catch (error) {
      setOutcome({ tone: "oxblood", text: error instanceof ApiError ? error.message : "The change could not be saved." });
    } finally { setBusy(null); }
  }

  const crumbs = [{ label: "Administration" }, { label: "Compliance" }];
  if (state.kind === "loading") return <div className="page"><RuledLoading label="Reading the compliance settings" rows={4} /></div>;
  if (state.kind === "error") return (
    <div className="page">
      <RegisterHead title="Compliance" crumbs={crumbs} reveal={false} />
      <ErrorNotice message={state.message} hint="You need the Settings permission to open this page." onRetry={() => setRetry(n => n + 1)} />
    </div>
  );
  if (!contact || !residency || !retention) return null;

  const { gaps } = state.data;
  return (
    <div className="page">
      <RegisterHead
        title="Compliance"
        crumbs={crumbs}
        ident={<><strong>{gaps.length === 0 ? "Recorded" : `${gaps.length} to record`}</strong><br />{contact.name ?? "No contact named"}</>}
        meta={<span>Who is accountable, where the data lives, and how long records are kept. These are this institution&apos;s decisions, not the supplier&apos;s, and may be changed whenever policy or law does.</span>}
      />

      {gaps.length > 0 && (
        <Notice tone="brass" role="status" title="Not yet recorded">
          <ul>{gaps.map(gap => <li key={gap.setting}><strong>{gap.setting}</strong> — {gap.why} <em>({gap.document})</em></li>)}</ul>
        </Notice>
      )}
      {outcome && <Notice tone={outcome.tone} role="status">{outcome.text}</Notice>}

      <Panel title="Data protection contact">
        <p className="text-muted">The person a student, a parent, a regulator or an auditor writes to. Published in the privacy notice and used in breach correspondence.</p>
        <label><span>Name</span><input value={contact.name ?? ""} onChange={e => setContact({ ...contact, name: e.target.value })} /></label>
        <label><span>Role or office</span><input value={contact.role ?? ""} placeholder="Registrar, Data Protection Officer, Head of IT" onChange={e => setContact({ ...contact, role: e.target.value })} /></label>
        <label><span>Email</span><input type="email" value={contact.email ?? ""} onChange={e => setContact({ ...contact, email: e.target.value })} /></label>
        <label><span>Phone</span><input value={contact.phone ?? ""} onChange={e => setContact({ ...contact, phone: e.target.value })} /></label>
        <label><span>Postal address</span><input value={contact.postalAddress ?? ""} onChange={e => setContact({ ...contact, postalAddress: e.target.value })} /></label>
        <label><span>Deputy name</span><input value={contact.deputyName ?? ""} onChange={e => setContact({ ...contact, deputyName: e.target.value })} /></label>
        <label><span>Deputy email</span><input type="email" value={contact.deputyEmail ?? ""} onChange={e => setContact({ ...contact, deputyEmail: e.target.value })} /></label>
        <button type="button" className={`btn btn-primary${busy === "Contact" ? " btn-loading" : ""}`} disabled={busy !== null} onClick={() => save("Contact", { contact })}>Save contact</button>
      </Panel>

      <Panel title="Where the data lives">
        <p className="text-muted">Recorded so the cross-border transfer question can be answered without guessing. Nothing here changes where data goes — it records where your infrastructure already is.</p>
        <label><span>Deployment model</span><input value={residency.deploymentModel ?? ""} placeholder="On-premise, university data centre, private cloud" onChange={e => setResidency({ ...residency, deploymentModel: e.target.value })} /></label>
        <label><span>Database location</span><input value={residency.databaseLocation ?? ""} placeholder="City, country" onChange={e => setResidency({ ...residency, databaseLocation: e.target.value })} /></label>
        <label><span>File storage location</span><input value={residency.fileStorageLocation ?? ""} onChange={e => setResidency({ ...residency, fileStorageLocation: e.target.value })} /></label>
        <label><span>Backup location</span><input value={residency.backupLocation ?? ""} onChange={e => setResidency({ ...residency, backupLocation: e.target.value })} /></label>
        <label><span>Read-replica or WAL archive location</span><input value={residency.replicaLocation ?? ""} onChange={e => setResidency({ ...residency, replicaLocation: e.target.value })} /></label>
        <label><span>Notes</span><textarea rows={2} value={residency.notes ?? ""} onChange={e => setResidency({ ...residency, notes: e.target.value })} /></label>
        <label className="check-row">
          <input type="checkbox" checked={residency.approvedForProduction} onChange={e => setResidency({ ...residency, approvedForProduction: e.target.checked })} />
          <span>This location is approved by the institution for production personal data</span>
        </label>
        <button type="button" className={`btn btn-primary${busy === "Residency" ? " btn-loading" : ""}`} disabled={busy !== null} onClick={() => save("Residency", { residency })}>Save location</button>
      </Panel>

      <Panel title="How long records are kept">
        <p className="text-muted">
          Your registrar and legal adviser own these periods; the values below are only a starting point.
          The ones marked <em>enforced nightly</em> are deleted automatically — the rest are published in the
          retention notice and applied by your own procedures.
        </p>
        {RETENTION_FIELDS.map(field => (
          <label key={field.key}>
            <span>{field.label} <small className="text-muted">({field.unit})</small></span>
            <input type="number" min={1} value={retention[field.key]}
              onChange={e => setRetention({ ...retention, [field.key]: Number(e.target.value) || 1 })} />
            <small className="field-hint">{field.hint}</small>
          </label>
        ))}
        <button type="button" className={`btn btn-primary${busy === "Retention" ? " btn-loading" : ""}`} disabled={busy !== null} onClick={() => save("Retention", { retention })}>Save periods</button>
      </Panel>

      <Panel title="In effect now">
        <Facts items={[
          { label: "Contact", value: contact.name ? `${contact.name}${contact.role ? ` (${contact.role})` : ""}` : "Not named" },
          { label: "Database", value: residency.databaseLocation ?? "Not recorded" },
          { label: "Approved for production", value: residency.approvedForProduction ? "Yes" : "No" },
          { label: "Sign-in records", value: `${retention.loginAuditRetentionDays} days` },
          { label: "Access audit", value: `${retention.auditRetentionYears} years` },
        ]} />
      </Panel>
    </div>
  );
}
