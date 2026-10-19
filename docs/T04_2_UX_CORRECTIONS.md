# T04.2 — Correcciones UX prioritarias de PROJECT ECHO

Fecha: 2026-10-19. Rama: `feature/t04-2-ux-corrections`, basada en `feature/t04-1-ux-audit` (`7aad4e36a58c009e51492b67265911aff9a82d4f`). El ancestro común directo de T03.5 es `41ac5ee782fc8a24684cc2fe253b9a991b9a04f3`.

## 1. Resumen de implementación

Se implementaron correcciones acotadas en Home, perfil, detalle, partida activa y navegación común: copy alineado con el flujo stateless, región de routing visible, resumen inicial delimitado, estados de cobertura con evidencia del contrato, mensajes de error seguros, formularios accesibles, enlaces de retorno explícitos y cargas separadas/cancelables. No se cambiaron endpoints ni contratos del backend y no se incorporó T03.6. Esta rama no cambia algoritmos de análisis ni persiste resultados.

El cierre de validaciones del 2026-10-19 se registra en la sección 12. Se reprodujo el error de build dentro del sandbox, se aisló como una restricción de procesos hijos y se verificó el build con instalación limpia fuera de esa restricción. También se incorporaron pruebas de interacción DOM; la revisión visual de WCAG en navegador/lector de pantalla sigue fuera de esta evidencia.

## 2. Hallazgos atendidos

| Hallazgo | Cambio y evidencia | Estado |
| --- | --- | --- |
| H01 | Home comunica cinco partidas iniciales y carga adicional desde el perfil; perfil y detalle no prometen sincronización ni persistencia. `apps/web/app/page.tsx`, `apps/web/app/player/[gameName]/[tagLine]/player-dashboard.tsx`, `apps/web/app/match/[matchId]/match-detail-view.tsx`. | Atendido; regresión verificada en build y pruebas. |
| H02 | Home muestra la plataforma configurada; el perfil muestra el `platformRegion` devuelto por la API. Propagación a búsqueda, historial, detalle, live y enlaces. `src/platform-region.ts`, rutas de páginas, `src/api.ts`, `docker-compose.yml`. | Implementado sin selector. |
| H03 | Historial/resumen se publican al concluir búsqueda sin esperar a Spectator; estado, fallo y reintento activo son independientes. No hay polling y ambos flujos usan cancelación. Dashboard. | Atendido; test DOM mantiene la respuesta activa pendiente y verifica que el historial ya se muestra. |
| H04 | `CoverageSummary` distingue `complete`, `partial`, `insufficient`, `error` y representa solo conteos entregados por `AnalysisCoverage`; el texto de resultado vacío se mantiene distinto de cobertura incompleta. | Atendido; pruebas de helper y suite completa pasan. |
| H05 | Etiqueta “Resumen de las cinco partidas iniciales” y aclaración que bloques añadidos no recalculan métricas. | Atendido; test DOM mantiene el resumen en cinco al ampliar la lista. |
| H07 | Perfil de regreso se conserva mediante Riot ID completo en query y región; si no hay contexto suficiente, se enlaza a Home. Eliminado `history.back()` del detalle. | Implementado. |
| H08 | `fetchJson` traduce 400/404/429/502/503/504 y fallback inesperado a copy accionable; descarta el detalle técnico del body. Las vistas ofrecen reintento y conservan información previa cuando está disponible. | Implementado; test verifica 404 y no filtración del body. |
| H13 | Labels visibles, ayuda del formato, error por campo, `aria-invalid`, `aria-describedby`, foco al primero inválido, estado de navegación. Validación solo requiere campos no vacíos y no filtra caracteres permitidos del Riot ID. | Atendido; pruebas DOM de foco, asociación de error y uso por teclado pasan. |
| H14 | Skip link global, `main` identificable, navegación nombrada, foco visible existente/reforzado, texto de jugador observado. Estados no dependen solo del color. | Mejoras implementadas y skip link/nav verificados por DOM/teclado; auditoría WCAG asistiva y visual sigue pendiente. |
| H19 | Al fallar actualizar perfil/detalle/live se conservan datos previos, se marca consulta anterior y se ofrece reintento. Los controles de refresh se deshabilitan durante su solicitud. | Implementado; sin almacenamiento persistente. |

## 3. Hallazgos parcialmente atendidos o pendientes

