# Decisiones del proyecto

Vigente para el runtime del checkout: **D-042 — stateless, sin base de datos ni jobs**. Las decisiones anteriores se conservan como historia; no deben interpretarse como requisitos de persistencia actuales. Los servicios activos siguen en T03 hasta un despliegue autorizado por separado.

## D-001 — Foundation documental antes del código

- Fecha: 2026-08-29
- Estado: cumplida
- Decisión: preservar la especificación original y crear primero la documentación, configuración de agente, memoria y roadmap; el código de Sprint 1 será una entrega posterior.
- Razón: el documento exige no construir todo de una vez y el repositorio inicial no contenía estructura, toolchains ni decisiones implementadas que pudieran verificarse.
- Consecuencias: la foundation quedó preservada antes del código; Sprint 1 convirtió después el repositorio en una aplicación ejecutable.

## D-002 — Identidad, secreto y fuente de verdad

- Fecha: 2026-08-29
- Estado: aceptada por especificación
- Decisión: usar PUUID como identidad interna, mantener la Riot API key solo en API/worker y usar PostgreSQL como fuente de verdad con deduplicación por Riot match ID.
- Razón: los Riot IDs pueden cambiar, la key no debe llegar al navegador y reutilizar partidas reduce coste y consumo de la API.
- Consecuencias: los contratos, entidades, índices, logs y tests deberán preservar estos límites desde Sprint 1.

## D-003 — Continuidad versionada para Codex

- Fecha: 2026-08-29
- Estado: aceptada
- Decisión: usar `AGENTS.md` como instrucciones persistentes, `docs/PROJECT_CONTEXT.md` como contexto vivo, este archivo como registro de decisiones, `.codex/config.toml` para habilitar memoria y `.agents/skills/lol-network-analyzer` como skill de alcance repositorio.
- Razón: replica el resultado útil del patrón de Vigilant con las rutas actuales de Codex y sin copiar memoria global, credenciales o datos específicos de otro proyecto.
- Consecuencias: cada cambio relevante debe mantener estos documentos alineados con la evidencia real del repositorio.

## D-004 — Red deshabilitada por defecto para el agente

- Fecha: 2026-08-29
- Estado: aceptada
- Decisión: mantener `sandbox_workspace_write.network_access = false` en la configuración versionada.
- Razón: instalaciones, llamadas externas y cambios remotos deben ser visibles y autorizados de forma proporcional; la aplicación tampoco debe hacer llamadas accidentales a Riot durante pruebas.
- Consecuencias: restaurar dependencias o consultar servicios desde shell puede requerir aprobación explícita del entorno.

## D-005 — Adoptar .NET 10 LTS y Node 24

- Fecha: 2026-08-30
- Estado: aceptada
- Decisión: implementar API/worker con .NET 10 y frontend con Node 24/Next.js 16, en lugar del .NET 9 provisional de la especificación.
- Razón: evita iniciar el proyecto sobre una versión STS próxima al fin de soporte y usa el baseline vigente del frontend.
- Consecuencias: SDK, paquetes, CI e imágenes fijan esas familias; un cambio mayor exige validación completa.

## D-006 — Ingesta API acotada antes de jobs persistentes

- Fecha: 2026-08-30
- Estado: aceptada para Sprint 2
- Decisión: sincronizar como máximo 20 partidas por petición en Sprint 2; jobs durables, progreso, exclusión concurrente y refresh se implementan en Sprint 6.
- Razón: demuestra reutilización, deduplicación y persistencia sin introducir prematuramente una cola.
- Consecuencias: la ruta síncrona no es el diseño final para historiales grandes.

## D-007 — Superficie privada por 38080

- Fecha: 2026-08-30
- Estado: aceptada
- Decisión: publicar únicamente Next.js por `38080`; API, worker, PostgreSQL y Redis quedan internos. PostgreSQL 18 monta el volumen en `/var/lib/postgresql`.
- Razón: reduce superficie expuesta y conserva la disposición versionada del clúster PostgreSQL 18+.
- Consecuencias: una exposición pública requiere HTTPS y el endurecimiento de Sprint 7.

## D-008 — Flujo PO/Codex en GitHub Project

