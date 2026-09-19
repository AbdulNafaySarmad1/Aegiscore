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

/**
 * Finish a sign-in that arrived by redirect. An identity provider's callback lands on the API,
 * which hands the browser a one-time code rather than tokens in a URL; exchanging it through the
 * portal's proxy sets the same HttpOnly cookies a password login sets. Resolves when the session
 * is usable; throws with the API's sentence when the code is stale or the account is not active.
 */
export async function completeSsoSignIn(code: string): Promise<void> {
  await apiPost("/api/auth/sso/exchange", { code });
  localStorage.removeItem("aegiscore_demo_session");
}

/** The sign-in methods the login screen should offer this university. */
export type LoginProvider = { type: string; displayName: string; startPath: string };
export type LoginMethods = { localEnabled: boolean; providers: LoginProvider[] };
export function loadLoginMethods(): Promise<LoginMethods> {
  return apiGet<LoginMethods>("/api/auth/sso/providers");
}

export async function logout() {
  await apiPost("/api/auth/logout");
  localStorage.removeItem("aegiscore_demo_session");
  window.location.assign("/login");
}
