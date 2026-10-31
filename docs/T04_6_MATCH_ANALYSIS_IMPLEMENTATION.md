# T04.6 — Implementación del análisis de partida

Estado: **READY_FOR_REVIEW**, validación local completa; CI del HEAD publicado y aprobación visual del PO pendientes. La estructura y contenido fueron aprobados por el PO. Base `main` `16b00eb343c5f43496fb55571084e119b84d6d49`. La [propuesta de diseño del PR #102](https://github.com/D0kker/improved-train/pull/102) se consultó sin fusionarla.

## Composición y componentes

[`MatchDetailView`](../apps/web/app/match/%5BmatchId%5D/match-detail-view.tsx) presenta, en orden, resumen, equipos, relaciones, grupos con coincidencias históricas y cobertura. Reutiliza `Card`, `Button`, `ChampionIcon`, `PlayerProfileLink`, `CoverageSummary`, tokens existentes y el estilo Gaming Competitivo. [`globals.css`](../apps/web/app/globals.css) añade estilos acotados a esta pantalla, con equipos apilados en móvil y datos adaptables. No se modificó el dashboard.

El resumen obtiene al owner exclusivamente por PUUID dentro de los participantes disponibles. Resultado, campeón y combate se muestran solo si existen. KDA individual = `(kills + assists) / max(1, deaths)`; datos inválidos muestran raya. Los equipos utilizan participantes reales y resultados coherentes; identificadores, equipos y nombres incompletos no generan personas o derrotas ficticias.

## Relaciones e inferencias

El POST ya existente devuelve `MatchAnalysisResult.relationships`. La vista conserva esa respuesta y [`presentMatchRelations`](../apps/web/src/match-relations.ts) resuelve identidades únicamente contra los participantes de la partida, sin mostrar PUUID como nombre. El backend contabiliza partidas históricas únicas donde coincidieron ambos; `allyMatches` y `enemyMatches` son el desglose por equipo. Se preservan los valores de backend, sin sumar ventanas ni calcular puntuaciones. Orden: encuentros descendentes; empate por par canónico de PUUID en orden ordinal, únicamente como criterio interno. Identidades ambiguas o ausentes se presentan como «Jugador no identificado». No se generan enlaces a partidas históricas que el contrato no proporciona.

La sección **Grupos con coincidencias históricas** usa `premadeGroups` y los conteos de relaciones disponibles entre sus miembros. No presenta la clasificación como una certeza ni como probabilidad. Explica que la recurrencia no confirma que hayan entrado juntos. La familiaridad existente permanece visible; su porcentaje solo se presenta cuando cobertura y estado permiten interpretarlo.

## Cobertura, estados y navegación

El indicador enlaza a cobertura detallada expandible y las advertencias de parcial/insuficiente/error permanecen visibles junto a la información afectada. Una respuesta completa sin pares indica que no se encontraron coincidencias en la muestra, no que nunca existieron; resultados incompletos no se convierten en cero. La ruta directa sin owner utiliza el GET existente y explica que el análisis histórico no fue solicitado.

La carga es una petición única y no inventa progreso de fases. Se mantienen reintentos, señales de cancelación y datos anteriores identificados como tales cuando corresponda. Cambiar de partida, owner o región desmonta el estado anterior y evita mostrar resultados de otra consulta. El regreso usa la identidad completa de navegación cuando existe; en caso contrario vuelve a búsqueda. No se cambian backend, contratos, límites, caché ni ventanas históricas cinco/tres.

## Validación y limitaciones

- Instalación limpia `npm ci` en Node 24.18.0 / npm 11.16.0. `npm test`: **81/81**; `npm run lint`, `npm run type-check`, `npm run build` y `npm audit --omit=dev` aprobados (0 vulnerabilidades de producción).
- `docker compose config --quiet` y `git diff --check` aprobados. No se modificaron API/backend, contratos, dependencias ni llamadas a Riot.
- Chromium con fixtures sintéticos interceptó las rutas API y assets; no hubo tráfico a Riot. Verificó secciones y ausencia de overflow en 320, 390, 768, 1024 y 1440 px, teclado/foco, estados parcial, insuficiente, error/reintento y texto largo. Axe no detectó infracciones en 390/1440; su auditoría de contraste quedó incompleta y requiere revisión manual. Esto no certifica WCAG.
- Capturas e índice: [resultados](releases/t04-6-captures/README.md), vista completa [desktop](releases/t04-6-captures/01-desktop.png) y [móvil](releases/t04-6-captures/02-mobile.png). La primera inspección visual detectó el margen vertical global heredado; se acotó el estilo para esta página y se repitió la matriz final sin errores.

Un análisis de participantes incompletos o ventanas limitadas sigue siendo parcial; no se exponen IDs o detalles internos de errores al usuario. Falta revisión visual del PO, evaluación manual de contraste/tecnologías asistivas y CI sobre el SHA publicado.