- **H06 — cursor de partidas:** el cursor ya no depende de cantidad de filas recuperadas; avanza por ventanas de cinco solicitadas y deduplica por `riotMatchId`. Si `coverage.status` es `partial`/`error`, vuelve a intentar el mismo offset; fallos HTTP de un bloque también conservan el offset. Esto evita saltar ventanas por detalles no recuperados y no convierte una respuesta incompleta en fin de historial. El endpoint real acepta `start` y `count` y reenvía el offset a Riot (`apps/api/src/LolAnalyzer.Api/Program.cs`, `apps/api/src/LolAnalyzer.Application/Stateless/OnDemandAnalysisService.cs`). La prueba DOM del frontend simula páginas consecutivas y confirma reintento del mismo `start`, deduplicación y avance solo tras recuperar el bloque. Limitación: si un detalle falla permanentemente, el contrato no tiene una operación para excluir solo ese ID y avanzar sin perderlo; mantener el offset evita saltos pero puede impedir progreso hasta que el fallo sea recuperable. Se documenta para una tarea futura de API, sin tocar backend ni T03.6.
- **Respuesta parcial:** UI expone los campos existentes `participants`, `matchIdsObtained`, `uniqueMatchIds`, `matchesRecovered`, filas `players` y cantidad de `failures`; no presenta PUUID ni códigos internos. El contrato no incluye por cada participante una razón legible ni un cursor estable; esos detalles no se fabrican.
- **H03/H19:** solicitudes cancelables y protección por PUUID verificadas en código y en pruebas existentes; el DOM confirma que la consulta de historial no espera a Spectator. No se afirma una auditoría completa de carreras entre varios perfiles en navegador.
- **Champion names:** el DTO active-game actual expone `championId`; se mantiene el ID legible, sin resolver ni alterar contrato. Mostrar nombre oficial localizado requiere catálogo/versión locale y es trabajo posterior.
- **Accesibilidad visual:** skip link, landmarks y ARIA se inspeccionaron en código; contraste, foco en navegador/teclado, lector de pantalla y viewport móvil no están validados con browser.
- **Errores:** el mapeo por HTTP status identifica no encontrado de jugador/partida, rate limit, indisponibilidad/timeout y error inesperado. Casos con distinto código HTTP del backend pero misma categoría no pueden discriminarse desde el contrato actual sin cambiarlo.

## 4. Componentes y archivos modificados

- Páginas y flujo: `apps/web/app/page.tsx`, `app/layout.tsx`, `app/player-search-form.tsx`, `app/player/[gameName]/[tagLine]/page.tsx`, `app/player/[gameName]/[tagLine]/player-dashboard.tsx`, `app/match/[matchId]/page.tsx`, `app/match/[matchId]/match-detail-view.tsx`, `app/match/live/page.tsx`, `app/match/live/live-match-view.tsx`.
- Componentes/helpers: `apps/web/src/riot-id-search-form.tsx`, `coverage-summary.tsx`, `coverage.ts`, `platform-region.ts`, `site-header.tsx`, `player-profile-link.tsx`, `api.ts`.
- Estilos mínimos: `apps/web/app/globals.css` (skip link, estado, cobertura, formulario y responsive).
- Configuración visible de plataforma para render server-side del Home: `docker-compose.yml`; misma variable `RIOT_PLATFORM_REGION` configurada para API y web. No se reiniciaron contenedores.
- Pruebas: `apps/web/tests/frontend.test.ts`.

## 5. Cambios de texto, navegación, región y resumen

Home usa el título “¿Con quién ya habías jugado?”, explica la consulta reciente y el alcance de cinco iniciales, y llama el CTA “Buscar jugador”. El perfil identifica las estadísticas como resumen inicial y muestra que cargar otros bloques no las modifica. El detalle explica que la evidencia precede a la partida seleccionada y excluye tanto el objetivo como partidas posteriores.

La región no se infiere del Tag. Home lee `RIOT_PLATFORM_REGION` (fallback `la1`) y genera la misma URL de búsqueda con `platformRegion`; dashboard también usa región efectiva del objeto Player. Las rutas existentes conservan sus segmentos y agregan query params opcionales. No hay selector porque el usuario no puede escoger routing distinto de forma completa y segura dentro del alcance actual.

Los links de detalle incluyen retorno a perfil solo cuando están disponibles Game Name y Tag completos; los links directos caen en búsqueda. La partida activa no reconstruye contexto Riot ID desde el PUUID. La región acompaña links y requests que la aceptan.

## 6. Cobertura, errores y frescura

El helper `coverage.ts` da copy separado a los cuatro estados. El resumen muestra métricas existentes sin completar contadores ausentes. Los fallos visibles se cuentan; no se exponen identificadores ni mensajes crudos del backend. Una ausencia normal de partida activa es un estado vacío, mientras fallo de Spectator es error independiente. La familiaridad no comunica cero conocidos como certeza en cobertura no completa.

