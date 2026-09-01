# Contexto vivo del proyecto

Última actualización: 2026-08-31

## Propósito

LoL Network Analyzer será un sitio/sistema de análisis histórico de partidas terminadas de League of Legends. Comienza para uso personal y debe evolucionar, tras validación, hacia un servicio público útil para muchas personas en distintas regiones del mundo: responderá quién reaparece, con qué frecuencia fue aliado o rival y qué relaciones o grupos pueden inferirse sin afirmar información que Riot no proporciona. A futuro podrá sostenerse mediante publicidad u otra estrategia de monetización, siempre subordinada a cumplimiento, privacidad, seguridad y utilidad del producto.

## Estado real

- El repositorio contiene web Next.js, API .NET, worker, PostgreSQL, Redis, pruebas, CI y la especificación original.
- Foundation y Sprints 1–6 están implementados localmente; Sprint 7 inició con auditoría Riot e inventario de datos.
- `docs/SPEC_ANALYSIS.md` registra fortalezas, tensiones y el orden recomendado para Sprint 1.
- El stack Docker está activo con cinco servicios saludables y solo la web publicada en `0.0.0.0:38080`.
- ACCOUNT-V1 resuelve Riot ID a PUUID; MATCH-V5 ingesta hasta 20 partidas, consulta primero PostgreSQL, conserva raw JSONB y normaliza participantes.
- Sprint 3 reconstruye encounters dirigidos owner/other y expone summary, repetidos, historial paginado y detalle de partida.
- La web permite buscar Riot ID, sincronizar el lote acotado y revisar summary, recurrentes, historial y equipos mediante `/api/v1` same-origin.
- En las tablas de posibles conexiones y jugadores recurrentes, cada Riot ID visible enlaza al dashboard del jugador mediante un componente compartido; los datos incompletos permanecen como texto no navegable.
- Sprint 4 contiene el modelo/migración de parejas canónicas, reconstrucción global idempotente, detector prudente, API paginada y vista de relaciones.
- S5-002 incorpora un cálculo puro de familiaridad por partida: usa únicamente historial anterior según `(occurred_at, riot_match_id)`, entrega conocidos/desconocidos/denominador/porcentaje y carga el historial en bloque desde PostgreSQL.
- S5-003 detecta cliques máximos de 3–5 jugadores a partir de parejas clasificadas, conserva solapamientos legítimos y aplica límites configurables antes de la integración en contratos/UI.
- S5-004 expone una red ego de profundidad uno mediante `/api/v1/players/{puuid}/network`, con orden estable heredado de relaciones, filtros por confianza/score, límites configurables y metadata de truncamiento.
- S5-005 muestra la red ego mediante SVG nativo y tabla equivalente, con filtros, zoom/pan por controles, selección accesible, truncamiento y estados de carga/vacío/error sin añadir dependencias.
- S5-006 identifica posibles premades por equipo y muestra familiaridad histórica según el owner transportado desde el historial, con estados explícitos y solo evidencia anterior.
- S5-007 hace lookup local-first, muestra frescura del resumen y reserva las llamadas de sincronización para el botón explícito de actualización.
- S6-001 tiene sus criterios implementados localmente: API start/status/cancel, claim atómico `SKIP LOCKED`, lotes/progreso, estados terminales seguros, requeue al apagar y recuperación por lease. Se validó sin Riot `queued → running → failed`, cancelación y recuperación stale; los datos sintéticos se eliminaron.
- S6-002 excluye solicitudes activas equivalentes con un índice único parcial PostgreSQL; una carrera real de dos peticiones devolvió el mismo job y, después de cancelarlo, una nueva petición creó otro GUID.
- S6-003 pagina hasta 200 IDs desde el progreso durable, reutiliza `riot_match_id` globalmente y serializa la persistencia concurrente del mismo match con advisory lock. La prueba 190/200 descarga exactamente 10 detalles.
- S6-004 centraliza las llamadas Riot de cada proceso mediante `IRiotRateLimiter`: concurrencia configurable, cooldown compartido por routing, `Retry-After`, backoff acotado y cancelación. La ingesta masiva permanece en el worker; varias réplicas requerirán coordinación distribuida.
- S6-005 incorpora `ICacheService`, Redis con fallback en memoria, TTL configurable y tags hash por jugador. El resumen se lee desde caché cuando existe y vuelve a PostgreSQL ante miss/fallo; jobs y sync invalidan tras reconstruir.
- S6-006 agrega schedules durables opt-in por PUUID, frecuencia/cantidad acotadas, claim `SKIP LOCKED` y creación idempotente de jobs sin solapes. La migración está aplicada en runtime; API y worker están saludables.
- S6-007 agrega métricas operativas nativas y snapshots internos en API/worker. Los logs HTTP son estructurados pero omiten path, query, PUUID, Riot ID, payloads y secretos.
- S6-008 verificó reinicio del worker, degradación con Redis caído, deduplicación y 429 simulados; las consultas reales usaron los índices previstos. `docs/OPERATIONS_RUNBOOK.md` contiene evidencia reproducible.
- Sprint 7 inició en `docs/RIOT_READINESS.md`: fuentes oficiales revalidadas el 2026-08-31, matriz de requisitos, inventario inicial y estado público `NO-GO` hasta completar dependencias.
- S7-004 está en curso: `docs/THREAT_MODEL.md` cubre fronteras y respuesta ante compromiso; web/API emiten headers defensivos, la API limita cuerpos a 64 KiB y aplica backpressure concurrente a `/api/v1`. CORS permanece deshabilitado y forwarded headers no se confían hasta definir el proxy. Faltan escaneo reproducible, límite por cliente en el borde y credenciales/operación públicas.
- S7-010 conserva la evaluación futura del crédito Azure y Azure DevOps; no hay activación, migración ni decisión de adoptar Azure.
- S8-007 muestra íconos oficiales de campeón en historial y detalle mediante un proxy Data Dragon same-origin, versionado, cacheado y con fallback accesible. El dato de profile icon no existe aún en el contrato, por lo que no se inventó ni se añadió SUMMONER-V4; podrá ampliarse cuando una historia aporte el dato de forma compatible.
- Sprints 9 y 10 están creados en el tablero: beta privada multi-región (#68–#74) y lanzamiento público sostenible (#75–#81). Planificarlos no autoriza despliegue, publicidad, pagos ni GO público.
- S7-011 registra la protección futura de historial de custom matches: antes de cualquier exposición pública se clasificará la cola y se bloqueará por defecto hasta definir el opt-in aplicable. No habilita RSO ni autenticación por sí misma.
- Los Riot IDs visibles del detalle y de la leyenda de premades enlazan al perfil mediante el componente compartido; datos incompletos permanecen como texto y no provocan sincronización automática.
- El worker base está separado y saludable; los jobs persistentes pertenecen a Sprint 6.
- No se ha seleccionado una licencia.
- Validación de cierre de Sprint 5 (2026-08-31): 43 pruebas unitarias y 15 de integración .NET; pruebas, lint, type-check, formato y build de Next.js; `dotnet format --verify-no-changes`; `docker compose config`; builds Docker de API/web; y cinco servicios saludables con API y web comprobadas en runtime.

## Arquitectura implementada del MVP

- Monorepo con Next.js 16 + TypeScript + Tailwind, .NET 10 LTS, worker, PostgreSQL 18 y Redis 8.
- PostgreSQL será la fuente de verdad. Las partidas terminadas se reutilizan por `riot_match_id` y conservan el JSON original en JSONB.
- PUUID será la identidad interna; Riot ID será la entrada de búsqueda y un atributo mutable.
- Browser y Next.js nunca llamarán directamente a Riot. API/worker concentran secreto, rate limiting, resiliencia y persistencia.
- El despliegue inicial será privado mediante Docker Compose sobre Raspberry Pi ARM64; la transición a una web pública y la monetización son objetivos posteriores, no compromisos de despliegue del MVP.

## Riesgos y preguntas abiertas

- Las políticas, límites y requisitos legales de Riot son temporales; deben verificarse en documentación oficial antes de integrar o publicar.
- Sprint 5 está completado; Sprint 6 debe comenzar por S6-001 porque deduplicación, refresh e incrementalidad dependen del contrato de jobs.
- Sprint 6 está integrado y publicado en `origin/main` mediante `8505212`; sus ocho tarjetas están completadas.
- `ARC-001` medirá .NET frente a Go en ARM64; el benchmark no autoriza una migración.
- Falta decidir la licencia del repositorio.
- Falta definir contraseñas locales seguras y la estrategia inicial de migraciones/health checks sin introducir secretos en Git.
- Antes de abrir el servicio al público habrá que validar registro/auditoría Riot, privacidad, consentimiento/visibilidad, términos, disclaimer, HTTPS, capacidad y una estrategia de monetización no invasiva.

## Validación realizada

- Se leyó y clasificó la especificación completa.
- Se comprobó que el archivo original no contiene valores reales de API keys, contraseñas, tokens o claves privadas.
- Se comparó el patrón de continuidad de `vigilant-adventure` y se adaptó sin copiar datos runtime ni decisiones ajenas.
- La skill del repositorio queda en `.agents/skills/lol-network-analyzer` y la memoria se habilita mediante `.codex/config.toml`.
- Frontend: lint, 4 pruebas, type-check, formato, build local y build Docker exitosos.
- .NET: 41 pruebas unitarias y 12 de integración exitosas tras S5-004; `dotnet format --verify-no-changes`, `docker compose config` y build Release de la API pasan.
- S5-006 parcial: 41 pruebas unitarias y 13 de integración; el contrato de detalle cubre dos grupos, un dúo, un clique de tres, niveles de evidencia y exclusión entre equipos.
- Frontend: 5 pruebas, lint, type-check, formato y build Docker exitosos tras añadir la vista de relaciones.
- Runtime: los cinco servicios están saludables; `/`, `/api/health` y `/openapi/v1.json` responden 200 dentro del contenedor publicado y un jugador inexistente devuelve 404.
- PostgreSQL aplicó `InitialCreate`, `AddPlayerEncounters` y `AddPlayerRelationships`; ambas tablas nuevas existen.
- Transacciones reversibles comprobaron en PostgreSQL la pareja dirigida/única de encounters y la canonicalización, suma, unicidad y rango de score de relationships.
- Una prueba efímera contra PostgreSQL reconstruyó dos veces tres partidas sintéticas en lotes de uno, verificó dos parejas y los contadores ally/opponent/consecutivo, y dejó la base nuevamente vacía.
- Runtime posterior a S5-002: PostgreSQL conserva 1.094 jugadores, 132 partidas y 5.795 relaciones; API/web, worker, PostgreSQL y Redis saludables; `/api/health` responde 200.
- Una verificación efímera de familiaridad contra PostgreSQL obtuvo `known=1`, `unknown=0`, `percentage=100` usando tres partidas sintéticas y eliminó esos datos al terminar.
- Frontend: los enlaces de conexiones y jugadores recurrentes a perfiles codifican correctamente Riot IDs con espacios o `/`; tests, lint, type-check, formato, build Next.js y build Docker pasan. El contenedor web quedó saludable tras recreación.
- Runtime posterior a S5-004: la API fue reconstruida/recreada, los cinco servicios permanecen saludables, `/health` responde `Healthy` y la nueva ruta `/network` devuelve un 404 controlado para un jugador inexistente.
- Frontend posterior a la presentación de premades: 6 pruebas, lint, type-check, Prettier y build Next.js/Docker aprobados; API y web fueron recreadas, `/health` responde `Healthy` y la ruta dinámica de detalle renderiza.
- Navegación desde detalle: el componente compartido enlaza Riot IDs completos tanto en las filas de participantes como en la leyenda de premades; pruebas frontend, lint, type-check, Prettier y build local/Docker pasan, y la web recreada queda saludable.
- S6-004: 48 pruebas unitarias y 18 de integración .NET, `dotnet format --verify-no-changes` y builds Docker de API/worker exitosos. Los casos HTTP simulados cubren 429 con `Retry-After`, reintento acotado, concurrencia y cancelación; no se llamó a Riot.
- S6-005: 51 pruebas unitarias y 18 de integración; formato y builds Docker aprobados. Redis real creó y sirvió el resumen cacheado; con Redis detenido la misma lectura funcionó desde memoria, luego Redis se restauró y los cinco servicios quedaron saludables.
- S6-006: 54 pruebas unitarias y 20 de integración; migración `player_refresh_schedules` aplicada y endpoint de refresh validado con 404 controlado para jugador inexistente. API/worker permanecen saludables sin llamada a Riot.
- Cierre Sprint 6: 55 pruebas unitarias y 20 de integración; formato .NET, `docker compose config`, builds de API/worker, métricas internas y cinco health checks aprobados. Redis caído y reinicio del worker se probaron de forma reversible.
- S7-004/S8-007: 55 pruebas unitarias y 20 de integración .NET; pruebas, lint, type-check, formato y build Next.js; `docker compose config` y builds Docker de API/web aprobados. Los cinco servicios quedaron saludables; web/API devolvieron headers defensivos y `/api/assets/champions/1` sirvió un PNG oficial de 30.267 bytes con caché.
- Se verificaron el 2026-08-30 las políticas oficiales vigentes de Riot: el producto se mantiene post-partida, no desanonimiza jugadores ocultos y trata relaciones como inferencias. Referencias: https://developer.riotgames.com/policies/general y https://developer.riotgames.com/docs/lol

## Próximo paso

Continuar S7-004 con escaneo de supply chain y límites de borde; después tomar S8-002/S8-003 o las historias que el PO priorice, sin abrir el servicio público hasta completar el go/no-go.

## Investigación delegable a Gemini

- El tablero tiene el carril `Para Gemini`: Codex deja allí prompts de investigación; el PO pasa el contenido a Gemini y mueve la tarjeta a `Listo para Codex` cuando adjunta el resultado.
- GM-04–GM-12 fueron revisados el 2026-08-31. Confirman el cierre de Sprint 6 y refinan Sprint 7/ARC-001; se rechazaron plazos, endpoints, métricas y migraciones no respaldados. S7-011 captura la brecha concreta de custom matches/opt-in detectada en GM-09.
