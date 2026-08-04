import "server-only";
import { cookies, headers } from "next/headers";
import { portalLocales, type PortalLocale } from "./messages";

export async function getPortalLocale(): Promise<PortalLocale> {
  const saved = (await cookies()).get("AEGISCORE_LOCALE")?.value;
  if (portalLocales.includes(saved as PortalLocale)) return saved as PortalLocale;
  const accepted = (await headers()).get("accept-language")?.toLowerCase() ?? "";
  return portalLocales.find(locale => accepted.includes(locale.toLowerCase()) || accepted.includes(locale.split("-")[0].toLowerCase())) ?? "en-PK";
}
