"use client";

// ─── Student Profile ───
// Renders only verified session identity (GET /api/session).
// Academic records (CGPA, program, fee balance) are intentionally absent:
// the backend identity model does not yet expose an authoritative
// user-to-student link (docs/FRONTEND_API_HANDOFF.md §Session).

import { AppShell } from "@/components/layout/AppShell";
import { useSession } from "@/components/session-provider";
import { BadgeCheck, Building2, CreditCard, Fingerprint, Mail, ShieldCheck, UserRound } from "lucide-react";

export default function StudentProfile() {
  const { session, isDemo, signOut } = useSession();

  const fields = [
    { label: "Full Name", value: session?.name ?? "—", icon: UserRound },
    { label: "Email", value: session?.userId ?? "—", icon: Mail },
    { label: "Roles", value: session?.roles.length ? session.roles.join(", ") : "—", icon: BadgeCheck },
    { label: "Student Record", value: session?.studentId ?? "Not linked to this account", icon: CreditCard },
    { label: "Campus Tenant", value: session?.tenantId ?? "—", icon: Building2 },
    { label: "Session", value: isDemo ? "Demo preview identity" : "Secure cookie session", icon: ShieldCheck },
  ];

  return (
    <AppShell>
      <div className="content-page">
        <div className="page-header">
          <div>
            <h1>My Profile</h1>
            <p>Your account identity as verified by the university directory.</p>
          </div>
        </div>

        <div className="card" style={{ maxWidth: 720 }}>
          <div className="card-header">
            <h3 style={{ fontSize: 14, display: "flex", alignItems: "center", gap: 8 }}>
              <Fingerprint size={16} /> Account Details
            </h3>
          </div>
          <div className="card-body" style={{ display: "grid", gap: 0 }}>
            {fields.map(f => (
              <div key={f.label} style={{ display: "flex", alignItems: "center", gap: 12, padding: "12px 0", borderBottom: "1px solid var(--border)" }}>
                <f.icon size={16} style={{ color: "var(--muted)", flexShrink: 0 }} />
                <span style={{ fontSize: 13, color: "var(--muted)", minWidth: 140 }}>{f.label}</span>
                <span style={{ fontSize: 14, fontWeight: 500, wordBreak: "break-all" }}>{f.value}</span>
              </div>
            ))}
          </div>
          <div className="card-footer" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 12, flexWrap: "wrap" }}>
            <p style={{ fontSize: 12, color: "var(--muted)", margin: 0 }}>
              Academic records, fee balances, and results appear once your student record is linked by the Registrar.
            </p>
            <button type="button" className="btn btn-secondary btn-sm" onClick={() => void signOut()}>
              Sign out
            </button>
          </div>
        </div>
      </div>
    </AppShell>
  );
}
