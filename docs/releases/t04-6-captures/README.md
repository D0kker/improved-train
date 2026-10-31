# T04.6 — Capturas con datos simulados

Las imágenes muestran fixtures sintéticas, sin cuentas reales ni llamadas a Riot. Los retratos son ilustraciones DEMO interceptadas por el navegador. Se comprobaron 320, 390, 768, 1024 y 1440 px; el detalle de medidas, solicitudes simuladas y accesibilidad está en [results.json](results.json).

- [Vista completa de escritorio](01-desktop.png) y [móvil](02-mobile.png).
- [Resumen](03-resumen.png), [equipos](04-equipos.png), [relaciones](05-relaciones.png), [grupos](06-grupos.png) y [cobertura](07-cobertura.png).
- [Cobertura parcial](08-partial.png), [datos insuficientes](08-empty.png), [reintento tras error](08-error.png) y [nombres largos a 320 px](08-long.png).

El script comprueba navegación mediante teclado y foco en el detalle de cobertura. Axe se ejecuta en 390 y 1440 px; sus resultados no sustituyen revisión manual ni certifican WCAG. Los enlaces y datos de la captura son ficticios y únicamente sirven para revisar la composición.

Para reproducir, instalar las dependencias del proyecto, compilar y servir la app en un puerto local aislado; después ejecutar `scripts/visual/t04-6.mjs` con `VISUAL_BASE_URL`, `CHROMIUM_PATH` y, si corresponde, `PLAYWRIGHT_PACKAGE`. Playwright y Chromium se facilitan por separado, no son dependencias nuevas del producto. Todas las rutas API del navegador se interceptan y las solicitudes externas se bloquean.
