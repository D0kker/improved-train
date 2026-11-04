# T04.7.2 — Revisión focalizada de contraste

Estos cálculos usan los tokens CSS de RELEASE-06 y los estilos del selector de T04.7. Son una comprobación de colores, no una auditoría integral de WCAG ni una medición de todos los píxeles y estados. Axe dejó `color-contrast` como incompleto en T04.6.4; la inspección real de navegador y tecnologías asistivas sigue siendo necesaria.

## Método

Se mezclan los canales **sRGB** de `color-mix(in srgb, ...)` y se compone la transparencia sobre el fondo supuesto. Después se convierte cada canal a luz lineal con la función WCAG y se calcula `(L_claro + 0,05) / (L_oscuro + 0,05)`. Para las tarjetas se calculan ambos extremos: superficie `#0b1728`, tinte azul `#60a5fa` al 9 %, tinte coral `#f87171` al 9 % y fila del owner `#e8c56a` al 9 %. El gradiente entre los extremos de cada tarjeta es gradual. Las etiquetas se identifican también mediante texto, no solo por color.

| Elemento / primer plano                              | Superficie | Azul al 9 % | Coral al 9 % | Owner dorado al 9 % |
| ---------------------------------------------------- | ---------: | ----------: | -----------: | ------------------: |
| Texto claro `#edf7ff`                                |    16,57:1 |     14,44:1 |      14,89:1 |             13,99:1 |
| Texto secundario `#a8bacd`                           |     9,05:1 |      7,89:1 |       8,13:1 |              7,64:1 |
| Texto sutil `#8ea3b8`                                |     6,92:1 |      6,03:1 |       6,22:1 |              5,84:1 |
| Texto y borde dorado `#e8c56a`                       |    10,82:1 |      9,42:1 |       9,71:1 |              9,13:1 |
| Foco cian `#55ddf2` al 45 % compuesto sobre el fondo |     3,22:1 |      3,10:1 |       3,11:1 |              3,06:1 |

El texto claro del selector sobre `--surface-raised` (`#101d30`) da 15,60:1. El borde original `#20364d` daba 1,37:1 frente al interior y 1,54:1 frente al fondo base; se corrigió a `#8ea3b8`, que da 6,51:1 frente al interior y al menos 6,83:1 frente al fondo exterior calculado con la superposición conservadora de los gradientes del body y el 78 % de `#07101f` del encabezado. Su foco menta sólido `#61e2b4` frente a los cuatro fondos de la tabla da entre 9,43:1 y 11,17:1. El foco de `summary` utiliza cian sólido y no el cian al 45 %.

Para la advertencia se aplicó el máximo simultáneo conservador de los gradientes del body: menta al 13 % y cian al 8 % sobre `#07101f`, seguido del amarillo de la alerta `#fde68a` al 8 %. El texto amarillo frente a ese fondo compuesto da aproximadamente 8,02:1. El texto rojo de error `#fda4af` frente a la superficie de su panel `#0b1728` da 9,51:1. El texto dorado del indicador de cobertura frente a ese fondo conservador del body da 7,37:1. La superposición simultánea máxima de ambos gradientes es un límite deliberadamente conservador y puede no ocurrir en un punto real de la página.

## Reproducción

```python
def rgb(h):
    return tuple(int(h[i:i+2], 16) / 255 for i in (1, 3, 5))

def mix(foreground, background, alpha):
    return tuple(alpha * x + (1 - alpha) * y for x, y in zip(foreground, background))

def luminance(color):
    linear = lambda c: c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    return sum(weight * linear(channel) for weight, channel in zip((0.2126, 0.7152, 0.0722), color))

def contrast(a, b):
    low, high = sorted((luminance(a), luminance(b)))
    return (high + 0.05) / (low + 0.05)

surface = rgb('#0b1728')
backgrounds = [surface, mix(rgb('#60a5fa'), surface, .09),
               mix(rgb('#f87171'), surface, .09), mix(rgb('#e8c56a'), surface, .09)]
for color in ('#edf7ff', '#a8bacd', '#8ea3b8', '#e8c56a', '#55ddf2', '#61e2b4'):
    print(color, [round(contrast(rgb(color), background), 2) for background in backgrounds])
print('focus 45%', [round(contrast(mix(rgb('#55ddf2'), b, .45), b), 2) for b in backgrounds])
body_max = mix(rgb('#61e2b4'), mix(rgb('#55ddf2'), rgb('#07101f'), .08), .13)
warning = mix(rgb('#fde68a'), body_max, .08)
print('warning', round(contrast(rgb('#fde68a'), warning), 2))
print('error', round(contrast(rgb('#fda4af'), surface), 2))
print('selector', round(contrast(rgb('#edf7ff'), rgb('#101d30')), 2))
topbar_max = mix(rgb('#07101f'), body_max, .78)
print('selector border/interior', round(contrast(rgb('#8ea3b8'), rgb('#101d30')), 2))
print('selector border/exterior', round(contrast(rgb('#8ea3b8'), topbar_max), 2))
print('coverage', round(contrast(rgb('#e8c56a'), body_max), 2))
```

Estos cocientes superan las referencias de 4,5:1 para texto normal y 3:1 para los bordes/indicadores calculados, pero el foco genérico al 45 % queda cerca de 3:1. Deben revisarse también geometría, área del indicador, estilos de navegador, superposiciones y estados reales; no se declara conformidad WCAG completa.
