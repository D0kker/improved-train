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

## Sprint 3 — iniciado con dos tareas

- [ ] **En curso:** persistir `PlayerEncounter` con constraints e índices.
- [ ] **En curso:** implementar `RepeatedPlayerAnalyzer` idempotente.
- [ ] Exponer summary, repeated players, matches y match detail.
- [ ] Crear las primeras vistas funcionales del frontend.

## Sprint 4

- [ ] Implementar relaciones, score configurable y posibles premades.
- [ ] Usar niveles `LOW`, `MEDIUM`, `HIGH`, `VERY_HIGH` sin presentarlos como probabilidades.

## Sprint 5

- [ ] Implementar grafo social, detección de grupos y familiaridad de partida.

## Sprint 6 — planificado

- [ ] Jobs persistentes, exclusión de duplicados y sincronización incremental.
- [ ] Rate limiting, caché, refresh, observabilidad y pruebas de resiliencia.

## Sprint 7 — planificado

- [ ] Cumplimiento Riot, privacidad/legal, seguridad, HTTPS y backups.
- [ ] Indexación, incident response y auditoría go/no-go de V1.

## Arquitectura

- [ ] Ejecutar `ARC-001`, benchmark reproducible .NET frente a Go en ARM64.

## Antes de una V1 pública

- [ ] Obtener el nivel de acceso Riot apropiado y registrar el producto.
- [ ] Revisar políticas, privacidad, términos, disclaimer, HTTPS, seguridad, backups y observabilidad.
- [ ] Decidir licencia, dominio e indexación pública.
