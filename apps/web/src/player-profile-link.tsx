import Link from "next/link";

import { playerProfilePath } from "./api";
import { useTranslations } from "next-intl";

export function PlayerProfileLink({
  compact = false,
  gameName,
  platformRegion,
  tagLine,
}: {
  compact?: boolean;
  gameName?: string;
  platformRegion?: string;
  tagLine?: string;
}) {
  const t = useTranslations("common");
  if (!gameName || !tagLine) {
    return compact ? (
      <span>{gameName || t("unidentifiedPlayer")}</span>
    ) : (
      <span className="player-identity-incomplete">
        <strong>{gameName || t("unidentifiedPlayer")}</strong>
        <span>{tagLine ? `#${tagLine}` : t("riotIdUnavailable")}</span>
      </span>
    );
  }

  const riotId = `${gameName}#${tagLine}`;
  return (
    <Link
      className={`player-link${compact ? " compact" : ""}`}
      href={playerProfilePath(gameName, tagLine, platformRegion)}
      aria-label={t("viewSummary", { riotId })}
    >
      {compact ? (
        `${gameName} #${tagLine}`
      ) : (
        <>
          <strong>{gameName}</strong>
          <span>#{tagLine}</span>
        </>
      )}
    </Link>
  );
}
