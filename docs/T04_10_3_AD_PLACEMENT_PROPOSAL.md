# T04.10.3 — Propuesta de ubicaciones publicitarias

Estado: propuesta de UX para una tarea futura, no aprobación ni integración publicitaria. No se insertan slots, scripts, SDK, proveedores ni dependencias. El espacio desperdiciado se corrige mediante composición antes de considerar publicidad.

## Ubicaciones evaluadas

| Ubicación | Recomendación | Razón y límites |
|---|---|---|
| Después de campeones y antes de relaciones | Primera candidata a probar | Separación natural entre rendimiento y coincidencias; nunca dentro de las métricas. Mantener el encabezado de relaciones unido a su contenido, separado del anuncio. |
| Después de relaciones y antes del historial | Alternativa a la anterior | Válida solo con distancia clara respecto a «Ver más», enlaces compartidos y filtros; no activar ambos emplazamientos inicialmente. |
| Columna lateral de escritorio | Descartada para esta distribución | El contenedor actual de 72rem necesita su ancho para siete métricas y tres campeones. Una columna adicional estrecharía el contenido o crearía nuevos vacíos. Reconsiderar solo con evidencia en pantallas mayores. |
| Entre tarjetas del historial | Descartada en primera integración | Interrumpe una lista de cinco, se parece a contenido navegable y compite con «Analizar encuentros». Requiere evaluación propia posterior. |
| Cabecera, anillo, tarjetas estadísticas, panel de partida activa | Descartadas | Son datos y acciones esenciales; publicidad allí crearía confusión o clics accidentales. |

## Desktop y móvil

Probar **un solo bloque** claramente rotulado «Publicidad», independiente de las tarjetas del producto. Desktop puede reservar un bloque horizontal centrado al ancho útil disponible, sin obligar a ampliar el contenedor. En móvil se utilizaría un formato compatible con el ancho interior real (menor que 320px en una pantalla de 320px); no asumir que un creativo fijo de 320px cabe en ese espacio. Si el proveedor no ofrece un formato compatible, omitir el slot en ese breakpoint.

Evitar elementos adhesivos, overlays o anuncios que desplacen filtros y controles. El inicio de búsqueda, la identidad del jugador y el acceso al análisis deben conservar prioridad. No presentar anuncios personalizados por PUUID, Riot ID o relaciones; no transmitir esas identidades a un proveedor.

## Reserva futura sin saltos de layout

Definir el formato permitido por breakpoint antes de integrar el proveedor. Reservar dimensiones o `min-block-size` estables desde el primer render y limitar el creativo a ellas; no insertar espacio retroactivamente al llegar la respuesta. La reserva solo se habilitaría en la variante futura con publicidad, no hoy. El estado de consentimiento/no disponibilidad debe decidirse antes de mostrar esa variante y no colapsar un bloque visible durante una interacción. Comparar las variantes con y sin publicidad y medir CLS en carga, navegación y carga adicional; no prometer CLS cero por usar un placeholder.

Como respaldo técnico, [web.dev sobre CLS](https://web.dev/articles/optimize-cls) identifica anuncios y elementos sin dimensiones como causas de desplazamientos y recomienda reservar espacio. El diseño exacto depende del inventario y no implica elegir proveedor.

## Condiciones que revisar antes de activar

Fuentes oficiales consultadas para esta propuesta el 2026-10-29 UTC:

- [Políticas generales de Riot: monetización](https://developer.riotgames.com/policies/general#monetization): exigen producto registrado y estado Approved o Acknowledged; la capa gratuita puede incluir publicidad. Revisar además integridad, assets, identidad propia y disclaimer. Esta propuesta no acredita aprobación de PROJECT ECHO ni activa monetización.
- [Políticas de emplazamiento de Google AdSense](https://support.google.com/adsense/answer/1346295?hl=es), solo como referencia de un proveedor posible: diferenciar anuncios del contenido y evitar posiciones próximas a navegación o controles que provoquen clics accidentales. AdSense no está seleccionado.
- Revisar las condiciones vigentes del proveedor elegido, privacidad, consentimiento, cookies y jurisdicciones antes de añadir scripts. Una revisión posterior debe evaluar impacto de terceros en rendimiento, seguridad y accesibilidad.

Historia futura propuesta: prioridad baja, dependiente de aprobación Riot/producto y selección de proveedor. Alcance: un emplazamiento experimental reversible, reserva responsive, rotulado, consentimiento cuando corresponda y medición. Criterios: sin identidades Riot enviadas a terceros, sin overflow ni clics engañosos, CLS medido y navegación preservada. Límites: sin pagos/premium, sin cambiar estadísticas ni habilitar publicidad por esta tarea.
