# Arquitectura

Última actualización: 2026-08-31

> Estado: Sprints 1–5 implementados. Sprint 6 inicia con jobs persistentes y operación avanzada.

## Contexto

LoL Network Analyzer es una aplicación de análisis histórico, no una herramienta para revelar información de partidas activas. Recibe un Riot ID visible, resuelve su PUUID y procesa partidas finalizadas para construir encuentros y relaciones reutilizables.

## Vista de contenedores implementada

```mermaid
flowchart LR
  Browser[Browser] -->|38080| Web[Next.js web]
  Web --> API[.NET API /api/v1]
  API --> Jobs[Background jobs]
  Jobs --> Worker[Ingestion worker]
  API --> Postgres[(PostgreSQL)]
  Worker --> Postgres
  API --> Redis[(Redis cache)]
  Worker --> Redis
  API --> Riot[Riot API]
  Worker --> Riot
```

Solo Next.js se publica al host. API, worker, PostgreSQL y Redis permanecen en la red privada. El endpoint síncrono de Sprint 2 continúa acotado a 20 partidas durante la transición. Sprint 6 persiste solicitudes de hasta 200 partidas en PostgreSQL mediante `analysis_jobs`; API crea/consulta/cancela y el worker reclama con bloqueo no bloqueante, pagina, actualiza progreso y recupera leases vencidos. Redis no es la fuente de verdad durable.

Cada proceso que llama a Riot usa un único `IRiotRateLimiter` para el routing regional configurado: limita concurrencia, comparte el cooldown indicado por `Retry-After` y permite cancelación. La ingesta masiva vive en el worker; esta primera versión no afirma coordinación distribuida entre réplicas y deberá evolucionar antes de escalar horizontalmente.

`ICacheService` mantiene PostgreSQL como fuente de verdad. La implementación principal escribe en Redis con TTL e índice de tags, y conserva memoria local como fallback temporal cuando Redis falla. El resumen del jugador es el primer contrato cacheado; completar un job o terminar la sincronización síncrona invalida el tag hash del owner. Un miss o payload inválido vuelve a PostgreSQL y nunca modifica datos persistidos.

## Monorepo implementado

```text
apps/
  web/                       Next.js, TypeScript, Tailwind
  api/
    src/
      LolAnalyzer.Api/
      LolAnalyzer.Application/
      LolAnalyzer.Domain/
      LolAnalyzer.Infrastructure/
    tests/
      LolAnalyzer.UnitTests/
      LolAnalyzer.IntegrationTests/
workers/
  ingestion-worker/
infrastructure/
  docker/
  aws/                       reservado; sin recursos obligatorios en MVP
docs/
scripts/
docker-compose.yml
```

## Flujo de datos

1. El usuario introduce `GameName#TagLine` y una región de plataforma.
2. API busca primero el Riot ID en PostgreSQL; solo un ID ausente se resuelve mediante ACCOUNT-V1 usando el routing regional correcto.
3. La operación acotada obtiene IDs de MATCH-V5 con concurrencia configurable entre 1 y 5.
4. Cada match se busca por ID en PostgreSQL antes de llamar a Riot.
5. Los faltantes se guardan como JSONB y se normalizan en jugadores, partidas y participantes.
6. `RepeatedPlayerAnalyzer` reconstruye de forma transaccional la pareja dirigida owner/other en `player_encounters`.
7. El frontend consulta summary, encounters, historial y detalle solo mediante `/api/v1` y conserva resultados locales si falla una actualización Riot.
8. Sprint 4 reconstruye `player_relationships` globalmente, clasifica evidencia como `possible premade`/`likely premade` y la expone mediante `/api/v1/players/{puuid}/relationships`; la UI muestra factores y lenguaje prudente.
9. Sprint 5 expone una red ego acotada y la representa con SVG nativo más tabla equivalente; el detalle combina grupos visibles y familiaridad estrictamente anterior usando el owner transportado desde el historial.
10. Abrir un perfil conocido es lectura local; sincronizar hasta 20 partidas exige una acción explícita.

## Dependencias e invariantes

- PostgreSQL es fuente de verdad; Redis acelera pero no reemplaza persistencia.
- `players.puuid` y `matches.riot_match_id` son únicos.
- `player_encounters` usa pareja dirigida `(owner_player_id, other_player_id)` y prohíbe autorrelaciones.
- `player_relationships` usa pareja canónica `player_a_id < player_b_id`, conserva la suma same/opposite y limita el score a 0–100.
- Los niveles de relación son etiquetas heurísticas explicables, nunca probabilidades ni confirmaciones de duo.
- Las llamadas a Riot usan cliente abstraído, timeout, cancelación, backoff y control de `429`.
- Logs estructurados nunca incluyen la Riot API key, cuerpos sensibles ni identificadores innecesarios.
- CI usa HTTP simulado y no consume Riot.
- Imágenes objetivo: `linux/arm64` y `linux/amd64`.

## Despliegue previsto

El runtime privado actual es Docker Compose, con volúmenes persistentes y health checks de web, API, worker, PostgreSQL y Redis. PostgreSQL 18 monta su volumen en `/var/lib/postgresql`; las aplicaciones corren no-root. Cloudflare Tunnel/Nginx, publicación web y AWS se incorporarán solo cuando el alcance correspondiente sea aprobado y validado.
