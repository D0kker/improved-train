# Contexto vivo del proyecto

Última actualización: 2026-08-30

## Propósito

LoL Network Analyzer analizará partidas terminadas de League of Legends para responder quién reaparece en el historial de un jugador, con qué frecuencia fue aliado o rival y qué relaciones históricas se pueden inferir sin afirmar información que Riot no proporciona.

## Estado real

- El repositorio contiene web Next.js, API .NET, worker, PostgreSQL, Redis, pruebas, CI y la especificación original.
- Foundation, Sprint 1 y Sprint 2 están implementados localmente; Sprint 3 inició con S3-001 y S3-002.
- `docs/SPEC_ANALYSIS.md` registra fortalezas, tensiones y el orden recomendado para Sprint 1.
- El stack Docker está activo con cinco servicios saludables y solo la web publicada en `0.0.0.0:38080`.
- ACCOUNT-V1 resuelve Riot ID a PUUID; MATCH-V5 ingesta hasta 20 partidas, consulta primero PostgreSQL, conserva raw JSONB y normaliza participantes.
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
- Los jobs, deduplicación concurrente y rate limiting global se decidirán e implementarán en Sprint 6.
- `ARC-001` medirá .NET frente a Go en ARM64; el benchmark no autoriza una migración.
- Falta decidir la licencia del repositorio.
- Falta definir contraseñas locales seguras y la estrategia inicial de migraciones/health checks sin introducir secretos en Git.

## Validación realizada

- Se leyó y clasificó la especificación completa.
- Se comprobó que el archivo original no contiene valores reales de API keys, contraseñas, tokens o claves privadas.
- Se comparó el patrón de continuidad de `vigilant-adventure` y se adaptó sin copiar datos runtime ni decisiones ajenas.
- La skill del repositorio queda en `.agents/skills/lol-network-analyzer` y la memoria se habilita mediante `.codex/config.toml`.
- Frontend: lint, prueba, type-check, formato y build exitosos; npm sin vulnerabilidades reportadas.
- .NET: 5 pruebas unitarias y 4 de integración, formato y auditoría NuGet sin vulnerabilidades reportadas.
- Runtime: `/`, `/api/health` y `/openapi/v1.json` responden 200; una sincronización sin key devuelve 503 controlado.
- PostgreSQL contiene `players`, `matches`, `match_participants` y `__EFMigrationsHistory` después de aplicar la migración.

## Próximo paso

Publicar el cierre de Sprint 1/2 y ejecutar las dos primeras tareas de Sprint 3: persistir `PlayerEncounter` e implementar el analizador idempotente de jugadores repetidos. Las vistas visuales llegan después de los contratos de summary/historial dentro del mismo Sprint 3.