Al actualizar, el estado anterior se conserva solamente en memoria del componente; se marca como consulta anterior y se muestra la fecha de partida más reciente (no se presenta como timestamp del intento fallido). Si no hay respuesta previa no se muestra contenido fabricado.

## 7. Paginación y semántica del resumen

El offset usa ventanas solicitadas de cinco, no `matches.items.length`. La identidad de deduplicación es `riotMatchId`. Se reintenta el mismo offset después de cobertura parcial/error, se avanza una ventana ante respuesta completa y solo se termina cuando el estado y cantidad recuperada no indican bloque reintentable. El resumen y sus encounters continúan asociados a la respuesta inicial; no se suman resultados adicionales.

La prueba DOM de dashboard recorre resultado completo, bloque parcial con tres detalles recuperados, fallo HTTP temporal, repetición de IDs y agotamiento. Comprueba `start=5` hasta que la ventana se completa, no duplica las filas y después solicita `start=10`; conserva cinco partidas analizadas en el resumen. El contrato offset permite decidir la ventana siguiente; una falla de detalle persistente no permite progreso seguro y se mantiene como limitación explícita para coordinación futura con backend/T03.6.

## 8. Accesibilidad y responsive

Se añadió skip link antes del contenido, destino `main-content` enfocable, nav global con nombre, labels y errores por campo, foco al primer campo vacío, anuncio del estado de navegación, badge textual “Jugador consultado” y `aria-live`/`role=status` en cambios de estado. El `noValidate` existente se conserva y no se introducen reglas restrictivas de formato.

CSS mantiene tema oscuro/tarjetas/tipografía y agrega rejilla que colapsa en móvil, tratamiento de errores/coverage y `prefers-reduced-motion`. La evaluación WCAG, contraste computado y ejecución con teclado/lector de pantalla siguen pendientes de navegador real.

## 9. Pruebas y validación

| Verificación | Resultado |
| --- | --- |
| `npm test` | Aprobado: 28 pruebas, incluidas 7 de DOM/interacción y 21 de lógica/integración frontend. |
| `npm run lint` | Aprobado con ESLint 9.39.5 desde instalación limpia. |
| `npm run type-check` | Aprobado con TypeScript 5.9.3 desde instalación limpia. |
| `npm run build` | Aprobado con Next 16.3.3 en Node 24.18.0 tras instalación limpia. |
| `git diff --check` | Aprobado; repetir tras las últimas ediciones documentales. |
| Pruebas DOM/interacciones | Aprobadas: JSDOM, Testing Library y user-event prueban foco, ARIA, teclado, navegación, carga parcial/reintentos y carga independiente de partida activa. |
| Riot API/backend/servicios | No se consultaron; no se alteró backend, contrato ni se reiniciaron servicios. |
| `docker compose config --quiet` | Aprobado. |

## 10. Riesgos y dependencias con T03.6

Los cambios de carga en dashboard/live son frontend-only y no cambian timeouts, límites de concurrencia, servicios ni backend. El abort de una solicitud Spectator al desmontar respeta cancelación; la concurrencia global permanece responsabilidad del backend. T03.6 puede cambiar esos límites en paralelo y debe revisarse su contrato/UX antes de integrar ambas ramas. Esta rama no contiene commits de T03.6.

La variable de Compose también configura el frontend. `docker compose config --quiet` pasa. No se tocaron secretos.

## 11. Próximas mejoras priorizadas y plan por pantalla

1. **P1 — continuidad ante falla permanente de detalle:** acordar con backend/T03.6 una forma segura de avanzar sin descartar una partida ni repetir indefinidamente una ventana.
2. **P1 — validación de navegador real:** probar lectores de pantalla, contraste, responsive y flujo de actualización en browser; las pruebas JSDOM no sustituyen esa revisión.
3. **P1 — Home/Dashboard/Detalle/Live:** completar recorrido visual y lector de pantalla en navegador con estados reales simulados.
4. **P2 — consistencia global:** prueba visual responsive, contraste, focus-visible/reduced-motion y etiquetas de navegación.
5. **P3 — alcance separado:** selector regional propagado en todos los flujos, champion names localizados, i18n (`next-intl`) y pantalla de feedback quedan fuera de esta entrega. No se agrega autenticación, persistencia ni nuevos contratos en frontend.

