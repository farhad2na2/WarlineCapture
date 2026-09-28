# Copyable implementation assignment

Implement the approved Support system in `/Users/farhad/Projects/WarlineCapture` using `Design/AgentReports/SupportSystem/IMPLEMENTATION.md` and `TASKS.md`.

The owner approved the four-ability roster, full-screen Build-style popup and project-asset-based mockup on 2026-09-28. Do not ask for that approval again. The approved selection reference is `Mockups/05-support-fullscreen-v02-project-assets.png`; use existing native sprites/models from `support_asset_manifest.md`, not the rejected aircraft in v01.

Start with T00 and proceed sequentially. Complete a working, tested Smoke slice (T01–T07) before adding Strike, Paratroopers and Supply. Each task names required files, behavior, tests and evidence. Read only the relevant verified source map and task before expanding searches; do not repeatedly re-audit the whole project. Treat proposed NEW types as work to implement, not APIs that already exist.

Follow root AGENTS.md, `Design/Architecture/gameplay_solid_ecs_contract.md`, and the existing assembly boundaries. Gameplay belongs in unmanaged ECS data/systems; UI emits requests and renders read models. No new SupportAbilityService singleton, fake outcome injection, account-resource fallback, permissive hidden-target fallback or hand-edited Unity asset YAML while an Editor is reachable. Preserve unrelated dirty work.

Use the supplied `support_defaults.json` to synchronize authoritative design/config and create an Editor-generated runtime catalog. Do not load Design JSON in a player build or turn this fixture into a second runtime authority. All numerical values are initial implementation values to be tuned through evidence; do not change the approved roster or consent rules.

Maintain `implementation_progress.md` as defined in TASKS.md. Tests use repository Unity wrappers, explicit logs/timeouts and fail-closed pass markers. Never invoke Unity directly or use macOS batchmode. Read unity-cli skill before live Editor CLI commands. Retain failed evidence. Design approval, automated tests, native UI review, complete normal-input mission playthroughs, player acceptance and device acceptance are separate gates.

Do routine technical work autonomously. When a dependency is missing, implement the narrow adapter prescribed in the spec rather than introducing a new global framework. If a genuinely unresolved product choice or external blocker remains, report the exact evidence and continue independent tasks; do not silently omit an approved ability or mark the system ready.

For the full assignment, continue through T12 as far as the available mission/device environment permits, leaving any unavailable future-mission or real-device acceptance explicitly pending. Do not launch additional agents unless separately authorized.


## Cross-mode roadmap extension — 2026-09-28

The owner additionally requested Support planning for future Campaign missions, all 120 Skirmish battles and all 60 Operations. Follow [Roadmap/Support/PLAN.md](../../Roadmap/Support/PLAN.md) and its complete per-entry policy CSVs. Campaign remains the first implementation slice; X01–X05 then add separately validated mode adapters and coverage. This is authorization for the roadmap scope, not runtime acceptance or permission to expose old catalog availableModes without validation.
