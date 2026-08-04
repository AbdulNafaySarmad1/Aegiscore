import { ModuleDashboard } from "@/components/academic/module-dashboard";

export default function MentalHealthPage() {
  return <ModuleDashboard title="Mental Health Support" description="Anonymous ticketing, counselor routing, crisis protocols, and wellness monitoring" apiPath="/api/mental-health/counselor/00000000-0000-0000-0000-000000000000/queue" metricKeys={["total"]} metricLabels={["Active Tickets"]} />;
}
