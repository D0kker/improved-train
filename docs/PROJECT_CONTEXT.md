# Contexto vivo del proyecto

Última actualización: 2026-08-30

## Propósito

LoL Network Analyzer analizará partidas terminadas de League of Legends para responder quién reaparece en el historial de un jugador, con qué frecuencia fue aliado o rival y qué relaciones históricas se pueden inferir sin afirmar información que Riot no proporciona.

## Estado real

- El repositorio contiene web Next.js, API .NET, worker, PostgreSQL, Redis, pruebas, CI y la especificación original.
- Foundation y Sprints 1–3 están implementados localmente; Sprint 4 inició con S4-001 y S4-002.
- `docs/SPEC_ANALYSIS.md` registra fortalezas, tensiones y el orden recomendado para Sprint 1.
- El stack Docker está activo con cinco servicios saludables y solo la web publicada en `0.0.0.0:38080`.
- ACCOUNT-V1 resuelve Riot ID a PUUID; MATCH-V5 ingesta hasta 20 partidas, consulta primero PostgreSQL, conserva raw JSONB y normaliza participantes.
- Sprint 3 reconstruye encounters dirigidos owner/other y expone summary, repetidos, historial paginado y detalle de partida.
- La web permite buscar Riot ID, sincronizar el lote acotado y revisar summary, recurrentes, historial y equipos mediante `/api/v1` same-origin.
- Sprint 4 ya contiene el modelo/migración de parejas canónicas y el calculador configurable 0–100; aún no reconstruye ni expone relaciones.
- El worker base está separado y saludable; los jobs persistentes pertenecen a Sprint 6.
- No se ha seleccionado una licencia.

## Arquitectura implementada del MVP

- Monorepo con Next.js 16 + TypeScript + Tailwind, .NET 10 LTS, worker, PostgreSQL 18 y Redis 8.
- PostgreSQL será la fuente de verdad. Las partidas terminadas se reutilizan por `riot_match_id` y conservan el JSON original en JSONB.
- PUUID será la identidad interna; Riot ID será la entrada de búsqueda y un atributo mutable.
- Browser y Next.js nunca llamarán directamente a Riot. API/worker concentran secreto, rate limiting, resiliencia y persistencia.
- El despliegue privado inicial será Docker Compose sobre Raspberry Pi ARM64; AWS, monetización y una web pública quedan fuera del MVP inicial.

## Riesgos y preguntas abiertas

- Las políticas, límites y requisitos legales de Riot son temporales; deben verificarse en documentación oficial antes de integrar o publicar.
- La reconstrucción global, possible premade detector, API y UI de relaciones siguen pendientes en Sprint 4.
- Los jobs, deduplicación concurrente y rate limiting global se decidirán e implementarán en Sprint 6.
- `ARC-001` medirá .NET frente a Go en ARM64; el benchmark no autoriza una migración.
- Falta decidir la licencia del repositorio.
- Falta definir contraseñas locales seguras y la estrategia inicial de migraciones/health checks sin introducir secretos en Git.

## Validación realizada

- Se leyó y clasificó la especificación completa.
- Se comprobó que el archivo original no contiene valores reales de API keys, contraseñas, tokens o claves privadas.
- Se comparó el patrón de continuidad de `vigilant-adventure` y se adaptó sin copiar datos runtime ni decisiones ajenas.
- La skill del repositorio queda en `.agents/skills/lol-network-analyzer` y la memoria se habilita mediante `.codex/config.toml`.
- Frontend: lint, 4 pruebas, type-check, formato, build local y build Docker exitosos.
- .NET: 18 pruebas unitarias y 6 de integración, formato y build Docker exitosos.
- Runtime: los cinco servicios están saludables; `/`, `/api/health` y `/openapi/v1.json` responden 200 dentro del contenedor publicado y un jugador inexistente devuelve 404.
- PostgreSQL aplicó `InitialCreate`, `AddPlayerEncounters` y `AddPlayerRelationships`; ambas tablas nuevas existen.
- Transacciones reversibles comprobaron en PostgreSQL la pareja dirigida/única de encounters y la canonicalización, suma, unicidad y rango de score de relationships.
- Se verificaron el 2026-08-30 las políticas oficiales vigentes de Riot: el producto se mantiene post-partida, no desanonimiza jugadores ocultos y trata relaciones como inferencias. Referencias: https://developer.riotgames.com/policies/general y https://developer.riotgames.com/docs/lol

## Próximo paso

Completar S4-003: reconstruir `PlayerRelationship` globalmente desde partidas persistidas, usando el score configurable y sin llamadas adicionales a Riot. Después continuar con detector prudente, API y UI de Sprint 4.
