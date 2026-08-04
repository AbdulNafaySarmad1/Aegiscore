import { ModuleDashboard } from "@/components/academic/module-dashboard";

export default function DefaulterPredictionPage() {
  return <ModuleDashboard title="Fee Defaulter Prediction" description="AI-powered risk scoring, intervention strategies, and cash flow forecasting" apiPath="/api/defaulter-prediction/outcomes" metricKeys={["total"]} metricLabels={["Intervention Outcomes"]} />;
}
