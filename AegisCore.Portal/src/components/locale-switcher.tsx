"use client";

import { useLocale } from "next-intl";
import { useRouter } from "next/navigation";
import { portalLocales, type PortalLocale } from "@/i18n/messages";

export function LocaleSwitcher({ label }: { label: string }) {
  const locale = useLocale() as PortalLocale;
  const router = useRouter();
  return <label className="locale-switcher"><span className="sr-only">{label}</span><select aria-label={label} value={locale} onChange={event => {
    const next = event.target.value as PortalLocale;
    document.cookie = `AEGISCORE_LOCALE=${next}; Path=/; Max-Age=31536000; SameSite=Lax${location.protocol === "https:" ? "; Secure" : ""}`;
    document.documentElement.lang = next; document.documentElement.dir = next === "ar-AE" ? "rtl" : "ltr"; router.refresh();
  }}>{portalLocales.map(item => <option key={item} value={item}>{item}</option>)}</select></label>;
}
