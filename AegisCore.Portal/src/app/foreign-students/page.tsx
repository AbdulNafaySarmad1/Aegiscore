import { ModuleDashboard } from "@/components/academic/module-dashboard";

export default function ForeignStudentsPage() {
  return <ModuleDashboard title="Foreign Student Management" description="Visa tracking, MORA compliance, document expiry alerts, and renewal workflows" apiPath="/api/foreign-students/compliance" metricKeys={["total"]} metricLabels={["Non-Compliant Students"]} />;
}
