"use client";

import { useState, useEffect } from "react";
import { apiGet } from "@/lib/api-client";

type ModulePageProps = {
  title: string;
  description: string;
  apiPath: string;
  metricKeys: string[];
  metricLabels: string[];
};

function statColor(label: string): string {
  if (label.toLowerCase().includes("resolved") || label.toLowerCase().includes("success") || label.toLowerCase().includes("cleared") || label.toLowerCase().includes("green")) return "var(--green)";
  if (label.toLowerCase().includes("warning") || label.toLowerCase().includes("pending") || label.toLowerCase().includes("review") || label.toLowerCase().includes("amber") || label.toLowerCase().includes("yellow")) return "var(--amber)";
  if (label.toLowerCase().includes("critical") || label.toLowerCase().includes("error") || label.toLowerCase().includes("red") || label.toLowerCase().includes("failed")) return "var(--red)";
  return "var(--ink)";
}

export function ModuleDashboard({ title, description, apiPath, metricKeys, metricLabels }: ModulePageProps) {
  const [data, setData] = useState<Record<string, number>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    apiGet<Record<string, number>>(apiPath)
      .then(d => { setData(d); setLoading(false); })
      .catch(() => { setError("Data unavailable"); setLoading(false); });
  }, [apiPath]);

  if (error) {
    return (
      <main style={{ padding: "1.5rem", maxWidth: 1100, margin: "0 auto" }}>
        <div style={{ background: "var(--surface)", borderRadius: 12, border: "1px solid var(--line)", padding: "2rem", textAlign: "center" }}>
          <p style={{ color: "var(--muted)" }}>{error}</p>
        </div>
      </main>
    );
  }

  return (
    <main style={{ padding: "1.5rem", maxWidth: 1100, margin: "0 auto" }}>
      <div style={{ marginBottom: "1.5rem" }}>
        <h1 style={{ color: "var(--ink)", fontSize: "1.5rem", fontWeight: 600, margin: "0 0 0.25rem 0" }}>{title}</h1>
        <p style={{ color: "var(--muted)", fontSize: "0.875rem", margin: 0 }}>{description}</p>
      </div>
      {loading ? (
        <div style={{ background: "var(--surface)", borderRadius: 12, border: "1px solid var(--line)", padding: "2rem", textAlign: "center" }}>
          <div style={{ color: "var(--muted)" }}>Loading...</div>
        </div>
      ) : (
        <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(220px, 1fr))", gap: "1rem" }}>
          {metricKeys.map((key, i) => (
            <div key={key} style={{ background: "var(--surface)", borderRadius: 12, border: "1px solid var(--line)", padding: "1.25rem" }}>
              <span style={{ color: "var(--muted)", fontSize: "0.8rem", display: "block", marginBottom: "0.25rem" }}>{metricLabels[i]}</span>
              <div style={{ fontSize: "2rem", fontWeight: 700, color: statColor(metricLabels[i]) }}>
                {data[key] ?? "-"}
              </div>
            </div>
          ))}
        </div>
      )}
    </main>
  );
}
