// ─── AegisCore Session ─── src/lib/session.ts ───
// Browser-side session contract, per docs/FRONTEND_API_HANDOFF.md.
// Identity is established by HttpOnly cookies set by the backend;
// tokens are never read into JavaScript or localStorage.

import { apiGet, apiPost } from "@/lib/api-client";

export type SessionUser = {
  userId: string;
  tenantId: string;
  studentId: string | null;
  departmentId: string | null;
  name: string;
  roles: string[];
  permissions: string[];
};

export type AuthTokens = {
  accessToken: string;
  refreshToken: string;
  tokenType: string;
  expiresIn: number;
};

/** GET /api/session — throws ApiError(401) when unauthenticated. */
export function fetchSession(): Promise<SessionUser> {
  return apiGet<SessionUser>("/api/session");
}

/** POST /api/auth/login — sets HttpOnly cookies on success. */
export function login(email: string, password: string): Promise<AuthTokens> {
  return apiPost<AuthTokens>("/api/auth/login", { email, password });
}

/** POST /api/auth/logout — idempotent; clears both cookies server-side. */
export async function logout(): Promise<void> {
  try {
    await apiPost("/api/auth/logout", {});
  } catch {
    // Logout is idempotent; a failed call still ends the local session.
  }
}

/** Permission keys arrive as `Module:Resource:Action` strings. */
export function hasPermission(session: SessionUser | null, permission: string): boolean {
  return session?.permissions.includes(permission) ?? false;
}

export function hasAnyPermission(session: SessionUser | null, permissions: string[]): boolean {
  return permissions.some(p => hasPermission(session, p));
}

// ─── Demo mode (explicitly isolated) ───
// Demo identity exists ONLY to preview role-based UI without a backend.
// It never replaces production auth: it is consulted solely when
// NEXT_PUBLIC_DEMO_MODE=true and no real backend session exists.

const DEMO_KEY = "aegiscore_demo_session";

export const isDemoMode = process.env.NEXT_PUBLIC_DEMO_MODE === "true";

export type DemoAccount = { email: string; label: string; desc: string };

export function readDemoSession(): SessionUser | null {
  if (!isDemoMode || typeof window === "undefined") return null;
  try {
    const raw = window.localStorage.getItem(DEMO_KEY);
    if (!raw) return null;
    const acc = JSON.parse(raw) as DemoAccount;
    return {
      userId: acc.email,
      tenantId: "demo-tenant",
      studentId: acc.label === "Student" ? "demo-student" : null,
      departmentId: null,
      name: acc.desc,
      roles: [acc.label],
      permissions: []
    };
  } catch {
    return null;
  }
}

export function writeDemoSession(account: DemoAccount): void {
  if (!isDemoMode || typeof window === "undefined") return;
  window.localStorage.setItem(DEMO_KEY, JSON.stringify(account));
}

export function clearDemoSession(): void {
  if (typeof window === "undefined") return;
  window.localStorage.removeItem(DEMO_KEY);
}
