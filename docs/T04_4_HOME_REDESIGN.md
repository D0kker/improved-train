# T04.4 — Rediseño de Home y búsqueda

## Problemas detectados

- La Home anterior centraba una sola tarjeta; no explicaba en pocos pasos cómo pasar de una búsqueda a los encuentros históricos.
- El titular y la descripción comunicaban la consulta, pero no daban contexto suficiente a una persona nueva sobre el producto.
- La región ya se mostraba por la corrección T04.2, aunque quedaba separada del formulario y no se aclaraba que es la región configurada.
- La página no tenía el encabezado reutilizable del producto ni una nota sencilla que explicara los límites del historial disponible.
- El formulario era funcional y accesible; se conservó su validación y navegación, y se alineó su acción principal con el botón del Design System.

## Variante elegida y estructura

Se eligió **Search First**. El titular, el valor del producto y el formulario aparecen antes que la explicación complementaria. La alternativa split hero separaría el formulario y el mensaje en dos columnas; en móvil añadiría decisiones de apilado sin mejorar la comprensión, por lo que se mantuvo una única columna centrada y de ancho legible.

Orden de Home:

1. `SiteHeader` con marca y enlace ancla directo al formulario.
2. Hero compacto: propósito y descripción.
3. Tarjeta de búsqueda con Riot ID, Tag, región configurada y acción primaria.
4. Alcance de cinco partidas y cautela sobre historial incompleto.
5. Tres tarjetas breves que describen búsqueda, historial y encuentros.

No se muestran estadísticas, testimonios, actividad en vivo ni detecciones presentadas como hechos.

## Textos y comportamiento

- Titular: **Cada partida tiene una historia.**
- Descripción: **Busca jugadores, explora sus partidas recientes y descubre cuándo se han encontrado antes.**
- Formulario: nombre de jugador, Tag, ayuda de formato, región configurada y botón **Buscar jugador**.
- Alcance: la consulta inicial revisa cinco partidas; la falta de datos no confirma que nunca haya existido un encuentro.
- Los textos de la página viven en `home-copy.ts` y llegan por props. El formulario acepta copy por props para facilitar extracción a traducciones futuras.
- La región y el valor `platformRegion` de navegación mantienen la misma configuración de T04.2. No se añadió un selector sin soporte funcional.
- No se añadieron llamadas HTTP, componentes de servidor pasan el formulario como composición y solo el formulario sigue siendo cliente.

## Componentes y archivos

- Reutilizados: `SiteHeader`, `Container`, `Card` y `Button` del Design System de T04.3. La acción del encabezado salta al formulario por su ID.
- Creado `app/home-content.tsx` como composición presentacional independiente del router; `app/page.tsx` conserva la lectura server-side de `RIOT_PLATFORM_REGION`.
- `src/riot-id-search-form.tsx` conserva validación, foco del primer campo inválido, ARIA, codificación de segmentos y navegación. Solo pasa a copy configurable y al botón Cyber Arcade.
- `app/globals.css` añade estilos Home con tokens semánticos y mantiene las reglas compartidas de formulario.
- `tests/components.test.tsx` cubre presentación, región, errores, foco, teclado, navegación y ausencia de peticiones nuevas.

## Responsive y accesibilidad

- El contenido usa `Container` wide y el hero/formulario tienen máximos legibles; el formulario se apila bajo 900 px y las tarjetas explicativas se apilan en el mismo breakpoint.
- Bajo 620 px se reduce el padding, el botón ocupa el ancho disponible y los dos campos conservan columnas flexibles. Las reglas evitan anchos mínimos rígidos para el nombre.
- Las etiquetas siguen asociadas a los campos. Los errores conservan `aria-invalid`, `aria-describedby` y foco; el estado de navegación usa el botón loading del Design System y un anuncio `role=status`.
- Se conserva el skip link y el `main` con `id=main-content`; el encabezado aporta un landmark de navegación nombrado.
- Los valores de texto son props/copy configurable; una prueba DOM comprueba descripción y región extendidas sin recortarlas.
- No se dispone de Chromium/browser en este entorno. No se hicieron capturas, medición de contraste renderizado ni validación completa WCAG/lector de pantalla. Responsive se revisó en CSS y DOM, no con emulación visual en todos los anchos solicitados.

## Pruebas y validaciones

- Toolchain: Node `v24.18.0`, npm `11.16.0`; `npm ci` instaló desde lockfile 425 paquetes.
- `npm test`: **35/35 aprobadas** con JSDOM/Testing Library; no consulta Riot ni API local.
- `npm run build`: **aprobado** con Next.js `16.4.2`; generó la Home y las rutas existentes.
- `npm run lint` y `npm run type-check`: aprobados; el resultado final se confirmó antes de publicar.
- `npm audit --omit=dev`: **0 vulnerabilidades** en dependencias de producción. El audit completo identifica un high en `zod@4.5.4`, dependencia transitiva de desarrollo vía `eslint-config-next@16.4.2` → `eslint-plugin-react-hooks@7.1.1`. El advisory afecta validación de colecciones grandes; no está en el árbol de producción y la Home no importa Zod. No se alteró el lockfile en este rediseño; el árbol de lint requiere actualización coordinada a una versión corregida (>=4.6.6) y nueva verificación.
- `docker compose config --quiet` y `git diff --check`: aprobados.
- No hay Chromium/browser instalado. Por eso no se tomaron capturas ni se declara validación visual por viewport o conformidad WCAG completa.
- Los tests DOM comprueban que Home presenta un único `main`, jerarquía de encabezados, región visible, descripción ampliada sin truncar, validación/foco, teclado, anuncio de carga, ruta codificada y cero solicitudes `fetch`.
- Publicación: `feature/t04-4-home-redesign`, PR [#90](https://github.com/D0kker/improved-train/pull/90), base `feature/t04-3-design-system`; abierto y sin merge.

## Limitaciones y pendientes para T04.5

- No se añadió selector de región; se presenta la región efectiva que ya usa la búsqueda.
- La evaluación visual en navegador y el contraste calculado de los textos pequeños quedan pendientes de entorno con browser.
- El dashboard puede reutilizar tokens y componentes del sistema, pero su nueva jerarquía y presentación pertenecen a T04.5.
- No se modificaron contratos, backend, staging, red de Riot ni servicios en ejecución.
