"use client";

import { apiGet } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel, Figures, Mark } from "@/components/register";
import { Ledger, toneOf, words } from "@/components/register/ledger";
import { ModulePage, useModuleData } from "@/components/register/module-page";

type MenuItem = { id: string; name: string; category: string; price: number; isHalalCertified: boolean; allergens: string | null; isAvailable: boolean };
type Order = { id: string; studentId: string; status: string; quantity: number; totalAmount: number; readyAt: string; createdAt: string };
type Alert = { id: string; itemName: string; quantityOnHand: number; reorderLevel: number; expiryDate: string | null };
type Data = { menu: MenuItem[]; orders: Order[]; alerts: Alert[] };

/** Cafeteria: the menu, the pre-orders placed against it, and what needs reordering. */
export default function CafeteriaPage() {
  const { date, money } = useTenantFormatter();
  const { state, reload } = useModuleData<Data>(async signal => {
    const [menu, orders, alerts] = await Promise.all([
      apiGet<MenuItem[]>("/api/cafeteria/menu-items", signal),
      apiGet<Order[]>("/api/cafeteria/orders", signal),
      apiGet<Alert[]>("/api/cafeteria/inventory-alerts", signal),
    ]);
    return { menu, orders, alerts };
  });

  return (
    <ModulePage
      title="Cafeteria"
      crumbs={[{ label: "Campus operations" }, { label: "Cafeteria" }]}
      meta={<span>Menu, pre-orders and stock below the reorder point.</span>}
      ident={(d: Data) => <><strong>{d.menu.filter(m => m.isAvailable).length}</strong><br />items available</>}
      state={state} reload={reload}
    >
      {({ menu, orders, alerts }) => (
        <>
          <Figures aria-label="Cafeteria figures" items={[
            { label: "Menu items", value: menu.length, note: `${menu.filter(m => m.isAvailable).length} available today` },
            { label: "Halal certified", value: menu.filter(m => m.isHalalCertified).length, note: menu.length === 0 ? "no menu recorded" : `of ${menu.length} items` },
            { label: "Pre-orders", value: orders.length, note: `${orders.filter(o => o.status !== "collected").length} outstanding` },
            { label: "Below reorder point", value: alerts.length, tone: alerts.length > 0 ? "brass" : "ink", note: alerts.length === 0 ? "stock is adequate" : "ingredients to reorder" },
          ]} />

          <div className="panel-stack">
            <Panel title="Menu" aside={<span className="text-muted">{menu.length} items</span>} flush>
              <Ledger rows={menu} keyOf={m => m.id} minWidth={680} status={m => m.isAvailable ? "laurel" : "ink"}
                empty={{ title: "No menu recorded", detail: "The cafeteria records each item with its price, allergens and halal certification." }}
                columns={[
                  { key: "name", label: "Item", render: m => <strong>{m.name}</strong> },
                  { key: "category", label: "Category", render: m => words(m.category) },
                  { key: "price", label: "Price", render: m => money(Number(m.price)), num: true },
                  { key: "halal", label: "Halal", render: m => m.isHalalCertified ? <Mark tone="laurel">Certified</Mark> : <Mark tone="brass">Not certified</Mark> },
                  { key: "allergens", label: "Allergens", render: m => m.allergens || "none declared" },
                  { key: "available", label: "Availability", render: m => m.isAvailable ? "available" : "off the menu" },
                ]} />
            </Panel>

            <Panel title="Pre-orders" aside={<span className="text-muted">{orders.length} placed</span>} flush>
              <Ledger rows={orders} keyOf={o => o.id} minWidth={600} status={o => toneOf(o.status)}
                empty={{ title: "No pre-orders", detail: "Students place pre-orders against the menu; they appear here as they are made." }}
                columns={[
                  { key: "student", label: "Student", render: o => o.studentId, mono: true },
                  { key: "quantity", label: "Quantity", render: o => o.quantity, num: true },
                  { key: "amount", label: "Amount", render: o => money(Number(o.totalAmount)), num: true },
                  { key: "status", label: "Status", render: o => <Mark tone={toneOf(o.status)}>{words(o.status)}</Mark> },
                  { key: "ready", label: "Ready at", render: o => date(o.readyAt), nowrap: true },
                  { key: "placed", label: "Placed", render: o => date(o.createdAt), nowrap: true },
                ]} />
            </Panel>

            <Panel title="Stock to reorder" aside={<span className="text-muted">at or below the reorder point</span>} flush>
              <Ledger rows={alerts} keyOf={a => a.id} minWidth={520} status={() => "brass"}
                empty={{ title: "Nothing to reorder", detail: "Every ingredient is above its reorder point." }}
                columns={[
                  { key: "item", label: "Ingredient", render: a => a.itemName },
                  { key: "onhand", label: "On hand", render: a => a.quantityOnHand, num: true },
                  { key: "reorder", label: "Reorder at", render: a => a.reorderLevel, num: true },
                  { key: "expiry", label: "Expires", render: a => a.expiryDate ? date(a.expiryDate) : "—", nowrap: true },
                ]} />
            </Panel>
          </div>
        </>
      )}
    </ModulePage>
  );
}
