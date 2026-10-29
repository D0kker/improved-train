import assert from "node:assert/strict";
import { test } from "node:test";
import { formatDashboardDate } from "../src/dashboard-presentation.ts";
import {
  platformRegionLabel,
  platformRegionName,
} from "../src/platform-region.ts";

test("dashboard display names do not change routing or existing region labels", () => {
  assert.equal(platformRegionName("LA1"), "Latinoamérica Norte (LAN)");
  assert.equal(platformRegionName("la2"), "Latinoamérica Sur (LAS)");
  assert.equal(platformRegionName(undefined), "Región no disponible");
  assert.equal(platformRegionName("unrecognized"), "Región configurada");
  assert.equal(platformRegionLabel("la1"), "Latinoamérica Norte (LAN) · LA1");
});

test("compact dashboard date uses Spanish month names without exposing invalid dates", () => {
  assert.equal(formatDashboardDate(null), "Fecha no disponible");
  assert.equal(formatDashboardDate("invalid"), "Fecha no disponible");
  const value = formatDashboardDate("2026-10-27T18:00:00Z");
  assert.match(value, /27 oct 2026/);
  assert.match(value, /\d+:\d{2}/);
  assert.doesNotMatch(value, /NaN|Invalid Date|T18/);
});
