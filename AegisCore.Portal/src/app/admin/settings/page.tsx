"use client";
import { useEffect, useState } from "react";
import Link from "next/link";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet } from "@/lib/api-client";

export default function SettingsPage() {
  const [settings, setSettings] = useState<Record<string, unknown>>({});
  const [error, setError] = useState("");
  useEffect(() => { apiGet<Record<string, unknown>>("/api/config/portal-preferences").then(setSettings).catch(reason => setError(reason.message)); }, []);
  return <AppShell><div className="content-page"><h1>University settings</h1>
    {error && <p role="alert">{error}</p>}<section className="card" style={{ padding: 24 }}><h2>Portal preferences</h2><dl>{Object.entries(settings).map(([key, value]) => <div key={key}><dt>{key}</dt><dd>{String(value)}</dd></div>)}</dl>
    <Link href="/admin/campus">Manage campus boundaries</Link><br /><Link href="/setup">Open secure installation settings</Link></section>
  </div></AppShell>;
}