- Fecha: 2026-08-30
- Estado: aceptada por el PO
- Decisión: usar `Por hacer`, `Listo para Codex`, `En curso`, `Bloqueado` y `Completado`; crear/refinar/priorizar/mover historias no requiere confirmación adicional. Codex puede delegar y siempre revisa la integración.
- Razón: el PO selecciona trabajo moviéndolo a `Listo para Codex` sin fricción administrativa.
- Consecuencias: completar exige evidencia; la autorización no cubre borrado de datos, publicación de V1 o migración de arquitectura no aprobada.

## D-009 — Score de relación heurístico y explicable

- Fecha: 2026-08-30
- Estado: aceptada para Sprint 4
- Decisión: calcular un score determinista acotado a 0–100 con pesos, ventanas y umbrales configurables; persistir además una etiqueta `LOW`, `MEDIUM`, `HIGH` o `VERY_HIGH`.
- Razón: combinar cantidad, recencia, consecutividad y mismo equipo permite ordenar evidencia sin fingir precisión estadística.
- Consecuencias: el score y confidence nunca se describen como probabilidad ni verifican un duo; API/UI deberán exponer los factores observables y las pruebas fijarán los límites inclusivos 25, 50 y 75.

## D-010 — Snapshot global y simétrico de relaciones

- Fecha: 2026-08-30
- Estado: aceptada para Sprint 4
- Decisión: reconstruir todas las parejas canónicas desde PostgreSQL y reemplazar el snapshot completo en una transacción. Para cada pareja, recencia y consecutividad se calculan sobre la unión cronológica de los historiales de ambos jugadores; la evaluación usa la fecha de la partida persistida más reciente.
- Razón: la unión evita depender de cuál UUID quedó como `player_a`, y una fecha derivada de los datos hace que reprocesar el mismo conjunto produzca exactamente el mismo resultado.
- Consecuencias: las consultas se proyectan en lotes configurables y no llaman a Riot; exclusión concurrente y ejecución durable siguen reservadas para Sprint 6.

## D-011 — Clasificación prudente de posibles premades

- Fecha: 2026-08-30
- Estado: aceptada para Sprint 4
- Decisión: clasificar una relación únicamente cuando supera simultáneamente mínimos configurables de partidas juntas, proporción de mismo equipo y confidence; distinguir `possible premade` de `likely premade` y omitir la etiqueta cuando la evidencia es insuficiente.
- Razón: la coincidencia histórica no demuestra intención, amistad ni identidad oculta; umbrales explícitos permiten auditar el resultado y ajustar el MVP sin fingir probabilidad.
- Consecuencias: la etiqueta se deriva al consultar la relación y no se persiste como hecho; contratos y UI deben explicar factores y mantener lenguaje de inferencia.

## D-012 — Alcance de Sprint 8: insights históricos seguros

- Fecha: 2026-08-30
- Estado: planificado
- Decisión: reservar Sprint 8 para historial A–B, evolución temporal, rankings de `most seen`/`best teammate`/`nemesis`, contratos de insights y tarjetas descargables bajo demanda.
- Razón: son extensiones derivadas de datos ya persistidos y no deben desplazar jobs, rate limiting, privacidad ni readiness V1 de Sprints 6–7.
- Consecuencias: toda comparación usa partidas terminadas y orden estable; compartir queda condicionado a decisiones de privacidad y nunca publica automáticamente.

## D-013 — Trayectoria personal a servicio público sostenible

- Fecha: 2026-08-30
- Estado: aceptada como objetivo de producto
- Decisión: desarrollar primero una herramienta privada útil para el propietario y evolucionarla gradualmente hacia un sitio público para muchas personas; evaluar publicidad u otra monetización únicamente después de validar cumplimiento Riot, privacidad, seguridad, capacidad operativa y valor transformativo.
- Razón: permite aprender con un caso real sin prometer publicación, ingresos ni escala antes de tener evidencia y aprobaciones necesarias.
- Consecuencias: Sprints 1–5 priorizan utilidad y exactitud; Sprints 6–7 preparan operación y readiness; cualquier monetización pública requiere una decisión posterior y mantiene un acceso gratuito no invasivo.

## D-014 — Familiaridad histórica sin información futura

