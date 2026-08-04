"use client";

import { useEffect, useState } from "react";
import { apiGet, apiPost, apiPut } from "@/lib/api-client";
import { usePersonalityTranslations } from "@/lib/personality-translator";
import { Archive, Download, FileText, Lock, Shield, ShieldCheck, Upload, CheckCircle } from "lucide-react";

type Tier = { id: string; name: string; level: number; retentionYears: number; documentCount: number; totalSizeBytes: number };
type VaultDoc = { id: string; fileName: string; category: string; tierLevel: number; tierName: string; fileSizeBytes: number; isPqcVerified: boolean; isRetentionLocked: boolean; uploadedAt: string; retentionExpiresAt: string; yearsRemaining: number };
type Dashboard = { totalDocuments: number; totalSizeBytes: number; pqcVerifiedCount: number; lockedCount: number; tiers: Tier[]; recentUploads: VaultDoc[] };

const cardS = { background: "var(--surface)", borderRadius: 12, border: "1px solid var(--line)", padding: "1.5rem", marginBottom: "1.25rem" };
const h3 = { color: "var(--ink)", fontSize: "1.1rem", fontWeight: 600, margin: "0 0 0.75rem 0", display: "flex", alignItems: "center", gap: "0.4rem" };
const btn = (bg: string) => ({ padding: "0.5rem 1rem", borderRadius: 8, border: "none", cursor: "pointer", fontWeight: 600, fontSize: "0.82rem", background: bg, color: "#fff", display: "inline-flex", alignItems: "center", gap: "0.35rem" });
const tierColors: Record<number, string> = { 1: "var(--red)", 2: "var(--amber)", 3: "var(--blue)" };
const tierBg: Record<number, string> = { 1: "var(--red-soft)", 2: "var(--amber-soft)", 3: "var(--blue-light)" };

function fmtBytes(bytes: number): string {
  if (bytes < 1024) return bytes + " B";
  if (bytes < 1048576) return (bytes / 1024).toFixed(1) + " KB";
  if (bytes < 1073741824) return (bytes / 1048576).toFixed(1) + " MB";
  return (bytes / 1073741824).toFixed(2) + " GB";
}

