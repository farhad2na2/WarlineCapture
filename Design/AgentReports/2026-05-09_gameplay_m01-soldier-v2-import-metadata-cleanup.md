# Lane

Gameplay

# Task

P0 make v2 soldier atlas import-ready before runtime integration.

# Files changed

- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/**/**/*_v2.png.meta`
  - Updated 226 v2 soldier PNG Unity meta files.
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest_v2.json`
  - Added runtime import policy, atlas layout policy, per-sequence anchors, and per-frame bounds metadata.
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest_v2.md`
  - Added the gameplay runtime import policy summary.
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest_v2.json.meta`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest_v2.md.meta`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png`
- `Design/AgentReports/2026-05-09_gameplay_m01-soldier-v2-import-metadata-cleanup.md`

# Contracts touched

- V2 soldier runtime PNG import contract:
  - Atlases remain separate by faction: player rifle squad and enemy patrol.
  - Atlas layout remains `4096x1792`, 16 columns, 256x256 cells.
  - Atlas import policy: Default texture, mipmaps disabled, alpha enabled, NPOT scaling disabled, clamp wrap, bilinear filter, uncompressed, max size 4096, Android/iOS overrides enabled.
  - Frame import policy: Sprite texture, single sprite mode, mipmaps disabled, alpha enabled, NPOT scaling disabled, clamp wrap, bilinear filter, uncompressed, max size 256, Android/iOS overrides enabled.
- V2 soldier manifest runtime contract:
  - Existing state, facing, frame order, fps, loop, atlas rect, and source references preserved.
  - Added `pivot_px`, `foot_anchor_px`, `pivot_normalized_unity_sprite`, and `contact_band_height_px` per sequence.
  - Added `pivot_px`, `foot_anchor_px`, `alpha_bounds_px`, `contact_bounds_px`, and `normalized_bounds` per frame.
- Atlas layout decision:
  - Keep current 4096x1792 faction atlases for import cleanup.
  - Do not repack before PM/user cleanup acceptance.
  - Edge bleed risk is handled by disabling mipmaps and forcing clamp wrap. Gutter/extrusion remains 0 by policy.
  - Repack trigger: only if PM/user requires mipmaps, QA detects edge bleed, or mobile memory profiling rejects the two-atlas layout.
- Runtime integration contract:
  - No v2 ECS/runtime gameplay integration performed.
  - V2 remains blocked from live gameplay until PM/user accepts this cleanup result.

# User-visible behavior

None intended. This cleanup prepares import metadata and manifests only. The public gameplay path still renders the current pre-v2 runtime presentation.

# Validation run

- Metadata audit:
  - `python3 -c '...'`
  - Checked 226 v2 PNG `.meta` files for mipmap disabled, alpha enabled, NPOT disabled, clamp wrap, and platform override markers.
  - Checked 224 manifest frames for pivot, foot anchor, alpha bounds, contact bounds, and normalized bounds.
- Unity import validation:
  - Workspace: `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
  - Command:
    - `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -logFile /private/tmp/warlinecapture-gameplay-v2-import-metadata-cleanup-import.log`
  - Log scan:
    - `rg -n "Aborting batchmode|error CS|Exception|fatal|Scripts have compiler errors" /private/tmp/warlinecapture-gameplay-v2-import-metadata-cleanup-import.log`
- Public runtime capture validation:
  - Workspace: `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
  - Command:
    - `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests.PublicCampaignLaunch_ReachesM01ProductionVisibleSlice -testResults /private/tmp/warlinecapture-gameplay-v2-atlas-audit-capture-results.xml -logFile /private/tmp/warlinecapture-gameplay-v2-atlas-audit-capture.log`
  - Capture paths:
    - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
    - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png`
- Text diff hygiene:
  - `git diff --check -- Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest_v2.json Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest_v2.md`

# Validation result

- Metadata audit passed:
  - `v2_png_metas 226`
  - `manifest_frames 224`
  - `meta_policy_problems 0`
  - `manifest_missing_metadata 0`
  - `mipmap_policy disabled for v2 import cleanup`
  - `repack_required_before_integration False`
- Unity import validation passed:
  - Unity batch import exited with code 0.
  - Focused log scan found no batch abort, C# compiler error, exception, fatal marker, or script compiler error marker.
- Public runtime capture validation passed for route accessibility:
  - The M01 public path reaches and captures the production visible slice.
  - The capture does not yet prove v2 soldier runtime quality because v2 integration is intentionally not allowed before PM/user acceptance.
- Visual comparison against approved references:
  - Current runtime capture does not match `M01_SelectedReadability_Gameplay_Target.png` overall: the approved target is darker, grittier, and more ruined; the current runtime still has the pre-v2 clean map/readability presentation.
  - Current runtime capture does not match `M01_SelectedReadability_Isometric_Grid_Proof.png` as a finished style target; it is isometric enough for route validation, but it is not using the approved target composition or overlay proof.
  - Current runtime soldiers do not match the approved v2 AI production soldier style because v2 soldiers are not integrated.
  - Current runtime markers do not match the approved marker target style because the approved marker assets are not integrated here.
  - The approved AI production strategic background, v2 soldier atlases, and marker/building assets remain import-ready assets awaiting PM/user acceptance and later runtime integration.

# Known gaps

- No live ECS/runtime v2 soldier integration was performed by design.
- No v2 soldier runtime playback capture exists yet.
- No QA pass has validated v2 soldier animation continuity inside the live gameplay scene.
- The current atlas policy uses no gutter/extrusion. Bleeding risk is mitigated through mipmaps disabled and clamp wrap; Art/Atlas should repack with padding only if QA detects edge bleed or mobile profiling rejects the current layout.
- The two 4096x1792 atlases are import-ready, but mobile memory cost still needs profiling after integration.

# Cross-lane impacts

- PM/user can now review the import cleanup result and decide whether v2 is accepted for Gameplay runtime integration.
- Art/Atlas does not need to act unless PM/user requests a padded/POT repack, QA detects bleed, or mobile profiling rejects the two-atlas layout.
- QA/HCI should wait for PM/user acceptance and Gameplay runtime integration before validating v2 in-scene readability, markers, and animation continuity.
- UI has no new dependency from this cleanup.

# Next recommended task

PM/user should accept or reject the v2 import-readiness cleanup. If accepted, route Gameplay to integrate the v2 soldier atlas and manifest into the M01 ECS sprite animation runtime, then capture the selected-readability runtime view again for QA/HCI comparison against the approved target package.
