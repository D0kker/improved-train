import assert from "node:assert/strict";
import { test } from "node:test";
import { GET } from "../app/api/assets/profile-icons/[iconId]/route.ts";
test("profile asset endpoint rejects invalid IDs and content types without arbitrary URLs", async () => {
  const original = globalThis.fetch;
  let calls = 0;
  globalThis.fetch = async (input) => {
    calls++;
    assert.match(
      String(input),
      /^https:\/\/ddragon\.leagueoflegends\.com\/cdn\/\d+\.\d+\.\d+\/img\/profileicon\/29.png$/,
    );
    return new Response("invalid", {
      headers: { "Content-Type": "text/html" },
    });
  };
  try {
    const context = (iconId: string) => ({
      params: Promise.resolve({ iconId }),
    });
    assert.equal(
      (await GET(new Request("http://local"), context("../evil"))).status,
      404,
    );
    assert.equal(calls, 0);
    assert.equal(
      (await GET(new Request("http://local"), context("29"))).status,
      404,
    );
    assert.equal(calls, 1);
    globalThis.fetch = async () =>
      new Response(new Uint8Array([137, 80, 78, 71]), {
        headers: { "Content-Type": "image/png" },
      });
    const response = await GET(new Request("http://local"), context("29"));
    assert.equal(response.status, 200);
    assert.equal(response.headers.get("X-Content-Type-Options"), "nosniff");
  } finally {
    globalThis.fetch = original;
  }
});
