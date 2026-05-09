# PM Art/Atlas Strategic Map Area Rejection

## Lane

PM

## Task

Record the user's rejection of the produced zoomed-out strategic/base-layout map because it covers too small a usable area for the intended base layout.

## Files changed

- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/art-atlas_pm_message.md`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/README.md`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/README.md`
- `Design/AgentReports/2026-05-09_pm_art-atlas-strategic-map-area-rejection.md`

## Contracts touched

- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_TacticalMap_Target.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Strategic/m01_isometric_strategic_background.png`

## User-visible behavior

No runtime behavior changed. The current strategic/background map direction is rejected until it clearly supports the required base layout.

## Validation run

- Opened `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Strategic/m01_isometric_strategic_background.png`.
- Compared it against the user's requested usage: refinery, soldier tents, soldier vehicles area, command/support, staging, roads, and perimeter space.
- Updated Art/Atlas instructions with explicit base-layout acceptance rules and an annotated overlay requirement.

## Validation result

Needs fixes. The produced strategic/background image reads as a dense small-lot map and does not clearly reserve enough contiguous operational space for the required separate base assets.

## Known gaps

- Art/Atlas must regenerate the strategic/base-layout background.
- Art/Atlas must include an annotated review overlay/contact sheet naming the intended placement zones.

## Cross-lane impacts

- Gameplay must not wire the current strategic/background map as final.
- QA/HCI must reject the strategic/background map if the placement zones are not obvious before separate assets are placed.

## Next recommended task

Art/Atlas should regenerate `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Strategic/m01_isometric_strategic_background.png` as a larger operational base-layout foundation, plus a review overlay labeling refinery/fuel, tents/camp, vehicle motor pool, command/support, staging/training, perimeter/defense, and roads.
