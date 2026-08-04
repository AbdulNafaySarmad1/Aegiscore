import { ModuleDashboard } from "@/components/academic/module-dashboard";

export default function ConvocationsPage() {
  return <ModuleDashboard title="Convocation Management" description="Eligibility checks, certificate generation, ceremony attendance tracking" apiPath="/api/convocations/upcoming" metricKeys={["total"]} metricLabels={["Upcoming Ceremonies"]} />;
}
