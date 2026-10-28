import "./dom-environment.ts";
import assert from "node:assert/strict";
import { test, afterEach } from "node:test";
import { render, screen, fireEvent, cleanup } from "@testing-library/react";
import { ProfileIcon } from "../src/profile-icon.tsx";
import { profileIconPath, profileIconUrl } from "../src/data-dragon.ts";
afterEach(cleanup);
test("profile icon is historical, validated, and falls back after image failure", () => {
  assert.equal(profileIconPath(-1), null);
  assert.equal(profileIconPath(NaN), null);
  assert.equal(
    profileIconUrl("https://evil", 29),
    "https://ddragon.leagueoflegends.com/cdn/16.17.1/img/profileicon/29.png",
  );
  render(<ProfileIcon id={29} observedAt="2026-01-01" />);
  const image = screen.getByRole("img");
  assert.equal(image.getAttribute("src"), "/api/assets/profile-icons/29");
  fireEvent.error(image);
  assert.ok(screen.getByRole("img", { name: "Ícono de perfil no disponible" }));
});
test("absent profile icon makes no resource URL", () => {
  render(<ProfileIcon />);
  assert.ok(screen.getByRole("img", { name: "Ícono de perfil no disponible" }));
});
