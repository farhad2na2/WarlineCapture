# Lane
Gameplay

# Task
P0 bind accepted M01 Art v6/v5 assets and regenerate runtime target-match proof. Follow-up correction after user review: redo the soldier presentation to use the already-produced TargetMatchV5 bottom/top angle assets instead of the side-biased animation fallback.

# Files changed
- `Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs`
- `Assets/Game/Scripts/Components/MissionRuntimeComponents.cs`
- `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
- `Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeVisualMatchProofCapture.cs`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v6_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v6_vs_Target_Comparison.png`
- `Design/AgentReports/2026-05-17_gameplay_m01-v6-art-binding-runtime-proof.md`

# Contracts touched
- `Design/Architecture/gameplay_solid_ecs_contract.md`: followed. Runtime soldier presentation remains ECS data plus ECS systems; editor code only captures proof; no bootstrap route policy or scene-start replacement was added.
- `Design/M01_FirstContact_Production_Contract.md`: preserved contracted M01 ids, including `iso.ch01.district_edge_01`, `unit.player.rifle_squad_01`, and `unit.enemy.patrol_01`.
- PM Art acceptance: `Design/AgentReports/2026-05-17_pm_art-atlas-m01-v6-accepted-gameplay-binding.md`.

# User-visible behavior
M01 still launches through the existing loading/main-menu/custom-game/match route and now binds the accepted visual-lock package in the runtime ECS presentation path:
- v6 tactical plate: `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v6_source_1920x1080.png`
- TargetMatchV5 player idle facing atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/PlayerRifleSquad/TargetMatchV5/player_rifle_squad_idle_facings_atlas_v5.png`
- TargetMatchV5 enemy idle facing atlas: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/EnemyPatrol/TargetMatchV5/enemy_patrol_idle_facings_atlas_v5.png`
- TargetMatchV5 strong shadow atlas was bound for proof, but is now rejected for final M01 visual approval because its cast direction does not match the baked map-light direction:
  `Design/VisualLock/Gameplay/M01_AIProductionAssets/Shadows/TargetMatchV5/unit_shadow_facings_atlas_v5_strong.png`
