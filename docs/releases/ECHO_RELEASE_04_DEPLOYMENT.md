# RELEASE-04 — registro de despliegue

Estado: **DEPLOYED_WITH_WARNINGS**. API y web healthy; advertencias por límites de validación descritos abajo. No se ejecutó rollback.

## Revisión y autorización

Despliegue autorizado explícitamente por el PO. `origin/main` verificado antes de construir y antes de actualizar: **`22d115233738a1722c1ad6942426d0b6c4fcf1c0`**, merge de PR #97. No había commits posteriores. El árbol de aplicaciones, Dockerfiles y Compose coincide con el HEAD de revisión `57656bd`; incluye T04.10, T04.10.1 y T04.10.3. No se fusionó otro PR ni se añadieron cambios funcionales.

CI del merge exacto: [40825954095](https://github.com/D0kker/improved-train/actions/runs/40825954095), aprobada. Worktree aislado desde main: `/tmp/improved-train-release04-deploy`, rama documental `docs/release04-deployment`. El checkout raíz permanece en su rama previa; no utilizarlo para reconstruir código sin comprobar su revisión.

Fecha: **2026-10-29**, Panamá. Inicio `2026-10-29T06:15:49.259898+00:00` UTC, fin de verificaciones iniciales `2026-10-29T06:16:06.092283+00:00` UTC (01:15:49–01:16:06 Panamá). Los timestamps no representan una medición continua de disponibilidad.

## Inventario anterior y preservación

Proyecto Compose real `lol-network-analyzer`, configuración anterior `/home/dokker/improved-train/docker-compose.yml`, red `lol-network-analyzer_default`, host Linux ARM64/aarch64. API y web estaban healthy; Home y proxy devolvían 200. API sin puerto publicado; web 8081→3000. No tenían mounts.

### API

- Contenedor anterior: `0aeea556e0575a14396f60d50f3a8ed9ba0767676e8069c8f718bdee0d1e4276`.
- Imagen anterior: `sha256:3676f4ef2a49c311eed5646d90d891de5ae3b17d09424e9f5d59be17b8a50ac0`.
- Nueva imagen: `project-echo-release04-api:22d1152`.
- **ID/digest local nuevo:** `sha256:4c1b686364b45cb7ecd4a5d24d7d648f234cf11b66a4df6b3800ef73d740650d`.
- Contenedor nuevo: `1f2c8a5351c35400a530e1265586aa1ef2ab45bebcf4ae4e695f0aa013018a0e`.
- Inicio nuevo: `2026-10-29T06:15:50.355966361Z`; health **healthy**.
- Plataforma `linux/arm64`; etiqueta OCI `org.opencontainers.image.revision=22d115233738a1722c1ad6942426d0b6c4fcf1c0`.
- Tamaño 131591117 bytes. Runtime: {"DOTNET_VERSION": "10.0.14", "ASPNET_VERSION": "10.0.14"}.

### WEB

- Contenedor anterior: `0dfbb8bcee76a36b7364bfc05b2cb1f94b1afa6d307b3f141c7c7bf766db8d23`.
- Imagen anterior: `sha256:f141e5dec8f4bbf4e6a5f0158ccd47f50a3ab5779edb1a47ebf1266a34e19ce5`.
- Nueva imagen: `project-echo-release04-web:22d1152`.
- **ID/digest local nuevo:** `sha256:f42f1bc29ca2ae6159fee977a2b335c8c735abdb54159e9bf5aa9612aa3bdd04`.
- Contenedor nuevo: `db41782be87e301645dff8bed8fb568fc4032f6c5c3e02cd957f7331d5bd6b8e`.
- Inicio nuevo: `2026-10-29T06:15:58.163972596Z`; health **healthy**.
- Plataforma `linux/arm64`; etiqueta OCI `org.opencontainers.image.revision=22d115233738a1722c1ad6942426d0b6c4fcf1c0`.
- Tamaño 215774003 bytes. Runtime: {"NODE_VERSION": "24.22.2"}.

Las imágenes son locales y `RepoDigests` está vacío; los SHA256 anteriores son IDs de contenido/configuración Docker, no digests de un manifiesto publicado en registry. No se publicaron imágenes externas. La API reutilizó capas válidas de código idéntico: su timestamp de metadata de imagen es anterior al merge; la procedencia se verificó por árbol fuente, build y etiqueta de revisión, no por ese timestamp.

Redis heredado conserva contenedor `c606fd91491e6faa880ac83731f847ab2ee3522509971dc2b87fd9a939b69135`, imagen `sha256:59c08762bdbcf53fa132aa0bae464a57a1309dc3fff8a034bb3e16d7a0b30ec5`, estado exited y el volumen `lol-network-analyzer_redis_data`. No se arrancó, recreó ni retiró. Compose advirtió que es huérfano; no se utilizó remove-orphans ni down.

## Configuración, backup y orden

Referencias persistentes locales en `/home/dokker/improved-train/backups/releases/release04-22d1152/`, directorio 0700, excluido de Git y del contexto Docker. Contiene inventario anterior, Compose previo/candidato, configuración efectiva con valores de entorno omitidos, overrides de imágenes/rollback y resultados sanitizados. No contiene copia de credenciales ni de `.env`; el secreto sigue en su archivo runtime existente, sin modificarlo.

La configuración API candidata coincidió con la efectiva anterior, incluida igualdad de clave comprobada en memoria. Se conservaron límites, budgets, región, routing, caché y `ASPNETCORE_ENVIRONMENT=Development` existente. En web se añadió explícitamente `RIOT_PLATFORM_REGION=la1`; antes usaba el mismo valor por defecto. Se mantuvieron API_BASE_URL, versión Data Dragon, puerto y red. No se hizo hardening ajeno al release.

Compatibilidad: contrato aditivo; web anterior/API nueva y web nueva/API anterior tienen fallback documentado en el [plan](ECHO_RELEASE_04_PLAN.md). Se actualizó **API primero**, esperó healthy y comprobó proxy desde web anterior; después **web** y sus health checks. Solo se recrearon esos dos servicios.

Overrides `images.yml` fijan los tags por commit y pull_policy never. Se utilizó Compose con proyecto explícito, env-file runtime, Compose candidato copiado y override, `up -d --no-deps --no-build --wait --wait-timeout 180`, un servicio a la vez. Los aliases locales `lol-network-analyzer-api:latest` y `lol-network-analyzer-web:latest` se actualizaron tras los checks, conservando tags de rollback.

## Construcción y validación previa

- API: Dockerfile `infrastructure/docker/api.Dockerfile`, contexto del worktree aprobado, plataforma linux/arm64 y etiqueta OCI del SHA.
- Web: Dockerfile `apps/web/Dockerfile`, contexto apps/web del mismo SHA, plataforma linux/arm64. Build Next de producción completo; no se reutilizó la web anterior a T04.10.3.
- SDK de pruebas .NET 10.0.402 ARM64 aislado: restore/build Release (0 warnings/errores), **130 pruebas aprobadas**, 3 opt-in omitidas, format verify aprobado.
- Host Node 24.18.0/npm 11.16.0: npm ci, **59/59 pruebas frontend**, lint y type-check aprobados. Build de producción verificado dentro de la imagen candidata. Audit producción: **0 vulnerabilidades**. No se modificó lockfile.
- Compose config y diff-check aprobados. CI main también aprobó web, dotnet y containers.
- Arranque candidato en red Docker internal exclusiva, sin host ports y con clave dummy: ambas healthy, Home/ruta dashboard/proxy 200, galería 404, asset inválido 404, ventana inválida 400. No hubo Riot real. Artefacto API sin DLLs de EF Core/StackExchange.Redis.
- Los dos contenedores de prueba y su red se retiraron por ID/label; las imágenes quedaron disponibles. No se eliminaron imágenes, volúmenes ni cachés compartidas. No se ejecutó prune.

## Comprobaciones posteriores

| Comprobación | Resultado |
|---|---|
| API y web | Healthy, imágenes esperadas, etiqueta SHA exacta, linux/arm64 |
| Puerto y red | 8081→3000 y misma red Compose; API interna |
| Home | HTTP 200 y titular «Cada partida tiene una historia» |
| Proxy web→API | `/api/health` 200 antes y después de actualizar web |
| Ruta dashboard sintética | HTTP 200, identidad/region y estructura `competitive-profile-main` presentes |
| Validación API por proxy | recent-matches con start=-1 devuelve 400 esperado, sin consulta Riot |
| Galería/asset inválido | `/design-system` 404; profile-icons/invalid 404 |
| Logs desde actualización | Cero coincidencias de fail/crit/unhandled/fatal en ambos servicios al corte inicial |
| Secretos | Misma clave en API, comprobada sin mostrarla; clave ausente de web |
| Redis | Mismo ID, imagen, estado y mounts |

La búsqueda, rendimiento 5→10, cabecera 60/40, ícono/fallback, top campeones, relaciones/enlaces compartidos, filtros, paginación, cobertura y partida activa se respaldan con las pruebas de componentes/integración y la revisión Chromium previa sobre código idéntico. [Capturas y revisión previa del refinamiento](../T04_10_3_DASHBOARD_VISUAL_REFINEMENT.md). **El smoke HTTP del dashboard verifica su shell, no un análisis Riot completo ni todas sus interacciones en producción.** No se presentó una respuesta con fixtures como búsqueda real.

## Advertencias y límites

1. No se lanzó una búsqueda/análisis real de Riot como prueba de esta tarea; no se proporcionó una identidad de prueba controlada. La validación de contratos y transformación usa HTTP Riot simulado. No se afirma verificación end-to-end real de cada función.
2. OpenAPI devuelve 200, pero los endpoints con resultados IResult no describen los DTO de análisis en su esquema: no aparecen profileIconId, profileIconObservedAt ni evidence. No se alteró backend para ampliar documentación; esos campos se validan mediante pruebas de integración, no por inspección del OpenAPI ni por una muestra real de producción.
3. Se mantiene Development en API porque era la configuración efectiva existente; su endurecimiento corresponde a otra tarea. Contraste manual/asistiva y revisión de assets reales conservan los límites documentados en T04.10.3. No se declara cumplimiento WCAG completo.
4. El disco llegó a ~98% durante builds; tras finalizar y retirar outputs regenerables propios quedaron ~3.1 GiB libres (~89% ocupado). Comprobar capacidad antes de nuevas construcciones; no se borraron imágenes para conseguir espacio.

Métricas agregadas observadas después de actualizar (tráfico de aplicación no atribuido a estos smokes): 57 solicitudes Riot, 0 rate limits, 0 reintentos, 0 timeouts y 0 rechazos. Caché estimada 17126254 bytes. No se guardaron identidades ni respuestas de jugadores. Estos contadores no sustituyen una validación funcional propia ni permiten comparar crecimiento con un baseline no medido.

## Rollback disponible, no ejecutado

Tags retenidos:

- `project-echo-release04-rollback-api:3676f4ef2a49` → ID API anterior indicado arriba.
- `project-echo-release04-rollback-web:f141e5dec8f4` → ID web anterior indicado arriba.
- También se conservaron `project-echo-release02-api:arm64`, `project-echo-release03-web:2c67fe5` y las candidatas históricas.

Restauración autorizada ante fallo crítico: usar el mismo proyecto/env-file runtime con `compose.before.yml` y `rollback.yml` del backup. Restaurar web anterior primero y luego API anterior (se puede conservar API nueva healthy en un rollback solo de web). Para cada servicio utilizar no-deps/no-build/wait, comprobar salud, `/` y `/api/health`, y restaurar aliases latest a los IDs anteriores. Nunca restaurar por nombre ambiguo ni ejecutar build desde el checkout raíz antiguo. Redis y sus volúmenes no participan.

El ejecutor incluía guardias de SHA de main/IDs previos, bloqueo de despliegue y restauración de la combinación anterior si fallaba Compose/health/proxy. Todas las comprobaciones pasaron al primer intento; **no se ejecutó rollback** ni se ensayó una recuperación sobre producción.

## Estado final y publicación

**DEPLOYED_WITH_WARNINGS**: servicios sanos y artefactos exactos verificados, con límites funcionales/OpenAPI anteriores explícitos. Esta rama y su PR publican exclusivamente documentación; no deben fusionarse automáticamente. El SHA de la rama documental no sustituye el SHA desplegado de main.
