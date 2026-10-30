"use client";
/* eslint-disable @next/next/no-img-element -- Fixed-size same-origin PNG already cached and bounded by our asset proxy. */
import { useId, useState } from "react";
import { formatDashboardDate } from "./dashboard-presentation";
import { profileIconPath } from "./data-dragon";
export function ProfileIcon({
  id,
  observedAt,
}: {
  id?: number | null;
  observedAt?: string | null;
}) {
  const descriptionId = useId();
  const [failedSource, setFailedSource] = useState<string | null>(null),
    source = profileIconPath(id);
  return (
    <div className="profile-icon">
      {source && failedSource !== source ? (
        <img
          src={source}
          alt="Ícono de invocador observado en su última partida disponible"
          aria-describedby={descriptionId}
          title={
            observedAt
              ? `Ícono observado en partida: ${formatDashboardDate(observedAt)}`
              : "Ícono de la última partida disponible"
          }
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
      {source && failedSource !== source && (
        <span className="sr-only" id={descriptionId}>
          Ícono observado en la última partida disponible
          {observedAt ? `, ${formatDashboardDate(observedAt)}` : ""}; puede
          diferir del avatar actual.
        </span>
      )}
    </div>
  );
}