- Fecha: 2026-08-30
- Estado: aceptada para Sprint 5
- Decisión: para una partida objetivo, considerar anterior únicamente una partida del owner cuya clave `(occurred_at, riot_match_id)` sea lexicográficamente menor; el denominador contiene jugadores identificables distintos del owner y sin duplicados.
- Razón: el timestamp puede empatar y un orden total estable evita resultados no deterministas o evidencia futura. Los conteos hacen auditable el porcentaje.
- Consecuencias: la familiaridad devuelve conocidos, desconocidos, total evaluable, porcentaje y razón de insuficiencia; no añade win-rate ni un score alternativo y se calcula sin llamadas a Riot.

## D-015 — Grupos como cliques máximos y acotados

- Fecha: 2026-08-30
- Estado: aceptada para Sprint 5
- Decisión: un posible grupo recurrente de 3–5 jugadores exige que todas sus parejas tengan al menos clasificación `possible premade`; solo se emiten cliques máximos canónicos y se permiten cliques máximos solapados.
- Razón: una cadena de relaciones no prueba cohesión del grupo y emitir todos los subgrupos produciría resultados redundantes y trabajo combinatorio innecesario.
- Consecuencias: IDs ordenados, etiqueta no concluyente, clasificación determinada por la pareja más débil y límites configurables de candidatos y combinaciones con fallo explícito al excederlos.

## D-016 — Red ego de profundidad uno y límites aplicados

- Fecha: 2026-08-30
- Estado: aceptada para Sprint 5
- Decisión: el primer contrato de red devuelve únicamente el owner y sus relaciones directas, reutiliza el orden estable de relaciones y aplica máximos configurables de nodos y aristas; la respuesta informa totales disponibles, límites y truncamiento.
- Razón: una red acotada es explicable, permite una tabla equivalente y evita consultas o renderizados sin límite durante el MVP.
- Consecuencias: `minimumConfidence` y `minimumScore` filtran antes de calcular totales; PUUID sigue siendo la identidad pública técnica disponible en este contrato, sin exponer IDs de base ni `raw_data`; ampliar profundidad requiere una historia explícita.

## D-017 — Adopción selectiva de GM-02 y GM-03

- Fecha: 2026-08-30
- Estado: aceptada para planificación
- Decisión: incorporar medición de consultas, caché y resiliencia en Sprint 6; concentrar cumplimiento, privacidad, seguridad, HTTPS, backups e incident response en Sprint 7; evaluar monetización solo después del go/no-go público.
- Razón: las investigaciones contienen recomendaciones útiles junto con cifras y afirmaciones no respaldadas o incompatibles con el orden del producto.
- Consecuencias: no se adopta un SLA de eliminación de 24 horas, remapeo de PUUID por cambio de key, metas arbitrarias de latencia/cobertura ni autenticación por defecto; cualquier decisión temporal sobre Riot se verifica otra vez en fuentes oficiales al implementarse.

## D-018 — Premades visibles como grupos máximos y códigos accesibles

- Fecha: 2026-08-30
- Estado: aceptada para Sprint 5
- Decisión: en el detalle de partida, evaluar relaciones únicamente entre participantes visibles del mismo equipo; mostrar cliques máximos de 3–5 y parejas detectadas solo cuando no estén contenidas en un grupo mayor. Etiquetar siempre como `Posible premade`, añadiendo `evidencia alta` cuando todas las parejas superen el umbral fuerte.
- Razón: los dúos también son útiles, pero repetir cada pareja de un grupo produciría ruido. El historial no demuestra que los jugadores hayan entrado juntos en esa partida.
- Consecuencias: cada grupo recibe un código determinista `P1`, `P2`, etc. y un tono visual; código, etiqueta y lista de integrantes garantizan que el color no sea el único identificador. Los solapamientos legítimos muestran varias insignias en el jugador.

## D-019 — Grafo SVG progresivo sin dependencia de producción

- Fecha: 2026-08-31
- Estado: aceptada para Sprint 5
- Decisión: representar la red ego acotada con SVG nativo, controles HTML y una tabla semántica equivalente en vez de incorporar Cytoscape/D3 durante el MVP.
- Razón: profundidad uno y máximo 50 nodos no justifican todavía el peso, mantenimiento y superficie de una dependencia; la tabla conserva toda la funcionalidad informativa si falla o no se usa el mapa.
- Consecuencias: zoom, pan, selección, filtros y reset se implementan localmente; posiciones, radios y grosores son deterministas y probados. Reevaluar una librería solo si aumenta la profundidad o complejidad del grafo.

