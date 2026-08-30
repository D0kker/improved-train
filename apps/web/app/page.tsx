import { PlayerSearchForm } from "./player-search-form";

export default function Home() {
  return (
    <main className="home-shell">
      <section className="hero" aria-labelledby="page-title">
        <div className="brand-mark" aria-hidden="true">
          LN
        </div>
        <p className="eyebrow">Análisis histórico post-partida</p>
        <h1 id="page-title">Descubre a quién sigues encontrando.</h1>
        <p className="lede">
          Busca un Riot ID para revisar partidas guardadas, rivales y aliados
          recurrentes. Las coincidencias muestran patrones históricos, no
          relaciones verificadas entre jugadores.
        </p>
        <PlayerSearchForm />
        <ul className="trust-list" aria-label="Límites del análisis">
          <li>Hasta 20 partidas por sincronización</li>
          <li>Solo partidas terminadas</li>
          <li>La API de Riot nunca se consulta desde tu navegador</li>
        </ul>
      </section>
    </main>
  );
}
