# RELEASE-06 — Plan de validación y despliegue candidato

**Estado:** `REVIEW_REQUIRED`  
**Fecha de actualización:** 2026-11-01 UTC

**Candidato actual:** `57835f14003007008975836cd74685a430eae0de` (`feature/t04-6-match-analysis-implementation`; captura/index y runner visual, sin cambios funcionales)
**Base observada:** `main` en `16b00eb343c5f43496fb55571084e119b84d6d49`

> La revisión T04.6.2 generó capturas nuevas del commit `b331627` con fixtures sintéticos en Chromium 120 ARM64. El runner añade una captura aislada del equipo owner; no hay cambios funcionales.

## 1. PR y alcance

- [PR #102 — propuesta UX de análisis](https://github.com/D0kker/improved-train/pull/102): abierto, no fusionado, base `main`, HEAD `df983d643abc51108e2bdd358b8a55c581bfd182`, fusionable. Solo documentación y wireframes textuales.
- [PR #103 — implementación T04.6](https://github.com/D0kker/improved-train/pull/103): abierto, no fusionado, base `main`, HEAD `57835f14003007008975836cd74685a430eae0de`, fusionable y no draft. CI del commit de evidencia #73 estaba en curso al actualizar este plan; CI #70 aprobó el código funcional `b331627` (web, .NET y contenedores).
- T04.6 implementa resumen, equipos y participantes, parejas con evidencia histórica, grupos con coincidencias históricas y cobertura. No modifica API/backend, algoritmos, contratos, caché, seguridad, solicitudes Riot ni el dashboard de RELEASE-05.
- La propuesta #102 y la implementación #103 son consistentes. La pantalla conserva la lista de parejas recomendada, la terminología cautelosa y la semántica de cobertura; la implementación sigue las decisiones aprobadas posteriormente por el Product Owner.

## 2. Validación técnica y funcional

En el worktree limpio de #103, Node `v24.18.0`, npm `11.16.0`:

| Comando | Resultado |
|---|---|
| `npm ci` | Aprobado. La ejecución normal del sandbox bloqueó el subprocess de esbuild; se repitió con permisos de ejecución y finalizó correctamente. |
| `npm test` | **81/81 aprobadas**, 0 fallidas, omitidas o canceladas. |
| `npm run lint` | Aprobado. |
| `npm run type-check` | Aprobado. |
| `npm run build` | Aprobado; rutas de Home, jugador, partida, partida activa y galería compilan. |
| `npm audit --omit=dev` | No verificable en esta actualización: el endpoint de auditoría no resolvió por DNS. El resultado anterior fue 0 vulnerabilidades de producción para el árbol sin cambios de dependencias; no se considera una auditoría del HEAD vigente. |
| `docker compose config --quiet` | Aprobado. |
| `git diff --check` | Aprobado; worktree de implementación permanece limpio. |

`dotnet` no se ejecutó localmente: el diff de #103 no incluye cambios de backend o contratos. Los checks web, .NET y contenedores de CI #70 pasaron para el HEAD vigente; la CI ejecutó la validación de servidor correspondiente. `npm ci` informa 16 moderate y 1 high en el árbol completo; al no poder consultar el endpoint de auditoría, no se atribuye aquí alcance de producción a esos avisos. No se modificaron dependencias.

Los tests DOM cubren las cinco secciones, identidades incompletas, relaciones ordenadas de forma determinista, ausencia de datos, equipos parciales, carga fallida/reintento, cancelación, respuestas tardías, regreso a búsqueda y KDA finito. El cliente realiza una solicitud stateless de análisis de partida; esta tarea no añade llamadas Riot.

## 3. Validación de navegador

Validación T04.6.2 ejecutada contra el servidor Next aislado en `127.0.0.1:3216`, con Chromium 120 ARM64 extraído en `/tmp` y Playwright local del proyecto. No se instaló software global ni se tocó Docker/producción. Los fixtures son sintéticos; las rutas API se interceptaron y las solicitudes externas se bloquearon. No se usó Riot real.

- Viewports: **320, 390, 768, 1024 y 1440 px**; `scrollWidth` coincide con el ancho en cada uno.
- Secciones y escenarios `complete`, `partial`, `insufficient`, error/reintento y nombres largos comprobados; sin errores de página ni solicitudes inesperadas.
- Teclado: detalle de cobertura operable y foco visible de 3 px.
- axe en 390 y 1440 px: cero violaciones detectadas; `color-contrast` sigue como incomplete y requiere revisión manual. No certifica WCAG.
- Capturas actualizadas de desktop, móvil, resumen, equipos, owner, relaciones, grupos, cobertura parcial, vacío/error y nombres largos; inventario en [índice T04.6.2](t04-6-captures/README.md) y [resultados del runner](https://github.com/D0kker/improved-train/blob/feature/t04-6-match-analysis-implementation/docs/releases/t04-6-captures/t04-6.2/results.json).
- Inspección visual: azul frío/cian y coral se distinguen del fondo oscuro sin saturación; nombres y estadísticas legibles; el owner conserva realce propio y etiqueta textual. La pila móvil se lee sin overflow. No se reprodujeron defectos visuales. La evaluación manual de contraste con metodología WCAG y aprobación visual del Product Owner siguen pendientes.
- El recorrido Home → búsqueda → dashboard → selección → análisis → regreso se mantiene validado por la ejecución Playwright anterior con respuestas simuladas. Se verificaron Riot ID/tag/región en ruta, owner PUUID/plataforma en POST y Match ID de navegación. Las llamadas active-game/ranked fueron sintéticas; no Riot real.

## 4. Hallazgos y correcciones

No se encontraron defectos visuales o funcionales reproducibles, por lo que no se alteró el CSS ni la estructura aprobada. La inspección confirma la separación visual entre azul, coral y el resaltado del owner. No se afirma prueba con Riot real, evaluación asistiva completa ni conformidad WCAG. La aprobación visual final corresponde al Product Owner.

## 5. Contratos y compatibilidad API/web

T04.6 no agrega campos ni endpoints. Reutiliza:

- `GET /api/v1/matches/{matchId}` para la vista de partida sin owner.
- `POST /api/v1/matches/{matchId}/analyze` con el owner para análisis stateless.
- El resultado ya existente con `match`, `coverage`, `relationships` y `premadeGroups`.

| Frontend | API | Resultado esperado |
|---|---|---|
| RELEASE-05 | RELEASE-05 | Combinación actual; sin cambios. |
| T04.6 | RELEASE-05 | Compatible: los endpoints y campos consumidos ya existen en RELEASE-05. Los componentes degradan cuando faltan identidades/evidencia según los tests. |
| RELEASE-05 | API nueva para T04.6 | No aplica: #103 no cambia API. |
| T04.6 | API anterior a los contratos introducidos antes de RELEASE-05 | No certificado; no usar esa combinación. Desplegar web contra la API RELEASE-05 inventariada. |

Los tests con mocks y el CI confirman contratos en código; no sustituyen una prueba runtime contra la API desplegada. La lectura del API responde a compatibilidad del contrato conocido, no a una consulta Riot real.

## 6. Inventario de runtime (solo lectura)

Inspección realizada el 2026-10-31, sin modificar contenedores:

| Servicio | Contenedor | Imagen/tag | Image ID | Estado |
|---|---|---|---|---|
| API | `lol-network-analyzer-api-1` (`75379dc8948f…`) | `lol-network-analyzer-api` (RELEASE-05) | `sha256:998e9bd6cf82c12658f9e0ea05ac7e13395350fc6cc1d18a495694f80fa2c47d` | running / healthy |
| Web | `lol-network-analyzer-web-1` (`84503e64b272…`) | `project-echo-release05-web:16b00eb` | `sha256:241a8fe565c5f0255698d5b8ab2ab08eda5f14273b20fe894baf8a7a450ce2f7` | running / healthy |

Host `aarch64` (ARM64). Web publica `0.0.0.0:8081 -> 3000/tcp`; API expone 8080 dentro de Docker. Ambos Image IDs locales tienen `RepoDigests: []`; se registran como IDs de configuración/contenido local, no como digest de registry. El tag y la imagen Release-05 de web están disponibles para rollback. No se inspeccionaron ni cambiaron secretos, Redis, volúmenes, redes o configuración runtime.

## 7. Servicios necesarios y despliegue futuro

**Solo requiere actualizar `web`. No reconstruir ni actualizar API.** La implementación no cambia backend ni contratos y la API desplegada RELEASE-05 contiene las rutas/campos consumidos. Mantener API y configuración existentes.

Antes de un release autorizado, volver a verificar `main`, CI, compatibilidad y estado/espacio del host. Construir la imagen web para `linux/arm64` desde el SHA que finalmente se apruebe, con tag inmutable por SHA; registrar image ID y comprobar la etiqueta OCI revision. Validar Compose y realizar una prueba aislada antes de recrear únicamente `web`. En la actualización, usar el proyecto, env-file y override de imágenes efectivos del entorno; verificar healthcheck, puerto 8081, Home, dashboard, detalle, proxy y logs sin secretos. No usar `down`, `remove-orphans`, limpieza general ni tocar API/Redis/volúmenes. El procedimiento requiere autorización de despliegue independiente; este documento no la concede.

## 8. Rollback a RELEASE-05

Conservar API actual sin cambios. Ante fallo exclusivo de la web, apuntar el override de despliegue a la imagen fijada por ID de RELEASE-05 `sha256:241a8fe565c5f0255698d5b8ab2ab08eda5f14273b20fe894baf8a7a450ce2f7` (tag `project-echo-release05-web:16b00eb`), recrear solo `web` con `--no-deps --no-build --wait` según el Compose/env-file reales, y verificar salud web/API, `/`, `/api/health`, puerto 8081 y proxy. No revertir API, modificar Redis o eliminar imágenes. Registrar cualquier rollback y conservar la candidata hasta declarar estable RELEASE-06.

## 9. Riesgos y estado

- Revisión visual por el Product Owner pendiente; axe dejó contraste como incompleto y falta evaluación manual/asistiva.
- El recorrido Home → búsqueda → dashboard → análisis → regreso sí se ejecutó en navegador con APIs simuladas, como se detalla en la sección 3. No se consultó una cuenta Riot real.
- La compatibilidad descrita se aplica a API RELEASE-05 identificada; no se certifican APIs de versiones anteriores.
- El audit de producción no pudo repetirse en el HEAD vigente por fallo DNS; no se declara aprobado en esta actualización.
- No hay correcciones funcionales pendientes identificadas. Los PR #102 y #103 continúan abiertos; ninguno se fusionó.

**Estado final: `REVIEW_REQUIRED`.** T04.6 pasa las validaciones automatizadas y visuales sintéticas para revisión; requiere aprobación visual del Product Owner y revisión manual de contraste/asistencia antes de que otra tarea autorice el release. RELEASE-06 no se fusionó a `main`, no se desplegó y no modificó el runtime.

## 10. T04.6.2 — validación del tinte y estado vigente

### GitHub

- PR #102 está abierto, no fusionado, base `main`, HEAD `df983d643abc51108e2bdd358b8a55c581bfd182`, fusionable.
- PR #103 está abierto, no fusionado, base `main`, HEAD `57835f14003007008975836cd74685a430eae0de`, fusionable y no Draft. El commit de evidencia contiene las capturas/index y una línea de runner; CI #73 se debe confirmar antes de dar por validado el HEAD final. GitHub Actions `ci`, run #70, concluyó `success` para el código funcional `b331627`.
- PR #104 permanece abierto como Draft, base `main`, y contiene el plan documental de RELEASE-06.
- PR #97 está fusionado en `main`; no se modifica ni se despliega nada en esta tarea.

### Cambio cromático comprobado en código

El commit `b331627` aplica a `.analysis-team--blue` y `.analysis-team--red` un gradiente 145° que mezcla al 9% el token semántico azul o coral con `--surface`, y vuelve a la superficie oscura a 74%. Se conserva borde y estructura. El renglón owner usa una mezcla opaca dorada del 9% sobre `--surface`, así el acento del equipo no se superpone visualmente al resaltado del jugador consultado. El rótulo textual del owner continúa presente.

Una estimación matemática sRGB a partir de los tokens actuales da aproximadamente `#13243b` (azul), `#201f2f` (rojo) y `#1f272e` (resaltado owner). La captura y la inspección visual confirman legibilidad aparente del texto principal; no es una medición automatizada de píxeles ni una validación WCAG. axe dejó el análisis de `color-contrast` incompleto y requiere revisión manual.

### Entorno de navegador y evidencia actualizada

- Host `aarch64`. Playwright oficial no ofrece Chromium para Debian 11 ARM64; se utilizó Chromium 120 ARM64 de paquetes Debian extraídos únicamente en `/tmp`, junto con Playwright local. No hubo instalación global, contenedor activo ni modificación de producción.
- Servidor Next aislado en `127.0.0.1:3216`. El runner interceptó las rutas API simuladas y bloqueó solicitudes externas. Sin Riot real.
- Viewports **320, 390, 768, 1024 y 1440 px**: `scrollWidth == viewport`, foco visible de 3 px, cinco secciones y estados `partial`, `empty/insufficient`, error/reintento y nombres largos; cero errores de navegador y cero requests inesperados.
- axe a 390/1440: cero violaciones; `color-contrast` incompleto, pendiente revisión manual. No se declara conformidad WCAG.
- Capturas vigentes y datos sintéticos en [índice de capturas T04.6.2](https://github.com/D0kker/improved-train/blob/feature/t04-6-match-analysis-implementation/docs/releases/t04-6-captures/README.md); destaca [equipos azul/coral](https://github.com/D0kker/improved-train/blob/feature/t04-6-match-analysis-implementation/docs/releases/t04-6-captures/t04-6.2/04-equipos.png), [owner resaltado](https://github.com/D0kker/improved-train/blob/feature/t04-6-match-analysis-implementation/docs/releases/t04-6-captures/t04-6.2/04-owner-team.png), [vista desktop](https://github.com/D0kker/improved-train/blob/feature/t04-6-match-analysis-implementation/docs/releases/t04-6-captures/t04-6.2/01-desktop.png) y [vista móvil](https://github.com/D0kker/improved-train/blob/feature/t04-6-match-analysis-implementation/docs/releases/t04-6-captures/t04-6.2/02-mobile.png).
- Inspección visual: tintes distinguibles y suaves sobre el fondo oscuro; texto y K/D/A legibles; owner diferenciado mediante superficie y etiqueta dorada; sin defectos visuales reproducibles ni overflow observado.

### Validaciones locales del HEAD

- `npm ci`: aprobado; la instalación informa 16 avisos moderate y 1 high en el árbol total.
- `npm test`: 81/81 aprobadas.
- `npm run lint`, `npm run type-check`, `npm run build`, `docker compose config --quiet`, `git diff --check` y `npx prettier --check app/globals.css`: aprobados.
- `npm audit --omit=dev`: no pudo consultar advisories por error DNS (`EAI_AGAIN registry.npmjs.org`); no se declara un resultado de auditoría.
- No hay cambios de backend, contrato, dependencias ni llamadas Riot en `b331627`. El CI del HEAD sí ejecutó las comprobaciones web, .NET y contenedores.

### Preparación de RELEASE-06

- Compatibilidad: T04.6 no modifica API ni sus contratos; el plan de solo web contra API RELEASE-05 sigue siendo válido, sujeto a revalidar configuración y SHA antes de un despliegue autorizado.
- Rollback: se conserva el tag/ID de imagen web RELEASE-05 ya documentado en la sección 8; API permanece sin cambios. No se inspeccionaron ni cambiaron producción, Redis o volúmenes durante T04.6.2.
- No se reprodujeron defectos visuales; la confirmación sintética del tinte y responsive está disponible. La revisión manual de contraste/asistencia permanece pendiente.

**Clasificación T04.6.2: `REVIEW_REQUIRED`.** Hay capturas de `b331627`, CI y validaciones automatizadas aprobadas; falta revisión manual de contraste/asistencia y aprobación visual del Product Owner. Ningún PR se fusionó y no hubo despliegue.
