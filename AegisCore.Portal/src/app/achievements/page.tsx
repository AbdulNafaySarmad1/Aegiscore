import { ModuleDashboard } from "@/components/academic/module-dashboard";

export default function AchievementsPage() {
  return <ModuleDashboard title="Student Achievements" description="Gamification engine with badges, Dean's List, leaderboards, and achievement roadmaps" apiPath="/api/achievements/catalog" metricKeys={["total"]} metricLabels={["Active Badges"]} />;
}
