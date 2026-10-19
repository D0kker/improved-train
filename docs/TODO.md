# Roadmap

Última actualización: 2026-10-18

La especificación original en `lol_network_analyzer_spec.md` conserva el detalle. Esta vista registra alcance y evidencia; no convierte ideas futuras en compromisos.

## T04.1 — auditoría UX/UI (solo documentación)

- [x] Aislar desde T03.5 `41ac5ee` en `feature/t04-1-ux-audit`, preservando la sesión paralela T03.6.
- [x] Inventariar las cuatro páginas reales y componentes/estados desde código; distinguir hallazgos confirmados, recomendaciones y validación visual pendiente.
- [x] Documentar copy, distribución, navegación, responsive, accesibilidad, componentes, Cyber Arcade, idiomas y feedback en [T04_1_UX_UI_AUDIT.md](T04_1_UX_UI_AUDIT.md), con prioridades y aceptación por pantalla.
- [ ] Seleccionar con el PO una mejora pequeña del informe para una historia posterior. No se implementó UI, i18n ni feedback; no se ejecutó evaluación en navegador ni se desplegó.

## T04.2 — correcciones UX prioritarias de PROJECT ECHO

- [ ] Implementar copy compatible con consultas stateless, región visible, enlaces de retorno explícitos, estados de cobertura/error separados, resumen de cinco partidas iniciales y accesibilidad básica; cambios en revisión de rama `feature/t04-2-ux-corrections`.
- [ ] Validar ventana adicional ante detalles fallidos con un mock de extremo a extremo; el frontend reintenta el mismo offset para respuestas parcial/error, pero el endpoint no entrega cursor opaco.
- [ ] Validar con DOM/navegador foco del primer campo inválido, atributos ARIA, navegación por teclado y render responsive; no hay emulador DOM en el lockfile y no se pudo descargar uno.
- [ ] Resolver la validación de build: Next 16.3.3 falla al consumir `tsc --showConfig` con Node 24.18.0; lint y type-check pasan. No declarar el sprint completo ni hallazgos completos antes de la evidencia.

## T03.5 — API stateless

