import {
  championCatalogUrl,
  championImageUrl,
  dataDragonVersion,
} from "@/src/data-dragon";

interface ChampionCatalogEntry {
  id?: string;
  key?: string;
}

interface ChampionCatalog {
  data?: Record<string, ChampionCatalogEntry>;
}

export const dynamic = "force-dynamic";

export async function GET(
  _request: Request,
  context: { params: Promise<{ championId: string }> },
) {
  const { championId } = await context.params;
  if (!/^[1-9]\d{0,4}$/.test(championId)) {
    return new Response(null, { status: 404 });
  }

  const version = dataDragonVersion(process.env.DATA_DRAGON_VERSION);
  try {
    const catalogResponse = await fetch(championCatalogUrl(version), {
      next: { revalidate: 86_400 },
    });
    if (!catalogResponse.ok) return new Response(null, { status: 404 });

    const catalog = (await catalogResponse.json()) as ChampionCatalog;
    const champion = Object.values(catalog.data ?? {}).find(
      (candidate) => candidate.key === championId,
    );
    const imageUrl = champion?.id
      ? championImageUrl(version, champion.id)
      : null;
    if (!imageUrl) return new Response(null, { status: 404 });

    const imageResponse = await fetch(imageUrl, {
      next: { revalidate: 604_800 },
    });
    const contentType = imageResponse.headers.get("content-type") ?? "";
    if (!imageResponse.ok || contentType !== "image/png") {
      return new Response(null, { status: 404 });
    }

    return new Response(await imageResponse.arrayBuffer(), {
      headers: {
        "Cache-Control": "public, max-age=86400, stale-if-error=604800",
        "Content-Type": "image/png",
        "X-Content-Type-Options": "nosniff",
      },
    });
  } catch {
    return new Response(null, { status: 404 });
  }
}
