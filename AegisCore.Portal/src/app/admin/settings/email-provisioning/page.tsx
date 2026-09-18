"use client";

import { useState, type FormEvent } from "react";
import { apiGet, apiPut } from "@/lib/api-client";
import { Panel, Notice, Mark } from "@/components/register";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Mode = "GoogleWorkspace" | "Microsoft365" | "Manual";
type ConfigView = { mode: Mode; domainName: string | null; hasCredentials: boolean; updatedAt: string };

const MODES: { value: Mode; label: string; detail: string }[] = [
  { value: "Manual", label: "By hand", detail: "Someone types each student's institutional address when they are admitted. Nothing is created for you." },
  { value: "GoogleWorkspace", label: "Google Workspace", detail: "An account is created in your Workspace domain through the Admin SDK when a student is admitted." },
  { value: "Microsoft365", label: "Microsoft 365", detail: "An account is created in your Microsoft tenant through Graph when a student is admitted." },
];

/** Where a student's institutional email address comes from when they are admitted. */
export default function EmailProvisioningPage() {
  const { state, reload } = useModuleData<ConfigView | "">(signal => apiGet<ConfigView | "">("/api/admin/settings/email-provisioning", signal));
  const action = useAction();
  const [draft, setDraft] = useState<{ mode: Mode; domain: string; credentials: string } | null>(null);

  async function save(e: FormEvent, current: ConfigView | "") {
    e.preventDefault();
    const chosen = draft ?? { mode: (current || { mode: "Manual" as Mode }).mode, domain: (current || { domainName: "" }).domainName ?? "", credentials: "" };
    await action.run("save", async () => {
      const body: Record<string, unknown> = { mode: chosen.mode, domainName: chosen.domain || null };
      if (chosen.credentials.trim()) body.credentialsJson = chosen.credentials.trim();
      const saved = await apiPut<ConfigView>("/api/admin/settings/email-provisioning", body);
      setDraft({ mode: saved.mode, domain: saved.domainName ?? "", credentials: "" });
      return saved.mode === "Manual"
        ? "Saved. Addresses will be entered by hand when a student is admitted."
        : `Saved. An account will be created in ${chosen.domain || "your domain"} when a student is admitted.`;
    }, reload);
  }

  return (
    <ModulePage
      title="Institutional email"
      crumbs={[{ label: "Settings", href: "/admin/settings" }, { label: "Institutional email" }]}
      meta={<span>Where a student&apos;s university address comes from on the day they are admitted.</span>}
      state={state} reload={reload} notice={action.outcome}
    >
      {config => {
        const current = config || null;
        const form = draft ?? { mode: current?.mode ?? "Manual", domain: current?.domainName ?? "", credentials: "" };
        const chosen = MODES.find(m => m.value === form.mode)!;
        return (
          <div className="record">
            <div className="record-body">
              <Panel title="How addresses are made">
                <form className="panel-form" onSubmit={e => save(e, config)}>
                  <label><span>Provisioning</span>
                    <select value={form.mode} onChange={e => setDraft({ ...form, mode: e.target.value as Mode })}>
                      {MODES.map(m => <option key={m.value} value={m.value}>{m.label}</option>)}
                    </select>
                  </label>
                  <p className="field-hint">{chosen.detail}</p>

                  {form.mode !== "Manual" && (
                    <>
                      <label><span>Your domain</span><input value={form.domain} onChange={e => setDraft({ ...form, domain: e.target.value })} placeholder="university.edu.pk" /></label>
                      <label>
                        <span>{form.mode === "GoogleWorkspace" ? "Service account credentials (JSON, including adminEmail)" : "App registration (JSON: tenantId, clientId, clientSecret)"}</span>
                        <textarea value={form.credentials} onChange={e => setDraft({ ...form, credentials: e.target.value })} rows={7}
                          placeholder={current?.hasCredentials ? "Credentials are stored. Paste new JSON only to replace them." : "{ … }"} />
                      </label>
                      <p className="field-hint">
                        Credentials are encrypted before they are written down and are never sent back to a browser,
                        which is why the box above is empty even when credentials are held.
                      </p>
                    </>
                  )}

                  <button type="submit" className={`btn btn-primary${action.busy === "save" ? " btn-loading" : ""}`} disabled={action.busy !== null}>Save</button>
                </form>
              </Panel>
            </div>

            <aside className="record-margin">
              <Panel title="As it stands">
                {current ? (
                  <ul className="notice-list">
                    <li><Mark tone="ink">{MODES.find(m => m.value === current.mode)?.label}</Mark></li>
                    <li>{current.domainName ? `Domain: ${current.domainName}` : "No domain set"}</li>
                    <li>{current.hasCredentials ? <Mark tone="laurel">Credentials held</Mark> : <Mark tone="brass">No credentials</Mark>}</li>
                  </ul>
                ) : (
                  <p>Nothing has been configured, so addresses are entered by hand.</p>
                )}
              </Panel>

              {form.mode !== "Manual" && !current?.hasCredentials && (
                <Notice tone="brass" title="Credentials are needed before this works.">
                  Until they are saved, admitting a student will not create an account, and whoever admits
                  them will be asked for an address instead.
                </Notice>
              )}
            </aside>
          </div>
        );
      }}
    </ModulePage>
  );
}
