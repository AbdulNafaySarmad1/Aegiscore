"use client";

import { useEffect, useRef, useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet, apiPut } from "@/lib/api-client";
import { Camera, Loader2, Save, ShieldCheck, UserRound } from "lucide-react";

type Profile = {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  username: string;
  roles: string[];
  employeeCode?: string | null;
  designation?: string | null;
  department?: string | null;
  avatarThumbnailUrl: string;
  avatarFullUrl: string;
};

export default function ProfilePage() {
  const [profile, setProfile] = useState<Profile | null>(null);
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [avatarNonce, setAvatarNonce] = useState(0);
  const [saving, setSaving] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);

  const load = async () => {
    setError("");
    try {
      const p = await apiGet<Profile>("/api/me/profile");
      setProfile(p);
      setFirstName(p.firstName ?? "");
      setLastName(p.lastName ?? "");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Unable to load profile.");
    }
  };

  useEffect(() => { void load(); }, []);

  const save = async () => {
    setSaving(true); setMessage(""); setError("");
    try {
      await apiPut("/api/me/profile", { firstName, lastName });
      setMessage("Profile updated successfully.");
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Unable to update profile.");
    } finally { setSaving(false); }
  };

  const uploadAvatar = async (file: File) => {
    if (!file.type.startsWith("image/")) { setError("Please select an image file."); return; }
    if (file.size > 5 * 1024 * 1024) { setError("Profile picture must be 5 MB or smaller."); return; }
    setUploading(true); setMessage(""); setError("");
    try {
      const token = localStorage.getItem("aegiscore_access_token");
      const response = await fetch("/api/profile-pictures/upload", {
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": file.type || "image/jpeg",
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: file,
      });
      const body = await response.json().catch(() => null);
      if (!response.ok) throw new Error(body?.error ?? `Upload failed (${response.status})`);
      setAvatarNonce(Date.now());
      setMessage("Profile picture updated successfully.");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Unable to upload profile picture.");
    } finally {
      setUploading(false);
      if (inputRef.current) inputRef.current.value = "";
    }
  };

  return (
    <AppShell>
      <div className="content-page">
        <div className="page-header">
          <div>
            <div className="page-header-breadcrumb"><span>Account</span><span>/</span><span>My Profile</span></div>
            <h1>My Profile</h1>
            <p>Manage your display name and profile picture. Institutional identity fields remain protected.</p>
          </div>
        </div>

        {error && <div className="error-state" style={{ marginBottom: 16 }}><p>{error}</p></div>}
        {message && <div style={{ marginBottom: 16, padding: 12, borderRadius: 10, border: "1px solid var(--success)", background: "var(--success-soft)", color: "var(--success)" }}>{message}</div>}

        {!profile ? (
          <div className="card"><div className="card-body" style={{ display: "flex", gap: 10, alignItems: "center" }}><Loader2 size={18} className="animate-spin" /> Loading profile…</div></div>
        ) : (
          <div style={{ display: "grid", gridTemplateColumns: "minmax(250px,.7fr) minmax(0,1.3fr)", gap: 16 }}>
            <div className="card">
              <div className="card-body" style={{ textAlign: "center" }}>
                <div style={{ width: 132, height: 132, margin: "0 auto 14px", borderRadius: "50%", overflow: "hidden", border: "3px solid var(--border)", display: "grid", placeItems: "center", background: "var(--surface-2)" }}>
                  <img
                    src={`${profile.avatarFullUrl}?v=${avatarNonce}`}
                    alt={`${profile.firstName} ${profile.lastName}`}
                    style={{ width: "100%", height: "100%", objectFit: "cover" }}
                    onError={e => { e.currentTarget.style.display = "none"; }}
                  />
                  <UserRound size={42} style={{ position: "absolute", zIndex: -1 }} />
                </div>
                <h2 style={{ margin: "0 0 4px", fontSize: 20 }}>{profile.firstName} {profile.lastName}</h2>
                <div style={{ color: "var(--muted)", fontSize: 13 }}>{profile.designation ?? profile.roles.join(", ")}</div>
                <div style={{ color: "var(--muted)", fontSize: 12, marginTop: 3 }}>{profile.department ?? "Department not assigned"}</div>

                <input ref={inputRef} type="file" accept="image/jpeg,image/png,image/webp" style={{ display: "none" }} onChange={e => { const file = e.target.files?.[0]; if (file) void uploadAvatar(file); }} />
                <button className="btn btn-primary" style={{ marginTop: 16, width: "100%" }} onClick={() => inputRef.current?.click()} disabled={uploading}>
                  {uploading ? <><Loader2 size={16} className="animate-spin" /> Uploading…</> : <><Camera size={16} /> Change Avatar</>}
                </button>
                <p style={{ margin: "9px 0 0", color: "var(--muted)", fontSize: 11 }}>JPEG, PNG or WebP · max 5 MB · face validation applies.</p>
              </div>
            </div>

            <div className="card">
              <div className="card-header"><strong>Personal & Institutional Details</strong></div>
              <div className="card-body" style={{ display: "grid", gap: 14 }}>
                <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
                  <Field label="First name"><input value={firstName} onChange={e => setFirstName(e.target.value)} /></Field>
                  <Field label="Last name"><input value={lastName} onChange={e => setLastName(e.target.value)} /></Field>
                </div>
                <Field label="Email"><input value={profile.email} disabled /></Field>
                <Field label="Username"><input value={profile.username} disabled /></Field>
                <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
                  <Field label="Employee code"><input value={profile.employeeCode ?? "—"} disabled /></Field>
                  <Field label="Department"><input value={profile.department ?? "—"} disabled /></Field>
                </div>
                <div style={{ display: "flex", gap: 8, alignItems: "flex-start", color: "var(--muted)", fontSize: 12, padding: 11, border: "1px solid var(--border)", borderRadius: 10 }}>
                  <ShieldCheck size={17} /> Email, employee code, designation and department are controlled by HR/identity administration and cannot be self-edited.
                </div>
                <button className="btn btn-primary btn-lg" onClick={() => void save()} disabled={saving}>
                  {saving ? <><Loader2 size={16} className="animate-spin" /> Saving…</> : <><Save size={16} /> Save Profile</>}
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </AppShell>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return <label style={{ display: "grid", gap: 6, fontSize: 12, fontWeight: 700 }}>{label}<div style={{ display: "grid" }}>{children}</div><style>{`label input{min-height:42px;border:1px solid var(--border);border-radius:9px;padding:0 11px;background:var(--surface);color:var(--ink);font:inherit}label input:disabled{opacity:.72;background:var(--surface-2)}`}</style></label>;
}
