import { ModuleDashboard } from "@/components/academic/module-dashboard";

export default function VoicePage() {
  return <ModuleDashboard title="Voice Assistant (NidaBot)" description="Multilingual voice interface supporting Urdu, Roman Urdu, and English for ERP operations" apiPath="/api/voice/sessions/00000000-0000-0000-0000-000000000000" metricKeys={["total"]} metricLabels={["Voice Sessions"]} />;
}
