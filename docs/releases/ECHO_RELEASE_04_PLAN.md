# RELEASE-04 — candidata de Dashboard Gaming Competitivo

Fecha: 2026-10-28
Estado: **READY_FOR_REVIEW** — validación de navegador realizada en T04.10.2; pendiente aprobación visual del Product Owner. No autorizado para merge a main ni despliegue. Las secciones de preparación inicial se conservan como registro histórico; el cierre de T04.10.3 al final es el estado vigente.

## 1. Estado inicial y consolidación

- `origin/main`: `2c67fe5cb9d5ffaba6b9f99e3651616a53526409` (T04.5/RELEASE-03).
- PR #97 abierto/Draft hacia main, HEAD previo `d010b981c368a021d552a902e7fa62d09a5e94d1` (T04.10).
- PR #98 abierto hacia T04.10, HEAD `f994e75eff62d73733d36cff751c990bb9b9fd32`; CI run `40644480798` aprobó web, dotnet y containers. Se revisaron sus 25 archivos y no hubo contenido inesperado ni conflictos.
- Solo se fusionó #98, con guardia de HEAD esperado. Merge: **`b551f4312b63af66717de6b6271ec2c190154729`**. PR #97 contiene ahora ambas tareas y conserva Draft. No se fusionó a main ni se cerraron otros PR.
- Antes del merge había un commit exclusivo de T04.10 respecto de main y otro exclusivo de T04.10.1 respecto de T04.10. El merge no requirió resolución de conflictos.
- Worktree de validación `/tmp/improved-train-release04`, rama local `release/echo-release04-plan`, creada desde el merge remoto. Los worktrees previos estaban limpios y se dejaron sin cambios de fuentes. No se detectaron cambios de HEAD durante la revisión; la guardia remota evita fusionar un HEAD distinto.
- Árbol Git de la revisión combinada `513ea4395fcca72d89f7128afeea98bbc23350c4`, idéntico al HEAD aprobado de #98. Se repitieron las pruebas combinadas; la CI previa no se usó como sustituto.

## 2. Funcionalidad incluida

Perfil 60/40 con Riot ID, Tag, región, última partida, refresco y consulta de partida activa independiente. Ícono real **histórico** obtenido del owner en la última partida recuperada, con contexto temporal y fallback neutral; no representa una consulta de avatar actual.

Rendimiento sobre todas las filas válidas cargadas: victorias/derrotas, win rate circular, media KDA por partida `(kills + assists) / max(1, deaths)`, promedios de combate y top 3 campeones con desempates. Filtros solo afectan al historial.

Relaciones sobre evidencia por Match ID y PUUID: únicos, recurrentes, participaciones y conjuntos de aliados/rivales; top 5 con expansión local y partidas compartidas navegables. Repetir/solapar ventanas no infla conteos. Datos sin owner identificable no crean relaciones; equipos desconocidos no se clasifican artificialmente.

Historial con resultado textual, campeón, cola, fecha, duración, K/D/A, ratio y detalles nativos expandibles. Paginación por cinco, deduplicación, reintento por ventana, cancelación y fin de historial conservados. Cobertura completa/parcial/insuficiente/error por ventana y advertencias próximas a métricas; separada del análisis profundo.

## 3. Backend, frontend y contrato

Backend: proyección ligera `PlayerMatchListItem.evidence` y propiedades opcionales de ícono en `PlayerIdentity`. Match-V5 ya entregaba estos datos. Se preservan nombres y semánticas de campos existentes, endpoints, ventanas profundas 5/3, rate limiting, single-flight, TTL, memoria y budgets. No hay Summoner-V4 adicional, jobs, BD o persistencia de análisis.

Frontend: agrega/deduplica la evidencia, navega partidas compartidas y sirve el ícono mediante proxy same-origin de Data Dragon con ID validado, timeout 5s, PNG y límite 1 MiB. No hay dependencias nuevas. Se mantiene versión configurada de assets; ausencia de ícono en ella activa fallback.

