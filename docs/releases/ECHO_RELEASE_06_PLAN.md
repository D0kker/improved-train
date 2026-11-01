# RELEASE-06 — Plan de validación y despliegue candidato

**Estado:** `READY_FOR_PO_APPROVAL`
**Fecha de actualización:** 2026-11-01 UTC

**Candidato actual:** `57835f14003007008975836cd74685a430eae0de` (`feature/t04-6-match-analysis-implementation`; captura/index y runner visual, sin cambios funcionales)
**Base observada:** `main` en `16b00eb343c5f43496fb55571084e119b84d6d49`

> La revisión T04.6.2 generó capturas nuevas del commit `b331627` con fixtures sintéticos en Chromium 120 ARM64. El runner añade una captura aislada del equipo owner; no hay cambios funcionales.

## 1. PR y alcance

- [PR #102 — propuesta UX de análisis](https://github.com/D0kker/improved-train/pull/102): abierto, no fusionado, base `main`, HEAD `df983d643abc51108e2bdd358b8a55c581bfd182`, fusionable. Solo documentación y wireframes textuales.
- [PR #103 — implementación T04.6](https://github.com/D0kker/improved-train/pull/103): abierto, no fusionado, base `main`, HEAD `57835f14003007008975836cd74685a430eae0de`, fusionable y no draft. CI `ci`, run #73, concluyó `success` en web, .NET y contenedores.
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
| `npm audit --omit=dev` | Aprobado: **0 vulnerabilidades de producción**. El primer intento restringido falló por DNS; el reintento de solo lectura contra el registry oficial pasó. |
| `docker compose config --quiet` | Aprobado. |
| `git diff --check` | Aprobado; worktree de implementación permanece limpio. |

`dotnet` no se ejecutó localmente porque el diff de T04.6 es frontend/documentación; CI #73 para el HEAD final pasó web, .NET y contenedores. `npm ci` informa 17 avisos en el árbol completo (16 moderate y 1 high); el audit específico de producción reportó 0 vulnerabilidades. No se modificaron dependencias. El type-check local se repitió después de terminar `next build`, porque la ejecución paralela inicial compitió por los tipos generados en `.next`; el reintento aislado aprobó.

Los tests DOM cubren las cinco secciones, identidades incompletas, relaciones ordenadas de forma determinista, ausencia de datos, equipos parciales, carga fallida/reintento, cancelación, respuestas tardías, regreso a búsqueda y KDA finito. El cliente realiza una solicitud stateless de análisis de partida; esta tarea no añade llamadas Riot.

## 3. Validación de navegador

Validación T04.6.2 ejecutada contra el servidor Next aislado en `127.0.0.1:3216`, con Chromium 120 ARM64 extraído en `/tmp` y Playwright local del proyecto. No se instaló software global ni se tocó Docker/producción. Los fixtures son sintéticos; las rutas API se interceptaron y las solicitudes externas se bloquearon. No se usó Riot real.

- Viewports: **320, 390, 768, 1024 y 1440 px**; `scrollWidth` coincide con el ancho en cada uno.
- Secciones y escenarios `complete`, `partial`, `insufficient`, error/reintento y nombres largos comprobados; sin errores de página ni solicitudes inesperadas.
- Teclado: detalle de cobertura operable y foco visible de 3 px.
- axe en 390 y 1440 px: cero violaciones detectadas; `color-contrast` sigue como incomplete. Se completó además el cálculo manual focalizado de colores efectivos descrito en T04.6.3. Esto no certifica WCAG integral.
- Capturas actualizadas de desktop, móvil, resumen, equipos, owner, relaciones, grupos, cobertura parcial, vacío/error y nombres largos; inventario en [índice T04.6.2](https://github.com/D0kker/improved-train/blob/feature/t04-6-match-analysis-implementation/docs/releases/t04-6-captures/README.md) y [resultados del runner](https://github.com/D0kker/improved-train/blob/feature/t04-6-match-analysis-implementation/docs/releases/t04-6-captures/t04-6.2/results.json).
- Inspección visual: azul frío/cian y coral se distinguen del fondo oscuro sin saturación; nombres y estadísticas legibles; el owner conserva realce propio y etiqueta textual. La pila móvil se lee sin overflow. No se reprodujeron defectos visuales. El cálculo focalizado de contraste pasa; siguen pendientes la aprobación visual del Product Owner y cualquier evaluación asistiva/WCAG integral.
- El recorrido Home → búsqueda → dashboard → selección → análisis → regreso se mantiene validado por la ejecución Playwright anterior con respuestas simuladas. Se verificaron Riot ID/tag/región en ruta, owner PUUID/plataforma en POST y Match ID de navegación. Las llamadas active-game/ranked fueron sintéticas; no Riot real.

## 4. Hallazgos y correcciones

No se encontraron defectos visuales o funcionales reproducibles, por lo que no se alteró el CSS ni la estructura aprobada. La inspección confirma la separación visual entre azul, coral y el resaltado del owner. El cálculo focalizado de contraste pasa los umbrales aplicables a los elementos revisados; no se afirma evaluación asistiva completa ni conformidad WCAG integral. La aprobación visual final corresponde al Product Owner.

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

- Revisión y aprobación visual del Product Owner pendientes. axe conserva `color-contrast` como incomplete; el cálculo focalizado manual pasa, pero no sustituye una evaluación WCAG/asistiva integral.
- El recorrido Home → búsqueda → dashboard → análisis → regreso sí se ejecutó en navegador con APIs simuladas, como se detalla en la sección 3. No se consultó una cuenta Riot real.
- La compatibilidad descrita se aplica a API RELEASE-05 identificada; no se certifican APIs de versiones anteriores.
- `npm audit --omit=dev` reportó 0 vulnerabilidades de producción. El árbol completo conserva 17 avisos de desarrollo/transitivos; no se actualizó ningún paquete.
- No hay correcciones funcionales pendientes identificadas. Los PR #102 y #103 continúan abiertos; ninguno se fusionó.

**Estado final: `READY_FOR_PO_APPROVAL`.** Las validaciones automatizadas, navegador y contraste focalizado están aprobados; queda la aprobación visual del Product Owner. No se declara conformidad WCAG integral. RELEASE-06 no se fusionó a `main`, no se desplegó y no modificó el runtime.

## 10. T04.6.2 — validación del tinte y estado vigente

### GitHub

- PR #102 está abierto, no fusionado, base `main`, HEAD `df983d643abc51108e2bdd358b8a55c581bfd182`, fusionable.
- PR #103 está abierto, no fusionado, base `main`, HEAD `57835f14003007008975836cd74685a430eae0de`, fusionable y no Draft. El commit de evidencia contiene capturas/index y una línea de runner. GitHub Actions `ci`, run #73, concluyó `success` para el HEAD final; run #70 también aprobó el código funcional `b331627`.
- PR #104 permanece abierto como Draft, base `main`; HEAD observado antes de T04.6.3: `ebf3e00ac6298124a7aa3ebaae03ae75fc6a67c1`, CI #76 success. Esta actualización documental genera un nuevo HEAD y requiere consultar su CI antes de aprobar la rama de documentos.
- PR #97 está fusionado en `main`; no se modifica ni se despliega nada en esta tarea.

### Cambio cromático comprobado en código

El commit `b331627` aplica a `.analysis-team--blue` y `.analysis-team--red` un gradiente 145° que mezcla al 9% el token semántico azul o coral con `--surface`, y vuelve a la superficie oscura a 74%. Se conserva borde y estructura. El renglón owner usa una mezcla opaca dorada del 9% sobre `--surface`, así el acento del equipo no se superpone visualmente al resaltado del jugador consultado. El rótulo textual del owner continúa presente.

Una estimación matemática sRGB a partir de los tokens actuales da aproximadamente `#13243b` (azul), `#201f2f` (rojo) y `#1f272e` (resaltado owner). T04.6.3 calculó el contraste de los colores efectivos (incluidas transparencia y gradiente): el mínimo de texto normal fue 7.62:1 (texto muted sobre fila owner), y el mínimo de foco fue 3.06:1 (anillo cyan compuesto sobre fila owner). La franja coral de equipo mide 5.85:1 como indicador no textual. Todos superan sus umbrales revisados (4.5:1 texto, 3:1 foco/indicador). El resultado es focalizado a los elementos enumerados; no es validación WCAG integral ni muestreo de píxeles. axe aún deja `color-contrast` incomplete porque no puede resolver automáticamente todos los colores dinámicos.

### Entorno de navegador y evidencia actualizada

- Host `aarch64`. Playwright oficial no ofrece Chromium para Debian 11 ARM64; se utilizó Chromium 120 ARM64 de paquetes Debian extraídos únicamente en `/tmp`, junto con Playwright local. No hubo instalación global, contenedor activo ni modificación de producción.
- Servidor Next aislado en `127.0.0.1:3216`. El runner interceptó las rutas API simuladas y bloqueó solicitudes externas. Sin Riot real.
- Viewports **320, 390, 768, 1024 y 1440 px**: `scrollWidth == viewport`, foco visible de 3 px, cinco secciones y estados `partial`, `empty/insufficient`, error/reintento y nombres largos; cero errores de navegador y cero requests inesperados.
- axe a 390/1440: cero violaciones; `color-contrast` incompleto. El cálculo manual focalizado pasa para texto, estados, botones, stripes y foco; no se declara conformidad WCAG integral.
- Capturas vigentes y datos sintéticos en [índice de capturas T04.6.2](https://github.com/D0kker/improved-train/blob/feature/t04-6-match-analysis-implementation/docs/releases/t04-6-captures/README.md); destaca [equipos azul/coral](https://github.com/D0kker/improved-train/blob/feature/t04-6-match-analysis-implementation/docs/releases/t04-6-captures/t04-6.2/04-equipos.png), [owner resaltado](https://github.com/D0kker/improved-train/blob/feature/t04-6-match-analysis-implementation/docs/releases/t04-6-captures/t04-6.2/04-owner-team.png), [vista desktop](https://github.com/D0kker/improved-train/blob/feature/t04-6-match-analysis-implementation/docs/releases/t04-6-captures/t04-6.2/01-desktop.png) y [vista móvil](https://github.com/D0kker/improved-train/blob/feature/t04-6-match-analysis-implementation/docs/releases/t04-6-captures/t04-6.2/02-mobile.png).
- Inspección visual: tintes distinguibles y suaves sobre el fondo oscuro; texto y K/D/A legibles; owner diferenciado mediante superficie y etiqueta dorada; sin defectos visuales reproducibles ni overflow observado.

### Validaciones locales del HEAD

- `npm ci`: aprobado; la instalación informa 16 avisos moderate y 1 high en el árbol total.
- `npm test`: 81/81 aprobadas.
- `npm run lint`, `npm run type-check`, `npm run build`, `docker compose config --quiet`, `git diff --check` y `npx prettier --check app/globals.css`: aprobados.
- El runner visual modificado pasa `node --check`. Prettier sobre el runner da warning tanto en la versión base como en la actual por el formato preexistente del archivo; la única línea añadida conserva ese estilo. El índice Markdown sí pasa Prettier.
- `npm audit --omit=dev`: aprobado con 0 vulnerabilidades de producción. El árbol total de `npm ci` reporta 16 moderate y 1 high; la salida no los atribuye todos al grafo de producción.
- No hay cambios de backend, contrato, dependencias ni llamadas Riot en `b331627`. El CI del HEAD sí ejecutó las comprobaciones web, .NET y contenedores.

### Preparación de RELEASE-06

- Compatibilidad: T04.6 no modifica API ni sus contratos; el plan de solo web contra API RELEASE-05 sigue siendo válido, sujeto a revalidar configuración y SHA antes de un despliegue autorizado.
- Rollback: se conserva el tag/ID de imagen web RELEASE-05 ya documentado en la sección 8; API permanece sin cambios. No se inspeccionaron ni cambiaron producción, Redis o volúmenes durante T04.6.2.
- No se reprodujeron defectos visuales; la confirmación sintética del tinte, responsive y contraste focalizado está disponible. La revisión visual del Product Owner y la evaluación asistiva integral siguen fuera de esta validación.

## 11. T04.6.3 — cierre de calidad

### Contraste focalizado

Los valores parten de tokens en `apps/web/src/design-system.css` y reglas de `apps/web/app/globals.css`. El tinte de equipo es `color-mix(in srgb, token 9%, --surface)` en el primer stop del gradiente y vuelve a `--surface` al 74%; el primer stop es el fondo más claro y, por tanto, el peor caso para texto claro. El owner mezcla 9% de dorado sobre `--surface`. Se compuso el anillo de foco cyan al 45% alpha sobre el fondo efectivo.

| Elemento revisado                    | Primer plano / fondo efectivo              | Ratio mínimo | Umbral aplicable | Resultado             |
| ------------------------------------ | ------------------------------------------ | -----------: | ---------------: | --------------------- |
| Texto principal en equipo azul       | `#edf7ff` / `#13243b`                      |      14.41:1 |            4.5:1 | Pasa                  |
| Texto muted en equipo azul           | `#a8bacd` / `#13243b`                      |       7.87:1 |            4.5:1 | Pasa                  |
| Texto principal en equipo rojo       | `#edf7ff` / `#201f2f`                      |      14.90:1 |            4.5:1 | Pasa                  |
| Texto muted en equipo rojo           | `#a8bacd` / `#201f2f`                      |       8.14:1 |            4.5:1 | Pasa                  |
| Texto principal sobre owner          | `#edf7ff` / `#1f272e`                      |      13.94:1 |            4.5:1 | Pasa                  |
| Etiqueta owner dorada                | `#e8c56a` / `#1f272e`                      |       9.10:1 |            4.5:1 | Pasa                  |
| Acento verde/mint sobre owner        | `#61e2b4` / `#1f272e`                      |       9.40:1 |            4.5:1 | Pasa                  |
| Stripe coral de equipo               | `#f87171` / `#201f2f`                      |       5.85:1 |              3:1 | Pasa                  |
| Foco global sobre fila owner         | cyan 45% compuesto → `#377986` / `#1f272e` |       3.06:1 |              3:1 | Pasa, margen estrecho |
| Aviso amarillo sobre banner mezclado | `#fde68a` / `#1e2830`                      |      12.03:1 |            4.5:1 | Pasa                  |
| Texto de error sobre surface         | `#fda4af` / `#0b1728`                      |       9.51:1 |            4.5:1 | Pasa                  |
| Primario de botón                    | `#07101f` / `#61e2b4`                      |      11.83:1 |            4.5:1 | Pasa                  |

También se comprobaron estadísticas y etiquetas de relaciones sobre `--surface`: texto principal 16.57:1 y muted 9.05:1. Las superficies de relación/cobertura comparten ese fondo. Los enlaces usan cyan/mint y exceden 9:1 en fondos revisados. No se encontró incumplimiento demostrable que requiera corrección. Los bordes neutrales decorativos no comunican estado por sí solos; nombres de equipo, texto y etiquetas proporcionan identificación redundante.

Estos ratios se calcularon a partir de los valores CSS opacos/compuestos y los extremos del gradiente; no son muestreo fotométrico de cada píxel. El foco tiene poco margen sobre el owner, así que conviene conservar estos tokens al retocar estilos. Axe detecta cero infracciones en sus tamaños, pero deja `color-contrast` incompleto. No se afirma conformidad WCAG 2.2 AA integral.

### npm y regresión

- `npm ci`: éxito, 425 paquetes instalados; 17 avisos en el árbol total (16 moderate, 1 high).
- `npm audit --omit=dev`: éxito con **0 vulnerabilidades**. El primer intento restringido devolvió `EAI_AGAIN`; el `npm config get registry` confirmado fue `https://registry.npmjs.org/`, `.npmrc` no existía en proyecto/usuario y el reintento aprobado resolvió DNS y terminó correctamente. No se cambiaron registry ni dependencias.
- En el HEAD `57835f1`: `npm test` 81/81; lint, TypeScript, build, Compose y diff-check aprobados. TypeScript se reejecutó después del build para evitar carrera con los tipos `.next` generados.
- `node --check scripts/visual/t04-6.mjs` pasó. Prettier pasa para CSS e índice Markdown; el runner continúa mostrando la advertencia que ya mostraba su archivo base, sin formatearlo masivamente por ser un script preexistente fuera de estilo.
- CI #73 del HEAD de implementación y CI #76 del HEAD documental previo (`ebf3e00`) concluyeron success en web, .NET y contenedores; el commit documental T04.6.3 requiere verificar el run generado para su nuevo HEAD.

### Secuencia recomendada de integración (no ejecutada)

1. Integrar PR #102 primero si el Product Owner quiere conservar la especificación aprobada como documento independiente. Es aditivo y solo añade `docs/T04_6_MATCH_ANALYSIS_DESIGN.md`; no se solapa con los archivos de #103.
2. Integrar PR #103 después; contiene la implementación, pruebas, capturas vigentes y modificaciones de `docs/PROJECT_CONTEXT.md` y `docs/TODO.md`.
3. Integrar PR #104 al final, tras actualizar su base/diff. #104 también modifica `PROJECT_CONTEXT.md` y `TODO.md` y contiene capturas anteriores separadas en `t04-6-validation-captures`; reconcilia manualmente esos archivos compartidos y evita presentar sus capturas pre-tinte como evidencia vigente. No hay conflicto funcional con #103, pero sí solapamiento documental que exige reconciliación.

### Release y rollback

- Solo actualizar `web`; API RELEASE-05 es compatible y no hubo cambios backend/contratos. Aplicar el procedimiento Compose ya descrito en secciones 7–8, reconstruyendo web desde el SHA finalmente aprobado y verificando health/proxy.
- Rollback: conservar/restaurar `project-echo-release05-web:16b00eb`, image ID `sha256:241a8fe565c5f0255698d5b8ab2ab08eda5f14273b20fe894baf8a7a450ce2f7`; no cambiar API, Redis ni volúmenes.
- No hay cambios de producción. PR #102, #103 y #104 siguen sin fusionar; #104 permanece Draft. Pendiente únicamente aprobación visual del Product Owner antes de autorizar integración/release; no se atribuye la aprobación a estas capturas.

**Clasificación T04.6.3: `READY_FOR_PO_APPROVAL`.** CI, pruebas, audit de producción, responsive y contraste focalizado pasan. La aprobación visual del Product Owner sigue pendiente; no se hizo merge ni despliegue.
