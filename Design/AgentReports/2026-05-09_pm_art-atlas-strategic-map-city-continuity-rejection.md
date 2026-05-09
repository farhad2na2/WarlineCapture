# PM Art/Atlas Strategic Map City Continuity Rejection

## Lane

PM

## Task

Reject the regenerated strategic/base-layout map because it changed the approved city-like strategic direction into a closed walled compound.

## Files changed

- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/art-atlas_pm_message.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/README.md`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/README.md`
- `Design/AgentReports/2026-05-09_pm_art-atlas-strategic-map-city-continuity-rejection.md`

## Contracts touched

- `Design/AgentReports/2026-05-09_art-atlas_m01-ai-production-asset-pack.md`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Strategic/m01_isometric_strategic_background.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_StrategicMap_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_TacticalMap_Target.png`

## User-visible behavior

No runtime behavior changed. Gameplay and QA/HCI remain blocked from consuming the current Art/Atlas asset pack.

## Validation run

- Opened the current strategic/background PNG.
- Compared it against the user's latest feedback and prior PM routing.
- Confirmed the image uses a closed walled compound/base concept instead of the previous city-like strategic map direction.
- Updated Art/Atlas instructions to require the same city-like map language expanded to a larger area, with reserved zones integrated into open urban roads/city blocks.
- Updated Gameplay and QA/HCI wait states so the current Art/Atlas handoff is not treated as accepted.

## Validation result

Needs fixes. The larger-map request was about area coverage and reserved spaces, not a concept change to a walled base.

## Known gaps

- Art/Atlas must regenerate only the strategic/base-layout background and overlay unless other user feedback arrives.
- The generated asset pack remains unaccepted.

## Cross-lane impacts

- Art/Atlas owns the correction.
- Gameplay must not wire the current strategic/background map.
- QA/HCI must reject any strategic/background map that reads as a closed compound, fortress, island base, or isolated military installation.

## Next recommended task

Art/Atlas should regenerate `m01_isometric_strategic_background.png` as a larger city-like strategic map: same urban-road-grid/city-block language, more area, broad reserved zones for refinery, tents, vehicles, command/support, staging, perimeter/defense, and an annotated overlay proving those zones.
