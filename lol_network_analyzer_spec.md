# LoL Player Network Analyzer
## Especificación funcional y técnica — MVP

**Estado:** Diseño inicial  
**Objetivo inicial:** Raspberry Pi / Docker Compose  
**Objetivo futuro:** Aplicación web pública y autosostenible  
**Juego:** League of Legends  
**Proveedor de datos:** Riot Games API

---

# 1. Visión del proyecto

Crear una aplicación web que analice el historial de partidas de League of Legends de un jugador para descubrir relaciones y patrones entre los jugadores que aparecen en sus partidas.

El producto debe ir más allá de mostrar estadísticas tradicionales como KDA, win rate o campeones.

El enfoque principal será responder preguntas como:

- ¿Con qué jugadores me he encontrado anteriormente?
- ¿Cuántas veces he jugado con una persona?
- ¿Cuántas veces ha sido mi compañero?
- ¿Cuántas veces ha sido rival?
- ¿Cuál es mi win rate jugando con esa persona?
- ¿Cuál es mi win rate jugando contra esa persona?
- ¿Hay jugadores dentro de una partida que juegan juntos frecuentemente?
- ¿Dos jugadores parecen ser un duo?
- ¿Hay grupos de 3 o más jugadores que juegan juntos regularmente?
- ¿Qué tan frecuentemente se repiten jugadores dentro de mis partidas?
- ¿Quién podría considerarse mi "nemesis"?
- ¿Quién ha sido uno de mis mejores compañeros recurrentes?
- ¿Cómo se relacionan entre sí los jugadores encontrados en mi historial?

La idea a largo plazo es construir una especie de:

**"Social Graph / Matchmaking Network de League of Legends".**

---

# 2. Principio importante

La aplicación será inicialmente una herramienta de:

**análisis histórico y post-partida.**

No debe utilizarse para revelar información oculta de una partida activa ni intentar desanonimizar jugadores que Riot haya decidido ocultar.

Debe cumplir las políticas vigentes de Riot Games.

Riot exige, entre otras cosas:

- registrar productos que sirvan a jugadores;
- no revelar información específica de una sesión que el jugador no conocería;
- no desanonimizar jugadores que no sean razonablemente identificables mediante información visible;
- proteger la API key;
- utilizar HTTPS;
- no incluir la API key en frontend o código distribuido;
- registrar y aprobar/acknowledge el producto antes de monetizarlo.

La aplicación deberá incluir posteriormente el disclaimer legal requerido por Riot.

Referencia:

https://developer.riotgames.com/docs/lol

https://developer.riotgames.com/policies/general

---

# 3. Modelo de producto

## Fase inicial

Uso personal.

El sistema correrá en una Raspberry Pi y analizará inicialmente una cuenta.

Ejemplo:

```text
Riot ID:
Eagly#LAN
```

El sistema descargará aproximadamente:

```text
100 - 200 partidas
```

y construirá una base local de jugadores y relaciones.

---

# 4. Evolución prevista

```text
Personal Tool
     ↓
MVP funcional
     ↓
Web privada
     ↓
Prototype
     ↓
Riot Production API application
     ↓
Web pública
     ↓
Usuarios reales
     ↓
Publicidad
     ↓
Infraestructura autosostenible
     ↓
Premium opcional
```

El primer objetivo económico NO es generar grandes beneficios.

El objetivo inicial es:

```text
Ingresos mensuales >= infraestructura mensual
```

Después:

```text
Ingresos
   -
Infraestructura
   =
Beneficio
```

---

# 5. Riot API Keys

Durante desarrollo se utilizará una Development API Key.

Una Development Key expira aproximadamente cada 24 horas.

Para un proyecto privado pequeño se puede solicitar una:

**Personal API Key**

Actualmente Riot documenta para Personal Keys:

```text
20 requests / second
100 requests / 2 minutes
```

por región.

Una Personal Key NO debe utilizarse para publicar una aplicación abierta al público.

Cuando exista un prototipo funcional se deberá solicitar:

**Production API Key**

El límite inicial actualmente documentado para Production es:

```text
500 requests / 10 seconds
30,000 requests / 10 minutes
```

por región.

Todos los límites deben considerarse configurables y nunca hardcodearse dentro del código.

---

# 6. Stack tecnológico

## Frontend

```text
Next.js
TypeScript
Tailwind CSS
```

Preferencia:

```text
Next.js 16+
```

Utilizar:

- App Router
- Server Components donde tengan sentido
- Client Components solamente cuando se necesite interacción
- TypeScript strict
- responsive design
- dark mode preparado desde el principio

---

# 7. Backend

```text
.NET 9 Web API
```

Responsabilidades:

