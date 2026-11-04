import type { AnalysisCoverage } from "./api";
import { useTranslations } from "next-intl";

export function CoverageSummary({
  coverage,
  title,
}: {
  coverage: AnalysisCoverage;
  title: string;
}) {
  const t = useTranslations("coverage");
  const windows = coverage.players ?? [];
  const requestedWindows = windows.reduce(
    (total, player) => total + player.requested,
    0,
  );
  const evaluableProfiles = windows.filter(
    (player) => player.eligibleMatches > 0,
  ).length;
  const incompleteWindows = windows.filter((player) => player.limited).length;
  const headingId = `coverage-${title.toLowerCase().replace(/[^a-z0-9]+/g, "-")}`;

  return (
    <section
      className={`coverage-panel coverage-${coverage.status}`}
      aria-labelledby={headingId}
      role={coverage.status === "error" ? "alert" : "status"}
    >
      <div>
        <p className="eyebrow">{title}</p>
        <h2 id={headingId}>{t(coverage.status)}</h2>
        <p>{t(`${coverage.status}Description` as "completeDescription")}</p>
      </div>
      <dl className="coverage-stats">
        {requestedWindows > 0 ? (
          <div>
            <dt>{t("requested")}</dt>
            <dd>{requestedWindows}</dd>
          </div>
        ) : null}
        <div>
          <dt>{t("ids")}</dt>
          <dd>{coverage.matchIdsObtained}</dd>
        </div>
        <div>
          <dt>{t("recovered")}</dt>
          <dd>{coverage.matchesRecovered}</dd>
        </div>
        <div>
          <dt>{t("uniqueIds")}</dt>
          <dd>{coverage.uniqueMatchIds}</dd>
        </div>
        {windows.length > 0 ? (
          <div>
            <dt>{t("profiles")}</dt>
            <dd>
              {evaluableProfiles} de {windows.length}
            </dd>
          </div>
        ) : null}
        {windows.length > 0 ? (
          <div>
            <dt>{t("incomplete")}</dt>
            <dd>
              {incompleteWindows} de {windows.length}
            </dd>
          </div>
        ) : null}
        {coverage.failures.length > 0 ? (
          <div>
            <dt>{t("failures")}</dt>
            <dd>{coverage.failures.length}</dd>
          </div>
        ) : null}
      </dl>
    </section>
  );
}
