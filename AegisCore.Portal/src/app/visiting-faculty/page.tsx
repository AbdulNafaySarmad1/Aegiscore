import { ModuleDashboard } from "@/components/academic/module-dashboard";

export default function VisitingFacultyPage() {
  return <ModuleDashboard title="Visiting Faculty Marketplace" description="Match visiting faculty to open slots, manage contracts, and track performance" apiPath="/api/visiting-faculty/eligible" metricKeys={["total"]} metricLabels={["Eligible Faculty"]} />;
}