- comunicación con Riot API;
- validación;
- caching;
- lógica de negocio;
- análisis de partidas;
- relaciones entre jugadores;
- detección de grupos;
- control de rate limits;
- creación de background jobs;
- servir API al frontend.

Arquitectura preferida:

```text
Clean Architecture ligera
```

Evitar hacer una arquitectura excesivamente compleja para el MVP.

---

# 8. Base de datos

Usar:

```text
PostgreSQL
```

Utilizar:

```text
Entity Framework Core
```

Migraciones manejadas mediante EF Core.

PostgreSQL será también el origen principal para los análisis.

NO utilizar Neo4j inicialmente.

Neo4j podrá evaluarse posteriormente cuando el social graph tenga suficiente complejidad.

---

# 9. Cache

Utilizar:

```text
Redis
```

Pero la aplicación deberá abstraer el mecanismo de caching.

Objetivo:

```text
ICacheService
```

Implementaciones posibles:

```text
RedisCacheService
MemoryCacheService
```

Esto permitirá ejecutar el sistema incluso sin Redis durante desarrollo.

---

# 10. Background processing

Inicialmente:

```text
Hangfire
```

almacenando los jobs de manera persistente.

Alternativamente se podrá construir un Worker Service independiente.

Diseñar desde el principio para permitir posteriormente:

```text
API
+
Worker
```

como procesos separados.

---

# 11. Arquitectura inicial

```text
                         Internet
                             │
                             ▼
                    Cloudflare Tunnel
                             │
                             ▼
                        Raspberry Pi
                             │
                       Docker Compose
                             │
          ┌──────────────────┼───────────────────┐
          │                  │                   │
          ▼                  ▼                   ▼
       Next.js           .NET API             Worker
          │                  │                   │
          └──────────────────┼───────────────────┘
                             │
                   ┌─────────┴─────────┐
                   ▼                   ▼
               PostgreSQL            Redis
```

Se deberá colocar un reverse proxy delante de los servicios cuando sea necesario.

Preferencia:

```text
Nginx
```

---

# 12. Arquitectura del repositorio

Utilizar monorepo.

```text
lol-network-analyzer/
│
├── apps/
│   ├── web/
│   │
│   └── api/
│
├── workers/
│   └── ingestion-worker/
│
├── infrastructure/
│   ├── docker/
│   └── aws/
│
├── docs/
│
├── scripts/
│
├── docker-compose.yml
├── .env.example
├── README.md
└── LICENSE
```

Backend:

```text
apps/api/
│
├── src/
│   ├── LolAnalyzer.Api
│   ├── LolAnalyzer.Application
│   ├── LolAnalyzer.Domain
│   └── LolAnalyzer.Infrastructure
│
└── tests/
    ├── LolAnalyzer.UnitTests
    └── LolAnalyzer.IntegrationTests
```

---

# 13. Seguridad de Riot API Key

Nunca:

```text
NEXT_PUBLIC_RIOT_API_KEY
```

Nunca enviar la Riot API Key al browser.

Nunca almacenarla en Git.

Utilizar:

```text
RIOT_API_KEY
```

como environment variable únicamente en backend/worker.

`.gitignore` debe contener:

```text
.env
.env.local
.env.production
```

Incluir únicamente:

```text
.env.example
```

Ejemplo:

```text
RIOT_API_KEY=
RIOT_PLATFORM_REGION=la1
RIOT_REGIONAL_ROUTING=americas

POSTGRES_HOST=postgres
POSTGRES_PORT=5432
POSTGRES_DB=lol_analyzer
POSTGRES_USER=lol
POSTGRES_PASSWORD=

REDIS_HOST=redis
REDIS_PORT=6379
```

---

# 14. Riot ID

Identificar jugadores internamente por:

```text
PUUID
```

NO utilizar Summoner Name como identificador primario.

El usuario buscará:

```text
GameName#TagLine
```

Ejemplo:

```text
Eagly#LAN
```

Flujo:

```text
GameName + TagLine
        ↓
ACCOUNT-V1
        ↓
PUUID
        ↓
MATCH-V5
```

---

# 15. Riot API Client

Crear una abstracción:

```csharp
IRiotApiClient
```

Responsabilidades aproximadas:

```text
GetAccountByRiotId()
GetMatchIds()
GetMatch()
```

No acoplar Application Layer directamente a HttpClient.

Utilizar:

```text
HttpClientFactory
```

Configurar:

- retry;
- timeout;
- handling 429;
- handling 404;
- handling 5xx;
- rate limit headers;
- structured logging.

Para retries utilizar:

```text
Polly
```

o resilience pipeline equivalente disponible en .NET.

---

# 16. Regiones

Separar claramente:

```text
Platform Routing
Regional Routing
```

Ejemplo LAN:

