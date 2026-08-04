import { DepartmentFinanceDashboard, type DepartmentDashboard } from "@/components/finance/department-finance-dashboard";
import { serverGet } from "@/lib/server-api";

export default async function DepartmentFinancePage({ params }: { params: Promise<{ departmentId: string }> }) {
  const { departmentId } = await params;
  let dashboard: DepartmentDashboard | null = null;
  try { dashboard = await serverGet<DepartmentDashboard>(`/api/finance/v2/dashboard/department/${encodeURIComponent(departmentId)}`); } catch { /* rendered by component */ }
  return <DepartmentFinanceDashboard dashboard={dashboard} />;
}