## D-020 — Perfil local-first y sincronización explícita

- Fecha: 2026-08-31
- Estado: aceptada para Sprint 5
- Decisión: resolver primero Riot ID desde PostgreSQL y cargar resumen, relaciones e historial sin sincronizar; ACCOUNT-V1 se usa para IDs ausentes y MATCH-V5 solo tras la acción explícita `Actualizar 20 partidas`.
- Razón: navegar entre perfiles debe ser rápido, reutilizar datos y no consumir cuota externa por efecto lateral.
- Consecuencias: el resumen expone la fecha local de actualización; los datos vacíos se muestran como tales y los Riot IDs siguen siendo atributos mutables sobre PUUID. Refresh durable y programado pertenece a Sprint 6.

## D-021 — PostgreSQL es la fuente durable de jobs

- Fecha: 2026-08-31
- Estado: aceptada para Sprint 6
- Decisión: persistir el estado completo de cada análisis en PostgreSQL y hacer que el worker reclame trabajo desde esa fuente. Redis podrá acelerar señales o coordinación, pero nunca será la única copia del job.
- Razón: un reinicio de API, worker o Redis no debe perder solicitudes ni su progreso; además, el endpoint de estado necesita una lectura durable y auditable.
- Consecuencias: `analysis_jobs` conserva estado, progreso, código de error seguro y timestamps; los índices `(status, created_at)` y `(puuid, created_at)` preparan claim y deduplicación. La siguiente entrega de S6-001 implementará transiciones atómicas y recuperación de jobs interrumpidos.

## D-022 — Exclusión durable por solicitud y por match

- Fecha: 2026-08-31
- Estado: aceptada para Sprint 6
- Decisión: impedir jobs activos equivalentes con un índice único parcial por `(puuid, requested_count)` y serializar la escritura concurrente de un mismo `riot_match_id` mediante un advisory lock transaccional PostgreSQL.
- Razón: una comprobación previa en aplicación no cierra carreras entre procesos; tanto jobs duplicados como descargas simultáneas pueden llegar desde varias instancias.
- Consecuencias: solicitudes `Queued`/`Running` equivalentes devuelven el job existente, mientras estados terminales permiten reintento. Dos análisis pueden compartir matches sin duplicarlos ni convertir una colisión esperable en fallo del job.

## D-023 — Carril de investigación Para Gemini

- Fecha: 2026-08-31
- Estado: aceptada por el PO
- Decisión: añadir el estado `Para Gemini` al tablero. Codex deja allí únicamente historias de investigación con prompt y entregable; el PO las envía a Gemini y mueve la tarjeta a `Listo para Codex` al adjuntar la investigación.
- Razón: separa investigación externa de trabajo de implementación y mantiene al PO como quien decide qué resultados entran al carril de Codex.
- Consecuencias: `Para Gemini` no autoriza cambios de código ni cierra historias. Codex revisa fuentes, alcance y compatibilidad antes de incorporar cualquier recomendación.

## D-024 — Rate limiting Riot coordinado por proceso y routing

- Fecha: 2026-08-31
- Estado: aceptada para Sprint 6
- Decisión: todas las llamadas Riot de un proceso pasan por un singleton `IRiotRateLimiter` con concurrencia configurable y cooldown compartido para el routing regional configurado. Un 429 registra `Retry-After`; si falta, el cliente usa backoff exponencial acotado y un número finito de reintentos.
- Razón: Riot exige detener llamadas durante `Retry-After`, mientras un retry HTTP genérico no coordina llamadas concurrentes y puede multiplicar solicitudes. El worker concentra la ingesta masiva y el API limita el lookup puntual.
- Consecuencias: la cancelación interrumpe tanto espera como petición; las pruebas usan HTTP simulado. API y worker aún tienen limitadores separados, por lo que un futuro despliegue con múltiples réplicas deberá introducir coordinación distribuida y revalidar límites oficiales antes de escalar.

## D-025 — Caché derivada con Redis y fallback en memoria

