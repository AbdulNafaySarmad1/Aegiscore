"use client";

import React, { useState, useCallback, useRef } from "react";

type NChartProps = {
  title: string;
  subtitle?: string;
  insight?: string;
  children: React.ReactNode;
  height?: number;
};

export function NChart({ title, subtitle, insight, children, height = 300 }: NChartProps) {
  const [zoom, setZoom] = useState(1);
  const [dataTableOpen, setDataTableOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const handleWheel = useCallback((e: React.WheelEvent) => {
    e.preventDefault();
    setZoom(z => {
      const next = z + (e.deltaY < 0 ? 0.1 : -0.1);
      return Math.max(0.5, Math.min(3, next));
    });
  }, []);

  const resetZoom = useCallback(() => setZoom(1), []);

  return (
    <div style={{ background: "var(--surface)", borderRadius: 12, border: "1px solid var(--line)", overflow: "hidden" }}>
      <div style={{ padding: "1rem 1.25rem", borderBottom: "1px solid var(--line)", display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
        <div>
          <h3 style={{ color: "var(--ink)", margin: 0, fontSize: "0.95rem", fontWeight: 600 }}>{title}</h3>
          {subtitle && <p style={{ color: "var(--muted)", margin: "0.25rem 0 0 0", fontSize: "0.8rem" }}>{subtitle}</p>}
          {insight && <div style={{ marginTop: "0.5rem", padding: "0.25rem 0.625rem", borderRadius: 4, background: "var(--blue-100)", color: "var(--blue-600)", fontSize: "0.75rem", display: "inline-block" }}>{insight}</div>}
        </div>
        <div style={{ display: "flex", gap: "0.375rem" }}>
          <button onClick={() => setDataTableOpen(v => !v)} style={chipBtnStyle} title="Toggle data table">&#9776;</button>
          <button onClick={resetZoom} style={chipBtnStyle} title="Reset zoom">&#x21BA;</button>
        </div>
      </div>
      <div ref={containerRef} onWheel={handleWheel} style={{ padding: "0.75rem 1rem", height, overflow: "hidden", transform: `scale(${zoom})`, transformOrigin: "top left", transition: "transform 0.15s ease" }}>
        {children}
      </div>
      {dataTableOpen && (
        <div style={{ borderTop: "1px solid var(--line)", padding: "1rem" }}>
          <div style={{ color: "var(--muted)", fontSize: "0.8rem", textAlign: "center" }}>Accessible data table — embed raw data here</div>
        </div>
      )}
    </div>
  );
}

const chipBtnStyle: React.CSSProperties = {
  padding: "0.25rem 0.5rem", borderRadius: 6, border: "1px solid var(--line)", background: "var(--canvas)", color: "var(--ink)", cursor: "pointer", fontSize: "0.8rem"
};
