# Roadmap

Última actualización: 2026-08-30

La especificación original en `lol_network_analyzer_spec.md` conserva el detalle. Esta vista registra alcance y evidencia; no convierte ideas futuras en compromisos.

## Foundation — completada

- [x] Analizar y preservar la especificación.
- [x] Crear README, arquitectura, decisiones y contexto vivo.
- [x] Preparar configuración de secretos de ejemplo, Git ignore, memoria y skill del agente.
- [x] Inicializar y publicar el repositorio GitHub después de la validación local.

## Sprint 1 — completado localmente

- [x] Verificar versiones soportadas de .NET, Next.js, Node, PostgreSQL y Redis.
- [x] Crear `apps/web`, `apps/api`, `workers/ingestion-worker` e `infrastructure`.
- [x] Añadir Dockerfiles multi-stage y `docker-compose.yml` compatible con ARM64/AMD64.
- [x] Implementar health checks de web, API, worker, PostgreSQL y Redis.
- [x] Configurar EF Core y crear migración para `Player`, `Match` y `MatchParticipant`.
- [x] Crear `IRiotApiClient`, routing configurable e implementación de `GetAccountByRiotId`.
- [x] Añadir pruebas unitarias/integración sin llamadas reales a Riot.
- [x] Verificar `docker compose up -d` por `38080` y documentar resultados observables.

## Sprint 2 — completado localmente

- [x] Implementar `GetMatchIds` y `GetMatch`.
- [x] Persistir raw JSONB y datos normalizados.
- [x] Deduplicar partidas y participantes.
- [x] Añadir pruebas de cliente, servicio y endpoint con HTTP simulado.

## Sprint 3 — completado localmente

- [x] S3-001: persistir `PlayerEncounter` dirigido con constraint anti-autorrelación, clave compuesta e índice de ranking.
- [x] S3-002: reconstruir encounters de forma determinista, transaccional e idempotente.
- [x] S3-003: exponer summary y repeated players; filtrar repetidos con dos o más encuentros.
- [x] S3-004: exponer historial paginado y detalle por Riot match ID.
- [x] S3-005: integrar summary, encounters e historial sin exponer raw JSON.
- [x] S3-006: crear búsqueda y flujo visual síncrono acotado a 20 hasta los jobs de Sprint 6.
- [x] S3-007: crear vistas responsive de repetidos, historial y detalle con estados de carga/error/vacío.

## Sprint 4 — completado

- [x] S4-001: persistir parejas canónicas `PlayerRelationship` con constraints, FKs e índices por ambos jugadores.
- [x] S4-002: calcular score 0–100 configurable y niveles `LOW`, `MEDIUM`, `HIGH`, `VERY_HIGH` explicables, no probabilísticos.
- [x] S4-003: reconstruir relaciones globales de forma transaccional e idempotente, con lectura/escritura por lotes y sin llamadas a Riot.
- [x] S4-004: detectar únicamente `possible premade`/`likely premade` con evidencia mínima configurable.
- [x] S4-005: exponer `/api/v1/players/{puuid}/relationships` paginado y explicable.
- [x] S4-006: crear vista accesible de relaciones y posibles premades.

## Sprint 5 — en curso

- [ ] S5-001: cerrar la historia padre de grafo, grupos y familiaridad.
- [x] S5-002: calcular familiaridad con orden `(occurred_at, riot_match_id)`, solo historial anterior y denominador auditable.
- [x] S5-003: detectar grupos canónicos máximos de 3–5 sin subgrupos redundantes ni explosión combinatoria.
- [ ] S5-004: exponer red ego `/api/v1/players/{puuid}/network` con límites, truncation metadata y orden estable.
- [ ] S5-005: crear grafo progresivo con alternativa tabular funcional y accesible.
- [ ] S5-006: mostrar familiaridad y grupos usando evidencia anterior y participantes visibles.
- [ ] S5-007: perfil relacionado con resumen local; navegación sin sincronización automática y con estado de frescura.

## Sprint 6 — planificado

- [ ] Jobs persistentes, exclusión de duplicados y sincronización incremental.
- [ ] Rate limiting, caché, refresh, observabilidad y pruebas de resiliencia.

## Sprint 7 — planificado

- [ ] Cumplimiento Riot, privacidad/legal, seguridad, HTTPS y backups.
- [ ] Indexación, incident response y auditoría go/no-go de V1.

## Sprint 8 — creado y refinado

- [ ] S8-001: cerrar la historia padre de insights históricos.
- [ ] S8-002: construir historial canónico y evolución temporal entre dos jugadores.
- [ ] S8-003: calcular `most seen`, `best teammate` y `nemesis` con evidencia mínima configurable.
- [ ] S8-004: exponer contratos paginados y acotados de relación e insights.
- [ ] S8-005: crear UI accesible de historial, tendencias e insights.
- [ ] S8-006: generar tarjeta compartible bajo demanda, sin publicación automática ni IDs internos.

## Arquitectura

- [ ] Ejecutar `ARC-001`, benchmark reproducible .NET frente a Go en ARM64.

## Antes de una V1 pública

- [ ] Obtener el nivel de acceso Riot apropiado y registrar el producto.
- [ ] Revisar políticas, privacidad, términos, disclaimer, HTTPS, seguridad, backups y observabilidad.
- [ ] Decidir licencia, dominio e indexación pública.
