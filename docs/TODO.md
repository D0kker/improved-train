# Roadmap

Última actualización: 2026-08-29

La especificación original en `lol_network_analyzer_spec.md` conserva el detalle. Esta vista registra alcance y evidencia; no convierte ideas futuras en compromisos.

## Foundation — completada

- [x] Analizar y preservar la especificación.
- [x] Crear README, arquitectura, decisiones y contexto vivo.
- [x] Preparar configuración de secretos de ejemplo, Git ignore, memoria y skill del agente.
- [x] Inicializar y publicar el repositorio GitHub después de la validación local.

## Sprint 1 — siguiente

- [ ] Verificar versiones soportadas de .NET, Next.js, Node, PostgreSQL y Redis.
- [ ] Crear `apps/web`, `apps/api`, `workers/ingestion-worker` e `infrastructure`.
- [ ] Añadir Dockerfiles multi-stage y `docker-compose.yml` compatible con ARM64/AMD64.
- [ ] Implementar health checks de web, API, worker, PostgreSQL y Redis.
- [ ] Configurar EF Core y crear migración para `Player`, `Match` y `MatchParticipant`.
- [ ] Crear `IRiotApiClient`, routing configurable e implementación de `GetAccountByRiotId`.
- [ ] Añadir pruebas unitarias/integración sin llamadas reales a Riot.
- [ ] Verificar `docker compose up -d` y documentar resultados observables.

## Sprint 2

- [ ] Implementar `GetMatchIds` y `GetMatch`.
- [ ] Persistir raw JSON y datos normalizados.
- [ ] Deduplicar partidas y participantes.
- [ ] Añadir pruebas de cliente y repositorios.

## Sprint 3

- [ ] Implementar `RepeatedPlayerAnalyzer` y `PlayerEncounters`.
- [ ] Exponer summary, repeated players, matches y match detail.
- [ ] Crear las primeras vistas funcionales del frontend.

## Sprint 4

- [ ] Implementar relaciones, score configurable y posibles premades.
- [ ] Usar niveles `LOW`, `MEDIUM`, `HIGH`, `VERY_HIGH` sin presentarlos como probabilidades.

## Sprint 5

- [ ] Implementar grafo social, detección de grupos y familiaridad de partida.

## Antes de una V1 pública

- [ ] Obtener el nivel de acceso Riot apropiado y registrar el producto.
- [ ] Revisar políticas, privacidad, términos, disclaimer, HTTPS, seguridad, backups y observabilidad.
- [ ] Decidir licencia, dominio e indexación pública.
