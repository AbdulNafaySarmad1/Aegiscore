"use client";
import { PersonalityModeProvider } from "@/lib/personality-context";
import { useSession } from "@/components/session-provider";

export function PersonalityLayoutWrapper({ children }: { children: React.ReactNode }) {
  const { session } = useSession();
  return (
    <PersonalityModeProvider userId={session?.userId ?? null}>
      {children}
    </PersonalityModeProvider>
  );
}