Feedback futuro: enlace persistente en navegación/footer abriría formulario con categoría requerida (sugerencia, error, funcionalidad, general), descripción, ruta opcional y consentimiento antes de incluir datos de diagnóstico. Evitar capturar Riot ID, PUUID, tokens o partidas automáticamente; validar longitud/tipo, honeypot y rate limit en endpoint/proveedor. Recepción económica sin DB: issue privado en repositorio dedicado con gateway validado, formulario de contacto tipo Formspree/Tally o correo; elegir tras revisar privacidad, retención y moderación. No está implementado.


## 12. Cierre de validaciones

**Toolchain y causa raíz.** Validado el 2026-10-19 con Node `v24.18.0`, npm `11.16.0`, Next `16.3.3`, React/React DOM `19.2.8` y TypeScript `5.9.3`. `tsconfig.json` mantiene `strict: true`, alias `@/*` a `./*`; `npx tsc --showConfig` produce JSON válido. El lockfile final se instaló con `npm ci` y no se regeneró manualmente. En ejecución dentro del sandbox, Node no tiene permiso para crear el proceso hijo `tsc` (`spawnSync` informa `EPERM`, status 0 y stdout vacío). Next reporta ese stdout vacío como `Could not parse output from TypeScript's --showConfig`. Con la misma instalación y sin esa restricción de procesos, `npm run build` compila y termina `BUILD_EXIT_CODE=0`; no se cambiaron versiones para ocultar el error.

**Pruebas DOM y accesibilidad.** Se añadieron `@testing-library/react`, `@testing-library/user-event`, `jsdom`, `@types/jsdom` y `tsx` como herramientas de desarrollo. Siete pruebas montan React sobre JSDOM y simulan acciones del usuario: asociación `aria-invalid`/`aria-describedby`, foco del primer campo inválido, entrada válida con teclado y anuncio de carga, activación del skip link, landmark de navegación y teclado, paginación del dashboard, y render del historial mientras la consulta activa sigue pendiente. Esto valida atributos e interacciones implementadas; no es una declaración de conformidad WCAG ni reemplaza lectores de pantalla, contraste calculado o pruebas en navegadores reales.

**Paginación mockeada.** Una prueba DOM usa `fetch` simulado, sin Riot API real. Cubre: bloque completo; parcial con tres filas; IDs repetidos entre intentos; 503 temporal conservando lista y `start`; reintento completo de la ventana; respuesta insuficiente vacía y cese de solicitudes. Verifica que el resumen analizado permanece en cinco y la lista queda deduplicada. El contrato de `recent-matches` acepta offset `start` y cantidad `count` (máximo cinco), por lo que no hace falta inventar cursor en frontend. Si un detalle falla de forma permanente, el frontend conserva el offset para no saltar la partida; el contrato no da al cliente un mecanismo seguro para excluir solo ese ID y continuar. No se cambió backend por el trabajo paralelo T03.6.

**Estado H01–H20.** H01, H02, H03, H04, H05, H07, H08 y H13 están atendidos en el alcance de T04.2 y las pruebas indicadas. H14 tiene mejoras y verificación de DOM/teclado, con auditoría WCAG visual/asistiva pendiente. H19 está implementado con conservación de datos en memoria, etiqueta de consulta anterior y reintento; no se afirma una auditoría de navegador completa. H06 está corregido para avance por ventanas solicitadas, reintento de parcial/error y deduplicación, con la limitación de fallo permanente descrita. H09, H10, H11, H12, H15, H16, H17, H18 y H20 no se implementaron en este alcance; siguen en backlog o requieren validación/propuesta posterior.

**Validaciones ejecutadas.** `npm ci` aprobado con las versiones fijadas por lockfile; `npx tsc --showConfig` aprobado; `npm run type-check` aprobado; `npm run lint` aprobado; `npm test` aprobado con 28/28 pruebas; `npm run build` aprobado; `docker compose config --quiet` aprobado; `git diff --check` aprobado antes de esta edición documental y se repite antes del commit. No se llamaron servicios Riot, no se ejecutaron validaciones backend, no se reiniciaron contenedores y no se modificó backend. La instalación informó 9 avisos de auditoría del árbol npm (8 high, 1 critical); no se ejecutó `npm audit fix` ni se actualizaron versiones como parte de este cierre.

**Riesgos pendientes.** La continuidad ante una falla de detalle persistente depende de una decisión de API; la evaluación de accesibilidad visual/asistiva y responsive requiere navegador real. Los avisos de auditoría npm requieren triage aparte del sprint para identificar versiones y alcance afectados. La rama no contiene cambios de T03.6 ni secretos.
