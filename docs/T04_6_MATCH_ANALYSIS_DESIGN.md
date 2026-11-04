# T04.6 — Diseño de análisis de partida

Estado: **propuesta para aprobación del PO**, sin implementación. Base inspeccionada: `origin/main` `16b00eb343c5f43496fb55571084e119b84d6d49`. Evaluación de código y contratos, no prueba visual ni estudio con usuarios. Wireframes en texto, accesibles y con datos ficticios.

## Estado y flujo comprobados

La ruta real es [`/match/[matchId]`](../apps/web/app/match/%5BmatchId%5D/page.tsx); recibe `ownerPuuid`, `platformRegion` y, opcionalmente, ambos elementos de la identidad para regresar al perfil. [`MatchDetailView`](../apps/web/app/match/%5BmatchId%5D/match-detail-view.tsx) usa POST de análisis si hay owner; sin owner usa GET de detalle, sin cobertura histórica y con mensaje para abrir desde un perfil. El retorno va al perfil solo con identidad suficiente; si no, a búsqueda. El contexto de región es opcional; no inferirlo del tag.

El POST es una **única respuesta**, no un stream de progreso: no puede garantizarse visualmente que «participantes cargados» o «historial cargando» hayan ocurrido. Conserva resultados previos marcados como anteriores si falla la actualización, permite reintentar y cancela/ignora respuestas obsoletas. Véanse [`api.ts`](../apps/web/src/api.ts), [`use-request-lifetime.ts`](../apps/web/src/use-request-lifetime.ts) y pruebas en [`frontend.test.ts`](../apps/web/tests/frontend.test.ts) y [`resource-protection.test.ts`](../apps/web/tests/resource-protection.test.ts).

[`OnDemandAnalysisService`](../apps/api/src/LolAnalyzer.Application/Stateless/OnDemandAnalysisService.cs) obtiene la partida objetivo y participantes con PUUID válido; exige owner presente y rechaza más de diez participantes. Pide hasta cinco históricos del owner y tres por cada otro participante identificable; solicita un ID adicional para compensar exclusión/borde temporal, excluye el objetivo, deduplica IDs y filtra partidas posteriores o sin fechas elegibles. Reutiliza caché, límites, cancelación y timeouts de T03.5/T03.6. No hay garantía de diez participantes o ventanas completas en resultados parciales. Los flujos de búsqueda y análisis son independientes.

## Hallazgos confirmados y prioridad

| ID | Severidad | Evidencia | Propuesta, pendiente de aprobación |
|---|---|---|---|
| F01 | Alta | `MatchAnalysisResult.relationships` contiene pares, pero la vista asigna únicamente `result.match` y `result.coverage`; no presenta esos pares. | Lista de pares con evidencia, sin alterar cálculo ni contrato. |
| F02 | Alta | Cobertura se coloca antes del encabezado y puede ocupar siete cifras técnicas. | Aviso compacto junto al título; detalle al final, con advertencias parciales visibles. |
| F03 | Alta | Sin owner se muestra detalle pero no análisis; familiaridad nula no equivale a cero. | Identificar claramente «detalle sin análisis histórico» y ofrecer regreso seguro. |
| F04 | Media | La identidad del jugador conocido aparece como insignia por participante y en tarjetas de «Jugadores conocidos». | Una representación principal y enlaces contextuales breves, sin perder evidencia. |
| F05 | Media | Grupos con letras y colores más título «Posibles premades» pueden dar peso excesivo a una inferencia. El texto actual sí advierte que no confirma entrada conjunta. | Priorizar coincidencias observadas; mantener la advertencia y etiquetas textuales. |
| F06 | Media | Resultado de partida genérico, duración separada y K/D/A del owner distribuido en su equipo. | Resumen compacto con datos del owner cuando esté identificable. |
| F07 | Media | `.teams-grid` pasa a una columna a 900 px y filas cambian a 620 px en [`globals.css`](../apps/web/app/globals.css); densidad y desbordes requieren medición real. | Probar marcador apilado y nombres largos; no declarar defecto visual sin navegador. |
| F08 | Media | Carga solo anuncia «Revisando el historial reciente» o «Cargando participantes»; no recibe eventos de avance. | Estado único anunciable con explicación de espera; sin porcentajes ni etapas simuladas. |
| F09 | Media | Los fallos de reconsulta conservan datos con aviso; error inicial y sin cobertura requieren contexto diferente. | Mantener vigencia anterior, reintento y acciones adecuadas según el estado. |
| F10 | Baja | La interfaz existente mezcla clases específicas de detalle con primitivas de T04.3. | Reutilizar tokens y componentes donde corresponda tras validación visual. |

Las observaciones sobre jerarquía o composición son recomendaciones de diseño; no se han medido tiempos de uso, contraste ni espacio visual en navegador en esta tarea.

## Inventario de contrato

