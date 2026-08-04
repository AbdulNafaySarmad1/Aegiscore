import { ModuleDashboard } from "@/components/academic/module-dashboard";

export default function ReadinessPage() {
  return <ModuleDashboard title="Accreditation Readiness Meter" description="Live HEC/PEC compliance scoring, gap analysis, and predictive readiness forecasting" apiPath="/api/readiness/compute/00000000-0000-0000-0000-000000000000" metricKeys={["overallScore", "daysUntilAudit", "complianceVelocity"]} metricLabels={["Overall Score", "Days Until Audit", "Compliance Velocity"]} />;
}
