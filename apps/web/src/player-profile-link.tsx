import Link from "next/link";

import { playerProfilePath } from "./api";

export function PlayerProfileLink({
  compact = false,
  gameName,
  tagLine,
}: {
  compact?: boolean;
  gameName?: string;
  tagLine?: string;
}) {
  if (!gameName || !tagLine) {
    return compact ? (
      <span>{gameName || "Jugador no identificado"}</span>
    ) : (
      <span className="player-identity-incomplete">
        <strong>{gameName || "Jugador no identificado"}</strong>
        <span>{tagLine ? `#${tagLine}` : "Riot ID no disponible"}</span>
      </span>
    );
  }

  const riotId = `${gameName}#${tagLine}`;
  return (
    <Link
      className={`player-link${compact ? " compact" : ""}`}
      href={playerProfilePath(gameName, tagLine)}
      aria-label={`Ver resumen de ${riotId}`}
    >
      {compact ? (
        riotId
      ) : (
        <>
          <strong>{gameName}</strong>
          <span>#{tagLine}</span>
        </>
      )}
    </Link>
  );
}
