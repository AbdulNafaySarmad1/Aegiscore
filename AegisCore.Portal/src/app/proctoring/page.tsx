import { ModuleDashboard } from "@/components/academic/module-dashboard";

export default function ProctoringPage() {
  return <ModuleDashboard title="Online Exam Proctoring" description="Remote exam integrity monitoring, browser lockdown, and forensic analysis" apiPath="/api/proctoring/sessions/00000000-0000-0000-0000-000000000000/records" metricKeys={["total"]} metricLabels={["Proctored Students"]} />;
}