```text
Platform:
la1
```

Match-v5 utilizará el regional routing correspondiente:

```text
americas
```

No hardcodear LAN como única región.

Crear enums/configuration para soportar posteriormente:

```text
LAN
LAS
NA
BR
EUW
EUNE
KR
JP
OCE
etc.
```

---

# 17. Persistencia

Una partida terminada debe considerarse esencialmente inmutable para nuestro uso.

Antes de llamar a Riot:

```text
¿Match existe en DB?

YES
 ↓
usar PostgreSQL

NO
 ↓
Riot API
 ↓
guardar DB
 ↓
usar DB
```

Esto es fundamental para disminuir el uso de Riot API.

---

# 18. Raw Riot Data

Además de normalizar datos importantes, guardar el JSON original de Riot.

PostgreSQL:

```sql
raw_data JSONB
```

Razón:

si posteriormente necesitamos analizar un campo que inicialmente no almacenamos, podremos reprocesar las partidas sin volver a consultar Riot.

---

# 19. Modelo inicial de datos

## Players

```text
players

id
puuid
game_name
tag_line
platform_region
created_at
updated_at
last_seen_at
```

`puuid` debe ser UNIQUE.

---

# 20. Matches

```text
matches

id
riot_match_id
queue_id
game_creation
game_start_timestamp
game_end_timestamp
game_duration
game_version
raw_data
created_at
```

`riot_match_id` debe ser UNIQUE.

---

# 21. MatchParticipants

```text
match_participants

id
match_id
player_id
team_id
participant_id

champion_id
champion_name

team_position
individual_position

kills
deaths
assists

win

gold_earned
total_damage_dealt_to_champions
vision_score
cs

created_at
```

Constraints importantes:

```text
UNIQUE(match_id, player_id)
```

---

# 22. Player Encounters

Tabla agregada:

```text
player_encounters

owner_player_id
other_player_id

total_matches

same_team_matches
enemy_team_matches

wins_together
losses_together

wins_against
losses_against

first_seen_at
last_seen_at
```

Esto permite rápidamente mostrar jugadores recurrentes.

---

# 23. Player Relationships

Crear una tabla independiente para detectar relaciones entre dos jugadores cualesquiera.

```text
player_relationships

player_a_id
player_b_id

matches_together
same_team_matches
opposite_team_matches

recent_matches_together
consecutive_matches

first_seen_at
last_seen_at

relationship_score
relationship_confidence
```

Ordenar siempre los IDs para evitar:

```text
A → B
B → A
```

duplicados.

Ejemplo:

```text
player_a_id < player_b_id
```

---

# 24. Repeated Player Analyzer

El primer algoritmo importante será:

```text
RepeatedPlayerAnalyzer
```

Para las últimas N partidas del jugador:

determinar:

```text
Jugador
Total encounters
As ally
As enemy
Wins together
Losses together
Wins against
Losses against
Last encounter
First encounter
```

Ejemplo:

```text
PlayerABC#LAN

12 encounters

8 ally
4 enemy

Together:
5W 3L

Against:
3W 1L
```

---

# 25. Match Familiarity

Para cada partida calcular:

```text
known_players
unknown_players
familiarity_percentage
```

Ejemplo:

```text
9 jugadores posibles

4 ya habían aparecido anteriormente

Lobby familiarity:

44.4%
```

Esto permitirá mostrar:

```text
"You had previously encountered 4 players in this match."
```

---

# 26. Detección de posibles duos

IMPORTANTE:

No afirmar que dos personas SON duo cuando Riot API no proporciona evidencia directa.

La herramienta solamente hará una inferencia basada en comportamiento histórico.

Utilizar términos como:

```text
Possible premade
Likely premade
Relationship confidence
```

NO:

```text
Verified Duo
```

salvo que en algún futuro exista una fuente oficial que lo confirme.

---

# 27. Relationship Score

Construir inicialmente un algoritmo heurístico.

Ejemplo conceptual:

```text
relationshipScore =
    matchesTogetherWeight
  + recentFrequencyWeight
  + consecutiveMatchesWeight
  + sameTeamWeight
```

Factores:

### Matches Together

Cuántas partidas aparecen juntos.

### Recent Frequency

Qué porcentaje de partidas recientes comparten.

### Consecutive Matches

Si aparecen juntos en partidas consecutivas.

Ejemplo:

```text
7 partidas consecutivas
```

es una señal más fuerte que:

```text
7 partidas durante 3 años
```

### Same Team

Los premades deberían aparecer frecuentemente en el mismo equipo.

---

# 28. Confidence Levels

Inicialmente NO mostrar porcentajes falsamente precisos.

Utilizar:

```text
LOW
MEDIUM
HIGH
VERY_HIGH
```

