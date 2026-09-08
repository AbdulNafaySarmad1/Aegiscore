"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet, apiPut } from "@/lib/api-client";

type ConfigView = { mode: "GoogleWorkspace" | "Microsoft365" | "Manual"; domainName: string | null; hasCredentials: boolean; updatedAt: string };

export default function EmailProvisioningPage() {
  const [mode, setMode] = useState<"GoogleWorkspace" | "Microsoft365" | "Manual">("Manual");
  const [domain, setDomain] = useState("");
  const [credentials, setCredentials] = useState("");
  const [hasCreds, setHasCreds] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    apiGet<ConfigView | "">("/api/admin/settings/email-provisioning")
      .then(v => { if (v) { setMode(v.mode); setDomain(v.domainName ?? ""); setHasCreds(v.hasCredentials); } })
      .catch(() => {});
  }, []);

  const save = async () => {
    setError(null); setMsg(null);
    try {
      const body: Record<string, unknown> = { mode, domainName: domain || null };
      if (credentials.trim()) body.credentialsJson = credentials.trim();
      const v = await apiPut<ConfigView>("/api/admin/settings/email-provisioning", body);
      setHasCreds(v.hasCredentials);
      setCredentials("");
      setMsg("Saved.");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Save failed.");
    }
  };

  return (
    <AppShell>
      <div className="content-page">
        <div className="page-header"><div><Link href="/admin/settings">← University settings</Link><h1>Institutional email provisioning</h1></div></div>
        {error && <div className="error-state">{error}</div>}
        {msg && <div className="form-notice">{msg}</div>}

        <div className="card">
          <div className="card-body" style={{ display: "grid", gap: 14, maxWidth: 560 }}>
            <label>Provisioning mode
              <select value={mode} onChange={e => setMode(e.target.value as typeof mode)} style={{ width: "100%" }}>
                <option value="Manual">Manual — admin enters each student email</option>
                <option value="GoogleWorkspace">Google Workspace — Admin SDK Directory API</option>
                <option value="Microsoft365">Microsoft 365 — MS Graph API</option>
              </select>
            </label>

            {mode !== "Manual" && (
              <>
                <label>University domain
                  <input value={domain} onChange={e => setDomain(e.target.value)} placeholder="university.edu.pk" style={{ width: "100%" }} />
                </label>
                <label>
                  {mode === "GoogleWorkspace" ? "Service account credentials JSON (with adminEmail)" : "App registration JSON (tenantId, clientId, clientSecret)"}
                  <textarea value={credentials} onChange={e => setCredentials(e.target.value)} rows={6} style={{ width: "100%" }}
                    placeholder={hasCreds ? "•••••• stored — paste new JSON to replace" : "{ ... }"} />
                </label>
                <p style={{ fontSize: 12, color: "var(--muted,#667)", margin: 0 }}>Credentials are encrypted at rest and never returned to the browser.</p>
              </>
            )}

            <div><button className="btn btn-primary" onClick={save}>Save</button></div>
          </div>
        </div>
      </div>
    </AppShell>
  );
}
