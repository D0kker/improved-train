# T04.1 — Auditoría UX/UI de PROJECT ECHO

Fecha de revisión: 2026-10-18 (Panamá). Repositorio: `D0kker/improved-train`.
Base inspeccionada: `origin/feature/t03-5-stateless-api`, commit `41ac5ee782fc8a24684cc2fe253b9a991b9a04f3`.
Rama de entrega: `feature/t04-1-ux-audit`; destino del PR: `feature/t03-5-stateless-api`.

## 1. Resumen ejecutivo

El frontend permite buscar un Riot ID, consultar cinco partidas iniciales, cargar bloques adicionales, analizar una partida terminada y consultar una partida activa. La identidad visual ya tiene superficies oscuras, tarjetas, acento menta y estados semánticos. El mayor valor está en explicar encuentros y evidencia histórica; conviene corregir esa explicación antes de invertir en Cyber Arcade.

Los problemas prioritarios son textos incompatibles con T03.5 (persistencia y sincronización de 20), cobertura reducida a avisos genéricos, ausencia de región en búsqueda y contexto poco claro al cargar más partidas. El detalle conserva una buena cautela sobre premades, pero oculta fechas y parte de los conteos disponibles. La partida activa presenta identificadores técnicos y estados de recuperación poco específicos.

Esta entrega solo analiza y documenta. No cambia pantallas, estilos, API, backend, dependencias, traducciones, secretos ni servicios. Las propuestas son candidatas para historias posteriores; no son decisiones arquitectónicas aceptadas ni autorización de implementación.

### Método y límites de evidencia

- Inspección de las cuatro páginas, layout raíz, todos los componentes locales y utilidades de `apps/web`, CSS, pruebas, proxy y contratos/flujo stateless. Contraste con [T03.5](T03_5_STATELESS_ARCHITECTURE.md), [decisiones](DECISIONS.md) (D-042), [contexto](PROJECT_CONTEXT.md), [roadmap](TODO.md) y [especificación](../lol_network_analyzer_spec.md), especialmente pantallas, familiaridad, premades, seguridad y errores.
- **C — Confirmado en código:** comportamiento o ausencia verificable estáticamente. No implica reproducción en navegador.
- **R — Recomendación:** juicio de diseño o propuesta de producto, pendiente de validación con usuarios.
- **V — Validación pendiente:** hipótesis sobre representación, interacción o tecnología asistiva que requiere navegador.
- No hay navegador de inspección disponible en esta sesión. No se realizaron capturas, pruebas visuales, teclado, lector de pantalla, mediciones de contraste renderizado ni evaluación con jugadores. Las distribuciones se describen desde JSX/CSS; no se declara conformidad WCAG.
- No se consultaron jugadores reales, Riot protegido ni datos sensibles. No se usaron los contenedores como evidencia de esta rama: pueden pertenecer a otra sesión. No se desplegó ni se reinició nada.
- Aislamiento: worktree limpio en `/tmp/improved-train-t04-1`, creado tras `git fetch origin`; la sesión principal de T03.6 permanece fuera de este análisis. No se integran sus cambios ni se presupone que resuelva estos hallazgos.

## 2. Inventario de pantallas

Solo existen cuatro rutas de página. Las rutas mencionadas en el diseño inicial no prueban implementación.

| Ruta real | Propósito y componentes | Composición y navegación |
| --- | --- | --- |
| `/` | Búsqueda. `Home`, `PlayerSearchForm` | Hero centrado, marca E, título, explicación, dos campos Riot ID, CTA y tres notas. Envía al perfil; no hay listado de resultados en Home. |
| `/player/[gameName]/[tagLine]` | `PlayerPage`, `PlayerDashboard`; internos `LoadingPanel`, `SummaryCards`, `ActiveGameBanner`, `LatestMatch`, `MatchHistory`, `EmptyState` | Topbar, identidad/actualización, avisos, cinco métricas, banner activo condicional, tarjeta última partida, historial y botón de cinco más. Links a Home, detalle y live. |
| `/match/[matchId]` | `MatchPage`, `MatchDetailView`; internos `MatchLoading`, `FamiliaritySummary`, `PremadeSummary`, `TeamCard`, `ParticipantRow`, `HistoryBadge` | Topbar, volver, estados, cabecera/fecha/duración, familiaridad, grupos y conocidos, dos equipos. Links a perfiles. Acepta `ownerPuuid` y `platformRegion` opcionales. |
| `/match/live` | `LiveMatchPage`, `LiveMatchView`, interno `LiveParticipant` | Topbar, volver, carga/error, cola/inicio/duración y dos equipos. Requiere `ownerPuuid` en query para recuperar datos. Links a perfiles visibles. |

Fuentes de páginas: [Home](../apps/web/app/page.tsx), [búsqueda](../apps/web/app/player-search-form.tsx), [página de jugador](../apps/web/app/player/[gameName]/[tagLine]/page.tsx), [dashboard](../apps/web/app/player/[gameName]/[tagLine]/player-dashboard.tsx), [página de partida](../apps/web/app/match/[matchId]/page.tsx), [detalle](../apps/web/app/match/[matchId]/match-detail-view.tsx), [página live](../apps/web/app/match/live/page.tsx), [vista live](../apps/web/app/match/live/live-match-view.tsx).

### Layout, soporte y código no montado

