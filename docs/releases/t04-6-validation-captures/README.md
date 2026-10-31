# T04.6.1 — Capturas de validación

Capturas generadas el 2026-10-31 con Chromium ARM64, datos sintéticos y la implementación candidata `ec0419cebf96f8f97818504e2d3a157926c804a8`. No se consultó Riot API.

- `01-desktop.png`: vista completa de escritorio.
- `02-mobile.png`: vista completa a 390 px.
- `03-resumen.png`: resumen de partida.
- `04-equipos.png`: equipos y participantes.
- `05-relaciones.png`: relaciones históricas.
- `06-grupos.png`: grupos con coincidencias históricas.
- `07-cobertura.png`: cobertura y transparencia.
- `08-partial.png`: información parcial/equipo incompleto.
- `08-empty.png`: sin evidencia suficiente ni relaciones.
- `08-error.png`: error recuperable.
- `08-long.png`: nombres largos.
- `results.json`: viewports, comprobaciones, requests sintéticas y evaluación axe.

Los cinco viewports (320, 390, 768, 1024 y 1440 px) no presentaron overflow. Axe no detectó violaciones en 390/1440; `color-contrast` queda incompleto y necesita evaluación manual. Esta evidencia no declara conformidad WCAG.
