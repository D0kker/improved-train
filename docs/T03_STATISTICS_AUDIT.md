# T03 — Estadísticas, encuentros y familiaridad

Fecha: 2026-10-18. Base revisada: `feature/echo-product-focus`, commit `648a74a` (incluye T01 y T02). El encargo recibido termina en «6. Validar»; este informe cubre los requisitos recibidos hasta ese punto.

## Rutas principales verificadas

- [Resumen y caché](../apps/api/src/LolAnalyzer.Application/Analysis/PlayerSummaryQueryService.cs), [cálculos y lecturas EF](../apps/api/src/LolAnalyzer.Infrastructure/Persistence/PlayerAnalysisRepository.cs).
- [Familiaridad](../apps/api/src/LolAnalyzer.Application/Analysis/MatchFamiliarityService.cs), [calculador puro](../apps/api/src/LolAnalyzer.Application/Analysis/MatchFamiliarityCalculator.cs), [encounters](../apps/api/src/LolAnalyzer.Application/Analysis/RepeatedPlayerAnalyzer.cs).
- [Servicio de relaciones](../apps/api/src/LolAnalyzer.Application/Analysis/PlayerRelationshipAnalysisService.cs), [repositorio y evidencia del lobby](../apps/api/src/LolAnalyzer.Infrastructure/Persistence/PlayerRelationshipRepository.cs).
- [Modelo PlayerEncounter](../apps/api/src/LolAnalyzer.Domain/Entities/PlayerEncounter.cs), [modelo PlayerRelationship](../apps/api/src/LolAnalyzer.Domain/Entities/PlayerRelationship.cs), [ingesta](../apps/api/src/LolAnalyzer.Application/Matches/MatchIngestionService.cs).
- [Dashboard](../apps/web/app/player/[gameName]/[tagLine]/player-dashboard.tsx), [detalle](../apps/web/app/match/[matchId]/match-detail-view.tsx), [helpers](../apps/web/src/echo.ts), [contratos HTTP](../apps/web/src/api.ts).
- [Fixtures EF InMemory](../apps/api/tests/LolAnalyzer.UnitTests/InMemoryStatisticsTests.cs), [regresiones frontend](../apps/web/tests/frontend.test.ts).

## Definiciones y alcance

| Métrica | Cálculo | Alcance |
| --- | --- | --- |
| Partidas analizadas | Match ID distintos con participación del owner | Todas las partidas disponibles del jugador en el proceso InMemory |
| Participaciones de otros jugadores | Pares distintos (Match ID, PUUID), sin owner ni identidades vacías | Las mismas partidas del resumen |
| Jugadores únicos encontrados | PUUID distintos de esas participaciones | Las mismas partidas del resumen |
| Recurrentes | PUUID distintos con participación en al menos dos Match ID | Las mismas partidas del resumen |
| Conocidos del lobby | Participantes identificables del objetivo con encuentros previos con el owner | Solo partidas anteriores según (fecha, Riot Match ID) |
| Evaluables | Identidades distintas presentes en el objetivo, sin owner | Hasta 9 en un lobby completo de 10 |
| Familiaridad | Conocidos / evaluables, redondeado a un decimal | La evidencia disponible, nunca una probabilidad de premade |

Las cinco partidas son el tamaño de consulta inicial, no un límite destructivo del corpus. El proceso puede reutilizar otras partidas del owner recuperadas al consultar participantes o al actualizar. La UI identifica el resumen como todas las partidas disponibles en memoria. Restringirlo a una ventana aislada por búsqueda requeriría otro contrato de contexto y no se introduce en T03.

`PlayerEncounter` es dirigido owner/other e incluye conteos y resultados aliado/rival. `PlayerRelationship` es una pareja canónica no dirigida con score explicable. No son conceptos intercambiables. Se conservan ambos modelos.

## Hallazgos y correcciones

