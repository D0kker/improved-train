# Contexto vivo del proyecto

Última actualización: 2026-08-30

## Propósito

LoL Network Analyzer será un sitio/sistema de análisis histórico de partidas terminadas de League of Legends. Comienza para uso personal y debe evolucionar, tras validación, hacia un servicio público útil para muchas personas en distintas regiones del mundo: responderá quién reaparece, con qué frecuencia fue aliado o rival y qué relaciones o grupos pueden inferirse sin afirmar información que Riot no proporciona. A futuro podrá sostenerse mediante publicidad u otra estrategia de monetización, siempre subordinada a cumplimiento, privacidad, seguridad y utilidad del producto.

## Estado real

- El repositorio contiene web Next.js, API .NET, worker, PostgreSQL, Redis, pruebas, CI y la especificación original.
- Foundation y Sprints 1–3 están implementados; Sprint 4 queda completo con S4-001 a S4-006.
- `docs/SPEC_ANALYSIS.md` registra fortalezas, tensiones y el orden recomendado para Sprint 1.
- El stack Docker está activo con cinco servicios saludables y solo la web publicada en `0.0.0.0:38080`.
- ACCOUNT-V1 resuelve Riot ID a PUUID; MATCH-V5 ingesta hasta 20 partidas, consulta primero PostgreSQL, conserva raw JSONB y normaliza participantes.
- Sprint 3 reconstruye encounters dirigidos owner/other y expone summary, repetidos, historial paginado y detalle de partida.
- La web permite buscar Riot ID, sincronizar el lote acotado y revisar summary, recurrentes, historial y equipos mediante `/api/v1` same-origin.
- Sprint 4 contiene el modelo/migración de parejas canónicas, reconstrucción global idempotente, detector prudente, API paginada y vista de relaciones.
- El worker base está separado y saludable; los jobs persistentes pertenecen a Sprint 6.
- No se ha seleccionado una licencia.

## Arquitectura implementada del MVP

- Monorepo con Next.js 16 + TypeScript + Tailwind, .NET 10 LTS, worker, PostgreSQL 18 y Redis 8.
- PostgreSQL será la fuente de verdad. Las partidas terminadas se reutilizan por `riot_match_id` y conservan el JSON original en JSONB.
- PUUID será la identidad interna; Riot ID será la entrada de búsqueda y un atributo mutable.
- Browser y Next.js nunca llamarán directamente a Riot. API/worker concentran secreto, rate limiting, resiliencia y persistencia.
- El despliegue inicial será privado mediante Docker Compose sobre Raspberry Pi ARM64; la transición a una web pública y la monetización son objetivos posteriores, no compromisos de despliegue del MVP.

## Riesgos y preguntas abiertas

- Las políticas, límites y requisitos legales de Riot son temporales; deben verificarse en documentación oficial antes de integrar o publicar.
- Sprint 5 es el siguiente alcance de producto; sus historias están refinadas pero aún no iniciadas.
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
- .NET: 28 pruebas unitarias y 8 de integración, formato y build Docker exitosos.
- Frontend: 5 pruebas, lint, type-check, formato y build Docker exitosos tras añadir la vista de relaciones.
- Runtime: los cinco servicios están saludables; `/`, `/api/health` y `/openapi/v1.json` responden 200 dentro del contenedor publicado y un jugador inexistente devuelve 404.
- PostgreSQL aplicó `InitialCreate`, `AddPlayerEncounters` y `AddPlayerRelationships`; ambas tablas nuevas existen.
- Transacciones reversibles comprobaron en PostgreSQL la pareja dirigida/única de encounters y la canonicalización, suma, unicidad y rango de score de relationships.
- Una prueba efímera contra PostgreSQL reconstruyó dos veces tres partidas sintéticas en lotes de uno, verificó dos parejas y los contadores ally/opponent/consecutivo, y dejó la base nuevamente vacía.
- Runtime posterior a la integración: PostgreSQL conserva 781 jugadores, 94 partidas y 4.127 relaciones; API/web, worker, PostgreSQL y Redis saludables; `/api/health` responde 200.
- Se verificaron el 2026-08-30 las políticas oficiales vigentes de Riot: el producto se mantiene post-partida, no desanonimiza jugadores ocultos y trata relaciones como inferencias. Referencias: https://developer.riotgames.com/policies/general y https://developer.riotgames.com/docs/lol

## Próximo paso

Iniciar S5-002: calcular familiaridad histórica con orden estricto `(occurred_at, riot_match_id)` y sin fuga de información futura. Sprint 8 queda creado como alcance posterior de insights históricos.
