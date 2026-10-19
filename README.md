# PROJECT ECHO

Análisis de partidas terminadas de League of Legends: jugadores encontrados, encuentros como aliado/rival, familiaridad e inferencias prudentes de posibles premades. La identidad interna es PUUID, no el nombre mutable.

## Arquitectura del checkout T03.5

```text
Browser → Next.js 16 / React 19 → ASP.NET Core .NET 10 → Riot API
                                      ↓
                              Objetos por solicitud
                              Caché Riot opcional
```

La API no requiere base de datos ni conserva resultados funcionales entre solicitudes. La caché solo evita llamadas repetidas. Los algoritmos y optimizaciones T01–T03 se conservan; EF Core, Redis y los jobs históricos quedan fuera del runtime de la API.

- Búsqueda: Riot ID → identidad y últimas cinco partidas con resumen y cobertura.
- Partida seleccionada: análisis independiente con cinco partidas históricas del owner y tres de cada otro participante; excluye objetivo y futuro, deduplica partidas y señala datos parciales o insuficientes.
- La API key permanece en el backend. El navegador usa únicamente rutas same-origin.
- El análisis es heurístico; nunca confirma una premade ni identifica jugadores ocultos.

Consulta [arquitectura y mediciones T03.5](docs/T03_5_STATELESS_ARCHITECTURE.md), [contexto actual](docs/PROJECT_CONTEXT.md), [decisiones](docs/DECISIONS.md) y [roadmap](docs/TODO.md).

La rama `feature/t03-6-resource-protection` añade [protección de recursos y carga simulada](docs/T03_6_RESOURCE_PROTECTION.md); no está desplegada. El servicio de8081 sigue en T03.5.

## Validación local

```bash
dotnet restore LolAnalyzer.slnx
dotnet build LolAnalyzer.slnx --configuration Release --no-restore
dotnet test LolAnalyzer.slnx --configuration Release --no-build
dotnet format LolAnalyzer.slnx --verify-no-changes --no-restore
cd apps/web
npm test
npm run lint
npm run type-check
npm run build
```

Las pruebas usan Riot simulado. Los repositorios anteriores se mantienen en `LolAnalyzer.Legacy` para regresiones/benchmarks; ese proyecto no forma parte del artefacto API. El benchmark opcional se ejecuta con `RUN_STATELESS_BENCHMARK=1`, como indica el informe.

## Ejecución futura, solo con autorización de despliegue

Compose define únicamente API y web. La configuración ignorada `.env` conserva la key Riot en runtime; jamás publicarla. `Cache__Enabled=false` desactiva la caché sin cambiar los flujos.

```bash
WEB_PORT=8081 docker compose up -d --build
```

La refactorización T03.5 se entregó inicialmente sin desplegar. Por instrucción posterior explícita del PO, se desplegó el 2026-10-18 (Panamá) en 8081 desde `f14f328`/código `3afd6c6`: API/web recreadas y saludables, sin cambiar secretos ni hacer merge. Ver evidencia en [contexto operativo](docs/PROJECT_CONTEXT.md). Las imágenes pueden validarse sin iniciar servicios mediante `docker compose -p t035-validation build`; cualquier siguiente despliegue requiere autorización nueva.

## Licencia y cumplimiento

La licencia aún está pendiente. Una publicación pública o monetización requiere las validaciones de registro, políticas Riot, privacidad, seguridad y go/no-go documentadas en el proyecto.
