# Lane
Gameplay

# Task
P0 bind M01 v17 clean animation baked-shadow package and regenerate runtime proof.

# Files changed
- `Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs`
- `Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeVisualMatchProofCapture.cs`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v17_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v17_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/M01_V17_Facing_Diagnostic_NE_SE_SW_NW.png`
- `Design/AgentReports/2026-05-18_gameplay_m01-v17-clean-animation-baked-shadow-runtime-proof.md`

# Contracts touched
- `Design/Architecture/gameplay_solid_ecs_contract.md`: followed. V17 binding is in the existing ECS/runtime sprite asset resolver and presentation path; no bootstrap mission policy, scene-start replacement, or new static gameplay facade was added.
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`: preserved contracted mission/map/unit ids.
- PM acceptance: `Design/AgentReports/2026-05-18_pm_art-atlas-m01-v17-accepted-gameplay-binding.md`.
- Art handoff: `Design/AgentReports/2026-05-18_art-atlas_m01-v17-clean-animation-baked-shadow-handoff.md`.

# User-visible behavior
M01 still launches through the existing loading/main-menu/custom-game/match flow. Runtime soldiers now bind the accepted v17 baked body+shadow animation atlases:

- Player: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV17/PlayerRifleSquad/player_rifle_squad_animation_body_shadow_atlas_v17.png`
- Enemy: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/IntegratedShadowV17/EnemyPatrol/enemy_patrol_animation_body_shadow_atlas_v17.png`
- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_baked_soldier_shadow_manifest_v17.json`
- Plate remains v6: `Design/VisualLock/Gameplay/M01_AIProductionAssets/TacticalMaps/m01_tactical_start_clean_plate_v6_source_1920x1080.png`
- Markers/readability remain v5: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Markers/TargetMatchV5/`

The separate TargetMatchV5 soldier shadow atlas is no longer bound for v17. Diagnostics show the legacy `soldierShadow` overlay entities are disabled (`visible=0`) because the shadows are baked into the v17 soldier atlas texture.

# Validation run
Runtime capture:
```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV17 -logFile /private/tmp/warlinecapture-m01-game-flow-v17-facing-correction-r2.log
```

Comparison generation:
```bash
magick Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v17_1920x1080.png +append Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v17_vs_Target_Comparison.png
```

Architecture contract:
```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform editmode -testFilter GameplayArchitectureContractTests -testResults /private/tmp/warlinecapture-gameplay-architecture-contract-v17-baked-shadow.xml -logFile /private/tmp/warlinecapture-gameplay-architecture-contract-v17-baked-shadow.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform editmode -testFilter GameplayArchitectureContractTests -testResults /private/tmp/warlinecapture-gameplay-architecture-contract-v17-facing-correction.xml -logFile /private/tmp/warlinecapture-gameplay-architecture-contract-v17-facing-correction.log
```

Diff hygiene:
```bash
git diff --check -- Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeVisualMatchProofCapture.cs Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs Assets/Game/Scripts/Components/MissionRuntimeComponents.cs
```

# Validation result
Focused Gameplay validation passed. Full visual approval is still held.

