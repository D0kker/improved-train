# T04.10.1 — Contratos y evidencia histórica del dashboard

## Alcance y base

Worktree aislado y rama `feature/t04-10-1-dashboard-data-contracts` desde T04.10 `d010b981c368a021d552a902e7fa62d09a5e94d1`. PR #97 sigue abierto/draft; no se integra en main ni se despliega. Se extienden contratos existentes de forma aditiva; no se agregan endpoints de análisis ni llamadas autenticadas a Riot.

## Auditoría de datos

| Dato | Origen existente | Backend anterior | API anterior/frontend | Cambio |
|---|---|---|---|---|
| Owner PUUID | Account-V1 y Match-V5 | Identidad interna | PlayerLookup y contexto de solicitudes | Se incluye también en evidencia de fila |
| Match ID | lista Match-V5 y metadata.matchId | Valida ID recuperado, deduplica | matches.items.riotMatchId | Se conserva como clave de evidencia |
| PUUID participantes | Match-V5 info.participants | Parseado y usado en calculadores | Solo agregados encounters; sin vínculo por partida | Proyección ligera por fila |
| Team ID | Match-V5 participants.teamId | Parseado, usado para equipo/encuentros | No presente en fila de historial | TeamId nullable en evidencia; 100/200 reconocidos |
| Riot ID/Tag | Account-V1 y participantes Match-V5 | Ambos parseados | Identidad y agregados | Incluidos en participantes de evidencia; no son claves |
| Plataforma | Routing de solicitud | Se conserva en identidad | PlayerLookup.platformRegion | También en evidencia; sin inferir desde tag |
| Campeón y combate owner | Match-V5 | Ya proyectados | Disponibles en MatchListItem | Sin cambios de semántica |
| profileIcon | Match-V5 participante owner | Se descartaba al parsear | Ausente | Se conserva y se expone en PlayerIdentity si válido |
| Ventana | start/count ya existentes | Paginación acotada | Frontend guarda offset | No hay nuevo cursor ni petición |
| Cobertura | IDs, descargas y fallos existentes | Estado y conteos por ventana | CoverageSummary | Se conserva; se añade aviso de evidencia incompleta |

Referencias: `RiotMatchData.cs`, `RiotApiClient.ParseParticipant`, `OnDemandAnalysisService.RecentAsync`, `AnalysisContracts.cs`, `PlayerAnalysisContracts.cs`, `apps/web/src/api.ts`.

## Ícono y fuentes oficiales

