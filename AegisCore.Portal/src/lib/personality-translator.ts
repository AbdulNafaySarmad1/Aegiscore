"use client";
import { useMessages, useTranslations } from "next-intl";
import { usePersonalityMode } from "@/lib/personality-context";

function interpolate(text: string, values?: Record<string, any>): string {
  if (!values) return text;
  return text.replace(/\{(\w+)\}/g, (_, key) => String(values[key] ?? `{${key}}`));
}

export function usePersonalityTranslations(namespace: string) {
  const t = useTranslations(namespace);
  const messages = useMessages();
  const { resolvedKey } = usePersonalityMode();

  return function tp(key: string, values?: Record<string, any>): string {
    const lookupKey = resolvedKey(key);
    if (lookupKey !== key) {
      const nsMsgs = (messages as Record<string, any>)?.[namespace] as Record<string, any> | undefined;
      const val = nsMsgs?.[lookupKey];
      if (typeof val === "string") return interpolate(val, values);
    }
    return t(key as any, values as any);
  };
}
