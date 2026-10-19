# Contexto vivo del proyecto

Última actualización: 2026-10-19

## Propósito

PROJECT ECHO analiza partidas terminadas de League of Legends para mostrar encuentros, familiaridad e inferencias prudentes de posibles premades. PUUID es la identidad interna; Riot ID es mutable. Nunca revelar identidades ocultas ni presentar una inferencia como relación verificada.

## Checkout actual: RELEASE-01 en integración local (sin desplegar)

- Rama candidata: `release/echo-staging`, worktree aislado `/tmp/improved-train-release-echo-staging`, base remota inicial `2eb547e` (T04.2). Merge explícito de `origin/feature/t03-6-resource-protection` (`217ec8e`) resuelto con commit local `5f7838a`; cuatro conflictos frontend, sin entradas de conflicto pendientes. Commit no publicado porque falta validación .NET/CI integrada. Informe: [RELEASE-01](releases/ECHO_STAGING_01.md).
- Refs frescas 2026-10-19: `feature/t03-5-stateless-api` avanzó a `eed0129`, que integra T03.6; el árbol coincide con `217ec8e`. `feature/t04-2-ux-corrections` avanzó a `002cbb4` por merge de T04.3, explícitamente excluida de staging. PR #86 aparece integrado por historial Git. El último estado consultable de #87 es open/draft/conflicting; el cliente GitHub falló en esta revisión y requiere verificación posterior. No se creó PR a main ni se cerró #87.
- `release/echo-staging` contiene T03.5/T04.1/T04.2 y localmente T03.6; se preservaron ambas implementaciones y se excluyó T04.3. Staging no está listo: `dotnet` no está instalado, el socket Docker denegó acceso, `npm ci` falló por ENOSPC; frontend test (33), lint, type-check, build, Compose config y diff-check sí pasaron con dependencias disponibles.
- Audit de producción detectó 1 crítica y 2 altas en Next/sharp/source-map-js. No se cambiaron dependencias; no desplegar hasta actualizar/revisar y repetir el audit.

- Rama histórica de T03.6: `feature/t03-6-resource-protection`; PR #86 fue integrado en `feature/t03-5-stateless-api` en `eed0129`. La base incluye T01/T02/T03/T03.5.
- T03.6 conserva los dos flujos y sus ventanas. Añade una admisión FIFO única (8 activos/16 cola), espera de cola5s, cuota por IP de conexión20 tokens/20por minuto y máximo1024 clientes; detrás de Next los usuarios comparten la IP/cuota del proxy. No se confía en XFF ni Forwarded.
- El mismo limiter singleton Riot incorpora ventanas configurables y headers app/método, además de concurrencia3, Retry-After y backoff. Estado solo local al proceso, sin coordinación entre réplicas. Las cuotas del proveedor simulado se elevan solo en benchmarks aislados, no en runtime.
- Métricas ampliadas: admisión, rechazos, espera, cancelación, stages, caché, CPU/working set/GC. Caché conserva cotas4096/64MiB estimados, evita array UTF-8 explícito al estimar y single-flight revalida caché tras un miss tardío. No se cachean resultados funcionales.
- Timeout API validado5–180s incluyendo cola/retries; navegador200s y Next210s. Acciones manuales, paginación y navegación propagan cancelación y no hacen retry automático.
- Frontend Next.js 16/React 19/TypeScript; API ASP.NET Core .NET 10. Clean Architecture ligera con calculadores puros reutilizados de T03.
- El runtime de la API ya no registra DbContext, repositorios, Redis, AnalysisJobs, Channel ni BackgroundService. No requiere base de datos.
- Búsqueda: `GET /api/v1/players/by-riot-id/{gameName}/{tagLine}/analysis` resuelve Riot ID y devuelve cinco partidas, resumen y cobertura en una respuesta.
- Partida seleccionada: `POST /api/v1/matches/{matchId}/analyze`, con owner PUUID y plataforma opcional, obtiene el objetivo independientemente de la búsqueda; consulta cinco históricas del owner y tres de cada otro participante, con filtro temporal `endTime` y comprobación final de fechas.
- La respuesta mantiene la asociación participante/historial; deduplica Match ID y calcula únicamente sobre la unión elegible de esa solicitud. Objetivo y futuro quedan fuera de la evidencia.
- Caché opcional de respuestas Riot: IDs 2 minutos, detalles 60 minutos, identidades 30 minutos; 4096 entradas y presupuesto estimado de 64 MiB. No se guardan resultados funcionales de análisis ni se exige una caché precargada. `Cache:Enabled=false` permite desactivarla.
- Single-flight comparte llamadas pendientes; cancelar un consumidor no cancela a los restantes. Al desaparecer todos se cancela la operación compartida. Un único rate limiter por proceso conserva concurrencia, Retry-After y backoff acotado.
- Budget HTTP de análisis 180 s, proxy Next.js 210 s, petición Riot 10 s, máximo 1 MiB por respuesta Riot, petición propia 64 KiB. Los timeouts y la cobertura incompleta se muestran explícitamente.
- El frontend ya no encadena sync/summary/known-players. Búsqueda y análisis usan sus respuestas autocontenidas; historial adicional se consulta en bloques de cinco. No hay rediseño visual.
- Los endpoints de jobs, schedules, sync y red global retornan 410 con instrucciones de migración. Se conservaron aliases de lecturas que pueden obtener sus datos sin estado previo.
- `apps/api/tests/LolAnalyzer.Legacy` compila los repositorios/EF/Redis históricos exclusivamente para regresiones y benchmarks. El worker antiguo sigue como código de referencia, fuera de Compose y de la cadena de dependencias de la API; no debe activarse como parte del runtime stateless.
- El artefacto API se prueba expresamente para excluir dependencias EF Core y StackExchange.Redis.
- Detalles, contratos, pruebas, métricas reproducibles, riesgos y límites: [T03.5](T03_5_STATELESS_ARCHITECTURE.md).
- Auditoría, validaciones, perfil CPU, 40 escenarios de carga aislada y límites de T03.6: [protección de recursos](T03_6_RESOURCE_PROTECTION.md). Build Release pasó sin warnings/errores; 105 unitarias y21 integración aprobadas, formato y perfil opt-in aprobados. El test de cuota quedó determinista. La base del PR avanzó a `56502f1`; se resolvió en T03.6 mediante el merge `1fa7150`, solo con cambios documentales de continuidad. CI push/PR pasó `web`, `dotnet` y `containers`; GitHub informa PR #86 `CLEAN`/`MERGEABLE`. Sin merge del PR ni despliegue. El runtime de abajo sigue en T03.5.

