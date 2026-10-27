# RELEASE-02 — consolidación de PROJECT ECHO

Fecha de evaluación: 2026-10-27
Rama candidata: `release/echo-v2`, creada desde `origin/main` (`6b5f088ed89a68ac69b4fcac31f8b1a8ac123b18`).
Estado: **READY** para revisión/autorización; no fusionado ni desplegado.

## 1. Estado inicial

- El checkout compartido estaba limpio en `feature/t03-6-resource-protection`; se dejó intacto. El trabajo de release está aislado en `/tmp/improved-train-release-echo-v2`.
- El host es Linux ARM64 (`aarch64`). `main` contenía T03.5 y T04.1–T04.4. No contenía la protección de recursos T03.6.
- La rama `release/echo-staging` estaba en `a71ff68`; conserva T03.5, T03.6 y T04.1/T04.2, pero no las entregas T04.3/T04.4 posteriores.
- T03.6 en `origin/feature/t03-6-resource-protection` estaba en `217ec8e`; su PR #86 estaba fusionado en la rama T03.5, no en `main`.

## 2. Estado de GitHub

Se verificó `merged` explícitamente, no solo `closed`:

| PR | Estado observado | Contenido / destino |
|---|---|---|
| #83 | merged | T03.5 → `main` |
| #84 | merged | T04.1 → rama T03.5 |
| #85 | merged | T04.2 → T04.1 |
| #86 | merged | T03.6 → T03.5 |
| #87 | merged | Integración T03.6 en `release/echo-staging` |
| #88 | merged | T04.3 → T04.2 |
| #89 | open, draft | RELEASE-01 antigua hacia `main`; no fusionada |
| #90 | merged | T04.1–T04.4 → `main` |
| #91/#92 | merged | Solo documentación de despliegue |
| #93 | merged | RELEASE-02: T03.6 + T04.1–T04.4 → `main` |

El merge-base de `main` y `release/echo-staging` es `2eb547e` (T04.2). Se eligió fusionar esa rama de staging porque ya incorpora T03.6 mediante el merge revisado #87 y preserva las resoluciones previas; la rama resultante conserva las entregas T04.3/T04.4 presentes en `main`. Esto evita aplicar un parche de API a ciegas o duplicar commits individuales.

Al comparar topologías en `2026-10-27`, `main...release/echo-staging` daba 11 commits solo en main y 12 solo en staging; estos incluyen el merge T03.6 y registros históricos de la rama. `main...feature/t03-6-resource-protection` daba 14 solo en main y 6 solo en feature; la feature parte del merge-base `7aad4e3` (T04.1) y no incluye la cadena T04.2–T04.4. `main...feature/t04-3-design-system` daba 9 solo en main y 0 solo en la rama; `main...feature/t04-4-home-redesign` daba 6 y 0. Por eso ninguna feature T03.6 o T04 antigua se usó como reemplazo de la punta de main.

## 3. Funcionalidades presentes y pendientes

| Alcance | `main` antes | Candidata RELEASE-02 | Evidencia |
|---|---:|---:|---|
| T03.5 stateless, flujos independientes y contratos 5/3 | Sí | Sí | PR #83, suite .NET |
| T03.6 admission/resource protection, rate limits Riot, cancelación y métricas | No | Sí | Merge #87, diff de integración y suite .NET |
| T04.1 auditoría y T04.2 UX/cobertura/accesibilidad | Sí | Sí | PR #90, pruebas DOM |
| T04.3 Cyber Arcade, tokens y galería protegida | Sí | Sí | PR #88, build y pruebas |
| T04.4 Home/Search First | Sí | Sí | PR #90, build y pruebas DOM |
| Persistencia/AnalysisJobs funcionales | No | No | D-042 y contrato stateless |

## 4. Integración y conflictos

- El merge de `origin/release/echo-staging` sobre `origin/main` en esta rama solo tuvo conflictos en `docs/PROJECT_CONTEXT.md` y `docs/TODO.md`.
- Se conservaron los registros actuales T04.3/T04.4 y se añadió el estado de T03.6 sin sustituirlos por documentación de staging desactualizada.
- Los cambios de aplicación de T03.6 se integraron sin conflicto textual. La revisión semántica y las pruebas combinadas son necesarias para validar su interacción con la Home y el Design System.
- No se incorporó otro commit de T03.6, ni se hizo merge a `main`.

## 5. Validaciones .NET

Host: contenedor SDK `10.0.402`, Linux ARM64. La restauración se ejecutó con acceso a NuGet; compilación, pruebas y formato corrieron con red deshabilitada. No se usó Riot real.

- `dotnet restore LolAnalyzer.slnx`: correcto.
- `dotnet build LolAnalyzer.slnx --configuration Release --no-restore`: correcto; 0 warnings, 0 errors.
- `dotnet test LolAnalyzer.slnx --configuration Release --no-build`: 126 aprobadas, 0 fallidas; 3 opt-in (dos benchmarks y un perfil) omitidas; 129 total.
- `dotnet format LolAnalyzer.slnx --verify-no-changes --no-restore`: correcto.

