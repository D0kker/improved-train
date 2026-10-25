# T04.3 — Design System Cyber Arcade

Fecha: 2026-10-19. Rama: `feature/t04-3-design-system`, creada desde `feature/t04-2-ux-corrections` en `2eb547e5c677b2f77baf99d6fc92134d2df82c39`.

## 1. Objetivo y alcance

Se incorporó una base visual reusable para PROJECT ECHO sin cambiar el flujo de búsqueda, las consultas stateless, las estadísticas, la paginación ni las llamadas a API de las cuatro pantallas reales. Los estilos existentes ahora usan aliases semánticos para sus colores y mantienen su distribución. No se añadieron dependencias de UI ni traducciones.

La interfaz usa azul marino y negro azulado como superficies, cian y menta como acentos, dorado para cobertura parcial, violeta limitado para evidencia insuficiente, y colores semánticos para errores, equipos y jugador consultado. Los efectos de brillo son discretos; el contenido y su etiqueta siguen siendo la fuente principal para interpretar estados.

## 2. Paleta y tokens

La única definición visual está en `apps/web/src/design-system.css`. Sus variables `--echo-*` están agrupadas por color, semántica, espaciado, tipografía, radios, sombras, transición, controles, contenedores y capas z. `@theme inline` expone colores, fuentes, spacing y radios al tema de Tailwind CSS 4. Los aliases `--background`, `--surface`, `--text`, `--muted`, `--accent`, etc. preservan el CSS de las vistas existentes.

| Grupo            | Tokens principales                                                                                                                  |
| ---------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| Fondo/superficie | `--echo-ink-950`, `--echo-ink-900`, `--echo-ink-850`, `--echo-ink-800`                                                              |
| Texto            | `--echo-text`, `--echo-text-muted`, `--echo-text-subtle`                                                                            |
| Acentos          | `--echo-cyan`, `--echo-mint`, `--echo-gold`, `--echo-violet`                                                                        |
| Estados          | `--echo-success`, `--echo-danger`, `--echo-warning`, `--echo-info`, `--echo-disabled`                                               |
| Partida/equipo   | `--echo-match-victory`, `--echo-match-defeat`, `--echo-blue-team`, `--echo-red-team`, `--echo-observed`                             |
| Sistema          | `--echo-space-*`, `--echo-font-*`, `--echo-radius-*`, `--echo-shadow-*`, `--echo-transition-*`, `--echo-control-size`, `--echo-z-*` |

La fuente de cuerpo y títulos es el stack local Arial/Helvetica para evitar descargas externas. Los números estadísticos usan cifras tabulares. Los breakpoints actuales del sistema son 36rem para controles/tarjetas y 20rem para ajuste de la galería; los tokens espaciales no se usan como media-query porque CSS no admite custom properties en condiciones `@media`.

Los colores base de texto (`text`, `muted`, cyan, mint, gold, violet, success, danger, warning e info) se comprobaron con la fórmula de contraste WCAG sobre navy y surface; la relación calculada va de 8.29:1 a 17.54:1. Equipo azul y rojo mantienen más de 6.4:1 en esas superficies. Texto navy sobre menta da 11.83:1. Esta comprobación puntual no certifica WCAG 2.2 AA para todas las combinaciones, controles, foco y páginas.

## 3. Componentes y variantes

`apps/web/src/design-system.tsx` expone componentes presentacionales sin estado global ni dependencias de API:

- `Button`: primary, secondary y ghost; `disabled`, `loading`, `aria-busy`, foco y control de tipo de botón. Área mínima de 44 px.
- `Card`: default, elevated y highlighted; permite componer el contenido y no introduce un estado interactivo falso.
- `Badge`: neutral, success, warning, error, info, partial e insufficient.
- `StatCard`: label/value/description como contenido recibido por props; lista de definición y números tabulares.
- `StatusMessage`: error, warning, information, success, partial e insufficient. Título y mensaje son props, errores usan `role=alert` y los demás estados `role=status`.
- `SectionHeader`: título, descripción y acción opcional, con estructura h2.
- `LoadingState`, `EmptyState`, `Divider` y `Container` (narrow, standard y wide).
- `.ui-link`: primitiva de estilo para enlaces; `SiteHeader` existente conserva la navegación del producto.

