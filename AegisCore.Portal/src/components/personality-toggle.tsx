"use client";

import { useState, useEffect } from "react";
import { apiGet, apiPut } from "@/lib/api-client";

export function PersonalityToggle({ userId }: { userId: string }) {
  const [isActive, setIsActive] = useState<boolean | null>(null);
  const [locked, setLocked] = useState(false);

  useEffect(() => { apiGet<{ isActive: boolean }>(`/api/personality/users/${userId}/active`).then(r => setIsActive(r.isActive)).catch(() => setIsActive(false)); }, [userId]);

  async function toggle() {
    const newVal = !isActive;
    await apiPut<unknown>(`/api/personality/users/${userId}/preference?isActive=${newVal}`);
    setIsActive(newVal);
  }

  if (isActive === null || locked) return null;

  return (
    <div style={{ display: "flex", alignItems: "center", gap: "0.5rem", padding: "0.5rem 0.75rem", borderRadius: 8, background: "var(--surface-soft)", border: "1px solid var(--line)" }}>
      <span style={{ fontSize: "0.8rem", color: "var(--muted)" }}>Personality Mode</span>
      <button onClick={toggle} style={{ padding: "0.25rem 0.625rem", borderRadius: 20, border: "none", cursor: "pointer", background: isActive ? "var(--blue-600)" : "var(--line)", color: isActive ? "#fff" : "var(--muted)", fontSize: "0.75rem", fontWeight: 600, transition: "background 0.2s" }}>
        {isActive ? "ON" : "OFF"}
      </button>
    </div>
  );
}
