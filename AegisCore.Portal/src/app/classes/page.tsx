import { ModuleDashboard } from "@/components/academic/module-dashboard";

export default function ClassesPage() {
  return <ModuleDashboard title="Class Generation & LMS" description="Auto-generate classes, distribute students into sections, and provision LMS spaces" apiPath="/api/classes/offering/00000000-0000-0000-0000-000000000000" metricKeys={["total"]} metricLabels={["Active Classes"]} />;
}