- Fecha: 2026-08-31
- Estado: aceptada para Sprint 6
- Decisión: exponer `ICacheService` con TTL e invalidación por tags; usar Redis como caché compartida y memoria local como fallback. Cachear inicialmente el resumen derivado y representar PUUID en claves/tags mediante SHA-256.
- Razón: Redis reduce lecturas repetidas, pero una dependencia temporal no debe reemplazar PostgreSQL ni hacer que su caída impida consultar datos persistidos. Los tags evitan `SCAN` durante invalidación.
- Consecuencias: completar un job o una sincronización invalida el owner; un miss, payload inválido o fallo Redis vuelve a PostgreSQL. El fallback puede conservar una lectura hasta su TTL si otra instancia pierde Redis simultáneamente, por lo que no se usa para información autoritativa ni permanente.

## D-026 — Refresh programado opt-in mediante jobs durables

- Fecha: 2026-08-31
- Estado: aceptada para Sprint 6
- Decisión: persistir como máximo un schedule por PUUID, desactivado por defecto, con frecuencia de 15 minutos a 7 días y cantidad de 1–200 partidas. El worker reclama una ejecución vencida con `SKIP LOCKED`, avanza `next_run_at` y crea el job mediante la exclusión activa existente.
- Razón: el refresh debe ser explícito, reintentable y seguro ante varios workers; no se permiten timers por usuario en memoria ni solapes que consuman Riot innecesariamente.
- Consecuencias: desactivar no borra historial ni jobs; un schedule no reclama Riot directamente, solo encola un job durable. La frecuencia inicial es fija por schedule y una ampliación a cuotas, ventanas o múltiples regiones requiere una historia posterior.

## D-027 — Observabilidad agregada y segura por proceso

- Fecha: 2026-08-31
- Estado: aceptada para Sprint 6
- Decisión: instrumentar con `System.Diagnostics.Metrics` y un snapshot interno por proceso, usando etiquetas de baja cardinalidad. Los logs HTTP incluyen correlación, método, estado y duración, pero omiten ruta/query e identidades de jugador.
- Razón: las rutas contienen Riot ID o PUUID y no son necesarias para operar el MVP; los instrumentos nativos permiten conectar OpenTelemetry después sin imponer ahora un proveedor o dependencia adicional.
- Consecuencias: API y worker exponen `/metrics` únicamente en su red interna actual. Un despliegue público deberá restringir esa superficie y decidir collector/exporter en S7; nunca se agregan API key, raw payload o identificadores de jugador como labels.

## D-028 — Capturar oportunidades relevantes como backlog trazable

- Fecha: 2026-08-31
- Estado: aceptada por el PO
- Decisión: toda oportunidad material detectada durante código, investigación o revisión se registra como historia futura antes de quedar fuera de la conversación. La historia incluye prioridad, alcance, criterios, dependencias y límites.
- Razón: conservar ideas importantes —como los íconos oficiales— sin colarlas en el sprint activo ni perder su contexto de seguridad, accesibilidad, coste o cumplimiento.
- Consecuencias: la captura no autoriza implementación automática ni reordena el sprint actual; el PO mantiene la prioridad moviendo historias a `Listo para Codex`.

## D-029 — Activos de campeón mediante proxy Data Dragon acotado

- Fecha: 2026-08-31
- Estado: aceptada para S8-007
- Decisión: resolver el ID numérico de campeón contra un catálogo Data Dragon versionado y servir el PNG desde una ruta same-origin del servidor con host fijo, caché, validación de tipo y fallback textual.
- Razón: los nombres no coinciden siempre con el identificador de asset y una llamada directa por fila desde el browser aumenta dependencia, superficie y acoplamiento al CDN.
- Consecuencias: `DATA_DRAGON_VERSION` no es secreto y se actualiza de forma explícita; un fallo externo devuelve 404 y la UI conserva nombre/KDA. El ícono de perfil espera un contrato oficial que aporte ese dato y no añade SUMMONER-V4 silenciosamente.

## D-030 — Beta antes de lanzamiento y monetización

- Fecha: 2026-08-31
- Estado: planificada por solicitud del PO
- Decisión: Sprint 9 validará una beta privada multi-región y Sprint 10 preparará un lanzamiento público sostenible. Ambos dependen del go/no-go de cumplimiento/seguridad; publicidad y premium son experimentos reversibles posteriores, no activaciones ni pagos.
- Razón: el objetivo mundial y económico necesita evidencia de utilidad, capacidad, privacidad y coste antes de ampliar tráfico o monetizar.
- Consecuencias: crear/refinar las historias no autoriza desplegar, activar Azure/anuncios, integrar pagos ni declarar GO. Esas acciones requieren sus criterios y autorización explícita correspondientes.

