"use client";

import { apiGet } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel, Figures, Mark } from "@/components/register";
import { Ledger, toneOf, words } from "@/components/register/ledger";
import { ModulePage, useModuleData } from "@/components/register/module-page";

type Vehicle = { id: string; registration: string; type: string; capacity: number; driver: string; status: string; nextServiceDue: string | null; lastServiceDate: string | null };
type Route = { id: string; name: string; routeType: string; distanceKm: number; stops: { stopName: string; sequence: number }[] };
type Alert = { id: string; subject: string; type: string; expiryDate: string; description: string; severity: "Critical" | "Warning" };
type FeeSummary = { studentsRegistered: number; boardingsRecorded: number; note: string | null };
type Data = { vehicles: Vehicle[]; routes: Route[]; alerts: Alert[]; fees: FeeSummary };

/** Transport: the fleet, the routes it runs, what is about to expire, and who rides. */
export default function TransportPage() {
  const { date } = useTenantFormatter();
  const { state, reload } = useModuleData<Data>(async signal => {
    const [vehicles, routes, alerts, fees] = await Promise.all([
      apiGet<Vehicle[]>("/api/transport/fleet", signal),
      apiGet<{ total: number; routes: Route[] } | Route[]>("/api/transport/routes", signal),
      apiGet<Alert[]>("/api/transport/compliance/alerts", signal),
      apiGet<FeeSummary>("/api/transport/fee-summary", signal),
    ]);
    return { vehicles, routes: Array.isArray(routes) ? routes : routes.routes ?? [], alerts, fees };
  });

  return (
    <ModulePage
      title="Transport"
      crumbs={[{ label: "Operations" }, { label: "Transport" }]}
      meta={<span>The fleet, its routes, and what falls due.</span>}
      ident={(d: Data) => <><strong>{d.vehicles.filter(v => v.status === "Active").length}/{d.vehicles.length}</strong><br />vehicles in service</>}
      state={state}
      reload={reload}
    >
      {({ vehicles, routes, alerts, fees }) => (
        <>
          <Figures aria-label="Transport figures" items={[
            { label: "Vehicles in service", value: `${vehicles.filter(v => v.status === "Active").length}/${vehicles.length}`, note: `${vehicles.filter(v => v.status === "Maintenance").length} under maintenance` },
            { label: "Routes", value: routes.length, note: routes.length === 0 ? "none configured" : `${routes.reduce((n, r) => n + (r.stops?.length ?? 0), 0)} stops` },
            { label: "Students riding", value: fees.studentsRegistered, note: fees.note ?? `${fees.boardingsRecorded} boardings recorded` },
            { label: "Falling due", value: alerts.length, tone: alerts.some(a => a.severity === "Critical") ? "oxblood" : alerts.length > 0 ? "brass" : "ink", note: alerts.filter(a => a.severity === "Critical").length + " overdue" },
          ]} />

          <div className="panel-stack">
            <Panel title="Fleet" aside={<span className="text-muted">{vehicles.length} vehicles</span>} flush>
              <Ledger rows={vehicles} keyOf={v => v.id} minWidth={720} status={v => toneOf(v.status)}
                empty={{ title: "No vehicles on the register", detail: "Vehicles are entered by the transport office; none has been recorded yet." }}
                columns={[
                  { key: "reg", label: "Registration", render: v => v.registration, mono: true },
                  { key: "type", label: "Model", render: v => v.type || "—" },
                  { key: "capacity", label: "Seats", render: v => v.capacity, num: true },
                  { key: "driver", label: "Driver", render: v => v.driver || "unassigned" },
                  { key: "status", label: "Status", render: v => <Mark tone={toneOf(v.status)}>{words(v.status)}</Mark> },
                  { key: "service", label: "Next service", render: v => v.nextServiceDue ? date(v.nextServiceDue) : "—", nowrap: true },
                ]} />
            </Panel>

            <Panel title="Routes" aside={<span className="text-muted">{routes.length} configured</span>} flush>
              <Ledger rows={routes} keyOf={r => r.id} minWidth={560}
                empty={{ title: "No routes configured", detail: "A route lists its stops in order; none has been entered." }}
                columns={[
                  { key: "name", label: "Route", render: r => <strong>{r.name}</strong> },
                  { key: "type", label: "Type", render: r => words(r.routeType) },
                  { key: "distance", label: "Distance", render: r => `${r.distanceKm} km`, num: true },
                  { key: "stops", label: "Stops", render: r => r.stops?.length ? r.stops.slice().sort((a, b) => a.sequence - b.sequence).map(s => s.stopName).join(" → ") : "—" },
                ]} />
            </Panel>

            <Panel title="Falling due" aside={<span className="text-muted">licences and maintenance within 30 days</span>} flush>
              <Ledger rows={alerts} keyOf={a => a.id} minWidth={640} status={a => a.severity === "Critical" ? "oxblood" : "brass"}
                empty={{ title: "Nothing falls due in the next 30 days", detail: "Driver licences and scheduled maintenance are checked against today's date." }}
                columns={[
                  { key: "subject", label: "Vehicle or driver", render: a => a.subject, mono: true },
                  { key: "type", label: "Item", render: a => a.type },
                  { key: "when", label: "Due", render: a => date(a.expiryDate), nowrap: true },
                  { key: "what", label: "What is due", render: a => a.description },
                ]} />
            </Panel>
          </div>
        </>
      )}
    </ModulePage>
  );
}
