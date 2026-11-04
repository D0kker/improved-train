# T04.7.2 — Capturas de validación

**Revisión:** `d79f54b2c3f8d724327ee58d3278e9fa21f666d4`

**Datos:** fixtures sintéticos identificados como `DEMO`; no se consultó Riot.

**Entorno:** Chromium/Playwright ARM64 aislado, con peticiones API simuladas y tráfico externo bloqueado.

## Vista de las pantallas

Las imágenes `*-match.png` muestran resumen, tarjetas azul/coral, owner, grupos, relaciones detalladas y cobertura. Las imágenes `*-partial.png` muestran el estado de cobertura parcial en cada locale.

| Idioma             | Home 1440                   | Dashboard 1440                        | Análisis 1440                                                  | Análisis móvil 390             | Cobertura parcial 390        |
| ------------------ | --------------------------- | ------------------------------------- | -------------------------------------------------------------- | ------------------------------ | ---------------------------- |
| Español            | [Home](es-1440-home.png)    | [Dashboard](es-1440-dashboard.png)    | [Equipos, grupos, historial y cobertura](es-1440-match.png)    | [Análisis](es-390-match.png)   | [Parcial](es-partial.png)    |
| Inglés             | [Home](en-1440-home.png)    | [Dashboard](en-1440-dashboard.png)    | [Teams, groups, relationships and coverage](en-1440-match.png) | [Analysis](en-390-match.png)   | [Partial](en-partial.png)    |
| Português (Brasil) | [Home](pt-BR-1440-home.png) | [Dashboard](pt-BR-1440-dashboard.png) | [Equipes, grupos, encontros e cobertura](pt-BR-1440-match.png) | [Análise](pt-BR-390-match.png) | [Parcial](pt-BR-partial.png) |

## Vistas y escenarios adicionales

- [Equipo azul y rojo en español](es-teams.png), [inglés](en-teams.png) y [portugués brasileño](pt-BR-teams.png).
- [Vista móvil comparativa en portugués](pt-BR-390-match.png), [tablet 768 px](pt-BR-768-match.png).
- [Historial contigo](es-historial-contigo.png), [sin coincidencias](es-empty.png), [nombres largos](es-long.png), [error/reintento](es-error.png) y [grupos solapados](es-overlap.png).
- [Reporte automatizado de anchos, locales y escenarios](results.json).

Se probaron Home, dashboard, análisis y partida activa en ES/EN/PT-BR a 390, 768 y 1440 px; análisis adicional en español a 320 y 1024 px. También se probó selector, cookie persistente, navegación, recarga y fallback. `results.json` registra cero errores de página/hidratación y cero solicitudes externas inesperadas. No se declara conformidad WCAG integral; ver [revisión focalizada de contraste](contrast.md).
