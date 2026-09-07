import { apiGet, apiPost } from "./api-client";

export type Session = { userId: string; tenantId: string; studentId: string | null; departmentId: string | null; name: string; roles: string[] };

export async function loadSession(): Promise<Session> {
  if (process.env.NEXT_PUBLIC_DEMO_MODE === "true") {
    const raw = localStorage.getItem("aegiscore_demo_session");
    if (raw) {
      const account = JSON.parse(raw);
      return { userId: account.email, tenantId: "iobm-demo-tenant", studentId: account.label === "Student" ? "STU-2024-001" : null,
        departmentId: null, name: account.desc ?? account.name ?? account.email, roles: [account.label === "Vice Chancellor" ? "VC" : account.label ?? account.role] };
    }
  }
  return apiGet<Session>("/api/session");
}

export async function logout() {
  await apiPost("/api/auth/logout");
  localStorage.removeItem("aegiscore_demo_session");
  window.location.assign("/login");
}
