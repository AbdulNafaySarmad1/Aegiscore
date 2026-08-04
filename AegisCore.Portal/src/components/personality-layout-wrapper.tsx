"use client";
import { useState, useEffect } from "react";
import { apiGet } from "@/lib/api-client";
import { PersonalityModeProvider } from "@/lib/personality-context";

type Session = { userId: string };

export function PersonalityLayoutWrapper({ children }: { children: React.ReactNode }) {
  const [userId, setUserId] = useState<string | null>(null);

  useEffect(() => {
    apiGet<Session>("/api/session")
      .then(s => setUserId(s.userId))
      .catch(() => setUserId(null));
  }, []);

  return (
    <PersonalityModeProvider userId={userId}>
      {children}
    </PersonalityModeProvider>
  );
}
