# T04.6 — Capturas con datos simulados

## Validación T04.6.2 — commit `b331627`

Las capturas de [T04.6.2](t04-6.2/) se generaron con fixtures sintéticos en Chromium 120 para ARM64, sin Riot API real. Corresponden al código `b331627cebf948b473b2132839fcf5426f3b2f19` más el ajuste del runner para capturar explícitamente el owner. Las solicitudes API fueron interceptadas y las externas bloqueadas.

- [Dashboard completo desktop](t04-6.2/01-desktop.png), [dashboard completo móvil](t04-6.2/02-mobile.png).
- [Resumen](t04-6.2/03-resumen.png), [equipos azul y rojo](t04-6.2/04-equipos.png), [owner resaltado](t04-6.2/04-owner-team.png).
- [Relaciones](t04-6.2/05-relaciones.png), [grupos](t04-6.2/06-grupos.png), [cobertura](t04-6.2/07-cobertura.png).
- [Cobertura parcial](t04-6.2/08-partial.png), [estado insuficiente](t04-6.2/08-empty.png), [error/reintento](t04-6.2/08-error.png), [nombres largos a 320 px](t04-6.2/08-long.png).
- [Resultados automatizados](t04-6.2/results.json): cinco viewports, sin overflow, errores ni solicitudes inesperadas; foco visible de 3 px.

La inspección visual confirma un tinte azul frío y coral discreto, texto legible y owner diferenciado con acento dorado/neutral. axe reportó cero violaciones en 390 y 1440 px; `color-contrast` quedó incompleto, por lo que estas capturas no constituyen certificación WCAG ni sustituyen la revisión manual del Product Owner.

Las imágenes muestran fixtures sintéticas, sin cuentas reales ni llamadas a Riot. Los retratos son ilustraciones DEMO interceptadas por el navegador. Se comprobaron 320, 390, 768, 1024 y 1440 px; el detalle de medidas, solicitudes simuladas y accesibilidad está en [results.json](results.json).

- [Vista completa de escritorio](01-desktop.png) y [móvil](02-mobile.png).
- [Resumen](03-resumen.png), [equipos](04-equipos.png), [relaciones](05-relaciones.png), [grupos](06-grupos.png) y [cobertura](07-cobertura.png).
- [Cobertura parcial](08-partial.png), [datos insuficientes](08-empty.png), [reintento tras error](08-error.png) y [nombres largos a 320 px](08-long.png).

El script comprueba navegación mediante teclado y foco en el detalle de cobertura. Axe se ejecuta en 390 y 1440 px; sus resultados no sustituyen revisión manual ni certifican WCAG. Los enlaces y datos de la captura son ficticios y únicamente sirven para revisar la composición.

Para reproducir, instalar las dependencias del proyecto, compilar y servir la app en un puerto local aislado; después ejecutar `scripts/visual/t04-6.mjs` con `VISUAL_BASE_URL`, `CHROMIUM_PATH` y, si corresponde, `PLAYWRIGHT_PACKAGE`. Playwright y Chromium se facilitan por separado, no son dependencias nuevas del producto. Todas las rutas API del navegador se interceptan y las solicitudes externas se bloquean.
