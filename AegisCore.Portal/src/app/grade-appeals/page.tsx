import { GradeAppealForm } from "@/components/academic/grade-appeal-form";
import { ModuleDashboard } from "@/components/academic/module-dashboard";

export default function GradeAppealsPage() {
  return (
    <div style={{ maxWidth: 1100, margin: "0 auto", padding: "1.5rem", display: "flex", flexDirection: "column", gap: "1.5rem" }}>
      <ModuleDashboard title="Grade Appeals" description="Submit and track grade appeals, evidence, and committee decisions" apiPath="/api/grade-appeals/pending" metricKeys={["total"]} metricLabels={["Pending Appeals"]} />
      <GradeAppealForm />
    </div>
  );
}
