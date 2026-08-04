import { AuditDashboard } from "@/components/finance/audit-dashboard";
import { serverGet } from "@/lib/server-api";

export type AuditRun = { id: string; trigger: string; referenceType: string; referenceId: string | null; controlCount: number; passedCount: number; flaggedCount: number; contentHash: string; evaluatedAt: string };
export type AuditorEngagement = { id: string; auditorName: string; auditorEmail: string; scopesJson: string; createdAt: string; expiresAt: string; revokedAt: string | null };
export type EvidencePackage = { id: string; framework: string; periodFrom: string; periodTo: string; contentHash: string; pqcAlgorithm: string; pqcKeyId: string; generatedAt: string };
export type AuditDashboardData = { controlCount: number; latestRun: AuditRun | null; runs: AuditRun[]; activeEngagements: number; engagements: AuditorEngagement[]; packages: EvidencePackage[] };
export type SodRule = { id: string; code: string; description: string; incompatibleActionsJson: string; isActive: boolean; updatedAt: string };

async function optional<T>(path: string): Promise<T | null> {
  try { return await serverGet<T>(path); } catch { return null; }
}

export default async function AuditPage() {
  const [dashboard, rules] = await Promise.all([
    optional<AuditDashboardData>("/api/finance/v2/audit/dashboard"),
    optional<SodRule[]>("/api/finance/v2/audit/sod-rules")
  ]);
  return <AuditDashboard dashboard={dashboard} rules={rules ?? []} />;
}
