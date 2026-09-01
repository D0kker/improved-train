# Roadmap

Última actualización: 2026-08-31

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

## Sprint 5 — completado

- [x] S5-001: cerrar la historia padre de grafo, grupos y familiaridad.
- [x] S5-002: calcular familiaridad con orden `(occurred_at, riot_match_id)`, solo historial anterior y denominador auditable.
- [x] S5-003: detectar grupos canónicos máximos de 3–5 sin subgrupos redundantes ni explosión combinatoria.
- [x] S5-004: exponer red ego `/api/v1/players/{puuid}/network` con límites, truncation metadata y orden estable.
- [x] S5-005: crear grafo progresivo con alternativa tabular funcional y accesible.
- [x] S5-006: mostrar familiaridad y grupos usando evidencia anterior y participantes visibles.
  - [x] Mostrar posibles premades de 2–5 jugadores en el detalle, con etiqueta prudente, código textual y color por grupo.
  - [x] Enlazar cada Riot ID visible del detalle y de la leyenda de grupos con su perfil; datos incompletos permanecen como texto.
  - [x] Integrar familiaridad histórica contextual y sus estados de evidencia.
- [x] S5-007: perfil relacionado con resumen local; navegación sin sincronización automática y con estado de frescura.

## Sprint 6 — completado localmente

- [x] S6-001: formalizar jobs persistentes.
  - [x] Definir contrato durable, migración PostgreSQL y endpoints `POST /players/{puuid}/analysis` / `GET /jobs/{jobId}`.
  - [x] Implementar claim/transiciones del worker, progreso, error seguro, cancelación y recuperación tras reinicio.
- [x] S6-002: excluir solicitudes activas equivalentes mediante índice parcial PostgreSQL y retorno idempotente del job existente.
- [x] S6-003: sincronizar incrementalmente en páginas, reutilizar matches globales y reconstruir agregados; el caso 190/200 descarga 10 detalles.
- [x] S6-004: centralizar concurrencia Riot por proceso/routing, respetar `Retry-After`, aplicar backoff acotado y cancelación cooperativa con HTTP simulado.
- [x] S6-005: abstraer caché Redis/memoria, TTL configurable e invalidación de resumen al completar o sincronizar.
- [x] S6-006: refresh programado opt-in con frecuencia configurable, claim durable sin solapes y desactivación segura.
- [x] S6-007: instrumentar logs seguros y métricas agregadas de requests, Riot/429, ingesta, caché y jobs sin identificadores ni payloads.
- [x] S6-008: verificar reinicio, deduplicación, 429, Redis caído y consultas reales; documentar planes y runbook reproducible.

## Sprint 7 — planificado

- [ ] S7-001: cumplimiento Riot y revisión actual de registro, acceso Production, monetización transformativa y canales de solicitudes de eliminación.
  - [x] Crear matriz inicial con fuentes oficiales verificadas el 2026-08-31.
  - [ ] Obtener registro/auditoría y acceso Production antes de abrir el producto.
- [ ] S7-002: inventario, retención, eliminación y exportación de datos.
  - [x] Crear inventario técnico inicial y límites de logging/exposición.
  - [ ] Aprobar periodos, contacto y proceso operativo antes de publicar.
- [ ] S7-003: Privacy Policy, Terms y disclaimer antes de publicación.
- [ ] Threat model, secretos de runtime, rate limiting propio, validación, errores seguros, headers/CORS/CSP, contenedores no-root y escaneo de dependencias.
- [ ] HTTPS, superficie privada de PostgreSQL/Redis, backups probados y runbook de incidentes.
- [ ] Indexación, incident response y auditoría go/no-go de V1.
- [ ] S7-010: evaluar el crédito Azure como staging opcional, con coste acotado y sin duplicar GitHub ni migrar el MVP prematuramente.

## Sprint 8 — creado y refinado

- [ ] S8-001: cerrar la historia padre de insights históricos.
- [ ] S8-002: construir historial canónico y evolución temporal entre dos jugadores.
- [ ] S8-003: calcular `most seen`, `best teammate` y `nemesis` con evidencia mínima configurable.
- [ ] S8-004: exponer contratos paginados y acotados de relación e insights.
- [ ] S8-005: crear UI accesible de historial, tendencias e insights.
- [ ] S8-006: generar tarjeta compartible bajo demanda, sin publicación automática ni IDs internos.

## Investigación revisada

- [x] GM-02: investigación contrastada; requisitos confirmables se trasladaron a Sprint 7 y se descartaron plazos/afirmaciones sin fuente oficial suficiente.
- [x] GM-03: refinamiento aplicado selectivamente; rendimiento/operación permanece en Sprint 6, seguridad/privacidad en Sprint 7 y monetización después del go/no-go público.
- [ ] Gemini listo para investigación: GM-04 a GM-08 cubren rate limiting, caché, refresh, observabilidad y resiliencia de Sprint 6.
- [ ] Gemini listo para investigación: GM-09 a GM-11 cubren cumplimiento Riot, privacidad/threat model y despliegue/continuidad de Sprint 7.
- [ ] Gemini listo para investigación: GM-12 diseña el benchmark ARC-001 .NET frente a Go en ARM64.

## Arquitectura

- [ ] Ejecutar `ARC-001`, benchmark reproducible .NET frente a Go en ARM64.

## Antes de una V1 pública

- [ ] Obtener el nivel de acceso Riot apropiado y registrar el producto.
- [ ] Revisar políticas, privacidad, términos, disclaimer, HTTPS, seguridad, backups y observabilidad.
- [ ] Decidir licencia, dominio e indexación pública.
