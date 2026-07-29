import type { Metadata, Viewport } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: {
    default: "AegisCore",
    template: "%s · AegisCore"
  },
  description: "The university operating system by Nocturne Systems",
  manifest: "/manifest.webmanifest"
};

export const viewport: Viewport = {
  colorScheme: "light dark",
  themeColor: "#17233f",
  width: "device-width",
  initialScale: 1
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
