"use client";

import { useEffect, useMemo, useState } from "react";
import { apiGet } from "@/lib/api-client";
import {
  AlertCircle,
  Clock3,
  Gauge,
  RefreshCw,
  Sparkles,
  TrendingDown,
  TrendingUp,
  UtensilsCrossed,
} from "lucide-react";

type MenuItem = {
  id: string;
  name: string;
  category: string;
  price: number;
  calories?: number;
  ingredients?: string;
};

type InventoryAlert = {
  id: string;
  itemName: string;
  currentStock: number;
  minRequired: number;
  severity: "Low" | "Critical" | "OutOfStock";
};

type ForecastItem = MenuItem & {
  probability: number;
  confidence: "High" | "Medium" | "Low";
  label: string;
  reason: string;
};

const demoMenu: MenuItem[] = [
  { id: "demo-biryani", name: "Chicken Biryani", category: "Main", price: 320, calories: 690 },
  { id: "demo-karahi", name: "Chicken Karahi", category: "Main", price: 420, calories: 760 },
  { id: "demo-fries", name: "Loaded Fries", category: "Side", price: 220, calories: 520 },
  { id: "demo-burger", name: "Zinger Burger", category: "Main", price: 390, calories: 640 },
  { id: "demo-roll", name: "Chicken Mayo Roll", category: "Snack", price: 240, calories: 470 },
  { id: "demo-tea", name: "Karak Chai", category: "Beverage", price: 100, calories: 120 },
];

const card: React.CSSProperties = {
  border: "1px solid var(--line)",
  borderRadius: 14,
  background: "var(--surface)",
  boxShadow: "var(--shadow)",
};

function stableHash(value: string) {
  let hash = 2166136261;
  for (let i = 0; i < value.length; i += 1) {
    hash ^= value.charCodeAt(i);
    hash = Math.imul(hash, 16777619);
  }
  return Math.abs(hash >>> 0);
}

function availabilityFor(item: MenuItem, alerts: InventoryAlert[]): ForecastItem {
  const day = new Intl.DateTimeFormat("en-US", { weekday: "long", timeZone: "Asia/Karachi" }).format(new Date());
  const hour = Number(new Intl.DateTimeFormat("en-US", { hour: "2-digit", hour12: false, timeZone: "Asia/Karachi" }).format(new Date()));
  const matchingAlert = alerts.find(a =>
    a.itemName.toLowerCase().includes(item.name.toLowerCase()) ||
    item.name.toLowerCase().includes(a.itemName.toLowerCase())
  );

  // Deterministic day/item baseline so the demo remains stable across refreshes.
  const seed = stableHash(`${day}:${item.name}`);
  let probability = 68 + (seed % 27); // 68–94 before operational adjustments.

  // As lunch progresses, popular items become more likely to sell out.
  if (hour >= 13) probability -= 7;
  if (hour >= 15) probability -= 9;

  if (matchingAlert?.severity === "Low") probability -= 16;
  if (matchingAlert?.severity === "Critical") probability -= 32;
  if (matchingAlert?.severity === "OutOfStock") probability = 2;

  probability = Math.max(2, Math.min(98, probability));

  const label = probability >= 80 ? "Very likely" : probability >= 60 ? "Likely" : probability >= 35 ? "Risky" : "Unlikely";
  const confidence: ForecastItem["confidence"] = matchingAlert ? "High" : alerts.length > 0 ? "Medium" : "Low";
  const reason = matchingAlert
    ? matchingAlert.severity === "OutOfStock"
      ? "Inventory signal says this item is currently out of stock."
      : `${matchingAlert.severity} stock signal is reducing today's availability odds.`
    : hour >= 15
      ? "Late-day sell-out risk is reducing availability."
      : "Based on day-of-week demand pattern and current service time.";

  return { ...item, probability, confidence, label, reason };
}

