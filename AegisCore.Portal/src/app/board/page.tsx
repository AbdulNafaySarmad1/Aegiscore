import { ModuleDashboard } from "@/components/academic/module-dashboard";

export default function BoardPage() {
  return <ModuleDashboard title="Board of Governors Portal" description="Executive dashboards, board meeting packs, strategic plan monitoring, and benchmark comparisons" apiPath="/api/board/alerts" metricKeys={["total"]} metricLabels={["Unread Alerts"]} />;
}
