"use client";
import { useMessages, useTranslations } from "next-intl";
import { usePersonalityMode } from "@/lib/personality-context";

type MessageValues = Record<string, string | number | Date>;

function interpolate(text: string, values?: MessageValues): string {
  if (!values) return text;
  return text.replace(/\{(\w+)\}/g, (_, key) => String(values[key] ?? `{${key}}`));
}

export function usePersonalityTranslations(namespace: string) {
  const t = useTranslations(namespace);
  const messages = useMessages();
  const { resolvedKey } = usePersonalityMode();

  return function tp(key: string, values?: MessageValues): string {
    const lookupKey = resolvedKey(key);
    if (lookupKey !== key) {
      const nsMsgs = (messages as Record<string, unknown>)?.[namespace] as Record<string, unknown> | undefined;
      const val = nsMsgs?.[lookupKey];
      if (typeof val === "string") return interpolate(val, values);
    }
    // Dynamic keys cannot satisfy next-intl's literal-key typing.
    return t(key as never, values as never);
  };
}