## Estado operativo separado del código

El PO autorizó explícitamente desplegar T03.5 después de la entrega sin despliegue. El 2026-10-18 a las 19:29 (Panamá; 2026-10-19 00:29 UTC) se construyeron y recrearon únicamente API/web desde `f14f328` (código `3afd6c6`), manteniendo el puerto 8081 y la misma clave Riot de runtime. No hubo merge del PR ni cambios de secretos.

- API: imagen `sha256:2eb828149a7afe2a43b07eeef6903bc334597581c32c0bac65b439124d2e62dc`, arranque `2026-10-19T00:29:47.350757351Z`, healthy.
- Web: imagen `sha256:471b29ea96b01f618d73f6d0612b186e4cc7f83b23cfbf3cfd85493e726396e7`, arranque `2026-10-19T00:29:53.71342093Z`, healthy; imágenes en ejecución coinciden con las construidas.
- `/` 200, `/api/health` 200/healthy y readiness de API saludable. Proxy frontend→API confirmado: endpoint retirado de jobs 410 y POST analyze con owner ausente 400, sin llamadas a Riot.
- El artefacto en ejecución no contiene dependencias EF Core ni StackExchange.Redis. Compose advirtió de un contenedor Redis heredado huérfano; no se eliminó ni se tocaron recursos ajenos a API/web.
- Comando: `WEB_PORT=8081 docker compose build api web` seguido de `WEB_PORT=8081 docker compose up -d --no-build --wait --wait-timeout 180 api web`; configuración validada previamente sin imprimir secretos.
- Estas comprobaciones no certifican validez actual de la clave ante Riot ni un análisis completo con datos reales; no se consumió cuota en los smoke tests.

## Validación y publicación

- Se ejecutan pruebas con Riot simulado; jamás consumir cuotas reales en CI.
- Fixtures cubren el análisis directo sin búsqueda previa, dos análisis aislados, cinco/tres por participante, temporalidad, deduplicación, caché vacía/expirada/desactivada, fallos parciales, 429, cancelación y límites de memoria/payload.
- Benchmark opt-in `RUN_STATELESS_BENCHMARK=1`: baseline EF InMemory T03 y flujo stateless, 12 muestras después de calentamiento, con Riot simulado. Sus cifras no representan producción.
- Código publicado en `3afd6c611cabd1ccc0d42333cf6a09e4f36c9e34`; [PR #83](https://github.com/D0kker/improved-train/pull/83) abierto hacia `main`, sin merge. La entrega inicial no desplegó; el despliegue posterior autorizado se registra arriba. Los checks web/dotnet/containers de `f14f328` terminaron en success antes de desplegar. Incluye los ancestros de la rama de referencia todavía ausentes de main. Consultar el informe T03.5 para los resultados finales.

## Próximo paso

### T04.1 — auditoría UX/UI documental

- Auditoría independiente desde T03.5 `41ac5ee` en `feature/t04-1-ux-audit`, mediante worktree aislado; no incorpora T03.6 ni modifica su sesión.
- Informe: [T04_1_UX_UI_AUDIT.md](T04_1_UX_UI_AUDIT.md). Inventaría cuatro páginas reales, distingue código no montado y propuestas futuras, y prioriza copy stateless, cobertura, paginación, navegación y validación responsive/accesible.
- Incluye dirección Cyber Arcade, preparación es/en/pt-BR y propuesta de feedback sin implementarlas. Son recomendaciones pendientes de selección del PO, no decisiones arquitectónicas aceptadas.
- Evidencia estática: no hubo navegador disponible, pruebas visuales ni llamadas a Riot. Solo cambia documentación; no despliega ni reinicia servicios. La siguiente implementación debe elegirse por pantalla y coordinar gestión de solicitudes con T03.6 cuando corresponda.
- Publicación de la auditoría mediante PR de `feature/t04-1-ux-audit` hacia `feature/t03-5-stateless-api`; no hacia main. El estado operativo de T03.5 registrado arriba no cambia.

Revisar el PR T03.5 y validar los flujos reales de jugador/partida mediante uso autorizado. El proxy Next local conserva 210 s frente al presupuesto API de 180 s; no se ha hecho prueba de carga contra Riot ni validado otro borde/hosting. Varias instancias siguen necesitando controlar la cuota agregada de la clave; no se implementó coordinación distribuida. Cualquier siguiente despliegue requiere nueva instrucción explícita del PO.

## Historia y decisiones

El contexto anterior se conserva íntegro en [archivo hasta T03](archive/PROJECT_CONTEXT_THROUGH_T03.md). Especificación y sprints anteriores describen decisiones históricas, no requisitos de persistencia actuales. D-042 reemplaza para el runtime las decisiones previas de almacenamiento/jobs; se mantienen los algoritmos, seguridad y cumplimiento. Publicación abierta y monetización continúan pendientes del go/no-go correspondiente.
