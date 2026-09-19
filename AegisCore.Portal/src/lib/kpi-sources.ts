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

export type StudentSummary = { enrolled: number; total: number; byStatus: Record<string, number>; asOf?: string };
export type FacultySummary = { total: number; active: number; teaching: number; byDesignation: Record<string, number>; asOf?: string };
export type ReadinessCurrent = {
  overall: { score: number; frameworkCount: number; generatedAt: string } | null;
  frameworks: Array<{ scoreId: string; frameworkId: string; frameworkName: string; overallScore: number; status: string; generatedAt: string; criteria?: unknown }>;
  asOf?: string;
};

const integer = (value: number) => new Intl.NumberFormat("en-PK", { maximumFractionDigits: 0 }).format(value);
const shortDate = (value: string) => new Intl.DateTimeFormat("en-GB", { day: "numeric", month: "short", year: "numeric" }).format(new Date(value));

async function feeCollectionRate(signal: AbortSignal): Promise<KpiReading> {
  const data = await apiGet<ExecutiveDashboard>("/api/finance/v2/dashboard/executive", signal);
  const demanded = Number(data.feeFunnel?.demanded ?? 0);
  const collected = Number(data.feeFunnel?.collected ?? 0);
  if (!(demanded > 0)) return { state: "value", value: "—", note: "no fee demands raised yet" };
  const rate = Math.min(100, Math.round((collected / demanded) * 1000) / 10);
  return { state: "value", value: `${rate}%`, note: `${data.feeFunnel?.paid ?? 0} of ${(data.feeFunnel?.paid ?? 0) + (data.feeFunnel?.open ?? 0)} demands settled` };
}

async function totalEnrolment(signal: AbortSignal): Promise<KpiReading> {
  const data = await apiGet<StudentSummary>("/api/students/summary", signal);
  const enrolled = Number(data.enrolled ?? 0);
  const total = Number(data.total ?? 0);
  if (total === 0) return { state: "value", value: "—", note: "no student records yet" };
  const graduated = Number(data.byStatus?.graduated ?? 0);
  const withdrawn = Number(data.byStatus?.withdrawn ?? 0);
  const rest = [graduated > 0 && `${integer(graduated)} graduated`, withdrawn > 0 && `${integer(withdrawn)} withdrawn`].filter(Boolean).join(", ");
  return { state: "value", value: integer(enrolled), note: rest ? `active students; ${rest}` : `of ${integer(total)} on the register` };
}

async function accreditationScore(signal: AbortSignal): Promise<KpiReading> {
  const data = await apiGet<ReadinessCurrent>("/api/readiness/current", signal);
  if (!data.overall || data.frameworks.length === 0) return { state: "value", value: "—", note: "no readiness score recorded yet" };
  // A score with no criterion breakdown carries no evidence; say so rather than print it as if it were assessed.
  const evidenced = data.frameworks.filter(f => Array.isArray(f.criteria) && f.criteria.length > 0);
  if (evidenced.length === 0) {
    return { state: "value", value: "—", note: `${data.frameworks.map(f => f.frameworkName).join(", ")}: score recorded without criterion evidence` };
  }
  const score = evidenced.length === data.frameworks.length ? data.overall.score : Math.round((evidenced.reduce((sum, f) => sum + f.overallScore, 0) / evidenced.length) * 10) / 10;
  const named = evidenced.length === 1 ? evidenced[0].frameworkName : `${evidenced.length} frameworks`;
  return { state: "value", value: `${score}%`, note: `${named}, assessed ${shortDate(data.overall.generatedAt)}` };
}

async function facultyStudentRatio(signal: AbortSignal): Promise<KpiReading> {
  // Both halves must be real; if either is missing the figure is withheld, not estimated.
  const [faculty, students] = await Promise.all([
    apiGet<FacultySummary>("/api/faculty/summary", signal),
    apiGet<StudentSummary>("/api/students/summary", signal),
  ]);
  const teaching = Number(faculty.teaching ?? 0);
  const enrolled = Number(students.enrolled ?? 0);
  if (teaching === 0) return { state: "value", value: "—", note: "no teaching faculty on the register" };
  if (enrolled === 0) return { state: "value", value: "—", note: "no active students on the register" };
  const perFaculty = Math.round((enrolled / teaching) * 10) / 10;
  return { state: "value", value: `1 : ${perFaculty}`, note: `${integer(teaching)} teaching faculty to ${integer(enrolled)} active students` };
}

type HealthReport = { status: string; infrastructure: Array<{ name: string; status: string; detail: string }>; modules: Array<{ status: string }> };

/** The report answers 503 with the same body when something is down; the body is what we want. */
async function healthReport(signal: AbortSignal): Promise<HealthReport> {
  try {
    return await apiGet<HealthReport>("/health/modules", signal);
  } catch (error) {
    if (error instanceof ApiError && error.status === 503 && error.payload && typeof error.payload === "object") return error.payload as HealthReport;
    throw error;
  }
}

// "Register health" → GET /health/modules: the overall word, with what is not answering.
async function registerHealth(signal: AbortSignal): Promise<KpiReading> {
  const report = await healthReport(signal);
  const down = report.infrastructure.filter(i => i.status === "unavailable").map(i => i.name);
  const unconfigured = report.infrastructure.filter(i => i.status === "degraded").map(i => i.name);
  const modules = report.modules.filter(m => m.status !== "healthy").length;
  const word = report.status === "healthy" ? "Healthy" : report.status === "degraded" ? "Degraded" : "Unhealthy";
  const note = down.length > 0 ? `${down.join(", ")} not answering`
    : modules > 0 ? `${modules} module${modules === 1 ? "" : "s"} with a problem`
    : unconfigured.length > 0 ? `${unconfigured.join(", ")} not configured`
    : `${report.modules.length} modules answering`;
  return { state: "value", value: word, note };
}

// "Database round trip" → GET /health/modules, the database line's measured time.
async function databaseRoundTrip(signal: AbortSignal): Promise<KpiReading> {
  const report = await healthReport(signal);
  const database = report.infrastructure.find(i => i.name === "database");
  const ms = database?.detail.match(/(\d+)\s*ms/)?.[1];
  if (!database || database.status === "unavailable") return { state: "value", value: "—", note: "the database did not answer" };
  return { state: "value", value: ms ? `${ms} ms` : "answering", note: "measured on the last health check" };
}

const resolvers: Record<string, Resolver> = {
  // IT admin "Register health" and "Database round trip" → GET /health/modules
  "register-health": registerHealth,
  "database-round-trip": databaseRoundTrip,
  // Admin, VC and SuperAdmin "Fee collection rate" → GET /api/finance/v2/dashboard/executive
  collection: feeCollectionRate,
  "collection-rate": feeCollectionRate,
  // "Total enrolment" → GET /api/students/summary
  enrollment: totalEnrolment,
  // "Accreditation score" → GET /api/readiness/current
  accreditation: accreditationScore,
  // "Faculty to student ratio" → GET /api/faculty/summary + GET /api/students/summary
  "faculty-ratio": facultyStudentRatio,
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
