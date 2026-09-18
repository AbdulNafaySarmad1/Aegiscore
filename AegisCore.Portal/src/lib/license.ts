import { apiGet, apiPost } from "@/lib/api-client";

/** What /api/license/status answers: the standing of the installation's licence, in words an administrator acts on. */
export type LicenseStatus = {
  status: string;
  isLocked: boolean;
  lockReason: string | null;
  lockedAt: string | null;
  lastHeartbeatAt: string | null;
  failedHeartbeatCount: number;
  overrideUntil: string | null;
  tier: number | string;
  universityId: string | null;
  universityName: string | null;
  severity: "ok" | "warning" | "locked";
  message: string | null;
  nextStep: string | null;
  graceUntil: string | null;
  isOffline: boolean;
  expiryDate: string | null;
};

export function loadLicenseStatus(signal?: AbortSignal) {
  return apiGet<LicenseStatus>("/api/license/status", signal);
}

export function activateLicense(licenseKey: string, universityId: string) {
  return apiPost<{ activated: boolean; heartbeat: string; message: string; status: LicenseStatus }>("/api/admin/license/activate", { licenseKey: licenseKey.trim(), universityId });
}

export function tierName(tier: number | string) {
  if (typeof tier === "string") return tier;
  return ({ 0: "Basic", 1: "Standard", 2: "Premium" } as Record<number, string>)[tier] ?? String(tier);
}
