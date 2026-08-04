import { ModuleDashboard } from "@/components/academic/module-dashboard";

export default function PrerequisiteConflictsPage() {
  return <ModuleDashboard title="Prerequisite Conflict Detection" description="Detect circular prerequisites, offering sequence conflicts, and graduation bottlenecks" apiPath="/api/prerequisite-conflicts/unresolved" metricKeys={["total"]} metricLabels={["Unresolved Conflicts"]} />;
}
