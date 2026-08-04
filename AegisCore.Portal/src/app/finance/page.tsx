import { FinanceDashboard } from "@/components/finance/finance-dashboard";
import { serverGet } from "@/lib/server-api";

export type ExecutiveDashboard = {
  kpis: { revenue: number; expenses: number; cashPosition: number };
  trend: Array<{ month: string; revenue: number; expenses: number; postings: number }>;
  feeFunnel: { demanded: number; collected: number; overdue: number; open: number; paid: number };
  expenseCategories: Array<{ category: string; amount: number }>;
  budgetBurn: Array<{ costCenterId: string; name: string; allocated: number; spent: number; consumedPercent: number }>;
  currencyExposure: Array<{ currency: string; foreignAmount: number; functionalAmount: number }>;
  cashFlow: Array<{ category: string; amount: number }>;
  unclassifiedCashFlow: number;
};

export type SecurityPosture = {
  chainIntegrity: "green" | "amber" | "red";
  lastSealTimestamp: string | null;
  merkleRoot: string | null;
  signatureAlgorithm: string | null;
  keyId: string | null;
  eventCount: number;
  unsignedLedgerEntries: number;
  keys: Array<{ keyId: string; purpose: string; algorithm: string; isActive: boolean; createdAt: string; expiresAt: string; lastUsedAt: string | null; daysToExpiry: number }>;
  deadKeys: string[];
  failedVerificationAlerts: Array<{ entryId: string; entryNumber: string; reason: string }>;
};

async function optional<T>(path: string): Promise<T | null> {
  try { return await serverGet<T>(path); } catch { return null; }
}

export default async function FinancePage() {
  const [dashboard, security] = await Promise.all([
    optional<ExecutiveDashboard>("/api/finance/v2/dashboard/executive"),
    optional<SecurityPosture>("/api/finance/v2/security-posture")
  ]);
  return <FinanceDashboard dashboard={dashboard} security={security} />;
}