- `PlayerAnalysisRepository.GetSummaryAsync` dependía del snapshot de encounters: ahora calcula directamente partidas, PUUID únicos, participaciones y recurrentes sobre las filas normalizadas. El contrato añade `otherPlayerParticipations` sin retirar campos anteriores.
- Las tarjetas del dashboard contaban filas del endpoint de conocidos. Ahora usan los contadores autoritativos del resumen. Los helpers secundarios deduplican por PUUID y admiten excluir al owner.
- `RepeatedPlayerAnalyzer` ya deduplicaba participantes por partida, pero no Match ID repetidos; ahora también lo hace y descarta IDs vacíos.
- `ReplaceEncountersAsync` eliminaba y recreaba entidades con la misma clave dentro del mismo DbContext. Se reemplazó por actualización de entidades existentes y eliminación de las ausentes; se prueba reconstrucción repetida con el proveedor real InMemory.
- El lock por Match ID no protegía identidades compartidas entre dos partidas diferentes y su retirada podía abrir una carrera entre esperadores. Un único semáforo de escritura InMemory compartido por lookup e ingesta protege PUUID y Match ID; las descargas HTTP siguen siendo concurrentes.
- El resumen cacheado del owner podía quedar obsoleto cuando otro participante importaba una partida suya. La ingesta invalida los resúmenes de todos los PUUID afectados por nuevas partidas.
- El resultado de ingesta contenía `Failed`, pero el endpoint no lo serializaba. Ahora se expone y ambas vistas inspeccionan los resultados HTTP exitosos que contienen fallos parciales. `NotFound` deja de incluir errores de descarga. JSON inválido también produce una descarga fallida aislada.
- La UI distingue cero coincidencias en evidencia evaluable, análisis parcial, historial insuficiente y error. Oculta el porcentaje definitivo cuando no hay cobertura completa y conserva la evidencia local si falla la recarga posterior.
- `NoPriorHistory` ya no se describe como la primera partida real del jugador: significa que no hay historial anterior disponible. Las colas 5v5 ya soportadas por la UI comprueban también el tamaño esperado del roster; identidades ausentes producen `PartialParticipants`. Un nombre/tag ausente no invalida un PUUID disponible.
- `LoadFamiliarityInputAsync` ahora incluye el objetivo aunque el owner no esté presente, permitiendo devolver `OwnerNotPresent` explícitamente.
- La familiaridad individual ya excluía correctamente objetivo y futuro. Se conservó ese orden temporal, incluida la regla estable de desempate.
- La detección de premades del lobby leía agregados globales que incluían objetivo/futuro. Ahora reutiliza el analizador existente sobre snapshots estrictamente anteriores. El contraste reciente del owner tampoco usa objetivo/futuro. Las inferencias conservan sus umbrales y etiquetas prudentes.
- La carga inicial del dashboard usaba `load(false)` y un jugador sin datos nunca consultaba sus cinco partidas. Se activa la consulta inicial con el conteo configurable existente.
- Las pruebas T02 no compilaban por una importación de xUnit ausente; corregida para poder validar la suite completa.

## Pruebas añadidas

- Fixture InMemory de cinco partidas completas: 45 participaciones, 37 PUUID únicos, dos recurrentes, 3 victorias y 2 derrotas.
- Familiaridad de ese fixture: nueve evaluables, dos conocidos, siete sin coincidencia detectada, 22,2%; balance aliado/rival y resultados sin contar objetivo.
- La primera partida no produce relaciones históricas; una partida intermedia solo usa las anteriores, aunque el proceso contenga las posteriores.
- Dieciséis escrituras concurrentes sobre dos Match ID compartidos: dos partidas lógicas e identidades únicas por PUUID.
- Invalidación del resumen cuando otro jugador importa una partida compartida.
- Descarga parcial: se conserva una partida válida, se informa un fallo y no se confunde con un 404.
- Lobby completo, roster incompleto, Match ID repetidos y participantes no identificables.
- Frontend: deduplicación por PUUID y estados para fallos HTTP, fallos en un 200, cero coincidencias, datos insuficientes y roster parcial.

## Validación ejecutada

- .NET SDK 10.0.402 en Docker, restore con caché NuGet temporal compartida: 75 pruebas unitarias y 21 de integración aprobadas, ninguna omitida.
- Frontend Node 24: 14 pruebas aprobadas; lint y type-check aprobados. Los comandos se ejecutaron fuera del sandbox: dentro de él, el runner reportaba solo archivos y Next.js no podía leer correctamente la salida de su subproceso TypeScript.
- Build de producción Next.js aprobado; rutas de jugador y partida generadas correctamente.
- Formato whitespace .NET aplicado a los archivos modificados y `git diff --check` aprobado.
- `docker compose config --quiet` y construcción Docker de API/web aprobados para la arquitectura disponible.
- No se llamó a Riot en las pruebas.

## Publicación y despliegue autorizados

- Código publicado por SSH en `origin/feature/echo-product-focus`, commit `1f54650`.
- El PO autorizó el despliegue el 2026-10-18. API y web fueron reconstruidas/recreadas en modo InMemory, con web publicada en 8081.
- Ambos contenedores están saludables. `/` respondió 200, `/api/health` devolvió `healthy` y readiness interno de API respondió `Healthy`.
- Se verificaron las imágenes ejecutadas contra los artefactos construidos: API `342899275dc5`, web `21ba547cf9f3`.
- El reinicio elimina los datos temporales anteriores; no se añadió persistencia durable.

## Límites

- Los estados de sincronización de Riot corresponden a la consulta de cada vista; el GET de familiaridad calcula evidencia local y no afirma que se haya descargado todo el historial de Riot.
- Las ventanas recientes pueden no contener partidas anteriores a un objetivo antiguo. Eso es información insuficiente, no prueba de ausencia de encuentros.
- InMemory no es almacenamiento durable ni un mecanismo de aislamiento entre usuarios. T03 conserva la arquitectura de instancia única y no agrega persistencia.
- No se añaden endpoints Riot ni se consultan cuotas reales en pruebas. Se conservan single-flight, caché y límites de concurrencia de T01 y el dispatcher de T02.
