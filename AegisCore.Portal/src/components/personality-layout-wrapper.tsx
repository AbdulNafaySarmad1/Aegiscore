"use client";
import { useState, useEffect } from "react";
import { apiGet } from "@/lib/api-client";
import { PersonalityModeProvider } from "@/lib/personality-context";
import { loadSession } from "@/lib/session";

type Session = { userId: string };

export function PersonalityLayoutWrapper({ children }: { children: React.ReactNode }) {
  const [userId, setUserId] = useState<string | null>(null);

  useEffect(() => {
    loadSession().then(session => setUserId(session.userId)).catch(() => setUserId(null));
  }, []);

  return (
    <PersonalityModeProvider userId={userId}>
      {children}
    </PersonalityModeProvider>
  );
}
