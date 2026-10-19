# RELEASE-01 — Integración PROJECT ECHO en staging

Fecha de revisión: 2026-10-19. Este informe describe `release/echo-staging`; **no implica merge a `main` ni despliegue**.

## 1. Resumen ejecutivo

La rama de staging partía de `feature/t04-2-ux-corrections` (`2eb547e`) con T03.5/T04.1/T04.2 previos a T04.3. Se integró T03.6 en worktree aislado con merge explícito, resolviendo cuatro conflictos frontend. El merge commit es `5f7838a`; la documentación final está en `e7b2ca5`. T03.6 también quedó integrado en T03.5 (`eed0129`), con árbol idéntico al head T03.6. T04.3 no forma parte de staging.

CI de GitHub aprobó el SHA integrado `e7b2ca50f828935a70aa871dcff3588edaa877cf`: 33 pruebas frontend, lint, type-check, build Next; restore/build/test/format .NET; `docker compose config` y build de imágenes API/web. El run fue `39210831898`. En .NET pasaron 105 unitarias y 21 de integración; se omitieron 2 y 1 respectivamente. La validación local no pudo ejecutar el SDK ni `docker build` por restricciones del entorno; `npm ci` local terminó en `ENOSPC`, mientras que CI ejecutó `npm ci` correctamente. Los bloqueos locales quedan suplidos por CI sobre el SHA exacto.

El audit de dependencias de producción informó 1 vulnerabilidad crítica y 2 altas. La revisión de fuentes no encontró uso de los vectores `next/og`/`ImageResponse` ni `images.remotePatterns` a los que aplican los avisos críticos revisados. Se acepta para un despliegue de prueba privado y controlado, pero el riesgo no queda eliminado: actualizar Next/sharp/source-map-js y repetir el audit es condición previa a exposición pública.

**Estado: READY para despliegue de prueba controlado (no ejecutado).** La rama está publicada y el CI del SHA exacto está verde. El PR #89 permanece Draft; no se hizo merge a `main`. La auditoría de dependencias limita esta decisión a un entorno privado de pruebas: antes de producción pública hay que corregir y auditar los avisos npm. No se reinició ni desplegó ningún servicio.

## 2. Estado de PR y ramas al 2026-10-19

| PR | Estado consultado | Base ← compare | Resultado relevante |
|---|---|---|---|
| #83 | Merged | `main` ← `feature/t03-5-stateless-api` | T03.5 integrado en main; checks `web`, `dotnet`, `containers` exitosos en el commit consultado. |
| #84 | Merged | `feature/t03-5-stateless-api` ← `feature/t04-1-ux-audit` | Auditoría T04.1 documental integrada en la rama T03.5. |
| #85 | Merged | `feature/t04-1-ux-audit` ← `feature/t04-2-ux-corrections` | T04.2 integrado en T04.1. |
| #86 | Merged | `feature/t03-5-stateless-api` ← `feature/t03-6-resource-protection` | T03.6 integrado; árbol igual al head `217ec8e`. |
| #87 | Merged | `release/echo-staging` ← `feature/t03-6-resource-protection` | Merge commit `5f7838a`; integrado al publicar el merge commit en la rama base. |
| #88 | Merged | `feature/t04-2-ux-corrections` ← `feature/t04-3-design-system` | Posterior al SHA de staging; **T04.3 está excluida** de RELEASE-01. |
| #89 | Open, Draft, clean | `main` ← `release/echo-staging` | Revisión del release. Checks completos exitosos sobre `e7b2ca5`; no hacer merge automáticamente. |

Refs finales: `origin/feature/t03-5-stateless-api`=`eed0129`, `origin/feature/t04-1-ux-audit`=`537daf9`, `origin/feature/t04-2-ux-corrections`=`002cbb4`, `origin/release/echo-staging`=`e7b2ca5`, `origin/feature/t03-6-resource-protection`=`217ec8e`. El commit T04.3 (`60ede46`, `a7cec40`, `002cbb4`) no está en staging. PR #89 es `OPEN/DRAFT/CLEAN`; PR #87 quedó merged con el push de la integración.

