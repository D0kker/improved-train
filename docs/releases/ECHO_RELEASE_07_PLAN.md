# RELEASE-07 — Plan de integración y despliegue

## Estado

`READY_FOR_PO_APPROVAL`: candidata integrada y validada en la rama `integration/t04-7-2-i18n-relationships`, SHA `d79f54b2c3f8d724327ee58d3278e9fa21f666d4`. Esto no representa aprobación visual del Product Owner ni autorización de merge o despliegue.

## Alcance

Reúne T04.7 (interfaz en español, inglés y portugués brasileño) y T04.6.4 (grupos históricos respaldados por la API y conteos de encuentros con el jugador consultado en las filas de participantes). La vista conserva los equipos azul/coral, retratos, nombres, K/D/A y resaltado dorado del owner. Estados parciales, identidades ambiguas y evidencia no disponible mantienen mensajes prudentes.

PR de origen: #106 (`d5406cf754b2426ead377df178117e96173ddaa4`) y #107 (`a309b8f76000e8f514e65dfa21dcdc9a1754f0a2`), ambos abiertos como Draft al verificar. El PR integrado [#108](https://github.com/D0kker/improved-train/pull/108) está abierto como Draft hacia `main`; su CI espera el HEAD publicado. PR #105 es un registro documental de RELEASE-06 aún abierto; no forma parte del alcance funcional de RELEASE-07.

## Integración y decisiones

Desde `main` `6b452208a3aa0ed9b8ea7a94fe51b7ca32cd50bb`, se integró primero #106 y después #107 mediante dos merge commits en la rama candidata. Conflictos en `match-detail-view.tsx`, `PROJECT_CONTEXT.md` y `TODO.md` se resolvieron conservando el cambio de idioma y la lógica de relaciones; CSS base y pruebas se fusionaron automáticamente y se inspeccionaron. Se añadieron mensajes ICU para etiquetas inline y pluralización cero/singular/plural en los tres catálogos. Un ajuste CSS posterior aumenta el contraste del borde del selector. No hay cambios de API/backend, contratos, infraestructura ni llamadas Riot.

Los grupos provienen únicamente de `premadeGroups` y admiten solapamientos. Los conteos owner-participante se conservan como agregados de API, no se reconstruyen ni suman como Match IDs. La partida objetivo y deduplicación histórica siguen bajo responsabilidad de la API existente. Ningún texto confirma premades.

## Arquitectura y compatibilidad

`next-intl`, catálogos tipados e ICU. Las rutas se mantienen sin prefijo; el locale se resuelve por cookie explícita `echo-locale`, idioma de navegador soportado y fallback `es`. El selector persiste la cookie y preserva URL/navegación. La API de RELEASE-06 continúa siendo compatible porque los contratos no cambian.

**Dependencias:** T04.7 añade `next-intl@4.14.9` como dependencia runtime y actualiza `apps/web/package-lock.json`. T04.6.4 y la rama de integración no agregan dependencias. `npm audit --omit=dev` reportó cero vulnerabilidades. No hay paquetes backend nuevos.

| Web        | API        | Resultado                                        |
| ---------- | ---------- | ------------------------------------------------ |
| RELEASE-06 | RELEASE-06 | Se mantiene                                      |
| RELEASE-07 | RELEASE-06 | Compatible; no requiere coordinación de contrato |

Release futura: reconstruir y actualizar solo `web` en ARM64, conservar la API RELEASE-06, configuración/secretos/redes/volúmenes y verificar health/proxy y rutas. Antes de ejecutar, inspeccionar Compose y salud reales; la imagen de rollback de referencia es `project-echo-release06-web:6b45220`, pero su Image ID debe verificarse en el entorno antes de un despliegue.

## Validación ejecutada

- `npm ci --offline`: aprobada; la instalación conectada se interrumpió por `ECONNRESET` al revalidar metadatos, sin cambios al lockfile.
- `npm test`: 94/94 aprobadas.
- `npm run lint`, `npm run type-check`, build Next de producción y Prettier en los archivos modificados: aprobados.
- `npm audit --omit=dev`: cero vulnerabilidades de producción.
- `docker compose config --quiet`: aprobado.
- Chromium/Playwright ARM64 con fixtures sintéticos: ES/EN/PT-BR a 390/768/1440 para Home, dashboard, análisis y partida activa; análisis adicional a 320 y 1024. Escenarios de cobertura parcial, sin relaciones, nombres largos, error/reintento, grupos solapados, cambio de locale/persistencia, navegación y fallback de idioma. Cero errores de página/hidratación, cero solicitudes externas inesperadas y cero overflow en la matriz. Capturas e índice en [evidencia T04.7.2](t04-7-2-captures/README.md).
- Revisión focalizada de contraste, con mezcla sRGB y composición de alfa sobre fondos calculados: texto normal muestreado mínimo 5.84:1; borde del selector corregido a 6.51:1 con el interior y al menos 6.83:1 con el fondo exterior calculado. El anillo de foco genérico al 45% queda cerca de 3:1. No es auditoría integral ni certificación WCAG; tecnologías asistivas y contraste de todos los estados siguen fuera de alcance de esta muestra.
- No se ejecutaron pruebas .NET: no hubo cambios de backend/contrato.

CI GitHub del PR #108: la primera ejecución tuvo un fallo transitorio en un test de cancelación .NET; los jobs repetidos pasaron. La ejecución completa [41925282013](https://github.com/D0kker/improved-train/actions/runs/41925282013), sobre el HEAD `6b8cb28`, pasó dotnet restore/build/test/format, web lint/tests/type-check/build y containers config/build. Confirmar que los checks continúen verdes antes de cualquier futura integración.

## Riesgos y seguimiento

- El contrato no expone Match IDs históricos subyacentes a los conteos de relaciones; la UI no puede auditar individualmente esos agregados.
- Axe había marcado contraste como incompleto; se calculó una muestra focalizada, no una auditoría WCAG integral.
- #106 y #107 permanecen como PR independientes y abiertos. Si se elige este PR integrado, revisar/cerrar los PR de origen como sustituidos solo después de verificar el contenido publicado; no fusionarlos además, pues duplicarían commits.
- `main`/producción no se cambiaron. RELEASE-06 continúa como baseline de compatibilidad a confirmar en el preflight del despliegue.

## Secuencia recomendada

1. Revisar y aprobar el PR integrado #108 hacia `main`, que contiene ambos historiales de trabajo.
2. No fusionar además #106 ni #107; cuando el PR integrado se acepte, marcar los originales como reemplazados según decisión del propietario.
3. Publicar RELEASE-07 solo tras aprobación visual/funcional y autorización separada. Revalidar HEAD, CI, Compose, ARM64, imagen rollback y salud en producción.

No se fusionó a `main`, no se desplegó y no se modificaron servicios activos.