- Capture command exited 0.
- Fresh runtime capture: `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v17_1920x1080.png`.
- Fresh comparison: `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v17_vs_Target_Comparison.png`.
- Normal flow proof: `WARLINECAPTURE_M01_GAME_FLOW_LAUNCHED splash=1 main=1 quickCustom=1 match=1 activeMission=saga.ch01.m01.first_contact`.
- Capture proof: `WARLINECAPTURE_M01_GAME_FLOW_CAPTURED path=Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v17_1920x1080.png`.
- ECS visible soldier proof: `WARLINECAPTURE_M01_ECS_QUAD_DIAG_SUMMARY runtimes=2 visibleSoldiers=8`.
- Player v17 atlas proof: `frame=idle_v17_baked_shadow.NW.1/2/3 tex=player_rifle_squad_animation_body_shadow_atlas_v17`.
- Enemy v17 atlas proof: `frame=idle_v17_baked_shadow.NE.1/2/3 tex=enemy_patrol_animation_body_shadow_atlas_v17`.
- User review after this proof rejects the soldier direction read in the runtime capture. The code and runtime logs prove the facing values changed, but the selected v17 facing labels do not visually satisfy the required bottom-soldiers-look-up/top-enemies-look-down target in the final camera.
- Facing diagnostic added: `Design/AgentReports/Captures/M01_V17_Facing_Diagnostic_NE_SE_SW_NW.png`. Order is row 1 player `NE, SE, SW, NW`; row 2 enemy `NE, SE, SW, NW`.
- Separate shadow atlas avoided: `WARLINECAPTURE_M01_ECS_OVERLAY_SUMMARY kind=soldierShadow total=4 visible=0` for both player and enemy runtimes.
- Enemy readability/health overlays remain active: `enemyReadability total=4 visible=4`, `enemyHealthBar total=4 visible=4`.
- Runtime draw count: `WARLINECAPTURE_M01_ECS_RUNTIME_QUAD_CAPTURE_DRAW_COUNT count=16`.
- `GameplayArchitectureContractTests`: passed 6/6 at `/private/tmp/warlinecapture-gameplay-architecture-contract-v17-facing-correction.xml`.
- Focused `git diff --check`: passed.

Visual self-review:
- Accepted for binding proof: v17 baked atlas is used by the actual runtime ECS presentation path, with animated frame diagnostics.
- Not accepted for final visual target match: user review confirms the runtime soldier direction still reads wrong.
- Root cause of this miss: Gameplay changed the bound facing values, but selected the mapping by compass labels and one runtime capture instead of producing an explicit facing matrix proof first. The v17 files contain `NE/SE/SW/NW` facings, but those labels are not enough to guarantee the screen-space read in the M01 camera.
- Additional implementation gap: the current no-selection policy applies one facing to all four soldiers in each squad. If the target requires per-soldier pose variation inside the formation, the ECS presentation data needs per-soldier facing slots rather than one squad-wide facing.
- Not accepted for final visual target match: v17 soldiers also read too dark/blue against the target mockup, and the HUD/composition still does not match the target exactly.
- The animation proof shows frame advancement, but Art's own caveat remains: v17 resamples source poses, so cadence/stutter should be PM/user reviewed before final approval.

# Known gaps
- Runtime visual approval remains held. This report proves binding and flow, not final target match.
- Soldier direction remains unresolved after user runtime review. The latest code uses player `NW` and enemy `NE`, and the latest log proves those frames are active; the visual result is still rejected.
- Soldier color/value still diverges from the target. The v17 units are much darker/bluer than the mockup soldiers in the side-by-side proof.
- HUD/layout composition still differs from the target mockup and needs a separate continuation if PM wants Gameplay to keep tuning M01 runtime layout.
- Runtime copies under `Assets/Game/Art/Generated/...` are still not productionized for v17; the editor proof resolves accepted `Design/VisualLock` assets directly.

# Cross-lane impacts
- Art/Atlas: v17 resolved the separate-shadow runtime blocker, but PM/user rejected the runtime direction read. Gameplay should first produce/validate an explicit runtime-facing matrix. If none of the v17 facings matches bottom-up/top-down at the target camera, Art should provide a corrected v18 direction-locked baked atlas with unambiguous bottom/player up-field and top/enemy down-field poses.
- Art/Atlas: PM/user should also review unit value/color and animation cadence. If the dark/blue read is not approved, Art should provide corrected v18 baked atlases rather than Gameplay applying color hacks.
- UI/HCI/QA: remain held for full visual approval.
- Gameplay: ECS binding path is ready for continued composition/HUD tuning once PM decides whether v17 unit art is accepted visually.

# Next recommended task
Gameplay should stop guessing facing labels and generate a runtime-facing matrix using the actual M01 camera/scale for all accepted v17 facings. Then set the no-selection policy from the matrix. If no v17 facing combination matches the target bottom-up/top-down direction, route Art/Atlas for a v18 direction-locked baked atlas and keep this Gameplay report as needs-fix, not ready for QA.
