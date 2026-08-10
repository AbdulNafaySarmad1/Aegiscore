"use client";

import { useEffect, useState } from "react";

export function useRoles(): string[] {
  const [roles, setRoles] = useState<string[]>([]);
  useEffect(() => {
    try {
      const token = localStorage.getItem("aegiscore_access_token");
      if (token) {
        const payload = token.split(".")[1];
        if (!payload) return;
        const json = JSON.parse(atob(payload));
        const roleClaim = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
        const raw = json[roleClaim] || json["role"];
        if (Array.isArray(raw)) setRoles(raw);
        else if (typeof raw === "string") setRoles([raw]);
      }
    } catch { /* ignore invalid JWT */ }
  }, []);
  return roles;
}

export function isStudentOnly(roles: string[]): boolean {
  return roles.length === 1 && roles[0] === "Student";
}

export function canManage(roles: string[]): boolean {
  if (roles.length === 0) return false;
  return !isStudentOnly(roles);
}
