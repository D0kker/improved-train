# Decisiones del proyecto

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
