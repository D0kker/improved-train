"use client";

import Image from "next/image";
import { useState } from "react";

import { championIconPath } from "./data-dragon";

export function ChampionIcon({
  championId,
  championName,
  size = 40,
}: {
  championId: number;
  championName: string;
  size?: 40 | 64 | 72 | 80;
}) {
  const [failed, setFailed] = useState(false);
  const source = championIconPath(championId);

  if (!source || failed) {
    return (
      <span
        className="champion-icon-fallback"
        style={size === 40 ? undefined : { width: size, height: size }}
        aria-hidden="true"
      >
        {championName.slice(0, 2).toUpperCase()}
      </span>
    );
  }

  return (
    <Image
      alt={`Ícono de ${championName}`}
      className="champion-icon"
      height={size}
      onError={() => setFailed(true)}
      src={source}
      unoptimized
      width={size}
    />
  );
}
