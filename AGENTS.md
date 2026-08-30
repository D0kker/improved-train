# Instrucciones persistentes del proyecto

## Al comenzar una sesión

- Lee `docs/PROJECT_CONTEXT.md` antes de proponer o realizar cambios.
- Lee `docs/DECISIONS.md` si la tarea afecta arquitectura, datos, despliegue, seguridad o cumplimiento.
- Lee las secciones relevantes de `lol_network_analyzer_spec.md`; antes de implementar un sprint nuevo, revisa el documento completo y `docs/TODO.md`.
- Comprueba el estado real del repositorio. La documentación orienta, pero el código, las migraciones y los contenedores son la evidencia final.
- Responde en español salvo que el usuario solicite otro idioma.

## Límites de producto y seguridad

- Prioriza, en este orden: cumplimiento Riot, seguridad, exactitud, reutilización de datos, experiencia, coste y escalabilidad.
- Usa PUUID como identidad interna. Los nombres y tags son atributos mutables.
- La Riot API key solo puede existir como secreto de runtime en API o worker. Nunca uses variables `NEXT_PUBLIC_*` para secretos ni llames a Riot desde el navegador.
- No guardes credenciales, tokens, `.env`, dumps, datos personales innecesarios ni respuestas sensibles en Git, logs, prompts, memoria o fixtures.
- No intentes desanonimizar jugadores ocultos ni presentar inferencias como hechos. Usa términos como `possible premade` o niveles de confianza, no `verified duo`.
- Las políticas, endpoints y límites de Riot cambian: verifica las fuentes oficiales actuales antes de implementar una decisión que dependa de ellos. Si existe una incompatibilidad, detén esa parte y documéntala.

## Forma de trabajo

- Implementa únicamente el sprint o alcance solicitado; no conviertas el documento completo en una sola entrega.
- Mantén una Clean Architecture ligera. No agregues servicios cloud, Neo4j, autenticación compleja, pagos o publicidad durante el MVP salvo petición explícita.
- Conserva los cambios existentes y evita acciones destructivas sin autorización explícita.
- Solo delega cuando existan subtareas realmente independientes y el usuario lo autorice. El agente principal revisa la integración y ejecuta la validación final.
- Antes de agregar dependencias de producción, explica brevemente por qué son necesarias.
- Mantén compatibilidad `linux/arm64` y `linux/amd64` para imágenes y herramientas del proyecto.

## Continuidad

- Actualiza `docs/PROJECT_CONTEXT.md` cuando cambien el estado, comandos, arquitectura, bloqueos o siguiente paso.
- Registra decisiones duraderas en `docs/DECISIONS.md` con fecha, estado, razón y consecuencias.
- Mantén `docs/TODO.md` alineado con el alcance real; una tarea pasa a completada solo con evidencia.
- No presentes ideas tentativas como decisiones aceptadas.

## Validación

- Documentación/configuración: revisa enlaces y ejecuta `git diff --check`.
- Frontend: ejecuta lint, pruebas, type-check y build definidos por `apps/web/package.json`.
- Backend/worker: ejecuta restore, build, pruebas y formato definidos por la solución .NET.
- Contenedores: ejecuta `docker compose config`, construye para la arquitectura disponible y verifica health checks reales.
- CI nunca debe llamar a Riot; usa un servidor HTTP simulado para el cliente.
