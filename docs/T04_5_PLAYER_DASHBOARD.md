# T04.5 — Rediseño del dashboard del jugador

## 1. Objetivo

Reordenar y actualizar visualmente el dashboard de `/player/[gameName]/[tagLine]` usando el Design System Cyber Arcade, manteniendo intactos los contratos HTTP, las consultas stateless, la paginación, la cancelación y la consulta independiente de partida activa.

## 2. Hallazgos iniciales confirmados

- La identidad Riot ID y la región se mostraban junto con acciones y una descripción larga, pero no estaban agrupadas en una superficie de perfil. Evidencia: `apps/web/app/player/[gameName]/[tagLine]/player-dashboard.tsx`.
- La API entrega `winRate` en `PlayerSummary` (`apps/web/src/api.ts` y contrato T03.5), pero el resumen renderizaba partidas, jugadores únicos/recurrentes, victorias y derrotas sin presentar esa métrica.
- La primera partida aparecía dos veces: en `LatestMatch` como tarjeta destacada y nuevamente en `MatchHistory`. El dashboard pasaba `latestMatch: null`, así que la tarjeta no tenía resultados de familiaridad que mostrar y repetía su acción.
- El historial ya incluía resultado textual, campeón, cola, K/D/A, duración, fecha y link al análisis. El link ocupaba toda la fila y su flecha no explicaba la acción.
- Existían cuatro estados de cobertura, carga inicial, errores, reintento, nota de resultados anteriores, paginación por ventanas y un estado independiente de partida activa. Estas funciones ya estaban cubiertas por pruebas DOM y no requerían cambios de contrato.
- La hoja global contenía estilos de la tarjeta de última partida y KPIs anteriores. Parte de esos estilos quedó obsoleta al retirar la tarjeta duplicada.

## 3. Diseño elegido

Se adoptó una composición de prioridad de datos:

1. Encabezado global fuera de `main`, con navegación principal.
2. Tarjeta de identidad con Riot ID, Tag, región visible, alcance de la consulta y actualización.
3. Resumen de las cinco partidas iniciales con seis métricas disponibles.
4. Estado independiente de partida activa.
5. Cobertura de la consulta inicial y del bloque más reciente, si existe.
6. Historial como único lugar donde aparece cada partida, con acción explícita para abrir el análisis.

Se eliminó la tarjeta separada de “última partida” porque repetía la primera fila sin aportar información de detalle en el flujo actual. No se modificó la ruta al detalle ni se solicitó información adicional.

## 4. Estructura y componentes

- Reutilizados: `SiteHeader`, `Card`, `Badge`, `Button`, `SectionHeader`, `StatCard`, `EmptyState`, `CoverageSummary`, `ChampionIcon`.
- `SectionHeader` acepta ahora `titleId` opcional para conectar encabezados a landmarks nombrados sin fijar copy en la primitiva.
- El encabezado de jugador tiene `h1` identificable, badge textual de región y acción de refresco con estados disabled/loading del Design System.
- Las estadísticas se presentan en `StatCard` dentro de una grilla. `winRate` procede del contrato; cuando no se analizaron partidas se muestra `—` con una descripción de falta de base de cálculo.
- Cada fila de historial conserva los datos existentes y expone resultado textual, campeón/cola, K/D/A, duración, fecha y texto de acción “Analizar partida”. Victoria y derrota no se distinguen solo por color.
- El estado vacío del historial diferencia error/parcialidad de una ventana sin partidas elegibles y aclara que no describe toda la cuenta.

## 5. Estadísticas y semántica

Se muestran únicamente campos de `PlayerSummary`: partidas analizadas, jugadores únicos encontrados, jugadores recurrentes, victorias, derrotas, win rate y participaciones de otros jugadores cuando están presentes.

El encabezado indica cuántas partidas se usaron de hasta cinco iniciales. Win rate queda como no disponible si `matchesAnalyzed` es cero. Las partidas adicionales continúan ampliando la lista, no el resumen. El backend y los cálculos no cambiaron.

## 6. Cobertura, paginación y estados

- `CoverageSummary` existente conserva los estados `complete`, `partial`, `insufficient` y `error`, con los conteos permitidos por el contrato.
- Las ventanas adicionales, sus errores y el mensaje de alcance continúan independientes del resumen inicial.
- Se preservó `loadMoreMatches`, offsets, deduplicación por Match ID, reintento de ventanas parciales/error, cancelación y condición de fin del historial.
- La consulta inicial continúa publicando el historial antes de que termine la petición de partida activa. Fallo/resultado de esa sección no oculta cobertura ni historial.
- No se agregaron endpoints, llamadas a Riot, retries automáticos, polling ni cambios en respuestas API.

## 7. Responsive

