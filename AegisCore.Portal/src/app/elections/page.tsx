import { ModuleDashboard } from "@/components/academic/module-dashboard";

export default function ElectionsPage() {
  return <ModuleDashboard title="Student Elections" description="Secure online elections, candidate nominations, encrypted balloting, and result certification" apiPath="/api/elections/00000000-0000-0000-0000-000000000000/tallies" metricKeys={["total"]} metricLabels={["Election Results"]} />;
}
