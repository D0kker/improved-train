import "./dom-environment.ts";
import assert from "node:assert/strict";
import { afterEach, test } from "node:test";
import { cleanup, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { act } from "react";
import { hydrateRoot } from "react-dom/client";
import { renderToString } from "react-dom/server";
import { useTranslations } from "next-intl";
import { NextIntlClientProvider } from "next-intl";
import { SiteHeader } from "../src/site-header.tsx";
import { renderWithIntl } from "./render-with-intl.tsx";
import {
  localePreferenceCookie,
  negotiateLocale,
  resolveLocale,
} from "../src/i18n/locales.ts";
import { formatDashboardDate } from "../src/dashboard-presentation.ts";
import { formatDuration, formatPercent } from "../src/format.ts";
import { messages } from "../src/i18n/messages.ts";

afterEach(cleanup);

test("locale resolution prioritizes explicit preference, supported browser language, then Spanish", () => {
  assert.equal(resolveLocale("pt-BR", "en-US,en;q=0.9"), "pt-BR");
  assert.equal(resolveLocale("xx", "en-US,en;q=0.9"), "en");
  assert.equal(resolveLocale(undefined, "pt-BR,pt;q=0.8"), "pt-BR");
  assert.equal(resolveLocale(undefined, "fr-FR,fr;q=0.9"), "es");
  assert.equal(negotiateLocale("en;q=0.4,pt;q=0.8"), "pt-BR");
});

test("explicit language preference cookie is persistent and secure on HTTPS", () => {
  assert.match(localePreferenceCookie("en", "https:"), /Max-Age=31536000/);
  assert.match(localePreferenceCookie("en", "https:"), /Secure/);
  assert.doesNotMatch(localePreferenceCookie("pt-BR", "http:"), /Secure/);
});

test("selector announces its label, exposes all locales, persists selection and invokes the locale reload action", async () => {
  const user = userEvent.setup();
  let changed = "";
  renderWithIntl(
    <SiteHeader
      onLocaleChange={(locale) => {
        changed = locale;
      }}
    />,
  );
  const selector = screen.getByRole("combobox", { name: "Idioma" });
  assert.deepEqual(
    Array.from((selector as HTMLSelectElement).options).map(
      (option) => option.value,
    ),
    ["es", "en", "pt-BR"],
  );
  await user.selectOptions(selector, "en");
  assert.equal(changed, "en");
  assert.match(document.cookie, /echo-locale=en/);
});

function PluralProbe() {
  const t = useTranslations("dashboard");
  return <p>{t("loadedMatches", { count: 2 })}</p>;
}

test("plural messages render in English and Brazilian Portuguese", () => {
  const { unmount } = renderWithIntl(<PluralProbe />, "en");
  assert.equal(
    screen.getByText("2 matches loaded").textContent,
    "2 matches loaded",
  );
  unmount();
  renderWithIntl(<PluralProbe />, "pt-BR");
  assert.equal(
    screen.getByText("2 partidas carregadas").textContent,
    "2 partidas carregadas",
  );
});

test("dates and durations use the selected locale without changing stored timestamps", () => {
  const timestamp = "2026-10-27T13:00:00Z";
  assert.match(formatDashboardDate(timestamp, "en"), /Oct/);
  assert.match(formatDashboardDate(timestamp, "pt-BR"), /out\.|outubro/i);
  assert.match(formatDashboardDate(timestamp, "en"), /1:00 PM UTC/);
  assert.equal(formatDuration(605), "10:05");
  assert.equal(formatDuration(605, "en"), "10m 5s");
  assert.equal(formatDuration(605, "pt-BR"), "10min 5s");
  assert.equal(formatPercent(12345.5, "es"), "12.345,5%");
  assert.equal(formatPercent(12345.5, "en"), "12,345.5%");
  assert.equal(formatPercent(12345.5, "pt-BR"), "12.345,5%");
});

test("all locale catalogs expose the same namespace and message keys", () => {
  const shape = (value: unknown): unknown =>
    Array.isArray(value)
      ? value.map(shape)
      : value && typeof value === "object"
        ? Object.fromEntries(
            Object.entries(value).map(([key, child]) => [key, shape(child)]),
          )
        : typeof value;
  assert.deepEqual(shape(messages.en), shape(messages.es));
  assert.deepEqual(shape(messages["pt-BR"]), shape(messages.es));
});

test("localized server markup hydrates without text or locale mismatch", async () => {
  function Probe() {
    const t = useTranslations("dashboard");
    return (
      <p>
        {t("loadedMatches", { count: 2 })} —{" "}
        {formatDashboardDate(timestamp, "en")}
      </p>
    );
  }
  const timestamp = "2026-10-27T13:00:00Z";
  const element = (
    <NextIntlClientProvider locale="en" messages={messages.en}>
      <Probe />
    </NextIntlClientProvider>
  );
  const container = document.createElement("div");
  container.innerHTML = renderToString(element);
  document.body.append(container);
  const errors: unknown[][] = [];
  const originalError = console.error;
  console.error = (...args) => errors.push(args);
  const root = hydrateRoot(container, element);
  try {
    await act(async () => {});
  } finally {
    await act(async () => root.unmount());
    console.error = originalError;
    container.remove();
  }
  assert.deepEqual(errors, []);
});
