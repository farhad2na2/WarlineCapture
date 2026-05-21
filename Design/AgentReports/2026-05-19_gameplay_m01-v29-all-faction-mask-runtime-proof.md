# Gameplay Handoff - M01 V29 All-Faction Mask Runtime Proof

Date: 2026-05-19

## Lane

Gameplay

## Task

P0 bind V29 all-faction soldier atlas, faction mask color path, and V29 overscan tactical background for M01.

## Files changed

- `Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs`
- `Assets/Game/Scripts/Components/MissionRuntimeComponents.cs`
- `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
- `Assets/Game/Scripts/Systems/MissionRuntimeTerrainSurfaceRendererSystem.cs`
- `Assets/Game/Scripts/TacticalMaps/TacticalMapRuntimeLoader.cs`
- `Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeVisualMatchProofCapture.cs`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_mask_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_mask_soft_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_mask_20x9_2400x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_mask_21x9_2520x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_background_refreshfit_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_background_refreshfit_20x9_2400x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_background_refreshfit_21x9_2520x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_no_faction_tint_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_dark_faction_tint_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_darker_faction_tint_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_visible_body_tint_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_player_closest_direction_1920x1080.png`
- `Design/AgentReports/Captures/M01_V29_DirectionKey_Proof.png`
- `Design/AgentReports/Captures/M01_V29_Background_ArtVsRuntime_RefreshFit_Comparison.png`
- `Design/AgentReports/Captures/M01_V29_Background_RefreshFit_AspectProof.png`
- `Design/AgentReports/Captures/M01-01_RuntimeV29Mask_PlayerCrop.png`
- `Design/AgentReports/Captures/M01-01_RuntimeV29Mask_EnemyCrop.png`
- `Design/AgentReports/Captures/M01-01_RuntimeV29Mask_BackgroundCenterCrop.png`

## Contracts touched

- V29 soldier atlas binding now resolves `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_direction_locked_soldier_manifest_v29.json` before older V28/V17/V5 fallbacks.
- Neutral body/shadow atlas is rendered with `Color.white`; faction color is applied only to the new per-soldier `SoldierFactionMask` ECS quad layer.
- Body and mask use the same manifest frame rect, pivot-derived transform path, UV scale, and UV offset.
- Player soldiers currently use closest-available `screen_locked_D` because the required up-right diagonal is missing from V29. Enemy soldiers use accepted `screen_locked_B`.
- V29 background now resolves before V6 and uses aspect-specific runtime crops derived from the accepted V29 overscan POT source.
- Terrain runtime now stores camera-follow data for V29 runtime background sprites and the terrain renderer refreshes camera-fitted scale/position from the active camera.
- Follow-up pass reduced faction mask alpha/saturation so player/enemy color reads as a softer tint overlay instead of solid paint.
- User follow-up rejected the visible faction tint as still too strong. Gameplay disabled visible mask tint by setting player/enemy faction mask material alpha to `0.0` while leaving the mask texture/UV path intact for future Art-directed color tuning.
- User follow-up requested mostly-black masks with only a little blue/red. Gameplay set player mask to `RGBA(0.020, 0.040, 0.100, 0.380)` and enemy mask to `RGBA(0.120, 0.025, 0.020, 0.380)`.
- User follow-up requested darker blue/red again. Gameplay set player mask to `RGBA(0.008, 0.018, 0.060, 0.400)` and enemy mask to `RGBA(0.075, 0.010, 0.008, 0.400)`.
- Pixel audit showed mask-only color changes were effectively invisible on the current V29 mask/body assets. Gameplay added a visible M01-only body material tint as a temporary runtime fallback: player body `RGBA(0.260, 0.310, 0.430, 1.000)`, enemy body `RGBA(0.430, 0.240, 0.220, 1.000)`, with the mask layer still present.
- Direction audit showed V29 contains only four screen-locked reads: `screen_locked_A` up/away, `screen_locked_B` down/toward, `screen_locked_C` left, and `screen_locked_D` right. Gameplay changed the bottom player squad from straight-up `screen_locked_A` to closest-available `screen_locked_D` because the enemy group is up-right, while enemy remains `screen_locked_B` as accepted.
- Editor proof capture utility now supports 16:9, 20:9, and 21:9 actual-flow captures, applies the requested capture aspect before rendering, and refreshes the terrain surface before `Camera.Render()` so proof images cannot inherit a stale 4:3 terrain scale.

