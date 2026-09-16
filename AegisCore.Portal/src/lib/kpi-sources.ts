// Resolvers for dashboard figures. A KPI is shown on the home page only if it has a
// resolver here that reads a real endpoint; anything else stays off the page rather
// than showing a placeholder. See roles.ts for the declarations.
import { apiGet, ApiError } from "@/lib/api-client";

export type KpiReading =
  | { state: "value"; value: string; note?: string }
  | { state: "forbidden"; note: string }
  | { state: "unavailable"; note: string };

type Resolver = (signal: AbortSignal) => Promise<KpiReading>;

type ExecutiveDashboard = { feeFunnel?: { demanded?: number; collected?: number; overdue?: number; open?: number; paid?: number } };

async function feeCollectionRate(signal: AbortSignal): Promise<KpiReading> {
  const data = await apiGet<ExecutiveDashboard>("/api/finance/v2/dashboard/executive", signal);
  const demanded = Number(data.feeFunnel?.demanded ?? 0);
  const collected = Number(data.feeFunnel?.collected ?? 0);
  if (!(demanded > 0)) return { state: "value", value: "—", note: "no fee demands raised yet" };
  const rate = Math.min(100, Math.round((collected / demanded) * 1000) / 10);
  return { state: "value", value: `${rate}%`, note: `${data.feeFunnel?.paid ?? 0} of ${(data.feeFunnel?.paid ?? 0) + (data.feeFunnel?.open ?? 0)} demands settled` };
}

const resolvers: Record<string, Resolver> = {
  // Admin, VC and SuperAdmin "Fee Collection Rate %" → GET /api/finance/v2/dashboard/executive
  collection: feeCollectionRate,
  "collection-rate": feeCollectionRate,
};

export function hasKpiSource(id: string) { return id in resolvers; }

export async function readKpi(id: string, signal: AbortSignal): Promise<KpiReading> {
  const resolver = resolvers[id];
  if (!resolver) return { state: "unavailable", note: "No data source" };
  try {
    return await resolver(signal);
  } catch (error) {
    if (error instanceof ApiError && error.status === 403) return { state: "forbidden", note: "Not available to your role" };
    return { state: "unavailable", note: "Not reachable right now" };
  }
}