Ejemplo inicial configurable:

```text
LOW
score < 25

MEDIUM
25 - 49

HIGH
50 - 74

VERY_HIGH
>= 75
```

Los thresholds deberán estar centralizados y ser configurables.

No asumir que estos números representan una probabilidad estadística.

---

# 29. Ejemplo de resultado

```text
PlayerA#LAN
+
PlayerB#LAN

Relationship:
VERY HIGH

18 games together
12 consecutive games
16 during last 30 days

Likely premade
```

---

# 30. Detección de grupos

Después de relaciones A-B, detectar grupos:

```text
A-B
A-C
B-C
```

Si todas las relaciones son fuertes:

```text
A
B
C

Possible premade group
```

Posteriormente soportar:

```text
duo
trio
4-player group
5-player group
```

Utilizar algoritmos de grafos cuando sea necesario.

Inicialmente puede implementarse usando PostgreSQL y código en backend.

---

# 31. Social Graph

Cada jugador:

```text
Node
```

Cada relación:

```text
Edge
```

Ejemplo:

```text
          Juan
         /    \
       18      16
       /        \
    Pedro --17-- Maria
```

El peso del edge:

```text
matches together
```

o:

```text
relationship score
```

---

# 32. Visualización del grafo

En frontend evaluar:

```text
Cytoscape.js
```

o:

```text
D3.js
```

Preferir Cytoscape.js inicialmente por simplicidad para grafos.

La visualización debe permitir:

- zoom;
- pan;
- click en jugador;
- tamaño basado en encounters;
- grosor de edge basado en relationship strength;
- filtros.

---

# 33. Páginas del MVP

## Home

```text
/
```

Contenido:

```text
Logo / nombre

Search Riot ID

[ Game Name ] #[ Tag ]
[ Analyze ]
```

---

# 34. Player Overview

```text
/player/{gameName}/{tagLine}
```

Mostrar:

```text
Riot ID
Profile icon
Level

Matches analyzed

Wins
Losses
Win rate

Unique players encountered

Repeated players

Possible relationships detected
```

---

# 35. Match History

```text
/player/{gameName}/{tagLine}/matches
```

Mostrar partidas recientes.

Cada partida:

```text
Champion
K/D/A
Result
Queue
Duration
Date
```

---

# 36. Repeated Players

```text
/player/{gameName}/{tagLine}/repeated
```

Tabla:

```text
Player
Encounters
Ally
Enemy
Together WR
Against WR
Last Seen
```

Ordenar inicialmente por:

```text
total encounters DESC
```

---

# 37. Relationships

```text
/player/{gameName}/{tagLine}/relationships
```

Mostrar:

```text
Possible duos
Possible groups
Relationship scores
```

---

# 38. Social Graph

```text
/player/{gameName}/{tagLine}/network
```

Mapa visual.

No es obligatorio para MVP v0.1.

Puede ser MVP v0.2.

---

# 39. Match Detail

```text
/match/{matchId}
```

Mostrar:

```text
Blue Team

Top
Jungle
Mid
ADC
Support

Red Team

Top
Jungle
Mid
ADC
Support
```

Marcar relaciones detectadas.

---

# 40. Historial entre dos jugadores

Posteriormente:

```text
/relationship/{playerA}/{playerB}
```

Mostrar:

```text
Matches together
Same team
Enemies
First match
Latest match
Win rate together
Relationship history
```

---

# 41. API REST inicial

Base:

```text
/api/v1
```

---

# 42. Player Lookup

```http
GET /api/v1/players/by-riot-id/{gameName}/{tagLine}
```

---

# 43. Start Analysis

```http
POST /api/v1/players/{puuid}/analysis
```

Body:

```json
{
  "matchCount": 200
}
```

Respuesta:

```json
{
  "jobId": "...",
  "status": "queued"
}
```

---

# 44. Analysis Status

```http
GET /api/v1/jobs/{jobId}
```

Ejemplo:

```json
{
  "status": "running",
  "matchesRequested": 200,
  "matchesProcessed": 87
}
```

---

# 45. Player Summary

```http
GET /api/v1/players/{puuid}/summary
```

---

# 46. Matches

```http
GET /api/v1/players/{puuid}/matches
```

Query params:

```text
page
pageSize
queue
```

---

# 47. Repeated Players

```http
GET /api/v1/players/{puuid}/encounters
```

---

# 48. Relationships

```http
GET /api/v1/players/{puuid}/relationships
```

---

# 49. Network

```http
GET /api/v1/players/{puuid}/network
```

Respuesta:

```json
{
  "nodes": [],
  "edges": []
}
```

---

# 50. Match Detail

```http
GET /api/v1/matches/{matchId}
```

---

# 51. Ingestion Pipeline

