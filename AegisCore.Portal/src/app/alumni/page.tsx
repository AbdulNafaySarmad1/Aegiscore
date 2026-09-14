"use client";

import { useEffect, useState } from "react";

type Profile = { alumniId: string; graduationYear: number; currentCompany?: string; bio?: string };

export default function AlumniPage() {
  const [profile, setProfile] = useState<Profile | null>(null);
  const [message, setMessage] = useState("Loading alumni profile…");
  useEffect(() => {
    let active = true;
    fetch("/api/student-lifecycle/alumni/me", { credentials: "include", cache: "no-store" })
      .then(async response => {
        if (!response.ok) throw new Error(response.status === 404
          ? "Your alumni profile becomes available after the university approves graduation."
          : response.status === 401 ? "Sign in to view your alumni profile." : "Unable to load your alumni profile.");
        return response.json() as Promise<Profile>;
      })
      .then(value => { if (active) setProfile(value); })
      .catch(error => { if (active) setMessage(error instanceof Error ? error.message : "Unable to load profile."); });
    return () => { active = false; };
  }, []);
  return <main style={{ maxWidth: 800, margin: "48px auto", padding: 24 }}>
    <h1>Alumni profile</h1>
    {profile ? <dl>
      <dt>Alumni ID</dt><dd>{profile.alumniId}</dd>
      <dt>Graduation year</dt><dd>{profile.graduationYear}</dd>
      {profile.currentCompany && <><dt>Company</dt><dd>{profile.currentCompany}</dd></>}
      {profile.bio && <><dt>About</dt><dd>{profile.bio}</dd></>}
    </dl> : <p role="status">{message}</p>}
  </main>;
}