| Pieza real | Estado y alcance |
| --- | --- |
| [Layout raíz](../apps/web/app/layout.tsx) | Único `layout.tsx`; `html lang="es"`, metadata fija y CSS global. Sin shell compartido de navegación ni footer. Las topbars se repiten en tres vistas. |
| [CSS global](../apps/web/app/globals.css) | Tokens, tarjetas, formularios, estados, equipos, estilos de red y breakpoints 900/620 px. Algunas clases heredadas no implican componentes activos. |
| [ChampionIcon](../apps/web/src/champion-icon.tsx) | Usado en historial y detalle; imagen same-origin, alt localizado fijo e iniciales de fallback. |
| [PlayerProfileLink](../apps/web/src/player-profile-link.tsx) | Usado en detalle, live y red heredada; crea enlace solo con nombre y tag completos; fallback de identidad incompleta. |
| [PlayerNetworkSection](../apps/web/app/player/[gameName]/[tagLine]/player-network-section.tsx) | Código existente con grafo, filtros y tabla; no importado/montado por el dashboard. No es pantalla navegable actual. Su endpoint está retirado en T03.5. |
| [API cliente](../apps/web/src/api.ts), [formatos](../apps/web/src/format.ts), [ECHO](../apps/web/src/echo.ts), [grupos](../apps/web/src/premade-groups.ts), [red](../apps/web/src/network.ts) | Tipos, paths y helpers; algunos de sync/red/relaciones son heredados y no deben interpretarse como acciones disponibles. |
| [Data Dragon](../apps/web/src/data-dragon.ts), [proxy de imágenes](../apps/web/app/api/assets/champions/[championId]/route.ts) | Catálogo `en_US`, versión fallback `16.17.1` configurable y PNG cacheado. Son soporte de assets, no páginas. |
| [Health route](../apps/web/app/api/health/route.ts), [health helper](../apps/web/src/health.ts) | Diagnóstico técnico, no pantalla de usuario. |
| [Next config](../apps/web/next.config.ts), [cabeceras](../apps/web/src/security-headers.ts) | Proxy same-origin `/api/v1` y `/openapi`, timeout 210 s, CSP restrictiva. No son rutas visuales. |
| [Pruebas frontend](../apps/web/tests/frontend.test.ts), [pruebas health](../apps/web/tests/health.test.ts), [package](../apps/web/package.json) | Pruebas de helpers/contratos, sin suite visual ni de interacción DOM. No hay infraestructura i18n instalada. |

**No existen** páginas separadas `/player/.../matches`, `/repeated`, `/relationships`, `/network`, `/relationship/{A}/{B}` ni `/feedback`. Tampoco hay `loading.tsx`, `error.tsx`, `not-found.tsx` propios, selector de idioma, icono/nivel de perfil ni ranking de conocidos montado. No añadir enlaces a estas superficies como si estuvieran disponibles.

## 3. Problemas identificados

Prioridad: P1 = corregir antes de la aplicación del nuevo diseño; P2 = siguiente mejora controlada; P3 = evolución. P1 no significa incidente crítico de seguridad. Esfuerzo relativo S/M/L, sin compromiso de fechas.

| ID | Tipo / prioridad | Evidencia e impacto | Recomendación |
| --- | --- | --- | --- |
| H01 | C / P1 | Home promete “Hasta 20 partidas por sincronización” y partidas “guardadas”; perfil dice “almacenadas” y error de detalle “puede no estar guardada”. T03.5 no persiste análisis. Fuentes: Home, dashboard y detalle. | Copy de cinco iniciales, consulta bajo demanda y ventana de evidencia; retirar promesas de almacenamiento. |
| H02 | C / P1 | Búsqueda no muestra ni envía región; `playerSearchPath` no la admite, aunque API acepta `platformRegion` y usa configuración por defecto. “LAN” es placeholder de tag, no selector de plataforma. | Mostrar el alcance configurado; evaluar selector con propagación coherente, sin inferir servidor desde tag. |
| H03 | C / P1 | Dashboard espera `active-game` después de obtener búsqueda y antes de `setData`; no publica primero historial/resumen. | Diseñar carga independiente del bloque activo para no bloquear resultados ya disponibles; implementación posterior debe coordinarse con T03.6. |
| H04 | C / P1 | Solo se guarda `coverage.status` en dashboard/detalle, no `coverage.players`, conteos o fallos. Un aviso puede sustituir otro: cobertura sobrescribe fallo de partida activa. | Avisos por sección y cobertura expandible; diferenciar error, parcial, insuficiente y completo. |
| H05 | C / P1 | `loadMoreMatches` amplía lista pero conserva `summary` y estado de cobertura inicial. El usuario ve diez o más filas junto a métricas de las primeras cinco. | Rotular explícitamente “Resumen de las 5 iniciales”; mantener la semántica actual, sin recalcular unión ni prometer un nuevo análisis. |
| H06 | C / P1 | Offset usa número de filas recuperadas/deduplicadas. Si fallan detalles, puede volver a consultar IDs ya pedidos; `hasMore` se basa en cinco filas y no en continuidad confirmada. Fuentes: dashboard y `RecentAsync`. | Especificar navegación que distinga ventana consultada de filas recuperadas y reintento de ventana parcial. Reproducir con mock antes de elegir solución sin cambiar contrato. |
| H07 | C / P1 | Detalle vuelve con `history.back()` y no tiene destino explícito de fallback. Una entrada directa puede salir del sitio o no regresar al perfil. | Retorno determinista cuando exista contexto de origen; fallback visible a búsqueda en enlaces directos. |
| H08 | C / P1 | `errorMessage` propaga `detail/message/title`; backend produce títulos en inglés y vistas también muestran `Error.message`. No existe catálogo amigable por estado. | Mensajes por operación/HTTP, fallback traducible y acción apropiada; no mostrar mensajes técnicos genéricos del navegador como copy final. |
| H09 | C / P2 | `MatchKnownPlayer` incluye `playedBefore`, fechas y V/D; tarjetas muestran solo aliados/rivales, y tooltip habla de “owner”. | Evidencia expandible con cuántas veces, última fecha, primera opcional y significado de los resultados. |
| H10 | C / P2 | Premades muestran Pn y “evidencia alta” sin explicación de factores; se imprime `Equipo 100/200`. `relationships` existe en respuesta pero se descarta. | Etiquetas humanas y explicación cauta basada en datos disponibles; nunca convertir score/confianza en probabilidad de amistad. |
| H11 | C / P2 | Live muestra número de campeón, fragmento de `summonerId`, “Spectator-V5” y puede repetir tag en la segunda línea. | Nombre/asset oficial si disponible; “Identidad no disponible” sin fragmentos técnicos y sin reconstrucción de identidades ocultas. |
| H12 | C / P2 | Live convierte ausencia de partida en error, no tiene botón de actualizar en éxito y conserva `game` anterior ante excepción de una carga posterior. Duración es snapshot. | Estado normal “No hay partida activa”, fecha de consulta y actualización manual sin polling; distinguir snapshot anterior de resultado nuevo. |
| H13 | C / P2 | Hay labels y alerta global en búsqueda, pero no `aria-invalid`, asociación error-campo ni gestión de foco. | Error contextual accesible; foco al primer campo inválido; helper visible sobre formato de Riot ID. |
| H14 | C / P2 | No hay skip link ni landmark `nav`; marca móvil queda en “E”; fila observada destaca principalmente por color/borde. | Acceso al contenido, nombre accesible completo de marca e insignia textual “Jugador consultado”. |
| H15 | V / P1 | Riesgo de overflow en familiaridad flex no apilada y Riot IDs largos; reglas móviles de participantes mantienen columnas que merecen prueba. | Verificar 320/360/390 px y zoom antes de declarar responsive validado. |
| H16 | C + V / P2 | Reduced motion ralentiza spinner, pero no lo detiene; no elimina transform de `latest-match-card`. Contraste/foco no medidos. | Reducir movimiento no esencial; comprobar contraste y visibilidad de foco con herramientas de navegador. |
| H17 | C / P2 | Idiomas mezclados, `lang=es`, formatos `es-PA`, catálogo `en_US`, errores backend ingleses y sin selector. | Extraer mensajes/formatos con locale explícito, después integrar idioma por pantallas. |
| H18 | R / P2 | Cinco KPIs, CTA de última partida y la misma partida en historial compiten; varios espacios verticales de 4rem aumentan recorrido móvil. | Priorizar explicación de encuentros y acceso a partida; evaluar densidad con usuarios. |
| H19 | C / P2 | Ante actualización fallida dashboard/detalle conservan datos previos; no hay etiqueta “resultado anterior”. Reintentar no se deshabilita en todas las vistas. | Mostrar vigencia y estado de actualización sin sugerir que los datos viejos son nuevos. Coordinar solicitudes con T03.6, sin implementar protección aquí. |
| H20 | C / P3 | No existe vía de feedback ni navegación de ayuda global. | Página futura sencilla y recepción explícita; ver sección 13. |