## User-visible behavior

- Actual M01 flow now shows blue player infantry and red enemy infantry using the same shared neutral V29 soldier atlas plus a colored mask overlay.
- Latest follow-up removes the visible colored soldier wash; player and enemy soldiers now render as the neutral shared V29 body atlas. Enemy readability still comes from red health bars and boot rings.
- Enemy soldiers no longer use the old V28/V5 substitution path.
- Body atlas material remains untinted, preserving baked shading and shadows from the neutral atlas.
- Tactical background uses the V29 overscan plate and covers 16:9, 20:9, and 21:9 without solid-color side fill. The previous runtime proof showed a 4:3-width map inside a 16:9 capture; the refreshed proof no longer has those bands.

## Validation run

- Unity compile/import:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -logFile /private/tmp/warlinecapture-v29-mask-compile.log`
- Architecture contract:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -testResults /private/tmp/warlinecapture-gameplay-architecture-contract-v29-mask.xml -logFile /private/tmp/warlinecapture-gameplay-architecture-contract-v29-mask.log`
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter GameplayArchitectureContractTests -testResults /private/tmp/warlinecapture-gameplay-architecture-contract-v29-backgroundfix.xml -logFile /private/tmp/warlinecapture-gameplay-architecture-contract-v29-backgroundfix.log`
- Actual-flow runtime captures:
  - `WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV29`
  - `WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV29_20x9`
  - `WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV29_21x9`
- Focused image audit:
  - Generated player, enemy, and center-background crops from the 16:9 runtime capture.
  - Edge-strip stddev audit for 16:9/20:9/21:9 captures to detect flat solid-fill bands.

## Validation result

- Unity compile/import: passed, no C# errors.
- `GameplayArchitectureContractTests`: passed 6/6 in the original V29 mask run and passed 6/6 again after the background refresh fix. Latest XML: `/private/tmp/warlinecapture-gameplay-architecture-contract-v29-backgroundfix.xml`.
- Actual M01 flow: passed. Logs show `splash=1 main=1 quickCustom=1 match=1`.
- Follow-up actual M01 flow after softer mask/background filtering: passed. Capture `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_mask_soft_1920x1080.png`.
- ECS proof: passed. Logs show 8 visible soldiers, body texture `soldier_animation_body_shadow_atlas_v29_pot_4096x2048`, mask texture `soldier_animation_faction_mask_atlas_v29_pot_4096x2048`, body material `RGBA(1.000, 1.000, 1.000, 1.000)`, mask queue `3101`.
- Player mask proof: passed. Initial overlay logged blue `RGBA(0.120, 0.550, 1.000, 0.960)`; softened follow-up logged `RGBA(0.300, 0.580, 0.950, 0.580)`.
- Enemy mask proof: passed. Initial overlay logged red `RGBA(0.920, 0.080, 0.050, 0.960)`; softened follow-up logged `RGBA(0.920, 0.240, 0.180, 0.580)`.
- No-tint follow-up: passed. Actual M01 flow capture `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_no_faction_tint_1920x1080.png`; log `/private/tmp/warlinecapture-m01-game-flow-v29-no-faction-tint.log` shows both player and enemy `soldierFactionMask` materials at `RGBA(1.000, 1.000, 1.000, 0.000)`.
- Dark faction follow-up: passed. Actual M01 flow capture `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_dark_faction_tint_1920x1080.png`; log `/private/tmp/warlinecapture-m01-game-flow-v29-dark-faction-tint.log` shows player masks at `RGBA(0.020, 0.040, 0.100, 0.380)` and enemy masks at `RGBA(0.120, 0.025, 0.020, 0.380)`.
- Darker faction follow-up: passed. Actual M01 flow capture `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_darker_faction_tint_1920x1080.png`; log `/private/tmp/warlinecapture-m01-game-flow-v29-darker-faction-tint.log` shows player masks at `RGBA(0.008, 0.018, 0.060, 0.400)` and enemy masks at `RGBA(0.075, 0.010, 0.008, 0.400)`.
- Pixel audit after darker mask-only changes: failed visual intent. Sampled player/enemy body pixels were unchanged versus the no-tint capture, so the current mask art/path does not visibly recolor soldiers enough.
- Visible tint follow-up: passed runtime capture. Actual M01 flow capture `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_visible_body_tint_1920x1080.png`; log `/private/tmp/warlinecapture-m01-game-flow-v29-visible-body-tint.log` shows player body material at `RGBA(0.260, 0.310, 0.430, 1.000)` and enemy body material at `RGBA(0.430, 0.240, 0.220, 1.000)`.
- Closest-direction follow-up: passed runtime capture. Actual M01 flow capture `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_player_closest_direction_1920x1080.png`; log `/private/tmp/warlinecapture-m01-game-flow-v29-player-closest-direction.log` shows player frames now use `idle_v29_shared_mask.screen_locked_D.*` and enemies remain `idle_v29_shared_mask.screen_locked_B.*`.
- Runtime captures produced:
  - `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_mask_1920x1080.png`
  - `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_mask_20x9_2400x1080.png`
  - `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_mask_21x9_2520x1080.png`
  - `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_background_refreshfit_1920x1080.png`
  - `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_background_refreshfit_20x9_2400x1080.png`
  - `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_background_refreshfit_21x9_2520x1080.png`
- Background comparison proof:
  - `Design/AgentReports/Captures/M01_V29_Background_ArtVsRuntime_RefreshFit_Comparison.png`
  - `Design/AgentReports/Captures/M01_V29_Background_RefreshFit_AspectProof.png`
- No solid-fill surround is visible in the refreshed 16:9, 20:9, or 21:9 captures. The map plate now fills the full render frame behind the HUD.

## Known gaps

- Final visual approval remains PM/user-owned.
- User rejected the first background proof because runtime did not match the art background. Gameplay confirmed the Art source was clean and fixed the runtime mapping/capture refresh path. Remaining visual review is about composition, scale, and HUD overlap, not a solid-fill background blocker.
- Art blocker for final soldier direction quality: V29 lacks diagonal soldier direction frames. Gameplay can only choose among up, down, left, and right. PM should request Art/Atlas to deliver all four diagonal directions for the shared body atlas and matching faction mask atlas: up-right, up-left, down-right, and down-left, with identical rects/pivots/UVs between body and mask.
- Current Gameplay fallback uses the closest available player direction (`screen_locked_D`, right) because the enemy is up-right from the player group. This is not final visual approval; it is a temporary best-fit until Art provides diagonal frames.
- The visible body tint is a temporary Gameplay fallback because mask-only tint does not visibly affect the current assets enough. Final preferred solution remains Art-provided faction-readable assets/masks that do not require whole-body material tint.
- Unity logs still emit a non-blocking editor `UnityEditor.Search.SearchDatabase` `ArgumentOutOfRangeException` during startup. It appears before gameplay flow and did not block compile, tests, capture, or runtime rendering.
- Unity logs still emit the existing SRP warning about `Camera.RemoveCommandBuffer` after capture. The PNGs were written and the capture commands exited successfully.

## Cross-lane impacts

- Art/Atlas: V29 shared body atlas, mask atlas, and overscan background are now bound in runtime. No new art blocker from Gameplay in this pass.
- UI: HUD/canvas target-lock remains UI-owned and was not modified.
- QA: Ready for PM review before QA routing.

## Next recommended task

PM/user visual review of the three V29 runtime captures, then route to QA if accepted.
