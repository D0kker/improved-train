# T04.7 — Internacionalización

## Estado

Implementación en `feature/t04-7-i18n`, basada en `origin/main` SHA `6b452208a3aa0ed9b8ea7a94fe51b7ca32cd50bb`. Estado `REVIEW_REQUIRED`: validaciones automatizadas aprobadas; revisión visual real y capturas pendientes por falta de navegador disponible. No se ha fusionado ni desplegado.

## Arquitectura

Se eligió `next-intl` para App Router y React Server/Client Components. Aporta ICU para interpolación/plurales, formato basado en `Intl`, provider compatible con SSR y tipos de mensajes. Una solución propia habría requerido mantener esas piezas y la integración SSR dentro del proyecto. Los catálogos locales siguen versionados; no hay traducción remota ni servicio externo.

Se mantiene el formato actual de URLs, sin prefijos de locale, para conservar enlaces y rutas compartibles existentes de Home, dashboard, partida y partida activa. El locale se resuelve por petición en servidor en este orden: cookie explícita `echo-locale`, cabecera `Accept-Language` negociada entre los locales soportados y español (`es`). El selector guarda una cookie `SameSite=Lax`, `Path=/`, de un año y `Secure` en HTTPS; recarga la ruta actual para que SSR responda con el locale elegido. No se guarda preferencia en backend, cuenta o base de datos. La contrapartida de no usar prefijos es que no hay URLs independientes por idioma para indexación SEO.

La integración usa `getRequestConfig` de `src/i18n/request.ts`, `NextIntlClientProvider` en el layout raíz y el plugin de Next en `next.config.ts`. `locales.ts` es la fuente de locales y negociación. `types.d.ts` declara `Locale` y `Messages`; `messages.ts` organiza claves en namespaces `common`, `home`, `dashboard`, `ranked`, `live`, `match`, `network`, `coverage` y `errors`. Los catálogos EN y PT-BR deben coincidir estructuralmente con ES (`satisfies` más prueba recursiva de paridad).

## Pantallas cubiertas

- Home y búsqueda: copy, etiquetas, región, validación, botón y estado de navegación.
- Dashboard del jugador: métricas, filtros, historial, paginación, cobertura, estado activo, región, fechas, errores y clasificación Solo/Dúo/Flex.
- Análisis de partida: equipos, resumen, participantes, relaciones, grupos prudentes, estados de cobertura, errores y navegación.
- Partida activa: equipos/participantes, carga, ausencia, errores y reintento.
- Elementos compartidos: navegación, skip link, nombres accesibles de perfil/campeón e ícono.

Los Riot IDs, Tags, PUUID, Match IDs, nombres propios recibidos de Riot y nombres oficiales de campeones no se traducen. Los códigos/modos de cola conocidos se localizan en la capa de presentación; valores desconocidos conservan el identificador recibido sin reinterpretarlo.

Los errores HTTP se traducen a mensajes seguros por estado; no se presenta el cuerpo del backend. La cobertura y familiaridad conservan semántica prudente en los tres idiomas: coincidencia o recurrencia no confirma premades. El código de negocio, endpoints, contratos, consultas a Riot, paginación, caché y cancelación no cambian.

## Formatos

Se usa `Intl.NumberFormat` para porcentajes y números, y `Intl.DateTimeFormat` para fechas. Fechas/hora se presentan en UTC explícito para que el primer render SSR y el cliente no discrepen por la zona horaria del host; el timestamp almacenado no se modifica. Duraciones y etiquetas de colas tienen presentación local sin alterar fórmulas de KDA o win rate. ICU maneja plurales, por ejemplo partidas cargadas.

## Añadir idioma o clave

1. Añadir el locale a `src/i18n/locales.ts` y definir su formato regional.
2. Completar el catálogo entero en `src/i18n/messages.ts`; TypeScript y la prueba de paridad deben pasar.
3. Usar `useTranslations("namespace")` en Client Components y `getTranslations` en Server Components; no introducir copy nuevo dentro de primitivas reutilizables.
4. Añadir pruebas DOM/formatos para el texto y revisar textos largos en el idioma nuevo.
5. Añadir la opción visible en `SiteHeader` con el nombre del idioma, no solo una bandera.

## Pruebas y validación

Las pruebas i18n cubren precedencia de preferencia explícita, negociación de navegador, fallback español, atributos de cookie, selector accesible y persistencia, plurales EN/PT-BR, fechas/duraciones localizadas, paridad de claves e hidratación SSR de un componente localizado. Las suites preexistentes envuelven las vistas con el provider de traducciones.

Validación ejecutada después de `npm ci --offline` (lockfile aplicado; registry inaccesible durante la instalación): 88/88 pruebas frontend, lint, TypeScript, build de producción, `docker compose config --quiet`, `git diff --check` y Prettier para todos los archivos modificados pasaron. `npm audit --omit=dev`, ejecutado con acceso al registry oficial tras el bloqueo DNS inicial, reportó cero vulnerabilidades de producción. El chequeo global `npm run format:check` sigue detectando seis archivos del baseline que no están modificados en esta rama (`app/match/[matchId]/page.tsx`, `app/player/[gameName]/[tagLine]/page.tsx`, `src/coverage.ts`, `src/riot-id-search-form.tsx`, `tests/frontend.test.ts`, `tests/resource-protection.test.ts`); todos los archivos cambiados pasan la revisión dirigida. No se ejecutó .NET porque no se cambió backend.

Capturas visuales reales no disponibles en esta ejecución: no se encontró Playwright, Chromium ni caché local de navegador, y el entorno no ofrece herramienta de navegador. No se instalaron navegadores o herramientas globales. Por ello no se declara validación de 390/768/1440 px ni revisión visual completa. Esa inspección debe hacerse con fixtures sintéticos antes de aprobar el diseño visual.

## Limitaciones

- Se conservan URLs sin prefijo; SEO por locale con hreflang/canonical requeriría una decisión futura de rutas.
- La zona horaria se fija en UTC para hidratación determinista; una futura preferencia horaria local necesita mecanismo explícito que no cambie el primer render.
- La ruta experimental `/design-system` solo existe tras habilitación de configuración y no es una pantalla pública por defecto.
- No se afirma certificación WCAG completa. El selector es nativo y accesible por teclado, pero aún falta una revisión visual/assistiva de los tres idiomas y tamaños.
- La auditoría npm queda sujeta a ejecución después de `npm ci`; no se debe inferir ausencia de vulnerabilidades si el registry no es accesible.
