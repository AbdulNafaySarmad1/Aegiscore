import type { Metadata, Viewport } from "next";
import { NextIntlClientProvider } from "next-intl";
import { getPortalLocale } from "@/i18n/server";
import { portalMessages } from "@/i18n/messages";
import { personalityMessages } from "@/i18n/personality-messages";
import { PersonalityLayoutWrapper } from "@/components/personality-layout-wrapper";
import "./globals.css";

export const metadata: Metadata = {
  title: {
    default: "AegisCore",
    template: "%s · AegisCore"
  },
  description: "The university operating system by AegisCore",
  manifest: "/manifest.webmanifest"
};

export const viewport: Viewport = {
  colorScheme: "light dark",
  themeColor: "#17233f",
  width: "device-width",
  initialScale: 1
};

export default async function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  const locale = await getPortalLocale();
  const messages = { ...portalMessages[locale], ...personalityMessages[locale] };
  return (
    <html lang={locale} dir={locale === "ar-AE" ? "rtl" : "ltr"}>
      <body><NextIntlClientProvider locale={locale} messages={messages}><PersonalityLayoutWrapper>{children}</PersonalityLayoutWrapper></NextIntlClientProvider></body>
    </html>
  );
}