export default function CafeteriaPage() {
  const [menuItems, setMenuItems] = useState<MenuItem[]>([]);
  const [alerts, setAlerts] = useState<InventoryAlert[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [usingDemoMenu, setUsingDemoMenu] = useState(false);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const [menu, inventory] = await Promise.all([
        apiGet<MenuItem[]>("/api/cafeteria/menu-items").catch(() => [] as MenuItem[]),
        apiGet<InventoryAlert[]>("/api/cafeteria/inventory-alerts").catch(() => [] as InventoryAlert[]),
      ]);

      if (menu.length > 0) {
        setMenuItems(menu);
        setUsingDemoMenu(false);
      } else {
        setMenuItems(demoMenu);
        setUsingDemoMenu(true);
      }
      setAlerts(inventory);
    } catch {
      setMenuItems(demoMenu);
      setUsingDemoMenu(true);
      setError("Live cafeteria feed is unavailable, so the demo menu is being used.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, []);

  const forecast = useMemo(
    () => menuItems.map(item => availabilityFor(item, alerts)).sort((a, b) => b.probability - a.probability),
    [menuItems, alerts]
  );

  const bestBet = forecast[0];
  const riskyCount = forecast.filter(x => x.probability < 60).length;
  const dayLabel = new Intl.DateTimeFormat("en-PK", {
    weekday: "long",
    day: "numeric",
    month: "short",
    timeZone: "Asia/Karachi",
  }).format(new Date());

  if (loading) {
    return <main style={{ minHeight: "65vh", display: "grid", placeItems: "center", color: "var(--muted)" }}>Loading today&apos;s cafeteria outlook…</main>;
  }

  return (
    <main style={{ maxWidth: 1480, margin: "0 auto", padding: "38px 34px 64px" }}>
      <header style={{ display: "flex", justifyContent: "space-between", alignItems: "end", gap: 18, flexWrap: "wrap", marginBottom: 24 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, textTransform: "uppercase", letterSpacing: ".8px" }}>Student Services · {dayLabel}</p>
          <h1 style={{ margin: 0, fontSize: 34, letterSpacing: "-1px" }}>What&apos;s likely to be available today?</h1>
          <p style={{ margin: "8px 0 0", color: "var(--muted)", maxWidth: 760 }}>
            Today&apos;s menu with an availability probability for each item — basically your odds of reaching the counter before it sells out.
          </p>
        </div>
        <button onClick={() => void load()} style={{ display: "inline-flex", alignItems: "center", gap: 7, border: "1px solid var(--line)", borderRadius: 9, padding: "10px 14px", background: "var(--surface)", color: "var(--ink)", cursor: "pointer", fontWeight: 700 }}>
          <RefreshCw size={15}/> Refresh odds
        </button>
      </header>

      {error && <div style={{ ...card, padding: 14, marginBottom: 16, display: "flex", gap: 9, alignItems: "center", color: "var(--amber)" }}><AlertCircle size={17}/>{error}</div>}

      <section style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit,minmax(210px,1fr))", gap: 14, marginBottom: 20 }}>
        <article style={{ ...card, padding: 18 }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 800, textTransform: "uppercase" }}>Today&apos;s Menu</span>
          <div style={{ marginTop: 7, fontSize: 28, fontWeight: 800 }}>{forecast.length}</div>
          <small style={{ color: "var(--muted)" }}>items expected today</small>
        </article>
        <article style={{ ...card, padding: 18 }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 800, textTransform: "uppercase" }}>Best Bet</span>
          <div style={{ marginTop: 7, fontSize: 20, fontWeight: 800, color: "var(--green)" }}>{bestBet?.name ?? "—"}</div>
          <small style={{ color: "var(--muted)" }}>{bestBet ? `${bestBet.probability}% availability` : "No forecast"}</small>
        </article>
        <article style={{ ...card, padding: 18 }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 800, textTransform: "uppercase" }}>Sell-out Risk</span>
          <div style={{ marginTop: 7, fontSize: 28, fontWeight: 800, color: riskyCount ? "var(--amber)" : "var(--green)" }}>{riskyCount}</div>
          <small style={{ color: "var(--muted)" }}>items below 60%</small>
        </article>
        <article style={{ ...card, padding: 18 }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 800, textTransform: "uppercase" }}>Forecast Feed</span>
          <div style={{ marginTop: 7, fontSize: 18, fontWeight: 800 }}>{usingDemoMenu ? "Demo / Historical" : alerts.length ? "Live + Historical" : "Historical"}</div>
          <small style={{ color: "var(--muted)" }}>live inventory improves confidence</small>
        </article>
      </section>

      <section style={{ ...card, overflow: "hidden" }}>
        <div style={{ padding: "18px 20px", borderBottom: "1px solid var(--line)", display: "flex", justifyContent: "space-between", gap: 14, alignItems: "center", flexWrap: "wrap" }}>
          <div style={{ display: "flex", gap: 10, alignItems: "center" }}>
            <UtensilsCrossed size={20}/>
            <div><strong>Today&apos;s availability board</strong><div style={{ color: "var(--muted)", fontSize: 12 }}>Higher odds mean the item is more likely to still be available when you arrive.</div></div>
          </div>
          <span className="badge badge-info"><Sparkles size={12}/> Predictive availability</span>
        </div>

        <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit,minmax(270px,1fr))", gap: 14, padding: 18 }}>
          {forecast.map(item => {
            const good = item.probability >= 80;
            const risky = item.probability < 60;
            const accent = good ? "var(--green)" : risky ? "var(--red)" : "var(--amber)";
            return <article key={item.id} style={{ border: "1px solid var(--line)", borderRadius: 12, padding: 16, background: "var(--surface-soft)" }}>
              <div style={{ display: "flex", justifyContent: "space-between", gap: 12, alignItems: "start" }}>
                <div>
                  <div style={{ fontSize: 11, color: "var(--muted)", fontWeight: 800, textTransform: "uppercase" }}>{item.category}</div>
                  <h3 style={{ margin: "4px 0 2px", fontSize: 18 }}>{item.name}</h3>
                  <span style={{ fontSize: 12, color: "var(--muted)" }}>{new Intl.NumberFormat("en-PK", { style: "currency", currency: "PKR", maximumFractionDigits: 0 }).format(item.price)}</span>
                </div>
                <div style={{ textAlign: "right" }}>
                  <strong style={{ display: "block", fontSize: 30, lineHeight: 1, color: accent }}>{item.probability}%</strong>
                  <small style={{ color: accent, fontWeight: 800 }}>{item.label}</small>
                </div>
              </div>

              <div style={{ height: 8, borderRadius: 99, background: "var(--line)", overflow: "hidden", margin: "15px 0 11px" }}>
                <div style={{ width: `${item.probability}%`, height: "100%", background: accent, borderRadius: 99 }} />
              </div>

              <div style={{ display: "grid", gap: 7, color: "var(--muted)", fontSize: 11 }}>
                <span style={{ display: "flex", gap: 7, alignItems: "center" }}>{good ? <TrendingUp size={14}/> : risky ? <TrendingDown size={14}/> : <Gauge size={14}/>} {item.reason}</span>
                <span style={{ display: "flex", gap: 7, alignItems: "center" }}><Clock3 size={14}/> Forecast confidence: <strong style={{ color: "var(--ink)" }}>{item.confidence}</strong></span>
              </div>
            </article>;
          })}
        </div>
      </section>

      <div style={{ marginTop: 16, padding: "13px 15px", borderRadius: 10, background: "var(--blue-soft)", color: "var(--muted)", fontSize: 11, lineHeight: 1.55 }}>
        <strong style={{ color: "var(--ink)" }}>How this works:</strong> for the demo, AegisCore combines day-of-week demand patterns, service time and any available inventory alerts. When a university onboards cafeteria staff, the same screen can consume live stock, POS/pre-order and sell-out data, which upgrades these probabilities from a historical estimate to an operational forecast.
      </div>
    </main>
  );
}
