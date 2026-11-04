# Contexto vivo del proyecto

Última actualización: 2026-10-31

## T04.6 — análisis de partida para revisión

La rama `feature/t04-6-match-analysis-implementation`, basada en `main` `16b00eb343c5f43496fb55571084e119b84d6d49`, implementa resumen, equipos, pares históricos, grupos prudentes y cobertura con los contratos stateless existentes; no cambia backend, cuotas ni dashboard. Validación local: 81 pruebas frontend, lint, TypeScript, build, audit de producción (0), Compose y diff-check aprobados. Chromium con fixtures recorrió cinco anchos y estados parciales, insuficientes, error/reintento y textos largos; Axe sin infracciones detectadas y contraste incompleto pendiente de evaluación manual. Capturas: [índice T04.6](releases/t04-6-captures/README.md). CI #66 pasó en el SHA funcional `2c0c4ff`; el PR #103 está abierto. Estado `READY_FOR_REVIEW`; falta aprobación visual del PO y evaluación manual de contraste/tecnologías asistivas. No se fusionó ni desplegó.

## T04.11 — refinamiento UX y clasificación (rama de revisión)

Desde main `22d1152`, rama `feature/t04-11-dashboard-ux-ranked`, se añade consulta League-V4 por PUUID independiente, caché temporal de cinco minutos y degradación segura. Perfil compacto, recurrentes con miniestadísticas y partidas compartidas legibles, KDA visible en historial sin expansión redundante. Fórmulas, métricas y campeones aprobados permanecen. [Informe T04.11](T04_11_DASHBOARD_UX_RANKED.md). Validación local:166 pruebas.NET y68 frontend aprobadas, builds/formato/lint/tipos/Compose; audit producción cero. Capturas y axe en cinco anchos sin infracciones detectadas (contraste incompleto/manual pendiente). Un aviso high transitivo de desarrollo en Zod queda en TODO. Sin merge ni despliegue. Las secciones inferiores conservan el contexto histórico previo a la integración de RELEASE-04.

## T04.10.3 — refinamiento visual pendiente de aprobación

Sobre PR #97 se optimiza cabecera, fecha/región, grilla de siete métricas y campeones horizontales, conservando lógica y contratos. [Informe](T04_10_3_DASHBOARD_VISUAL_REFINEMENT.md), [comparación visual](releases/release04-captures/t04-10-3/README.md) y [publicidad futura documental](T04_10_3_AD_PLACEMENT_PROPOSAL.md). 59 pruebas frontend, lint/tipos/build y audit producción cero; validación Chromium con fixtures en cinco anchos. Se corrigió una cifra dividida en tablet y se añadió comprobación de una línea para valores. READY_FOR_REVIEW, PR Draft; aprobación visual del PO y contraste/asistiva pendientes. La imagen web b551f43 es anterior al refinamiento y deberá reconstruirse antes de un futuro despliegue autorizado. Sin cambios de backend, main, secretos ni servicios.

## T04.10.2 — candidata lista para revisión del PO

Se realizó validación real Chromium/Playwright aislada con datos/assets sintéticos en 320/390/768/1024/1440, sin Riot ni cambios de producción. Cabecera, rendimiento 5→10, relaciones, filtros, expansiones, cobertura y reintentos comprobados; 16 capturas en [índice visual](releases/release04-captures/README.md). Pruebas repetidas: 130 .NET (3 opt-in omitidas), 55 frontend, builds, formato, lint, tipos y audit producción cero. Axe sin infracciones detectadas, con contraste incompleto pendiente de revisión manual. [Plan actualizado](releases/ECHO_RELEASE_04_PLAN.md): READY_FOR_REVIEW; #97 sigue Draft esperando aprobación visual del PO. Sin correcciones funcionales demostrables, merge a main ni despliegue. Las notas anteriores de preparación se conservan debajo como historial.

## RELEASE-04 — candidata integrada, revisión visual pendiente

PR #98 se fusionó únicamente en T04.10 mediante `b551f4312b63af66717de6b6271ec2c190154729`; PR #97 sigue abierto/Draft hacia main. Se repitieron pruebas combinadas (130 .NET y 55 frontend) y una matriz DOM adicional de cuatro combinaciones API/web. Despliegue recomendado API primero por contrato aditivo. Plan y evidencias: [RELEASE-04](releases/ECHO_RELEASE_04_PLAN.md). Estado REVIEW_REQUIRED: no hay navegador disponible y la revisión visual del PO sigue pendiente. No se hizo merge a main, despliegue ni reinicio de servicios. La información anterior de T04.10/T04.10.1 que sigue abajo conserva su contexto histórico de implementación.

## T04.10.1 — contratos aditivos de dashboard

Rama hija `feature/t04-10-1-dashboard-data-contracts`, desde PR #97 `d010b98`, aún abierto/draft. Match-V5 ya disponible alimenta evidencia por fila y el ícono histórico del owner; no agrega llamadas Riot API. Relaciones deduplicadas por Match ID/PUUID con enlaces compartidos reales. Detalle, compatibilidad y pruebas en [T04.10.1](T04_10_1_DASHBOARD_DATA_CONTRACTS.md). No hay merge ni cambios de servicios/secrets.

