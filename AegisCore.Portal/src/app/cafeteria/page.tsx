"use client";

import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { useTranslations } from "next-intl";
import {
  AlertCircle, AlertTriangle, BarChart3, CheckCircle2, Clock, Coffee, DollarSign,
  ForkKnife, Package, PlusCircle, RefreshCw, Send, ShoppingCart, Store, Trash2,
  TrendingUp, UtensilsCrossed
} from "lucide-react";

type MenuItem = {
  id: string; name: string; category: "Main" | "Side" | "Beverage" | "Snack" | "Dessert" | "Combo";
  price: number; calories: number; ingredients: string;
};

type Order = {
  id: string; studentId: string; studentName: string; itemName: string;
  quantity: number; amount: number; status: "Placed" | "Preparing" | "Ready" | "Delivered" | "Cancelled";
};

type VendorScorecard = {
  id: string; vendorName: string; foodQuality: number; hygiene: number;
  service: number; complaints: number; halalCertified: boolean;
};

type InventoryAlert = {
  id: string; itemName: string; currentStock: number; minRequired: number;
  severity: "Low" | "Critical" | "OutOfStock";
};

const orderStatusBadge = (s: Order["status"]) => {
  const map: Record<Order["status"], { c: string; bg: string }> = {
    Placed: { c: "var(--muted)", bg: "var(--surface-soft)" },
    Preparing: { c: "var(--amber)", bg: "var(--amber-soft)" },
    Ready: { c: "var(--green)", bg: "var(--green-soft)" },
    Delivered: { c: "var(--blue-600)", bg: "var(--blue-100)" },
    Cancelled: { c: "var(--red)", bg: "var(--red-soft)" }
  };
  const v = map[s];
  return <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: v.bg, color: v.c }}>{s}</span>;
};

const severityBadge = (s: InventoryAlert["severity"]) => {
  const map: Record<InventoryAlert["severity"], { c: string; bg: string }> = {
    Low: { c: "var(--muted)", bg: "var(--surface-soft)" },
    Critical: { c: "var(--amber)", bg: "var(--amber-soft)" },
    OutOfStock: { c: "var(--red)", bg: "var(--red-soft)" }
  };
  const v = map[s];
  return <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: v.bg, color: v.c }}>{s}</span>;
};

const inputS: React.CSSProperties = { minHeight: 42, padding: "0 11px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--ink)", background: "var(--canvas)", fontSize: "0.85rem", width: "100%", boxSizing: "border-box" };
const labelS: React.CSSProperties = { display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 };

