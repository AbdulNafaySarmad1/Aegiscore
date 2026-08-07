import { getRequestConfig } from "next-intl/server";
import { getPortalLocale } from "@/i18n/server";
import { portalMessages } from "@/i18n/messages";

export default getRequestConfig(async () => {
  const locale = await getPortalLocale();
  return {
    locale,
    messages: portalMessages[locale]
  };
});
