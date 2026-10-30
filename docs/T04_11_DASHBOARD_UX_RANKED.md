# T04.11 — Refinamiento UX y rango competitivo

## Alcance y feedback del PO

Desde `origin/main` `22d115233738a1722c1ad6942426d0b6c4fcf1c0`, en worktree aislado y rama `feature/t04-11-dashboard-ux-ranked`. Se compactan perfil, recurrentes e historial. Se conserva íntegramente el cálculo deportivo, la agregación por Match ID/PUUID, la paginación, los reintentos y el análisis profundo cinco/tres. Sin merge, despliegue, cambios de secretos, Redis o servicios existentes.

## Perfil y composición

Se retira «PERFIL DEL JUGADOR». El ícono de 88 px conserva su origen histórico mediante descripción accesible y ayuda nativa con fecha, sin etiqueta permanente ni afirmación de avatar actual. Riot ID, región legible, fecha y actualización permanecen. El rango ocupa el espacio derecho del perfil; en tablet pasa a una fila propia y en móvil se apila sin forzar columnas estrechas. La cabecera exterior conserva 60/40 en escritorio.

El estado sin partida activa dice «No se detectó una partida en curso», con contexto temporal para lectores de pantalla. La carga de partida activa sigue independiente.

## Auditoría de datos y fuente oficial

Account-V1 aporta PUUID e identidad, no rango. Match-V5 ya aporta ícono histórico y participantes, pero no clasificación actual. No se deduce rango de partidas, LP ficticios ni emblemas decorativos.

