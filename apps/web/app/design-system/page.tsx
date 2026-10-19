import { notFound } from "next/navigation";
import {
  Badge,
  Button,
  Card,
  Container,
  Divider,
  EmptyState,
  LoadingState,
  SectionHeader,
  StatCard,
  StatusMessage,
} from "@/src/design-system";
import { isDesignSystemGalleryEnabled } from "@/src/design-system-access";

export const dynamic = "force-dynamic";

const colors = [
  ["Fondo", "--echo-ink-950"],
  ["Superficie", "--echo-ink-900"],
  ["Elevada", "--echo-ink-850"],
  ["Texto", "--echo-text"],
  ["Texto secundario", "--echo-text-muted"],
  ["Cian", "--echo-cyan"],
  ["Menta", "--echo-mint"],
  ["Dorado", "--echo-gold"],
  ["Violeta", "--echo-violet"],
  ["Éxito", "--echo-success"],
  ["Advertencia", "--echo-warning"],
  ["Error", "--echo-danger"],
  ["Equipo azul", "--echo-blue-team"],
  ["Equipo rojo", "--echo-red-team"],
  ["Jugador consultado", "--echo-observed"],
];

export default function DesignSystemPage() {
  if (
    !isDesignSystemGalleryEnabled(
      process.env.NODE_ENV,
      process.env.ENABLE_DESIGN_SYSTEM_GALLERY,
    )
  )
    notFound();

  return (
    <main id="main-content" className="ds-gallery">
      <Container size="wide">
        <div className="ds-gallery__intro">
          <p className="eyebrow">PROJECT ECHO · UI FOUNDATION</p>
          <h1>Design System Cyber Arcade</h1>
          <p>
            Galería de componentes y tokens visuales con ejemplos ficticios.
            Esta página no consulta el backend ni muestra datos de jugadores.
          </p>
        </div>
      </Container>

      <Container size="wide" className="ds-gallery__section">
        <SectionHeader
          title="Paleta semántica"
          description="Los estados se identifican por etiqueta y tratamiento visual, además del color."
        />
        <div className="ds-gallery__grid">
          {colors.map(([label, token]) => (
            <Card key={token} className="ds-gallery__swatch">
              <span>{label}</span>
              <span
                className="ds-gallery__swatch-color"
                style={{ backgroundColor: `var(${token})` }}
              />
              <code>{token}</code>
            </Card>
          ))}
        </div>
      </Container>

      <Container size="wide" className="ds-gallery__section">
        <SectionHeader
          title="Tipografía y espaciado"
          description="Familia legible del sistema, jerarquía breve, números tabulares y escala consistente."
        />
        <Card className="ds-gallery__type">
          <h3>Precisión en cada partida</h3>
          <h4>Encuentros anteriores</h4>
          <p>
            Texto de lectura preparado para etiquetas más largas en español,
            inglés y portugués de Brasil.
          </p>
          <p className="ui-stat-card__value">1,284 · 98.6%</p>
        </Card>
        <div className="ds-gallery__spacing">
          {[1, 2, 3, 4, 6, 8, 12, 16].map((step) => (
            <div key={step} className="ds-gallery__spacing-item">
              <span
                className="ds-gallery__spacing-box"
                style={{ width: `var(--echo-space-${step})` }}
              />
              <code>space-{step}</code>
            </div>
          ))}
        </div>
      </Container>

      <Container size="wide" className="ds-gallery__section">
        <SectionHeader
          title="Acciones"
          description="Botones de al menos 44 px, con estados deshabilitado y carga explícitos."
        />
        <div className="ds-gallery__row">
          <Button>Primary action</Button>
          <Button variant="secondary">Secondary action</Button>
          <Button variant="ghost">Ghost action</Button>
          <Button disabled>Disabled</Button>
          <Button loading>Loading</Button>
        </div>
        <div className="ds-gallery__row">
          <Button className="ds-gallery__focus-demo">Focusable action</Button>
        </div>
      </Container>

      <Container size="wide" className="ds-gallery__section">
        <SectionHeader
          title="Tarjetas, badges y estadísticas"
          description="Variantes presentacionales independientes de los contratos de la API."
        />
        <div className="ds-gallery__grid">
          <Card>
            <h3>Default card</h3>
            <p>Contenido informativo de ejemplo.</p>
          </Card>
          <Card variant="elevated">
            <h3>Elevated card</h3>
            <p>La elevación mantiene el contraste y la lectura.</p>
          </Card>
          <Card variant="highlighted">
            <h3>Highlighted card</h3>
            <p>Un acento discreto señala prioridad visual.</p>
          </Card>
        </div>
        <div className="ds-gallery__row">
          {(
            [
              "neutral",
              "success",
              "warning",
              "error",
              "info",
              "partial",
              "insufficient",
            ] as const
          ).map((v) => (
            <Badge key={v} variant={v}>
              {v}
            </Badge>
          ))}
        </div>
        <div className="ds-gallery__stat-grid">
          <Card>
            <StatCard
              label="Partidas revisadas"
              value="5"
              description="Ventana inicial de ejemplo"
            />
          </Card>
          <Card>
            <StatCard
              label="Encuentros anteriores"
              value="12"
              description="Dato ficticio para la galería"
            />
          </Card>
          <Card>
            <StatCard
              label="Texto largo de etiqueta"
              value="Long translated value that wraps safely"
              description="Sin truncar información esencial."
            />
          </Card>
        </div>
      </Container>

      <Container size="wide" className="ds-gallery__section">
        <SectionHeader
          title="Estados"
          description="Los mensajes conservan etiquetas textuales y el error se anuncia como alerta."
        />
        <div className="ds-gallery__grid">
          <StatusMessage variant="success" title="Consulta completa">
            Se recuperó toda la información prevista para esta consulta.
          </StatusMessage>
          <StatusMessage variant="partial" title="Información parcial">
            Algunas partidas de ejemplo no pudieron recuperarse.
          </StatusMessage>
          <StatusMessage variant="insufficient" title="Datos insuficientes">
            No hay historial suficiente para completar el análisis.
          </StatusMessage>
          <StatusMessage variant="warning" title="Aviso">
            Este contenido puede estar incompleto.
          </StatusMessage>
          <StatusMessage variant="information" title="Información">
            Los ejemplos son ficticios y no se guardan.
          </StatusMessage>
          <StatusMessage variant="error" title="Error">
            No fue posible completar la consulta. Puedes reintentar.
          </StatusMessage>
        </div>
        <div className="ds-gallery__grid">
          <Card>
            <LoadingState message="Cargando datos de ejemplo…" />
          </Card>
          <Card>
            <EmptyState
              title="Aún no hay coincidencias"
              description="La ausencia de resultados no significa que falte información."
              action={<Button variant="secondary">Volver a buscar</Button>}
            />
          </Card>
        </div>
      </Container>

      <Container size="wide">
        <Divider />
      </Container>
    </main>
  );
}
