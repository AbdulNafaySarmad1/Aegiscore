"use client";

import { createContext, useContext, useEffect, useMemo, useState } from "react";
import { apiGet } from "@/lib/api-client";

export type TenantPreferences = {
  locale: string;
  currency: string;
  paymentGateway: string;
  accreditorName: string;
  identityDocumentName: string;
  /** Legal name of the institution (config key identity.university_name). Empty until loaded or if unset. */
  universityName: string;
  /** Short form used where space is tight (identity.university_short_name). */
  universityShortName: string;
  /** Institution crest/logo, from GET /api/branding/client-logo. Empty when none is configured. */
  logoUrl: string;
  /** Official seal image (Branding:SealUrl). Empty when none is configured. */
  sealUrl: string;
};

const defaults: TenantPreferences = {
  locale: "en-US",
  currency: "USD",
  paymentGateway: "stripe",
  accreditorName: "Accreditor",
  identityDocumentName: "Identity document",
  universityName: "",
  universityShortName: "",
  logoUrl: "",
  sealUrl: "",
};

const TenantPreferencesContext = createContext<TenantPreferences>(defaults);

export function TenantPreferencesProvider({ children }: { children: React.ReactNode }) {
  const [preferences, setPreferences] = useState<TenantPreferences>(defaults);

  useEffect(() => {
    const controller = new AbortController();
    apiGet<Partial<TenantPreferences>>("/api/config/portal-preferences", controller.signal)
      .then(value => setPreferences(current => ({ ...current, ...stripEmpty(value) })))
      .catch(() => { /* Neutral defaults keep setup mode usable. */ });
    apiGet<{ logoUrl?: string }>("/api/branding/client-logo", controller.signal)
      .then(value => { if (value?.logoUrl) setPreferences(current => ({ ...current, logoUrl: value.logoUrl! })); })
      .catch(() => { /* No crest configured. */ });
    return () => controller.abort();
  }, []);

  const value = useMemo(() => preferences, [preferences]);
  return <TenantPreferencesContext.Provider value={value}>{children}</TenantPreferencesContext.Provider>;
}

function stripEmpty<T extends object>(value: T): Partial<T> {
  return Object.fromEntries(Object.entries(value).filter(([, v]) => v !== null && v !== undefined)) as Partial<T>;
}

export function useTenantPreferences() { return useContext(TenantPreferencesContext); }

/**
 * The institution as it should be named and marked everywhere: rail, gate pages, seals, PDFs.
 * Falls back to neutral wording, never to a placeholder institution.
 */
export function useInstitution() {
  const p = useTenantPreferences();
  return {
    name: p.universityName.trim(),
    shortName: p.universityShortName.trim(),
    /** Name for display, or a neutral descriptor while unset. */
    displayName: p.universityName.trim() || "University register",
    logoUrl: p.logoUrl.trim(),
    sealUrl: p.sealUrl.trim(),
  };
}

export function useTenantFormatter() {
  const preferences = useTenantPreferences();
  return {
    ...preferences,
    money: (value: number, currency = preferences.currency) =>
      new Intl.NumberFormat(preferences.locale, { style: "currency", currency, maximumFractionDigits: 0 }).format(value),
    date: (value: string | number | Date) =>
      new Intl.DateTimeFormat(preferences.locale, { dateStyle: "medium" }).format(new Date(value)),
    dateTime: (value: string | number | Date) =>
      new Intl.DateTimeFormat(preferences.locale, { dateStyle: "medium", timeStyle: "short" }).format(new Date(value)),
    longDate: (value: string | number | Date) =>
      new Intl.DateTimeFormat(preferences.locale, { dateStyle: "long" }).format(new Date(value)),
    // Clock time alone, for screens about what is happening now — an invigilation list, a deadline.
    time: (value: string | number | Date) =>
      new Intl.DateTimeFormat(preferences.locale, { timeStyle: "short" }).format(new Date(value)),
  };
}
