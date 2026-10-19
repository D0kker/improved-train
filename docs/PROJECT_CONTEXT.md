# Contexto vivo del proyecto

Última actualización: 2026-10-18

## Propósito

PROJECT ECHO analiza partidas terminadas de League of Legends para mostrar encuentros, familiaridad e inferencias prudentes de posibles premades. PUUID es la identidad interna; Riot ID es mutable. Nunca revelar identidades ocultas ni presentar una inferencia como relación verificada.

## Checkout actual: T03.5 stateless

- Rama de trabajo: `feature/t03-5-stateless-api`, creada desde `feature/echo-product-focus` en `4d4580f`. Incluye T01, T02 y T03; no asumir que `main` los contiene.
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

## Estado operativo separado del código

El servicio existente en 8081 sigue ejecutando T03 (`1f54650`). T03.5 no se ha desplegado y no autoriza reiniciar servicios. Cualquier despliegue futuro requiere instrucción explícita del PO. No se modificaron secretos.

## Validación y publicación

- Se ejecutan pruebas con Riot simulado; jamás consumir cuotas reales en CI.
- Fixtures cubren el análisis directo sin búsqueda previa, dos análisis aislados, cinco/tres por participante, temporalidad, deduplicación, caché vacía/expirada/desactivada, fallos parciales, 429, cancelación y límites de memoria/payload.
- Benchmark opt-in `RUN_STATELESS_BENCHMARK=1`: baseline EF InMemory T03 y flujo stateless, 12 muestras después de calentamiento, con Riot simulado. Sus cifras no representan producción.
- Código publicado en `3afd6c611cabd1ccc0d42333cf6a09e4f36c9e34`; [PR #83](https://github.com/D0kker/improved-train/pull/83) abierto hacia `main`, sin merge ni deploy. Incluye los ancestros de la rama de referencia todavía ausentes de main. El commit posterior solo registra esta evidencia documental. Consultar el informe T03.5 para los resultados finales.

## Próximo paso

Revisar el PR T03.5 y los contratos de migración. Antes de desplegar, evaluar el límite de tiempo del proxy/borde real y la cuota Riot con carga real autorizada. Varias instancias siguen necesitando controlar la cuota agregada de la clave; no se implementó coordinación distribuida.

## Historia y decisiones

El contexto anterior se conserva íntegro en [archivo hasta T03](archive/PROJECT_CONTEXT_THROUGH_T03.md). Especificación y sprints anteriores describen decisiones históricas, no requisitos de persistencia actuales. D-042 reemplaza para el runtime las decisiones previas de almacenamiento/jobs; se mantienen los algoritmos, seguridad y cumplimiento. Publicación abierta y monetización continúan pendientes del go/no-go correspondiente.
