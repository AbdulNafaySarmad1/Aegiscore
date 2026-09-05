"use client";
import { useMessages, useTranslations } from "next-intl";
import { usePersonalityMode } from "@/lib/personality-context";

function interpolate(text: string, values?: Record<string, unknown>): string {
  if (!values) return text;
  return text.replace(/\{(\w+)\}/g, (_, key) => String(values[key] ?? `{${key}}`));
}

export function usePersonalityTranslations(namespace: string) {
  const t = useTranslations(namespace);
  const messages = useMessages();
  const { resolvedKey } = usePersonalityMode();

  return function tp(key: string, values?: Record<string, unknown>): string {
    const lookupKey = resolvedKey(key);
    if (lookupKey !== key) {
      const nsMsgs = (messages as Record<string, Record<string, unknown>>)?.[namespace];
      const val = nsMsgs?.[lookupKey];
      if (typeof val === "string") return interpolate(val, values);
    }
    return t(key as never, values as never);
  };
}
