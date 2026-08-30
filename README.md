# LoL Network Analyzer

Herramienta de análisis histórico y post-partida para descubrir jugadores recurrentes, relaciones y patrones dentro del historial de League of Legends.

Los Sprints 1–3 están implementados y Sprint 4 está iniciado: el monorepo, la ingesta ACCOUNT-V1/MATCH-V5, encounters, APIs de consulta, primeras vistas y bases de relaciones son ejecutables. La fuente funcional y técnica continúa siendo [`lol_network_analyzer_spec.md`](lol_network_analyzer_spec.md).

## Enfoque del producto

- Identificar jugadores por PUUID, no por nombre visible.
- Reutilizar partidas persistidas para reducir llamadas a Riot.
- Distinguir encuentros como aliado y como rival.
- Inferir relaciones con lenguaje prudente, sin afirmar duos verificados.
- Mantener la Riot API key únicamente en backend o worker.
- Ejecutar el MVP de forma local en Raspberry Pi mediante Docker Compose.

## Arquitectura implementada

```text
Browser -> Next.js -> .NET API -> PostgreSQL
                         |      -> Redis
                         +-----> Riot API
                         +-----> ingestion worker
```

El detalle y la separación entre estado planificado e implementado están en [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md). El análisis de alcance, fortalezas, tensiones y decisiones pendientes está en [`docs/SPEC_ANALYSIS.md`](docs/SPEC_ANALYSIS.md).

## Estado actual

- [x] Especificación inicial analizada y preservada.
- [x] Documentación de arquitectura, decisiones y continuidad creada.
- [x] Configuración de Codex, memoria y skill del repositorio preparada.
- [x] Sprint 1: monorepo, servicios, health checks y primera integración de Riot.
- [x] Sprint 2: ingesta acotada, raw JSONB, normalización y deduplicación de partidas.
- [x] Sprint 3: encounters, analizador de repetidos, APIs de lectura y primeras vistas funcionales.
- [ ] Sprint 4: persistencia y score listos; reconstrucción, posibles premades, API y UI pendientes.

Consulta [`docs/TODO.md`](docs/TODO.md) para el roadmap y [`docs/PROJECT_CONTEXT.md`](docs/PROJECT_CONTEXT.md) antes de comenzar una nueva sesión.

## Ejecutar y validar

```bash
cp .env.example .env
docker compose up -d --build
docker compose ps
```

La web es el único servicio publicado al host: `http://localhost:38080`. En la máquina actual también responde en `http://192.168.100.55:38080`. La portada y `/api/health` funcionan sin una key; las operaciones Riot devuelven un `503` controlado mientras `RIOT_API_KEY` no esté configurada.

## Secretos

Copia `.env.example` como `.env`, define credenciales locales y nunca publiques `.env`, una Riot API key, credenciales de PostgreSQL ni tokens.

## Licencia y cumplimiento

La licencia del código aún no está decidida. Antes de una publicación abierta o monetización se deben revisar las políticas vigentes de Riot, registrar el producto cuando corresponda y agregar los avisos legales requeridos.