## 4. Evaluación UX por pantalla

### Home y búsqueda

**Función actual (C):** recibe Game Name y Tag separados por `#`; trim y codificación de segmentos; valida solo que no estén vacíos, con límites de entrada de 64/16. `noValidate` desactiva validación nativa. No hay selector de región, resultados, sugerencias, historial de búsquedas ni estado de envío. Navega inmediatamente al dashboard; el error de identidad llega allí. No asumir que los máximos de input representan las reglas actuales oficiales de Riot ID.

**Jerarquía (R):** el titular inglés es dominante y la explicación técnica secundaria es extensa. Se recomienda una pregunta en español sobre encuentros, ejemplo ficticio `Jugador#TAG` y aclaración de región; CTA “Buscar jugador”. La nota sobre el navegador es un detalle de implementación que puede ir en ayuda. Mantener claro que el análisis usa partidas terminadas y una muestra acotada.

**Estados:** inicial y campos incompletos implementados; búsqueda en progreso, no encontrado, servicio no disponible y resultados pertenecen al dashboard. Proponer indicación de transición sin progreso inventado. En móvil el botón baja a otra fila a 900 px; los campos siguen juntos a 620 px: comprobar legibilidad del tag y teclado con 320 px (V).

### Dashboard del jugador

**Función actual (C):** una búsqueda autocontenida devuelve identidad, resumen, cinco partidas, encuentros y cobertura. La UI usa identidad, resumen, lista y estado global. Después consulta partida activa. No hay consultas sync/summary/known-players encadenadas. No se muestran icono, nivel, win rate ni lista de recurrentes; el contrato de resumen sí incluye win rate, pero no icono ni nivel. Agregarlos requeriría evaluar alcance, no asumir datos existentes.

**Orden actual:** identidad y “Actualizar 5 partidas” → aviso → cinco KPIs (analizadas, únicos, recurrentes, victorias, derrotas) → partida activa si existe → última partida → historial. Las cinco filas iniciales muestran resultado textual, campeón/icono, cola, K/D/A, duración y fecha. Todas son enlaces. “Cargar 5 partidas más” añade filas en la misma pantalla; no hay páginas adicionales separadas, numeración ni navegación anterior/siguiente.

**Estados implementados:** carga con spinner; fallo global con reintento; advertencia de cobertura/consulta activa; cero partidas; refresco con datos anteriores; carga adicional deshabilitada mientras se procesa. La carga real pasa a `sync`, aunque existen textos de lookup y results que no se recorren como etapas de backend. No presentar tres fases medidas ni porcentaje: la respuesta es síncrona y no tiene progreso granular.

**Riesgos de interpretación:** cero partidas recuperadas por error y ausencia real comparten componentes de vacío; `coverage.status=error` puede acompañar resumen cero. El mensaje “Actualiza ... cuando la API ... esté disponible” presume la causa. La tarjeta última partida siempre recibe `latestMatch=null`, por lo que no muestra un análisis de familiaridad ya calculado. No interpretar ese placeholder como funcionalidad disponible.

**Recomendación (R):** mostrar pronto lo recuperado, aviso junto a la sección afectada, resumen con denominador fijo, detalle de cobertura opcional y luego historial. Conservar la última partida como acceso compacto o integrarla como primera fila destacada, evitando duplicación prominente. La actualización vuelve a las cinco iniciales: explicarlo y comprobar que no destruya inesperadamente el contexto del usuario.

### Detalle de partida terminada

**Función actual (C):** con owner ejecuta una sola operación `POST .../analyze`; no depende de una búsqueda anterior. Obtiene partida objetivo y evidencia anterior de cinco históricas del owner/tres de cada otro participante. Sin owner usa lectura de compatibilidad y no dispone del contexto de familiaridad. La cabecera muestra cola, fecha y duración; no identifica visualmente al jugador de referencia por su Riot ID.

**Participantes/equipos:** itera participantes devueltos, sin inventar diez filas cuando faltan; dos columnas en escritorio y una a 900 px. Muestra campeón, Riot ID navegable cuando está completo, K/D/A, antecedentes y Pn. `teamPosition` está tipado pero no se representa. No hay estadísticas de objetivos, oro, daño o ítems en este contrato; no diseñar secciones con datos inexistentes. El resultado del equipo se deriva de `participants.some(win===true)`: ausencia de victoria o de filas puede etiquetarse derrota sin evidencia suficiente; revisar como caso de dato incompleto antes de rediseñar.

