"use client";

import { useTranslations } from "next-intl";

export function GradeAppealsDashboard() {
  const t = useTranslations("academic");

  return (
    <main style={{ padding: "1.5rem", maxWidth: 1100, margin: "0 auto" }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "1.5rem" }}>
        <h1 style={{ color: "var(--ink)", fontSize: "1.5rem", fontWeight: 600, margin: 0 }}>{t("gradeAppeals")}</h1>
      </div>
      <div style={{ background: "var(--surface)", borderRadius: 12, border: "1px solid var(--line)", padding: "1.5rem" }}>
        <p style={{ color: "var(--muted)" }}>{t("gradeAppealsDesc")}</p>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "1rem", marginTop: "1rem" }}>
          <div style={{ background: "var(--canvas)", borderRadius: 8, padding: "1rem" }}>
            <span style={{ color: "var(--muted)", fontSize: "0.8rem" }}>{t("submitted")}</span>
            <div style={{ fontSize: "2rem", fontWeight: 700, color: "var(--ink)" }}>0</div>
          </div>
          <div style={{ background: "var(--canvas)", borderRadius: 8, padding: "1rem" }}>
            <span style={{ color: "var(--muted)", fontSize: "0.8rem" }}>{t("underReview")}</span>
            <div style={{ fontSize: "2rem", fontWeight: 700, color: "var(--amber)" }}>0</div>
          </div>
          <div style={{ background: "var(--canvas)", borderRadius: 8, padding: "1rem" }}>
            <span style={{ color: "var(--muted)", fontSize: "0.8rem" }}>{t("resolved")}</span>
            <div style={{ fontSize: "2rem", fontWeight: 700, color: "var(--green)" }}>0</div>
          </div>
          <div style={{ background: "var(--canvas)", borderRadius: 8, padding: "1rem" }}>
            <span style={{ color: "var(--muted)", fontSize: "0.8rem" }}>{t("pendingFaculty")}</span>
            <div style={{ fontSize: "2rem", fontWeight: 700, color: "var(--ink)" }}>0</div>
          </div>
        </div>
      </div>
    </main>
  );
}
