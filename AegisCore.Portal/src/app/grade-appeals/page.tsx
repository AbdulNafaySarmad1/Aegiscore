import { ModuleDashboard } from "@/components/academic/module-dashboard";

export default function GradeAppealsPage() {
  return <ModuleDashboard title="Grade Appeals" description="Manage student grade appeals, evidence, and committee decisions" apiPath="/api/grade-appeals/pending" metricKeys={["total"]} metricLabels={["Pending Appeals"]} />;
}
