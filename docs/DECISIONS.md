# Decisiones del proyecto

## D-001 — Foundation documental antes del código

- Fecha: 2026-08-29
- Estado: aceptada
- Decisión: preservar la especificación original y crear primero la documentación, configuración de agente, memoria y roadmap; el código de Sprint 1 será una entrega posterior.
- Razón: el documento exige no construir todo de una vez y el repositorio inicial no contenía estructura, toolchains ni decisiones implementadas que pudieran verificarse.
- Consecuencias: el estado actual es intencionalmente no ejecutable y no se deben afirmar health checks ni servicios existentes.

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
