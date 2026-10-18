# PROJECT ECHO

Aplicación histórica y post-partida de League of Legends enfocada en una pregunta: **¿con quién ya había jugado antes?** El nombre técnico interno continúa siendo `LolAnalyzer` para evitar una migración cosmética de namespaces.

La base funcional de los Sprints 1–6 está implementada. ECHO-1 reduce la experiencia a encuentros conocidos, jugadores recurrentes y familiaridad histórica sin eliminar las capacidades existentes.

## Enfoque del producto

- Distinguir jugadores conocidos (al menos un encuentro) de jugadores recurrentes (dos o más).
- Mostrar cuántos participantes de una partida ya habían aparecido antes y quiénes eran.
- Identificar jugadores por PUUID, no por nombre visible.
- Reutilizar partidas persistidas para reducir llamadas a Riot.
- Distinguir encuentros como aliado y como rival.
- Inferir relaciones con lenguaje prudente, sin afirmar duos verificados.
- Mantener la Riot API key únicamente en backend o worker.
- Ejecutar el MVP de forma local en Raspberry Pi mediante Docker Compose.
- Evolucionar gradualmente hacia un servicio público sostenible; la publicidad u otra monetización solo se evaluará después de cumplir políticas Riot, privacidad, seguridad y requisitos legales.

## Arquitectura implementada

```text
Browser -> Next.js -> .NET API -> PostgreSQL
                         |      -> Redis
                         +-----> Riot API
                         +-----> ingestion worker
```

El detalle y la separación entre estado planificado e implementado están en [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md). El análisis de alcance, fortalezas, tensiones y decisiones pendientes está en [`docs/SPEC_ANALYSIS.md`](docs/SPEC_ANALYSIS.md).

## Estado actual

- [x] Especificación inicial analizada y preservada.
- [x] Documentación de arquitectura, decisiones y continuidad creada.
- [x] Configuración de Codex, memoria y skill del repositorio preparada.
- [x] Sprint 1: monorepo, servicios, health checks y primera integración de Riot.
- [x] Sprint 2: ingesta acotada, raw JSONB, normalización y deduplicación de partidas.
- [x] Sprint 3: encounters, analizador de repetidos, APIs de lectura y primeras vistas funcionales.
- [x] Sprint 4: relaciones, detector prudente, API paginada y vista web.
- [x] Sprint 5: familiaridad, grupos y red social.
- [x] Sprint 6: jobs persistentes, worker, resiliencia, caché y operación.
- [x] ECHO-1: enfoque de producto y validación de la experiencia central.
- [ ] Sprint 8: insights históricos y tarjetas compartibles seguras.

Consulta [`docs/TODO.md`](docs/TODO.md) para el roadmap y [`docs/PROJECT_CONTEXT.md`](docs/PROJECT_CONTEXT.md) antes de comenzar una nueva sesión.

## Ejecutar y validar

```bash
cp .env.example .env
# Modo mínimo: PostgreSQL + API + worker + web; caché en memoria.
docker compose up -d --build
docker compose ps
```

Para la instancia local actualmente publicada en el puerto `8081`, usa:

```bash
WEB_PORT=8081 CACHE_PROVIDER=memory docker compose up -d
```

Redis es opcional. Para habilitar caché compartida con fallback local:

```bash
CACHE_PROVIDER=redis docker compose --profile redis up -d --build
```

La web es el único servicio publicado al host. En la instancia local actual está disponible en `http://localhost:8081`; PostgreSQL, API, worker y Redis permanecen privados. La portada y `/api/health` funcionan sin una key; las operaciones Riot devuelven un error controlado mientras `RIOT_API_KEY` no esté configurada.

## Secretos

Copia `.env.example` como `.env`, define credenciales locales y nunca publiques `.env`, una Riot API key, credenciales de PostgreSQL ni tokens.

## Licencia y cumplimiento

La licencia del código aún no está decidida. Antes de una publicación abierta o monetización se deben revisar las políticas vigentes de Riot, registrar el producto cuando corresponda y agregar los avisos legales requeridos. La monetización es una posibilidad futura —publicidad u otra estrategia—, no una condición para el MVP privado.
