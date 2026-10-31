# RELEASE-06 — Plan de validación y despliegue candidato

**Estado:** `REVIEW_REQUIRED`  
**Fecha de validación:** 2026-10-31  
**Candidato de código:** `ec0419cebf96f8f97818504e2d3a157926c804a8` (`feature/t04-6-match-analysis-implementation`)  
**Base observada:** `main` en `16b00eb343c5f43496fb55571084e119b84d6d49`

## 1. PR y alcance

- [PR #102 — propuesta UX de análisis](https://github.com/D0kker/improved-train/pull/102): abierto, no fusionado, base `main`, HEAD `df983d643abc51108e2bdd358b8a55c581bfd182`, fusionable. Solo documentación y wireframes textuales.
- [PR #103 — implementación T04.6](https://github.com/D0kker/improved-train/pull/103): abierto, no fusionado, base `main`, HEAD `ec0419cebf96f8f97818504e2d3a157926c804a8`, fusionable y no draft. CI del SHA exacto: workflow `ci`, run #67, `success` (web, .NET y contenedores).
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
| `npm audit --omit=dev` | 0 vulnerabilidades de producción. |
| `docker compose config --quiet` | Aprobado. |
| `git diff --check` | Aprobado; worktree de implementación permanece limpio. |

`dotnet` no se ejecutó localmente: el diff de #103 no incluye cambios de backend o contratos. Los checks web, .NET y contenedores de CI #67 pasaron para el HEAD candidato; la CI ejecutó la validación de servidor correspondiente. La instalación informa además findings en el árbol completo de desarrollo (16 moderate, 1 high); no afectan el resultado de auditoría de producción y no se modificaron dependencias.

Los tests DOM cubren las cinco secciones, identidades incompletas, relaciones ordenadas de forma determinista, ausencia de datos, equipos parciales, carga fallida/reintento, cancelación, respuestas tardías, regreso a búsqueda y KDA finito. El cliente realiza una solicitud stateless de análisis de partida; esta tarea no añade llamadas Riot.

## 3. Validación de navegador

Se ejecutó el runner existente `scripts/visual/t04-6.mjs` con Chromium ARM64/Playwright aislado y datos sintéticos, servido desde `127.0.0.1:3217`. Todas las solicitudes fuera del origen de prueba se bloquearon.

- Viewports: **320, 390, 768, 1024 y 1440 px**; `scrollWidth` coincide con el ancho en cada uno.
- Cinco secciones presentes, los dos equipos, detalles de cobertura operables por teclado y foco visible de 3 px.
- Escenarios `complete`, `partial`, `insufficient`/sin relaciones, error con reintento y nombres largos aprobados; sin errores de página ni solicitudes inesperadas.
- axe en 390 y 1440 px: 0 violaciones detectadas; `color-contrast` quedó como incomplete y requiere evaluación manual. Esto no certifica WCAG.
- Home → búsqueda → dashboard → selección → análisis → regreso no se ejecutó como un único recorrido E2E de navegador. Los pasos de búsqueda/dashboard/análisis/regreso están cubiertos por pruebas DOM existentes; la pantalla de análisis sí se ejecutó en navegador con fixture. No se utilizó Riot real.

Capturas nuevas y reporte: [índice de validación](t04-6-validation-captures/README.md). Son datos ficticios. Las capturas anteriores de PR #103 siguen en [evidencia de implementación](t04-6-captures/README.md).

## 4. Hallazgos y correcciones

No se encontró un defecto reproducible que justificara cambios de código. Se ejecutó Chromium con permisos locales después de que el sandbox impidiera abrir sockets de Chromium; el servidor Next de prueba se limitó a loopback y se detuvo tras las capturas. No se modificaron fuentes, backend ni servicios activos durante esta validación.

No se afirma prueba con Riot real, evaluación asistiva completa ni conformidad WCAG. La aprobación visual final corresponde al Product Owner.

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
- No se realizó recorrido E2E completo en navegador ni consulta con cuenta Riot real; los pasos están cubiertos por DOM tests y la vista por fixtures.
- La compatibilidad descrita se aplica a API RELEASE-05 identificada; no se certifican APIs de versiones anteriores.
- Los findings de dependencias de desarrollo permanecen; audit de producción informa cero.
- No hay correcciones funcionales pendientes identificadas. Los PR #102 y #103 continúan abiertos; ninguno se fusionó.

**Estado final: `REVIEW_REQUIRED`.** T04.6 pasa las validaciones automatizadas y visuales sintéticas para revisión; requiere aprobación visual del Product Owner y revisión manual de contraste/asistencia antes de que otra tarea autorice el release. RELEASE-06 no se fusionó a `main`, no se desplegó y no modificó el runtime.