Las tarjetas de métricas usan tres columnas amplias y dos columnas a partir del breakpoint existente de 900 px, manteniendo dos columnas en móvil. La identidad se apila bajo 620 px. Las filas del historial pasan a una composición de dos columnas con resultado y datos legibles, y acción separada; nombres largos pueden envolver.

El CSS fue revisado para los anchos solicitados (320, 360, 390, 768, 1024 y 1440 px), incluyendo reglas heredadas de 900/620 px. No hay Chromium ni navegador automatizado disponible: no se verificó layout renderizado, overflow real ni se capturaron imágenes. La comprobación de responsive es estática y DOM, no una validación visual completa.

## 8. Accesibilidad

- El `SiteHeader` queda fuera del landmark `main`; se conserva el skip link global y `main-content` enfocable.
- Identidad y resumen tienen jerarquía de encabezados y regiones nombradas.
- Botones son controles nativos, usan variantes accesibles, foco visible, estado loading y disabled.
- Los enlaces de partidas son focuseables por teclado y nombran la acción; el resultado aparece como texto y Badge semántico.
- Partida activa conserva su anuncio `aria-live`; el error de su actualización usa un contenedor `status` independiente.
- Los valores largos pueden envolver; se respetan los estilos de foco y `prefers-reduced-motion` existentes.

Las pruebas DOM no certifican WCAG 2.2 AA; contraste renderizado, browser, lectores de pantalla y tecnología asistiva siguen pendientes.

## 9. Pruebas y validación

Validación en Node `v24.18.0`, npm `11.16.0`, Next.js `16.4.2`, React `19.2.8` y TypeScript `5.9.3`:

| Comando | Resultado |
| --- | --- |
| `npm ci` | Aprobado; 425 paquetes instalados desde lockfile. npm reportó un high en desarrollo, no afecta al audit de producción. |
| `npm test` | Aprobado: 42/42. Incluye perfil/región/resumen, resultado y acción por partida, teclado, cobertura vacía, paginación parcial/reintento/dedupe/fin e historial visible con partida activa pendiente. |
| `npm run lint` | Aprobado. |
| `npm run type-check` | Aprobado. |
| `npm run build` | Aprobado con Next.js 16.4.2; todas las rutas existentes aparecen en el output. |
| `npm audit --omit=dev` | Aprobado: 0 vulnerabilidades de producción. |
| `docker compose config --quiet` | Aprobado. |
| `git diff --check` | Aprobado antes del commit final; repetir antes de publicar. |

Las pruebas usan `fetch` simulado. No contactan Riot ni backend real. El build tuvo que repetirse con permisos de ejecución ampliados por la restricción de procesos del sandbox; no se alteró configuración de seguridad.

### Diagnóstico de build

En la primera ejecución confinada, Next.js recibió salida vacía al ejecutar su proceso hijo de TypeScript y mostró `Could not parse output from TypeScript's --showConfig`. `npx tsc --showConfig` ejecutado directamente produjo JSON válido (3,225 bytes, salida 0), y `npm run type-check` también pasó. El build se repitió con ejecución ampliada y finalizó con código 0, incluyendo compilación, type-check interno, trazas y las rutas existentes. La causa fue la restricción de procesos del runner, no una modificación del proyecto; no se cambió `tsconfig`, TypeScript ni Next.js.

## 10. Archivos modificados

- `apps/web/app/player/[gameName]/[tagLine]/player-dashboard.tsx`
- `apps/web/app/globals.css`
- `apps/web/src/design-system.tsx`
- `apps/web/tests/components.test.tsx`
- `docs/T04_5_PLAYER_DASHBOARD.md`
- `docs/TODO.md`
- `docs/PROJECT_CONTEXT.md`

No se modificaron API, backend, contratos, lockfile, dependencias, secretos, staging ni contenedores.

## 11. Limitaciones

- Sin navegador en el entorno, no hay capturas ni validación renderizada de los seis viewports.
- No se ejecutó auditoría asistiva manual ni se declara conformidad WCAG completa.
- Los textos/formats siguen en español y `es-PA`; la internacionalización funcional corresponde a T04.7.
- El campo de timestamp conserva la semántica existente del proyecto; no se creó una nueva marca de consulta ni persistencia.

## 12. Pendientes para T04.6

- Rediseñar de forma independiente `/match/[matchId]` sin modificar los contratos T03.5.
- Hacer más visible el jugador consultado dentro de los diez participantes cuando el contrato permita identificarlo.
- Evaluar orden, evidencia histórica, familiaridad, grupos posibles y cobertura parcial sin expresar inferencias como certezas.
- Probar regreso a perfil y entrada directa, teclado, móvil y estados complete/partial/insufficient/error.
