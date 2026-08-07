"use client";

import { useEffect, useState } from "react";
import { motion } from "framer-motion";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet } from "@/lib/api-client";
import { UserRound, GraduationCap, CreditCard, Mail, Phone, MapPin, CalendarDays, BookOpenCheck } from "lucide-react";

type Session = { userId: string; tenantId: string; studentId: string | null; departmentId: string | null; name: string; roles: string[] };

export default function StudentProfile() {
  const [session, setSession] = useState<Session | null>(null);

  useEffect(() => { try {
      const raw = localStorage.getItem("aegiscore_demo_session");
      if (raw) {
        const acc = JSON.parse(raw);
        setSession({
          userId: acc.email,
          tenantId: "iobm-demo-tenant",
          studentId: acc.role === "Student" ? "STU-2024-001" : null,
          departmentId: "CS-DEPT-001",
          name: acc.name,
          roles: [acc.role],
        });
      } else { window.location.href = "/login"; }
    } catch { window.location.href = "/login"; } }, []);

  const fields = [
    { label: "Full Name", value: session?.name ?? "--", icon: UserRound },
    { label: "Student ID", value: session?.studentId?.slice(0, 8) ?? "IoBM-2024-0178", icon: GraduationCap },
    { label: "Program", value: "BS Computer Science", icon: BookOpenCheck },
    { label: "Current Semester", value: "5th", icon: CalendarDays },
    { label: "CGPA", value: "3.67 / 4.0", icon: GraduationCap },
    { label: "Credit Hours", value: "87 / 130 Completed", icon: BookOpenCheck },
    { label: "Fee Status", value: "PKR 48,500 Outstanding — Due Sep 30", icon: CreditCard },
    { label: "Contact", value: "Koenigsegg$One1", icon: Mail },
    { label: "Guardian", value: "Mr. Hassan Ali — +92 300 1234567", icon: Phone },
    { label: "Address", value: "A-12, Block 3, Gulshan-e-Iqbal, Karachi", icon: MapPin },
  ];

  return (
    <AppShell>
      <div className="content-page">
        <div className="page-header">
          <div>
            <div className="page-header-breadcrumb"><span>Student</span><span>/</span><span>Profile</span></div>
            <h1>Student Profile</h1>
            <p>Your complete academic record.</p>
          </div>
        </div>

        <div className="card" style={{ maxWidth: 680 }}>
          <div className="card-header">
            <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
              <div style={{ width: 56, height: 56, borderRadius: 12, background: "var(--primary)", color: "#fff", display: "grid", placeItems: "center", fontSize: 22, fontWeight: 700 }}>
                {(session?.name?.[0] ?? "A").toUpperCase()}
              </div>
              <div>
                <h3 style={{ margin: 0 }}>{session?.name ?? "Student"}</h3>
                <span className="badge badge-primary" style={{ marginTop: 4 }}>Student</span>
              </div>
            </div>
          </div>
          <div className="card-body" style={{ display: "grid", gap: 0 }}>
            {fields.map((f, i) => (
              <div key={i} style={{ display: "flex", alignItems: "center", gap: 12, padding: "14px 0", borderBottom: i < fields.length - 1 ? "1px solid var(--border)" : "none" }}>
                <f.icon size={18} style={{ color: "var(--muted)", flexShrink: 0 }} />
                <div>
                  <span style={{ fontSize: 11, color: "var(--muted)", textTransform: "uppercase", letterSpacing: ".5px" }}>{f.label}</span>
                  <strong style={{ display: "block", fontSize: 14 }}>{f.value}</strong>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </AppShell>
  );
}