Contrato aditivo sin incremento de `/api/v1`: `profileIconId`, `profileIconObservedAt` y `evidence` son opcionales en TypeScript; DTO .NET conserva constructores y default null. JSON desconocido no invalida al consumidor anterior. No hay datos funcionales persistidos que migrar. Cada respuesta sigue limitada a cinco partidas; evidencia con hasta diez participantes ligeros por fila. `docker-compose.yml`, dependencias y lockfile no cambian.

## 4. Matriz de compatibilidad comprobada

Se ejecutó un harness DOM temporal con el componente T04.5 real extraído de `origin/main` y el componente candidato. Respuestas sintéticas equivalentes, con y sin campos aditivos; ninguna llamada Riot. Las cuatro combinaciones aprobaron identidad, cinco enlaces de historial y dos peticiones existentes (consulta y partida activa). En el frontend nuevo se verificó además win rate y presencia/ausencia de listas de partidas compartidas. El harness se retiró al terminar; no forma parte del runtime.

| Frontend | API | Resultado observado |
|---|---|---|
| T04.5 anterior | Anterior | Funciona; baseline de cinco partidas |
| T04.5 anterior | Nueva | Funciona; ignora campos aditivos |
| Candidato | Nueva | Funciona con evidencia, ícono y enlaces compartidos |
| Candidato | Anterior | Funciona con fallback: ícono neutral y agregados antiguos; relaciones con solapamientos pueden quedar no disponibles y no hay enlaces compartidos |

**Orden recomendado: API primero, comprobar salud/compatibilidad con web anterior; después web.** La combinación nueva/nueva es necesaria para la experiencia completa, aunque la nueva web tolere temporalmente la API antigua. En rollback conjunto, volver primero a web anterior y luego a API anterior; un fallo exclusivo de web permite conservar API nueva, por aditividad.

## 5. Pruebas combinadas ejecutadas

SDK Docker .NET `10.0.402`, ARM64, sin credenciales ni conexión Riot; Node `24.18.0`, npm `11.16.0`, Next `16.4.2`.

| Comando | Resultado |
|---|---|
| `dotnet restore LolAnalyzer.slnx` | Aprobado en raíz |
| `dotnet build LolAnalyzer.slnx --configuration Release --no-restore` | Aprobado, 0 warnings/errores |
| `dotnet test LolAnalyzer.slnx --configuration Release --no-build` | 109 unitarias + 21 integración aprobadas; 3 benchmarks/perfiles opt-in omitidos; 133 total |
| `dotnet format LolAnalyzer.slnx --verify-no-changes --no-restore` | Aprobado |
| `npm ci` | Aprobado, 425 paquetes |
| `npm test` | 55/55 aprobadas |
| `npm run lint` | Aprobado |
| `npm run type-check` | Aprobado |
| `npm run build` | Aprobado, todas las rutas existentes y asset de perfil |
| `npm audit --omit=dev` | 0 vulnerabilidades |
| Matriz DOM API/web adicional | 4/4 aprobadas |
| `docker compose config --quiet` y `git diff --check` | Aprobados |

Las pruebas existentes permanecen; incluyen Home, galería protegida, análisis independiente, cancelaciones, timeouts, 429/503, cobertura, deduplicación, reintentos, filtros, promedios y enlaces de evidencia. No se consumió cuota Riot real.

### Incidencia de capacidad

Los primeros intentos de npm ci y build .NET fallaron por `ENOSPC`, no por defectos del código. Se liberaron aproximadamente 2 GB quitando solo node_modules/.next/bin/obj regenerables de worktrees terminados de esta conversación, preservando fuentes, imágenes y volúmenes. Se repitieron todas las validaciones y aprobaron. Después se retiraron las salidas temporales de esta validación para poder preparar imágenes aisladas. La capacidad libre del host debe revisarse de nuevo antes del release; no se hizo prune de imágenes ni cachés compartidas.

## 6. CI

- #98, HEAD `f994e75`: run `40644480798`, success (web/dotnet/containers).
- #97 combinado, HEAD `b551f43`: run `40647200977`, success (web/dotnet/containers).
- El plan añade únicamente documentación a la candidata; el HEAD documental definitivo se verifica en GitHub antes de cerrar esta tarea. El SHA final y su run se reportan en la descripción del PR y en la entrega, sin atribuir la CI previa a un HEAD diferente.