export default function DocumentVaultPage() {
  const t = usePersonalityTranslations("academic");
  const [dash, setDash] = useState<Dashboard | null>(null);
  const [msg, setMsg] = useState("");
  const [upload, setUpload] = useState({ file: null as File | null, category: "General" });

  async function refresh() {
    const d = await apiGet<Dashboard>("/api/document-vault/dashboard").catch(() => null);
    setDash(d);
  }
  useEffect(() => { refresh(); }, []);

  async function handleUpload() {
    if (!upload.file) return;
    const form = new FormData(); form.append("file", upload.file);
    try {
      const resp = await fetch(`/api/document-vault/upload?fileName=${upload.file.name}&contentType=${upload.file.type}&category=${upload.category}&uploadedByUserId=00000000-0000-0000-0000-000000000000&uploadedByRole=Admin`, { method: "POST", body: upload.file });
      const data = await resp.json();
      setMsg(`Uploaded → Tier ${data.tier} (${data.retentionYears}yr lock), PQC: ${data.pqcVerified ? "✓" : "✗"}`);
      setUpload({ file: null, category: "General" }); refresh();
    } catch { setMsg("Upload failed"); }
  }

  async function seedTiers() { await apiPost("/api/document-vault/seed-tiers"); setMsg("Tiers seeded"); refresh(); }

  if (!dash) return <main style={{ maxWidth: 1100, margin: "0 auto", padding: "1.5rem" }}><p style={{ color: "var(--muted)" }}>Loading...</p></main>;

  return (
    <main style={{ maxWidth: 1100, margin: "0 auto", padding: "1.5rem" }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: "1.5rem" }}>
        <div>
          <h1 style={{ color: "var(--ink)", fontSize: "1.5rem", fontWeight: 600, marginBottom: "0.25rem" }}>Document Vault</h1>
          <p style={{ color: "var(--muted)", fontSize: "0.85rem" }}>Tiered PQC-verified storage on Cloudflare R2 with retention locks — 50yr / 20yr / 5yr</p>
        </div>
        <button onClick={seedTiers} style={btn("var(--blue)")}><Shield size={14} /> Seed Tiers</button>
      </div>

      {msg && <div onDoubleClick={() => setMsg("")} style={{ padding: "0.5rem 1rem", borderRadius: 8, background: "var(--blue-light)", color: "var(--blue)", marginBottom: "1rem", fontSize: "0.85rem", cursor: "pointer" }}>{msg}</div>}

      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(180px, 1fr))", gap: "0.75rem", marginBottom: "1.5rem" }}>
        {[ ["Total Documents", dash.totalDocuments, "var(--ink)"], ["Total Size", fmtBytes(dash.totalSizeBytes), "var(--muted)"], ["PQC Verified", dash.pqcVerifiedCount, "var(--green)"], ["Retention Locked", dash.lockedCount, "var(--blue)"] ].map(([l,v,c]) => (
          <div key={l} style={{ background: "var(--surface)", borderRadius: 12, border: "1px solid var(--line)", padding: "1rem" }}>
            <div style={{ fontSize: "0.75rem", color: "var(--muted)", marginBottom: "0.25rem" }}>{l}</div>
            <div style={{ fontSize: "1.5rem", fontWeight: 700, color: c as string }}>{v}</div>
          </div>
        ))}
      </div>

      <div style={cardS}>
        <h3 style={h3}><Upload size={18} /> Upload Document to Vault</h3>
        <div style={{ display: "grid", gridTemplateColumns: "1fr auto auto", gap: "0.75rem", alignItems: "end" }}>
          <div>
            <div style={{ fontSize: "0.75rem", color: "var(--muted)", marginBottom: "0.2rem" }}>Select File</div>
            <input type="file" onChange={e => setUpload(prev => ({ ...prev, file: e.target.files?.[0] || null }))} style={{ padding: "0.5rem", borderRadius: 8, border: "1px solid var(--line)", background: "var(--canvas)", color: "var(--ink)", fontSize: "0.85rem", width: "100%", boxSizing: "border-box" }} />
          </div>
          <div>
            <div style={{ fontSize: "0.75rem", color: "var(--muted)", marginBottom: "0.2rem" }}>Category</div>
            <select value={upload.category} onChange={e => setUpload(prev => ({ ...prev, category: e.target.value }))} style={{ padding: "0.5rem 0.75rem", borderRadius: 8, border: "1px solid var(--line)", background: "var(--canvas)", color: "var(--ink)", fontSize: "0.85rem" }}>
              <option value="Degree">Degree (50yr)</option>
              <option value="Certification">Certification (50yr)</option>
              <option value="FinancialBilling">Financial Billing (50yr)</option>
              <option value="FeeChallan">Fee Challan (20yr)</option>
              <option value="GradeAssessment">Grade Assessment (20yr)</option>
              <option value="Quiz">Quiz (20yr)</option>
              <option value="AcademicNotice">Academic Notice (20yr)</option>
              <option value="Attendance">Attendance (5yr)</option>
              <option value="ProfilePicture">Profile Picture (5yr)</option>
              <option value="General">General (5yr)</option>
            </select>
          </div>
          <button onClick={handleUpload} disabled={!upload.file} style={btn(upload.file ? "var(--green)" : "var(--line)")}><Upload size={14} /> Upload &amp; Lock</button>
        </div>
      </div>

      <div style={cardS}>
        <h3 style={h3}><Archive size={18} /> Storage Tiers</h3>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(280px, 1fr))", gap: "0.75rem" }}>
          {(dash.tiers.length === 0 ? [{ id: "1", name: "Critical", level: 1, retentionYears: 50, documentCount: 0, totalSizeBytes: 0 }, { id: "2", name: "Important", level: 2, retentionYears: 20, documentCount: 0, totalSizeBytes: 0 }, { id: "3", name: "Standard", level: 3, retentionYears: 5, documentCount: 0, totalSizeBytes: 0 }] : dash.tiers).map(tier => (
            <div key={tier.id} style={{ background: tierBg[tier.level], borderRadius: 12, border: `1px solid ${tierColors[tier.level]}`, padding: "1.25rem" }}>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: "0.75rem" }}>
                <div>
                  <div style={{ fontSize: "0.75rem", color: tierColors[tier.level], fontWeight: 600, textTransform: "uppercase", letterSpacing: "0.04em" }}>Tier {tier.level}</div>
                  <div style={{ fontSize: "1.15rem", fontWeight: 700, color: "var(--ink)" }}>{tier.name}</div>
                </div>
                <div style={{ display: "flex", alignItems: "center", gap: "0.35rem" }}>
                  <Lock size={14} style={{ color: tier.level === 1 ? "var(--red)" : tier.level === 2 ? "var(--amber)" : "var(--blue)" }} />
                  <span style={{ fontWeight: 700, fontSize: "1.25rem", color: tierColors[tier.level] }}>{tier.retentionYears}</span>
                  <span style={{ fontSize: "0.7rem", color: "var(--muted)" }}>years</span>
                </div>
              </div>
              <div style={{ display: "flex", gap: "1.5rem", fontSize: "0.85rem" }}>
                <div><span style={{ color: "var(--muted)" }}>Docs: </span><strong style={{ color: "var(--ink)" }}>{tier.documentCount}</strong></div>
                <div><span style={{ color: "var(--muted)" }}>Size: </span><strong style={{ color: "var(--ink)" }}>{fmtBytes(tier.totalSizeBytes)}</strong></div>
                <div><ShieldCheck size={14} style={{ color: "var(--green)" }} /></div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {dash.recentUploads.length > 0 && (
        <div style={cardS}>
          <h3 style={h3}><FileText size={18} /> Recent Uploads</h3>
          <table style={{ width: "100%", borderCollapse: "collapse", fontSize: "0.85rem" }}>
            <thead><tr style={{ borderBottom: "1px solid var(--line)" }}>{["File","Category","Tier","Lock","Size","Uploaded","Expires",""].map(h => <th key={h} style={{ textAlign: "left", padding: "0.5rem 0.75rem", color: "var(--muted)", fontWeight: 500, fontSize: "0.75rem" }}>{h}</th>)}</tr></thead>
            <tbody>
              {dash.recentUploads.map(doc => (
                <tr key={doc.id} style={{ borderBottom: "1px solid var(--gray)" }}>
                  <td style={{ padding: "0.4rem 0.75rem", color: "var(--ink)", fontWeight: 500 }}>{doc.fileName}</td>
                  <td style={{ padding: "0.4rem 0.75rem", color: "var(--muted)", fontSize: "0.82rem" }}>{doc.category}</td>
                  <td style={{ padding: "0.4rem 0.75rem" }}><span style={{ padding: "0.1rem 0.5rem", borderRadius: 20, fontSize: "0.7rem", fontWeight: 600, background: tierBg[doc.tierLevel], color: tierColors[doc.tierLevel] }}>T{doc.tierLevel} · {doc.tierName}</span></td>
                  <td style={{ padding: "0.4rem 0.75rem" }}>{doc.isRetentionLocked ? <Lock size={12} style={{ color: "var(--green)" }} /> : "—"}</td>
                  <td style={{ padding: "0.4rem 0.75rem", color: "var(--muted)", fontSize: "0.82rem" }}>{fmtBytes(doc.fileSizeBytes)}</td>
                  <td style={{ padding: "0.4rem 0.75rem", color: "var(--muted)", fontSize: "0.78rem" }}>{new Date(doc.uploadedAt).toLocaleDateString()}</td>
                  <td style={{ padding: "0.4rem 0.75rem", color: doc.yearsRemaining < 1 ? "var(--red)" : "var(--muted)", fontSize: "0.78rem", fontWeight: 600 }}>{doc.yearsRemaining}yr left</td>
                  <td style={{ padding: "0.4rem 0.75rem" }}>{doc.isPqcVerified && <ShieldCheck size={14} style={{ color: "var(--green)" }} />}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </main>
  );
}
