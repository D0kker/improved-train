---
name: lol-network-analyzer
description: Implement or review work in the LoL Network Analyzer repository when Riot API integration, match ingestion, player identity, relationship analysis, Docker Compose, or project continuity is involved. Do not use for unrelated League of Legends questions or generic web development.
---

# LoL Network Analyzer

Use this skill to keep repository work aligned with the product specification and its safety boundaries.

## Establish the current scope

Read `docs/PROJECT_CONTEXT.md`, `docs/DECISIONS.md`, and the relevant sections of `lol_network_analyzer_spec.md`. Read the full specification before starting a new sprint. Inspect the actual checkout and implement only the requested sprint or feature.

Treat planned architecture as planned until code, migrations, containers, and health checks prove otherwise. Keep `docs/TODO.md` and the context document aligned with observable results.

## Preserve core invariants

- Compliance and security outrank feature breadth.
- PUUID is the internal player identity; Riot ID is mutable lookup data.
- Browser and frontend never receive the Riot API key or call Riot directly.
- PostgreSQL is the source of truth. Look up a match locally before requesting it and retain raw JSONB for safe reprocessing.
- Bound concurrency, honor `429`, apply backoff and cancellation, and centralize routing/limits in configuration.
- CI and ordinary tests use mocked HTTP; never consume Riot quotas.
- Describe inferred relations as possible or likely. Do not desanonimize hidden players or label an inference as a verified duo.
- Keep Docker output compatible with ARM64 and AMD64 unless the user narrows the target.

## Verify changing external facts

Before implementing behavior that depends on Riot endpoints, policy, rate limits, legal text, API key class, region routing, or monetization, consult current official Riot documentation. If it conflicts with the specification, stop the incompatible implementation and record the discrepancy rather than silently changing product intent.

## Finish with evidence

Run the checks defined by the affected app plus `docker compose config` and relevant health checks when containers change. Update `docs/PROJECT_CONTEXT.md`; add a dated decision only for a durable architectural choice. Never record secrets, real player data, raw Riot responses, or credentials in documentation, fixtures, logs, or memory.
