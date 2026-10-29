# RELEASE-04 — revisión visual con datos simulados

Estas capturas proceden de Chromium real sobre la imagen candidata ARM64 `project-echo-release04-web:b551f43`. El código web es idéntico al HEAD documental `749e0771921c5a24598abf209ab9017481cbef71`. Todos los jugadores, partidas, respuestas y gráficos de avatar/campeones son **sintéticos**; los assets llevan DEMO. No representan perfiles reales ni verifican disponibilidad del CDN de Riot.

| Captura | Contenido |
|---|---|
| [Desktop](01-desktop.png) | Cinco partidas, pantalla completa a 1440 px |
| [Cabecera](02-header.png) | Proporción 60/40, identidad y contexto histórico del ícono |
| [Rendimiento](03-performance.png) | Anillo, promedios y campeones |
| [Relaciones](04-relationships-expanded.png) | Nueve recurrentes y partidas compartidas expandidas |
| [Historial](05-history.png) | Tarjetas y detalles |
| [Cobertura parcial](06-partial.png) | Tres detalles disponibles, advertencias y ventana expandida |
| [Móvil](07-mobile.png) | Pantalla completa a 390 px |
| [Partida activa](08-active-game.png) | Estado y acción en cabecera |
| [Diez partidas](09-ten-matches.png) | Muestra ampliada y rendimiento actualizado |
| [Vacío](10-empty.png) | Información insuficiente y fallback del ícono |
| [Error inicial](11-error.png) | Error recuperable |
| [Error de partida activa](12-active-error.png) | Aviso independiente del historial |
| [Error de ventana](13-window-error.png) | Historial conservado y reintento específico |
| [Cobertura error](14-coverage-error.png) | Sin confundir fallo con cero definitivo |
| [Carga](15-loading.png) | Estado de actualización |
| [Móvil expandido y texto largo](16-mobile-expanded-long-text.png) | Expansiones y cadenas ampliadas |

[Resultados del recorrido](results.json) y [resultados de accesibilidad](accessibility.json) contienen comprobaciones ejecutadas, anchos, foco y limitaciones de axe. No son una certificación WCAG.

## Reproducir sin Riot

No hay dependencias nuevas en el proyecto. Se instalaron Playwright 1.64.1 y axe-core 4.14.0 únicamente en un directorio temporal externo. Chromium 156.0.8078.4 ARM64 y bibliotecas gráficas se descargaron/extrajeron bajo `/tmp`, sin instalación global ni cambios al sistema. Debian 11 ARM64 no está soportado directamente por Playwright; se utilizó su binario de Ubuntu 22.04 ARM64 tras comprobar arranque y dependencias. En un equipo compatible se puede utilizar el Chromium correspondiente de Playwright.

1. Arrancar un servidor de prueba de la web de producción, exclusivo y ligado a localhost, por ejemplo en 3187. El servidor no debe tener credenciales Riot; usar una API de prueba inaccesible, nunca la de producción.
2. Instalar las herramientas en un directorio temporal propio y preparar Chromium allí. El script admite `PLAYWRIGHT_PACKAGE` (ruta absoluta al package.json de ese directorio), `CHROMIUM_PATH` y `VISUAL_BASE_URL`.
3. Ejecutar desde la raíz `node scripts/visual/release04.mjs` con esas variables y, si corresponde, `LD_LIBRARY_PATH` apuntando a bibliotecas extraídas localmente.
4. Ejecutar de nuevo con `VISUAL_SUPPLEMENT_ONLY=1` para teclado, texto largo y axe.

El script intercepta las APIs y assets con fixtures y bloquea peticiones a otros orígenes. No sirve como prueba end-to-end de Riot. `CAPTURE_DIR` permite cambiar el destino sin sobrescribir estas referencias. Sus aserciones fallan ante overflow, errores JavaScript, solicitudes externas, cambios de métricas por filtros o navegación insegura en los casos incluidos. Los hallazgos de axe quedan registrados para revisión; no se convierten en una declaración automática de conformidad.

Pendiente: aprobación visual del Product Owner, evaluación asistiva y contraste manual donde axe devuelve `incomplete`; revisión con assets autorizados reales cuando se permita acceso al proveedor.
