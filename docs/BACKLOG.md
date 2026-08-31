# Tablero Kanban

Última actualización: 2026-08-31

Este archivo conserva las historias y criterios versionados. GitHub Project mantiene el estado operativo una vez sincronizado. Los estados son **Por hacer**, **Listo para Codex**, **En curso**, **Bloqueado** y **Completado**; una tarea solo se completa con la evidencia exigida en `AGENTS.md`.

- **Repositorio:** [D0kker/improved-train](https://github.com/D0kker/improved-train)
- **Tablero operativo:** [LoL Network Analyzer — Kanban](https://github.com/users/D0kker/projects/5)
- **Sincronización actual:** 55 historias registradas como issues `#1` a `#55`; GitHub Project es la fuente operativa del estado y este archivo conserva criterios y decisiones de flujo.
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

## Sprint 4 — completado

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
- **Criterios cumplidos:** pareja canónica única, cronología estable sobre la unión de historiales, snapshot global reemplazable, lectura/escritura por lotes y cero llamadas a Riot.

### S4-004 — Detectar posibles premades con lenguaje prudente

- **Issue:** #22.
- **Prioridad:** P1.
- **Dependencias:** S4-003 y revisión vigente de políticas Riot.
- **Criterios cumplidos:** mínimos configurables, clasificación `possible premade`/`likely premade`, negativos para evidencia casual/oponente y sin desanonimización.

### S4-005 — Exponer API de relaciones

- **Issue:** #21.
- **Prioridad:** P1.
- **Dependencias:** S4-003 y S4-004.
- **Criterios cumplidos:** contrato paginado, filtros de confidence, orden estable, metadata observable, 404/límites y sin raw JSON.

### S4-006 — Crear vista de relaciones

- **Issue:** #20.
- **Prioridad:** P1.
- **Dependencias:** S4-005 y frontend de Sprint 3.
- **Criterios cumplidos:** tabla semántica responsive con estados de carga/vacío/error/reintento y lenguaje de inferencia.

## Sprint 5 — en curso

### S5-001 — Historia padre de grafo, grupos y familiaridad

- **Issue:** #11.
- **Prioridad:** P1.

### S5-002 — Calcular familiaridad histórica de partida

- **Issue:** #41.
- **Prioridad:** P0.
- **Criterios ajustados:** usar solo partidas estrictamente anteriores por `(occurred_at, riot_match_id)`; excluir owner, duplicados y participantes no identificables del denominador; conservar conteos además del porcentaje.
- **Estado:** completada el 2026-08-30.
- **Evidencia:** calculador puro determinista, servicio de aplicación, carga agregada sin N+1 y matriz automatizada de primera partida, aliado/rival repetido, futuro, empate temporal, duplicados y datos incompletos. GM-01 fue revisada; se adoptaron sus recomendaciones compatibles con el contrato y se aplazaron score/win-rate fuera de alcance.

### S5-003 — Detectar posibles grupos recurrentes

- **Issue:** #42.
- **Prioridad:** P0.
- **Criterios ajustados:** grupos canónicos máximos de 3–5 con todas las parejas por encima del umbral; solapamientos explícitos, sin emitir subgrupos redundantes y con candidatos/límites configurables.
- **Estado:** completada el 2026-08-30.
- **Evidencia:** detector puro de cliques, clasificación por la evidencia más débil, IDs canónicos, eliminación de subgrupos, solapamientos permitidos y límites configurables de candidatos/combinaciones.

### S5-004 — Exponer contrato de red social

- **Issue:** #45.
- **Prioridad:** P1.
- **Contrato ajustado:** `GET /api/v1/players/{puuid}/network`; red ego de profundidad uno por defecto, máximo configurable de nodos/edges, filtros, truncation metadata y orden estable.
- **Estado:** completada el 2026-08-30.
- **Evidencia:** contrato con centro/nodos/aristas, filtros por confianza y score, límites configurables, metadata de totales/truncamiento y 404/vacío/límites cubiertos por integración; reutiliza la consulta estable de relaciones y no llama a Riot.

### S5-005 — Crear visualización accesible del grafo

- **Issue:** #43.
- **Prioridad:** P1.
- **Criterio ajustado:** interacción visual progresiva y alternativa tabular equivalente; el grafo no bloquea la tabla y cualquier dependencia requiere justificar peso, mantenimiento y compatibilidad.

### S5-006 — Mostrar familiaridad y grupos en detalle

- **Issue:** #44.
- **Prioridad:** P1.
- **Criterio ajustado:** evidencia estrictamente anterior, grupos inferidos solo entre participantes visibles y estados explícitos para historial insuficiente/incompleto.
- **Estado:** en curso; la presentación de premades quedó implementada, familiaridad contextual permanece pendiente.
- **Entrega parcial:** el detalle expone dúos no contenidos y cliques máximos de 3–5 por equipo. La UI usa `P1`, `P2`, etc., cinco tonos reutilizables y una leyenda textual; distingue `Posible premade` de `Posible premade · evidencia alta` y aclara que no confirma que hayan entrado juntos.
- **Navegación:** los Riot IDs completos son enlaces accesibles al perfil tanto en cada equipo como en la leyenda de premades; nombres/tags incompletos no generan rutas ambiguas.
- **Evidencia parcial:** contrato cubierto con dos grupos, niveles distintos, exclusión de relaciones entre rivales y ausencia de lenguaje `verified`; 41 pruebas unitarias, 13 de integración y 6 pruebas frontend aprobadas, además de lint, type-check, formato y builds Docker de API/web.

### S5-007 — Perfil relacionado con resumen local

- **Issue:** #53.
- **Prioridad:** P1.
- **Estado:** propuesta pendiente de revisión.
- **Objetivo:** al navegar desde una conexión recurrente, mostrar primero el resumen local disponible y dejar la sincronización con Riot como acción explícita.
- **Criterios propuestos:** conservar PUUID como identidad interna, indicar frescura/ausencia de datos, evitar consumo automático de API al navegar, tolerar Riot IDs mutables y cubrir estados cacheado, vacío, error, cancelación y rutas codificadas.
- **Fuera de alcance:** autenticación, perfiles públicos, compartir, publicidad o cambios de identidad Riot.

## Sprint 6 — operación y rendimiento planificados

- Mantener jobs persistentes, exclusión concurrente, sincronización incremental, caché, rate limiting y observabilidad como núcleo del sprint.
- Incorporar de GM-03 únicamente mediciones reproducibles sobre consultas reales e índices PostgreSQL justificados con planes/latencias antes y después; no usar objetivos arbitrarios como `<50 ms`.
- Probar reintentos, `429`, timeout y degradación con HTTP simulado; CI nunca consume Riot.
- Los análisis de sinergia de línea o `performance delta` no reemplazan este alcance y requieren una historia futura separada por su riesgo de convertirse en una métrica de habilidad no permitida.

## Sprint 7 — cumplimiento, privacidad y seguridad planificados

- **S7-001/#32:** verificar de nuevo fuentes oficiales y requisitos de registro/acceso Production, monetización transformativa, branding y disclaimer antes de publicar.
- **S7-002/#31:** inventariar datos, propósito, retención, exportación y eliminación; definir cómo atender identificadores enviados por Riot por sus canales oficiales sin inventar un SLA no publicado.
- **S7-003/#33:** Privacy Policy, Terms y disclaimer versionados, con revisión PO/legal.
- **S7-004/#36:** threat model; secretos solo en runtime; rate limiting propio; validación y errores seguros; CORS/CSP/headers; contenedores no-root; DB/Redis privados y escaneo de dependencias/imágenes.
- **S7-005–009/#34–#39:** HTTPS, backups restaurables, indexación prudente, incident response y auditoría go/no-go con evidencia.
- Autenticación/RSO solo se incorporará si una función pública concreta la necesita y tras confirmar acceso Production; no se agrega por defecto al MVP privado.

## Sprint 8 — creado y refinado

### S8-001 — Historia padre de insights históricos

- **Issue:** #48.
- **Prioridad:** P1.
- **Objetivo:** convertir relaciones y grupos persistidos en explicaciones históricas acotadas, comparables y compartibles de forma segura.

### S8-002 — Construir historial entre dos jugadores

- **Issue:** #51.
- **Prioridad:** P0.
- **Criterios:** detalle canónico A–B, partidas terminadas paginadas, evolución por periodos estables, ally/opponent y cero fuga de partidas futuras.

### S8-003 — Calcular rankings explicables de relaciones

- **Issue:** #49.
- **Prioridad:** P0.
- **Criterios:** `most seen`, `best teammate` y `nemesis` con muestra mínima configurable, evidencia visible, desempate estable y sin presentar comportamiento o intención como hecho.

### S8-004 — Exponer API de insights históricos

- **Issue:** #46.
- **Prioridad:** P1.
- **Contratos:** detalle de relación e insights del jugador con paginación, filtros temporales, límites y sin exponer PUUID internos ni raw JSON.

### S8-005 — Crear UI de historial e insights

- **Issue:** #47.
- **Prioridad:** P1.
- **Criterios:** navegación desde relaciones/grafo, tendencias accesibles con alternativa tabular, evidencia y estados de carga/vacío/error.

### S8-006 — Generar tarjeta compartible segura

- **Issue:** #50.
- **Prioridad:** P2.
- **Criterios:** generación explícita bajo demanda, vista previa, texto alternativo y descarga local; sin publicación automática, PUUID, secretos ni datos no visibles. Requiere decisiones de privacidad de Sprint 7 antes de exposición pública.

## Investigación Gemini

### GM-02 — Requisitos Riot y estrategia de servicio público

- **Issue:** #54.
- **Prioridad:** P1.
- **Estado:** revisada por Codex el 2026-08-30.
- **Prompt:** investigar fuentes actuales de Riot, restricciones de datos y API, privacidad, publicación, monetización y competidores; separar hechos confirmados de recomendaciones e inferencias.
- **Resultado útil:** se conservaron registro/acceso Production, secreto de runtime, HTTPS, producto transformativo, disclaimer, privacidad/retención/eliminación y revisión de monetización para Sprint 7.
- **Correcciones:** no se adoptaron como hechos el remapeo de PUUID entre keys, un SLA fijo de 24 horas para borrar ni un umbral de confianza de 95 %, porque no quedaron respaldados por la fuente oficial revisada.

### GM-03 — Refinamiento de Sprints 6, 7 y futuro

- **Issue:** #55.
- **Estado:** revisada por Codex el 2026-08-30.
- **Resultado útil:** medición de índices/caché/resiliencia para Sprint 6; threat model, rate limiting propio, secretos, headers, contenedores y escaneo para Sprint 7; Data Dragon como opción visual futura.
- **Ajustes:** seguridad y privacidad permanecen en Sprint 7; no se reemplazan jobs/operación de Sprint 6 por métricas de juego; monetización queda condicionada al go/no-go público y no se crea un Sprint 9 artificial.

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
