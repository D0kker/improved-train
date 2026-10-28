# T04.10 — Dashboard Gaming Competitivo

## Objetivo y base

Rama `feature/t04-10-competitive-dashboard`, desde `origin/main` `2c67fe5` (merge T04.5). T04.3/T04.4 ya están integradas. PR #96 continúa abierto y solo contiene documentación posterior; no se incorporó su rama. No hubo cambios de backend, endpoints, dependencias, secretos ni runtime.

## Hallazgos y decisiones del PO

La cabecera anterior repetía espacios y colocaba partida activa debajo del resumen. El resumen permanecía limitado a cinco partidas. La nueva decisión del PO sustituye esa semántica: rendimiento sobre **todas las partidas válidas cargadas**, filtros únicamente sobre el historial. Cabecera desktop 60/40, rendimiento, relaciones, historial y cobertura al final.

## Identidad visual y componentes

Se reutilizan navy y gold del sistema Cyber Arcade, Card, StatCard, Button, Badge y ChampionIcon. CSS acotado al dashboard; sin recursos externos nuevos ni animaciones. Cabecera de dos columnas 3fr/2fr que se apila a 900px. Móvil mantiene perfil, estado, rendimiento, relaciones, historial y transparencia. Se conserva el skip link, headings, regiones nombradas, botones nativos y aria-pressed para filtros.

`CompetitivePerformance`, `CompetitiveRelationships` y `CompetitiveCoverage` están separados de la coordinación HTTP. `competitive-dashboard.ts` contiene funciones puras de deduplicación, rendimiento, filtros y relaciones. La partida activa conserva su petición independiente y reintento manual, integrada solo en la cabecera.

## Perfil y alcance de datos

`PlayerLookup` no incluye profileIconId: se presenta un placeholder E propio, sin rango ni nueva consulta Riot. Se conservan Riot ID, Tag, región y timestamp de última partida de la respuesta. No se crea un timestamp ficticio de consulta.

## Fórmulas de rendimiento

- Filas válidas: Match ID presente y kills/deaths/assists finitos y no negativos. Deduplicación por Match ID, preferencia por la última fila recuperada.
- Win rate = victorias / (victorias + derrotas clasificables) × 100. Un resultado no booleano se excluye del denominador y se indica como no disponible.
- Kills, deaths y assists: media aritmética sobre todas las filas válidas.
- KDA promedio: media aritmética de `(kills + assists) / max(1, deaths)` por partida. Es una media de ratios, no un ratio de sumas. Cero muertes usa denominador 1; sin muestra se muestra —.
- Top 3: frecuencia por championId; desempate por ID ascendente. Conteos V/D y win rate son de la muestra cargada, nunca de temporada.
- El anillo SVG lleva nombre accesible con porcentaje y conteos equivalentes. Sin datos no produce Infinity/NaN.

## Relaciones y límites del contrato

`encounters` contiene PUUID y conteos agregados por ventana, no participantes por partida ni asociación PUUID→Match IDs. En ventanas disjuntas se acumulan por PUUID: únicos, recurrentes (total ≥2), participaciones y conjuntos de aliados/rivales. Estos últimos pueden solaparse y no se suman. Recurrentes ordenados por total descendente y PUUID como desempate; muestra cinco y expansión local.

Si hay Match IDs solapados entre ventanas, detalles inválidos o una respuesta de reintento que no cubre todas las filas conservadas, se omiten conteos de relaciones acumulados y se explica el motivo. No se puede garantizar deduplicación de relaciones por Match ID con el contrato actual. **Partidas compartidas identificables y sus enlaces quedan pendientes**: no existe esa información en la respuesta. No se inventan URLs. Los enlaces de perfil exigen nombre, tag y región reconocida.

## Historial y paginación

Filtros Todas/Victorias/Derrotas actúan localmente, sin modificar cursor ni estadísticas. Cada tarjeta conserva campeón, resultado textual, cola, fecha, duración y K/D/A. Acción Analizar encuentros mantiene la ruta existente. Details/summary nativo permite consultar ratio KDA e información de partida ya recibida; no carga datos adicionales.

Las ventanas se guardan por offset; reintentos sustituyen reporte de cobertura y conservan detalles válidos ya recibidos. Unión deduplicada para rendimiento e historial. Se preservan ventanas de cinco, cancelación, fin de historial, reintentos manuales del mismo bloque y protección contra solicitudes simultáneas. Actualización deshabilita carga adicional. No hay polling ni nuevas solicitudes.

## Cobertura

Estado compacto junto a métricas y advertencias visibles para muestras parciales/insuficientes/error. Detalle expandible por ventana al final: solicitadas, IDs obtenidos, recuperadas, IDs únicos y fallos según el contrato. No se suman esos contadores como si fueran partidas únicas; se muestra aparte la muestra válida deduplicada. Fallos HTTP conservan la muestra previa con aviso. Reintentos específicos solo por ventana, nunca por detalle individual: falta ese contrato.

## Archivos

- `apps/web/src/competitive-dashboard.ts`
- `apps/web/src/competitive-sections.tsx`
- `apps/web/app/player/[gameName]/[tagLine]/player-dashboard.tsx`
- `apps/web/app/globals.css`
- `apps/web/tests/competitive.test.ts`
- `apps/web/tests/components.test.tsx`
- Este informe, TODO y PROJECT_CONTEXT.

## Validación

Pruebas con fetch simulado: agregación creciente, deduplicación, desconocidos, cero muertes, KDA definido, desempates, conjuntos aliados/rivales, parcialidad, filtros, expansión y navegación local. Se adaptaron las aserciones del resumen anterior a la decisión nueva del PO, conservando las verificaciones de offsets/reintento/dedupe/fin/cancelación.

No hay navegador instalado: no hay capturas ni validación visual real de 320/360/390/768/1024/1440, contraste o tecnología asistiva; no se declara cumplimiento WCAG completo. JSDOM comprueba foco de summary y expansión por clic, no reproduce Enter nativo sobre details.

## Pendientes y recomendaciones

- Prioridad alta: futura integración que aporte asociación PUUID→Match IDs/equipo por partida para relaciones exactas con solapamientos y enlaces de partidas compartidas. Requiere contrato/backend coordinado; fuera de T04.10.
- Ícono real requiere propiedad explícita del perfil; placeholder entregado.
- Validación visual y asistiva antes de aprobar el diseño.
- No se declara completada la parte de relaciones que depende del contrato ausente. No se modificó el análisis profundo de premades.

## Resultados ejecutados

Node 24.18.0, npm 11.16.0, Next.js 16.4.2. `npm ci` completó con 425 paquetes; el primer intento confinado falló al ejecutar esbuild (EPERM), y se repitió con permisos de procesos ampliados. `npm test`: **49/49** aprobadas. `npm run lint`, `npm run type-check`, `npm run build`, `npm audit --omit=dev` (0 vulnerabilidades), `docker compose config --quiet` y `git diff --check`: aprobados. El build final incluye todas las rutas existentes. npm ci reportó un high de desarrollo; no se alteraron dependencias.

Las pruebas usan fixtures y mocks. No se consultó Riot real ni se iniciaron/reiniciaron servicios. No se añadieron peticiones HTTP: búsqueda, historial y partida activa conservan sus endpoints y presupuestos.