Los textos llegan desde el componente consumidor; no hay frases de producto embebidas en las primitivas. `StatusMessage` exige etiqueta visible para que la cobertura no dependa del color. Las tarjetas y estados admiten contenido largo y usan `overflow-wrap` donde puede haber valores no separables.

## 4. Accesibilidad y responsive

Los botones son elementos nativos, alcanzables por teclado, tienen foco visible, alturas de 44 px y estado loading/deshabilitado. Los mensajes de error se anuncian; las cargas y estados informativos usan status live. Estadísticas usan `dl/dt/dd`, y se preserva jerarquía de headings. Los estados combinan color, texto y borde lateral.

El CSS incluye `prefers-reduced-motion`, rejillas que colapsan sin un ancho mínimo fijo y tratamiento de texto largo. Se probó render y eventos mediante JSDOM/Testing Library. No hay Chromium, Playwright u otro navegador de pruebas disponible en el entorno; no se ejecutó inspección visual, lector de pantalla ni prueba real en 320/360/390/768/1024/1440 px. Por ello no se afirma conformidad WCAG completa ni validación visual completa.

## 5. Galería visual

Ruta: `/design-system`. Muestra tokens/paleta, tipografía, spacing, botones, cards, badges, estados, loading, empty, estadísticas y texto largo. Utiliza valores ficticios, no tiene enlaces de navegación desde el producto y no usa API.

La ruta es dinámica y está oculta con `notFound()` salvo que `isDesignSystemGalleryEnabled` permita acceso. En desarrollo se habilita automáticamente. En producción queda cerrada por defecto y se habilita únicamente con `ENABLE_DESIGN_SYSTEM_GALLERY=true` en el entorno de ejecución del servidor web. El test cubre los valores dev/production y la bandera explícita.

## 6. Preparación multidioma

No se instaló `next-intl` ni se implementaron traducciones, selector de idioma o rutas localizadas. Los componentes reciben todos los textos por props, admiten nodos React y no concatenan fragmentos gramaticales. Las vistas consumidoras deberán entregar formatos de fecha/número regionales; la galería muestra texto ampliado y permite revisar wrap. La integración i18n permanece en T04.7.

## 7. Archivos modificados

- `apps/web/src/design-system.css`: tokens, aliases, tema Tailwind, estilos de componentes y galería.
- `apps/web/src/design-system.tsx`: primitivas de UI.
- `apps/web/src/design-system-access.ts`: condición testeable de acceso a galería.
- `apps/web/app/design-system/page.tsx`: galería ficticia protegida.
- `apps/web/app/globals.css`: sustituye colores dispersos por referencias token; no cambia distribución funcional.
- `apps/web/tests/design-system.test.tsx`: pruebas DOM de variantes, teclado, disabled, loading, contenido largo, roles y acceso a galería.
- `apps/web/package.json`, `apps/web/package-lock.json`: actualización de Next/config a 16.4.2 y fixes transitivos de seguridad.
- `docs/PROJECT_CONTEXT.md`, `docs/TODO.md`: continuidad y estado verificable del sprint.

## 8. Pruebas

Validación final desde instalación limpia: `npm ci` terminó con 426 paquetes y reportó las cinco alertas altas ya descritas; `npm test` aprobó 33/33, `npm run lint`, `npm run type-check` y `npm run build` terminaron con código 0 usando Node 24.18.0, npm 11.16.0, Next 16.4.2, React 19.2.8 y TypeScript 5.9.3. `docker compose config --quiet` y `git diff --check` también pasan.

La ruta de galería apareció como dinámica en el build; las cuatro rutas del producto siguieron compilando. Se inició el runtime standalone del build en loopback con entorno `production` y sin la bandera, y `/design-system` respondió HTTP 404; el proceso local se detuvo después de la prueba. No se modificaron API, backend, contratos, Riot endpoints ni servicios, y las pruebas no llamaron a Riot.

