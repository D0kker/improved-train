import Link from "next/link";

export function SiteHeader({
  actionLabel = "Buscar jugador",
  actionHref = "/",
}: {
  actionLabel?: string;
  actionHref?: string;
}) {
  return (
    <header className="topbar">
      <nav aria-label="Navegación principal">
        <Link className="wordmark" href="/" aria-label="PROJECT ECHO — Inicio">
          <span className="wordmark-icon" aria-hidden="true">E</span>
          <span>PROJECT ECHO</span>
        </Link>
        <Link className="text-link" href={actionHref}>{actionLabel}</Link>
      </nav>
    </header>
  );
}
