# Contexto vivo del proyecto

Última actualización: 2026-08-29

## Propósito

LoL Network Analyzer analizará partidas terminadas de League of Legends para responder quién reaparece en el historial de un jugador, con qué frecuencia fue aliado o rival y qué relaciones históricas se pueden inferir sin afirmar información que Riot no proporciona.

## Estado real

- El repositorio contiene la especificación funcional y técnica original, de 104 secciones y cinco sprints.
- Esta entrega prepara la continuidad del proyecto: instrucciones de agente, memoria, skill, arquitectura, decisiones, roadmap y archivos seguros de configuración.
- `docs/SPEC_ANALYSIS.md` registra fortalezas, tensiones y el orden recomendado para Sprint 1.
- Todavía no existen `apps/web`, `apps/api`, `workers/ingestion-worker`, `docker-compose.yml`, solución .NET, migraciones ni servicios ejecutables.
- No se ha seleccionado una licencia.

## Arquitectura objetivo del MVP

- Monorepo con Next.js + TypeScript + Tailwind, .NET 9 Web API, worker de ingesta, PostgreSQL y Redis.
- PostgreSQL será la fuente de verdad. Las partidas terminadas se reutilizan por `riot_match_id` y conservan el JSON original en JSONB.
- PUUID será la identidad interna; Riot ID será la entrada de búsqueda y un atributo mutable.
- Browser y Next.js nunca llamarán directamente a Riot. API/worker concentran secreto, rate limiting, resiliencia y persistencia.
- El despliegue privado inicial será Docker Compose sobre Raspberry Pi ARM64; AWS, monetización y una web pública quedan fuera del MVP inicial.

## Riesgos y preguntas abiertas

- Las políticas, límites y requisitos legales de Riot son temporales; deben verificarse en documentación oficial antes de integrar o publicar.
- La especificación solicita .NET 9 y Next.js 16+. Antes de generar el scaffold se deben comprobar compatibilidad, soporte y disponibilidad reales, conservando la intención salvo decisión documentada.
- Falta decidir si Hangfire vive inicialmente en API, en el worker separado o si el worker usa otra cola compatible con el alcance de Sprint 1.
- Falta decidir la licencia del repositorio.
- Falta definir contraseñas locales seguras y la estrategia inicial de migraciones/health checks sin introducir secretos en Git.

## Validación realizada

- Se leyó y clasificó la especificación completa.
- Se comprobó que el archivo original no contiene valores reales de API keys, contraseñas, tokens o claves privadas.
- Se comparó el patrón de continuidad de `vigilant-adventure` y se adaptó sin copiar datos runtime ni decisiones ajenas.
- La skill del repositorio queda en `.agents/skills/lol-network-analyzer` y la memoria se habilita mediante `.codex/config.toml`.

## Próximo paso recomendado

Ejecutar Sprint 1 como una entrega separada: comprobar toolchains actuales, crear el monorepo y los servicios mínimos, añadir migración inicial, implementar `IRiotApiClient.GetAccountByRiotId`, levantar Docker Compose y verificar todos los health checks.
