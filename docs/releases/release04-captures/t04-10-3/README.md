# T04.10.3 — comparación visual

Todas las capturas usan datos y assets **sintéticos DEMO**, sin Riot real. Se obtuvieron en Chromium 156.0.8078.4/Playwright 1.64.1 sobre el build de producción de esta revisión, servido únicamente en localhost:3197. No son capturas de producción ni una aprobación del PO.

| Área | Antes (T04.10.2) | Después (T04.10.3) |
|---|---|---|
| Dashboard desktop, cinco partidas | [Antes](../01-desktop.png) | [Después](01-desktop.png) |
| Cabecera sin partida activa | [416 px de alto](../02-header.png) | [223 px de alto](02-header.png) |
| Rendimiento y campeones | [Antes](../03-performance.png) | [Después](03-performance.png) |
| Campeones horizontales | Incluidos en rendimiento anterior | [Detalle](17-champions.png) |
| Muestra ampliada a diez partidas | [Antes](../09-ten-matches.png) | [Después](09-ten-matches.png) |
| Partida activa | [Antes](../08-active-game.png) | [Después](08-active-game.png) |
| Dashboard móvil, 390 px | [Antes](../07-mobile.png) | [Después](07-mobile.png) |
| Tablet, 768 px | Ancho probado sin captura dedicada | [Después](viewport-768.png) |
| Desktop compacto, 1024 px | Ancho probado sin captura dedicada | [Después](viewport-1024.png) |
| Texto largo y expansión, 390 px | [Antes](../16-mobile-expanded-long-text.png) | [Después](16-mobile-expanded-long-text.png) |

Otras referencias: [relaciones](04-relationships-expanded.png), [historial](05-history.png), [parcial](06-partial.png), [vacío](10-empty.png), [error inicial](11-error.png), [error de partida activa](12-active-error.png), [error de ventana](13-window-error.png), [cobertura error](14-coverage-error.png) y [loading](15-loading.png). Total: 19 PNG nuevos; no se sobrescribieron los anteriores.

[Resultados y geometría medida](results.json), [teclado, texto largo y axe](accessibility.json). La región mantiene LA1 en routing pero muestra solo Latinoamérica Norte (LAN). La fecha de escritorio ocupa 21 px de alto; el ícono mide 88 px en desktop/tablet y 64 px en móvil; campeones 72/64 px respectivamente. La grilla de rendimiento tiene filas 3+4 en escritorio, sin tarjetas de relleno. Ambos paneles de cabecera tienen altura propia: a 1440 px, perfil 212 px y estado 222 px.

## Reproducir

Seguir la preparación aislada del [índice general](../README.md), utilizando este checkout y un servidor de producción de prueba en localhost:3197. Ejecutar el runner desde la raíz con `VISUAL_BASE_URL=http://127.0.0.1:3197`, `CAPTURE_DIR=docs/releases/release04-captures/t04-10-3` y `VISUAL_REFINEMENT=1`. Para el complemento, usar `VISUAL_SUPPLEMENT_ONLY=1`. Las variables de herramientas locales descritas en el índice general siguen aplicando.

El runner valida overflow, dimensiones y composición, además de los flujos anteriores; bloquea conexiones a otros orígenes. El contraste que axe marca como `incomplete`, la tecnología asistiva y la calidad de assets reales siguen requiriendo revisión manual. No se declara conformidad WCAG completa.