**Familiaridad:** muestra evaluables, conocidos y porcentaje únicamente si cobertura completa y familiaridad `Available`; de lo contrario guion y explicación. Es una buena protección contra interpretar parcial como cero. Mantener el denominador (normalmente otros nueve, condicionado a identidades visibles), el punto de vista del owner y el límite temporal. “Completo” describe la ventana solicitada, no todo el historial de la cuenta.

**Premades:** se conserva lenguaje inferencial y advertencia; código Pn y miembros permiten distinguir grupos sin depender solo del color. Vacío dice “No hay evidencia suficiente”, una formulación prudente. No significa “no existen premades”. Separar este bloque de “Jugadores conocidos”: coincidir con el owner y formar un posible grupo son conceptos diferentes.

**Estados:** carga, error con reintento, parcial/insuficiente/error de cobertura y sin contexto del owner. Hay vacío por equipo y por grupos; no hay vista global dedicada para participantes incompletos. Proponer resumen compacto de estado, aviso específico al faltar owner, diez participantes cuando estén disponibles, luego evidencia expandible. No esconder que faltan identidades ni rellenarlas con inferencias.

### Partida activa

**Función actual (C):** existe y se enlaza desde dashboard cuando la consulta devuelve una partida. Recupera un snapshot al montar; no calcula familiaridad ni premades, no hace polling y no tiene marcador temporal en vivo. Equipos 100/200 se presentan azul/rojo y se cuenta lo recibido, sin garantía visual de diez. No hay historial ni K/D/A.

**Estados:** cargando; falta owner; partida ya no disponible tratada como error; fallo de servicio con mismo texto de posible finalización; identidad incompleta con fallback. No hay vacío de equipo explícito ni botón de refresco en éxito. Recomendación: distinguir fin normal de error técnico y mostrar “Consultado a ...”; no animar un reloj como si fueran datos actualizados.

