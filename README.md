# LoL Network Analyzer

Herramienta de análisis histórico y post-partida para descubrir jugadores recurrentes, relaciones y patrones dentro del historial de League of Legends.

El repositorio está en fase de **foundation**. La fuente funcional y técnica es [`lol_network_analyzer_spec.md`](lol_network_analyzer_spec.md); todavía no se ha implementado el monorepo ni existen servicios ejecutables.

## Enfoque del producto

- Identificar jugadores por PUUID, no por nombre visible.
- Reutilizar partidas persistidas para reducir llamadas a Riot.
- Distinguir encuentros como aliado y como rival.
- Inferir relaciones con lenguaje prudente, sin afirmar duos verificados.
- Mantener la Riot API key únicamente en backend o worker.
- Ejecutar el MVP de forma local en Raspberry Pi mediante Docker Compose.

## Arquitectura prevista

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
- [ ] Sprint 1: monorepo, servicios, health checks y primera integración de Riot.

Consulta [`docs/TODO.md`](docs/TODO.md) para el roadmap y [`docs/PROJECT_CONTEXT.md`](docs/PROJECT_CONTEXT.md) antes de comenzar una nueva sesión.

## Secretos

Copia `.env.example` como `.env` solamente cuando existan servicios que consuman esas variables. Nunca publiques `.env`, una Riot API key, credenciales de PostgreSQL ni tokens.

## Licencia y cumplimiento

La licencia del código aún no está decidida. Antes de una publicación abierta o monetización se deben revisar las políticas vigentes de Riot, registrar el producto cuando corresponda y agregar los avisos legales requeridos.
