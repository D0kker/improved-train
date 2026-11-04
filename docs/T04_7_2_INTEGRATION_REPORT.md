# T04.7.2 — Integración de internacionalización y relaciones

## Estado

`READY_FOR_PO_APPROVAL`: candidata en `integration/t04-7-2-i18n-relationships`. SHA del código funcional validado: `d79f54b2c3f8d724327ee58d3278e9fa21f666d4`; el PR #108 incluye después commits solo documentales. No es aprobación del Product Owner ni autorización para integrar a `main` o desplegar.

PR #106 (`d5406cf754b2426ead377df178117e96173ddaa4`) y #107 (`a309b8f76000e8f514e65dfa21dcdc9a1754f0a2`) permanecen abiertos como Draft; #105, registro documental de RELEASE-06, también permanece abierto. PR integrado [#108](https://github.com/D0kker/improved-train/pull/108) está abierto como Draft hacia `main`, limpio y esperando CI del HEAD publicado. La rama candidata conserva ambos historiales mediante merges y suma el ajuste de contraste del selector.

## Cambios integrados

- `next-intl` para ES, EN y PT-BR, con cookie de preferencia, negociación del idioma del navegador, fallback a español, ICU plural y rutas sin prefijo.
- Grupos derivados únicamente de `premadeGroups`, etiquetas estables y solapamientos preservados.
- Conteos del owner localizados; sin agregar agregados duplicados ni inventar Match IDs.
- Selector de idioma con borde elevado a `--echo-text-subtle` para corregir el contraste visible.
- Casos DOM de grupo y pluralización incorporados a la vista de análisis.

Los conflictos se limitaron a `match-detail-view.tsx`, `PROJECT_CONTEXT.md` y `TODO.md`; se combinaron ambas implementaciones/documentaciones. `globals.css` y el test principal se fusionaron automáticamente y luego se revisaron. No hay cambios backend, de contrato, endpoints o llamadas a Riot.

## Pruebas y calidad

- `npm ci --offline`: aprobado. El primer `npm ci` con red se atascó al revalidar metadatos y mostró `ECONNRESET`; se canceló y ejecutó una instalación limpia offline. Lockfile sin cambios.
- `npm test`: 94/94 aprobadas.
- `npm run lint`, `npm run type-check`, `npm run build`: aprobados en el HEAD candidato.
- Prettier dirigido a los archivos de código modificados: aprobado.
- `npm audit --omit=dev`: cero vulnerabilidades de producción.
- `docker compose config --quiet` y `git diff --check`: aprobados antes de las actualizaciones documentales finales; volver a ejecutar al cerrar el diff.
- CI del PR #108: el primer job .NET tuvo un fallo transitorio en `MalformedQuotaHeadersAreIgnoredWithoutUnboundedParsing` (esperó cancelación en 30 ms y no recibió excepción). Se repitieron los jobs fallidos; `dotnet test`, `dotnet format`, `web` (lint/tests/types/build) y `containers` completaron con éxito en run [41923687044](https://github.com/D0kker/improved-train/actions/runs/41923687044). No se cambió backend.
- Sin pruebas .NET: no se modificó API ni contrato.

## Validación visual

Chromium/Playwright ARM64 usó fixtures sintéticos e interceptó API/assets; las solicitudes externas no permitidas se abortaron. En 390, 768 y 1440 px se visitaron Home, dashboard, análisis y partida activa para ES/EN/PT-BR. El análisis se comprobó además a 320 y 1024 px. Se verificaron escenarios parciales, sin relaciones, nombres largos, error/reintento y grupos solapados; selector ES→EN→PT-BR→ES, cookie, navegación, recarga, locale no soportado, foco y `html[lang]`.

Resultado del script: `errors=[]`, `unexpectedRequests=[]`, sin overflow en las vistas/anchos probados. Capturas generadas sobre el código `d79f54b2c3f8d724327ee58d3278e9fa21f666d4`; ver [índice de capturas](releases/t04-7-2-captures/README.md) y [matriz JSON](releases/t04-7-2-captures/results.json).

La revisión focalizada de contraste calcula los extremos de fondos y opacidades: texto normal de la muestra mínimo 5.84:1; borde selector 6.51:1 contra interior y ≥6.83:1 contra fondo exterior compuesto. El foco genérico al 45% queda cerca de 3:1. Axe había dejado el análisis de contraste incompleto en las tareas de origen; esto no certifica WCAG 2.2 AA integral ni sustituye evaluación con tecnología asistiva. Cálculos y método: [contraste](releases/t04-7-2-captures/contrast.md).

## Compatibilidad y publicación

API RELEASE-06 sin cambios; contratos y llamadas Riot intactos. La release futura debe reconstruir solo web sobre API RELEASE-06, previa inspección de la configuración y entorno ARM64. La imagen de rollback de referencia es `project-echo-release06-web:6b45220`; verificar su Image ID en el host antes del despliegue.

Recomendación: integrar un único PR (#108) hacia `main`, pues ya incluye ambos historiales. No fusionar #106/#107 aparte; gestionar su sustitución/cierre solo después de decidir e integrar el PR candidato. Main, producción y Redis no se modificaron.
