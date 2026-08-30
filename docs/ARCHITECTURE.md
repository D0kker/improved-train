# Arquitectura

Última actualización: 2026-08-29

> Estado: arquitectura planificada. Todavía no hay componentes implementados.

## Contexto

LoL Network Analyzer es una aplicación de análisis histórico, no una herramienta para revelar información de partidas activas. Recibe un Riot ID visible, resuelve su PUUID y procesa partidas finalizadas para construir encuentros y relaciones reutilizables.

## Vista de contenedores prevista

```mermaid
flowchart LR
  Browser[Browser] --> Web[Next.js web]
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

La ubicación final de Hangfire y la frontera API/worker se decidirán al implementar Sprint 1. El diagrama expresa responsabilidades, no una topología ya desplegada.

## Monorepo previsto

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
2. API resuelve el PUUID mediante ACCOUNT-V1 usando el routing regional correcto.
3. Un job obtiene IDs de MATCH-V5 con concurrencia acotada.
4. Cada match se busca por ID en PostgreSQL antes de llamar a Riot.
5. Los faltantes se guardan como JSONB y se normalizan en jugadores, partidas y participantes.
6. Procesos derivados reconstruyen encuentros; relaciones, grupos y grafo pertenecen a sprints posteriores.
7. El frontend consulta el progreso y resultados solo mediante `/api/v1`.

## Dependencias e invariantes

- PostgreSQL es fuente de verdad; Redis acelera pero no reemplaza persistencia.
- `players.puuid` y `matches.riot_match_id` son únicos.
- Una pareja de relaciones se normaliza como `player_a_id < player_b_id`.
- Las llamadas a Riot usan cliente abstraído, timeout, cancelación, backoff y control de `429`.
- Logs estructurados nunca incluyen la Riot API key, cuerpos sensibles ni identificadores innecesarios.
- CI usa HTTP simulado y no consume Riot.
- Imágenes objetivo: `linux/arm64` y `linux/amd64`.

## Despliegue previsto

El primer runtime privado será Docker Compose sobre Raspberry Pi, con volúmenes persistentes para PostgreSQL y health checks de web, API, worker, PostgreSQL y Redis. Cloudflare Tunnel/Nginx, publicación web y AWS se incorporarán solo cuando el alcance correspondiente sea aprobado y validado.
