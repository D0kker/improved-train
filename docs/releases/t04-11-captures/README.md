# T04.11 — Evidencia visual sintética

Capturas de Chromium ARM64 sobre build de producción local aislado (127.0.0.1:3207). Todos los jugadores, partidas y estados son fixtures; ningún request Riot real. Emblemas oficiales locales; retratos de campeón e ícono de perfil sustituidos por ilustración DEMO para no consumir servicios externos.

Se comprobaron 320, 390, 768, 1024 y 1440 px, cabecera60/40, siete métricas y campeones sin cambios, selección Solo/Flex por teclado, expansión de relaciones, KDA visible, filtros, carga5→10, errores, reintentos y ausencia de overflow. `results.json` contiene medidas y solicitudes interceptadas. La ejecución complementaria registra axe y foco en `accessibility.json`; no certifica conformidad WCAG completa ni aprobación visual del PO.

## Capturas

- [01-desktop](01-desktop.png)
- [02-header](02-header.png)
- [03-performance](03-performance.png)
- [04-relationships-expanded](04-relationships-expanded.png)
- [05-history](05-history.png)
- [06-partial](06-partial.png)
- [07-mobile](07-mobile.png)
- [08-active-game](08-active-game.png)
- [09-ten-matches](09-ten-matches.png)
- [10-empty](10-empty.png)
- [11-error](11-error.png)
- [12-active-error](12-active-error.png)
- [13-window-error](13-window-error.png)
- [14-coverage-error](14-coverage-error.png)
- [15-loading](15-loading.png)
- [16-mobile-expanded-long-text](16-mobile-expanded-long-text.png)
- [17-champions](17-champions.png)
- [18-unranked](18-unranked.png)
- [19-solo](19-solo.png)
- [20-flex](20-flex.png)
- [21-rank-error](21-rank-error.png)
- [22-both-flex-selected](22-both-flex-selected.png)
- [viewport-1024](viewport-1024.png)
- [viewport-768](viewport-768.png)

## Reproducción

Desde la raíz, con `npm ci` y `npm run build` ejecutados en apps/web y servidor propio en3207:

```bash
VISUAL_BASE_URL=http://127.0.0.1:3207 VISUAL_REFINEMENT=1 \
PLAYWRIGHT_PACKAGE=/tmp/echo-visual-browser/package.json \
CHROMIUM_PATH=/ruta/a/chromium node scripts/visual/t04-11.mjs
# Repetir con VISUAL_SUPPLEMENT_ONLY=1 para foco, texto largo y axe.
```

Playwright y axe se proporcionan externamente en un entorno temporal; no son dependencias del producto ni requieren instalación global. Las librerías de Chromium usadas en este host se extrajeron en /tmp, sin modificar servicios del sistema. Se respeta reduced motion.
