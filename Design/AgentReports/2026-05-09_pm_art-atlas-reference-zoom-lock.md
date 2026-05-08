# PM Art/Atlas Reference Zoom Lock

## Lane

PM

## Task

Clarify that the Art/Atlas AI production asset pack must follow the previously approved gameplay visual references for zoom level, camera, composition, background, scale, and marker footprint.

Latest user clarification also locks scale and style identity: no smaller soldiers, no smaller buildings, no different building designs, and no different soldier styles.

## Files changed

- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/art-atlas_pm_message.md`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/README.md`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/README.md`
- `Design/AgentReports/2026-05-09_pm_art-atlas-reference-zoom-lock.md`

## Contracts touched

- `Design/VisualTargets/Gameplay/M01_SelectedReadability/README.md`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Target_Manifest.md`

## User-visible behavior

No runtime behavior changed. Art/Atlas is now explicitly blocked from inventing a different zoom level, camera, city direction, or composition.

## Validation run

- Re-read the approved visual target README and manifest.
- Confirmed the approved package already defines gameplay zoom, background density, isometric camera proof, scale board, marker target, enemy readability, and pose/contact guidance.
- Updated the active Art/Atlas task and PM message to reference those files as the source of truth.

## Validation result

Passed for PM routing. The active task now says the production assets must follow the approved reference package for:

- gameplay zoom,
- isometric camera/parallel axes,
- background and map density,
- soldier/building/road/door scale,
- marker footprint,
- hostile readability,
- unit pose/contact,
- soldier/building visual identity and size family.

## Known gaps

- Art/Atlas still needs to produce the corrected AI production asset pack.

## Cross-lane impacts

- QA/HCI should reject any asset pack that changes the approved zoom/camera/style direction even if the assets are high quality.
- Gameplay remains blocked until the corrected asset pack lands.

## Next recommended task

Art/Atlas should create `Design/AgentReports/2026-05-09_art-atlas_m01-ai-production-asset-pack.md` with ready-to-use AI-generated PNG assets following the approved `M01_SelectedReadability_*` package exactly for visual direction.
