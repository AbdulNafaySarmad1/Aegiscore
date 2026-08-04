import { ModuleDashboard } from "@/components/academic/module-dashboard";

export default function CafeteriaPage() {
  return <ModuleDashboard title="Cafeteria Management" description="Digital menu, cashless payments, demand forecasting, and vendor scorecards" apiPath="/api/cafeteria/units/00000000-0000-0000-0000-000000000000/orders" metricKeys={["total"]} metricLabels={["Pending Orders"]} />;
}