## T04.10 — implementación para revisión

Rama `feature/t04-10-competitive-dashboard`, base `main` `2c67fe5`. El PO aprobó cambiar el rendimiento del dashboard a todas las partidas válidas cargadas. No cambia el resumen del backend ni el análisis profundo. Relaciones por ventanas disjuntas disponibles; asociación jugador→Match IDs ausente, pendiente de contrato futuro. Ver [informe](T04_10_COMPETITIVE_DASHBOARD.md). Validaciones: 49/49 pruebas, lint, tipos, build y audit de producción aprobados. Límites del contrato documentados; no se declara completada la navegación a partidas compartidas. Sin cambios de runtime; el registro inferior es histórico de la base.

## Estado vigente — RELEASE-02 / T04.5

- `main` parte de RELEASE-02: PR #93 se integró mediante merge `1a8ae443`; API y web están desplegadas con smoke/readiness aprobados. El informe y los digests están en [RELEASE-02](releases/ECHO_RELEASE_02.md).
- PR #94 (`docs/release02-deployment-record` → `main`) es un seguimiento documental todavía abierto en esta revisión de main; sus cambios no pertenecen al código base de T04.5.
- T04.5 trabaja en `feature/t04-5-player-dashboard`, creada desde `origin/main` `1a8ae443678b13f3e56c31e388df3624860ab051`, en `/tmp/improved-train-t04-5`.
- El dashboard presenta identidad/región, resumen inicial de hasta cinco partidas, estado independiente de partida activa, cobertura e historial. `loadMoreMatches` y los contratos no cambian. Hallazgos, validaciones y límites visuales: [T04.5](T04_5_PLAYER_DASHBOARD.md).
- Estado al inicio del sprint: Node 24.18.0/npm 11.16.0, Next 16.4.2. No se modificó backend, dependencias, secrets ni runtime; no hubo llamadas a Riot ni despliegue.

El registro detallado bajo esta sección conserva notas de sprints anteriores; cuando discrepe con el estado vigente de arriba o los informes de sprint, esos pasajes son históricos.

## RELEASE-02 — integración aislada