## D-031 — Congelar inversión en el grafo social actual

- Fecha: 2026-08-31
- Estado: aceptada por el PO
- Decisión: mantener funcionando el grafo de profundidad uno tal como está, pero no invertir más tiempo en ampliarlo durante Sprints 7–8 ni convertirlo en dependencia de nuevas funcionalidades.
- Razón: el PO no encuentra útil o clara la visualización actual; tablas, historial A–B, recurrentes, posibles premades y rankings responden mejor las preguntas prioritarias del producto.
- Consecuencias: no se elimina código, API o datos ahora. `UX-001/#82` lo reevaluará únicamente con feedback o petición posterior, comparando mejorar, simplificar, ocultar o retirar de forma reversible.

## D-032 — PROJECT ECHO prioriza encounters y familiaridad

- Fecha: 2026-09-21
- Estado: aceptada por el PO para ECHO-1
- Decisión: distinguir `known player` con uno o más encuentros de `repeated player` con dos o más; hacer de la familiaridad histórica y sus jugadores conocidos la experiencia principal. Relaciones y posibles premades permanecen como contexto secundario, y el grafo pasa a una sección experimental sin nueva inversión.
- Razón: estas capacidades ya se derivan de partidas persistidas y responden directamente a “¿ya jugué antes con esta persona?” sin competir con productos de estadísticas generales.
- Consecuencias: `/known-players` incluye encuentros únicos, `/encounters` conserva recurrentes, y la UI prioriza matches analizados, conocidos, recurrentes, última partida y familiaridad.

## D-033 — Actualización incremental de relaciones afectadas

- Fecha: 2026-09-21
- Estado: aceptada para ECHO-1
- Decisión: después de ingerir partidas nuevas, recalcular y hacer upsert únicamente de las parejas canónicas afectadas usando las partidas persistidas en PostgreSQL; no borrar ni reconstruir el snapshot global en cada sincronización.
- Razón: el reemplazo global crece con todo el corpus aunque una sincronización agregue pocas partidas.
- Consecuencias: el cálculo mantiene orden determinista, `player_a_id < player_b_id`, idempotencia y exclusión transaccional. Una reconstrucción global queda como operación explícita de reparación, no como ruta ordinaria.

## D-034 — Redis opcional mediante proveedor explícito

- Fecha: 2026-09-21
- Estado: aceptada para ECHO-1
- Decisión: `CACHE_PROVIDER=memory` ejecuta API y worker sin Redis; `CACHE_PROVIDER=redis` conserva caché compartida y fallback local. PostgreSQL permanece como fuente de verdad.
- Razón: Redis no aporta valor imprescindible para validar el MVP en una sola instancia y no debe bloquear readiness.
- Consecuencias: Redis usa un perfil opcional de Compose y solo participa en el health check cuando está seleccionado.

## D-035 — Retención de raw JSON pendiente de decisión

- Fecha: 2026-09-21
- Estado: pendiente del PO
- Decisión: conservar `matches.raw_data` durante ECHO-1 como mecanismo de reprocesamiento, debugging controlado y compatibilidad futura. No fijar todavía una eliminación automática ni un periodo de retención.
- Razón: borrar ahora impediría recalcular campos sin volver a consumir Riot; conservar indefinidamente también requiere una decisión informada de privacidad y coste.
- Consecuencias: S7-002 deberá proponer y obtener aprobación para periodo, alcance de borrado/exportación y procedimiento operativo antes de la beta pública.

## D-036 — Contexto automático y parametrizable para la última partida

