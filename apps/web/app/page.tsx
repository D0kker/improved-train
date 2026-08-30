export default function Home() {
  return (
    <main>
      <section className="panel" aria-labelledby="page-title">
        <p className="eyebrow">Sprint 1</p>
        <h1 id="page-title">LoL Network Analyzer</h1>
        <p className="lede">
          La base segura para analizar encuentros históricos de League of
          Legends está en marcha.
        </p>
        <dl>
          <div>
            <dt>Identidad interna</dt>
            <dd>PUUID</dd>
          </div>
          <div>
            <dt>Fuente de verdad</dt>
            <dd>PostgreSQL</dd>
          </div>
          <div>
            <dt>Estado</dt>
            <dd>Servicios base saludables</dd>
          </div>
        </dl>
        <p className="note">
          La búsqueda y la ingesta visual se habilitarán en sus sprints
          correspondientes.
        </p>
      </section>
    </main>
  );
}
