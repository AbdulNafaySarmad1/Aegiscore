import type { NextConfig } from "next";
import createNextIntlPlugin from "next-intl/plugin";

const withNextIntl = createNextIntlPlugin();

const nextConfig: NextConfig = {
  output: "standalone",
  async redirects() {
    return [
      { source: "/dashboard", destination: "/student/dashboard", permanent: false },
      { source: "/students", destination: "/student/dashboard", permanent: false },
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
      { source: "/api/:path*", destination: `${api}/api/:path*` },
      { source: "/health/:path*", destination: `${api}/health/:path*` },
      { source: "/hubs/:path*", destination: `${api}/hubs/:path*` }
    ];
  }
};

export default withNextIntl(nextConfig);