**Límite de producto:** mantener esta superficie informativa ya existente, sin añadir análisis competitivo, recomendaciones durante partida ni resolución de jugadores ocultos. La política oficial de [integridad de juego de Riot](https://developer.riotgames.com/docs/lol#game-integrity) prohíbe identificar/analizar jugadores deliberadamente ocultos. La presencia de fragmentos de ID no prueba por sí sola una infracción, pero no aporta valor de lectura; sustituirla por una ausencia explícita en una historia futura. Cualquier ampliación de live necesita nueva revisión de cumplimiento.

## 5. Evaluación de los textos

| Texto o patrón actual | Problema | Copy candidato en español (R) |
| --- | --- | --- |
| “Who have you played with before?” / “ECHO overview” | Mezcla de idioma. | “¿Con quién ya habías jugado?” / “Resumen de encuentros”. |
| “Hasta 20 partidas por sincronización” | Contradice ventana inicial y runtime. | “Consulta tus últimas 5 partidas; puedes cargar más en bloques de 5”. |
| “partidas guardadas/almacenadas” | Promesa de persistencia inexistente. | “partidas recuperadas para esta consulta”. |
| “Analizar jugador” | Puede sugerir que analiza todo el historial. | “Buscar jugador”, acompañado de alcance de cinco iniciales. |
| “Partidas analizadas” | No aclara ventana al ampliar lista. | “Partidas del resumen inicial” y “5 recuperadas de 5 solicitadas”, según datos. |
| “Jugadores únicos encontrados” | Puede confundirse con participaciones. | “Jugadores distintos” con ayuda que excluye owner y deduplica. |
| “Jugadores recurrentes” | Métrica sin acceso a quiénes son. | “Se repiten en 2 o más partidas”, con explicación o lista futura de `encounters` de esta consulta. |
| “owner” / “Equipo 100” / “Spectator-V5” | Jerga interna. | “jugador consultado” / “Equipo azul” / “Identidad no disponible”. |
| “evidencia alta” | Puede parecer certeza o probabilidad. | “Evidencia histórica alta”, con explicación de límites; no “dúo confirmado”. |
| “Ya jugó junto” / “como equipo” | Falta sujeto de referencia. | “Fue tu aliado” / “Encuentros como aliado del jugador consultado”. |
| K / D / A | Conocido en gaming, pero sin expansión. | Mantener K/D/A con ayuda “Asesinatos / muertes / asistencias”. |
| Error que presupone partida guardada o finalizada | Diagnóstico no sustentado. | Separar “No encontramos la partida”, “El servicio no respondió” y “No hay partida activa”. |

Preferir primera persona solo si se explica que el perfil puede ser de otro jugador. No traducir Riot ID, PUUID interno, Match ID ni códigos Pn; sí traducir etiquetas, accesibilidad, metadata, vacíos y errores. Las cifras deben tener denominador y ventana. Un tooltip no basta para contenido esencial: la evidencia necesita acceso por toque y teclado.

## 6. Distribución recomendada

Propuesta conceptual, sin nuevo layout implementado:

```text
Home:       Marca → propósito → Riot ID + alcance/región → buscar → límites breves
Jugador:    Navegación → identidad + región → estado de consulta
            → resumen de 5 iniciales → partida activa (si existe)
            → historial con primera partida destacada → cargar 5 más
Terminada:  Volver / búsqueda → contexto owner + cola/fecha/duración
            → familiaridad + cobertura → equipo azul / equipo rojo
            → conocidos y evidencia → posibles premades y explicación
Activa:     Volver / búsqueda → contexto + momento de consulta
            → equipo azul / equipo rojo → actualizar manualmente
Feedback:   Navegación → tipo y mensaje → privacidad → enviar → acuse real
```

En detalle, probar con usuarios la evidencia debajo de equipos frente al orden actual: los participantes permiten ubicar la información antes de leer inferencias. No separar en tabs que oculten errores importantes. Métricas y advertencias deben ser visibles antes de expandir explicaciones. No introducir dashboards de rendimiento, gráficos globales ni más estadísticas sin datos/alcance explícito.

## 7. Recomendaciones de navegación

- Reutilizar una cabecera compacta con marca nombrada, “Buscar jugador” y futura ayuda/feedback. Navegación pequeña, sin barra lateral vacía ni enlaces a rutas planificadas.
- Conservar `/player/...`, `/match/...` y `/match/live`; distinguir estática `live` de parámetro dinámico en cualquier futura estrategia de rutas.
- Hacer explícito “Volver al perfil” cuando se conozca el origen; si no existe Riot ID de origen, ofrecer búsqueda. No resolver navegación guardando un corpus o reconstruyendo identidades.
- Mantener `ownerPuuid`, Match ID y plataforma correctos al navegar. Hoy `matchDetailPath` lleva owner, pero no plataforma; backend puede inferirla del Match ID. No afirmar que toda selección de región ya se propaga correctamente a live/perfiles.
- “Cargar más” es expansión en la misma página. Preservar scroll/foco y anunciar cuántas filas se añadieron; documentar que el resumen no se amplía.
- Incorporar enlace a contenido principal. No indexar perfiles automáticamente ni activar hreflang/SEO público por añadir idiomas: sigue pendiente el go/no-go público.

## 8. Recomendaciones responsive

**Base existente (C):** widths fluidos y tipografía `clamp`; dos equipos apilan a 900 px; KPIs pasan de cinco a dos y luego uno a 620 px; cada partida pasa de grilla horizontal a tarjeta vertical. Identidad principal permite `overflow-wrap:anywhere`. Son decisiones útiles que se pueden conservar.

**Riesgos pendientes (V):** nombres largos en enlaces secundarios y miembros de grupos; `.familiarity-summary` sigue flex horizontal; formulario deja nombre/tag en una fila estrecha; cabeceras y botones largos en portugués; la grilla móvil de participante tiene una columna de 3.5rem además de identidad que contiene icono y texto. No hay evidencia renderizada que permita declarar clipping real.

Plan de aceptación futura: 320, 360, 390, 768, 1024 y 1440 CSS px; zoom 200% y 400%; etiquetas expandidas 30–40%; Riot IDs sintéticos largos, cifras grandes, fechas ausentes, diez participantes y grupos solapados. Evitar desplazamiento horizontal global, preservar orden de lectura y controles alcanzables. En móvil preferir filas de estadísticas compactas, equipo con heading visible y detalles expandibles. No depender de hover.

Referencia: [WCAG 1.4.10 Reflow](https://www.w3.org/WAI/WCAG22/Understanding/reflow.html); evaluar contenido a 320 CSS px y excepciones aplicables, sin considerar que toda tabla permite overflow indiscriminado. El `min-width:59rem` de tablas existe en CSS, pero la tabla de red no está montada: no es un defecto confirmado de estas cuatro páginas.

## 9. Accesibilidad

Objetivo propuesto: WCAG 2.2 AA. No es una certificación del estado actual.

| Área | Evidencia actual | Aceptación propuesta |
| --- | --- | --- |
| Semántica | `main`, headings, secciones, `dl` para KPIs y listas para participantes. Topbars repetidas sin `nav`. | Jerarquía h1/h2 coherente también en carga/error, landmarks y skip link. |
| Formularios | Labels envolventes y alerta presentes. Sin `aria-invalid` ni descripción del error por campo. | Error asociado, instrucción de formato y foco al campo; prueba solo con teclado. |
| Cambios de estado | `role=status`, `role=alert`, algún `aria-live=polite`; conteo de nuevas filas no anunciado. | Anuncio breve al completar/refrescar, `aria-busy` por región y foco estable; evitar alertas duplicadas. |
| Color y significado | Victoria/derrota textual y códigos Pn son fortalezas. Owner depende del destacado visual. | Etiqueta textual de jugador observado; grupos con código y leyenda, nunca solo tono. |
| Contraste/foco | CSS define foco visible de 3 px con alpha; texto secundario pequeño. Sin medición renderizada. | Medir 4.5:1 para texto normal, 3:1 para texto grande y contraste no textual aplicable; foco sin quedar oculto. |
| Objetivos táctiles | Botones principales de 3.6rem; links compactos y volver sin área mínima explícita. | Comprobar mínimo de 24×24 CSS px o excepciones/espaciado WCAG; preferir 44 px como objetivo de comodidad, no confundirlo con requisito AA universal. |
| Movimiento | Spinner infinito y hover con transform; reducción parcial. | Alternativa estática de carga y retirada de movimiento no esencial con preferencia reducida. |
| Idioma y assets | `lang=es` fijo; alt de iconos presente, fallback decorativo junto al nombre. | Actualizar lang por idioma; revisar redundancia del alt en contexto, conservar nombre visible si falla PNG. |
| Teclado/lector | No hay pruebas DOM/assistive en suite actual. | Recorrido búsqueda→resultado→partida→volver; Enter/Tab/Shift+Tab, lector en móvil/escritorio, sin pérdida de foco al cargar más. |

Referencias primarias: [mensajes de estado](https://www.w3.org/WAI/WCAG22/Understanding/status-messages.html), [tamaño mínimo de objetivo](https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum.html) y [catálogo WCAG 2.2](https://www.w3.org/WAI/WCAG22/Understanding/). Estas comprobaciones deben ejecutarse en las historias de implementación, con automatización y revisión manual complementarias.

## 10. Componentes reutilizables

Extraer solo cuando exista uso real en más de una pantalla; no crear un framework de diseño completo.

| Candidato futuro | Reutilización / responsabilidad |
| --- | --- |
| AppHeader + PageContainer | Topbars repetidas, ancho y navegación con nombre accesible. |
| AsyncStatePanel | Loading/error/empty con acción, estado anunciado y mensaje localizable; no unificar causas distintas en un mismo texto. |
| CoverageNotice + CoverageDetails | Estado por operación, recuperadas/solicitadas y limitaciones; detalle por participante sin exponer identificadores internos innecesarios. |
| PlayerIdentity | Extender uso coherente de `PlayerProfileLink`; identidad incompleta sin enlace y marca del observado. |
| ChampionIdentity | Reutilizar `ChampionIcon` con nombre visible/fallback; live no debe renderizar un número como imagen final. |
| MatchSummaryCard / StatCard | Historial y acceso destacado con título/denominador; no implica nueva consulta. |
| TeamPanel / ParticipantRow | Estructura común entre terminada y activa con contenido propio: no inventar K/D/A en live. |
| HistoryEvidence / PremadeBadge | Separar conocido de posible grupo; códigos Pn y evidencia accesible. |
| LocaleSelector / FeedbackForm | Componentes futuros, una vez acordados routing y canal receptor. No existen hoy. |

Mantener fetch y presentación comprensibles, tipos actuales y caminos same-origin. Esta auditoría no recomienda reactivar `PlayerNetworkSection`, sync, jobs ni red global. Coordinación con T03.6 al implementar estados/solicitudes: revisar sus cambios después de integración explícita, sin copiarlos ahora.

## 11. Dirección Cyber Arcade

**R — dirección propuesta:** evolucionar el verde oscuro actual hacia azul noche/negro con cian y menta. Mantener rojo exclusivamente para derrota/error y dorado para énfasis moderado; evidencia alta no debe parecer premio ni certeza.

Paleta candidata a medir sobre componentes: fondo `#070D1A`, superficie `#101B2D`, texto `#EDF7FF`, secundario `#A8BACD`, cian `#55DDF2`, menta `#61E2B4`, dorado `#E8C56A`. Son propuestas, no tokens aceptados ni combinaciones certificadas de contraste.

- Tipografía de lectura clara; números tabulares para métricas/KDA y títulos con personalidad solo donde no comprometan lectura. Evitar textos largos en mayúsculas y tracking excesivo. Evaluar fuentes locales/sistema antes de sumar descargas o dependencias.
- Tarjetas con borde discreto, elevación limitada y una única jerarquía de acción primaria por bloque. Reservar glow para foco/estado activo; evitar resplandor detrás de cifras y fondos con ruido.
- Escala candidata 12/14/16 para notas/cuerpo, 20–24 para secciones y 32–48 para título según viewport; comprobar zoom. Espaciado consistente 4/8/12/16/24/32, ajustable en móvil.
- Transiciones breves de 120–180 ms, sin animación de contadores que cambie la interpretación de datos; skeletons solo para estructura conocida, sin simular fases de Riot. Respetar reducción de movimiento.
- Resultado, familiaridad y cobertura deben leerse sin color. Los equipos siguen etiquetados azul/rojo incluso si los acentos de marca cambian.

Primer artefacto futuro: comparar una única tarjeta de partida y un panel de estado en escritorio/móvil antes de extender la estética. No generar assets, instalar librerías ni rehacer toda la aplicación para esta auditoría.

## 12. Preparación multidioma

### Estado confirmado

JSX contiene textos, aria-labels y títulos hardcodeados; `format.ts` y `toLocaleString` del dashboard fuerzan `es-PA`; `echo.ts`, `premade-groups.ts` y `api.ts` contienen mensajes. Metadata y `html lang` son fijos. Backend emite títulos ingleses; el cliente los muestra. `championCatalogUrl` fija `en_US` y el nombre de campeón de UI proviene del DTO, no de un catálogo localizado. No hay selector ni segmento de idioma.

### Propuesta de preparación, sin instalar nada

1. Inventariar claves por dominio: Common, Search, Player, Match, Live, Coverage, Errors, Feedback. Incluir labels accesibles, metadata, pluralización y fechas ausentes. No concatenar frases que cambien de orden entre idiomas.
2. Locales de UI candidatos `es`, `en`, `pt-BR`; formatos inicialmente `es-PA`, `en-US`, `pt-BR`, sujetos a preferencia de producto. Separar idioma de plataforma Riot y zona horaria. No deducir región de cuenta a partir de idioma/tag.
3. Pasar locale/timezone a formatters o al proveedor futuro; fechas absolutas como referencia, relativas opcionales. Evitar divergencia servidor/cliente; duración `mm:ss` y Riot IDs permanecen estables. Porcentajes deben usar Intl con la escala correcta: actual recibe 0–100; `style:percent` recibiría fracción, no reutilizar sin adaptar.
4. Selector accesible “Español / English / Português (Brasil)”, sin banderas; conservar pantalla, owner y plataforma. Preferencia explícita primero; detección/fallback solo inicial. No disparar otro análisis Riot solo por cambiar la presentación del idioma.

### `next-intl` y rutas: alternativas por decidir

La [guía oficial App Router](https://next-intl.dev/docs/getting-started/app-router) documenta mensajes, configuración por solicitud, provider cliente y uso en componentes servidor/cliente. Es candidato porque centraliza traducción y formato en la arquitectura Next existente. La compatibilidad exacta de versión se verificará en la historia que autorice la dependencia; no se modifica package/lock aquí.

| Alternativa | Ventaja | Coste/riesgo |
| --- | --- | --- |
| URLs actuales + preferencia de idioma | Migración menor, enlaces existentes estables; recomendación inicial para MVP privado. | Idioma no representado en enlace compartido; cache/render deben considerar preferencia. |
| Prefijo `/es`, `/en`, `/pt-BR` con `[locale]` | Enlaces expresan idioma. | Cambios de routing, links, redirects y pruebas; mantener rutas sin prefijo mediante política explícita. |

La [configuración oficial de routing](https://next-intl.dev/docs/routing/configuration) permite estrategias de prefijos; no obliga a traducir `player`/`match`. Si se adopta prefijo, excluir `/api`, assets, `/openapi` y rutas internas de negociación. Next 16 denomina `proxy.ts` al punto de integración antes llamado middleware, según la [documentación oficial](https://next-intl.dev/docs/routing/middleware); conservar rewrites, cabeceras y timeout actual. No crear proxy ni decidir SEO en este sprint.

### Errores y campeones

Con contrato actual: mapear HTTP + operación a claves locales y fallback seguro; no depender del texto inglés exacto. Los estados coverage/familiarity ya son códigos utilizables. Si después se necesitan causas más precisas, una historia de contrato separada debe aprobarlas; no se propone cambiar API como prerequisito oculto del rediseño.

Para campeones, conservar `championId` y resolver **nombre oficial localizado**, sin traducción manual de nombres ni de asset IDs. Data Dragon ofrece `es_MX`, `en_US`, `pt_BR`; la [documentación oficial](https://developer.riotgames.com/docs/lol#data-dragon) distingue catálogos por idioma y versión. Mapear UI `es` a `es_MX` es propuesta para público latinoamericano, no decisión vigente. Cachear catálogo por versión/locale, fallback al nombre del DTO y al icono actual; no bajar todo el catálogo por cada fila. El proxy de imágenes actual no devuelve nombres: la futura resolución exige trabajo frontend/servidor Next explícito, sin modificar backend ni API en T04.1.

Aceptación futura: tres idiomas, cero claves faltantes, singular/plural y cero, errores traducidos, locale inválido, expansión de texto, fechas deterministas, enlaces directos y query intacta. No traducir ni registrar identidades de jugadores como parte del catálogo.

## 13. Propuesta de feedback

**Ruta futura:** `/feedback`, hoy inexistente. Link “Enviar comentarios” en navegación secundaria/ayuda y footer; desde errores, “Reportar un problema” abre formulario con categoría preseleccionada, sin adjuntar URL completa o datos de la partida automáticamente.

**Formulario propuesto:** título breve y explicación de recepción → tipo → asunto → descripción → detalles opcionales según tipo → aviso de privacidad → envío y estado. Sin login obligatorio ni adjuntos en primera versión.

| Campo | Regla propuesta | Motivo |
| --- | --- | --- |
| Tipo | Obligatorio: sugerencia, error, solicitud de funcionalidad, comentario general. | Clasificar recepción. |
| Asunto | Obligatorio, 5–120 caracteres tras trim. | Resumen escaneable. |
| Mensaje | Obligatorio, 20–3000 caracteres. | Descripción útil y payload limitado. |
| Pasos / esperado / observado | Opcionales y visibles para error, cada uno acotado; invitar a completarlos. | Diagnóstico sin bloquear comentarios válidos. |
| Correo de contacto | Opcional, formato válido si se rellena y máximo 254 caracteres; indicar uso para responder. | Permitir envío sin dato personal. |
| Contexto | Idioma, versión pública y nombre de pantalla solo con vista previa/inclusión voluntaria. | Evitar filtrar query de owner, Riot ID, Match ID o secretos. |

Validar cliente para UX y servidor para seguridad, con errores asociados y resumen enfocable. Contador de caracteres accesible. Estados: inicial, validación, enviando (un envío activo), aceptado por receptor, fallo recuperable y límite temporal. Conservar texto ante fallo de red y ofrecer copiarlo; no prometer recepción si solo se abrió otro sitio o cliente de correo. No almacenar borradores sensibles en localStorage por defecto.

### Recepción económica sin base de datos de aplicación

| Alternativa | Ventaja | Límite / decisión pendiente |
| --- | --- | --- |
| `mailto:` o copiar mensaje | Sin endpoint receptor propio; operación sencilla. | Depende de cliente de correo, expone dirección y no confirma entrega; útil como salida alternativa, no como envío garantizado. |
| Abrir issue GitHub prellenado | Reutiliza triage y cuenta del proyecto. | Requiere acceso/cuenta; en repo público el contenido sería público, en privado muchos visitantes no podrán acceder. Nunca adjuntar correo/IDs o datos privados en URL. |
| Receptor same-origin mínimo que reenvía a buzón | Formulario sin cuenta ni BD propia; recomendación para feedback privado si hay buzón/proveedor aprobado. | Necesita autorización de nuevo endpoint/secretos, controles de abuso y manejo de fallos; API actual no lo ofrece. |
| Servicio alojado de formularios | Menor mantenimiento de entrega. | Proveedor recibe datos; revisar precio/cuotas, privacidad y retención al elegir. No se elige proveedor ni se promete plan gratuito permanente. |

GitHub documenta creación con [parámetros de URL](https://docs.github.com/en/issues/tracking-your-work-with-issues/using-issues/creating-an-issue); eso abre una pantalla de creación, no envía el feedback ni garantiza privacidad. No crear issues públicos automáticamente con un token desde el navegador.

**Spam:** honeypot fuera del foco/lector y no visible para usuarios, validación de tamaño/tipo en servidor, comprobación de origen, límite por intervalos y rechazo de ráfagas antes de reenviar. Un limitador en memoria se reinicia y no coordina réplicas: suficiente solo si el futuro alcance lo acepta. CAPTCHA solo si abuso observado lo justifica, con alternativa accesible y revisión del proveedor; no añadirlo por defecto ni relajar CSP sin evaluar necesidad.

**Privacidad/seguridad:** pedir que no se incluyan contraseñas, claves ni datos de terceros. Sin screenshots ni logs por defecto. Mostrar receptor y finalidad; acceso restringido al buzón, sin log de cuerpos, retención propuesta de 90 días para revisar con PO y canal de eliminación. “Sin BD” no significa sin almacenamiento: correo, GitHub y proveedores conservan datos. No afirmar cumplimiento legal sin revisión correspondiente. Escapar contenido, no interpretar HTML ni usar entrada de usuario como cabeceras de correo.

La CSP actual solo admite conexiones/formularios same-origin; un POST directo a proveedor no funcionaría sin cambios explícitos. Preferir evaluación de receptor same-origin si se decide formulario real. Esta propuesta es un alcance nuevo y no modifica contratos T03.5, backend ni configuración hoy.

## 14. Lista priorizada de mejoras

No se autoriza implementación por figurar en esta lista. Las verificaciones pendientes no equivalen a defectos reproducidos.

| Orden | Prioridad / esfuerzo | Mejora e IDs | Criterio de aceptación y dependencia |
| --- | --- | --- | --- |
| 1 | P1 / S | Corregir copy stateless y semántica de muestra (H01, H05). | Ninguna promesa de 20/sync/guardado; diferencia explícita entre cinco iniciales y filas adicionales. Frontend, sin contrato nuevo. |
| 2 | P1 / M | Cobertura, errores, vacíos y vigencia (H04, H08, H19). | Cuatro estados distinguidos, fallo nunca significa cero real, avisos no se pisan; casos de mock por operación. Revisar integración T03.6 antes de tocar gestión de solicitudes. |
| 3 | P1 / M | Continuidad de historial (H06). | Mock con detalle fallido/duplicado no produce bucle ni pérdida silenciosa; regla de offset/reintento documentada. Evaluar solución compatible con contrato actual. |
| 4 | P1 / S–M | Región visible y navegación contextual (H02, H07). | Alcance configurado visible y retorno directo seguro; selector futuro solo si plataforma recorre todo el flujo. |
| 5 | P1 / S | Comprobar responsive real (H15). | Evidencia en 320 px/zoom y textos largos; convertir solo problemas reproducidos en ajustes de CSS. |
| 6 | P1 / M | Resultados sin esperar bloque activo (H03). | Historial visible aunque active-game falle/tarde; estados independientes y sin regresiones de cancelación. Depende de coordinación con T03.6, no de incluirla en esta rama. |
| 7 | P2 / M | Evidencia de encuentros/premades (H09, H10). | Conteos/fecha consultables por toque/teclado, owner y equipos claros, confianza no presentada como probabilidad. Usar DTOs existentes. |
| 8 | P2 / M | Accesibilidad transversal (H13, H14, H16). | Labels/error/foco, lector, objetivos, contraste y movimiento verificados; mantener Pn y resultado textual. |
| 9 | P2 / M | Claridad de live (H11, H12). | Sin IDs técnicos en copy, ausencia normal, snapshot fechado y refresco manual; sin resolver ocultos ni análisis adicional. |
| 10 | P2 / M | Cyber Arcade en una pantalla piloto (H18). | Tokens y tarjeta aprobados en móvil/escritorio con contraste; extender solo después de revisión. |
| 11 | P2 / L | Preparación e integración de idiomas (H17). | Decidir routing, locale/timezone, extraer mensajes y probar es/en/pt-BR por pantalla; dependencia nueva solo con explicación/autorización de esa historia. |
| 12 | P3 / M | Feedback privado mínimo (H20). | Receptor aprobado, privacidad clara, fallo conserva texto y éxito corresponde a recepción; requiere historia nueva, sin BD propia obligatoria. |

No se propone un P0: esta revisión estática no demuestra una caída general o una exposición de secretos. No se incluyen cambios de recursos/rate limiting ni otras tareas de T03.6.

## 15. Plan de implementación por pantalla

Cada entrega futura será pequeña, con revisión y evidencia propia. No agrupar toda la estética, idioma y feedback en un único sprint. La separación siguiente es una propuesta de secuencia, no historias ya aceptadas en tablero.

| Incremento | Pantalla / alcance | Fuera de alcance y aceptación |
| --- | --- | --- |
| A | Home: corregir copy/alcance, ayuda Riot ID y validación accesible. | Sin nuevos endpoints ni reglas Riot supuestas. Probar vacío, espacios, caracteres codificados y transición al perfil. |
| B | Dashboard: muestra de cinco, avisos por bloque, errores/vigencia. | Sin recomputar resumen al cargar más. Mocks con 0/1/5 partidas, búsqueda fallida, parcial, active-game lento/fallido y refresco con datos previos. |
| C | Dashboard: continuidad “cargar cinco más” y regreso al historial. | Sin persistencia o paginación ilimitada. Probar página de menos de cinco, detalle fallido, duplicados y último bloque; scroll/foco estable. Revisar primero contratos y T03.6 integrada por vía autorizada. |
| D | Detalle terminado: owner/cobertura, equipos y evidencia expandible. | Sin nuevos scores/estadísticas ni análisis de identidades ocultas. Fixtures sintéticos con diez, menos participantes, sin owner, owner ausente, grupos solapados y todos los estados de familiaridad. |
| E | Live: lectura clara, estado sin partida y actualización manual. | Sin polling, inferencias o ventaja competitiva. Probar owner ausente, snapshot correcto, identidad incompleta, fin de partida y servicio fallido; no usar datos reales. |
| F | Home piloto Cyber Arcade, después dashboard, después detalle y live en entregas separadas. | Solo presentación con tokens comunes y componentes usados; verificar en cada pantalla carga/error/vacío/éxito y accesibilidad antes de pasar a la siguiente. |
| G | Infraestructura i18n mínima + Home; después dashboard, detalle y live individualmente. | Decisión explícita de routing previa; mantener URLs/query y semántica T03.5. Pruebas de mensajes/formato y revisión humana de traducciones. |
| H | Feedback como página nueva y recepción aprobada. | Prototipo de formulario y canal antes de envío real; abuso, privacidad, rechazo/reintento y acuse verificables. No activar proveedor por efecto de este plan. |

### Invariantes que toda historia posterior debe preservar

1. Búsqueda con cinco partidas iniciales y detalle independiente por `POST analyze`; cargar historial no supone reanalizar una unión global.
2. Evidencia solo anterior a la partida objetivo; cinco del owner/tres por otro participante; deduplicación y cobertura real. No usar objetivo/futuro como prueba histórica.
3. Sin jobs, polling de jobs, almacenamiento funcional durable, sync ni red global retirados. Caché opcional de respuestas no es un historial guardado.
4. PUUID como identidad interna, Riot ID mutable como presentación, claves solo backend/runtime y tráfico browser same-origin. No añadir llamadas Riot desde UI.
5. No inventar progreso: timeout API 180 s/proxy 210 s no garantiza duración de análisis. Mensaje honesto, error recuperable y reintento manual, sin bucles automáticos.
6. “Posible premade” sigue siendo inferencia; parcial/error nunca confirma ausencia de conocidos. No crear identidades faltantes para completar diez filas.

Fuentes de invariantes: [contratos](../apps/api/src/LolAnalyzer.Application/Stateless/AnalysisContracts.cs), [servicio bajo demanda](../apps/api/src/LolAnalyzer.Application/Stateless/OnDemandAnalysisService.cs), [endpoints y errores](../apps/api/src/LolAnalyzer.Api/Program.cs), cliente `src/api.ts`, D-042 e informe T03.5.

### Validación de esta entrega documental

- `git diff --check` y revisión del diff/índice; solo informe y continuidad relacionada en `docs/PROJECT_CONTEXT.md`/`docs/TODO.md`.
- Verificación de destinos de enlaces locales y rutas/componentes en el árbol de T03.5; no confundir utilidades heredadas con UI montada.
- Base exacta T03.5, cero integración de T03.6, remoto SSH `git@github.com:D0kker/improved-train.git`, rama de auditoría y PR dirigido a T03.5.
- No corresponde ejecutar builds, benchmarks, pruebas de backend ni reconstruir contenedores para este diff documental. Las pruebas UI manuales quedan como criterios futuros, no como evidencia ejecutada.

Las fuentes externas enlazadas se consultaron durante la auditoría; sus políticas, versiones y límites deben revisarse de nuevo cuando se implemente una decisión que dependa de ellos.
