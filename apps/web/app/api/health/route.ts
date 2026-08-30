import { healthPayload } from "@/src/health";

export const dynamic = "force-dynamic";

export function GET() {
  return Response.json(healthPayload(), {
    headers: { "Cache-Control": "no-store" },
  });
}