export default function CafeteriaPage() {
  const t = useTranslations("academic");

  const [menuItems, setMenuItems] = useState<MenuItem[]>([]);
  const [orders, setOrders] = useState<Order[]>([]);
  const [alerts, setAlerts] = useState<InventoryAlert[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const [unitName, setUnitName] = useState("");
  const [unitLocation, setUnitLocation] = useState("");
  const [unitCapacity, setUnitCapacity] = useState("");
  const [unitHours, setUnitHours] = useState("");

  const [itemName, setItemName] = useState("");
  const [itemCategory, setItemCategory] = useState<MenuItem["category"]>("Main");
  const [itemPrice, setItemPrice] = useState("");
  const [itemCalories, setItemCalories] = useState("");
  const [itemIngredients, setItemIngredients] = useState("");

  const [studentId, setStudentId] = useState("");
  const [selectedItem, setSelectedItem] = useState("");
  const [orderQuantity, setOrderQuantity] = useState("1");
  const [submitting, setSubmitting] = useState(false);

  const [wasteItem, setWasteItem] = useState("");
  const [wasteQty, setWasteQty] = useState("");

  const [vendorName, setVendorName] = useState("");
  const [foodQuality, setFoodQuality] = useState("3");
  const [hygiene, setHygiene] = useState("3");
  const [service, setService] = useState("3");
  const [complaints, setComplaints] = useState("0");
  const [halalCert, setHalalCert] = useState(false);

  useEffect(() => {
    const ctrl = new AbortController();
    const fetch = async () => {
      try {
        const [m, o, a] = await Promise.all([
          apiGet<MenuItem[]>("/api/cafeteria/menu-items", ctrl.signal).catch(() => [] as MenuItem[]),
          apiGet<Order[]>("/api/cafeteria/orders", ctrl.signal).catch(() => [] as Order[]),
          apiGet<InventoryAlert[]>("/api/cafeteria/inventory-alerts", ctrl.signal).catch(() => [] as InventoryAlert[])
        ]);
        setMenuItems(m); setOrders(o); setAlerts(a);
      } catch { setError("Failed to load cafeteria data"); }
      finally { setLoading(false); }
    };
    fetch();
    return () => { ctrl.abort(); };
  }, []);

  async function placeOrder(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true); setMessage(null);
    try {
      await apiPost("/api/cafeteria/orders", { studentId, itemId: selectedItem, quantity: Number(orderQuantity) });
      setMessage("success: Order placed successfully");
      setStudentId(""); setSelectedItem(""); setOrderQuantity("1");
      const ctrl = new AbortController();
      const o = await apiGet<Order[]>("/api/cafeteria/orders", ctrl.signal);
      setOrders(o);
    } catch { setMessage("error: Failed to place order"); }
    finally { setSubmitting(false); }
  }

  async function submitVendorScorecard(e: React.FormEvent) {
    e.preventDefault();
    try {
      await apiPost("/api/cafeteria/vendor-scorecard", {
        vendorName, foodQuality: Number(foodQuality), hygiene: Number(hygiene),
        service: Number(service), complaints: Number(complaints), halalCertified: halalCert
      });
      setMessage("success: Vendor scorecard submitted");
      setVendorName(""); setFoodQuality("3"); setHygiene("3"); setService("3"); setComplaints("0"); setHalalCert(false);
    } catch { setMessage("error: Failed to submit scorecard"); }
  }

  async function logWaste(e: React.FormEvent) {
    e.preventDefault();
    try {
      await apiPost("/api/cafeteria/waste-log", { itemId: wasteItem, quantity: Number(wasteQty) });
      setMessage("success: Waste logged");
      setWasteItem(""); setWasteQty("");
    } catch { setMessage("error: Failed to log waste"); }
  }

  const pendingOrders = orders.filter(o => o.status !== "Delivered" && o.status !== "Cancelled");

  if (loading) return (
    <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", gap: 16, color: "var(--muted)" }}>
      <div style={{ width: 40, height: 4, borderRadius: 99, overflow: "hidden", background: "var(--line)" }}>
        <div style={{ width: "50%", height: "100%", borderRadius: "inherit", background: "var(--blue-500)", animation: "launch 1s ease-in-out infinite alternate" }} />
      </div>
      Loading cafeteria management...
    </main>
  );

  if (error) return (
    <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", alignContent: "center", gap: 12, color: "var(--red)" }}>
      <AlertCircle style={{ width: 32 }} />
      <strong>{error}</strong>
      <button onClick={() => window.location.reload()} style={{ minHeight: 40, padding: "0 18px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--blue-600)", background: "var(--surface)", cursor: "pointer", fontWeight: 700 }}>Retry</button>
    </main>
  );

  return (
    <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
      <header style={{ display: "flex", alignItems: "end", justifyContent: "space-between", gap: 20, marginBottom: 28 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>Campus Operations</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>{t("cafeteria") ?? "Cafeteria Management"}</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>Digital menu, cashless payments, demand forecasting, and vendor scorecards</span>
        </div>
      </header>

      {message && (
        <div style={{ display: "flex", alignItems: "center", gap: 9, padding: "11px 14px", marginBottom: 18, border: message.startsWith("error") ? "1px solid #ecc0c6" : "1px solid #aaddc9", borderRadius: 10, color: message.startsWith("error") ? "var(--red)" : "var(--green)", background: message.startsWith("error") ? "var(--red-soft)" : "var(--green-soft)", fontSize: 11 }}>
          {message.startsWith("error") ? <AlertCircle /> : <CheckCircle2 />}{message.replace(/^[^:]+: /, "")}
        </div>
      )}

      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(160px, 1fr))", gap: 14, marginBottom: 18 }}>
        <article style={{ minHeight: 110, display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Menu Items</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28 }}>{menuItems.length}</strong>
          <small style={{ color: "var(--muted)" }}>active items</small>
        </article>
        <article style={{ minHeight: 110, display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Pending Orders</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28, color: "var(--amber)" }}>{pendingOrders.length}</strong>
          <small style={{ color: "var(--muted)" }}>awaiting action</small>
        </article>
        <article style={{ minHeight: 110, display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Today Revenue</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28, color: "var(--green)" }}>
            {new Intl.NumberFormat("en-PK", { style: "currency", currency: "PKR", maximumFractionDigits: 0 }).format(orders.reduce((s, o) => s + o.amount, 0))}
          </strong>
          <small style={{ color: "var(--muted)" }}>{orders.length} transactions</small>
        </article>
        <article style={{ minHeight: 110, display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Inventory Alerts</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28, color: "var(--red)" }}>{alerts.length}</strong>
          <small style={{ color: "var(--muted)" }}>low stock items</small>
        </article>
      </div>

      <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 1.2fr) minmax(360px, 0.7fr)", gap: 18, marginBottom: 18 }}>
        <div style={{ display: "grid", gap: 18, alignContent: "start" }}>
          <div className="surface" style={{ overflow: "hidden" }}>
            <div className="surface-heading"><div><h2>Place Order</h2><p>Process student cafeteria orders</p></div><ShoppingCart /></div>
            <form onSubmit={placeOrder} style={{ display: "grid", gap: 12, padding: 18 }}>
              <label style={labelS}>
                Student ID <b style={{ color: "var(--red)" }}>*</b>
                <input placeholder="e.g. STU-2024-001" value={studentId} onChange={e => setStudentId(e.target.value)} style={inputS} required />
              </label>
              <div style={{ display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: 12 }}>
                <label style={labelS}>
                  Menu Item <b style={{ color: "var(--red)" }}>*</b>
                  <select value={selectedItem} onChange={e => setSelectedItem(e.target.value)} style={inputS} required>
                    <option value="">Select item...</option>
                    {menuItems.map(m => <option key={m.id} value={m.id}>{m.name} ({new Intl.NumberFormat("en-PK", { style: "currency", currency: "PKR" }).format(m.price)})</option>)}
                  </select>
                </label>
                <label style={labelS}>
                  Quantity <b style={{ color: "var(--red)" }}>*</b>
                  <input type="number" min={1} value={orderQuantity} onChange={e => setOrderQuantity(e.target.value)} style={inputS} required />
                </label>
              </div>
              <button type="submit" disabled={submitting}
                style={{ padding: "0.7rem", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, display: "flex", alignItems: "center", justifyContent: "center", gap: 7, opacity: submitting ? 0.6 : 1 }}>
                <Send style={{ width: 14 }} /> {submitting ? "Placing..." : "Place Order"}
              </button>
            </form>
          </div>

          <div className="surface" style={{ overflow: "hidden" }}>
            <div className="surface-heading"><div><h2>Demand Forecast</h2><p>Predicted vs actual demand analysis</p></div><TrendingUp /></div>
            <div style={{ padding: 18, display: "grid", gap: 12 }}>
              <div style={{ display: "flex", justifyContent: "space-between", padding: "12px 16px", borderRadius: 8, background: "var(--surface-soft)" }}>
                <span style={{ display: "grid" }}>
                  <strong style={{ fontSize: 22, color: "var(--blue-600)" }}>1,240</strong>
                  <small style={{ color: "var(--muted)", fontSize: 10 }}>Predicted Meals Today</small>
                </span>
                <span style={{ display: "grid", textAlign: "right" }}>
                  <strong style={{ fontSize: 22, color: "var(--green)" }}>1,185</strong>
                  <small style={{ color: "var(--muted)", fontSize: 10 }}>Actual Meals Served</small>
                </span>
              </div>
              <div style={{ padding: "8px 12px", borderRadius: 6, background: "var(--green-soft)", color: "var(--green)", fontSize: 10, fontWeight: 700, textAlign: "center" }}>
                Forecast Accuracy: 95.6%
              </div>
            </div>
          </div>
        </div>

        <div style={{ display: "grid", gap: 18, alignContent: "start" }}>
          <div className="surface" style={{ overflow: "hidden" }}>
            <div className="surface-heading"><div><h2>Vendor Scorecard</h2><p>Rate vendor performance</p></div><ForkKnife /></div>
            <form onSubmit={submitVendorScorecard} style={{ display: "grid", gap: 12, padding: 18 }}>
              <label style={labelS}>
                Vendor Name
                <input placeholder="e.g. Gourmet Services Ltd" value={vendorName} onChange={e => setVendorName(e.target.value)} style={inputS} required />
              </label>
              <div style={{ display: "grid", gridTemplateColumns: "repeat(3, minmax(0, 1fr))", gap: 8 }}>
                {[
                  { l: "Food Quality", v: foodQuality, s: setFoodQuality },
                  { l: "Hygiene", v: hygiene, s: setHygiene },
                  { l: "Service", v: service, s: setService }
                ].map(({ l, v, s }) => (
                  <label key={l} style={labelS}>
                    {l} (1-5)
                    <input type="number" min={1} max={5} value={v} onChange={e => s(e.target.value)} style={inputS} required />
                  </label>
                ))}
              </div>
              <label style={labelS}>
                Complaints Count
                <input type="number" min={0} value={complaints} onChange={e => setComplaints(e.target.value)} style={inputS} />
              </label>
              <label style={{ display: "flex", alignItems: "center", gap: 8, color: "var(--ink)", fontSize: 11, fontWeight: 700, cursor: "pointer" }}>
                <input type="checkbox" checked={halalCert} onChange={e => setHalalCert(e.target.checked)} />
                Halal Certified
              </label>
              <button type="submit" style={{ padding: "0.7rem", background: "var(--amber)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12 }}>
                Submit Scorecard
              </button>
            </form>
          </div>

          <div className="surface" style={{ overflow: "hidden" }}>
            <div className="surface-heading"><div><h2>Inventory Alerts</h2><p>{alerts.length} alerts</p></div><AlertTriangle /></div>
            <div style={{ padding: 12, maxHeight: 260, overflow: "auto" }}>
              {alerts.map(a => (
                <div key={a.id} style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 8, padding: "10px 8px", borderBottom: "1px solid var(--line)" }}>
                  <div style={{ display: "grid" }}>
                    <strong style={{ fontSize: 11 }}>{a.itemName}</strong>
                    <small style={{ color: "var(--muted)", fontSize: 10 }}>Stock: {a.currentStock} / Min: {a.minRequired}</small>
                  </div>
                  {severityBadge(a.severity)}
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      <div style={{ display: "grid", gridTemplateColumns: "minmax(0, 1fr) minmax(360px, 0.5fr)", gap: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Pending Orders</h2><p>{pendingOrders.length} orders in queue</p></div><Clock /></div>
          {pendingOrders.length === 0 ? (
            <div style={{ minHeight: 180, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
              <ShoppingCart style={{ width: 28, opacity: 0.5 }} />
              <strong>No pending orders</strong>
              <span>Place orders through the order form</span>
            </div>
          ) : (
            <div style={{ overflow: "auto" }}>
              <table style={{ width: "100%", minWidth: 600, borderCollapse: "collapse" }}>
                <thead>
                  <tr style={{ background: "var(--surface-soft)" }}>
                    {["Student", "Item", "Qty", "Amount", "Status"].map(h => (
                      <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {pendingOrders.map(o => (
                    <tr key={o.id} style={{ borderBottom: "1px solid var(--line)" }}>
                      <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{o.studentName}</td>
                      <td style={{ padding: "10px 14px", fontSize: 11 }}>{o.itemName}</td>
                      <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>x{o.quantity}</td>
                      <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{new Intl.NumberFormat("en-PK", { style: "currency", currency: "PKR" }).format(o.amount)}</td>
                      <td style={{ padding: "10px 14px" }}>{orderStatusBadge(o.status)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Waste Log</h2><p>Record food waste entries</p></div><Trash2 /></div>
          <form onSubmit={logWaste} style={{ display: "grid", gap: 12, padding: 18 }}>
            <label style={labelS}>
              Item
              <select value={wasteItem} onChange={e => setWasteItem(e.target.value)} style={inputS} required>
                <option value="">Select item...</option>
                {menuItems.map(m => <option key={m.id} value={m.id}>{m.name}</option>)}
              </select>
            </label>
            <label style={labelS}>
              Quantity Wasted
              <input type="number" min={1} placeholder="e.g. 5" value={wasteQty} onChange={e => setWasteQty(e.target.value)} style={inputS} required />
            </label>
            <button type="submit" style={{ padding: "0.7rem", background: "var(--red)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12 }}>
              <Trash2 style={{ width: 14, verticalAlign: "middle", marginRight: 6 }} />Log Waste
            </button>
          </form>
        </div>
      </div>
    </main>
  );
}
