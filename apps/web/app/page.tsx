import { PlayerSearchForm } from "./player-search-form";
import { platformRegionLabel } from "@/src/platform-region";

export const dynamic = "force-dynamic";

export default function Home() {
  const platformRegion = process.env.RIOT_PLATFORM_REGION ?? "la1";
  return (
    <main className="home-shell" id="main-content" tabIndex={-1}>
      <section className="hero" aria-labelledby="page-title">
        <div className="brand-mark" aria-hidden="true">
          E
        </div>
        <p className="eyebrow">PROJECT ECHO · historial post-partida</p>
        <h1 id="page-title">¿Con quién ya habías jugado?</h1>
        <p className="lede">
          Consulta tus partidas recientes y descubre jugadores con los que has
          coincidido anteriormente.
        </p>
        <PlayerSearchForm platformRegion={platformRegion} />
        <ul className="trust-list" aria-label="Alcance de la consulta">
          <li>Comenzamos con tus últimas cinco partidas.</li>
          <li>Puedes consultar más partidas desde tu perfil.</li>
          <li>Solo partidas terminadas</li>
        </ul>
        <p className="region-note">Región de búsqueda: {platformRegionLabel(platformRegion)}</p>
      </section>
    </main>
  );
}