- Fecha: 2026-10-17
- Estado: aceptada para la vista de última partida
- Decisión: al cargar el dashboard o el detalle de una partida con owner, la web solicita al backend el historial reciente del owner y de hasta nueve participantes visibles de esa partida. Los conteos por defecto son 5 para el owner y 3 por participante; se centralizan en `latestMatchSyncConfig` y admiten los overrides públicos `NEXT_PUBLIC_ECHO_OWNER_MATCH_COUNT`, `NEXT_PUBLIC_ECHO_OTHER_MATCH_COUNT` y `NEXT_PUBLIC_ECHO_OTHER_PLAYER_COUNT`.
- Razón: la tarjeta de última partida debe calcular familiaridad con evidencia reciente sin que el navegador contacte a Riot ni que los límites queden duplicados en varios componentes.
- Consecuencias: la selección es determinista por orden de equipos/participantes y usa PUUID; las solicitudes pasan por la ruta de sincronización existente, continúan mostrando datos locales si alguna falla y respetan el límite de 20 partidas por llamada. Los overrides se aplican al compilar la web y no contienen secretos. La tarjeta y el detalle muestran, cuando existe evidencia previa, resultados contra el jugador principal (victorias/derrotas).

## D-037 — Evidencia temporal para posibles premades

- Fecha: 2026-10-17
- Estado: aceptada para la familiaridad de la última partida
- Decisión: cuando existe owner, la detección de grupos cruza las relaciones del lobby con hasta `PremadeDetection:RecentOwnerMatchCount` partidas recientes del owner. Una coincidencia como rival dentro de esa ventana descarta la señal de premade para la pareja; coincidencias como aliado en fechas distintas elevan la clasificación prudente, sin convertirla en verificación.
- Razón: repetir equipo en fechas distintas es evidencia más útil que una coincidencia aislada, mientras que alternar entre aliado y rival es compatible con emparejamiento casual.
- Consecuencias: la ventana y el mínimo de fechas distintas son configurables; si no se transporta owner se conserva la detección histórica existente. Los resultados siguen usando `possible premade`/`likely premade` y nunca afirman una relación confirmada.

## D-038 — Estado de partida activa exclusivamente server-side

- Fecha: 2026-10-17
- Estado: aceptada para el dashboard de jugador
- Decisión: consultar desde la API (nunca desde el navegador) el summoner-v4 por PUUID y luego spectator-v5 active-games; devolver solo un resumen mínimo de partida activa: cola, inicio, duración y game ID.
- Razón: Riot expone el estado activo mediante endpoints protegidos y la API key debe permanecer en API/worker. La señal es útil en la pantalla principal, pero no amplía el historial de partidas terminadas.
- Consecuencias: 404 de Riot se representa como `null` y no como error; errores de cuota/autorización se degradan a warning en la UI. El endpoint se vuelve a consultar al buscar/refrescar el dashboard.
## D-039 — Excluir la partida objetivo de la evidencia histórica reciente

- Fecha: 2026-10-18
- Estado: aceptada para la optimización del análisis de partidas
- Decisión: cuando se analiza una partida seleccionada, las ventanas de historial solicitadas para el owner y sus participantes deben devolver hasta `count` partidas adicionales, excluyendo el `riot_match_id` objetivo. La partida objetivo sigue disponible para mostrar sus equipos, pero no cuenta como encuentro histórico previo.
- Razón: incluir la partida seleccionada como evidencia inflaría encuentros aliados/rivales y podría producir una inferencia de premade basada en el mismo evento que se está evaluando.
- Consecuencias: la petición de Match-V5 puede solicitar una partida adicional para compensar la exclusión; la deduplicación y caché se realizan por Match ID, y la familiaridad/premade deben mantener la exclusión incluso si el objetivo ya estaba almacenado localmente.

## D-040 — AnalysisJobs en cola acotada dentro de la API

- Fecha: 2026-10-18
- Estado: aceptada para el runtime InMemory actual
- Decisión: los jobs creados por la API se procesan en el mismo proceso mediante `BackgroundService` y un `Channel<Guid>` acotado. El repositorio InMemory usa mutaciones EF rastreadas y un candado de proceso en vez de operaciones SQL relacionales.
- Razón: el ingestion-worker no está declarado en Compose y usa otro almacén InMemory; no puede compartir los jobs de la API.
- Consecuencias: los jobs y resultados se pierden al reiniciar y la capacidad queda limitada a una instancia. La coordinación compartida queda para una decisión futura de persistencia durable.

## D-041 — Métricas de corpus y cobertura histórica explícita