## 7. Seguridad, tamaño y coste

No hay nuevas llamadas autenticadas a Riot ni cambios de credenciales. El campo de ícono proviene de descargas ya realizadas; la imagen es un asset CDN cacheable. No hay nuevos endpoints de análisis, ni incremento de ventanas profundas o límites T03.6. Secretos revisados por patrones del diff sin imprimir valores.

Medición UTF-8 del mismo fixture sintético de cinco partidas y diez participantes, con PUUID de longitud similar a los reales: contrato anterior **4.859 bytes**, aditivo **13.395 bytes**, incremento **8.536 bytes** (aprox. 2,76×). Se obtuvo eliminando únicamente evidence y campos de perfil de la misma respuesta sintética. Es una medición de contrato, no tráfico de jugadores reales ni un máximo de producción. El crecimiento está acotado por cinco filas × diez participantes y límites existentes de payload Riot; no se devuelve raw JSON. Debe observarse tamaño real después de un despliegue autorizado.

Map/Set deduplican Match IDs y PUUID; se ordena de forma determinista la evidencia para reconciliar duplicados. Los cálculos se limitan a la muestra de la vista. El estado de partidas vive en memoria de la vista; el backend solo conserva caché Riot temporal acotada, no análisis persistentes. La caché en memoria se enfría al recrear API; no hay migración y deben respetarse cuotas durante calentamiento.

Audit de producción limpio. `npm ci` conserva un aviso high del árbol de desarrollo y la deprecación de ESLint de la base; no se ejecutó audit fix ni se actualizaron dependencias. No se declara aquí un nuevo audit completo del árbol de desarrollo.

## 8. Revisión visual pendiente

No existe ejecutable Chromium/Chrome/Firefox, caché Playwright, herramienta de navegador conectada ni imagen local de navegador. No se instalaron herramientas globales ni se modificó el servidor para obtener capturas.

**Capturas: ninguna. Tamaños realmente probados en navegador: ninguno.** DOM no verifica layout, contraste ni overflow. Queda revisión visual del PO con fixtures de cinco y diez o más partidas; parcialidad, recurrentes/partidas compartidas, activa/no activa, filtros, detalles expandidos y vacíos. Revisar 320, 390, 768, 1024 y 1440 px: cabecera 60/40, tipografía/espacio, anillo, retratos, foco/teclado, contraste y overflow horizontal. No se declara cumplimiento WCAG completo.

PR #97 permanece Draft hasta completar esa revisión y resolver cualquier hallazgo.

## 9. Entorno desplegado inspeccionado sin modificaciones

Host `aarch64`, Docker `29.7.1`. API/web healthy; puerto público web `8081:3000`; API solo puerto interno 8080. Red de proyecto existente `lol-network-analyzer_default`; API/web sin mounts.

| Servicio | Contenedor | Imagen local actual / rollback |
|---|---|---|
| API | `e4bb58bed744f64af82002e71560820f104c6bcb4bf8477fa4c22479d7358f70` | `sha256:3676f4ef2a49c311eed5646d90d891de5ae3b17d09424e9f5d59be17b8a50ac0`, `project-echo-release02-api:arm64` |
| web | `632ab74505282b8c9078b6e77754b2c2a0cfa8dbdb431ec384fbf49dbfec114d` | `sha256:f141e5dec8f4bbf4e6a5f0158ccd47f50a3ab5779edb1a47ebf1266a34e19ce5`, `project-echo-release03-web:2c67fe5` |
| Redis heredado | `c606fd91491e6faa880ac83731f847ab2ee3522509971dc2b87fd9a939b69135` | `sha256:59c08762bdbcf53fa132aa0bae464a57a1309dc3fff8a034bb3e16d7a0b30ec5` |

API arrancó `2026-10-28T05:29:08Z`; web `2026-10-28T05:20:25Z`. Redis ya estaba detenido (exited), con estado health antiguo unhealthy; no es dependencia de la API stateless. Se conserva su volumen `lol-network-analyzer_redis_data` y no se lo inició/eliminó.

