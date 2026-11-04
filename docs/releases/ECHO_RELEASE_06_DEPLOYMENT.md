# RELEASE-06 — Registro de despliegue

**Estado: `DEPLOYED`**

**Fecha:** 2026-11-03, 23:00:22–23:02 (America/Panama; UTC 2026-11-04 04:00:22–04:02)

**Código:** `main` en `6b452208a3aa0ed9b8ea7a94fe51b7ca32cd50bb`

## Alcance

Se actualizó exclusivamente el servicio Compose `web` con T04.6 — análisis de partida, equipos, relaciones históricas, grupos con coincidencias y cobertura. La API no cambió y permanece en RELEASE-05. No se consultó Riot API real durante las pruebas.

## Integración y build

- PR #102, #103 y #104 están fusionados en el SHA aprobado de `main`.
- GitHub Actions para `6b452208a3aa0ed9b8ea7a94fe51b7ca32cd50bb`: checks `web`, `dotnet` y `containers` concluyeron `success` (run 41768746262).
- Imagen construida desde un worktree limpio de ese SHA con `docker buildx build --platform linux/arm64`, Dockerfile `apps/web/Dockerfile` y lockfile `package-lock.json`.
- Tag: `project-echo-release06-web:6b45220`.
- Image ID: `sha256:c5a601b112bf3eff80800a947473bb391b5971d9856f2d6e68baaa10b78899eb`.
- Plataforma: `linux/arm64`. La etiqueta `org.opencontainers.image.revision` coincide con el SHA aprobado. Tamaño: 220,921,808 bytes.
- La construcción solo cargó la imagen local; no se reconstruyó ni publicó una imagen API.

## Contenedores antes y después

| Servicio | Antes | Después | Estado posterior |
|---|---|---|---|
| `web` (`lol-network-analyzer-web-1`) | Container `84503e64b2727266181982344152efbf6361fbabdc6839b43cbc45ce8b648e3c`; imagen `project-echo-release05-web:16b00eb`; ID `sha256:241a8fe565c5f0255698d5b8ab2ab08eda5f14273b20fe894baf8a7a450ce2f7` | Container `c02f3fffaaeee90aafd069ad73fd00b2a137505fcd18818a62a561f4c9868d9e`; imagen `project-echo-release06-web:6b45220`; ID `sha256:c5a601b112bf3eff80800a947473bb391b5971d9856f2d6e68baaa10b78899eb` | `running`, health `healthy`, puerto host `8081` a contenedor `3000/tcp` |
| `api` (`lol-network-analyzer-api-1`) | Container `75379dc8948f82a2f5b8b3475f187d19362eed2eab391286f622e9436b821cfc`; image ID `sha256:998e9bd6cf82c12658f9e0ea05ac7e13395350fc6cc1d18a495694f80fa2c47d` | Mismo container e image ID; revisión `16b00eb343c5f43496fb55571084e119b84d6d49` | `running`, health `healthy` |

Compose usó el proyecto `lol-network-analyzer`, la configuración efectiva de RELEASE-05 y un override temporal de imagen para `web`. `up` se ejecutó con `--no-deps --no-build --wait`; no se recreó API ni ningún otro servicio.

## Preflight y smoke tests

- Host ARM64; Docker Engine 29.7.1.
- Antes del build: 6.5 GB de disco libre y 7.0 GiB de memoria disponible. Después: 6.3 GB y 6.9 GiB.
- Compose config efectivo y override validados; API continuó fijada en `project-echo-release05-api:16b00eb`; mapeo de web conservó `8081:3000`.
- Home `/`: HTTP 200.
- Proxy web → API `/api/health`: HTTP 200; cuerpo reportó `healthy`.
- Ruta del dashboard de jugador con identidad sintética: HTTP 200.
- Ruta de análisis de partida con Match ID sintético: HTTP 200.
- Web y API healthy después de la recreación. En los logs de los últimos 10 minutos no se encontraron líneas de nivel crítico/fatal ni excepciones no controladas.
- No se consumió Riot API real. Las rutas del dashboard y análisis se validaron como respuestas HTTP de página; la presentación de datos y estados se cubrió con fixtures y capturas Chromium ya versionadas en T04.6.2. No se afirma que una partida real se haya analizado después del despliegue.

## Redis, volúmenes y rollback

- No había contenedor Redis en `docker ps -a` antes ni después. El volumen `lol-network-analyzer_redis_data` existe y no fue modificado. No se tocaron volúmenes, secretos ni redes.
- La imagen de rollback permanece disponible: `project-echo-release05-web:16b00eb`, Image ID `sha256:241a8fe565c5f0255698d5b8ab2ab08eda5f14273b20fe894baf8a7a450ce2f7`.
- No fue necesario ejecutar rollback. Si hiciera falta, usar los Compose files de RELEASE-05 y `WEB_PORT=8081 docker compose ... up -d --no-deps --no-build --wait web`, sin recrear API.

## Resultado

RELEASE-06 quedó desplegada en `web`; API RELEASE-05 permanece saludable e intacta. La web nueva está saludable, responde en 8081 y alcanza la API mediante el proxy. No se modificaron Redis, volúmenes ni infraestructura. Este documento registra el resultado; se publicará en una rama documental y PR separados, sin un nuevo despliegue.
