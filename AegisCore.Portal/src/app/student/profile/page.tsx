"use client";

import { useEffect, useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet, apiPut } from "@/lib/api-client";
import { UserRound, GraduationCap, CreditCard, Mail, Phone, MapPin, CalendarDays, BookOpenCheck, Check } from "lucide-react";

type StudentProfile = {
  studentId: string; name: string; email: string; program: string;
  currentSemester: string; cgpa: string; creditsCompleted: string; totalCredits: string;
  enrollmentStatus: string; universityId: string; phone: string; address: string;
  guardianName: string; guardianPhone: string; paymentAlias: string;
};

export default function StudentProfilePage() {
  const [profile, setProfile] = useState<StudentProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);
  const [phone, setPhone] = useState("");
  const [address, setAddress] = useState("");
  const [paymentAlias, setPaymentAlias] = useState("");
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    apiGet<StudentProfile>("/api/student-portal/me")
      .then(p => { setProfile(p); setPhone(p.phone ?? ""); setAddress(p.address ?? ""); setPaymentAlias(p.paymentAlias ?? ""); })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const handleSave = async () => {
    setSaving(true); setSaved(false);
    try {
      await apiPut("/api/student-portal/me/profile", { phone, address, paymentAlias });
      setProfile(p => p ? { ...p, phone, address, paymentAlias } : p);
      setSaved(true);
      setEditing(false);
    } catch {} finally { setSaving(false); }
  };

  if (loading) return <AppShell><div className="content-page"><div className="skeleton" style={{height:300}} /></div></AppShell>;

  const fields = [
    { label: "Full Name", value: profile?.name ?? "--", icon: UserRound },
    { label: "Student ID", value: profile?.universityId ?? "--", icon: GraduationCap },
    { label: "Program", value: profile?.program ?? "--", icon: BookOpenCheck },
    { label: "Current Semester", value: profile?.currentSemester ?? "--", icon: CalendarDays },
    { label: "CGPA", value: (profile?.cgpa ? `${profile.cgpa} / 4.0` : "--"), icon: GraduationCap },
    { label: "Credit Hours", value: profile ? `${profile.creditsCompleted} / ${profile.totalCredits} Completed` : "--", icon: BookOpenCheck },
    { label: "Email", value: profile?.email ?? "--", icon: Mail },
    { label: "Phone", value: editing ? null : (profile?.phone ?? "--"), icon: Phone, editable: true },
    { label: "Address", value: editing ? null : (profile?.address ?? "--"), icon: MapPin, editable: true },
    { label: "Guardian", value: profile?.guardianName ? `${profile.guardianName} — ${profile.guardianPhone}` : "--", icon: Phone },
    { label: "Payment Alias", value: editing ? null : (profile?.paymentAlias ?? "--"), icon: CreditCard, editable: true },
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
          {!editing && <button className="btn btn-primary" onClick={() => setEditing(true)}>Edit Profile</button>}
          {editing && <button className="btn btn-primary" onClick={handleSave} disabled={saving}>{saving ? "Saving..." : <><Check size={16} />Save</>}</button>}
        </div>

        {saved && <div className="error-state" style={{marginBottom:12,background:"var(--success-soft)",border:"1px solid var(--success)",color:"var(--success)"}}><Check size={20} /><p>Profile updated.</p></div>}

        <div className="card" style={{ maxWidth: 680 }}>
          <div className="card-header">
            <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
              <div style={{ width: 56, height: 56, borderRadius: 12, background: "var(--primary)", color: "#fff", display: "grid", placeItems: "center", fontSize: 22, fontWeight: 700 }}>
                {(profile?.name?.[0] ?? "A").toUpperCase()}
              </div>
              <div>
                <h3 style={{ margin: 0 }}>{profile?.name ?? "Student"}</h3>
                <span className="badge badge-primary" style={{ marginTop: 4 }}>Student</span>
              </div>
            </div>
          </div>
          <div className="card-body" style={{ display: "grid", gap: 0 }}>
            {fields.map((f, i) => (
              <div key={i} style={{ display: "flex", alignItems: "center", gap: 12, padding: "14px 0", borderBottom: i < fields.length - 1 ? "1px solid var(--border)" : "none" }}>
                <f.icon size={18} style={{ color: "var(--muted)", flexShrink: 0 }} />
                <div style={{ flex: 1 }}>
                  <span style={{ fontSize: 11, color: "var(--muted)", textTransform: "uppercase", letterSpacing: ".5px" }}>{f.label}</span>
                  {f.editable && editing ? (
                    f.label === "Phone" ? <input value={phone} onChange={e => setPhone(e.target.value)} style={{width:"100%",minHeight:32,borderRadius:6,border:"1px solid var(--border)",padding:"2px 8px",fontSize:13}} /> :
                    f.label === "Address" ? <input value={address} onChange={e => setAddress(e.target.value)} style={{width:"100%",minHeight:32,borderRadius:6,border:"1px solid var(--border)",padding:"2px 8px",fontSize:13}} /> :
                    <input value={paymentAlias} onChange={e => setPaymentAlias(e.target.value)} style={{width:"100%",minHeight:32,borderRadius:6,border:"1px solid var(--border)",padding:"2px 8px",fontSize:13}} />
                  ) : (
                    <strong style={{ display: "block", fontSize: 14 }}>{f.value}</strong>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </AppShell>
  );
}