Cuando se analiza un jugador:

```text
Riot ID
   ↓
Resolve PUUID
   ↓
Store/update player
   ↓
Retrieve match IDs
   ↓
For each Match ID
   ↓
DB lookup
   ↓
   ├── Exists → skip Riot request
   │
   └── Missing
          ↓
       Riot Match-V5
          ↓
       Store raw JSON
          ↓
       Normalize participants
          ↓
       Store Players
          ↓
       Store MatchParticipants
   ↓
Rebuild encounters
   ↓
Update relationships
   ↓
Analysis complete
```

---

# 52. Incremental updates

Nunca volver a descargar 200 partidas completas si ya tenemos 190.

Ejemplo:

```text
DB contains:
190 matches

Riot history:
200 matches

Download only:
10 missing matches
```

---

# 53. API Rate Limiting

Crear:

```text
IRiotRateLimiter
```

El cliente debe respetar:

```text
429 Too Many Requests
```

Leer headers de Riot cuando estén disponibles.

Nunca generar loops agresivos contra Riot API.

Implementar:

```text
bounded concurrency
queue
retry with backoff
```

---

# 54. Concurrency

No hacer:

```text
200 matches
=
200 simultaneous requests
```

Utilizar concurrency control.

Ejemplo inicial:

```text
2-5 concurrent Riot requests
```

configurable.

---

# 55. Caching

Ejemplos:

```text
Account lookup:
1 hour+

Match:
effectively permanent

Player profile:
configurable

Analysis response:
minutes
```

No depender de cache para información persistente.

PostgreSQL es el source of truth.

---

# 56. Observability

Desde el MVP:

structured logs.

Utilizar:

```text
Serilog
```

Log fields:

```text
CorrelationId
MatchId
PUUID
RiotEndpoint
HttpStatus
Duration
CacheHit
```

NO imprimir:

```text
RIOT_API_KEY
```

---

# 57. Healthchecks

Backend:

```http
GET /health
```

y:

```http
GET /health/ready
```

Comprobar:

```text
PostgreSQL
Redis
```

No hacer una llamada Riot en cada healthcheck.

---

# 58. Metrics futuras

Preparar para OpenTelemetry.

Métricas interesantes:

```text
riot_api_requests_total
riot_api_errors_total
riot_api_429_total

matches_ingested_total

match_cache_hits_total
match_cache_misses_total

analysis_duration_seconds

active_jobs

database_query_duration
```

---

# 59. Docker

Todas las aplicaciones deben tener Dockerfile.

Construir imágenes:

```text
linux/arm64
linux/amd64
```

Motivo:

```text
Raspberry Pi = ARM64
AWS Graviton = ARM64
Laptop/servidores pueden ser AMD64
```

---

# 60. Docker Compose

Servicios iniciales:

```yaml
services:
  web:
  api:
  worker:
  postgres:
  redis:
  nginx:
```

Crear healthchecks y dependencies correctas.

---

# 61. Raspberry Pi

La Raspberry Pi será el environment inicial de producción privada.

Objetivo:

```text
$0 cloud compute cost
```

No depender de servicios administrados para ejecutar el MVP.

Persistir PostgreSQL mediante Docker Volume.

Realizar backups periódicos.

---

# 62. Acceso externo

Utilizar inicialmente:

```text
Cloudflare Tunnel
```

para evitar abrir directamente puertos del router.

Arquitectura:

```text
Internet
  ↓
Cloudflare
  ↓
Tunnel
  ↓
Nginx
  ↓
Web/API
```

---

# 63. CI/CD

Inicialmente evaluar:

```text
GitHub Actions
```

Pipeline:

```text
push
 ↓
lint
 ↓
tests
 ↓
build
 ↓
Docker build
 ↓
multiarch image
 ↓
container registry
 ↓
deploy Raspberry
```

Deployment automático puede implementarse después del MVP.

---

# 64. AWS futuro

Primera migración cloud económica:

```text
EC2 Graviton
Docker Compose
```

Idealmente:

```text
t4g.small
```

o equivalente apropiado al momento de desplegar.

NO comenzar directamente con una arquitectura costosa.

---

# 65. AWS arquitectura madura

Cuando existan usuarios:

```text
Route53
    │
    ▼
CloudFront
    │
    ▼
Load Balancer
    │
    ▼
ECS Fargate
 ├── Web/API
 └── Worker
    │
    ├── RDS PostgreSQL
    └── ElastiCache Redis
```

Servicios adicionales:

```text
ECR
CloudWatch
Secrets Manager
WAF
ACM
```

---

# 66. Infraestructura como código

Cuando se migre a AWS:

preferir:

```text
Terraform
```

La carpeta será:

```text
/infrastructure/aws
```