Versiones identificadas por los tags y registros de despliegue: API RELEASE-02/T03.6 y web RELEASE-03/T04.5. Son imágenes locales sin RepoDigest de registry ni label de SHA en las imágenes previas; la identidad confirmada es el image ID listado, no una nueva certificación del SHA de runtime.

`GET /` y `/api/health`: 200. Validación sintética `GET /api/v1/players/synthetic-puuid/recent-matches?start=-1`: 400 esperado, antes de cualquier consulta Riot, confirmando proxy. Configuración no sensible: web API_BASE_URL `http://api:8080`, región la1, Data Dragon 16.17.1 y NODE_ENV production; sin key en web. API: región la1/routing americas, concurrencia de admisión 8, cola 16, espera 5000ms, concurrencia Riot 3, budget 180s. Key presente solo en API, valor no mostrado. No hay nuevas variables requeridas.

## 10. Imágenes candidatas aisladas

Se construyeron desde `b551f43` con Dockerfiles existentes, plataforma linux/arm64 y tags exclusivos. No se sobrescribieron `lol-network-analyzer-api:latest` ni `lol-network-analyzer-web:latest`.

| Artefacto | Tag | Image ID / digest de configuración local |
|---|---|---|
| API | `project-echo-release04-api:b551f43` | `sha256:b75422295d830186d86ecd27211fca239eba0c2da60fa0e80ce65ffcd9f76f6d` |
| web | `project-echo-release04-web:b551f43` | `sha256:830ea8410abd5d18012d21bdb01ea0910b917510002bb10e0a3d39e3be43085e` |

Ambas llevan label OCI revision `b551f4312b63af66717de6b6271ec2c190154729`; no tienen RepoDigest de registry porque no se publicaron a uno. Tamaños locales: API 131.591.117 bytes, web 215.764.536 bytes. Creación: 2026-10-28 02:17:25 -05 y 02:20:30 -05 respectivamente. El commit de publicación posterior solo añade documentación; se verificó diff vacío en apps/api, apps/web y Dockerfiles respecto a la revisión usada para construir.

Smoke en red Docker nueva **internal**, sin puertos de host y sin credenciales reales (solo valor sintético de prueba en API). API y web candidatas healthy. Home, `/api/health` y ruta de jugador: 200; galería `/design-system`: 404 en producción por defecto; iconId inválido: 404; validación de recent-matches a través del proxy: 400 esperado; readiness API satisfactoria. No aparecieron errores fatales/críticos en los logs de esas pruebas. El artefacto API no incluye EF Core ni StackExchange.Redis.

Se retiraron exclusivamente los dos contenedores de validación y su red después de verificar sus labels. Las imágenes candidatas permanecen disponibles. IDs, imágenes y estado de API/web desplegados y Redis coinciden con el baseline; volumen Redis y tags de rollback intactos. No se hizo un análisis contra Riot real ni una revisión visual de la UI.

**Riesgo observado del host:** Docker avisó que el kernel/cgroup no permite aplicar `--memory` y descartó ese límite durante la prueba. No se declara validado un aislamiento duro de memoria de contenedor; T03.6 mantiene sus protecciones de aplicación. Revisar esa capacidad y espacio libre antes del despliegue público, sin cambios automáticos del host. Tras preparación quedaban aproximadamente 2,4 GB libres (host 29 GB).

## 11. Plan de despliegue futuro — requiere autorización

1. Completar revisión visual y comprobar CI del HEAD final de #97. Presentar evidencias y solicitar autorización explícita de merge a main y actualización de API/web. Este plan no concede esa autorización.
2. Tras merge autorizado, registrar SHA nuevo de main y comparar aplicaciones, Dockerfiles y Compose contra `b551f43`. Si el árbol de aplicación cambia, reconstruir/revalidar; si solo cambia documentación, registrar la equivalencia antes de reutilizar artefactos.
3. Confirmar espacio libre suficiente, estado de API/web, imagen/ID de rollback, red, puertos y archivo runtime env. Conservar tags anteriores y secretos de runtime; no imprimirlos ni copiarlos a la imagen.
4. Desde checkout del SHA aprobado y usando el env runtime explícito, configurar imágenes candidatas verificadas. Los tags de servicio deben apuntar de forma explícita a esos IDs; no ejecutar un up que pueda usar una imagen accidentalmente antigua.
5. Actualizar **solo api** mediante Compose con `--no-deps --no-build --wait`, comprobar readiness y proxy desde web anterior sin consultas Riot reales. Abortar si falla salud o compatibilidad.
6. Actualizar **solo web** de igual modo, manteniendo 8081. Verificar health, Home, ruta de dashboard, galería protegida, proxy con validación sintética y logs sin secretos. Confirmar IDs de imagen contra los aprobados.
7. No usar compose down/remove-orphans, ni tocar Redis/volúmenes o recursos ajenos. Registrar digests, contenedores y smoke; solo declarar DEPLOYED tras comprobarlos.

