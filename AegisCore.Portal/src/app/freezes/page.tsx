import { ModuleDashboard } from "@/components/academic/module-dashboard";

export default function FreezesPage() {
  return <ModuleDashboard title="Semester Freeze & Deferral" description="Process semester freeze requests, withdrawals, and return management" apiPath="/api/freezes/student/00000000-0000-0000-0000-000000000000" metricKeys={["total"]} metricLabels={["Active Freezes"]} />;
}
