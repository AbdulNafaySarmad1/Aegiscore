import { ModuleDashboard } from "@/components/academic/module-dashboard";

export default function GradeDistributionsPage() {
  return <ModuleDashboard title="Grade Distribution Analysis" description="Statistical analysis and anomaly detection across course grades" apiPath="/api/grade-distributions/anomalies/none" metricKeys={["total"]} metricLabels={["Distributions Analyzed"]} />;
}
