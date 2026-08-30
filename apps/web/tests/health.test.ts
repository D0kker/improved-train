import assert from "node:assert/strict";
import test from "node:test";

import { healthPayload } from "../src/health.ts";

test("health payload remains stable for container probes", () => {
  assert.deepEqual(healthPayload(), { status: "healthy" });
});
