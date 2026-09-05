"use client";
import { useState, useEffect } from "react";
import { apiGet } from "@/lib/api-client";
import { PersonalityModeProvider } from "@/lib/personality-context";

type Session = { userId: string };

export function PersonalityLayoutWrapper({ children }: { children: React.ReactNode }) {
  const [userId, setUserId] = useState<string | null>(null);

  useEffect(() => {
    try {
      const raw = localStorage.getItem("aegiscore_demo_session");
      // eslint-disable-next-line react-hooks/set-state-in-effect -- client-only session hydration.
      if (raw) { const acc = JSON.parse(raw); setUserId(acc.email); }
      else { setUserId(null); }
    } catch { setUserId(null); }
  }, []);

  return (
    <PersonalityModeProvider userId={userId}>
      {children}
    </PersonalityModeProvider>
  );
}
