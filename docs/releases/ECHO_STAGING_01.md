# RELEASE-01 — Integración PROJECT ECHO en staging

Fecha de revisión: 2026-10-19. Este informe describe el árbol de integración en el worktree `release/echo-staging`; **no implica merge a `main` ni despliegue**.

## 1. Resumen ejecutivo

La rama remota de staging parte de `feature/t04-2-ux-corrections` (`2eb547e`) y contiene T03.5/T04.1/T04.2 previos a T04.3. En un worktree aislado se integró mediante merge explícito `origin/feature/t03-6-resource-protection` (`217ec8e`), resuelto manualmente en cuatro conflictos frontend, y se creó el commit local `5f7838a`. T03.6 fue después integrado en `feature/t03-5-stateless-api` (`eed0129`) y ambos árboles fuente son idénticos; staging conserva el mismo contenido T03.6 mediante el merge explícito, sin incorporar T04.3.

El frontend integrado pasó 33 pruebas, ESLint, TypeScript y build. `docker compose config --quiet` y `git diff --check` pasaron. Las validaciones .NET no se pudieron ejecutar: no hay SDK .NET en el host y el acceso al socket Docker fue denegado. `npm ci` tampoco terminó porque el filesystem se quedó sin espacio. Las pruebas frontend se ejecutaron con dependencias ya instaladas del worktree T04.2, sin modificar ese worktree. No hay CI observado para el commit integrado.

El audit de dependencias de producción informó 1 vulnerabilidad crítica y 2 altas. La versión fijada de Next es 16.3.3; se recomienda actualizar a una versión parcheada y repetir el audit antes de cualquier despliegue público. El informe no autoriza ni implementa esa actualización.

**Estado: BLOCKED para release/despliegue controlado.** El merge `5f7838a` solo existe localmente. Falta validación backend y CI del árbol integrado; no se publicó la rama, no se creó PR a `main`, no se cerró PR #87 ni se desplegó.

## 2. Estado de PR y ramas al 2026-10-19

| PR | Estado consultado | Base ← compare | Resultado relevante |
|---|---|---|---|
| #83 | Merged | `main` ← `feature/t03-5-stateless-api` | T03.5 integrado en main; checks `web`, `dotnet`, `containers` exitosos en el commit consultado. |
| #84 | Merged | `feature/t03-5-stateless-api` ← `feature/t04-1-ux-audit` | Auditoría T04.1 documental integrada en la rama T03.5. |
| #85 | Merged | `feature/t04-1-ux-audit` ← `feature/t04-2-ux-corrections` | T04.2 integrado en T04.1. |
| #86 | Merged | `feature/t03-5-stateless-api` ← `feature/t03-6-resource-protection` | Merge commit remoto `eed0129`; el árbol resultante coincide con el de T03.6. |
| #87 | Open, draft, conflicting | `release/echo-staging` ← `feature/t03-6-resource-protection` | Es la propuesta de integración que este trabajo reemplazará solo después de validar y publicar la rama. |
| #88 | Merged (observado en el fetch) | `feature/t04-2-ux-corrections` ← `feature/t04-3-design-system` | Posterior al SHA de staging; **T04.3 está excluida** de RELEASE-01. |

Refs consultadas en el fetch local: `origin/feature/t03-5-stateless-api`=`eed0129`, `origin/feature/t04-1-ux-audit`=`537daf9`, `origin/feature/t04-2-ux-corrections`=`002cbb4`, `origin/release/echo-staging`=`2eb547e`, `origin/feature/t03-6-resource-protection`=`217ec8e`. Staging no incluye el merge T04.3 (`60ede46`, `a7cec40`, `002cbb4`). GitHub API/CLI dejó de estar accesible durante esta revisión; el último estado consultable de PR es el de la sesión previa, salvo PR #86 cuyo merge se confirma en refs Git frescas.

Los checks verdes reportados anteriormente pertenecen al head T03.6 (`217ec8e`), no al árbol combinado. No se pudieron consultar los estados actuales de todos los PR ni checks del árbol integrado.

## 3. Historial y commits candidatos

