import type { Metadata, Viewport } from "next";
import { NextIntlClientProvider } from "next-intl";
import { getPortalLocale } from "@/i18n/server";
import { portalMessages } from "@/i18n/messages";
import { PersonalityLayoutWrapper } from "@/components/personality-layout-wrapper";
import { ErrorBoundary } from "@/components/ErrorBoundary";
import { TenantPreferencesProvider } from "@/components/TenantPreferencesProvider";
import { fontClassName } from "@/lib/fonts";
import "./globals.css";

export const metadata: Metadata = {
  title: {
    default: "AegisCore",
    template: "%s — AegisCore"
  },
  description: "The university operating system by AegisCore",
  manifest: "/manifest.webmanifest"
};

export const viewport: Viewport = {
  colorScheme: "light dark",
  themeColor: "#1B1A18",
  width: "device-width",
  initialScale: 1
};

// Applies the viewer's saved scheme, text size and contrast before first paint so the
// shell never flashes the wrong theme or reflows when the client hydrates.
const preferenceScript = `(function(){try{var d=document.documentElement,s=localStorage;
var t=s.getItem("aegis-theme");if(t==="dark"||t==="light")d.dataset.theme=t;
var f=s.getItem("aegis-font-scale");if(f&&f!=="medium")d.dataset.fontScale=f;
if(s.getItem("aegis-high-contrast")==="true")d.dataset.highContrast="true";}catch(e){}})();`;

export default async function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  const locale = await getPortalLocale();
  return (
    <html lang={locale} dir={locale === "ar-AE" ? "rtl" : "ltr"} className={fontClassName} suppressHydrationWarning>
      <head>
        <script dangerouslySetInnerHTML={{ __html: preferenceScript }} />
      </head>
      <body>
        <a className="skip-link" href="#main-content">Skip to main content</a>
        <NextIntlClientProvider locale={locale} messages={portalMessages[locale]}>
          <TenantPreferencesProvider>
            <PersonalityLayoutWrapper>
              <ErrorBoundary>{children}</ErrorBoundary>
            </PersonalityLayoutWrapper>
          </TenantPreferencesProvider>
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