## 12. Rollback futuro

- Conservar `project-echo-release02-api:arm64` y `project-echo-release03-web:2c67fe5`; sus IDs anteriores están arriba. No se eliminaron ni sobrescribieron estos tags.
- Fallo exclusivo de web: restaurar el tag de servicio `lol-network-analyzer-web:latest` al ID anterior y recrear solo web con `--no-deps --no-build --wait`. API nueva puede permanecer si está healthy y compatible.
- Rollback conjunto autorizado: web anterior primero, health, luego API anterior, readiness y proxy. Restaurar solo los tags de servicio y contenedores afectados, con el mismo archivo de runtime. No revertir secretos ni Redis por un fallo del dashboard.
- Abortar el avance ante health fallido, errores sostenidos, proxy roto, imagen incorrecta, incompatibilidad de contrato o falta de espacio. Mantener imágenes candidatas para diagnóstico; no podar imágenes ni volúmenes.

## 13. Estado final

**READY_FOR_REVIEW** tras la validación aislada documentada abajo, pendiente aprobación del PO y comprobación de capacidad antes de operar. La integración de desarrollo está autorizada y realizada; PR #97 sigue disponible para revisión en Draft. **No se fusionó PR #97 a main ni se desplegó RELEASE-04.**


## 14. T04.10.2 — validación real de navegador y cierre para revisión

### Revisiones y CI

Se verificó `origin/main` y el HEAD publicado `749e0771921c5a24598abf209ab9017481cbef71`. #98 está fusionado mediante `b551f4312b63af66717de6b6271ec2c190154729`; su commit `f994e75` es ancestro de la candidata. CI del HEAD documental inicial: [run 40650209230](https://github.com/D0kker/improved-train/actions/runs/40650209230), success. La publicación de estas evidencias añade únicamente documentación, imágenes sintéticas y un runner opt-in de pruebas; no modifica código de aplicación, contratos, infraestructura ni dependencias. El SHA definitivo y su run de CI se registran en PR #97 y en el reporte de entrega para evitar una referencia autorreferencial dentro de este commit.

Se reutilizó el worktree aislado de RELEASE-04, limpio y sin cambios remotos durante la revisión. Se revisó la descripción obsoleta de #97 y se actualizó para T04.10 + T04.10.1. Sigue abierto/Draft hacia main. Ningún otro PR se fusionó o cerró.

### Navegador y aislamiento

Playwright 1.64.1, Chromium 156.0.8078.4 ARM64 y axe-core 4.14.0, instalados exclusivamente bajo `/tmp`. El host Debian 11 ARM64 no está soportado directamente; el binario fallback Ubuntu 22.04 arrancó con bibliotecas descargadas y extraídas localmente. No se instaló software global, no se modificaron configuraciones de seguridad del host ni contenedores existentes.

Servidor exclusivo `echo-release04-visual-web`, imagen candidata ARM64 `project-echo-release04-web:b551f43`, publicado solo en `127.0.0.1:3187`. Sin credenciales, con destino API inaccesible. Playwright suministró datos y assets sintéticos mediante interceptación; las conexiones externas se bloquearon. El servidor de prueba se retira al finalizar, conservando las imágenes candidatas y todas las imágenes de rollback. Producción API/web y Redis no se modifican.

### Resultados por sección