- Rama `release/echo-v2`, worktree `/tmp/improved-train-release-echo-v2`, creada desde `origin/main` `6b5f088`. Commit de integración `0db73581f4c2c3bb7f7cf028866273cc5eab9f98`; integra el merge T03.6 ya revisado en `release/echo-staging` (#87), manteniendo T04.1–T04.4 de `main`.
- No hubo conflictos de código; se resolvieron `docs/PROJECT_CONTEXT.md` y `docs/TODO.md` preservando la continuidad más reciente. Evidencia, validaciones, riesgos y rollback: [RELEASE-02](releases/ECHO_RELEASE_02.md).
- Restore/build/test/format .NET en SDK10 ARM64 pasaron; 126 tests aprobados, 3 opt-in omitidos. Frontend: 40 tests, lint, types y build pasaron; audit producción cero, audit completo un high dev-only de Zod.
- Imágenes ARM64 compilaron y un smoke API/web aislado pasó sin Riot. El run de GitHub Actions `40465130308` pasó `web`, `dotnet` y `containers` (build de Compose en runner Ubuntu x64) para el HEAD `3924478`; se publica una actualización documental y se confirmarán los checks sobre su HEAD final.
- [PR #93](https://github.com/D0kker/improved-train/pull/93) está abierto, limpio y no fusionado; estado listo para revisión. Merge/despliegue esperan autorización explícita.
- El stack existente permanece intacto: API T03.5 y web T04.4 healthy (sin mounts, puerto web 8081); Redis está detenido y su volumen persistente se conservó. No se fusionó a `main` ni se desplegó.
- PR #89 permanece abierto como draft; RELEASE-02 contiene su trabajo útil y T04.3/T04.4 más recientes. Recomendar revisar su cierre como sustituido; no cerrarlo automáticamente.

## Continuidad actual T04.3

- T04.3 parte de `feature/t04-2-ux-corrections` (`2eb547e5c677b2f77baf99d6fc92134d2df82c39`) en un worktree aislado; su rama es `feature/t04-3-design-system`.
- Se añadieron tokens semánticos, componentes presentacionales y galería `/design-system`, sin modificar contratos, llamadas de API ni distribución funcional de las cuatro rutas. La galería queda oculta en producción salvo `ENABLE_DESIGN_SYSTEM_GALLERY=true` en runtime.
- Ver detalle y validación en [T04.3](T04_3_DESIGN_SYSTEM.md). La prueba visual en navegador permanece pendiente porque no hay browser de pruebas disponible. El triage npm dejó 5 avisos high únicamente en el árbol de lint; Next 16 se actualizó a 16.4.2, sin degradaciones ni `--force`.
- Rama publicada en `origin/feature/t04-3-design-system`; commit inicial `60ede464a07afeaaeae076964eac2ed819a61bba`; [PR #88](https://github.com/D0kker/improved-train/pull/88) fue integrado en T04.2. La rama T04.3 conserva head `a7cec40b77b9f98ffdd86adb4e3248c2fc7183f8`, elegido como base de T04.4.

## Continuidad actual T04.4

- Worktree aislado: `/tmp/improved-train-t04-4`, rama `feature/t04-4-home-redesign`, base exacta `origin/feature/t04-3-design-system` (`a7cec40b77b9f98ffdd86adb4e3248c2fc7183f8`). No se modificaron checkout T03.6 ni `release/echo-staging`.
- La Home usa variante Search First, `SiteHeader`, `Container`, `Card` y `Button` existentes. Conserva búsqueda/configuración de región; los textos de Home y formulario son configurables por props. Evidencia, pruebas y límite de validación visual: [T04.4](T04_4_HOME_REDESIGN.md).
- Build Next 16.4.2 y 35 pruebas DOM aprobaron; `npm audit --omit=dev` reportó cero vulnerabilidades. El audit completo encontró un high de desarrollo en Zod transitivo del plugin ESLint; no se cambió el lockfile. No hubo navegador para capturas/validación visual. T04.4 no incluye contratos ni cambios de backend.
- Rama `origin/feature/t04-4-home-redesign`, HEAD previo al merge `cedf6212957f7ebbff5e7dd85debfaadaa6c5ad6`; [PR #90](https://github.com/D0kker/improved-train/pull/90) integrado a `main` el 2026-10-25. El merge incorpora T04.1–T04.4 (frontend y documentación), sin API/worker ni T03.6. La PR #89 continúa draft e independiente.

## Despliegue vigente

- El despliegue se construyó desde el merge de PR #90, commit `8f27b1f61130263ac6586a5edd3591422328d632` (HEAD de `main` en ese momento). PR #91 añadió solo documentación; la comparación de `apps/web` entre ambos HEADs fue idéntica, así que la imagen desplegada corresponde al código web vigente.
- Imagen desplegada: `lol-network-analyzer-web`, digest `sha256:66f3e18a13d09bd1e34dc0f7325a00239b2577e0a7c2902ae9e2d66a04817b17`; contenedor healthy desde `2026-10-25T23:41:20Z`, publicado en `8081`.
- Smoke checks locales: `GET /` devolvió 200 y contenía “Cada partida tiene una historia”; `GET /api/health` devolvió 200 con `healthy`.
- API no se recreó: sigue healthy con digest `sha256:2eb828149a7afe2a43b07eeef6903bc334597581c32c0bac65b439124d2e62dc` y arranque `2026-10-19T00:29:47Z`. No se hicieron llamadas a Riot.
- Compose reportó `lol-network-analyzer-redis-1` como huérfano. Se dejó intacto; no se retiraron recursos existentes.

## Propósito

PROJECT ECHO analiza partidas terminadas de League of Legends para mostrar encuentros, familiaridad e inferencias prudentes de posibles premades. PUUID es la identidad interna; Riot ID es mutable. Nunca revelar identidades ocultas ni presentar una inferencia como relación verificada.

## Checkout actual: RELEASE-01 publicado en staging (sin desplegar)

- Rama de integración: `release/echo-staging`, worktree aislado `/tmp/improved-train-release-echo-staging`; merge T03.6 `5f7838a`, snapshot de código `e7b2ca5`, con documentación de release posterior. Cuatro conflictos frontend resueltos; no quedan conflictos. Informe: [RELEASE-01](releases/ECHO_STAGING_01.md).
- Refs verificadas 2026-10-19: T03.5=`eed0129`; T04.2 avanzó a `002cbb4` por merge posterior T04.3, explícitamente excluido de staging. PR #86 y #87 están merged; PR #89 está OPEN/Draft hacia `main`.
- CI del SHA de staging validado más reciente `60558d6` aprobó `web`, `dotnet` y `containers`. Staging está lista para pruebas controladas, pero no se desplegó ni se hizo merge a `main`.
- Audit npm detectó 1 crítica y 2 altas en Next/sharp/source-map-js. El análisis no halló uso de los vectores concretos revisados; actualizar y re-auditar antes de exposición pública.

- Rama histórica de T03.6: `feature/t03-6-resource-protection`; PR #86 se integró en T03.5 (`eed0129`) y después en staging mediante PR #87/merge `5f7838a`. La base incluye T01/T02/T03/T03.5.
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
- Auditoría, validaciones, perfil CPU, 40 escenarios de carga aislada y límites de T03.6: [protección de recursos](T03_6_RESOURCE_PROTECTION.md). Build Release pasó sin warnings/errores; 105 unitarias y 21 integración aprobadas, formato y perfil opt-in aprobados. T03.6 se integró vía PR #86 y luego en staging mediante #87. Run RELEASE-01 `39210831898` pasó `web`, `dotnet` y `containers`; el PR de revisión #89 está Draft.

## Estado operativo separado del código

RELEASE-01 no verificó el estado actual de contenedores por la restricción de acceso al socket Docker; `3afd6c6` es la última versión documentada, no confirmación del runtime actual.

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
