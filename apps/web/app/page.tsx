import { PlayerSearchForm } from "./player-search-form";

export default function Home() {
  return (
    <main className="home-shell">
      <section className="hero" aria-labelledby="page-title">
        <div className="brand-mark" aria-hidden="true">
          E
        </div>
        <p className="eyebrow">PROJECT ECHO · historial post-partida</p>
        <h1 id="page-title">Who have you played with before?</h1>
        <p className="lede">
          Busca tu Riot ID para descubrir qué aliados y rivales ya habían
          aparecido en tus partidas guardadas, cuántas veces y cuándo fue el
          último encuentro.
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
