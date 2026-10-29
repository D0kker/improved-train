"use client";
/* eslint-disable @next/next/no-img-element -- Fixed-size same-origin PNG already cached and bounded by our asset proxy. */
import { useState } from "react";
import { profileIconPath } from "./data-dragon";
export function ProfileIcon({
  id,
  observedAt,
}: {
  id?: number | null;
  observedAt?: string | null;
}) {
  const [failedSource, setFailedSource] = useState<string | null>(null),
    source = profileIconPath(id);
  return (
    <div className="profile-icon">
      {source && failedSource !== source ? (
        <img
          src={source}
          alt="Ícono de invocador observado en su última partida disponible"
          title={observedAt ? `Observado en partida: ${observedAt}` : undefined}
          className="profile-icon__image"
          width={88}
          height={88}
          onError={() => setFailedSource(source)}
        />
      ) : (
        <span
          className="competitive-avatar"
          role="img"
          aria-label="Ícono de perfil no disponible"
        >
          E
        </span>
      )}
      {source && failedSource !== source && <small>Ícono histórico</small>}
    </div>
  );
}
