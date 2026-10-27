import assert from "node:assert/strict";
import test from "node:test";
import { ApiError, fetchJson } from "../src/api.ts";
import { RequestLifetime } from "../src/request-lifetime.ts";

test("superseding or leaving a view cancels requests without retaining results", () => {
  const lifetime = new RequestLifetime();
  const first = lifetime.begin();
  const second = lifetime.begin();
  assert.equal(first.aborted, true);
  assert.equal(second.aborted, false);
  lifetime.cancel();
  assert.equal(second.aborted, true);
  const next = lifetime.begin();
  assert.equal(next.aborted, false);
  lifetime.cancel();
});

test("parent cancellation reaches a manually initiated request", () => {
  const parent = new AbortController();
  const lifetime = new RequestLifetime();
  const signal = lifetime.begin(parent.signal);
  parent.abort();
  assert.equal(signal.aborted, true);
  lifetime.cancel();
});

test("429 and 503 are surfaced once without automatic retries", async () => {
  const originalFetch = globalThis.fetch;
  let calls = 0;
  try {
    for (const status of [429, 503]) {
      globalThis.fetch = async () => {
        calls++;
        return Response.json(
          { detail: "Intenta más tarde." },
          {
            status,
            headers: { "Retry-After": "5" },
          },
        );
      };
      await assert.rejects(
        fetchJson("/api/v1/example"),
        (error: unknown) =>
          error instanceof ApiError && error.status === status,
      );
    }
    assert.equal(calls, 2);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("request cancellation reaches fetch and is not retried", async () => {
  const originalFetch = globalThis.fetch;
  const controller = new AbortController();
  let calls = 0;
  globalThis.fetch = async (_path, init) => {
    calls++;
    return await new Promise<Response>((_resolve, reject) => {
      init?.signal?.addEventListener(
        "abort",
        () => reject(init.signal?.reason),
        {
          once: true,
        },
      );
    });
  };
  try {
    const pending = fetchJson("/api/v1/example", { signal: controller.signal });
    controller.abort();
    await assert.rejects(pending, { name: "AbortError" });
    assert.equal(calls, 1);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("browser budget cancels slow fetch at 200s, before the 210s proxy", async (context) => {
  context.mock.timers.enable({ apis: ["setTimeout"] });
  const originalFetch = globalThis.fetch;
  let calls = 0;
  globalThis.fetch = async (_path, init) => {
    calls++;
    return await new Promise<Response>((_resolve, reject) => {
      init?.signal?.addEventListener("abort", () => reject(init.signal?.reason), {
        once: true,
      });
    });
  };
  try {
    const pending = fetchJson("/api/v1/example");
    const assertion = assert.rejects(pending, { name: "TimeoutError" });
    context.mock.timers.tick(200_000);
    await assertion;
    assert.equal(calls, 1);
  } finally {
    globalThis.fetch = originalFetch;
  }
});
