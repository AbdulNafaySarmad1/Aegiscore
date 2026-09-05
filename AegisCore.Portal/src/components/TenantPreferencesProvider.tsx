"use client";

import { createContext, useContext, useEffect, useMemo, useState } from "react";
import { apiGet } from "@/lib/api-client";

export type TenantPreferences = {
  locale: string;
  currency: string;
  paymentGateway: string;
  accreditorName: string;
  identityDocumentName: string;
};

const defaults: TenantPreferences = {
  locale: "en-US",
  currency: "USD",
  paymentGateway: "stripe",
  accreditorName: "Accreditor",
  identityDocumentName: "Identity document",
};

const TenantPreferencesContext = createContext<TenantPreferences>(defaults);

export function TenantPreferencesProvider({ children }: { children: React.ReactNode }) {
  const [preferences, setPreferences] = useState<TenantPreferences>(defaults);

  useEffect(() => {
    const controller = new AbortController();
    apiGet<Partial<TenantPreferences>>("/api/config/portal-preferences", controller.signal)
      .then(value => setPreferences(current => ({ ...current, ...value })))
      .catch(() => { /* Neutral defaults keep setup mode usable. */ });
    return () => controller.abort();
  }, []);

  const value = useMemo(() => preferences, [preferences]);
  return <TenantPreferencesContext.Provider value={value}>{children}</TenantPreferencesContext.Provider>;
}

export function useTenantPreferences() { return useContext(TenantPreferencesContext); }

export function useTenantFormatter() {
  const preferences = useTenantPreferences();
  return {
    ...preferences,
    money: (value: number, currency = preferences.currency) =>
      new Intl.NumberFormat(preferences.locale, { style: "currency", currency, maximumFractionDigits: 0 }).format(value),
    date: (value: string | number | Date) =>
      new Intl.DateTimeFormat(preferences.locale, { dateStyle: "medium" }).format(new Date(value)),
  };
}