- [x] Verificar rama base con T01/T02/T03 y aislar implementación en `feature/t03-5-stateless-api`.
- [x] Implementar búsqueda autocontenida de cinco partidas y análisis directo sin búsqueda previa.
- [x] Conservar cinco históricas del owner y tres por otro participante, con objetivo/futuro excluidos y deduplicación.
- [x] Eliminar dependencias EF/Redis/jobs del runtime; preservar regresiones y benchmark en proyecto Legacy.
- [x] Acotar caché/concurrencia/payloads, cancelar operaciones y transportar cobertura parcial explícita.
- [x] Cerrar validación final y registrar pruebas/benchmark en `T03_5_STATELESS_ARCHITECTURE.md` (94 unitarias, 15 integración, 16 frontend; builds/formato/imágenes correctos).
- [x] Publicar rama feature y abrir [PR #83](https://github.com/D0kker/improved-train/pull/83) a main, sin merge ni deploy; código en `3afd6c6` y evidencia documental posterior en el historial de la rama.
- [x] Desplegar T03.5 por autorización separada del PO: API/web saludables en 8081, imágenes verificadas, clave preservada y smoke HTTP sin consumo Riot; evidencia en `PROJECT_CONTEXT.md`.
- [ ] Validar análisis real y comportamiento bajo cuota/carga autorizada; revisar timeout/cuota agregada antes de otro hosting o varias instancias.

Las secciones siguientes conservan hitos históricos. Sus referencias a EF, Redis, corpus compartido, jobs y despliegues anteriores no describen requisitos del runtime T03.5. Ver D-042 y el informe `T03_5_STATELESS_ARCHITECTURE.md`.

## ECHO-1 — enfoque del producto

- [x] Separar jugadores conocidos (`>= 1`) de recurrentes (`>= 2`) sin recrear `PlayerEncounter`.
- [x] Exponer conocidos individuales de cada partida con conteos estrictamente anteriores.
- [x] Priorizar encounters y familiaridad en Home, dashboard y detalle de partida.
- [x] Mantener el grafo funcional como sección secundaria/experimental sin ampliarlo.
- [x] Sustituir la reconstrucción global ordinaria por actualización incremental de parejas afectadas.
- [x] Permitir `CACHE_PROVIDER=memory` sin Redis y conservar `CACHE_PROVIDER=redis` con fallback.
- [x] Añadir métricas de producto ECHO agregadas y de baja cardinalidad.
- [x] Conservar worker y refresh programado sin ampliar sus funcionalidades.
- [x] Mantener `raw_data` y registrar la decisión de retención como pendiente.

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

## T02 — estabilización InMemory y AnalysisJobs

- [x] Procesar jobs de la API con cola acotada y `BackgroundService` en el mismo proceso.
- [x] Eliminar operaciones de actualización masiva incompatibles con el proveedor InMemory y serializar transiciones de estado.
- [x] Documentar la limitación de instancia única y pérdida de jobs al reiniciar.
- [ ] Evolucionar a coordinación compartida únicamente junto con una decisión explícita de persistencia durable.

## T03 — estadísticas y familiaridad

- [x] Validar resumen, encuentros y familiaridad con fixtures InMemory completos, repetidos y parciales; 75 pruebas unitarias, 21 de integración y 14 frontend aprobadas. Evidencia en `docs/T03_STATISTICS_AUDIT.md`.

## Sprint 7 — planificado

- [ ] S7-001: cumplimiento Riot y revisión actual de registro, acceso Production, monetización transformativa y canales de solicitudes de eliminación.
  - [x] Crear matriz inicial con fuentes oficiales verificadas el 2026-08-31.
  - [ ] Obtener registro/auditoría y acceso Production antes de abrir el producto.
- [ ] S7-002: inventario, retención, eliminación y exportación de datos.
  - [x] Crear inventario técnico inicial y límites de logging/exposición.
  - [ ] Aprobar periodos, contacto y proceso operativo antes de publicar.
- [ ] S7-003: Privacy Policy, Terms y disclaimer antes de publicación.
- [ ] Threat model, secretos de runtime, rate limiting propio, validación, errores seguros, headers/CORS/CSP, contenedores no-root y escaneo de dependencias.
  - [x] Documentar threat model, política CORS/proxy y respuesta ante compromiso de key.
  - [x] Añadir headers defensivos, límite de cuerpo y backpressure concurrente; verificar contenedores de aplicación no-root.
  - [ ] Añadir límite por cliente en el borde confiable y escaneo reproducible de dependencias/imágenes.
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
- [x] S8-007: incorporar íconos oficiales de campeón con Data Dragon, proxy/caché acotados y fallback accesible; profile icon espera un contrato que aporte el dato.

## Sprint 9 — beta privada multi-región

- [ ] S9-001: cerrar la historia padre de beta privada controlada.
- [ ] S9-002: resolver región y onboarding internacional sin duplicar identidades.
- [ ] S9-003: localizar la experiencia en español e inglés.
- [ ] S9-004: incorporar feedback y analítica respetuosos de privacidad.
- [ ] S9-005: validar capacidad, cuotas, backpressure y degradación.
- [ ] S9-006: controlar acceso y abuso durante la beta.
- [ ] S9-007: ejecutar go/no-go, aprendizaje, soporte y rollback de beta.

## Sprint 10 — lanzamiento público sostenible

- [ ] S10-001: cerrar la historia padre de lanzamiento público.
- [ ] S10-002: diseñar dominio, rollout, presupuesto y rollback.
- [ ] S10-003: evaluar publicidad compatible, consentida y no invasiva.
- [ ] S10-004: medir costes e Infrastructure Coverage Ratio.
- [ ] S10-005: validar propuesta Premium sin integrar pagos.
- [ ] S10-006: implementar indexación pública controlada.
- [ ] S10-007: ejecutar auditoría final y mantener NO-GO ante cualquier P0.

## Experiencia futura — fuera de los sprints activos

- [ ] UX-001/#82: reevaluar con feedback si el grafo debe mejorarse, simplificarse, ocultarse o eliminarse. Mientras tanto permanece funcional y no recibe inversión adicional.

## Investigación revisada

- [x] GM-02: investigación contrastada; requisitos confirmables se trasladaron a Sprint 7 y se descartaron plazos/afirmaciones sin fuente oficial suficiente.
- [x] GM-03: refinamiento aplicado selectivamente; rendimiento/operación permanece en Sprint 6, seguridad/privacidad en Sprint 7 y monetización después del go/no-go público.
- [x] GM-04: rate limiting revisado; confirma límites por routing, `429`/`Retry-After`, concurrencia y cancelación ya implementados. No se añade Polly ni coordinación distribuida fuera del alcance actual.
- [x] GM-05: caché revisada; confirma Redis opcional, fallback en memoria, TTL/invalidation y hash de PUUID ya implementados. No se sustituye la abstracción actual por HybridCache sin necesidad medida.
- [x] GM-06: refresh revisado; confirma opt-in, límites, claim durable y exclusión de jobs ya implementados. Jitter/circuit breaker quedan como mejoras futuras sujetas a una historia y medición explícitas.
- [x] GM-07: observabilidad revisada; confirma métricas agregadas, logs redactados y health aislado ya publicados. No se incorpora exporter/SaaS ni metas de coste sin necesidad operativa.
- [x] GM-08: resiliencia revisada; confirma pruebas de worker, 429, Redis y `EXPLAIN ANALYZE` ya realizadas. No se crean índices nuevos sin medición sobre consulta representativa.
- [x] GM-09: auditoría Riot revisada; se conserva post-partida, inferencia prudente y go/no-go. Se rechazan como no verificadas un SLA de 24 h y un webhook concreto; se crea S7-011 para custom matches/opt-in.
- [x] GM-10: threat model revisado; requisitos útiles permanecen en S7-002/S7-004 sin inventar retención, asesoría legal ni endpoint de borrado.
- [x] GM-11: continuidad revisada; refina S7-005–S7-008 con HTTPS, red privada, backups restaurables e incidentes, sin desplegar ni añadir Caddy todavía.
- [x] GM-12: diseño ARC-001 revisado; sirve como punto de partida reproducible sin autorizar prototipo Go, umbrales rígidos ni migración.

## Arquitectura

- [ ] Ejecutar `ARC-001`, benchmark reproducible .NET frente a Go en ARM64; el corpus, métricas y decisión final no se infieren de una sola investigación.

## Antes de una V1 pública

- [ ] Obtener el nivel de acceso Riot apropiado y registrar el producto.
- [ ] Revisar políticas, privacidad, términos, disclaimer, HTTPS, seguridad, backups y observabilidad.
- [ ] Decidir licencia, dominio e indexación pública.