| Dato | Evidencia y uso posible | Límite |
|---|---|---|
| Match ID, cola, inicio, duración | `MatchDetail` en [`api.ts`](../apps/web/src/api.ts) | cola, fecha y duración pueden ser null; ID es identificador técnico. |
| Equipos y participantes | `teamId`, campeón, K/D/A, `win?`, PUUID/Riot ID/tag opcionales | No asumir dos equipos completos ni cinco jugadores por equipo; resultado solo si datos coherentes. |
| Owner | `owner` en respuesta POST y `ownerPuuid` de navegación | GET sin owner no permite identificar su resultado ni historial; no derivar identidad de datos parciales. |
| Familiaridad | `knownPlayers`, `evaluablePlayers`, porcentaje, status y detalles por owner | Porcentaje solo cuando el estado y cobertura lo permiten; nunca equivale a probabilidad de premade. |
| Pares | `relationships`: dos PUUID, `matchesTogether`, `allyMatches`, `enemyMatches`, `score`, `confidence` | La vista actual los descarta; verificar semántica exacta de `matchesTogether`, score y confidence antes de diseñar etiquetas o rangos. No convertir score a probabilidad. |
| Grupos | `premadeGroups` con clasificación, miembros y teamId | Son inferencias; no existe confirmación de que entraran juntos. |
| Cobertura | `participants`, IDs obtenidos/únicos, partidas recuperadas, ventanas por jugador y fallos | Conteos distintos, no sumarlos como si fueran personas o partidas únicas; no exponer PUUID ni códigos internos. |

No consta en este contrato una lista de Match IDs históricos por cada par, timestamps de esas coincidencias, orden de posiciones garantizado, rango clasificatorio ni avatares actuales. No prometer enlaces a encuentros históricos concretos sin evidencia por pareja. Los retratos actuales usan [`ChampionIcon`](../apps/web/src/champion-icon.tsx) y su fallback; no se requieren nuevas llamadas autenticadas a Riot. El KDA individual propuesto usa la convención del dashboard `(kills + assists) / max(1, deaths)` solo con valores válidos; no modificar el algoritmo backend ni fabricar datos ausentes.

## Estructura propuesta

1. **Resumen:** volver al perfil/búsqueda; cola, fecha, duración, región disponible y owner. Si el owner aparece en la partida, resultado textual, campeón, K/D/A y KDA. Ante datos ausentes mostrar «No disponible». Match ID secundario y copiable solo si se justifica una acción real.
2. **Equipos:** marcador azul/rojo para `teamId` 100/200 y nombre neutro en otros IDs; listas semánticas de participantes, campeón, Riot ID si está completo, K/D/A y resultado solo cuando sea coherente. Resaltar owner con texto, no solo color. Contar filas disponibles; no rellenar huecos con jugadores ficticios.
3. **Relaciones históricas:** lista ordenada de pares con identidad conocida, encuentros y desglose aliados/rivales conforme a semántica verificada. Etiquetar si están en el mismo equipo en la partida objetivo o en equipos contrarios, solo con ambos equipos identificables. PUUID sirve para asociar, nunca como identidad visible.
4. **Posibles grupos:** sección breve de grupos devueltos, evidencia y mensaje «La recurrencia no confirma que los jugadores sean premades». No calcular nuevos grupos ni probabilidades. Con datos insuficientes decirlo; ausencia de grupo no demuestra ausencia histórica.
5. **Cobertura:** estado y límites junto al resultado; detalle expandible final con participantes identificables, IDs obtenidos/únicos, partidas recuperadas, ventanas y fallos disponibles. «Completa» significa ventana solicitada recuperada, no toda la historia. Sin owner: «Análisis histórico no solicitado».

## Alternativas para relaciones

| Alternativa | Lectura y densidad | Móvil/accesibilidad | Riesgo |
|---|---|---|---|
| A. Lista de pares | Ordena evidencia y permite explicar cada relación; escala con filtros locales si hicieran falta | Una columna, texto y controles nativos | Puede crecer con muchos pares; expansión progresiva sin ocultar avisos. |
| B. Matriz | Vista global de hasta diez participantes | 10×10 exige navegación y encabezados de tabla; ancho problemático a 320 px | Triángulo duplicado, celdas sin datos confundidas con cero. |
| C. Grafo | Agrupaciones intuitivas con pocos enlaces | Orden, teclado y texto alternativo complejos; líneas se cruzan | Color/grosor pueden sugerir certeza; muchos enlaces saturan. |

**Recomendación subjetiva: A**, con texto de evidencia y orden claro. Una matriz o grafo podrían investigarse después con usuarios y tabla equivalente accesible; no son necesarios para la primera implementación.

## Wireframes conceptuales

Desktop, 1024–1440 px:

