"use client";
import { createContext, useContext, useState, useEffect, useCallback } from "react";
import { apiGet } from "@/lib/api-client";

type PersonalityContextValue = {
  isActive: boolean;
  isLoading: boolean;
  resolvedKey: (baseKey: string) => string;
  togglePersonality: () => Promise<void>;
};

const PersonalityContext = createContext<PersonalityContextValue>({
  isActive: false,
  isLoading: true,
  resolvedKey: (k) => k,
  togglePersonality: async () => {},
});

export function PersonalityModeProvider({ children, userId }: { children: React.ReactNode; userId: string | null }) {
  const [isActive, setIsActive] = useState(false);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- unauthenticated state is resolved on mount.
    if (!userId) { setIsLoading(false); return; }
    const ctrl = new AbortController();
    apiGet<{ isActive: boolean }>(`/api/personality/users/${userId}/active`, ctrl.signal)
      .then(r => setIsActive(r.isActive))
      .catch(() => setIsActive(false))
      .finally(() => setIsLoading(false));
    return () => ctrl.abort();
  }, [userId]);

  const resolvedKey = useCallback((baseKey: string) => {
    if (!isActive) return baseKey;
    return baseKey + ".personality";
  }, [isActive]);

  const togglePersonality = useCallback(async () => {
    if (!userId) return;
    const newVal = !isActive;
    await fetch(`/api/personality/users/${userId}/preference?isActive=${newVal}`, { method: "PUT" });
    setIsActive(newVal);
  }, [userId, isActive]);

  return (
    <PersonalityContext.Provider value={{ isActive, isLoading, resolvedKey, togglePersonality }}>
      {children}
    </PersonalityContext.Provider>
  );
}

export function usePersonalityMode() {
  return useContext(PersonalityContext);
}
