# RELEASE-03 — despliegue de T04.5 Player Dashboard

Fecha: 2026-10-28 (America/Panama)
Estado: **DEPLOYED**

## Resumen

Se fusionó PR #95 y se desplegó exclusivamente el servicio `web` desde el `main` resultante. API y Redis no se recrearon ni modificaron. No se integró T04.6, no se llamó a Riot y no se expusieron valores secretos.

## GitHub y revisión

- PR [#95 — T04.5 Player Dashboard](https://github.com/D0kker/improved-train/pull/95): merged hacia `main` mediante merge commit.
- HEAD del PR comprobado antes del merge: `0720065dfa7a5bd45a6ff19c2d6f8379cb904046`; `main` previo: `1a8ae443678b13f3e56c31e388df3624860ab051`.
- SHA final de `main`: `2c67fe5cb9d5ffaba6b9f99e3651616a53526409`.
- El árbol del merge y el HEAD probado del PR coinciden (`18b1d7d5d196fefcd22d9fb0ddf954675ed286b3`).
- GitHub Actions run `40472081429`: `dotnet`, `web` y `containers` aprobaron. Incluyó `npm ci`, lint, tests, type-check, build y Docker Compose build. El merge commit tiene el mismo árbol que el HEAD evaluado.
- El PR cambiaba solo CSS, componentes/página y pruebas web, además de documentación. No tocaba API, contratos, infraestructura, Compose ni secretos.

## Validaciones del SHA desplegado

Ejecutadas en `/tmp/improved-train-release-03`, detached en el SHA de `main`:

| Comando | Resultado |
| --- | --- |
| `npm ci` | Aprobado; 425 paquetes. npm reportó una vulnerabilidad high en desarrollo. |
| `npm test` | Aprobado, 42/42. |
| `npm run lint` | Aprobado. |
| `npm run type-check` | Aprobado. |
| `npm run build` | Aprobado; generó las rutas Home, dashboard, detalle, partida activa, galería y health. |
| `npm audit --omit=dev` | Aprobado; 0 vulnerabilidades de producción. |
| `docker compose --env-file /home/dokker/improved-train/.env config --quiet` | Aprobado; no se imprimieron valores del archivo. |
| `git diff --check` | Aprobado. |

Next.js mostró una salida incompleta del proceso hijo de TypeScript en la primera ejecución confinada. `tsc --showConfig` directo devolvió JSON válido; el build repetido con ejecución ampliada terminó con código 0. No se modificó configuración ni se desactivaron validaciones.

## Baseline operativo antes del despliegue

- Host: `aarch64` (Linux ARM64); Docker Server `29.7.1`, Compose `5.3.1`.
- Web: contenedor `lol-network-analyzer-web-1`, ID `80e4c2ae562ab74cf7ede755deafeb46cfdf65da131a800cb178b51c3b6e145e`; imagen ID `sha256:f39bb94667393bbcfc7d084b6da1d4bce30de8436756f596264650f6f760aa78`, tags `lol-network-analyzer-web:latest` y `project-echo-release02-web:arm64`; healthy en `8081:3000`.
- API: contenedor `lol-network-analyzer-api-1`, ID `0594920115f67c14f522f9cb5fa6656017d86f85b615bd57f14eaf5c7323c8ab`; imagen ID `sha256:3676f4ef2a49c311eed5646d90d891de5ae3b17d09424e9f5d59be17b8a50ac0`; healthy en el puerto interno `8080`, sin publicación host.
- Redis: contenedor `lol-network-analyzer-redis-1`, ID `c606fd91491e6faa880ac83731f847ab2ee3522509971dc2b87fd9a939b69135`; imagen `sha256:59c08762bdbcf53fa132aa0bae464a57a1309dc3fff8a034bb3e16d7a0b30ec5`; ya estaba detenido (exited) y marcado unhealthy desde antes. Su volumen `lol-network-analyzer_redis_data` estaba presente. No se inició, eliminó ni modificó.
- Baseline HTTP: `/` 200, `/api/health` 200 y readiness interna API `/health/ready` satisfactoria. Una ruta API sintética desconocida devolvió 404 por el proxy.
- Rollback: la imagen anterior permaneció disponible bajo `project-echo-release02-web:arm64` con el ID anterior indicado arriba.

## Build y despliegue

- SHA fuente: `2c67fe5cb9d5ffaba6b9f99e3651616a53526409`.
- Comando de build: `docker compose --env-file /home/dokker/improved-train/.env build web`, desde el worktree en ese SHA. Solo se construyó `web`, en `linux/arm64`; no se reconstruyó API.
- Imagen resultante: ID/config digest local `sha256:f141e5dec8f4bbf4e6a5f0158ccd47f50a3ab5779edb1a47ebf1266a34e19ce5`, tag de servicio `lol-network-analyzer-web:latest` y tag de release `project-echo-release03-web:2c67fe5`. No se publicó a un registry, por lo que no hay `RepoDigest` de registry.
- Build completado: 2026-10-28 00:19:54 -05 (metadata de imagen). Despliegue: 2026-10-28 00:20 -05.
- Solo se actualizó el servicio web con `docker compose ... up -d --no-deps --no-build --wait --wait-timeout 180 web`. Compose advirtió el contenedor Redis huérfano; no se usó `--remove-orphans`.

## Verificación posterior

- Web: contenedor nuevo `632ab74505282b8c9078b6e77754b2c2a0cfa8dbdb431ec384fbf49dbfec114d`, imagen `sha256:f141e5dec8f4bbf4e6a5f0158ccd47f50a3ab5779edb1a47ebf1266a34e19ce5`, `healthy`, puerto `8081:3000`.
- API: ID e imagen idénticos al baseline; continúa `healthy`. Redis: mismo ID, imagen, estado detenido y volumen.
- Smoke HTTP: `/` 200; `/api/health` 200; `/player/Release%20Smoke/NA1` 200.
- Proxy: `GET /api/v1/players/synthetic-puuid/recent-matches?start=-1` devolvió 400, resultado esperado de validación del endpoint antes de consultar Riot; prueba el proxy web→API sin red externa.
- El healthcheck interno de API `/health/ready` pasó. No se observaron errores fatales, críticos o excepciones no controladas en los logs recientes (solo se contó coincidencias; no se imprimió contenido de logs).
- La configuración del web conserva `API_BASE_URL=http://api:8080` y la región configurada; `RIOT_API_KEY` no está en el entorno del frontend.
- No se llamaron endpoints de Riot, no se reiniciaron API/Redis y no se tocaron volúmenes.

## Rollback

La imagen anterior conserva el tag `project-echo-release02-web:arm64`. Para volver atrás desde un checkout con el Compose de `main` en SHA `2c67fe5` y el mismo archivo runtime `.env`:

```bash
docker image tag project-echo-release02-web:arm64 lol-network-analyzer-web:latest
docker compose --env-file /home/dokker/improved-train/.env up -d --no-deps --no-build --wait --wait-timeout 180 web
curl --fail --silent --show-error http://127.0.0.1:8081/
curl --fail --silent --show-error http://127.0.0.1:8081/api/health
```

El rollback solo cambia la imagen/contenedor `web`; no revierte API ni Redis y no elimina la imagen nueva.

## Límites

- No se hizo una prueba con Riot real. El dashboard se probó con fixtures y la ruta responde; no se verificó una consulta de jugador real.
- No había navegador disponible en T04.5; no hay capturas ni validación visual renderizada de breakpoints. La limitación quedó registrada en `docs/T04_5_PLAYER_DASHBOARD.md`.
- La auditoría npm de producción no reportó vulnerabilidades. `npm ci` mostró un hallazgo high de desarrollo que debe evaluarse aparte; no bloquea el audit de producción.
- T04.6 no fue incorporada.

## Publicación del informe

Este registro se publica mediante un PR documental independiente hacia `main`; ese PR no cambia el runtime y no se fusiona automáticamente.
