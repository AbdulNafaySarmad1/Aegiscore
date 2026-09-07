import type { NextConfig } from "next";
import createNextIntlPlugin from "next-intl/plugin";

const withNextIntl = createNextIntlPlugin();

const nextConfig: NextConfig = {
  output: "standalone",
  devIndicators: false,
  async redirects() {
    return [
      // `/grades` and `/timetable` have no page root of their own. These aliases map the
      // conventional names to the canonical routes and are relied on by the capture suite
      // (tests/capture/*.spec.ts); keep them in sync with its `aliases` map.
      { source: "/grades", destination: "/examinations", permanent: false },
      { source: "/timetable", destination: "/classes", permanent: false },
      { source: "/admin", destination: "/admin/campus", permanent: false },
      { source: "/profile", destination: "/student/profile", permanent: false }
    ];
  },
  poweredByHeader: false,
  async rewrites() {
    const api = process.env.AEGISCORE_API_INTERNAL_URL ?? "http://api:8080";
    return [
      { source: "/health/:path*", destination: `${api}/health/:path*` },
      { source: "/hubs/:path*", destination: `${api}/hubs/:path*` }
    ];
  }
};

export default withNextIntl(nextConfig);
