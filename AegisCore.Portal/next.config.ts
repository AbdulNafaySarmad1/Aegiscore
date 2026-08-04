import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "standalone",
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

export default nextConfig;
