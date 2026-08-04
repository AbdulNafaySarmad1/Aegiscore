"use client";

import { usePersonalityMode } from "@/lib/personality-context";

export function PersonalityToggle() {
  const { isActive, isLoading, togglePersonality } = usePersonalityMode();

  if (isLoading) return null;

  return (
    <div style={{ display: "flex", alignItems: "center", gap: "0.5rem", padding: "0.5rem 0.75rem", borderRadius: 8, background: "var(--surface-soft)", border: "1px solid var(--line)" }}>
      <span style={{ fontSize: "0.8rem", color: "var(--muted)" }}>Personality Mode</span>
      <button onClick={togglePersonality} style={{ padding: "0.25rem 0.625rem", borderRadius: 20, border: "none", cursor: "pointer", background: isActive ? "var(--blue-600)" : "var(--line)", color: isActive ? "#fff" : "var(--muted)", fontSize: "0.75rem", fontWeight: 600, transition: "background 0.2s" }}>
        {isActive ? "ON" : "OFF"}
      </button>
    </div>
  );
}
