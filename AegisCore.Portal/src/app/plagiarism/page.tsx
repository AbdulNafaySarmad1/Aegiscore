import { ModuleDashboard } from "@/components/academic/module-dashboard";

export default function PlagiarismPage() {
  return <ModuleDashboard title="Plagiarism Detection" description="Cross-semester similarity detection, collusion analysis, and AI-generation flags" apiPath="/api/plagiarism/assignment/00000000-0000-0000-0000-000000000000" metricKeys={["total"]} metricLabels={["Scans Completed"]} />;
}