```text
← Volver al perfil       PARTIDA · cola · fecha · duración · región
[ campeón | Jugador #TAG | Victoria | 4 / 2 / 7 · KDA 5.50 ]
[ Cobertura parcial: faltan datos. Ver detalle ↓ ]
[ EQUIPO AZUL · Victoria     ] [ EQUIPO ROJO · Derrota     ]
[ campeón · jugador · K/D/A  ] [ campeón · jugador · K/D/A ]
[ hasta cinco filas reales    ] [ hasta cinco filas reales  ]
RELACIONES HISTÓRICAS
[ Jugador A ↔ Jugador B | encuentros | aliados | rivales ]
POSIBLES GRUPOS · La recurrencia no confirma premades
[ miembros · evidencia disponible ]
COBERTURA Y TRANSPARENCIA [expandir]
```

Móvil, 320–390 px:

```text
← Volver al perfil
PARTIDA · cola · fecha
[ Jugador #TAG · región ]
[ Victoria · campeón · K/D/A · KDA ]
[ Aviso de cobertura si aplica ]
EQUIPO AZUL  [filas]
EQUIPO ROJO  [filas]
RELACIONES  [pares en una columna]
POSIBLES GRUPOS [evidencia + cautela]
COBERTURA [detalle expandible]
```

A 768 px usar columnas solo si caben sin truncar; a 1024/1440 usar marcador de dos columnas. Las filas deben admitir texto 40 % más largo, wrapping y objetivos táctiles adecuados. No depender de hover, color ni scroll horizontal de tabla; probar también 320, 390, 768, 1024 y 1440 px con datos parciales y nombres largos. Son propuestas, no mediciones.

## Estados y accesibilidad

| Situación | Tratamiento propuesto |
|---|---|
| Inicio/carga | `role=status`, «Analizando partida e historial anterior…»; sin progreso inventado. La carga GET sin owner puede decir «Cargando partida…». |
| Participantes/historial | Son fases internas de una misma respuesta; no presentarlas como completadas independientemente sin contrato nuevo justificado. |
| Completo | Mostrar evidencia de la ventana; no afirmar historia completa. |
| Parcial | Aviso visible junto a métricas afectadas y detalle de cobertura. |
| Sin coincidencias | Solo si evaluable: «No se encontraron coincidencias en las partidas disponibles»; no certificar que nunca ocurrieron. |
| Insuficiente/error | Identificar información insuficiente o fallo, no sustituir por cero. |
| Riot/429/timeout | Mensaje seguro y acción de reintento existente; no mostrar trazas ni secretos. |
| Cancelación | Abandonar cancela; no mostrar error tardío ni resultados del jugador anterior. |
| Reintento fallido | Conservar datos anteriores solo marcados como tales; controles incompatibles deshabilitados durante actualización. |

Usar landmarks, jerarquía h1→h2, listas/tablas semánticas, nombres accesibles, foco visible, `details/summary` o botones con `aria-expanded`, regiones anunciables, texto alternativo significativo y `prefers-reduced-motion`. Verificar contraste, zoom, teclado, lector de pantalla y objetivos táctiles; no se afirma cumplimiento WCAG completo.

## Rendimiento, publicidad y riesgos

El rediseño debe consumir la respuesta ya disponible; cálculos de presentación locales y memoización cuando se justifique, sin polling ni consultas por participante adicionales. Preservar el presupuesto, cancelación y las ventanas cinco/tres; no añadir persistencia. Datos sensibles no deben aparecer en URLs nuevas, capturas ni logs. No representar Riot ID como identidad estable.

Publicidad futura: considerar un bloque claramente identificado **después de resultados y antes o después de cobertura**, solo si no separa una advertencia del resultado al que afecta; lateral únicamente con ancho suficiente. Reservar dimensiones al cargar para evitar desplazamientos; descartar anuncios entre jugadores, junto a acciones o imitando coincidencias. Revisar políticas vigentes de Riot y del proveedor antes de cualquier activación. Sin slots ni scripts en esta propuesta.

Riesgos: relacionar incorrectamente PUUID de pares con identidades incompletas; confundir ausencia con falta de cobertura; inferir premades como hechos; presentar score como probabilidad; mostrar una partida vieja como actual; saturar móvil o incrementar cuota. Las relaciones se refieren a la muestra temporal elegible, no a toda la cuenta.

## Decisiones pendientes del PO y plan

1. Aprobar orden resumen → equipos → relaciones → posibles grupos → cobertura y lista de pares como visual principal.
2. Elegir terminología de grupos y nivel de detalle de confianza; conservar la advertencia de inferencia.
3. Decidir si priorizar pares del mismo equipo o por frecuencia; resolver empates de forma determinista tras verificar semántica backend.
4. Decidir qué información de cobertura mostrar inicialmente y cuál expandir, manteniendo avisos críticos visibles.
5. Validar con prototipo y usuarios el marcador móvil, navegación, contrastes, estados parciales y textos largos.
6. Tratar publicidad como propuesta futura independiente; revisar políticas antes de implementación.

Tras aprobación: primero adaptar cabecera y equipos; después lista de relaciones y grupos; finalmente cobertura y estados. En cada fase ejecutar pruebas de componentes y contratos con Riot simulado, pruebas de cancelación y revisión de navegador; no alterar backend ni agregar llamadas sin una tarea explícita. No comenzar el rediseño antes de la aprobación del PO.
