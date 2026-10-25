import { HomeContent } from "./home-content";
import { PlayerSearchForm } from "./player-search-form";
import { homeCopy } from "./home-copy";
import { platformRegionLabel } from "@/src/platform-region";
import { SiteHeader } from "@/src/site-header";

export const dynamic = "force-dynamic";

export default function Home() {
  const platformRegion = process.env.RIOT_PLATFORM_REGION ?? "la1";
  return (
    <>
      <SiteHeader
        actionHref="#player-search-form"
        actionLabel="Ir a la búsqueda"
      />
      <HomeContent
        copy={homeCopy}
        platformRegionLabel={platformRegionLabel(platformRegion)}
        searchForm={
          <PlayerSearchForm
            copy={homeCopy.form}
            platformRegion={platformRegion}
          />
        }
      />
    </>
  );
}
