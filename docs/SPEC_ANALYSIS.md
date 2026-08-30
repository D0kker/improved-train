# Análisis de la especificación

Última actualización: 2026-08-29

## Lectura ejecutiva

La especificación describe un producto coherente y diferenciable: no intenta replicar un portal generalista de estadísticas, sino construir una red histórica de encuentros alrededor de un jugador. Su activo central no es una pantalla aislada, sino una base de partidas reutilizables y relaciones derivadas que gana valor con cada análisis.

El documento cubre visión, límites legales, arquitectura, modelo de datos, API, ingesta, operación, monetización y cinco sprints. También contiene una frontera de MVP explícita. Esa amplitud es útil como visión, pero exige mantener separados tres horizontes:

1. **Foundation y MVP privado:** Raspberry Pi, Docker Compose, una cuenta inicial, ingesta y jugadores repetidos.
2. **Análisis social avanzado:** relaciones, posibles premades, grupos y grafo.
3. **Producto público:** registro Riot, seguridad pública, privacidad, monetización y cloud.

Mezclar esos horizontes en Sprint 1 produciría sobrearquitectura y dificultaría validar el núcleo.

## Fortalezas

- El diferenciador está formulado como preguntas concretas sobre recurrencia y relaciones.
- PUUID, deduplicación por match ID, JSONB original y PostgreSQL como fuente de verdad forman una base de datos sólida.
- Los límites de seguridad son claros: key solo en servidor, cliente Riot abstraído, concurrencia acotada, `429`, logs seguros y CI sin llamadas reales.
- La especificación distingue inferencia de evidencia; los niveles de confianza no se presentan como probabilidades.
- El roadmap pospone correctamente Neo4j, AWS, pagos, publicidad, RSO y machine learning.
- ARM64 desde el inicio reduce riesgo para Raspberry Pi y una futura migración a Graviton.

## Tensiones que Sprint 1 debe resolver

### API, worker y Hangfire

El documento pide una API, un worker saludable y Hangfire persistente, pero no define dónde vive el servidor de Hangfire ni cómo se comparte el job store. Conviene mantener proyectos separables y escoger una única ejecución inicial para evitar jobs duplicados.

### Redis obligatorio frente a cache abstraída

Docker Compose incluye Redis, mientras `MemoryCacheService` debe permitir desarrollo sin Redis. La readiness no debería volver inutilizable el modo memory-only; la configuración debe expresar si Redis es requerido en cada ambiente.

### Health frente a readiness

`/health` debe comprobar vida del proceso y `/health/ready` sus dependencias requeridas. Ninguno debe consultar Riot en cada petición. El worker necesita una señal observable que no implique exponer un servidor de negocio innecesario.

### Toolchains fijados en una especificación temporal

.NET 9, Next.js 16+ y límites de Riot son datos que pueden cambiar. Antes del scaffold se deben verificar versiones soportadas y políticas actuales, luego documentar cualquier desviación; no se deben actualizar silenciosamente por preferencia del implementador.

### Privacidad y retención

Guardar raw JSON facilita reprocesamiento, pero amplía el conjunto de datos retenidos. Antes de una web pública se necesitarán política de retención, borrado, acceso y tratamiento de Riot IDs/PUUIDs. En el MVP privado se debe evitar publicar dumps y respuestas reales como fixtures.

## Orden recomendado para Sprint 1

1. Fijar versiones y contratos de configuración sin secretos.
2. Crear la solución .NET, el frontend y el worker mínimos.
3. Levantar PostgreSQL y Redis con volúmenes/health checks.
4. Crear entidades, índices y migración inicial.
5. Implementar routing y `IRiotApiClient.GetAccountByRiotId` con HTTP simulado en tests.
6. Añadir Dockerfiles multi-stage y Compose.
7. Validar primero en la arquitectura local y después, si está disponible, construir explícitamente para ARM64.

## Fuera del alcance actual

No corresponde aún implementar relaciones, grafo, detección de grupos, Cloudflare Tunnel, Nginx público, AWS, GitHub Actions de despliegue, monetización ni páginas indexables. Tampoco corresponde usar una key real durante la creación del repositorio.