Los checks verdes del árbol integrado son el run `39210831898` sobre `e7b2ca5`: `web`, `dotnet`, `containers` todos `SUCCESS`. El workflow ejecutó el set de pruebas integrado y build de contenedores; no se utilizaron claves Riot.

## 3. Historial y commits candidatos

Staging conserva su línea de base en `2eb547e` y la etiqueta de respaldo `backup/release-echo-staging-20261019`. El merge conserva el historial T03.6: `f7428c6` (protección), `323ac1c` (CI), `56502f1`, `20ffa33`, `1fa7150` y `217ec8e`. El merge `5f7838a` y el commit documental `e7b2ca5` están publicados.

No se incorporaron commits de T04.3. No se modificaron las ramas fuente ni `main`.

## 4. Conflictos y resolución

Git reportó conflictos de contenido en:

| Archivo | Resolución aplicada |
|---|---|
| `apps/web/src/api.ts` | Se conservó el mapeo de errores por endpoint de T04.2 y se integró el timeout de fetch de 200 s de T03.6, con cancelación combinada, sin reintentos automáticos y limpieza del timer. |
| `apps/web/app/player/[gameName]/[tagLine]/player-dashboard.tsx` | Se conservó T04.2: región visible, cobertura, reintento de ventana parcial y paginación por bloques de cinco con deduplicación. Se mantuvieron los AbortController separados para búsqueda e historial, que cancelan solicitudes supersedidas/unmount y previenen actualizaciones obsoletas; no se reemplazaron por una vida de solicitud única que pudiera afectar las dos vías concurrentes. |
| `apps/web/app/match/[matchId]/match-detail-view.tsx` | Se conservaron región, cobertura, navegación de regreso y estados T04.2. Se añadió `useRequestLifetime` de T03.6; cada carga supersede la anterior y el cleanup del hook cancela al desmontar. |
| `apps/web/app/match/live/live-match-view.tsx` | Se conservó consulta independiente de partida activa, región, estado sin partida, datos anteriores y refresco de T04.2. Se integró `useRequestLifetime` con el AbortController local y ruta con región; no se acopló la partida activa a la carga del historial. |

`docker-compose.yml`, `apps/web/tests/frontend.test.ts`, `docs/TODO.md` y la documentación/contexto se combinaron automáticamente y se revisaron en el diff integrado. El merge no reintroduce EF Core, jobs durables ni persistencia.

## 5. Funcionalidades presentes en el árbol candidato

- T01/T03: búsqueda y resumen inicial de cinco partidas, estadísticas, familiaridad, relaciones y cobertura parcial.
- T03.5: API stateless; búsqueda y análisis de partida independientes; cinco partidas del owner y tres por cada otro participante; objetivo y partidas futuras excluidos de evidencia; deduplicación; caché opcional.
- T03.6: admisión concurrente, cola acotada, límites de Riot, single-flight, Retry-After, cancelación, timeout global, métricas y límites de memoria documentados.
- T04.1: auditoría UX/UI como documentación heredada.
- T04.2: región visible, cobertura/errores, paginación por ventanas de cinco, accesibilidad y consulta independiente de partida activa.
- T02: el procesamiento durable fue sustituido por T03.5; no se reactivan AnalysisJobs.

No se cambió la ventana 5/3 ni los algoritmos de relaciones/familiaridad/premades.

## 6. Contratos y flujo frontend/backend

No se modificaron contratos públicos en esta integración. `fetchJson` combina la cancelación del caller con el timeout de navegador de 200 s, mantiene errores seguros por estado/ruta y no reintenta. El proxy Next conserva 210 s, por encima del presupuesto API de 180 s. La admisión y los límites Riot son por proceso; no hay coordinación entre réplicas. Los rechazos controlados 429/503 y el timeout global 504 siguen descritos en [T03.6](../T03_6_RESOURCE_PROTECTION.md).