- TargetMatchV5 enemy readability/health markers: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Markers/TargetMatchV5/`

Correction made from the user review:
- Player/lower squad no-selection idle now uses the produced upward/top-facing TargetMatchV5 angle (`NW` slot), not the side fallback.
- Enemy/upper patrol no-selection idle now uses the produced downward/bottom-facing TargetMatchV5 angle (`SE` slot), not the same player fallback.
- Infantry scale and formation offsets were retuned after capture review so the squads are closer to the target mockup instead of the earlier wide side-facing spread.

# Validation run
Runtime capture:
```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV6 -logFile /private/tmp/warlinecapture-m01-game-flow-v6-targetmatch-spacing-redo.log
```

Comparison generation:
```bash
magick Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v6_1920x1080.png +append Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v6_vs_Target_Comparison.png
```

Architecture contract:
```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform editmode -testFilter GameplayArchitectureContractTests -testResults /private/tmp/warlinecapture-gameplay-architecture-contract-targetmatch-redo.xml -logFile /private/tmp/warlinecapture-gameplay-architecture-contract-targetmatch-redo.log
```

Diff hygiene:
```bash
git diff --check -- Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs Assets/Game/Scripts/Components/MissionRuntimeComponents.cs Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeVisualMatchProofCapture.cs Design/AgentReports/2026-05-17_gameplay_m01-v6-art-binding-runtime-proof.md
```

# Validation result
Focused Gameplay validation passed. Full visual target match still needs review/fixes before QA.

- Runtime capture command exited 0.
- Fresh capture: `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v6_1920x1080.png`.
- Fresh side-by-side comparison: `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v6_vs_Target_Comparison.png`.
- Existing route proof remained intact: `WARLINECAPTURE_M01_GAME_FLOW_CAPTURED path=Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v6_1920x1080.png`.
- ECS soldier proof: `WARLINECAPTURE_M01_ECS_QUAD_DIAG_SUMMARY runtimes=2 visibleSoldiers=8`.
- Corrected player facing proof: player diagnostics show `frame=idle_targetmatch_v5.NW.0` using `player_rifle_squad_idle_facings_atlas_v5`.
- Corrected enemy facing proof: enemy diagnostics show `frame=idle_targetmatch_v5.SE.0` using `enemy_patrol_idle_facings_atlas_v5`.
- Shadow proof: player and enemy `soldierShadow` overlays show `total=4 visible=4` using `unit_shadow_facings_atlas_v5_strong`.
- Enemy overlay proof: `enemyReadability` and `enemyHealthBar` each show `total=4 visible=4`.
- `GameplayArchitectureContractTests`: passed 6/6. Results: `/private/tmp/warlinecapture-gameplay-architecture-contract-targetmatch-redo.xml`.
- Focused `git diff --check` on Gameplay-touched files: passed.
- Repo-wide `git diff --check` is blocked by unrelated trailing whitespace in `Assets/Game/Prefabs/UI/Components/PREFAB-04_AssistantButton.prefab`; Gameplay did not modify that UI prefab.

Visual self-review:
- Fixed: the runtime no longer uses the wrong side-facing soldier angle for M01 no-selection. Player and enemy are now opposing via the produced target-match top/bottom angle assets.
- Improved: soldier size, spacing, and enemy readability are closer than the rejected side-facing capture.
- Rejected: the currently bound separate shadow atlas casts opposite to the baked shadows in the M01 background. This is not acceptable for final visual approval. Gameplay should not keep tuning offsets against this source; Art needs to provide soldier shadows that match the map lighting.
- Still not final: the runtime HUD layout differs from the target mockup, and the target-match idle assets are static single-frame facings. The full v5 animation atlas remains available as fallback for animated states, but the no-selection visual proof now prioritizes target-facing correctness over idle frame cycling.

# Known gaps
- Full visual approval should remain held until PM/user reviews the new comparison. This pass fixes the specific wrong-angle/wrong-asset binding issue and improves spacing, but it is not a claim of pixel-perfect target lock.
- Soldier shadows are blocked on Art. The current separate TargetMatchV5 shadow atlas is directionally inconsistent with the baked M01 plate shadows. Request Art-provided shadows baked into the soldier/facing atlases, or per-facing/per-frame shadow assets explicitly authored to the same M01 map light direction and foot anchors.
- The no-selection TargetMatchV5 facing assets are static idle frames. They solve the top/bottom angle mismatch, but they do not provide a multi-frame idle animation proof for those exact target-match angles.
- HUD placement still diverges from the original M01-01 target image in the current runtime capture. That is outside this soldier-angle correction but remains part of the broader M01 visual target.
- Runtime copies under `Assets/Game/Art/Generated/...` for v5/v6 accepted assets are still absent; editor proof resolves accepted `Design/VisualLock` assets directly. Productionization should import/copy locked assets under `Assets` once PM locks them for shipping.

# Cross-lane impacts
- Art/Atlas: the produced TargetMatchV5 top/bottom angle atlases are now actually used in the Gameplay runtime proof. Shadows must be rerouted to Art: provide M01-light-matched baked/contact shadows for each soldier facing/frame, preferably integrated into the soldier atlas so the runtime cannot drift from the map's baked shadow direction. If PM wants animated idle plus exact top/bottom angles, Art also needs multi-frame target-match idle strips for those same facings.
- UI/HCI/QA: should remain held for full visual approval until PM/user accepts the refreshed comparison.
- Architecture: no new bootstrap/static logging debt added. Existing startup static logging remains grandfathered debt covered by the architecture contract tests.

# Next recommended task
Route to Art/Atlas for M01-light-matched soldier shadows before another final visual pass. Gameplay can continue HUD/layout target-match and production import, but soldier-shadow approval is blocked until Art provides baked/integrated or per-frame shadow assets that match the background plate's shadow direction.
