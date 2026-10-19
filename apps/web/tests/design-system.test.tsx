import "./dom-environment.ts";

import assert from "node:assert/strict";
import { afterEach, test } from "node:test";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import {
  Badge,
  Button,
  Card,
  Container,
  EmptyState,
  LoadingState,
  SectionHeader,
  StatCard,
  StatusMessage,
} from "../src/design-system.tsx";
import { isDesignSystemGalleryEnabled } from "../src/design-system-access.ts";

const originalFetch = globalThis.fetch;

afterEach(() => {
  cleanup();
  globalThis.fetch = originalFetch;
});

test("button exposes variants, keyboard action, and prevents disabled actions", async () => {
  const user = userEvent.setup();
  let actions = 0;
  render(
    <>
      <Button onClick={() => actions++}>Primary</Button>
      <Button variant="secondary">Secondary</Button>
      <Button variant="ghost">Ghost</Button>
      <Button disabled onClick={() => actions++}>
        Unavailable
      </Button>
    </>,
  );

  const primary = screen.getByRole("button", { name: "Primary" });
  assert.match(primary.className, /ui-button--primary/);
  assert.match(
    screen.getByRole("button", { name: "Secondary" }).className,
    /ui-button--secondary/,
  );
  assert.match(
    screen.getByRole("button", { name: "Ghost" }).className,
    /ui-button--ghost/,
  );
  await user.tab();
  assert.equal(document.activeElement, primary);
  await user.keyboard("{Enter}");
  await user.click(screen.getByRole("button", { name: "Unavailable" }));
  assert.equal(actions, 1);
});

test("loading button and loading state announce status and disable duplicate action", () => {
  render(
    <>
      <Button loading>Consultando</Button>
      <LoadingState message="Cargando partidas ficticias…" />
    </>,
  );
  const button = screen.getByRole("button", { name: "Consultando" });
  assert.equal(button.hasAttribute("disabled"), true);
  assert.equal(button.getAttribute("aria-busy"), "true");
  const status = screen.getByRole("status");
  assert.equal(status.getAttribute("aria-live"), "polite");
  assert.equal(status.textContent, "Cargando partidas ficticias…");
});

test("presentational primitives preserve long translated text, heading structure, and semantic states", () => {
  globalThis.fetch = async () => {
    throw new Error(
      "Los componentes presentacionales no deben consultar APIs.",
    );
  };
  render(
    <Container size="narrow">
      <SectionHeader
        title="A translated section title that is about forty percent longer"
        description="A longer description that must wrap without losing its wording on a narrow viewport."
      />
      <Card variant="highlighted">
        <StatCard
          label="Long translated metric label"
          value="12,345"
          description="An explanatory value that remains available in the rendered text."
        />
      </Card>
      <Badge variant="partial">Partial coverage</Badge>
      <StatusMessage variant="error" title="Error">
        A safe, actionable example error.
      </StatusMessage>
      <EmptyState
        title="No matches"
        description="No data is available in this fictional state."
      />
    </Container>,
  );

  assert.ok(
    screen.getByRole("heading", { level: 2, name: /forty percent longer/ }),
  );
  assert.equal(
    screen.getByText(
      "A longer description that must wrap without losing its wording on a narrow viewport.",
    ).textContent,
    "A longer description that must wrap without losing its wording on a narrow viewport.",
  );
  assert.match(screen.getByText("12,345").className, /ui-stat-card__value/);
  assert.match(
    screen.getByText("Partial coverage").className,
    /ui-badge--partial/,
  );
  assert.ok(
    screen.getByRole("alert").textContent?.includes("safe, actionable"),
  );
  assert.ok(screen.getByRole("heading", { name: "No matches" }));
});

test("card, badge, and coverage status variants retain distinct semantic classes", () => {
  const badgeVariants = [
    "neutral",
    "success",
    "warning",
    "error",
    "info",
    "partial",
    "insufficient",
  ] as const;
  const statusVariants = [
    "error",
    "warning",
    "information",
    "success",
    "partial",
    "insufficient",
  ] as const;
  render(
    <>
      <Card variant="elevated">Elevated</Card>
      <Card variant="highlighted">Highlighted</Card>
      {badgeVariants.map((variant) => (
        <Badge key={variant} variant={variant}>
          {variant}
        </Badge>
      ))}
      {statusVariants.map((variant) => (
        <StatusMessage key={variant} variant={variant} title={variant}>
          {variant} state text
        </StatusMessage>
      ))}
    </>,
  );

  assert.match(screen.getByText("Elevated").className, /ui-card--elevated/);
  assert.match(
    screen.getByText("Highlighted").className,
    /ui-card--highlighted/,
  );
  for (const variant of badgeVariants) {
    const badge = document.querySelector(`.ui-badge--${variant}`);
    assert.ok(badge);
    assert.match(badge.className, new RegExp(`ui-badge--${variant}`));
  }
  assert.equal(screen.getAllByRole("alert").length, 1);
  assert.equal(screen.getAllByRole("status").length, statusVariants.length - 1);
  for (const variant of statusVariants) {
    const status = document.querySelector(`.ui-status--${variant}`);
    assert.ok(status);
    assert.equal(
      status.getAttribute("role"),
      variant === "error" ? "alert" : "status",
    );
  }
});

test("design-system gallery guard is open in development and closed by default in production", () => {
  assert.equal(isDesignSystemGalleryEnabled("development", undefined), true);
  assert.equal(isDesignSystemGalleryEnabled("production", undefined), false);
  assert.equal(isDesignSystemGalleryEnabled("production", "false"), false);
  assert.equal(isDesignSystemGalleryEnabled("production", "true"), true);
  assert.equal(isDesignSystemGalleryEnabled("test", undefined), false);
});