Staging conserva su línea de base en `2eb547e` y la etiqueta de respaldo `backup/release-echo-staging-20261019`. El merge conserva el historial T03.6: `f7428c6` (protección), `323ac1c` (CI), `56502f1`, `20ffa33`, `1fa7150` y `217ec8e`. El commit candidato local es `5f7838a`; no está publicado.

No se incorporaron commits de T04.3. No se modificaron las ramas fuente ni `main`.

## 4. Conflictos y resolución

Git reportó conflictos de contenido en:

| Archivo | Resolución aplicada |
|---|---|
| `apps/web/src/api.ts` | Se conservó el mapeo de errores por endpoint de T04.2 y se integró el timeout de fetch de 200 s de T03.6, con cancelación combinada, sin reintentos automáticos y limpieza del timer. |
| `apps/web/app/player/[gameName]/[tagLine]/player-dashboard.tsx` | Se conservó T04.2: región visible, cobertura, reintento de ventana parcial y paginación por bloques de cinco con deduplicación. Se mantuvieron los AbortController separados para búsqueda e historial, que cancelan solicitudes supersedidas/unmount y previenen actualizaciones obsoletas; no se reemplazaron por una vida de solicitud única que pudiera afectar las dos vías concurrentes. |
| `apps/web/app/match/[matchId]/match-detail-view.tsx` | Se conservaron región, cobertura, navegación de regreso y estados T04.2. Se añadió `useRequestLifetime` de T03.6; cada carga supersede la anterior y el cleanup del hook cancela al desmontar. |
| `apps/web/app/match/live/live-match-view.tsx` | Se conservó consulta independiente de partida activa, región, estado sin partida, datos anteriores y refresco de T04.2. Se integró `useRequestLifetime` con el AbortController local y ruta con región; no se acopló la partida activa a la carga del historial. |

`docker-compose.yml`, `apps/web/tests/frontend.test.ts`, `docs/TODO.md` y la documentación/contexto se combinaron automáticamente; se revisarán en el diff final. El merge no reintroduce EF Core, jobs durables ni persistencia.

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
| `npm ci` | Bloqueado por `ENOSPC` durante extracción de paquetes; no se modificó lockfile. Se quitó solo el `node_modules` parcial de este worktree. |
| `npm test` | 33 aprobadas, 0 fallidas. Ejecutado reutilizando en solo lectura el `node_modules` previamente instalado en el worktree T04.2 con el mismo `package.json`/lockfile. |
| `npm run lint` | Pasó. |
| `npm run type-check` | Pasó. |
| `npm run build` | Pasó con Next.js 16.3.3; todas las rutas esperadas se generaron. |
| `npm run format:check` | Informó diferencias de estilo en 15 archivos, incluidos varios que no forman parte del diff de integración. No se hizo reformat masivo porque no es requisito de esta release y produciría cambios ajenos al alcance. |
| `dotnet --info`, `dotnet restore LolAnalyzer.slnx` | No ejecutables: `dotnet: command not found`. |
| Docker SDK runner / `docker info` | Socket `/var/run/docker.sock` denegó acceso (`permission denied`). No se alteraron permisos ni se buscó eludir controles. |
| `docker compose config --quiet` | Pasó, sin imprimir valores de configuración. |
| `git diff --check` | Pasó tras resolver los cuatro conflictos. |
| Riot API | No se consultó. |

Backend build, tests, format, los escenarios integrados 503/504, CI sobre la integración, y pruebas reales de contenedores permanecen pendientes. No se declaran exitosos por el hecho de existir checks anteriores de #86.

## 8. Seguridad de dependencias