## 6. Validaciones frontend

Node `v24.18.0`, npm `11.16.0`.

- `npm ci`: correcto; 425 paquetes.
- `npm test`: 40 aprobadas, 0 fallidas.
- `npm run lint`: correcto.
- `npm run type-check`: correcto.
- `npm run build`: correcto; Next.js 16.4.2 compiló Home, galería, dashboard, detalle y partida activa.
- `npm audit --omit=dev`: 0 vulnerabilidades reportadas.
- `npm audit`: 1 high en Zod `4.5.4`, transitivo solo de desarrollo vía `eslint-config-next@16.4.2` → `eslint-plugin-react-hooks@7.1.1`. No se ejecutó `npm audit fix`; revisar parche compatible como trabajo separado. El intento posterior de obtener JSON actualizado del audit no pudo contactar registry.npmjs.org, por lo que no se presenta como un segundo resultado válido.

## 6.1 GitHub Actions

- PR [#93](https://github.com/D0kker/improved-train/pull/93), base `main`, compare `release/echo-v2`.
- Run `40465130308` del HEAD `392447879d7b68ad969901193c60548bbf5e0f93`: `web`, `dotnet` y `containers` terminaron `success`. El job `containers` ejecutó `docker compose config --quiet` y `docker compose build` en runner Ubuntu x64, completando la validación AMD64 que no permite el host ARM64 local.
- Al actualizar este informe se generará un HEAD documental nuevo; los checks de ese HEAD deben confirmarse antes de considerar READY definitivo.

## 7. Docker, despliegue actual y regresiones

- `docker compose config --quiet`: correcto en el worktree release.
- El host activo es ARM64. API y web preexistentes estaban `healthy`; API no publica puerto al host y web publica `8081`. No se reiniciaron ni alteraron servicios.
- Digests activos al inspeccionar: API `sha256:2eb828149a7afe2a43b07eeef6903bc334597581c32c0bac65b439124d2e62dc` (runtime T03.5); web `sha256:66f3e18a13d09bd1e34dc0f7325a00239b2577e0a7c2902ae9e2d66a04817b17` (Home T04.4). Ambos ARM64.
- IDs activos sin cambio durante la evaluación: API `647cab7b9f06`, web `cd331589abfa`. Ambos sin mounts y en `lol-network-analyzer_default`; API tiene puerto interno `8080` sin publicación host, web publica `8081:3000`. `GET /` y `/api/health` devolvieron 200.
- Redis `lol-network-analyzer-redis-1` estaba detenido (`Exited 0`), sin ser eliminado ni iniciado; su volumen persistente `lol-network-analyzer_redis_data` permanece montado en `/data`. T03.5/T03.6 no lo requieren en el runtime. Tampoco se tocaron otros contenedores detenidos.
- `GET /` y `GET /api/health` desde `127.0.0.1:8081`: HTTP 200. El healthcheck interno de API `/health/ready` pasó; el contenedor no expone 8080 al host.
- Redis aparece fuera de Compose como recurso huérfano; se conserva intacto.
- La suite integrada usa servicios Riot simulados para rate limits, timeouts, cancelación, fallos parciales y concurrencia. No se consumió cuota real.
- Builds Docker ARM64: API y web completados.
  - API `project-echo-release02-api:arm64`: `sha256:3676f4ef2a49c311eed5646d90d891de5ae3b17d09424e9f5d59be17b8a50ac0`.
  - Web `project-echo-release02-web:arm64`: `sha256:f39bb94667393bbcfc7d084b6da1d4bce30de8436756f596264650f6f760aa78`.
- Smoke aislado sin Riot ni credenciales: API `/health/ready` pasó; web root y `/api/health` pasaron internamente; Next.js pudo conectar/proxy a API en red interna (`/api/v1/release02-smoke-nonexistent` devolvió 404 esperado). Los contenedores/red temporales fueron retirados; los servicios existentes permanecieron intactos.
- Build AMD64 local: intentado con buildx, falló al ejecutar `/bin/sh` por falta de emulación/binfmt en el host ARM64. No se afirma build AMD64 local exitoso; el job `containers` de Actions en Ubuntu x64 sí completó `docker compose build` satisfactoriamente para el PR #93.

### Despliegue RELEASE-02 (2026-10-27)

- PR #93 se fusionó en `main` mediante merge commit `1a8ae443678b13f3e56c31e388df3624860ab051`. El código de aplicación de ese main es idéntico al árbol validado/build de `0db73581f4c2c3bb7f7cf028866273cc5eab9f98`; commits documentales posteriores no cambiaron `apps/api`, `apps/web`, Compose ni infraestructura.
- Se actualizaron solo API y web con Compose desde `/tmp/improved-train-release-echo-v2`, pasando el `.env` existente y `WEB_PORT=8081`: `docker compose ... up -d --no-build --wait --wait-timeout 180 api web`. Compose advirtió Redis huérfano; no se utilizó `--remove-orphans`.
- Se conservaron para rollback los tags `lol-network-analyzer-api:release02-rollback` y `lol-network-analyzer-web:release02-rollback` con los digests previos exactos.
- API nueva: `sha256:3676f4ef2a49c311eed5646d90d891de5ae3b17d09424e9f5d59be17b8a50ac0`, contenedor `388d3b1fca2c`, ARM64, started `2026-10-27T07:22:49.228957282Z`, healthy. API sigue interna en `8080`, sin host port ni mounts.
- Web nueva: `sha256:f39bb94667393bbcfc7d084b6da1d4bce30de8436756f596264650f6f760aa78`, contenedor `6f00409d54b8`, ARM64, started `2026-10-27T07:22:55.574496826Z`, healthy. Publica `8081:3000`, sin mounts.
- Smoke posterior: `GET /` HTTP 200 y copy de Home presente; `GET /api/health` HTTP 200 con `healthy`; healthcheck API `/health/ready` pasó; desde el contenedor web, `api:8080/health/ready` respondió y una ruta API inexistente fue proxyada a 404 esperado. No se hicieron llamadas a Riot.
- Redis conservó el ID `c606fd91491e`, estado `exited`, y el volumen `lol-network-analyzer_redis_data` montado en `/data`; no se inició, borró ni recreó. Ningún otro contenedor se tocó.
- CI previo al despliegue: run `40465960715` en el HEAD de PR validó `web`, `dotnet` y `containers` (incluido Docker Compose build x64). No apareció un run separado post-merge para el merge commit; el árbol de aplicación es idéntico al HEAD de PR aprobado.

## 8. Seguridad y limitaciones

- No se detectaron secretos añadidos en el diff; la clave Riot permanece como variable de runtime de API y no se imprimió ni incluyó en el informe.
- T03.6 limita recursos por proceso; la cuota por IP detrás de Next es compartida por usuarios y no se coordina entre réplicas. No constituye protección volumétrica DDoS.
- Hay 1 aviso high no productivo en la cadena de lint; audit de producción sin findings.
- La validación local no prueba cuotas Riot reales, producción pública, ni latencia/carga real. Se ejecutó en ARM64, sin prueba AMD64 nativa.
- La validación funcional es automatizada; no hubo prueba manual en navegador o tecnologías asistivas durante este release.

## 9. PR #89

El candidato contiene el trabajo útil de #89 (T03.5/T03.6 y UX T04.1/T04.2) además de T04.3/T04.4 más recientes que #89 excluía. Se recomienda cerrar #89 como sustituido por el PR de RELEASE-02, una vez que el propietario confirme que no necesita preservar el PR histórico. No se cerró automáticamente.

## 10. Preparación de despliegue y rollback

**Ejecutado con autorización explícita del propietario.**

- SHA de integración (árbol de aplicación usado para construir): `0db73581f4c2c3bb7f7cf028866273cc5eab9f98`.
- El HEAD de publicación puede incluir después un commit documental adicional; el código de app no cambia respecto a este SHA.
- `main` previo: `6b5f088ed89a68ac69b4fcac31f8b1a8ac123b18`.
- Servicios actualizados: API y web; ambos ahora corresponden a los artefactos listados en §7.
- Puertos: web `8081` → contenedor `3000`; API solo interno `8080`.
- Se mantuvo la configuración Riot runtime existente, sin imprimir secretos. Inspección segura de `.env`: `RIOT_PLATFORM_REGION=la1`, `RIOT_REGIONAL_ROUTING=americas`, timeout Riot `10s` y concurrencia Riot `3`; `RIOT_API_KEY` está configurada solo en API (su valor nunca se mostró). Compose aplicó defaults de admisión 8 activos/16 en cola, espera de cola 5000ms, cuota por cliente burst20/20 por minuto, máximo1024 clientes/idle180s y ventanas Riot 20 por 1s / 100 por 120s.
- Health: API `/health/ready`; web `/api/health`; smoke `GET /` y proxy web→API.
- Rollback disponible: volver a etiquetar `lol-network-analyzer-api:release02-rollback` y `lol-network-analyzer-web:release02-rollback` sobre los tags Compose `latest`, y ejecutar desde el mismo worktree `WEB_PORT=8081 docker compose --env-file /home/dokker/improved-train/.env up -d --no-build --wait --wait-timeout 180 api web`; Redis/volúmenes permanecen fuera de la operación. Verificar `/health/ready`, `/api/health` y `GET /`.
- Criterios de aborto originales: healthcheck fallido, errores sostenidos, proxy roto o configuración inválida. No se activaron; ambos servicios quedaron healthy y los smoke checks pasaron.

## 11. Estado final

**DEPLOYED**. PR #93 está merged en `main` (`1a8ae44`); API/web usan los digests RELEASE-02 y quedaron healthy en el puerto web 8081. Smoke checks y proxy pasaron; Redis/volumen permanecen intactos. No hubo llamadas a Riot ni cambios a otros servicios. Triage residual: un high solo de desarrollo en Zod transitivo de ESLint; audit de producción cero.