## 9. Triage de dependencias

En Node `v24.18.0` / npm `11.16.0`, la instalación limpia inicial informó 9 vulnerabilidades (8 high, 1 critical). El análisis de `npm audit` identificó Next.js 16.3.3 como directo/runtime; `sharp` y `source-map-js` eran transitivos; `braces`, `micromatch` y `fast-glob` pertenecían al lint de desarrollo.

Se actualizaron `next` y `eslint-config-next` juntos a 16.4.2, manteniendo el major, y se ejecutó `npm audit fix` sin `--force`. Esto actualizó `sharp` a 0.35.5, `source-map-js` a 1.2.2 y `brace-expansion` a versiones corregidas. El resultado completo es 5 high y 0 critical; `npm audit --omit=dev` reporta 0 vulnerabilidades en runtime. El aviso crítico de `next/og` documenta impacto solo cuando valores no confiables llegan a `ImageResponse`; esta aplicación no contiene importaciones `ImageResponse` ni rutas de generación OG. La actualización conserva el parche preventivo; no se usan `images.remotePatterns` ni esa función de Next.

Los 5 avisos restantes son un solo encadenamiento del lint de desarrollo: `eslint-config-next` → `@next/eslint-plugin-next` → `fast-glob` → `micromatch` → `braces@3.0.3`. La advisory actual indica que `braces <=3.0.3` está afectado y todavía no tiene versión corregida. `npm audit fix` propone degradar `eslint-config-next` a 14.2.35, incompatible con Next 16, por lo que no se aceptó. No se encontró esa rama en dependencias runtime de producción. Se debe revisar de nuevo el aviso cuando el mantenedor publique una corrección; este PR no pretende dejar `npm audit` en cero.

Fuentes: [advisory crítico de Next ImageResponse](https://github.com/vercel/next.js/security/advisories/GHSA-vcvr-r3jv-pc5j), [release Next 16.4.2](https://github.com/vercel/next.js/releases/tag/v16.4.2), [advisory de sharp](https://github.com/lovell/sharp/security/advisories/GHSA-wq5f-xc86-pv6w), [advisory de source-map-js](https://github.com/advisories/GHSA-68fv-2mgg-jv7q) y [advisory de braces](https://github.com/advisories/GHSA-vfj7-8cjw-p6xm).

## 10. Limitaciones y siguiente trabajo por pantalla

1. Home: adoptar Button, Card y Container en un cambio visual separado, conservando búsqueda, región y copy T04.2.
2. Dashboard: aplicar StatCard/Badge/StatusMessage sin alterar las cinco partidas de resumen, ventanas de historial o carga independiente de partida activa.
3. Detalle: aplicar estados de cobertura y tarjetas a participantes/equipos sin cambiar semántica de encuentros, familiaridad o posibles premades.
4. Partida activa: aplicar variantes de equipo y estados descriptivos sin revelar identidad de jugadores ocultos ni añadir polling.
5. Antes de cada rediseño, probar visualmente viewports, teclado, lectores de pantalla y contraste en navegador real.
6. Resolver el riesgo pendiente de `braces` cuando exista versión compatible; no degradar ESLint/Next para eliminar el aviso.

El siguiente sprint puede trabajar una sola pantalla reutilizando esta capa. No está implementado el rediseño de pantallas, modo alterno, i18n, feedback, autenticación o funcionalidades de análisis.

## 11. Publicación

La rama `feature/t04-3-design-system` se publicó con el commit inicial `60ede464a07afeaaeae076964eac2ed819a61bba`. El [PR #88](https://github.com/D0kker/improved-train/pull/88) está abierto hacia `feature/t04-2-ux-corrections`, sin merge. En la consulta de CI para ese commit, GitHub no devolvió workflow runs ni status checks. No hubo despliegue ni reinicio de servicios.