`npm audit --omit=dev --json` al 2026-10-19 reportó 3 vulnerabilidades en el árbol de producción: 1 crítica y 2 altas. La app declara Next `16.3.3`; el audit informa como fix disponible Next `16.4.2`. Los avisos de Next incluyen RCE en `next/og` (GHSA-vcvr-r3jv-pc5j) y otros problemas hasta `16.3.8`; `sharp` aparece en `0.35.4` (< parche `0.35.5`) y `source-map-js` en `1.2.1` (< parche `1.2.2`). Ver [advisory Next RCE](https://github.com/advisories/GHSA-vcvr-r3jv-pc5j), [advisory Next SSRF](https://github.com/advisories/GHSA-cjq9-62q9-8jv4), [advisory sharp](https://github.com/advisories/GHSA-wq5f-xc86-pv6w) y [advisory source-map-js](https://github.com/advisories/GHSA-68fv-2mgg-jv7q).

La revisión estática de este árbol no encontró `next/og`/`ImageResponse`, Cache Components ni `images.remotePatterns`. El componente `ChampionIcon` usa `next/image` con `unoptimized`; las imágenes provienen de un proxy con identificador numérico y URLs Data Dragon fijas. Esto reduce la aplicabilidad de los avisos concretos de ImageResponse/optimizer en el código inspeccionado, pero **no elimina el riesgo de mantener dependencias en rangos afectados** ni sustituye actualizar y volver a escanear. `sharp` y `source-map-js` son transitivos presentes en la instalación de producción; no se afirmó explotabilidad directa desde una ruta pública.

No se ejecutó `npm audit fix` ni se actualizó ninguna dependencia. Recomendación antes del despliegue público: actualizar Next al menos a la versión corregida compatible más baja (`16.3.8`) o a una versión parcheada aprobada, actualizar `sharp` y `source-map-js` transitivos mediante una actualización lockfile revisada, y repetir audit, pruebas y build. El hallazgo de Next es un bloqueo de go/no-go de despliegue, no una modificación silenciosa dentro de RELEASE-01.

## 9. Limitaciones y riesgos pendientes

- No hay evidencia local de que el backend integrado compile o que sus pruebas pasen; el entorno no tiene SDK y el acceso Docker está bloqueado.
- El filesystem llegó a 100% de uso; `npm ci` quedó incompleto. Tras retirar su artefacto parcial solo quedaron aproximadamente 69 MiB libres. No se borraron caches, worktrees, imágenes, volúmenes ni servicios existentes.
- Checks verdes previos corresponden al head de T03.6, no a la resolución combinada.
- Formato Prettier reporta 15 archivos; varias diferencias son preexistentes y no se corrigieron masivamente.
- Auditoría npm de producción encuentra dependencias con advisories; se requiere parche/re-audit antes de despliegue público.
- Rate limits y estado single-flight son locales por instancia. La cuota por IP detrás del proxy puede compartirse.
- Paginación puede quedar detenida si un detalle falla permanentemente; se conserva la cobertura y limitación documentadas en T04.2.
- No hubo test visual en navegador ni llamadas a Riot API.

## 10. Procedimiento posterior de publicación y revisión

Solo cuando las validaciones obligatorias se hayan ejecutado con éxito:

1. Confirmar rama `release/echo-staging`, `git status`, `git diff --check`, secretos y diff completo.
2. Ejecutar restore/build/test/format .NET en runner autorizado; ejecutar `npm ci`, test/lint/type-check/build y `docker compose config --quiet` en entorno con espacio suficiente.
3. Ejecutar CI sobre el merge commit publicado.
4. Publicar por SSH el commit local `5f7838a` cuando el runner autorizado permita completar las validaciones; verificar CI sobre ese SHA exacto.
5. Tras CI satisfactorio, publicar el merge de staging y verificar que PR #87 no aporta cambios exclusivos antes de cerrarlo.
6. Crear PR draft de revisión `release/echo-staging` → `main` si no existe equivalente. No hacer merge ni desplegar sin instrucción explícita.

No se deben volver a crear ramas ni rehacer la integración; continuar desde el MERGE_HEAD actual en el worktree aislado.

## 11. Rollback

Antes de cualquier merge/despliegue, el rollback de staging es mantener `main` sin cambios y revertir el merge commit en la rama de staging con `git revert -m 1 <sha-del-merge>`, después de revisar su diff. Si un despliegue futuro se autoriza por separado, seguir el runbook operativo de la plataforma: volver al artefacto previamente verificado y confirmar health checks; no borrar contenedores/volúmenes ni usar `reset --hard` como mecanismo de rollback.
