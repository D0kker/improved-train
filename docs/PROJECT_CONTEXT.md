# Contexto vivo del proyecto

Última actualización: 2026-08-31

## Propósito

LoL Network Analyzer será un sitio/sistema de análisis histórico de partidas terminadas de League of Legends. Comienza para uso personal y debe evolucionar, tras validación, hacia un servicio público útil para muchas personas en distintas regiones del mundo: responderá quién reaparece, con qué frecuencia fue aliado o rival y qué relaciones o grupos pueden inferirse sin afirmar información que Riot no proporciona. A futuro podrá sostenerse mediante publicidad u otra estrategia de monetización, siempre subordinada a cumplimiento, privacidad, seguridad y utilidad del producto.

## Estado real

- El repositorio contiene web Next.js, API .NET, worker, PostgreSQL, Redis, pruebas, CI y la especificación original.
- Foundation y Sprints 1–5 están implementados; Sprint 6 inicia con la formalización de jobs persistentes.
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
- Los jobs, deduplicación concurrente y rate limiting global se decidirán e implementarán en Sprint 6.
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
- Se verificaron el 2026-08-30 las políticas oficiales vigentes de Riot: el producto se mantiene post-partida, no desanonimiza jugadores ocultos y trata relaciones como inferencias. Referencias: https://developer.riotgames.com/policies/general y https://developer.riotgames.com/docs/lol

## Próximo paso

Implementar S6-001: formalizar jobs persistentes con estados, progreso, error seguro y endpoints start/status antes de mover la sincronización al worker.
