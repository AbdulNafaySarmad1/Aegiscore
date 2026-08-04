"use client";
import { useTranslations } from "next-intl";
import { usePersonalityMode } from "@/lib/personality-context";

export function usePersonalityTranslations(namespace: string) {
  const t = useTranslations(namespace);
  const { resolvedKey } = usePersonalityMode();

  return function tp(key: string, values?: Record<string, any>): string {
    const targetKey = resolvedKey(namespace + "." + key);
    try {
      const result = t(targetKey as any, values as any);
      if (result && result !== targetKey) return result;
    } catch {}
    return t(key as any, values as any);
  };
}
