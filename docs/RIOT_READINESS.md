# Readiness Riot y gobierno de datos

Estado: auditoría iniciada, no apto todavía para publicación pública.
Fuentes verificadas: 2026-08-31

Esto es una lista técnica de preparación, no asesoría legal ni evidencia de aprobación de Riot.

## Matriz vigente

| Requisito oficial | Estado del producto | Acción pendiente |
| --- | --- | --- |
| Todo producto que sirve a jugadores debe registrarse y mantener descripción/features actualizadas | no registrado como producto público | registrar el prototipo y someter cambios relevantes a auditoría |
| Una Development key es temporal y no permite un producto público; Personal tampoco permite consumo público ni alpha/beta abierta | uso privado de desarrollo | obtener Production antes de abrir a terceros |
| Producción suele requerir una aplicación casi completa y un sitio funcional, no solo GitHub | prototipo privado funcional | preparar dominio/sitio revisable después del hardening |
| HTTPS y key fuera del código/distribución | key solo backend/worker; despliegue aún privado | implementar HTTPS y gestión de secretos antes de publicar |
| No desanonimizar jugadores no identificables ni crear MMR/ELO alternativo | análisis post-partida e inferencias prudentes | mantener estos invariantes en cada feature |
| Mostrar boilerplate visible y no aparentar endorsement | aún no incorporado a UI pública | completar S7-003 con texto vigente |
| Monetización solo registrada en estado Approved/Acknowledged; free tier permitido con ads y cobro solo por valor transformativo | monetización futura, no implementada | consultar a Riot si el modelo concreto genera dudas |
| Riot puede transmitir identificadores de solicitudes de datos por sus canales | no existe flujo operativo | definir recepción, trazabilidad, borrado/exportación y evidencia |
| El historial de custom matches tiene restricciones específicas de visibilidad pública | todavía no se clasifica para exposición | S7-011 bloquea por defecto esa exposición hasta definir el opt-in aplicable |

Fuentes primarias: [General Policies](https://developer.riotgames.com/policies/general), [League of Legends policy](https://developer.riotgames.com/docs/lol), [Developer Portal y tipos de key](https://developer.riotgames.com/docs/portal), [API Terms](https://developer.riotgames.com/terms) y [Production key FAQ](https://developer.riotgames.com/docs/faqs).

## Inventario inicial de datos

| Dato | Propósito | Fuente/ubicación | Regla inicial |
| --- | --- | --- | --- |
| PUUID | identidad interna estable | Riot; PostgreSQL | no usar como etiqueta humana ni incluir en logs ordinarios |
| Riot ID mutable | búsqueda y navegación | Riot; PostgreSQL | actualizar sin crear una identidad nueva |
| partidas terminadas y raw JSONB | deduplicación y reproceso | MATCH-V5; PostgreSQL | acceso solo server-side; nunca servir raw JSON al navegador |
| participantes/encounters/relationships | análisis histórico explicable | derivados locales | inferencias, no hechos sociales; reconstruibles |
| jobs y schedules | operación durable y opt-in | PostgreSQL | errores seguros; schedule desactivable sin borrar historial |
| métricas/logs | operación y diagnóstico | memoria/stdout | agregados de baja cardinalidad; sin key, payload, Riot ID o PUUID |

Antes de S7-002 completo el PO debe aprobar periodos concretos de retención, alcance de exportación/eliminación, contacto público y visibilidad/indexación. No se inventa un plazo universal.

## Go/no-go público

Estado actual: **NO-GO**. Faltan registro/acceso Production, políticas legales visibles, decisiones de privacidad, HTTPS, hardening, backup/restore e incident response. Azure es solo una opción de staging a evaluar en S7-010; no cambia este resultado.
