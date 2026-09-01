# Threat model del MVP privado

Última revisión: 2026-08-31

## Alcance y activos

Este modelo cubre browser, Next.js, API, worker, Riot, PostgreSQL, Redis, volúmenes y CI. Los activos prioritarios son la `RIOT_API_KEY`, datos históricos, disponibilidad de la ingesta y decisiones de privacidad. PUUID es identidad técnica, no un dato para logs o analítica.

## Fronteras y controles actuales

| Frontera | Riesgo principal | Control implementado | Pendiente antes de público |
| --- | --- | --- | --- |
| Browser → web | XSS, framing, navegación manipulada | CSP same-origin, `frame-ancestors 'none'`, `nosniff`, referrer/permissions policy y segmentos codificados | Revisar CSP en modo reporte y eliminar `unsafe-inline` cuando Next lo permita sin romper hidratación |
| Web → API | Abuso, payloads o concurrencia sin límite | Rewrites same-origin, API no publicada, cuerpo máximo 64 KiB y backpressure concurrente con 429 | Límite por cliente en el proxy público y trusted proxies explícitos |
| API/worker → Riot | Fuga de key, 429, tormenta de reintentos | Secreto solo runtime, cliente centralizado, concurrencia acotada, `Retry-After`, backoff y cancelación | Rotación operativa y acceso Production apropiado |
| API/worker → PostgreSQL | Inyección, duplicados, escrituras concurrentes | EF parametrizado, constraints, transacciones, advisory locks y red Docker privada | Credencial fuerte, mínimo privilegio y restauración periódica probada |
| API/worker → Redis | Caída o datos derivados obsoletos | Redis no es fuente de verdad, TTL, invalidación y fallback en memoria | Autenticación/TLS si sale del host y límites de memoria |
| Web → Data Dragon | CDN caído, contenido inesperado, SSRF | Ruta servidor con host fijo, versión validada, ID numérico, PNG estricto, caché y fallback textual | Procedimiento de actualización y observación de fallos |
| Imágenes/volúmenes | Secretos o datos embebidos | Builds multi-stage, contexto acotado, `.env` ignorado; API, worker y web ejecutan no-root | Escaneo reproducible de imágenes y permisos de volúmenes |
| CI/supply chain | Dependencia vulnerable o workflow comprometido | Permisos `contents: read`, lockfile, restore/build/test sin Riot | Escaneo de dependencias/imágenes y política de corrección acordada |

## Decisiones explícitas

- CORS no se habilita: el browser usa únicamente la web same-origin. Una API pública requerirá allowlist concreta, nunca `*` con credenciales.
- Forwarded headers no se confían todavía porque no existe un proxy público definido. Al desplegar, solo se aceptarán proxies/redes conocidas.
- OpenAPI solo se mapea en Development; métricas, PostgreSQL y Redis permanecen dentro de la red Compose.
- No se añade RSO/autenticación por defecto. Debe existir una función pública concreta y requisitos Riot confirmados.
- Los iconos son decorativos respecto a la lógica: un fallo de Data Dragon no impide leer campeón, resultado o KDA.

## Respuesta ante compromiso de la Riot key

1. Detener temporalmente ingesta y schedules sin borrar PostgreSQL.
2. Revocar/regenerar la key en el portal Riot y actualizar únicamente el secreto runtime.
3. Revisar logs redactados, imágenes y configuración para confirmar que el valor no fue persistido.
4. Reiniciar API/worker, verificar health y ejecutar una llamada mínima controlada; no colocar la nueva key en tickets o evidencia.
5. Documentar causa, alcance y acción preventiva sin copiar el secreto.

## Riesgo residual y bloqueo público

El entorno sigue siendo privado y el estado público es `NO-GO`. Faltan escaneo de supply chain, límite confiable por cliente en el proxy, HTTPS, credenciales de producción, backups restaurables, políticas legales y auditoría de readiness.
