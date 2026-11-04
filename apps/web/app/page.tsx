import { HomeContent } from "./home-content";
import { PlayerSearchForm } from "./player-search-form";
import { platformRegionLabel } from "@/src/platform-region";
import { SiteHeader } from "@/src/site-header";
import { getLocale, getTranslations } from "next-intl/server";

export const dynamic = "force-dynamic";

export default async function Home() {
  const platformRegion = process.env.RIOT_PLATFORM_REGION ?? "la1";
  const [locale, t] = await Promise.all([getLocale(), getTranslations("home")]);
  const copy = {
    eyebrow: t("eyebrow"),
    title: t("title"),
    description: t("description"),
    searchHeading: t("searchHeading"),
    searchDescription: t("searchDescription"),
    scope: t("scope"),
    regionAria: t("regionAria"),
    regionLabel: t("regionLabel"),
    howEyebrow: t("howEyebrow"),
    howTitle: t("howTitle"),
    steps: [0, 1, 2].map((index) => ({
      number: `0${index + 1}`,
      title: t(`steps.${index}`),
      description: t(`stepDescriptions.${index}`),
    })),
    form: {
      gameNameLabel: t("playerName"),
      tagLineLabel: t("tag"),
      riotIdHelp: t("riotIdHelp"),
      gameNameError: t("playerNameError"),
      tagLineError: t("tagError"),
      submit: t("submit"),
      navigating: t("navigating"),
      navigatingAnnouncement: t("navigatingAnnouncement"),
    },
  };
  return (
    <>
      <SiteHeader
        actionHref="#player-search-form"
        actionLabel={t("searchLink")}
      />
      <HomeContent
        copy={copy}
        platformRegionLabel={platformRegionLabel(platformRegion, locale)}
        searchForm={
          <PlayerSearchForm copy={copy.form} platformRegion={platformRegion} />
        }
      />
    </>
  );
}
