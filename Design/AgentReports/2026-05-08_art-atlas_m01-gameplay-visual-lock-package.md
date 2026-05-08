# Art/Atlas M01 Gameplay VisualLock Package

## Lane

Art/Atlas

## Task

Create a Gameplay VisualLock package from the approved true-isometric M01 reference before Gameplay runtime implementation resumes.

## Files changed

- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_TacticalMap_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_TacticalMap_GridProof.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_StrategicMap_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_MapTiles_RoadSidewalkBuildings.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_PlayerRifleSquad_Atlas_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_EnemyPatrol_Atlas_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_Atlas_StateGuide_IdleRunAimFireDeathDestroyed.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_Markers_SelectionMoveAttackEnemyObjectiveHover.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_ScaleGrounding_Rules.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_GameplayVisualLock_Manifest.md`
- `Design/AgentReports/2026-05-08_art-atlas_m01-gameplay-visual-lock-package.md`

## Contracts touched

- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/README.md`
- `Design/AgentReports/2026-05-08_pm_art-atlas-isometric-visual-target-approved.md`
- `Design/AgentReports/2026-05-08_art-atlas_m01-aaa-isometric-gameplay-visual-target-package.md`

## User-visible behavior

No runtime behavior changed. The approved true-isometric M01 art direction is now split into locked gameplay reference families for map/background, strategic map, tiles/buildings, player/enemy atlas style, state guidance, markers, and scale/grounding.

## Validation run

- Read the active Art/Atlas task, PM approval/routing report, PM Art/Atlas message, user feedback gate, gameplay VisualLock README, and approved true-isometric visual target references.
- Generated all required VisualLock PNGs under `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/`.
- Wrote `VL_M01_GameplayVisualLock_Manifest.md` with per-file downstream usage rules.
- Built and visually inspected `/private/tmp/m01_gameplay_visual_lock_contact.png`.

## Validation result

Ready for PM/user review.

The package locks:

- tactical isometric gameplay map/background,
- tactical map isometric grid proof,
- strategic map visual style,
- road/sidewalk/building/map tile treatment,
- player rifle squad atlas style,
- enemy patrol atlas style,
- idle/run/aim/fire/death/destroyed atlas state style,
- selection, move, attack, enemy, objective, and hover marker family,
- scale/grounding rules for soldiers, roads, buildings, and markers.

## QA acceptance checks

- Runtime captures must compare against both `Design/VisualTargets/Gameplay/M01_SelectedReadability/` and `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/`.
- Tactical/strategic maps must preserve true-isometric camera, parallel axes, and the approved material density.
- Soldier and enemy atlases must preserve consistent scale, lighting, silhouette, and foot contact.
- Alive state frames must not use death/destroyed/sitting artifacts.
- Markers must stay ground-plane aligned and avoid rejected cases: yellow square, huge green marker, filled blob, detached hover/target FX.
- Scale/grounding must reject floating, half-buried, squashed, or perspective-rescaled units.

## User Review Steps

1. Open `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_GameplayVisualLock_Manifest.md`.
2. Review the nine PNG lock files in the same folder.
3. Check whether the locks cover all requested item families: strategic/tactical maps, map/background/tiles, soldiers, enemies, atlas states, markers, and grounding rules.
4. Answer exactly `approve gameplay visual lock package` or `reject gameplay visual lock package with notes`.

## Known gaps

- This is a VisualLock/reference package, not runtime implementation.
- Gameplay should not resume implementation against the approved visual direction until PM/user approves this VisualLock package or PM explicitly routes otherwise.

## Cross-lane impacts

- Gameplay can use this as the pre-runtime visual lock once approved.
- QA/HCI should compare future runtime evidence against this package and the approved selected-readability target package.
- Designer, UI, and Support/FTUE remain unaffected unless Gameplay reports a concrete mismatch.

## Next recommended task

PM/user should approve or reject the Gameplay VisualLock package with notes.
