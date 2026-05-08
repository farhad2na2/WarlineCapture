# PM Art/Atlas VisualLock Source Correction

## Lane

PM

## Task

Correct the Art/Atlas AI production asset task after the user stopped a second non-matching generation attempt.

## Files changed

- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/art-atlas_pm_message.md`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/README.md`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/README.md`
- `Design/AgentReports/2026-05-09_pm_art-atlas-visual-lock-source-correction.md`

## Contracts touched

- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_TacticalMap_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_GameplayVisualLock_Manifest.md`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_PlayerRifleSquad_Atlas_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_EnemyPatrol_Atlas_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_Atlas_StateGuide_IdleRunAimFireDeathDestroyed.png`

## User-visible behavior

No runtime behavior changed. The Art/Atlas lane is corrected so production assets must match the approved VisualLock tactical map target instead of drifting to a different visual family.

## Validation run

- Located `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_TacticalMap_Target.png`.
- Read the VisualLock manifest and README.
- Confirmed the prior active Art/Atlas task referenced selected-readability files but did not make `VL_M01_TacticalMap_Target.png` the primary production source of truth.
- Updated Art/Atlas instructions to reject mismatched soldier rotations, building styles, faction-combined atlases, incomplete direction/state animation sets, and strategic maps with finished buildings baked in.

## Validation result

Needs correction from prior PM routing, now fixed in active task files.

## Known gaps

- Art/Atlas has not yet produced the corrected production asset pack.

## Cross-lane impacts

- Art/Atlas must restart from the corrected source lock.
- Gameplay remains blocked until the corrected asset pack lands.
- QA/HCI must reject any future asset pack that does not visibly match `VL_M01_TacticalMap_Target.png`.

## Next recommended task

Art/Atlas should produce `Design/AgentReports/2026-05-09_art-atlas_m01-ai-production-asset-pack.md` with assets that match `VL_M01_TacticalMap_Target.png` and complete separate player/enemy directional animation atlases.
