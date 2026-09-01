"use client";

import Image from "next/image";
import { useState } from "react";

import { championIconPath } from "./data-dragon";

export function ChampionIcon({
  championId,
  championName,
}: {
  championId: number;
  championName: string;
}) {
  const [failed, setFailed] = useState(false);
  const source = championIconPath(championId);

  if (!source || failed) {
    return (
      <span className="champion-icon-fallback" aria-hidden="true">
        {championName.slice(0, 2).toUpperCase()}
      </span>
    );
  }

  return (
    <Image
      alt={`Ícono de ${championName}`}
      className="champion-icon"
      height={40}
      onError={() => setFailed(true)}
      src={source}
      unoptimized
      width={40}
    />
  );
}