No implementar AWS inicialmente salvo archivos/documentación necesaria para una futura migración.

---

# 67. Monetización futura

El producto podrá utilizar:

```text
Publicidad
```

como primer modelo.

Objetivo:

```text
Publicidad mensual
>=
Infraestructura mensual
```

No suponer RPM fijo ni ingresos garantizados.

Los ingresos dependen de:

- ubicación de visitantes;
- páginas vistas;
- sesión;
- fill rate;
- plataforma publicitaria;
- consentimiento/cookies;
- ad blockers;
- tipo de contenido;
- demanda publicitaria.

---

# 68. Diseño de publicidad

Evitar publicidad invasiva.

Ejemplo:

```text
Dashboard

Player summary

[ Advertisement ]

Repeated players

Relationships

[ Advertisement ]
```

No colocar anuncios cerca de controles de manera que provoquen clicks accidentales.

La experiencia del usuario debe tener prioridad.

---

# 69. Premium futuro

Posible modelo:

## Free

```text
100-200 partidas
Repeated players
Basic relationships
Basic graph
Ads
```

## Premium

```text
Mayor historial
Advanced relationship analysis
Advanced graph
Historical trends
More filters
No advertising
```

Precio todavía NO definido.

No implementar pagos en MVP.

---

# 70. Riot monetization compliance

Antes de monetizar:

el producto deberá estar registrado ante Riot.

Riot actualmente requiere que el producto monetizado tenga estado:

```text
Approved
```

o:

```text
Acknowledged
```

Debe existir una modalidad gratuita.

La publicidad puede formar parte del free tier.

Las funciones pagas deben aportar valor transformativo sobre los datos originales.

---

# 71. Disclaimer Riot

Antes de publicar el producto agregar el boilerplate exigido por Riot en una ubicación visible.

No afirmar:

```text
Official Riot application
```

No utilizar branding de forma que parezca un producto oficial.

---

# 72. SEO

Cuando la web sea pública, implementar SEO cuidadosamente.

Páginas potencialmente indexables:

```text
/player/...
```

pero estudiar políticas de Riot y privacidad antes de indexar automáticamente datos de jugadores.

NO diseñar indexación pública indiscriminada hasta validar cumplimiento.

---

# 73. Analytics

Cuando la web sea pública:

evaluar:

```text
Plausible
```

o:

```text
GA4
```

Métricas:

```text
Monthly active users
Daily active users
Pages/session
Matches analyzed
Players analyzed
Analysis completion rate
Average analysis duration
Return users
```

---

# 74. Métricas económicas

Posteriormente medir:

```text
Hosting cost / month
Database cost
Bandwidth
API-related infrastructure cost
Revenue
Revenue per user
Revenue per 1000 page views
```

Indicador principal inicial:

```text
Infrastructure Coverage Ratio

Revenue / Infrastructure Cost
```

Objetivo:

```text
>= 1.0
```

---

# 75. Nombre del producto

Todavía por definir.

Utilizar temporalmente:

```text
LoL Network Analyzer
```

como working title.

NO acoplar namespaces internos demasiado al nombre comercial final.

Namespaces técnicos pueden permanecer como:

```text
LolAnalyzer
```

---

# 76. MVP v0.1

Implementar SOLAMENTE:

1. Docker Compose.
2. PostgreSQL.
3. Redis.
4. .NET API.
5. Next.js frontend.
6. Riot API Client.
7. búsqueda Riot ID.
8. resolución PUUID.
9. descarga de match IDs.
10. descarga de partidas.
11. almacenamiento de raw JSON.
12. almacenamiento normalizado.
13. análisis de jugadores repetidos.
14. player summary.
15. repeated players UI.
16. match history UI.
17. basic tests.
18. README.

No implementar todavía:

```text
payments
advertising
AWS
Neo4j
RSO
complex authentication
machine learning
mobile app
```

---

# 77. MVP v0.2

Agregar:

```text
Relationship Analyzer
Possible Duo detection
Possible Trio detection
Match familiarity
Relationship detail
Network graph
```

---

# 78. MVP v0.3

Agregar:

```text
Incremental synchronization
Scheduled refreshes
Improved caching
Advanced filters
Analytics
Better visualization
```

---

# 79. V1 pública

Antes del lanzamiento público:

```text
Production Riot API Key
Riot product registration
Policy review
Privacy Policy
Terms of Service
Riot disclaimer
HTTPS
Rate limiting
Security review
Public domain
Analytics
Backups
Monitoring
```

---

# 80. Ideas futuras

Posibles features:

### Nemesis
Jugador recurrente contra quien tenemos malos resultados.

### Best Teammate
Jugador recurrente con excelente rendimiento juntos.

