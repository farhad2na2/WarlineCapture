# M01 Approved Isometric Gameplay VisualLock Manifest

Date: 2026-05-08
Owner: Art/Atlas
Status: ready for PM/user review

## Source Of Truth

Approved target package:

- `Design/AgentReports/2026-05-08_art-atlas_m01-aaa-isometric-gameplay-visual-target-package.md`

Approved reference images:

- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Gameplay_Target.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Isometric_Grid_Proof.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_AAA_Isometric_AI_Source.png`

User approval:

- The approved true-isometric package is the reference quality for image, background, map, soldiers, markers, strategic/tactical maps, and atlases.

## Locked Files And Usage Rules

- `VL_M01_TacticalMap_Target.png`
  - Runtime usage: primary M01 tactical gameplay map/background quality reference.
  - Rule: runtime tactical captures must preserve the same true-isometric camera, material density, lighting, road/building treatment, and tactical readability.

- `VL_M01_TacticalMap_GridProof.png`
  - Runtime usage: camera/projection comparison reference.
  - Rule: Gameplay captures must show parallel isometric ground-plane axes with no horizon, vanishing point, wide-angle distortion, or cinematic perspective convergence.

- `VL_M01_StrategicMap_Target.png`
  - Runtime usage: strategic map, tactical overview, and minimap visual style reference.
  - Rule: strategic/map views should reuse the same dark isometric city material language with cyan, amber, green, and red tactical symbols.

- `VL_M01_MapTiles_RoadSidewalkBuildings.png`
  - Runtime usage: road, sidewalk, curb, wall, building, debris, roof, fire, and damage material reference.
  - Rule: tile/map assets must match the approved texture density, lighting, damage language, and orthographic isometric angle.

- `VL_M01_PlayerRifleSquad_Atlas_Target.png`
  - Runtime usage: player rifle squad unit atlas style reference.
  - Rule: friendly soldier atlas frames must preserve small RTS scale, coherent armor material, blue/cyan affiliation, consistent lighting, and grounded feet.

- `VL_M01_EnemyPatrol_Atlas_Target.png`
  - Runtime usage: enemy patrol unit atlas style reference.
  - Rule: enemy atlas frames must match friendly scale and lighting while using restrained hostile red markers or accents; do not over-tint into red blobs.

- `VL_M01_Atlas_StateGuide_IdleRunAimFireDeathDestroyed.png`
  - Runtime usage: idle, run, aim, fire, death, and destroyed atlas state style guide.
  - Rule: alive states must keep standing/running/aiming silhouettes and grounded foot contact; death/destroyed states must not be sampled for alive patrol or movement.

- `VL_M01_Markers_SelectionMoveAttackEnemyObjectiveHover.png`
  - Runtime usage: selection, move, attack, enemy, objective, and hover marker family.
  - Rule: markers must be ground-plane tactical feedback aligned to the isometric axes; reject yellow squares, huge green markers, filled blobs, and screen-space icons pretending to be ground FX.

- `VL_M01_ScaleGrounding_Rules.png`
  - Runtime usage: scale and grounding capture-comparison board.
  - Rule: soldiers, doors, roads, buildings, walls, and markers must share one orthographic isometric scale; reject floating, half-buried, squashed, or perspective-rescaled units.

## Global Runtime Acceptance Rules

- Gameplay implementation must compare captures against both this VisualLock package and `Design/VisualTargets/Gameplay/M01_SelectedReadability/`.
- The approved true-isometric style is required for image, background, tactical map, strategic map, soldiers, enemies, atlas states, and markers.
- The VisualLock package is a visual reference and approval gate, not a runtime implementation.
- Downstream lanes should not lower the quality bar to fit current assets; runtime art should be adjusted until it matches this lock.

## Rejected Cases

Do not accept runtime visuals with:

- non-isometric camera or perspective convergence,
- low-detail placeholder map/background,
- inconsistent soldier size or lighting,
- half-buried, floating, squashed, or cut-off soldiers,
- yellow square selection,
- giant green marker,
- red sitting/death artifact used as an alive enemy,
- marker FX that covers units or detaches from the ground plane.

## Related Sequence Lock

The exact M01 step-by-step gameplay mockup lock is defined in:

- `Design/VisualLock/GamePlay/M01_StepByStepGameplayMockups/README.md`
- `Design/VisualLock/GamePlay/M01_StepByStepGameplayMockups/M01_StepByStepGameplayMockup_Manifest.json`

Use it with this art VisualLock to validate runtime frame order, user actions, bridge calls, command feedback, ARIA behavior, objective completion, and result flow.
