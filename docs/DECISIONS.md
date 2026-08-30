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
