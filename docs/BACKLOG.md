# Tablero Kanban

Última actualización: 2026-08-30

Este archivo conserva las historias y criterios versionados. GitHub Project mantiene el estado operativo una vez sincronizado. Los estados son **Por hacer**, **Listo para Codex**, **En curso**, **Bloqueado** y **Completado**; una tarea solo se completa con la evidencia exigida en `AGENTS.md`.

- **Repositorio:** [D0kker/improved-train](https://github.com/D0kker/improved-train)
- **Tablero operativo:** [LoL Network Analyzer — Kanban](https://github.com/users/D0kker/projects/5)
- **Sincronización actual:** 45 historias registradas como issues `#1` a `#45`; GitHub Project es la fuente operativa del estado y este archivo conserva criterios y decisiones de flujo.
- **Prioridades:** P0 bloquea el sprint; P1 es necesaria para el resultado; P2 mejora continuidad u operación sin bloquear el núcleo.

## Sprint 1/2 — completados

### S1-001 — Crear los servicios mínimos del monorepo

- **Prioridad:** P0
- **Criterios de aceptación:** existen web, API y worker compilables; las capas .NET preservan dependencias hacia el dominio; no hay secretos versionados.

### S1-002 — Persistencia inicial en PostgreSQL

- **Prioridad:** P0
- **Criterios de aceptación:** `Player`, `Match` y `MatchParticipant` tienen configuración, índices, constraints y migración EF Core; no se usa `EnsureCreated()`.

### S1-003 — Resolver Riot ID mediante ACCOUNT-V1

- **Prioridad:** P0
- **Criterios de aceptación:** `IRiotApiClient.GetAccountByRiotId` usa routing regional configurable, timeout, cancelación y resiliencia; el browser nunca recibe la key; las pruebas usan HTTP simulado.

### S1-004 — Ejecutar el stack privado con Docker Compose

- **Prioridad:** P0
- **Criterios de aceptación:** web, API, worker, PostgreSQL y Redis quedan saludables; la web y el proxy `/api/v1` son accesibles por `38080`; PostgreSQL y Redis conservan datos en volúmenes.

### S1-005 — Automatizar validación segura

- **Prioridad:** P1
- **Criterios de aceptación:** CI ejecuta frontend y .NET, valida Compose y no llama a Riot ni requiere una API key real.

### S2-001 — Ingestar y deduplicar partidas terminadas

- **Prioridad:** P1
- **Criterios de aceptación:** implementar `GetMatchIds`, `GetMatch`, raw JSONB, normalización y consulta local previa por `riot_match_id`.

## Sprint 3 — completado localmente

### S3-001 — Persistir encuentros entre jugadores

- **Prioridad:** P0
- **Criterios de aceptación:** entidad/migración `PlayerEncounter`, pareja dirigida, sin autorrelaciones, clave única e índices de consulta.

### S3-002 — Implementar RepeatedPlayerAnalyzer

- **Prioridad:** P0
- **Criterios de aceptación:** reconstrucción determinista e idempotente, aliado/rival, victorias/derrotas, fechas y pruebas de límites.

### S3-003 — Exponer resumen y jugadores repetidos

- **Issue:** #15.
- **Prioridad:** P0.
- **Criterios de aceptación:** summary incluye partidas, W/L, win rate, jugadores únicos/repetidos; encounters filtra `total_matches >= 2`; 404 y caso cero probados.

### S3-004 — Exponer historial y detalle de partidas

- **Issue:** #13.
- **Prioridad:** P1.
- **Criterios de aceptación:** historial paginado/filtrable; detalle usa Riot match ID, ordena equipos y no expone raw JSON.

### S3-005 — Integrar contratos de consulta

- **Issue:** #12.
- **Prioridad:** P1.
- **Criterios de aceptación:** endpoints usan DTOs estables, validan límites y tienen pruebas de integración sin Riot.

### S3-006 — Crear flujo de análisis en frontend

- **Issue:** #18.
- **Prioridad:** P1.
- **Criterios de aceptación:** búsqueda, lookup, sync síncrono acotado a 20 y carga de resultados same-origin; jobs/status quedan para Sprint 6.

### S3-007 — Crear vistas de resultados

- **Issue:** #19.
- **Prioridad:** P1.
- **Criterios de aceptación:** summary, repetidos, historial y detalle responsive con estados de carga/error/vacío y lenguaje prudente.

## Sprint 4 — iniciado

### S4-001 — Persistir relaciones normalizadas

- **Issue:** #6.
- **Prioridad:** P0.
- **Criterios de aceptación:** pareja canónica `player_a_id < player_b_id`, clave compuesta, FKs, suma same/opposite, score 0–100, migración e índices por ambos jugadores.

### S4-002 — Calcular relationship score configurable

- **Issue:** #16.
- **Prioridad:** P0.
- **Criterios de aceptación:** pesos/ventanas/umbrales validados, score determinista y explicable, pruebas 24/25, 49/50 y 74/75; confidence no es probabilidad.

### S4-003 — Reconstruir relaciones de forma idempotente

- **Issue:** #17.
- **Prioridad:** P0.
- **Dependencias:** S4-001 y S4-002.

### S4-004 — Detectar posibles premades con lenguaje prudente

- **Issue:** #22.
- **Prioridad:** P1.
- **Dependencias:** S4-003 y revisión vigente de políticas Riot.

### S4-005 — Exponer API de relaciones

- **Issue:** #21.
- **Prioridad:** P1.
- **Dependencias:** S4-003 y S4-004.

### S4-006 — Crear vista de relaciones

- **Issue:** #20.
- **Prioridad:** P1.
- **Dependencias:** S4-005 y frontend de Sprint 3.

## Sprint 5 — refinado

### S5-001 — Historia padre de grafo, grupos y familiaridad

- **Issue:** #11.
- **Prioridad:** P1.

### S5-002 — Calcular familiaridad histórica de partida

- **Issue:** #41.
- **Prioridad:** P0.
- **Criterio clave:** usar solo partidas estrictamente anteriores y excluir al owner del denominador.

### S5-003 — Detectar posibles grupos recurrentes

- **Issue:** #42.
- **Prioridad:** P0.
- **Criterio clave:** grupos canónicos de 3–5 con todas las parejas fuertes y candidatos acotados.

### S5-004 — Exponer contrato de red social

- **Issue:** #45.
- **Prioridad:** P1.
- **Contrato:** `GET /api/v1/players/{puuid}/network` con filtros, límites y orden estable.

### S5-005 — Crear visualización accesible del grafo

- **Issue:** #43.
- **Prioridad:** P1.
- **Criterio clave:** interacción visual y alternativa tabular accesible; justificar una dependencia antes de agregarla.

### S5-006 — Mostrar familiaridad y grupos en detalle

- **Issue:** #44.
- **Prioridad:** P1.
- **Criterio clave:** evidencia histórica y lenguaje de inferencia sin desanonimización.

## Por hacer

### GOV-001 — Elegir licencia

- **Prioridad:** P1
- **Criterios de aceptación:** decisión explícita registrada antes de agregar `LICENSE`; no inferir una licencia por defecto.

### GOV-002 — Preparar requisitos de una V1 pública

- **Prioridad:** P2
- **Criterios de aceptación:** acceso Production apropiado, registro Riot, HTTPS, política/privacidad, disclaimer, backups y revisión de seguridad verificados antes de abrir el producto.

### ARC-001 — Medir .NET frente a Go en ARM64

- **Prioridad:** P1
- **Criterios de aceptación:** comparar con una carga equivalente memoria RSS, arranque, throughput/latencia, tamaño de imagen, tiempo de build, persistencia/jobs y coste de migración; registrar mediciones reproducibles y una recomendación. El benchmark no autoriza una migración sin decisión explícita del PO.

## Bloqueado

- **GOV-001** requiere que el propietario elija la licencia.
- **GOV-002** no debe iniciarse durante el MVP privado y depende de decisiones externas de Riot y del propietario.

## Completado

- [x] **FND-001 — Foundation documental y continuidad** (2026-08-29). Especificación preservada, contexto, arquitectura, decisiones, TODO, handoff, configuración de memoria y skill específica creados y revisados.

## Política de movimiento

- Una historia entra en **En curso** solo cuando existe trabajo activo observable.
- El PO mueve una historia a **Listo para Codex** y luego pide a Codex trabajar lo marcado. Codex revisa alcance/dependencias y la mueve a **En curso** al comenzar; el movimiento sin la instrucción del PO no inicia trabajo por sí solo.
- Una historia entra en **Bloqueado** únicamente por una dependencia o decisión externa concreta.
- Una historia entra en **Completado** después de cumplir criterios, actualizar contexto/TODO y ejecutar las validaciones correspondientes.
- Las propuestas no son decisiones aceptadas; las decisiones duraderas se registran en `docs/DECISIONS.md`.
