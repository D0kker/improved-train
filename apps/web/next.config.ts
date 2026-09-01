import type { NextConfig } from "next";

import { browserSecurityHeaders } from "./src/security-headers";

const apiBaseUrl = (process.env.API_BASE_URL || "http://api:8080").replace(
  /\/$/,
  "",
);

const nextConfig: NextConfig = {
  output: "standalone",
  poweredByHeader: false,
  async headers() {
    return [
      {
        source: "/:path*",
        headers: [...browserSecurityHeaders],
      },
    ];
  },
  async rewrites() {
    return [
      {
        source: "/api/v1/:path*",
        destination: `${apiBaseUrl}/api/v1/:path*`,
      },
      {
        source: "/openapi/:path*",
        destination: `${apiBaseUrl}/openapi/:path*`,
      },
    ];
  },
};

export default nextConfig;
