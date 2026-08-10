"use client";

import { useEffect, useState } from "react";

function decodeJwtPayload(token: string): Record<string, unknown> | null {
  const payload = token.split(".")[1];
  if (!payload) return null;

  try {
    const normalized = payload.replace(/-/g, "+").replace(/_/g, "/");
    const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, "=");
    return JSON.parse(atob(padded)) as Record<string, unknown>;
  } catch {
    return null;
  }
}

export function useRoles(): string[] {
  const [roles, setRoles] = useState<string[]>([]);

  useEffect(() => {
    try {
      const token = localStorage.getItem("aegiscore_access_token");
      if (!token) return;

      const json = decodeJwtPayload(token);
      if (!json) return;

      const roleClaim = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
      const raw = json[roleClaim] ?? json.role ?? json.roles;
      if (Array.isArray(raw)) setRoles(raw.filter((r): r is string => typeof r === "string"));
      else if (typeof raw === "string") setRoles([raw]);
    } catch {
      setRoles([]);
    }
  }, []);

  return roles;
}

export function isStudentOnly(roles: string[]): boolean {
  return roles.length === 1 && roles[0] === "Student";
}

export function canManage(roles: string[]): boolean {
  return roles.length > 0 && !isStudentOnly(roles);
}
