"use client";

import { useEffect, useState } from "react";
import { apiGet } from "@/lib/api-client";
import { PersonalityModeProvider } from "@/lib/personality-context";

type Session = { userId: string };

export function PersonalityLayoutWrapper({ children }: { children: React.ReactNode }) {
  const [userId, setUserId] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    apiGet<Session>("/api/session")
      .then(session => {
        if (!cancelled) setUserId(session.userId);
      })
      .catch(() => {
        if (!cancelled) setUserId(null);
      });

    return () => { cancelled = true; };
  }, []);

  return (
    <PersonalityModeProvider userId={userId}>
      {children}
    </PersonalityModeProvider>
  );
}