Se verificó el catálogo oficial vigente de [League-V4](https://developer.riotgames.com/apis#league-v4/GET_getLeagueEntriesByPUUID), incluyendo su [descripción pública JSON](https://developer.riotgames.com/api-details/league-v4): `GET /lol/league/v4/entries/by-puuid/{encryptedPUUID}` sobre host de **plataforma**, no routing continental. Devuelve ambas colas en la misma respuesta. El backend conserva exclusivamente queueType, tier, rank como division y leaguePoints. No devuelve el payload completo ni PUUID adicionales.

Se admite la intersección del catálogo oficial y las plataformas ya soportadas por PROJECT ECHO: BR1, EUN1, EUW1, JP1, KR, LA1, LA2, NA1, OC1, RU, SG2, TR1, TW2 y VN2. ME1 no se añade al routing general; PH2/TH2 no están en el catálogo vigente consultado. Una plataforma no admitida degrada el rango sin romper la búsqueda.

## Contrato aditivo e independencia

Nuevo endpoint secundario:

```text
GET /api/v1/players/{puuid}/ranked?platformRegion=la1
200 { entries: [{ queueType, tier, division, leaguePoints }], observedAt }
```

`entries: []` solo proviene de una respuesta Riot correcta sin clasificación. 404, 429, 5xx, timeout y JSON inválido conservan semántica de error; la UI muestra «Rango no disponible» y reintento exclusivo. Los errores upstream se convierten en ProblemDetails genéricos, sin cuerpo sensible. No se alteran contratos anteriores de búsqueda, partidas o Spectator.

Frontend nuevo con API anterior: el endpoint ausente produce «Rango no disponible» sin bloquear el resto. Frontend anterior con API nueva: conserva sus contratos. Nueva combinación: rango independiente tras resolver identidad. Para un futuro despliegue, API primero permite obtener rango desde el primer uso; no autoriza desplegar esta tarea.

## Consumo, caché y protección

Una solicitud frontend secundaria por identidad/actualización correcta, sin polling ni consultas por cada participante. Un miss de caché realiza normalmente **una** llamada League-V4 para ambas colas. Cambiar de cola, filtrar, expandir relaciones o cargar más historial no consulta rango de nuevo. Un reintento manual puede consultar de nuevo si no existe respuesta válida en caché.

Se reutilizan admisión global, cuota por conexión, limiter Riot singleton, límites de payload, caché acotada, single-flight, timeouts y cancelación de T03.6. Los reintentos existentes ante 429 permanecen: pueden producir hasta tres intentos según configuración/presupuesto, no se promete una llamada absoluta ante fallos. Se observan cuotas app/método devueltas en headers; no se inventa una cuota universal ni se eleva la configuración existente.

`Cache:RankedTtlSeconds`: 300 s predeterminados, rango válido 1–900 s. Clave por plataforma normalizada + PUUID; ambas colas comparten snapshot con `observedAt`. No se persisten historiales. `Riot:RankedRequestTimeoutSeconds`: 5 s, validado entre 1 y 5, además de admisión global acotada. Frontend cancela a los 12 s para cubrir admisión y transferencia; desmontaje o cambio de identidad aborta y descarta respuestas antiguas. El rango secundario puede compartir capacidad Riot con otras consultas, pero nunca se espera dentro del flujo de historial.

## Presentación de rango y emblemas

Solo/Dúo se selecciona primero si existe; si solo hay Flex se muestra Flex inicialmente. El selector nativo permite inspeccionar ambas colas y muestra «Sin clasificación» para una entrada ausente en una respuesta válida. LP negativos se conservan si Riot los proporciona. Apex muestra tier sin una división engañosa. Validación defensiva rechaza tiers/colas desconocidos y respuestas malformadas.

Los diez emblemas PNG oficiales se alojan en el mismo origen, sin proveedor externo ni dependencia nueva. Fuente, atribución y hash del archivo original en [README de recursos](../apps/web/public/assets/ranks/README.md). Se extrajeron únicamente los PNG de tier, sin alterarlos. Un fallo de imagen muestra fallback neutral y conserva rango textual. Se deben revisar futuras actualizaciones y políticas antes de reutilizarlos en otros contextos.

## Rendimiento, relaciones e historial

El encabezado principal de rendimiento conserva únicamente su título y advertencias críticas si la muestra es parcial. El anillo, siete métricas y tres tarjetas horizontales de campeones permanecen. Alcance, filtros y limitaciones están en Cobertura y transparencia.

Los recurrentes conservan Top 5 y expansión local. Identidad y región acompañan tres miniestadísticas: compartidas, aliados y rivales. «Ver partidas (N)» usa `details/summary` nativos; cada partida se identifica por posición, modo, fecha disponible, relación y acción accesible. El Match ID permanece en rutas y claves, sin ser la única etiqueta. Las identidades incompletas no generan perfiles ficticios. Se mantiene «La recurrencia no confirma que los jugadores sean premades».

El historial elimina el panel redundante «Ver detalles de…». KDA aparece en la fila principal con el criterio existente `(kills + assists) / max(1, deaths)` por partida. No cambia el promedio de ratios aprobado. Datos no finitos se representan con raya, resultados no clasificables conservan su etiqueta y fechas ausentes muestran su fallback. Filtros y cursor no cambian.

## Archivos y pruebas

- Backend: `RankedEndpointMapping`, `RiotRankedProfile`, `IRiotApiClient`, `RiotApiClient`, `RiotOptions`, `CacheOptions`, `RiotCacheKeys`, registro en `Program` y pruebas dirigidas de cliente/endpoint.
- Frontend: `player-ranked.tsx`, `profile-icon.tsx`, `competitive-sections.tsx`, `player-dashboard.tsx`, CSS acotado y emblemas locales.
- Evidencia: `tests/ranked.test.tsx`, regresión DOM de dashboard y `scripts/visual/t04-11.mjs` con fixtures.

No se utilizan credenciales ni cuotas Riot en las pruebas. Resultados de cierre abajo.

## Limitaciones y siguientes pasos

El rango es un snapshot con TTL, no una suscripción en tiempo real. Si Riot añade tiers, colas o plataformas, se necesita revisar el catálogo y las validaciones. La información competitiva no certifica grupos premade. La validación automática de accesibilidad no equivale a conformidad WCAG completa; faltan evaluación asistiva y aprobación visual del PO. Esta tarea no autoriza monetización ni cambios de otras pantallas.

## Cierre de validaciones

Toolchain real: Linux ARM64, Node24.18.0, npm11.16.0, Next16.4.2, React19.2.8 y TypeScript5.9.3; .NET SDK10.0.402 mediante contenedor de validación aislado. Sin dependencias nuevas de producción o desarrollo.

| Comando | Resultado |
|---|---|
| `npm ci` | instalación limpia aprobada con lockfile conservado |
| `npm test` | 68 aprobadas,0 fallidas |
| `npm run lint` | aprobado |
| `npm run type-check` | aprobado |
| `npm run build` | aprobado; Home, dashboard, detalle, live y galería compilan |
| `npm audit --omit=dev` | cero vulnerabilidades |
| `dotnet restore LolAnalyzer.slnx` | aprobado |
| `dotnet build LolAnalyzer.slnx --configuration Release --no-restore` | aprobado,0 warnings/errores |
| `dotnet test LolAnalyzer.slnx --configuration Release --no-build` | 166 aprobadas (131unitarias+35integración),3omitidas opt-in,0fallos |
| `dotnet format LolAnalyzer.slnx --verify-no-changes --no-restore` | aprobado, sin cambios de formato requeridos |
| `docker compose config --quiet` | aprobado, sin cambiar servicios |
| `git diff --check` | aprobado |

El primer intento de build dentro del sandbox falló al iniciar subprocesos de TypeScript (`--showConfig`). Se repitió fuera del sandbox, sin modificar dependencias ni desactivar verificaciones, y el build terminó correctamente. Un mock de integración inicialmente no sustituía las opciones capturadas al iniciar Program; se corrigió su DI y se acotaron las esperas, sin cambios de producción por ese defecto de fixture. Los mocks DOM pendientes ahora respetan abort para no dejar timers abiertos.

Se desactivan los loggers automáticos del typed HttpClient de Riot mediante `RemoveAllLoggers`, conservando logging seguro y métricas de la aplicación. Una prueba de integración con loggingTrace verifica que no aparezcan PUUID, hostRiot ni credencial sintética y que permanezca la telemetría segura.

### Validación visual

Chromium/Playwright con fixtures en 320/390/768/1024/1440px: sin overflow horizontal; composición60/40, rango/sin rango/error, Solo y Flex, partida activa, cinco→diez partidas, Top5/expansión, enlaces compartidos, filtros, KDA directo, cobertura parcial/insuficiente/error, reintento de ventana y estados vacíos. Se comprobó selección de cola con teclado sin requests adicionales.

La revisión de capturas detectó espacio desperdiciado junto a una relación expandida; se corrigió haciendo que la evidencia use la fila y se organice en dos columnas cuando hay ancho. Métricas, anillo y campeones aprobados no se rediseñaron. [Capturas e índice](releases/t04-11-captures/README.md), [medidas y controles](releases/t04-11-captures/results.json) y [axe/foco/texto largo](releases/t04-11-captures/accessibility.json).

Axe no detectó infracciones en los cinco tamaños con contenido expandido/textos largos; `color-contrast` quedó incompleto y requiere evaluación manual. Navegación Home y galería protegida en producción comprobadas. No equivale a aprobación visual del PO ni certificación WCAG completa. Imágenes de perfil/campeones en capturas son DEMO; emblemas de rango sí usan los PNG oficiales locales. No se comprobó rango contra Riot real ni rendimiento bajo cuota real.

Estado de entrega: candidata para revisión del PO; sin merge, despliegue o reinicio de servicios existentes.

### Riesgo residual de dependencias

La auditoría completa, ejecutada adicionalmente, reporta un aviso high de **desarrollo** en Zod transitivo (`GHSA-mrhx-c7xc-m3w5`, agotamiento de memoria con colecciones sobredimensionadas, corregido desde4.6.6). No está en el árbol de producción auditado, que conserva cero vulnerabilidades. El lockfile no cambió en T04.11. Revisar actualización compatible y repetir toolchain en tarea independiente; no se ejecutó `audit fix --force`.
