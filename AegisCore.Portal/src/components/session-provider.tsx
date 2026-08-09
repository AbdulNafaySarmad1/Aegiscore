"use client";

// ─── Session Provider ───
// Bootstraps identity from GET /api/session (HttpOnly cookie auth).
// The api-client performs a transparent refresh before this ever sees a
// terminal 401, so a 401 here means "signed out".

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { AUTH_EXPIRED_EVENT } from "@/lib/api-client";
import {
  clearDemoSession,
  fetchSession,
  isDemoMode,
  logout,
  readDemoSession,
  type SessionUser
} from "@/lib/session";

export type SessionStatus = "loading" | "authenticated" | "unauthenticated";

type SessionContextValue = {
  status: SessionStatus;
  session: SessionUser | null;
  /** True when the current identity comes from the isolated demo store. */
  isDemo: boolean;
  /** Re-reads the session from the backend (e.g. right after login). */
  reload: () => Promise<SessionUser | null>;
  signOut: () => Promise<void>;
};

const SessionContext = createContext<SessionContextValue>({
  status: "loading",
  session: null,
  isDemo: false,
  reload: async () => null,
  signOut: async () => {}
});

export function useSession(): SessionContextValue {
  return useContext(SessionContext);
}

export function SessionProvider({ children }: { children: React.ReactNode }) {
  const [status, setStatus] = useState<SessionStatus>("loading");
  const [session, setSession] = useState<SessionUser | null>(null);
  const [isDemo, setIsDemo] = useState(false);

  const bootstrap = useCallback(async (): Promise<SessionUser | null> => {
    try {
      const user = await fetchSession();
      clearDemoSession();
      setSession(user);
      setIsDemo(false);
      setStatus("authenticated");
      return user;
    } catch {
      // No valid backend session. Demo mode may supply a preview identity.
      const demo = readDemoSession();
      if (demo) {
        setSession(demo);
        setIsDemo(true);
        setStatus("authenticated");
        return demo;
      }
      setSession(null);
      setIsDemo(false);
      setStatus("unauthenticated");
      return null;
    }
  }, []);

  const signOut = useCallback(async () => {
    if (isDemoMode) clearDemoSession();
    await logout();
    setSession(null);
    setIsDemo(false);
    setStatus("unauthenticated");
    window.location.assign("/login");
  }, []);

  // Bootstrap runs in a microtask: state transitions happen after the first
  // await inside bootstrap(), never synchronously during the effect commit.
  useEffect(() => { queueMicrotask(() => void bootstrap()); }, [bootstrap]);

  useEffect(() => {
    const onExpired = () => {
      clearDemoSession();
      setSession(null);
      setIsDemo(false);
      setStatus("unauthenticated");
    };
    window.addEventListener(AUTH_EXPIRED_EVENT, onExpired);
    return () => window.removeEventListener(AUTH_EXPIRED_EVENT, onExpired);
  }, []);

  const value = useMemo<SessionContextValue>(
    () => ({ status, session, isDemo, reload: bootstrap, signOut }),
    [status, session, isDemo, bootstrap, signOut]
  );

  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>;
}
