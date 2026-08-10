"use client";

import { ChangeEvent, useEffect, useRef, useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet, apiPost, apiPut } from "@/lib/api-client";
import { UserRound, GraduationCap, CreditCard, Mail, Phone, MapPin, CalendarDays, BookOpenCheck, Check, Camera } from "lucide-react";

type StudentProfile = {
  studentId: string; name: string; email: string; program: string;
  currentSemester: string; cgpa: string; creditsCompleted: string; totalCredits: string;
  enrollmentStatus: string; universityId: string; phone: string; address: string;
  guardianName: string; guardianPhone: string; paymentAlias: string;
};

type AvatarResponse = { dataUrl: string | null };

const inputStyle: React.CSSProperties = {
  width: "100%", minHeight: 36, borderRadius: 6, border: "1px solid var(--border)",
  padding: "4px 8px", fontSize: 13, background: "var(--surface)", color: "var(--ink)"
};

export default function StudentProfilePage() {
  const [profile, setProfile] = useState<StudentProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);
  const [preferredName, setPreferredName] = useState("");
  const [phone, setPhone] = useState("");
  const [address, setAddress] = useState("");
  const [paymentAlias, setPaymentAlias] = useState("");
  const [avatar, setAvatar] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [uploadingAvatar, setUploadingAvatar] = useState(false);
  const [message, setMessage] = useState("");
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    let cancelled = false;
    Promise.all([
      apiGet<StudentProfile>("/api/student-portal/me"),
      apiGet<AvatarResponse>("/api/student-profile-media/me/avatar").catch(() => ({ dataUrl: null })),
    ])
      .then(([p, media]) => {
        if (cancelled) return;
        setProfile(p);
        setPreferredName(p.name ?? "");
        setPhone(p.phone ?? "");
        setAddress(p.address ?? "");
        setPaymentAlias(p.paymentAlias ?? "");
        setAvatar(media.dataUrl);
      })
      .catch(err => {
        if (!cancelled) setMessage(err instanceof Error ? err.message : "Failed to load profile.");
      })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, []);

  const handleSave = async () => {
    const name = preferredName.trim();
    if (!name) { setMessage("Name cannot be empty."); return; }
    setSaving(true);
    setMessage("");
    try {
      await apiPut("/api/student-portal/me/profile", { phone, address, paymentAlias, preferredName: name });
      setProfile(p => p ? { ...p, name, phone, address, paymentAlias } : p);
      setPreferredName(name);
      setMessage("Profile updated successfully.");
      setEditing(false);
    } catch (err) {
      setMessage(err instanceof Error ? err.message : "Profile update failed.");
    } finally {
      setSaving(false);
    }
  };

  const handleAvatar = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) return;
    if (file.size > 2 * 1024 * 1024) { setMessage("Profile image must be 2 MB or smaller."); return; }
    if (!["image/jpeg", "image/png", "image/webp"].includes(file.type)) { setMessage("Use a JPEG, PNG, or WebP image."); return; }

    const form = new FormData();
    form.append("avatar", file);
    setUploadingAvatar(true);
    setMessage("");
    try {
      const response = await apiPost<AvatarResponse>("/api/student-profile-media/me/avatar", form);
      setAvatar(response.dataUrl);
      setMessage("Profile picture updated.");
    } catch (err) {
      setMessage(err instanceof Error ? err.message : "Profile picture upload failed.");
    } finally {
      setUploadingAvatar(false);
    }
  };

  if (loading) return <AppShell><div className="content-page"><div className="skeleton" style={{ height: 300 }} /></div></AppShell>;

  const fields = [
    { label: "Full Name", value: profile?.name ?? "--", icon: UserRound, editable: "name" },
    { label: "Student ID", value: profile?.universityId ?? "--", icon: GraduationCap },
    { label: "Program", value: profile?.program ?? "--", icon: BookOpenCheck },
    { label: "Current Semester", value: profile?.currentSemester ?? "--", icon: CalendarDays },
    { label: "CGPA", value: profile?.cgpa ? `${profile.cgpa} / 4.0` : "--", icon: GraduationCap },
    { label: "Credit Hours", value: profile ? `${profile.creditsCompleted} / ${profile.totalCredits} Completed` : "--", icon: BookOpenCheck },
    { label: "Email", value: profile?.email ?? "--", icon: Mail },
    { label: "Phone", value: profile?.phone ?? "--", icon: Phone, editable: "phone" },
    { label: "Address", value: profile?.address ?? "--", icon: MapPin, editable: "address" },
    { label: "Guardian", value: profile?.guardianName ? `${profile.guardianName} — ${profile.guardianPhone}` : "--", icon: Phone },
    { label: "Payment Alias", value: profile?.paymentAlias ?? "--", icon: CreditCard, editable: "paymentAlias" },
  ];

  return (
    <AppShell>
      <div className="content-page">
        <div className="page-header">
          <div>
            <div className="page-header-breadcrumb"><span>Student</span><span>/</span><span>Profile</span></div>
            <h1>Student Profile</h1>
            <p>Manage your display name, contact information, payment alias, and profile picture.</p>
          </div>
          {!editing ? <button className="btn btn-primary" onClick={() => setEditing(true)}>Edit Profile</button> : (
            <div style={{ display: "flex", gap: 8 }}>
              <button className="btn btn-ghost" onClick={() => { setEditing(false); setPreferredName(profile?.name ?? ""); setPhone(profile?.phone ?? ""); setAddress(profile?.address ?? ""); setPaymentAlias(profile?.paymentAlias ?? ""); }}>Cancel</button>
              <button className="btn btn-primary" onClick={() => void handleSave()} disabled={saving}>{saving ? "Saving..." : <><Check size={16} />Save</>}</button>
            </div>
          )}
        </div>

        {message && <div className="error-state" style={{ marginBottom: 12 }}><Check size={20} /><p>{message}</p></div>}

        <div className="card" style={{ maxWidth: 720 }}>
          <div className="card-header">
            <div style={{ display: "flex", alignItems: "center", gap: 14 }}>
              <div style={{ position: "relative", width: 72, height: 72, flexShrink: 0 }}>
                {avatar ? (
                  <img src={avatar} alt="Student profile" style={{ width: 72, height: 72, objectFit: "cover", borderRadius: 14, border: "1px solid var(--border)" }} />
                ) : (
                  <div style={{ width: 72, height: 72, borderRadius: 14, background: "var(--primary)", color: "#fff", display: "grid", placeItems: "center", fontSize: 26, fontWeight: 700 }}>
                    {(profile?.name?.[0] ?? "A").toUpperCase()}
                  </div>
                )}
                <button
                  type="button"
                  className="btn btn-primary btn-sm"
                  aria-label="Change profile picture"
                  title="Change profile picture"
                  disabled={uploadingAvatar}
                  onClick={() => fileInputRef.current?.click()}
                  style={{ position: "absolute", right: -8, bottom: -8, width: 32, height: 32, padding: 0, borderRadius: "50%" }}
                ><Camera size={15} /></button>
                <input ref={fileInputRef} type="file" accept="image/jpeg,image/png,image/webp" hidden onChange={handleAvatar} />
              </div>
              <div>
                <h3 style={{ margin: 0 }}>{profile?.name ?? "Student"}</h3>
                <span className="badge badge-primary" style={{ marginTop: 4 }}>Student</span>
                <small style={{ display: "block", marginTop: 5, color: "var(--muted)" }}>{uploadingAvatar ? "Uploading picture..." : "JPEG, PNG or WebP · max 2 MB"}</small>
              </div>
            </div>
          </div>
          <div className="card-body" style={{ display: "grid", gap: 0 }}>
            {fields.map((f, i) => (
              <div key={f.label} style={{ display: "flex", alignItems: "center", gap: 12, padding: "14px 0", borderBottom: i < fields.length - 1 ? "1px solid var(--border)" : "none" }}>
                <f.icon size={18} style={{ color: "var(--muted)", flexShrink: 0 }} />
                <div style={{ flex: 1 }}>
                  <span style={{ fontSize: 11, color: "var(--muted)", textTransform: "uppercase", letterSpacing: ".5px" }}>{f.label}</span>
                  {editing && f.editable === "name" ? <input value={preferredName} maxLength={200} onChange={e => setPreferredName(e.target.value)} style={inputStyle} /> :
                   editing && f.editable === "phone" ? <input value={phone} maxLength={60} onChange={e => setPhone(e.target.value)} style={inputStyle} /> :
                   editing && f.editable === "address" ? <input value={address} maxLength={500} onChange={e => setAddress(e.target.value)} style={inputStyle} /> :
                   editing && f.editable === "paymentAlias" ? <input value={paymentAlias} maxLength={100} onChange={e => setPaymentAlias(e.target.value)} style={inputStyle} /> :
                   <strong style={{ display: "block", fontSize: 14 }}>{f.value}</strong>}
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </AppShell>
  );
}
