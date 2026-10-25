import type { ReactNode } from "react";
import { Card, Container } from "@/src/design-system";
import type { homeCopy } from "./home-copy";
type HomeCopy = typeof homeCopy;

export function HomeContent({
  searchForm,
  platformRegionLabel,
  copy,
}: {
  searchForm: ReactNode;
  platformRegionLabel: string;
  copy: HomeCopy;
}) {
  return (
    <main className="home-shell" id="main-content" tabIndex={-1}>
      <Container size="wide" className="home-content">
        <section className="home-hero" aria-labelledby="page-title">
          <div className="home-intro">
            <p className="eyebrow">{copy.eyebrow}</p>
            <h1 id="page-title">{copy.title}</h1>
            <p className="home-lede">{copy.description}</p>
          </div>

          <Card variant="elevated" className="home-search-card">
            <div className="home-search-heading">
              <h2>{copy.searchHeading}</h2>
              <p>{copy.searchDescription}</p>
            </div>
            {searchForm}
            <p className="region-note" aria-label="Región configurada">
              Región de búsqueda: <strong>{platformRegionLabel}</strong>
            </p>
          </Card>

          <p className="home-scope">{copy.scope}</p>
        </section>

        <section className="home-how" aria-labelledby="how-title">
          <div className="home-section-heading">
            <p className="eyebrow">{copy.howEyebrow}</p>
            <h2 id="how-title">{copy.howTitle}</h2>
          </div>
          <div className="home-steps">
            {copy.steps.map((step) => (
              <Card className="home-step" key={step.number}>
                <span className="home-step-number" aria-hidden="true">
                  {step.number}
                </span>
                <h3>{step.title}</h3>
                <p>{step.description}</p>
              </Card>
            ))}
          </div>
        </section>
      </Container>
    </main>
  );
}