Se verificó la documentación oficial [Riot LoL/Data Dragon](https://developer.riotgames.com/docs/lol) y el [catálogo de APIs](https://developer.riotgames.com/apis). El identificador procede del campo opcional `profileIcon` del participante owner en la partida más reciente ya recuperada. Se expone junto con `profileIconObservedAt`. **Es un ícono histórico observado en esa partida, no una consulta del perfil actual**. Account-V1 por sí solo no entrega el ícono.

La alternativa para obtener perfil actual sería una consulta Summoner-V4 por PUUID por búsqueda (más rate limit, latencia y caché adicional). No se implementó porque la evidencia histórica existente resuelve la necesidad dentro del alcance aprobado. Sin partidas o sin campo válido, permanece el fallback neutral.

La imagen usa el CDN fijo Data Dragon con `DATA_DRAGON_VERSION` validada y el default ya existente 16.17.1. Se sirve mediante proxy same-origin `/api/assets/profile-icons/{id}`, sin clave Riot, caché diaria, timeout 5s, content-type PNG y máximo 1 MiB leído. IDs enteros 0–99999; no se aceptan URLs arbitrarias. Una imagen ausente o no disponible activa fallback. No se actualizó la versión de assets; íconos no presentes en ella pueden caer al fallback.

## Modelo y compatibilidad

- `PlayerIdentity`: propiedades opcionales `ProfileIconId`, `ProfileIconObservedAt`.
- `RiotMatchParticipantData`: propiedad opcional interna `ProfileIconId`, sin cambiar el constructor posicional.
- `PlayerMatchListItem.Evidence`: opcional, contiene owner PUUID, plataforma, status complete/partial y hasta diez participantes con PUUID, nombre, tag y team ID nullable.
- Se excluyen PUUID vacíos; se deduplican por PUUID. Equipos contradictorios/inválidos se proyectan como null. Un owner ausente produce evidencia parcial y el frontend no construye relaciones para esa fila.
- Los campos anteriores, incluyendo encounters agregados y cobertura, permanecen disponibles para clientes existentes. TypeScript trata evidence como opcional para permitir respuesta de API anterior.
- Parseo obligatorio de Match-V5 mantiene sus reglas: un payload sin campos obligatorios puede fallar la descarga y aparecer en la cobertura existente; no se inventa una fila completa.

## Deduplicación y reconciliación

Frontend agrega evidencia sobre Match IDs únicos y relaciones por PUUID. Un jugador cuenta una vez por partida aunque se repita en la proyección. Se prioriza evidencia completa y luego la más informativa; desempate canónico y orden estable por Match ID/PUUID, independiente del orden de llegada de ventanas equivalentes. El reintento de la misma ventana conserva evidencia completa ante una respuesta menos informativa y no incrementa coincidencias.

Con equipo válido de owner y participante se clasifica aliado/rival. Si el equipo falta, la coincidencia de identidad cuenta, pero su equipo queda desconocido. Aliados/rivales son conjuntos que pueden solaparse. Owner no cuenta como otro jugador. Las identidades mostradas provienen de la evidencia ordenada de partidas, no de una consulta nueva del perfil actual.

La lista sharedMatches proviene de las filas válidas disponibles y se enlaza con el owner correcto y una plataforma reconocida. No se fabrican enlaces para filas sin evidencia. Respuestas de API anteriores conservan el fallback agregado de T04.10 con sus limitaciones; si se mezclan filas con y sin evidencia se muestran avisos y solo se calculan coincidencias justificables.

## Cobertura y recursos

Se mantienen los estados y conteos de cobertura de ventana. Se añade el número de partidas sin evidencia completa en relaciones; rendimiento deportivo continúa utilizando las filas válidas aunque falten participantes. Cada respuesta conserva máximo cinco partidas y la proyección aporta hasta diez participantes ligeros por fila: no copia raw JSON, inventarios, runas ni estadísticas innecesarias. El coste es O(partidas × participantes) y memoria local proporcional a la muestra cargada; no hay persistencia, jobs ni estado compartido de análisis.

No cambian admisión, single-flight, caché Riot, timeouts, cancelación, límites históricos ni flujo profundo cinco/tres. El icono agrega solo una petición de asset CDN cuando se renderiza, cacheable; **cero solicitudes Riot API autenticadas adicionales**.

## Pruebas y límites visuales

Se añaden pruebas para transformación del ícono desde HTTP Riot simulado, contrato endpoint aditivo, ícono ausente/inválido, evidencia incompleta, owner/PUUID/equipo ausente, solapamientos, orden de recepción, reconciliación de reintentos y enlaces compartidos. Se conservan tests de timeout, cancelación, parcialidad y ventanas del flujo profundo.

No existe navegador disponible: no se generaron capturas ni se afirma validación visual real de 390/768/1440, contraste o certificación WCAG. Pruebas DOM verifican ícono/fallback, expansión y navegación con mocks. Los contenedores desplegados no se modificaron.

## CI de la rama encadenada

Se amplía únicamente el filtro `pull_request.branches` de `.github/workflows/ci.yml` para incluir la base `feature/t04-10-competitive-dashboard`. Los jobs y pasos de validación existentes no cambian. Sin esta ampliación el PR hijo no ejecutaría CI, porque el filtro anterior solo admitía main y T03.5.

## Evidencia de validación ejecutada

| Validación | Resultado |
|---|---|
| `dotnet restore LolAnalyzer.slnx` (raíz) | Aprobado, SDK Docker .NET 10.0 aislado en ARM64 |
| `dotnet build LolAnalyzer.slnx --configuration Release --no-restore` | Aprobado, 0 warnings / 0 errores |
| `dotnet test LolAnalyzer.slnx --configuration Release --no-build` | 109 unitarias + 21 integración aprobadas; 3 benchmarks/perfiles opt-in omitidos |
| `dotnet format LolAnalyzer.slnx --verify-no-changes --no-restore` | Aprobado |
| `npm ci` | Aprobado, 425 paquetes, Node 24.18.0/npm 11.16.0 |
| `npm test` | 55/55 aprobadas con mocks |
| Pruebas dirigidas tras ajuste final de reconciliación | 9/9 aprobadas |
| `npm run lint` / `npm run type-check` | Aprobados |
| `npm run build` | Aprobado, Next 16.4.2 y todas las rutas existentes |
| `npm audit --omit=dev` | 0 vulnerabilidades de producción; npm ci conserva un aviso high de desarrollo previo |
| `docker compose config --quiet` / `git diff --check` | Aprobados |

Los datos de prueba son sintéticos. El SDK se ejecutó en contenedor temporal sin credenciales y no se alteraron API/web/Redis desplegados. La clave runtime no se leyó ni modificó para esta tarea.
