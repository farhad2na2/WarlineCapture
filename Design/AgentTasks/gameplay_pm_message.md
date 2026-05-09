# PM Message For Gameplay

Date: 2026-05-09
Priority: P0 integrate full M01 AI production art pack into ECS runtime and capture proof

PM heartbeat follow-up:

- `Design/AgentReports/2026-05-09_gameplay_m01-ai-production-assets-runtime-integration.md` is not visible yet.
- Continue this task now if unblocked.
- If blocked, write a blocker report immediately under `Design/AgentReports/` with the exact missing file, Unity/import issue, or implementation owner.
- Do not switch to fallback work.
- Do not commit or push.

PM escalation, 2026-05-09 12:25 Europe/Berlin:

- This lane is still silent after the previous PM nudge.
- Next heartbeat must produce one of these:
  - completion handoff: `Design/AgentReports/2026-05-09_gameplay_m01-ai-production-assets-runtime-integration.md`, or
  - blocker handoff: `Design/AgentReports/2026-05-09_gameplay_m01-ai-production-assets-runtime-blocker.md`.
- If Unity or asset import is still running, write a blocker/progress report with the running command/log path and current state.

PM accepts the v2 soldier import-readiness cleanup for runtime integration. This is not final visual approval; final approval requires runtime capture/video.

User clarification: integrate all new M01 AI production art assets, not only the v2 soldiers.

Use:

- `Design/AgentReports/2026-05-09_designer_m01-soldier-v2-animation-aaa-audit.md`
- `Design/AgentReports/2026-05-09_gameplay_m01-soldier-v2-atlas-runtime-audit.md`
- `Design/AgentReports/2026-05-09_gameplay_m01-soldier-v2-import-metadata-cleanup.md`
- `Design/AgentReports/2026-05-09_art-atlas_m01-ai-production-asset-pack.md`
- `Design/AgentReports/2026-05-09_art-atlas_m01-soldier-animation-atlas-fix-v2.md`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_ai_production_asset_manifest.json`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest_v2.json`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/player_rifle_squad_animation_atlas_v2.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/enemy_patrol_animation_atlas_v2.png`

Use the full production asset manifest for maps/buildings/markers. Use the v2 soldier animation manifest for soldiers.

Integrate:

- strategic/background map,
- all tactical map plates,
- building/prop atlases and states,
- marker atlas and individual marker sprites,
- use v2 manifest as the ECS soldier animation source,
- keep player/enemy atlases separate,
- use manifest fps, loop flags, facing ids, state ids, anchors, bounds, and frame order,
- no SpriteRenderer/MeshRenderer runtime soldier presentation,
- replace old/pre-production M01 visuals where equivalent AI production assets exist,
- capture runtime proof at actual M01 camera scale showing the production map/background, buildings, markers, selected player rifle squad idle/run, and enemy patrol if reachable.

Expected report:

`Design/AgentReports/2026-05-09_gameplay_m01-ai-production-assets-runtime-integration.md`

Return whether the full AI production visual integration is ready for PM/user runtime review or needs Gameplay/Art fixes. Do not commit or push.
