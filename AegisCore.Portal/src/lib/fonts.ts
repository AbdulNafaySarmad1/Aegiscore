import localFont from "next/font/local";

// Self-hosted so on-premise builds never reach the network. Latin + latin-ext subsets only;
// the same families ship as TTF under public/fonts for the PDF renderer.
export const caslon = localFont({
  variable: "--font-caslon",
  display: "swap",
  adjustFontFallback: "Times New Roman",
  src: [
    { path: "../fonts/LibreCaslonText-400-normal-latin.woff2", weight: "400", style: "normal" },
    { path: "../fonts/LibreCaslonText-400-italic-latin.woff2", weight: "400", style: "italic" },
    { path: "../fonts/LibreCaslonText-700-normal-latin.woff2", weight: "700", style: "normal" },
  ],
});

export const plexSans = localFont({
  variable: "--font-plex",
  display: "swap",
  adjustFontFallback: "Arial",
  src: [
    { path: "../fonts/IBMPlexSans-400-700-normal-latin.woff2", weight: "400 700", style: "normal" },
    { path: "../fonts/IBMPlexSans-400-700-italic-latin.woff2", weight: "400 700", style: "italic" },
  ],
});

export const plexMono = localFont({
  variable: "--font-plex-mono",
  display: "swap",
  adjustFontFallback: false,
  src: [
    { path: "../fonts/IBMPlexMono-400-normal-latin.woff2", weight: "400", style: "normal" },
    { path: "../fonts/IBMPlexMono-500-normal-latin.woff2", weight: "500", style: "normal" },
    { path: "../fonts/IBMPlexMono-600-normal-latin.woff2", weight: "600", style: "normal" },
  ],
});

export const fontClassName = `${caslon.variable} ${plexSans.variable} ${plexMono.variable}`;