- Fecha: 2026-10-18
- Estado: aplicada para T03
- Decisión: el resumen describe todas las partidas disponibles del owner en InMemory; cinco es el tamaño inicial de consulta. Únicos se deduplica por PUUID y participaciones por (Match ID, PUUID). Familiaridad y grupos de un lobby usan solo evidencia anterior al objetivo. Las vistas distinguen cobertura parcial, insuficiente y error del cero evaluable.
- Razón: los historiales de participantes se solapan y pueden ampliar el corpus del owner; contar filas, reutilizar agregados globales como evidencia previa o ignorar errores parciales produciría cifras engañosas.
- Consecuencias: se conserva el contrato previo con un contador adicional de participaciones y un estado de roster parcial; el porcentaje definitivo solo se presenta con cobertura evaluable. El GET local no certifica cobertura de Riot. Una ventana aislada por consulta necesitaría un contrato posterior explícito.

## D-042 — Procesamiento stateless por solicitud, sin base de datos

- Fecha: 2026-10-18
- Estado: aceptada para T03.5; desplegada localmente por autorización posterior explícita del PO el 2026-10-18 (Panamá), con evidencia en `docs/PROJECT_CONTEXT.md`
- Decisión: búsqueda y análisis seleccionado obtienen todos sus datos desde Riot o caché opcional y calculan sobre objetos locales. La API no registra EF Core, repositorios, jobs, Channel, BackgroundService ni Redis. Los históricos se limitan a cinco del owner y tres por otro participante, excluyendo el objetivo y partidas posteriores con `endTime` y validación local de fechas.
- Razón: el MVP no necesita conservar historiales ni resultados funcionales entre solicitudes. Un proceso sin datos previos debe poder analizar directamente una partida. La arquitectura anterior exigía sincronización/corpus previo y coordinaba estado que el frontend actual no necesita.
- Consecuencias: reemplaza para el runtime las decisiones previas de persistencia (incluyendo D-021–D-026), D-040 sobre jobs y el corpus global de D-041; conserva sus definiciones estadísticas y degradación de cobertura. EF/Redis/repositorios históricos se compilan exclusivamente en el proyecto Legacy para pruebas y benchmark; el worker queda fuera del runtime/Compose. Los endpoints stateful retirados responden 410 y el frontend usa contratos autocontenidos. No existe progreso persistido ni reanudación de jobs. Caché acotada y single-flight solo optimizan llamadas, nunca constituyen un almacén funcional.
- Límites: presupuesto HTTP configurable (180 s por defecto), proxy Next 210 s y cancelación hasta Riot. Benchmarks simulados no certifican duración bajo cuota real; un presupuesto agotado devuelve 504 y requiere reintento explícito. La cuota agregada entre instancias sigue pendiente de diseño antes de escalar.
- Evidencia y contratos: `docs/T03_5_STATELESS_ARCHITECTURE.md`. No autoriza despliegue, reinicio, cambios de secretos ni merge automático.

## D-043 — Admisión acotada, cuota de conexión y presupuestos locales

- Fecha: 2026-10-18
- Estado: aceptada para T03.6 en rama feature; no desplegada
- Decisión: reutilizar ConcurrencyLimiter en una admisión singleton con FIFO, cola y deadline propios; cuota token-bucket en tabla acotada por IP de conexión; no confiar en XFF del proxy Next sin una frontera que lo sanee. Usar503 para falta de capacidad y429 para cuota, con Retry-After. El presupuesto global comienza antes de cola y queda por debajo del navegador/proxy.
- Razón: controlar memoria y trabajo pendiente ante sobrecarga, evitar particiones de clientes ilimitadas y spoofing de identidad, sin nueva infraestructura ni autenticación.
- Consecuencias: usuarios detrás de Next/NAT comparten cuota hasta diseñar ingress confiable. Se conservan objetos locales y ventanas cinco/tres. El limiter Riot existente conserva un único singleton y añade ventanas preventivas configurables y observación app/método; puede esperar de forma conservadora más de lo mínimo conocido. Las cotas de caché/estructuras no son un techo de RSS. Escalar réplicas requiere resolver cuotas agregadas; no se implementa coordinación distribuida.
- Evidencia y límites: `docs/T03_6_RESOURCE_PROTECTION.md`. Cargas exclusivamente simuladas, aisladas y opt-in; sin despliegue ni reinicio de servicios.