| Sección | Evidencia observada |
|---|---|
| Perfil | Identidad, región y fecha legibles; 60/40 real en 1024/1440, apilado en anchos menores; ícono histórico simulado con rótulo y fallback neutral; botón actualizar disponible |
| Rendimiento | Anillo accesible, promedios/KDA y top 3; al ampliar 5→10 el win rate cambia 60→50%; filtrar derrotas conserva el resumen global |
| Relaciones | Nueve jugadores únicos/recurrentes, 45 participaciones en cinco partidas; mismos jugadores alternan aliado/rival; expansión 5→9 sin API adicional, evidencia y enlaces disponibles |
| Historial | Filtros Todas/Derrotas/Todas, detalles nativos, carga de cinco, fallo conservando cinco filas y reintento hasta diez |
| Cobertura | Complete/partial/insufficient/error, detalles expandibles y advertencias próximas a las métricas; parcial usa tres filas disponibles, no cinco inventadas |
| Partida activa | Presente, ausente y error con acción propia; historial y rendimiento siguen visibles |
| Accesibilidad/responsive | Sin overflow en 320/390/768/1024/1440, también con expansiones/texto largo; skip link por teclado, apertura de summary con Enter y foco visible |

[Índice de 16 capturas](release04-captures/README.md), [recorrido](release04-captures/results.json), [axe/teclado/texto largo](release04-captures/accessibility.json). Se inspeccionaron capturas reales de cabecera, rendimiento, móvil, parcial y partida activa. Los retratos DEMO no son assets reales de Riot ni demuestran su disponibilidad. No hubo errores JavaScript ni peticiones externas en el recorrido.

Axe no encontró infracciones en los cinco anchos (28 reglas aprobadas por ejecución), pero dejó `color-contrast` como comprobación incompleta sobre parte del contenido. Se conserva ese resultado, sin convertirlo en aprobación completa de contraste o WCAG. Las superficies navy, cifras claras y acentos dorados resultan legibles en las capturas revisadas; la evaluación asistiva y los casos de contraste inconclusos quedan para revisión manual. Los controles expandibles tienen objetivo mínimo de 44 px por los estilos existentes; no se realizó una auditoría exhaustiva de todos los objetivos del producto.

### Defectos y validaciones repetidas

No se demostró un defecto de aplicación que requiriese corrección funcional. El primer runner buscó el nombre anterior del botón después de fallar una ventana; se corrigió el selector a «Reintentar este bloque de cinco» y el recorrido completo pasó. No se debilitó ninguna prueba existente.

- .NET: restore, build Release (cero advertencias/errores), test y format verify aprobados en SDK 10.0.402 ARM64 aislado: 109 unitarias + 21 integración = **130 aprobadas**, 3 benchmarks/perfiles opt-in omitidos.
- Web: `npm ci`, **55/55 pruebas**, lint, type-check y build Next 16.4.2 aprobados en Node 24.18.0/npm 11.16.0. Todas las rutas existentes compilan.
- `npm audit --omit=dev`: **0 vulnerabilidades**; no se alteró lockfile ni versiones. El aviso de desarrollo previo no se considera remediado por este sprint.
- Runner principal: cinco anchos y nueve comprobaciones agrupadas de escenarios; runner complementario: cinco anchos con expansión, teclado, texto largo y axe. Evidencias JSON conservadas.
- `docker compose config --quiet` y `git diff --check` aprobados. No hubo cambios de API, web funcional, Docker ni secretos en T04.10.2.

### Compatibilidad, rollback y recomendación

Sigue vigente la matriz de la sección 4 y el plan API primero/web después; web nueva sobre API antigua mantiene fallback. Los IDs y tags candidatos/anteriores de las secciones operativas siguen siendo los artefactos de referencia, sin nuevos builds de imagen por esta publicación documental. Las limitaciones de memoria cgroup y espacio del host continúan siendo precondiciones operativas, no defectos resueltos aquí.

Recomendación: **READY_FOR_REVIEW** del Product Owner con las capturas, manteniendo #97 Draft hasta aprobación visual. Revisión de contraste inconcluso/asistiva y assets reales claramente pendiente; no se afirma una certificación de accesibilidad. Cualquier merge a main y actualización de servicios exige autorización posterior explícita. **No se fusionó PR #97 a main ni se desplegó RELEASE-04.**