## 7. Validación realizada

| Comando / control | Resultado |
|---|---|
| `npm ci` | Falló localmente por `ENOSPC`; CI lo ejecutó satisfactoriamente en el SHA release. |
| `npm test` | CI: 33 aprobadas, 0 fallidas. El entorno local también pasó usando las dependencias ya instaladas de T04.2. |
| `npm run lint` / `npm run type-check` / `npm run build` | Pasaron localmente y en CI con Next.js 16.3.3. |
| `npm run format:check` | Informó diferencias de estilo en 15 archivos, incluidos varios que no forman parte del diff de integración. No se hizo reformat masivo porque no es requisito de esta release y produciría cambios ajenos al alcance. |
| `dotnet restore/build/test/format` | CI: todos pasaron. 105 unitarias + 21 integración aprobadas; 2 unitarias y 1 integración skipped; 0 fallos. Localmente `dotnet` no está instalado. |
| Docker Compose config y build API/web | CI: ambos pasaron. Runner GitHub Linux x64; no certifica por sí solo imagen ARM64. Socket Docker local denegado (`permission denied`), no se alteraron permisos. |
| `docker compose config --quiet` | Pasó localmente y en CI, sin imprimir valores de configuración. |
| Riot API | No se consultó. |
| `git diff --check` | Pasó en el diff de la rama publicada. |

Los checks obligatorios de backend, frontend y contenedores pasaron sobre el candidato integrado. Los tests 503/504 y de resiliencia están incluidos en el set de 21 integraciones/105 unitarias, pero el CI no reporta por separado sus nombres en el resumen. No se ejecutó prueba visual manual ni smoke contra el servicio activo.

## 8. Seguridad de dependencias

