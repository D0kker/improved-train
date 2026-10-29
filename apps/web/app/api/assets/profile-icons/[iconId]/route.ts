import { dataDragonVersion, profileIconUrl } from "@/src/data-dragon";
export async function GET(
  _request: Request,
  context: { params: Promise<{ iconId: string }> },
) {
  const { iconId } = await context.params;
  if (!/^\d{1,5}$/.test(iconId)) return new Response(null, { status: 404 });
  const url = profileIconUrl(
    dataDragonVersion(process.env.DATA_DRAGON_VERSION),
    Number(iconId),
  );
  if (!url) return new Response(null, { status: 404 });
  try {
    const response = await fetch(url, {
      next: { revalidate: 86400 },
      signal: AbortSignal.timeout(5000),
    });
    if (!response.ok || response.headers.get("content-type") !== "image/png")
      return new Response(null, { status: 404 });
    const reader = response.body?.getReader();
    if (!reader) return new Response(null, { status: 404 });
    const chunks: Uint8Array[] = [];
    let size = 0;
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
      size += value.length;
      if (size > 1048576) {
        await reader.cancel();
        return new Response(null, { status: 404 });
      }
      chunks.push(value);
    }
    const body = new Uint8Array(size);
    let offset = 0;
    for (const chunk of chunks) {
      body.set(chunk, offset);
      offset += chunk.length;
    }
    return new Response(body, {
      headers: {
        "Content-Type": "image/png",
        "Cache-Control": "public, max-age=86400",
        "X-Content-Type-Options": "nosniff",
      },
    });
  } catch {
    return new Response(null, { status: 404 });
  }
}