### Familiar Lobby
Porcentaje de jugadores conocidos dentro de una partida.

### Matchmaking Network
Mapa completo de relaciones.

### Duo History
Cómo evolucionó una relación durante semanas/meses.

### Season Graph
Comparar redes por temporada.

### Most Seen Players
Ranking de jugadores recurrentes.

### Friends Without Being Friends
Jugadores que aparecen sorprendentemente seguido aunque no estén agregados.

### Group Detector
Detectar clusters recurrentes.

### Champion relationships
Ejemplo:

```text
PlayerX aparece frecuentemente contigo cuando utiliza ChampionY.
```

### Share Card
Crear imágenes compartibles:

```text
My 2026 LoL Network
```

con:

```text
Most encountered player
Best teammate
Nemesis
Unique players
Most common champion
Largest detected group
```

Esta funcionalidad puede servir como mecanismo orgánico de adquisición de usuarios.

---

# 81. Testing

Unit tests prioritarios:

```text
RelationshipScoreCalculator
RepeatedPlayerAnalyzer
MatchFamiliarityCalculator
RiotRoutingResolver
```

Integration tests:

```text
PostgreSQL repositories
API endpoints
Riot client using mocked HTTP
```

No realizar llamadas reales a Riot durante CI.

---

# 82. Code quality

Configurar:

Frontend:

```text
ESLint
Prettier
TypeScript strict
```

Backend:

```text
nullable enable
warnings as appropriate
dotnet format
```

Preferir:

```text
async/await
CancellationToken
dependency injection
immutable DTOs where useful
```

---

# 83. Performance

Evitar:

```text
N+1 database queries
```

Utilizar:

```text
indexes
pagination
batch inserts
AsNoTracking()
```

cuando corresponda.

Indexes iniciales:

```text
players(puuid)

matches(riot_match_id)

match_participants(match_id)
match_participants(player_id)

player_encounters(owner_player_id, total_matches)

player_relationships(player_a_id, player_b_id)
```

---

# 84. Database migrations

Todas las modificaciones deberán generar migrations.

No utilizar:

```text
EnsureCreated()
```

en producción.

---

# 85. Database backups

Raspberry:

crear script:

```text
pg_dump
```

guardar backups rotativos.

Posteriormente poder enviarlos a almacenamiento externo/S3.

No implementar almacenamiento cloud obligatorio en MVP.

---

# 86. Configuración

Utilizar configuración basada en environment.

Backend:

```text
appsettings.json
appsettings.Development.json
environment variables
```

Secrets nunca en appsettings committed.

---

# 87. API versioning

Desde el comienzo:

```text
/api/v1/
```

para permitir cambios posteriores.

---

# 88. OpenAPI

Generar:

```text
OpenAPI / Swagger
```

para backend.

Disponible en Development.

Proteger o desactivar según sea necesario en producción.

---

# 89. Frontend API communication

NO permitir que Next.js haga llamadas directamente a Riot.

Siempre:

```text
Browser
 ↓
Our API
 ↓
Riot API
```

Esto protege la key y centraliza rate limiting/cache.

---

# 90. UX del análisis inicial

Cuando un usuario busca:

```text
Eagly#LAN
```

mostrar:

```text
Analyzing account...

Resolving Riot ID ✓
Fetching matches ✓
Processing 47 / 200
Building player relationships...
```

La operación larga deberá realizarse mediante background job.

El browser consultará status periódicamente.

Posteriormente considerar:

```text
SSE
```

o WebSockets.

Para MVP polling es suficiente.

---

# 91. Error handling

Casos:

```text
Riot ID not found

Invalid region

Riot API unavailable

Rate limit reached

API key invalid

API key expired

Database unavailable

Analysis already running
```

Mostrar errores amigables al frontend.

Nunca enviar stack traces al usuario.

---

# 92. Duplicate jobs

Si ya existe:

```text
analysis running for PUUID X
```

no lanzar otro idéntico.

Retornar el job existente cuando sea razonable.

---

# 93. Data ownership

Los matches son reutilizables entre análisis.

Ejemplo:

Jugador A analiza Match X.

Después jugador B también aparece en Match X.

NO volver a descargar Match X.

Esto crea un efecto acumulativo:

```text
más usuarios
→
más matches en DB
→
mayor cache hit ratio
→
menos Riot API requests por usuario
```

Este comportamiento será fundamental para escalar económicamente.

---

# 94. Diferenciador principal

No intentar competir con OP.GG copiando todas sus funciones.

El diferenciador será:

```text
Relationships between players
```

y:

```text
Repeated matchmaking analysis
```

El producto debe centrarse visualmente en:

```text
Who do you keep encountering?
```

y:

```text
Who appears to play together?
```

---

# 95. Definición de éxito del MVP

