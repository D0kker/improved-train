"use client";
/* eslint-disable @next/next/no-img-element -- Fixed-size same-origin PNG already cached and bounded by our asset proxy. */
import { useId, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatDashboardDate } from "./dashboard-presentation";
import { profileIconPath } from "./data-dragon";
export function ProfileIcon({
  id,
  observedAt,
}: {
  id?: number | null;
  observedAt?: string | null;
}) {
  const t = useTranslations("common");
  const locale = useLocale();
  const descriptionId = useId();
  const [failedSource, setFailedSource] = useState<string | null>(null),
    source = profileIconPath(id);
  return (
    <div className="profile-icon">
      {source && failedSource !== source ? (
        <img
          src={source}
          alt={t("observedProfileIcon")}
          aria-describedby={descriptionId}
          title={
            observedAt
              ? t("profileIconAtMatch", {
                  date: formatDashboardDate(observedAt, locale),
                })
              : t("lastAvailableProfileIcon")
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
          aria-label={t("profileIconUnavailable")}
        >
          E
        </span>
      )}
      {source && failedSource !== source && (
        <span className="sr-only" id={descriptionId}>
          {t("profileIconDescription", {
            date: observedAt
              ? `, ${formatDashboardDate(observedAt, locale)}`
              : "",
          })}
        </span>
      )}
    </div>
  );
}
