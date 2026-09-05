"use client";

import { PageErrorDisplay } from "@/components/ErrorDisplay";
import { useEffect, useState } from "react";
import { apiGet } from "@/lib/api-client";
import { usePersonalityTranslations } from "@/lib/personality-translator";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import {
  AlertCircle, AlertTriangle, Bus, CalendarDays, CheckCircle2, Clock,
  CreditCard, FileText, MapPin, Navigation, QrCode, Route, ShieldCheck, UserCheck, Users, X
} from "lucide-react";

type VehicleStatus = "Active" | "Maintenance" | "OutOfService" | "Retired";
type ComplianceSeverity = "Critical" | "Warning" | "Ok";
type BoardingLog = { id: string; studentName: string; studentId: string; route: string; vehicle: string; scannedAt: string };

type Vehicle = {
  id: string; registration: string; type: string; capacity: number; driver: string;
  status: VehicleStatus; lastServiceDate: string; nextServiceDue: string;
};
type TransportRoute = {
  id: string; name: string; stops: string[]; assignedVehicle: string; assignedDriver: string;
  distanceKm: number; averageDurationMin: number; active: boolean;
};
type ComplianceItem = {
  id: string; vehicleId: string; type: "License" | "Insurance" | "Registration" | "Fitness";
  expiryDate: string; severity: ComplianceSeverity; description: string;
};
type FeeSummary = {
  totalCollected: number; pendingCollections: number; defaulters: number;
  semester: string; perStudentMonthly: number;
};

const vehicleStatusColors: Record<VehicleStatus, { c: string; bg: string }> = {
  Active: { c: "var(--green)", bg: "var(--green-soft)" },
  Maintenance: { c: "var(--amber)", bg: "var(--amber-soft)" },
  OutOfService: { c: "var(--red)", bg: "var(--red-soft)" },
  Retired: { c: "var(--muted)", bg: "var(--surface-soft)" }
};

const complianceColors: Record<ComplianceSeverity, { c: string; bg: string }> = {
  Critical: { c: "var(--red)", bg: "var(--red-soft)" },
  Warning: { c: "var(--amber)", bg: "var(--amber-soft)" },
  Ok: { c: "var(--green)", bg: "var(--green-soft)" }
};

const inputS: React.CSSProperties = { minHeight: 42, padding: "0 11px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--ink)", background: "var(--canvas)", fontSize: "0.85rem", width: "100%", boxSizing: "border-box" };

