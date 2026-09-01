# Runbook operativo privado

Última verificación: 2026-08-31

Este runbook cubre el stack privado Docker Compose. No autoriza una publicación en Internet.

## Salud y métricas

- `docker compose ps` debe mostrar web, API, worker, PostgreSQL y Redis saludables.
- API y worker exponen internamente `/health`, `/health/ready` y `/metrics`; solo la web publica el puerto `38080`.
- Liveness no consulta Riot. Readiness comprueba PostgreSQL y Redis.
- `/metrics` entrega agregados por proceso y los instrumentos nativos usan el meter `LolAnalyzer.Operations`.
- Los logs HTTP incluyen correlación, método, estado y duración. No incluyen path, query string, PUUID, Riot ID, payload ni `RIOT_API_KEY`.

## Resiliencia verificada

| Escenario | Evidencia | Resultado esperado |
| --- | --- | --- |
| Reinicio de worker | recreación del contenedor con PostgreSQL preservado | vuelve healthy; jobs siguen siendo durables y los leases vencidos son recuperables |
| Solicitudes duplicadas | prueba de carrera y constraint parcial `ux_analysis_jobs_active_request` | un único job activo equivalente |
| Riot 429 | HTTP simulado con `Retry-After`, backoff y cancelación | cooldown compartido y reintentos acotados, sin loop agresivo |
| Redis caído | Redis detenido durante una consulta de resumen local y restaurado después | respuesta desde PostgreSQL/memoria; Redis nunca es fuente de verdad |
| Corpus local | 2,632 encounters y 12,358 relationships | consultas agregadas sin llamadas a Riot |

Las pruebas automáticas nunca requieren una Riot API key ni consumen su cuota.

## Consultas e índices medidos

`EXPLAIN (ANALYZE, BUFFERS)` se ejecutó el 2026-08-31 sobre el corpus local. Los tiempos describen únicamente ese equipo/corpus y no son un SLA.

| Consulta real | Plan elegido | Tiempo observado |
| --- | --- | ---: |
| reclamar siguiente job `queued` | `ix_analysis_jobs_status_created_at` | 0.113 ms |
| reclamar refresh vencido | `ix_player_refresh_schedules_enabled_next_run_at` | 0.142 ms |
| recurrentes por owner y mínimo de encuentros | `ix_player_encounters_owner_player_id_total_matches` | 0.413 ms |
| relaciones donde el jugador ocupa A o B | `BitmapOr` sobre ambos índices jugador/score | 0.539 ms |

Las rutas de lectura usan proyecciones EF Core y un número acotado de consultas por respuesta; no consultan una vez por fila. Cualquier índice adicional exige volver a medir una consulta representativa y considerar coste de escritura/tamaño.

## Fallos

1. Comprobar `docker compose ps` y los dos health checks internos.
2. Revisar logs por `CorrelationId`, código seguro del job y estado HTTP; nunca copiar secretos o payloads.
3. Si Redis falla, conservar PostgreSQL y dejar que la caché degrade. Restaurar Redis y verificar readiness.
4. Si el worker se reinicia, no editar filas manualmente: el lease y la recuperación durable reclaman trabajo interrumpido.
5. Si una key Riot se expone, revocarla en el Developer Portal, retirar el valor de runtime, revisar logs/artefactos y no reanudar ingesta hasta rotarla.