El MVP será exitoso si:

1. Puedo introducir un Riot ID.
2. La aplicación identifica el PUUID.
3. Descarga 200 partidas.
4. Guarda los datos correctamente.
5. Puedo cerrar/reiniciar la aplicación sin perder información.
6. Volver a analizar el mismo usuario genera pocas o ninguna llamada innecesaria.
7. Puedo ver los jugadores que más se han repetido.
8. Puedo distinguir aliados y enemigos.
9. Puedo abrir una partida individual.
10. Todo funciona en Raspberry Pi ARM64 mediante Docker Compose.

---

# 96. Primera tarea para Codex

NO intentar construir todo este documento de una sola vez.

Comenzar creando solamente la foundation.

## Sprint 1

Crear monorepo:

```text
apps/web
apps/api
workers/ingestion-worker
infrastructure
docs
```

Crear:

```text
Next.js + TypeScript + Tailwind

.NET 9 Web API

PostgreSQL

Redis

Docker Compose
```

Implementar healthchecks.

Implementar conexión PostgreSQL.

Crear EF Core.

Crear entidades:

```text
Player
Match
MatchParticipant
```

Crear migrations.

Implementar:

```text
IRiotApiClient
```

Crear configuración Riot.

Implementar:

```text
GetAccountByRiotId(gameName, tagLine)
```

NO implementar todavía el relationship analyzer.

---

# 97. Sprint 2

Implementar:

```text
GetMatchIds()
GetMatch()
```

Persistencia:

```text
Players
Matches
MatchParticipants
Raw JSON
```

Implementar deduplicación.

Añadir tests.

---

# 98. Sprint 3

Implementar:

```text
RepeatedPlayerAnalyzer
PlayerEncounters
```

API:

```text
GET player summary
GET repeated players
GET matches
GET match detail
```

Crear frontend.

---

# 99. Sprint 4

Implementar:

```text
PlayerRelationships
RelationshipScoreCalculator
Possible Premade Detector
```

Agregar:

```text
LOW
MEDIUM
HIGH
VERY_HIGH
```

---

# 100. Sprint 5

Implementar:

```text
Social Graph
Group detection
Match familiarity
```

---

# 101. Instrucciones de trabajo para Codex

Antes de escribir código:

1. Leer completamente este documento.
2. Revisar el repositorio existente.
3. Crear un archivo:

```text
docs/ARCHITECTURE.md
```

resumiendo la arquitectura finalmente implementada.

4. Crear:

```text
docs/TODO.md
```

con los sprints.

5. Implementar únicamente Sprint 1 inicialmente.
6. Mantener las decisiones simples.
7. Evitar overengineering.
8. Utilizar código mantenible y production-ready donde no complique innecesariamente el MVP.
9. No introducir servicios cloud obligatorios.
10. Todo debe ejecutarse localmente mediante:

```bash
docker compose up -d
```

11. Todo debe ser compatible con:

```text
linux/arm64
```

12. Nunca colocar Riot API Key en código.
13. No llamar Riot desde frontend.
14. No implementar features que puedan infringir Riot API policy.
15. Si alguna decisión contradice una política actual de Riot Games, detener esa implementación y documentar la incompatibilidad.

---

# 102. Comando de validación esperado

Después de Sprint 1 debería ser posible ejecutar:

```bash
cp .env.example .env

docker compose up -d
```

y obtener:

```text
Web:        healthy
API:        healthy
PostgreSQL: healthy
Redis:      healthy
Worker:     healthy
```

El API debe responder:

```http
GET /health
```

y Swagger/OpenAPI debe funcionar en Development.

---

# 103. Filosofía del proyecto

Prioridades, en este orden:

```text
1. Cumplimiento Riot
2. Seguridad
3. Exactitud de datos
4. Cache/reutilización
5. Experiencia del usuario
6. Coste bajo
7. Escalabilidad
8. Monetización
```

No optimizar prematuramente para millones de usuarios.

Diseñar componentes que puedan separarse posteriormente, pero ejecutar inicialmente todo de la manera más económica posible.

La Raspberry Pi será nuestra primera plataforma.

Docker será nuestra capa de portabilidad.

PostgreSQL será nuestro source of truth.

La Riot API será tratada como un recurso externo limitado y costoso.

El principal activo generado por el sistema será la base histórica de partidas y las relaciones derivadas entre jugadores.

---

# 104. Resultado esperado a largo plazo

Queremos evolucionar desde:

```text
"Ver mi historial de LoL"
```

hacia:

```text
"Entender la red de jugadores que existe alrededor de mis partidas de LoL."
```

La pregunta principal que debe responder el producto es:

**"¿Quiénes son todas estas personas que sigo encontrándome cuando juego League of Legends?"**
