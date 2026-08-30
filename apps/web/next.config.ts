import type { NextConfig } from "next";

const apiBaseUrl = (process.env.API_BASE_URL || "http://api:8080").replace(
  /\/$/,
  "",
);

const nextConfig: NextConfig = {
  output: "standalone",
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
