# RELEASE-05 — Despliegue de T04.11

Estado: **DEPLOYED_WITH_WARNINGS**.

## Autorización y revisión desplegada

El PO confirmó aprobación visual de T04.11 y autorizó expresamente actualizar API y web. PR #100 fusionado; SHA exacto desplegado: **`16b00eb343c5f43496fb55571084e119b84d6d49`**. `origin/main` y la referencia remota se comprobaron antes de construir y nuevamente antes de recrear servicios; no existían cambios posteriores inesperados. El árbol de este merge es idéntico al de T04.11 `d82960d`.

[CI main63](https://github.com/D0kker/improved-train/actions/runs/41000981512) aprobada sobre este SHA: web, .NET y contenedores. La publicación documental posterior no cambia el SHA del runtime.

Inicio de la actualización: `2026-10-30T04:38:42.923293+00:00` UTC. Cierre de checks iniciales: `2026-10-30T04:39:00.090294+00:00` UTC. Host Linux ARM64/aarch64. Estos timestamps no certifican disponibilidad continua durante la recreación de una instancia única.

## Inventario y preparación

Proyecto Compose `lol-network-analyzer`; red conservada `lol-network-analyzer_default`; web 8081→3000, API 8080 interna, sin mounts en ambos. Se verificaron API/web healthy y Home 200 y validación de proxy con respuesta 400 esperada antes de empezar. Se preparó un worktree aislado desde main; no se modificó el checkout raíz ni ramas de otras sesiones.

El contenedor Redis histórico de RELEASE-04 **ya no existía en el inventario inicial de esta tarea**. El volumen `lol-network-analyzer_redis_data` sí existía y se conservó con metadata idéntica. No se creó, arrancó, eliminó ni recreó Redis; tampoco se montaron o modificaron datos de ese volumen. Este informe no atribuye la desaparición previa del contenedor.

Había aproximadamente 5.1 GiB libres al inicio. Se retiraron solo node_modules/.next regenerables creados por esta validación, después de finalizar sus comandos; ninguna imagen/volumen/cacheDocker compartida se eliminó. Sin `prune`, `down` ni `remove-orphans`.

## Imágenes y contenedores

### API

- Imagen anterior: `lol-network-analyzer-api`.
- ID anterior: `sha256:4c1b686364b45cb7ecd4a5d24d7d648f234cf11b66a4df6b3800ef73d740650d`.
- Contenedor anterior: `67f909941bcaa7044408d3f58572426056a78ea8a9ae79045c4e129da7bbf784`.
- Imagen nueva: `project-echo-release05-api:16b00eb`.
- ID de contenido/configuración nuevo: `sha256:998e9bd6cf82c12658f9e0ea05ac7e13395350fc6cc1d18a495694f80fa2c47d`.
- RepoDigests: `[]`.
- Contenedor nuevo: `c0b84717dbf92ab060a92bcb930d261161445f4d2f33a6a611639598bfaa5ab4`; estado `healthy`.
- Inicio nuevo: `2026-10-30T04:38:44.646581102Z`.
- Imagen construida: `2026-10-29T23:28:37.186226668-05:00`, `arm64`, 131607173 bytes; etiqueta OCI revision igual al SHA aprobado.

### WEB

- Imagen anterior: `lol-network-analyzer-web`.
- ID anterior: `sha256:f42f1bc29ca2ae6159fee977a2b335c8c735abdb54159e9bf5aa9612aa3bdd04`.
- Contenedor anterior: `c8805918b39dac2de01177f0b05a4e3cfa6fd7c4078f4dbbbcf9bdb21e0ad642`.
- Imagen nueva: `project-echo-release05-web:16b00eb`.
- ID de contenido/configuración nuevo: `sha256:241a8fe565c5f0255698d5b8ab2ab08eda5f14273b20fe894baf8a7a450ce2f7`.
- RepoDigests: `[]`.
- Contenedor nuevo: `84503e64b2727266181982344152efbf6361fbabdc6839b43cbc45ce8b648e3c`; estado `healthy`.
- Inicio nuevo: `2026-10-30T04:38:52.450474861Z`.
- Imagen construida: `2026-10-29T23:31:48.951229592-05:00`, `arm64`, 220917460 bytes; etiqueta OCI revision igual al SHA aprobado.


Los RepoDigests vacíos significan imágenes locales sin manifiesto publicado en registry. Los SHA256 anteriores son IDs reales de contenido/configuración Docker; no se presentan como digests de un manifiesto remoto. Se mantienen tags e IDs de RELEASE-04 para rollback.

## Configuración y compatibilidad

Backup privado0700, excluido de Git y del contexto Docker: `/home/dokker/improved-train/backups/releases/release05-16b00eb/`. Contiene inventario sanitizado, Compose anterior/candidato, overrides de imágenes y rollback, resultados y logs de validación. Los snapshots efectivos tienen valores de entorno redactados; no se copió ni modificó la clave o archivo de secretos.

Se compararon en memoria los valores efectivos con runtime antes y después: configuración y clave iguales. Se mantuvieron región, routing, budgets, concurrencia, cuota, caché, puerto, red y restart policy. `ASPNETCORE_ENVIRONMENT=Development` es la configuración preexistente y permanece; hardening corresponde a otra tarea.

T04.11 añade únicamente un endpoint secundario de rango. API nueva mantiene contratos de web anterior; web nueva con API anterior degrada a «Rango no disponible». Se actualizó **API primero**, esperó health satisfactorio y comprobó proxy con web anterior; después se actualizó web y verificó su salud. TTL clasificatorio300s y presupuesto5s usan valores predeterminados del código, sin añadir variables ni cambiar límites Riot.

La construcción utilizó Dockerfiles existentes, plataforma linux/arm64, tags por commit y etiqueta OCI revision. API desde el worktree aprobado con `.dockerignore`; web desde archivo Git limpio del mismo SHA, sin secretos u outputs locales. Se refrescaron bases del mismo major mediante `--pull`; no se modificaron Dockerfiles, lockfiles ni código funcional.

## Validaciones previas

- SDK .NET 10.0.402 ARM64 aislado: restore/buildRelease aprobados (0 warnings/errores); **166 pruebas aprobadas**,0 fallidas,3 benchmarks opt-in omitidos; formatverify aprobado.
- League-V4 simulado: errores404/429/5xx no se convierten en sin clasificación; timeout, caché/TTL, single-flight, cancelación y logging seguro comprobados.
- Host Node24.18.0/npm11.16.0: npmci, **68 pruebas frontend**, lint, type-check y build de producción aprobados.
- Audit producción: **0 vulnerabilidades**. El aviso high transitivo de desarrollo en Zod ya documentado permanece como backlog; no se ejecutó auditfix ni actualización funcional.
- Composeconfig y gitdiffcheck aprobados. Builds de ambas imágenes finales aprobados.
- Arranque candidato con API en red Docker interna exclusiva sin egress, clave ficticia y web con red de ingreso de laboratorio publicada solo en loopback3217: API/web healthy, Home/dashboard200, galería404, emblemas200, inputs inválidos400 a través del proxy y OpenAPI con endpoint/schema de rango. Sin llamadas Riot reales.
- Revisión navegador de la imagen candidata con interceptación de APIs/assets: rango Solo/Flex y estados sin clasificación/no disponible; perfil, relaciones y compartidas, historialKDA, filtros, carga5→10, cobertura y reintento, partida activa y errores. Cinco anchos320/390/768/1024/1440 y sin overflow. Datos de demostración, sin cuota Riot. Evidencia JSON sanitizada en backup; [capturas T04.11 aprobadas por PO](t04-11-captures/README.md).

## Despliegue y verificaciones posteriores

Se adquirió un bloqueo exclusivo de despliegue y se comprobaron IDs previos, SHA remoto, configuración e imágenes esperadas. Compose usó proyecto/env-file explícitos y archivos guardados: `up -d --no-deps --no-build --wait --wait-timeout180`, un servicio a la vez. No se reconstruyó durante la recreación ni se actuó sobre servicios ajenos.

| Comprobación | Resultado |
|---|---|
| API/web | Healthy; imágenes ARM64 esperadas y etiqueta SHA aprobada |
| Home | HTTP200 y contenido de PROJECT ECHO |
| `/api/health` | HTTP200; health de la web, por sí solo no prueba el proxy |
| Proxy web→API | recent-matches con start=-1 y ranked con plataforma inválida devuelven400 de API, sin Riot |
| Contrato rango | OpenAPI expone endpoint y RiotRankedProfile; estados/transformación validados con Riot simulado |
| Dashboard | ShellHTTP200; interacciones verificadas con browser/fixtures sobre artefacto candidato |
| Emblemas | Oro/plata disponibles desde mismo origen |
| Galería | Oculta por defecto en producción(404) |
| Logs nuevos | Sin coincidencias de clave configurada, identificador sintético o errores críticos al corte |
| Redis | Contenedor ausente antes/después; volumen y metadata conservados |
| Configuración | Variables efectivas, puerto, red y mounts iguales |

La validación posterior con Chromium sobre la web desplegada pasó en 390 y 1440 px: colas Solo/Flex, KDA visible, partidas compartidas, filtros y carga5→10; escenarios adicionales sin clasificación, rango no disponible y partida activa. Cinco controles completos, cero errores y sin overflow. Todos esos datos fueron fixtures interceptados; no equivalen a una consulta real de Riot. `/api/health` no se describe erróneamente como readiness de la API.

## Recreación externa detectada y preservada

El inventario inicial observó API `1f2c8a5351c35400a530e1265586aa1ef2ab45bebcf4ae4e695f0aa013018a0e` y web `db41782be87e301645dff8bed8fb568fc4032f6c5c3e02cd957f7331d5bd6b8e`. Durante los builds fueron recreadas fuera de esta ejecución a las04:21–04:22UTC, manteniendo exactamente las mismas imágenes de RELEASE-04. La guardia de IDs abortó el primer intento **antes de modificar servicios**. Se inspeccionó la deriva: servicios healthy, sin otro despliegue activo; API coincide con configuración vigente y web ya no incluye RIOT_PLATFORM_REGION explícita (su fallback sigue siendo la1).

Se conservó el inventario inicial y se capturó una nueva línea base. El Compose candidato/rollback se tomó de la configuración runtime vigente para preservar ese cambio ajeno, en vez de reinsertar variables de la plantilla main. Se comprobó igualdad efectiva con runtime, incluida la clave en memoria sin mostrarla; no se editó el archivo de secretos. Las imágenes sí proceden exclusivamente del SHA aprobado de main. La tabla de contenedores anteriores refleja esta última línea base validada. No se atribuye el origen o motivo de la recreación externa.

## Incidencia de laboratorio

El primer intento de publicar la web de prueba directamente en una red internal produjo connectionrefused desde el host, aunque API/web estaban healthy. Se corrigió únicamente el arnés creando una red de ingreso exclusiva para la web, conservando API sin salida externa. Se repitieron todas las comprobaciones; no fue un fallo del código ni afectó producción.

## Advertencias y alcance real

No se recibió una cuenta conocida de prueba para una consulta funcional mínima a Riot; se solicitó durante la preparación. No se consumió cuota real como parte de la validación. **Rango Solo/Dúo/Flex real, clasificación actual de una cuenta, búsqueda real, historial/relaciones reales y partida activa real quedan sin verificar por esta ejecución.** Las pruebas con mocks y HTTP200 del shell no certifican esos flujos end-to-end.

Se conserva el aviso de desarrollo Zod y la configuración Development de API. Contraste manual/asistiva mantienen límites documentados en T04.11; no se declara certificación WCAG. El PO sí autorizó visualmente este release. No se introdujeron cambios funcionales adicionales ni ajustes de Riot.

## Rollback disponible

No ejecutado (`rollback_executed=False`). Las imágenes de RELEASE-04 indicadas arriba siguen presentes y no se eliminaron. Archivos `compose.before.yml` y `rollback.yml` del backup fijan IDs, no un tag latest mutable.

Ante fallo crítico: usar el mismo proyecto y env-file runtime con esos dos archivos; restaurar **web anterior y después API anterior**, un servicio por vez con no-deps/no-build/wait; verificar ambos healthy, `/`200, `/api/health`200 y respuesta 400 esperada del proxy para parámetros inválidos. Confirmar imágenes, puerto8081 y metadata del volumen Redis; restaurar los aliases locales latest a los IDs de RELEASE-04 tras un rollback exitoso. No ejecutar down, remove-orphans o limpieza de imágenes/volúmenes. La compatibilidad aditiva permite rollback exclusivo de web si API nueva permanece sana.

La automatización tenía recuperación ante fallo de Compose, salud, proxy, deriva de configuración o logs críticos. No se ensayó un rollback sobre producción porque no fallaron esos checks. La disponibilidad futura de esas imágenes debe verificarse antes de una recuperación posterior.

## Publicación documental

Rama documental separada desde el SHA desplegado; PR hacia main sin merge automático. Solo informe, contexto y TODO. El SHA de documentación no sustituye al desplegado.

## Cierre operativo

- Actualización API→web entre 2026-10-30 04:38:42 y 04:39:00UTC (2026-10-29 23:38:42–23:39:00Panamá).
- Runtime API .NET/ASP.NET 10.0.14; web Node 24.22.2. Imagen API sin dependencia EF/Redis funcional, según pruebas de artefacto existentes.
- Los aliases locales `lol-network-analyzer-api:latest` y `lol-network-analyzer-web:latest` se alinearon con las imágenes RELEASE-05 después de healthchecks; los tags RELEASE-04 y sus IDs se conservaron.
- Laboratorio retirado por IDs/labels propios: solo dos contenedores y dos redes temporales; sin eliminar imágenes ni volúmenes. Backups y evidencias sanitizadas conservados.
- Advertencia operativa: el checkout raíz permanece en su rama anterior. No construir desde ese checkout antiguo ni usar un tag ambiguo para un futuro release; usar el SHA aprobado y los overrides versionados por operación.
- No se fusionó ningún PR documental ni se publicaron cambios funcionales adicionales.

Comprobación final2026-10-30 04:42:02UTC: API/web siguen healthy con los IDs de imagen esperados; métricas agregadas de la nueva API:0 solicitudes Riot y0 rate limits. Son contadores al corte, no una garantía de comportamiento futuro. Quedaban aproximadamente2.2GiB libres.