### Observación final del runtime (solo lectura)

El 2026-10-29 04:13 UTC se observaron recreaciones de los contenedores existentes **fuera de los comandos de esta validación**. Los IDs del inventario inicial son históricos; las imágenes no cambiaron. API: `0aeea556e0575a14396f60d50f3a8ed9ba0767676e8069c8f718bdee0d1e4276`, imagen `3676f4ef…`; web: `0dfbb8bcee76a36b7364bfc05b2cb1f94b1afa6d307b3f141c7c7bf766db8d23`, imagen `f141e5de…`. Se verificaron ambos healthy, `/` 200 y `/api/health` 200. No se investigaron ni modificaron secretos para atribuir esas recreaciones. Redis conserva el mismo ID/imagen y estado detenido. Esta tarea creó y eliminó exclusivamente `echo-release04-visual-web`; no recreó servicios de producción. Antes de cualquier release autorizado se debe volver a inventariar el runtime.

Al terminar las pruebas se observaron aproximadamente 954 MiB libres (97% ocupado). Se retiraron exclusivamente las herramientas temporales de navegador, node_modules/.next y bin/obj generados por esta validación en su worktree, después de finalizar todos los comandos. Se recuperó espacio hasta aproximadamente 2.4 GiB libres (92% ocupado). No se eliminaron imágenes, cachés Docker compartidas ni volúmenes; comprobar nuevamente capacidad antes de desplegar.

Commit de publicación de las evidencias: `d1f2a20f0442170d97d01e3443bdf02e8ddc0f2f`. El seguimiento posterior solo registra esta limpieza local; el SHA de entrega y su CI están en PR #97.


## 15. T04.10.3 — optimización visual para revisión del PO

Se refinan únicamente presentación y estilos del dashboard, sobre HEAD inicial `21b1051560d153a3aedbd81f7c99bef368dbc6dc`. Informe: [T04.10.3](../T04_10_3_DASHBOARD_VISUAL_REFINEMENT.md). Cabecera 60/40 con alturas independientes, ícono histórico 88/64 px, región sin código duplicado, fecha española compacta, siete métricas con cifras protagonistas y grillas completas, campeones horizontales de 72/64 px. La muestra, fórmulas, cobertura, deduplicación, offsets/reintentos y contratos no cambian; no hay nuevas llamadas Riot ni dependencias.

Validación de producción web: npm ci, 59/59 pruebas, lint, TypeScript, build y audit producción cero; Compose config y diff-check. Chromium con fixtures en 320/390/768/1024/1440, incluidos estados y expansiones. La revisión detectó el KDA dividido en tablet: se ajustó la grilla tablet a 3+2+2 y se agregó una aserción geométrica para que las cifras quepan en una línea. Desktop conserva 3+4. [Capturas y comparación](release04-captures/t04-10-3/README.md): 19 imágenes nuevas, sin sustituir las anteriores. Cabecera completa a 1440px: 416→223px. Axe sin infracciones detectadas, con contraste parcialmente inconcluso; evaluación manual/asistiva y aprobación visual del PO pendientes. El SHA final y su CI se registran en PR #97.

[Propuesta publicitaria](../T04_10_3_AD_PLACEMENT_PROPOSAL.md) exclusivamente documental. No se implementa monetización, slot ni reserva visual artificial.

**Artefactos:** la imagen web `project-echo-release04-web:b551f43` no contiene T04.10.3 y queda como candidata histórica/comparativa; no reutilizarla para publicar este refinamiento. Será necesario construir la web desde el SHA finalmente aprobado antes de cualquier despliegue autorizado. La API no cambió: su candidata anterior y la matriz aditiva/orden API→web se conservan, sujetos a comparar árboles al preparar la ejecución. Los tags e imágenes anteriores de rollback no se eliminan ni modifican.

Estado **READY_FOR_REVIEW**, #97 continúa Draft. El servidor de prueba fue exclusivo de localhost y no utilizó Riot real. Esta tarea no modifica servicios de producción, Redis, secretos o main; no fusiona ni despliega. Las secciones previas conservan evidencias históricas de RELEASE-04 y no sustituyen esta revisión web.