`npm audit --omit=dev --json` al 2026-10-19 reportó 3 vulnerabilidades en el árbol de producción: 1 crítica y 2 altas. La app declara Next `16.3.3`; el audit informa como fix disponible Next `16.4.2`. Los avisos de Next incluyen RCE en `next/og` (GHSA-vcvr-r3jv-pc5j) y otros problemas hasta `16.3.8`; `sharp` aparece en `0.35.4` (< parche `0.35.5`) y `source-map-js` en `1.2.1` (< parche `1.2.2`). Ver [advisory Next RCE](https://github.com/advisories/GHSA-vcvr-r3jv-pc5j), [advisory Next SSRF](https://github.com/advisories/GHSA-cjq9-62q9-8jv4), [advisory sharp](https://github.com/advisories/GHSA-wq5f-xc86-pv6w) y [advisory source-map-js](https://github.com/advisories/GHSA-68fv-2mgg-jv7q).

La revisión estática de este árbol no encontró `next/og`/`ImageResponse`, Cache Components ni `images.remotePatterns`. El componente `ChampionIcon` usa `next/image` con `unoptimized`; las imágenes provienen de un proxy con identificador numérico y URLs Data Dragon fijas. Esto reduce la aplicabilidad de los avisos concretos de ImageResponse/optimizer en el código inspeccionado, pero **no elimina el riesgo de mantener dependencias en rangos afectados** ni sustituye actualizar y volver a escanear. `sharp` y `source-map-js` son transitivos presentes en la instalación de producción; no se afirmó explotabilidad directa desde una ruta pública.

No se ejecutó `npm audit fix` ni se actualizó ninguna dependencia. Recomendación antes del despliegue público: actualizar Next al menos a la versión corregida compatible más baja (`16.3.8`) o a una versión parcheada aprobada, actualizar `sharp` y `source-map-js` transitivos mediante una actualización lockfile revisada, y repetir audit, pruebas y build. El hallazgo de Next es un bloqueo de go/no-go de despliegue, no una modificación silenciosa dentro de RELEASE-01.

## 9. Limitaciones y riesgos pendientes

- El host local no tiene SDK .NET ni acceso al socket Docker; las validaciones completas se obtuvieron en GitHub Actions sobre el SHA exacto.
- `npm ci` local terminó por falta de espacio; CI ejecutó `npm ci` satisfactoriamente. No se borraron caches, worktrees, imágenes, volúmenes ni servicios existentes.
- Formato Prettier reporta 15 archivos; varias diferencias son preexistentes y no se corrigieron masivamente.
- Auditoría npm de producción encuentra dependencias con advisories; se requiere parche/re-audit antes de despliegue público.
- GitHub Actions advierte que `actions/checkout@v4` y `actions/setup-node@v4` apuntan a runtime Node 20 en retirada (forzado a Node 24); tratar como mantenimiento de CI separado.
- Build Docker validado en Linux x64; ARM64 y runtime del host destino siguen pendientes.
- Rate limits y estado single-flight son locales por instancia. La cuota por IP detrás del proxy puede compartirse.
- Paginación puede quedar detenida si un detalle falla permanentemente; se conserva la cobertura y limitación documentadas en T04.2.
- No hubo test visual en navegador ni llamadas a Riot API.

## 10. Configuración y procedimiento para despliegue controlado (no ejecutado)

Commit candidato: `e7b2ca50f828935a70aa871dcff3588edaa877cf`; API y web se construyen desde esa revisión. Compose genera imágenes `lol-network-analyzer-api` y `lol-network-analyzer-web` (tags locales implícitos; no hay digest/tag inmutable publicado). CI validó build Linux x64; validar ARM64 por separado antes de promover arquitectura multi-arch.

Variables de runtime: `RIOT_API_KEY` (secreto requerido para llamadas reales; no incluirlo en Git ni imprimirlo), `RIOT_PLATFORM_REGION` (default `la1`), `RIOT_REGIONAL_ROUTING` (default `americas`), `WEB_PORT` (usar `8081` para el entorno local autorizado), `DATA_DRAGON_VERSION`; límites/TTL tienen defaults en `docker-compose.yml`. API escucha `8080` solo en red Compose; web publica `3000` en `${WEB_PORT}` y proxya a `http://api:8080`. Compose solo define API y web, sin base de datos.

Health checks: API `/health/ready` dentro del contenedor en `8080`; web `/api/health` en `3000`. La web espera readiness de API. Build/compose se validaron en CI, pero el despliegue/estado de contenedores activos no fue inspeccionado ni modificado en esta tarea. La última referencia documental del runtime es T03.5 (`3afd6c6`); verificar el SHA e imágenes reales en el host antes de planificar el cambio.

Cuando se autorice el despliegue: registrar `docker compose ps`, SHA/imagen actual y health checks sin mostrar `.env`; hacer backup de la configuración no secreta, construir `WEB_PORT=8081 docker compose build api web`, revisar `docker compose config --quiet`, recrear únicamente `api web` con espera por salud, validar `/`, `/api/health` y acceso proxy de forma sintética sin llamar a Riot. Abortar si readiness no llega a healthy, API/web no corresponden al SHA previsto, hay errores de proxy/health, o se observa impacto en servicios ajenos.

Rollback: volver a la revisión/artefacto previamente registrado (T03.5 documentado: código `3afd6c6`) y reconstruir/recrear únicamente API/web tras autorización; verificar ambos health checks. No borrar volúmenes, no usar `reset --hard`, no cambiar secretos. InMemory implica que reiniciar API elimina la caché temporal, pero no existe historial durable que restaurar.

La rama ya está publicada y el PR draft #89 existe. No hacer merge ni desplegar sin autorización explícita.

No se deben volver a crear ramas ni rehacer la integración; el worktree aislado está en el commit publicado `e7b2ca5`.

## 11. Rollback de Git

`main` permanece intacta. Si se decide retirar la integración del historial de staging, revisar el diff y revertir el merge mediante `git revert -m 1 5f7838a3dd38e2930411ac1bd2a95a51a635acc8`; no usar reset/force-push. El commit documental `e7b2ca5` puede conservarse o revertirse por separado.