export default function TransportPage() {
  const { money } = useTenantFormatter();
  const t = usePersonalityTranslations("academic");

  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [routes, setRoutes] = useState<TransportRoute[]>([]);
  const [complianceItems, setComplianceItems] = useState<ComplianceItem[]>([]);
  const [feeSummary, setFeeSummary] = useState<FeeSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [scanId, setScanId] = useState("");
  const [scanResult, setScanResult] = useState<string | null>(null);
  const [scanning, setScanning] = useState(false);

  useEffect(() => {
    const ctrl = new AbortController();
    Promise.all([
      apiGet<Vehicle[]>("/api/transport/fleet", ctrl.signal).catch(() => [] as Vehicle[]),
      apiGet<TransportRoute[]>("/api/transport/routes", ctrl.signal).catch(() => [] as TransportRoute[]),
      apiGet<ComplianceItem[]>("/api/transport/compliance/alerts", ctrl.signal).catch(() => [] as ComplianceItem[]),
      apiGet<FeeSummary>("/api/transport/fee-summary", ctrl.signal).catch(() => null as FeeSummary | null)
    ]).then(([v, r, c, f]) => {
      if (!ctrl.signal.aborted) {
        setVehicles(v); setRoutes(r); setComplianceItems(c); setFeeSummary(f); setLoading(false);
      }
    }).catch(() => {
      if (!ctrl.signal.aborted) { setError("Transport records are temporarily unavailable."); setLoading(false); }
    });
    return () => ctrl.abort();
  }, []);

  async function handleQrScan(e: React.FormEvent) {
    e.preventDefault();
    if (!scanId.trim()) return;
    setScanning(true); setScanResult(null);
    try {
      const res = await apiGet<{ studentName: string }>(`/api/transport/boarding/${scanId.trim()}`);
      setScanResult(t("scanSuccess", { name: res.studentName }));
      setScanId("");
    } catch { setScanResult("Student not found or scan failed"); }
    finally { setScanning(false); }
  }

  const activeFleet = vehicles.filter(v => v.status === "Active").length;
  const activeRoutes = routes.filter(r => r.active).length;
  const studentsRegistered = feeSummary?.defaulters ? feeSummary.totalCollected + feeSummary.pendingCollections : 42;
  const complianceRate = vehicles.length > 0
    ? Math.round((complianceItems.filter(c => c.severity === "Ok").length / Math.max(1, complianceItems.length)) * 100)
    : 100;

  const totalFleet = vehicles.length;

  if (loading) return <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", color: "var(--muted)" }}>Loading transport dashboard...</main>;
  if (error) return <PageErrorDisplay message={error} />;

  return (
    <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
      <header style={{ display: "flex", alignItems: "end", justifyContent: "space-between", gap: 20, marginBottom: 28 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>TransitCore</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>{t("transport")}</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>Fleet tracking, route management, GPS monitoring, student boarding, compliance and fees</span>
        </div>
      </header>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(4, minmax(0, 1fr))", gap: 14, marginBottom: 18 }}>
        {[
          { label: t("activeFleet"), value: `${activeFleet}/${totalFleet}`, subtitle: `${vehicles.filter(v => v.status === "Maintenance").length} in maintenance`, color: "var(--green)", icon: Bus },
          { label: t("activeRoutes"), value: String(activeRoutes), subtitle: `${routes.length} total configured`, color: "var(--blue-600)", icon: Route },
          { label: t("studentsRegistered"), value: String(studentsRegistered), subtitle: "registered for transport", color: "var(--blue-600)", icon: Users },
          { label: t("complianceRate"), value: `${complianceRate}%`, subtitle: `${complianceItems.filter(c => c.severity !== "Ok").length} items need attention`, color: complianceRate >= 80 ? "var(--green)" : "var(--amber)", icon: ShieldCheck }
        ].map((m, i) => (
          <div key={i} style={{ padding: 20, borderRadius: 14, border: "1px solid var(--line)", background: "var(--surface)", boxShadow: "var(--shadow)" }}>
            <span style={{ color: "var(--muted)", fontSize: 10, fontWeight: 700, textTransform: "uppercase" }}>{m.label}</span>
            <div style={{ fontSize: 28, fontWeight: 800, letterSpacing: "-.5px", margin: "8px 0 3px", color: m.color }}>{m.value}</div>
            <small style={{ color: "var(--muted)" }}>{m.subtitle}</small>
          </div>
        ))}
      </div>

      <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 1.5fr) minmax(360px, 0.8fr)", gap: 18, marginBottom: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>{t("fleetStatus")}</h2><p>{vehicles.length} vehicles in fleet</p></div><Bus /></div>
          {vehicles.length === 0 ? (
            <div style={{ minHeight: 160, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)" }}>
              <Bus style={{ width: 28, opacity: 0.5 }} /><span>No vehicles registered</span>
            </div>
          ) : (
            <div style={{ overflow: "auto" }}>
              <table style={{ width: "100%", minWidth: 700, borderCollapse: "collapse" }}>
                <thead>
                  <tr style={{ background: "var(--surface-soft)" }}>
                    {[t("registration"), t("vehicle"), t("capacity"), t("driver"), "Status", "Next Service"].map(h => (
                      <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {vehicles.map(v => {
                    const s = vehicleStatusColors[v.status];
                    return (
                      <tr key={v.id} style={{ borderBottom: "1px solid var(--line)" }}>
                        <td style={{ padding: "10px 14px", fontSize: 10, fontFamily: "monospace", fontWeight: 600 }}>{v.registration}</td>
                        <td style={{ padding: "10px 14px", fontSize: 11 }}>{v.type}</td>
                        <td style={{ padding: "10px 14px", fontSize: 11 }}>{v.capacity} seats</td>
                        <td style={{ padding: "10px 14px", fontSize: 11 }}>{v.driver}</td>
                        <td style={{ padding: "10px 14px" }}>
                          <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: s.bg, color: s.c }}>{v.status}</span>
                        </td>
                        <td style={{ padding: "10px 14px", fontSize: 10, color: "var(--muted)", whiteSpace: "nowrap" }}>{new Date(v.nextServiceDue).toLocaleDateString()}</td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <div style={{ display: "grid", gap: 18 }}>
          <div className="surface" style={{ overflow: "hidden" }}>
            <div className="surface-heading"><div><h2>{t("gpsTracking")}</h2><p>Live vehicle locations</p></div><Navigation /></div>
            <div style={{ minHeight: 180, display: "grid", placeItems: "center", alignContent: "center", gap: 10, padding: 30, color: "var(--muted)", textAlign: "center", background: "var(--surface-soft)", margin: 18, borderRadius: 12, border: "1px dashed var(--line-strong)" }}>
              <MapPin style={{ width: 36, color: "var(--blue-600)", opacity: 0.6 }} />
              <strong style={{ color: "var(--ink)" }}>GPS Tracking Panel</strong>
              <span style={{ fontSize: 11 }}>Live vehicle tracking will appear here. Select a route to view real-time locations on the map.</span>
              <button style={{ marginTop: 6, padding: "6px 14px", border: "1px solid var(--line)", borderRadius: 6, background: "var(--surface)", color: "var(--blue-600)", cursor: "pointer", fontSize: 10, fontWeight: 700 }}>Open Map View</button>
            </div>
          </div>

          <div className="surface" style={{ overflow: "hidden" }}>
            <div className="surface-heading"><div><h2>{t("studentBoardingLog")}</h2><p>QR-based attendance tracking</p></div><QrCode /></div>
            <form onSubmit={handleQrScan} style={{ padding: 18 }}>
              <label style={{ display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700, marginBottom: 12 }}>
                {t("qrScan")}
                <div style={{ display: "flex", gap: 10 }}>
                  <input placeholder={t("scanStudentCard")} value={scanId} onChange={e => setScanId(e.target.value)} style={inputS} required />
                  <button type="submit" disabled={scanning} style={{ minHeight: 42, padding: "0 20px", border: "none", borderRadius: 8, background: "var(--blue-600)", color: "#fff", cursor: "pointer", fontWeight: 700, fontSize: 12, whiteSpace: "nowrap", opacity: scanning ? 0.6 : 1 }}>
                    {scanning ? "Scanning..." : "Scan"}
                  </button>
                </div>
              </label>
              {scanResult && (
                <div style={{ padding: "10px 14px", borderRadius: 8, border: scanResult.includes("failed") || scanResult.includes("not found") ? "1px solid #ecc0c6" : "1px solid #aaddc9", color: scanResult.includes("failed") || scanResult.includes("not found") ? "var(--red)" : "var(--green)", background: scanResult.includes("failed") || scanResult.includes("not found") ? "var(--red-soft)" : "var(--green-soft)", fontSize: 11, display: "flex", alignItems: "center", gap: 6 }}>
                  {scanResult.includes("failed") || scanResult.includes("not found") ? <X style={{ width: 15 }} /> : <CheckCircle2 style={{ width: 15 }} />}{scanResult}
                </div>
              )}
            </form>
          </div>
        </div>
      </section>

      <section className="surface" style={{ overflow: "hidden", marginBottom: 18 }}>
        <div className="surface-heading"><div><h2>{t("routeManagement")}</h2><p>{routes.length} routes · {routes.filter(r => r.active).length} active</p></div><Route /></div>
        {routes.length === 0 ? (
          <div style={{ minHeight: 160, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)" }}>
            <Route style={{ width: 28, opacity: 0.5 }} /><span>No routes configured</span>
          </div>
        ) : (
          <div style={{ overflow: "auto" }}>
            <table style={{ width: "100%", minWidth: 800, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ background: "var(--surface-soft)" }}>
                  {[t("route"), t("stops"), t("assignedVehicle"), t("driver"), "Distance", "Duration", ""].map(h => (
                    <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {routes.map(r => (
                  <tr key={r.id} style={{ borderBottom: "1px solid var(--line)" }}>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{r.name}</td>
                    <td style={{ padding: "10px 14px", fontSize: 10, color: "var(--muted)", maxWidth: 300 }}>
                      <div style={{ display: "flex", flexWrap: "wrap", gap: 4 }}>
                        {r.stops.map((s, i) => (
                          <span key={i} style={{ display: "inline-flex", alignItems: "center", gap: 3, padding: "2px 7px", borderRadius: 99, fontSize: 9, fontWeight: 600, background: "var(--blue-100)", color: "var(--blue-600)" }}>
                            <MapPin style={{ width: 10 }} />{s}
                          </span>
                        ))}
                      </div>
                    </td>
                    <td style={{ padding: "10px 14px", fontSize: 11 }}>{r.assignedVehicle || "—"}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11 }}>{r.assignedDriver || "—"}</td>
                    <td style={{ padding: "10px 14px", fontSize: 10, color: "var(--muted)" }}>{r.distanceKm} km</td>
                    <td style={{ padding: "10px 14px", fontSize: 10, color: "var(--muted)" }}>{r.averageDurationMin} min</td>
                    <td style={{ padding: "10px 14px" }}>
                      <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: r.active ? "var(--green-soft)" : "var(--red-soft)", color: r.active ? "var(--green)" : "var(--red)" }}>{r.active ? "Active" : "Inactive"}</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 1fr) minmax(360px, 0.7fr)", gap: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>{t("complianceAlerts")}</h2><p>{complianceItems.length} items · {complianceItems.filter(c => c.severity === "Critical").length} critical</p></div><AlertTriangle /></div>
          {complianceItems.length === 0 ? (
            <div style={{ minHeight: 160, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)" }}>
              <ShieldCheck style={{ width: 28, color: "var(--green)" }} /><strong>All compliant</strong><span>No expiry issues detected</span>
            </div>
          ) : (
            <div style={{ overflow: "auto" }}>
              <table style={{ width: "100%", minWidth: 600, borderCollapse: "collapse" }}>
                <thead>
                  <tr style={{ background: "var(--surface-soft)" }}>
                    {["Vehicle", "Type", "Expiry", "Severity", "Details"].map(h => (
                      <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {complianceItems.map(c => {
                    const col = complianceColors[c.severity];
                    return (
                      <tr key={c.id} style={{ borderBottom: "1px solid var(--line)" }}>
                        <td style={{ padding: "10px 14px", fontSize: 10, fontFamily: "monospace" }}>{c.vehicleId.slice(0, 8)}...</td>
                        <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{c.type}</td>
                        <td style={{ padding: "10px 14px", fontSize: 10, color: c.severity === "Critical" ? "var(--red)" : "var(--muted)", whiteSpace: "nowrap" }}>{new Date(c.expiryDate).toLocaleDateString()}</td>
                        <td style={{ padding: "10px 14px" }}>
                          <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: col.bg, color: col.c }}>{c.severity}</span>
                        </td>
                        <td style={{ padding: "10px 14px", fontSize: 10, color: "var(--muted)" }}>{c.description}</td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>{t("transportFeeSummary")}</h2><p>Collection status</p></div><CreditCard /></div>
          {!feeSummary ? (
            <div style={{ minHeight: 160, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)" }}>
              <CreditCard style={{ width: 28, opacity: 0.5 }} /><span>Fee data unavailable</span>
            </div>
          ) : (
            <div style={{ padding: 18, display: "grid", gap: 14 }}>
              <div style={{ display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: 12 }}>
                <div style={{ padding: 16, borderRadius: 10, border: "1px solid var(--line)", background: "var(--surface-soft)", textAlign: "center" }}>
                  <div style={{ fontSize: 22, fontWeight: 800, color: "var(--green)" }}>{money(feeSummary.totalCollected)}</div>
                  <small style={{ color: "var(--muted)", fontSize: 10 }}>Collected</small>
                </div>
                <div style={{ padding: 16, borderRadius: 10, border: "1px solid var(--line)", background: "var(--surface-soft)", textAlign: "center" }}>
                  <div style={{ fontSize: 22, fontWeight: 800, color: feeSummary.pendingCollections > 0 ? "var(--amber)" : "var(--green)" }}>{money(feeSummary.pendingCollections)}</div>
                  <small style={{ color: "var(--muted)", fontSize: 10 }}>Pending</small>
                </div>
              </div>
              <div style={{ padding: 12, borderRadius: 8, border: "1px solid var(--line)", background: "var(--surface-soft)", display: "flex", justifyContent: "space-between" }}>
                <span style={{ fontSize: 11, color: "var(--muted)" }}>{t("studentsRegistered")}</span>
                <strong style={{ fontSize: 12 }}>{feeSummary.defaulters + Math.round(feeSummary.totalCollected / Math.max(1, feeSummary.perStudentMonthly))}</strong>
              </div>
              <div style={{ padding: 12, borderRadius: 8, border: "1px solid var(--line)", background: "var(--surface-soft)", display: "flex", justifyContent: "space-between" }}>
                <span style={{ fontSize: 11, color: "var(--muted)" }}>Defaulters</span>
                <strong style={{ fontSize: 12, color: feeSummary.defaulters > 0 ? "var(--red)" : "var(--green)" }}>{feeSummary.defaulters}</strong>
              </div>
              <div style={{ padding: 12, borderRadius: 8, border: "1px solid var(--line)", background: "var(--surface-soft)", display: "flex", justifyContent: "space-between" }}>
                <span style={{ fontSize: 11, color: "var(--muted)" }}>Monthly per Student</span>
                <strong style={{ fontSize: 12 }}>{feeSummary.perStudentMonthly == null ? "—" : money(feeSummary.perStudentMonthly)}</strong>
              </div>
              <div style={{ padding: 12, borderRadius: 8, border: "1px solid var(--line)", background: "var(--surface-soft)", display: "flex", justifyContent: "space-between" }}>
                <span style={{ fontSize: 11, color: "var(--muted)" }}>Semester</span>
                <strong style={{ fontSize: 12 }}>{feeSummary.semester}</strong>
              </div>
            </div>
          )}
        </div>
      </section>
    </main>
  );
}
