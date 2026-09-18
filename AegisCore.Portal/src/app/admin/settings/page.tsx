"use client";

import { useState, type FormEvent } from "react";
import Link from "next/link";
import { apiGet, apiPost } from "@/lib/api-client";
import { Panel, Facts, Notice } from "@/components/register";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Preferences = {
  locale: string; currency: string; paymentGateway: string; accreditorName: string;
  identityDocumentName: string; universityName: string; universityShortName: string; sealUrl: string;
};

/**
 * What the institution is called and how it writes things down. Every one of these appears on a
 * screen or a printed document, so the field says where it shows up rather than naming a config key.
 */
const SETTINGS: { key: string; domain: string; label: string; hint: string; field: keyof Preferences }[] = [
  { key: "identity.university_name", domain: "Identity", label: "Institution name", hint: "Printed in full on transcripts, letters and the sign-in screen.", field: "universityName" },
  { key: "identity.university_short_name", domain: "Identity", label: "Short name", hint: "Used where the full name will not fit, such as the navigation rail.", field: "universityShortName" },
  { key: "locale.language", domain: "System", label: "Language and region", hint: "Decides how dates and numbers are written, e.g. en-PK.", field: "locale" },
  { key: "locale.currency", domain: "Financial", label: "Currency", hint: "The currency fees and payments are shown in, e.g. PKR.", field: "currency" },
  { key: "payment.default_gateway", domain: "Financial", label: "Default payment gateway", hint: "Which gateway a fee challan is issued against unless another is chosen.", field: "paymentGateway" },
  { key: "academic.accreditor_name", domain: "Academic", label: "Accrediting body", hint: "Named on accreditation reports and readiness evidence.", field: "accreditorName" },
  { key: "identity.document_name", domain: "Identity", label: "National identity document", hint: "What the identity document is called where you are, e.g. CNIC.", field: "identityDocumentName" },
];

export default function SettingsPage() {
  const { state, reload } = useModuleData<Preferences>(signal => apiGet<Preferences>("/api/config/portal-preferences", signal));
  const action = useAction();
  const [edits, setEdits] = useState<Record<string, string>>({});

  async function save(e: FormEvent, preferences: Preferences) {
    e.preventDefault();
    const changed = SETTINGS.filter(s => edits[s.key] !== undefined && edits[s.key] !== String(preferences[s.field] ?? ""));
    if (changed.length === 0) return;
    await action.run("save", async () => {
      for (const setting of changed) {
        await apiPost("/api/config", {
          domain: setting.domain,
          key: setting.key,
          valueJson: JSON.stringify(edits[setting.key]),
          description: setting.label,
        });
      }
      setEdits({});
      return changed.length === 1
        ? `${changed[0].label} is updated. It changes everywhere it appears.`
        : `${changed.length} settings updated. They change everywhere they appear.`;
    }, reload);
  }

  return (
    <ModulePage
      title="University settings"
      crumbs={[{ label: "Settings" }]}
      meta={<span>What this installation calls itself, and the conventions it writes in.</span>}
      state={state} reload={reload} notice={action.outcome}
    >
      {preferences => (
        <div className="record">
          <div className="record-body">
            <Panel title="Identity and conventions">
              <form className="panel-form" onSubmit={e => save(e, preferences)}>
                {SETTINGS.map(setting => (
                  <label key={setting.key}>
                    <span>{setting.label}</span>
                    <input
                      value={edits[setting.key] ?? String(preferences[setting.field] ?? "")}
                      onChange={e => setEdits({ ...edits, [setting.key]: e.target.value })}
                    />
                    <small className="field-hint">{setting.hint}</small>
                  </label>
                ))}
                <button type="submit" className={`btn btn-primary${action.busy === "save" ? " btn-loading" : ""}`} disabled={action.busy !== null || Object.keys(edits).length === 0}>
                  Save changes
                </button>
              </form>
            </Panel>
          </div>

          <aside className="record-margin">
            <Panel title="As the portal reads it">
              <Facts items={[
                { label: "Institution", value: preferences.universityName },
                { label: "Short name", value: preferences.universityShortName, mono: true },
                { label: "Locale", value: preferences.locale, mono: true },
                { label: "Currency", value: preferences.currency, mono: true },
                { label: "Seal", value: preferences.sealUrl ? "Configured" : "None set" },
              ]} />
            </Panel>

            <Panel title="Elsewhere">
              <ul className="notice-list">
                <li><Link href="/admin/campus">Campus boundaries</Link> — where attendance inside a geofence counts.</li>
                <li><Link href="/admin/settings/email-provisioning">Institutional email</Link> — where a student&apos;s address comes from.</li>
                <li><Link href="/admin/license">Licence</Link> — what this installation is licensed to do.</li>
              </ul>
            </Panel>

            <Notice tone="ink">
              These can also be set before first start, as environment variables, so a new container
              comes up already carrying the institution&apos;s name.
            </Notice>
          </aside>
        </div>
      )}
    </ModulePage>
  );
}
