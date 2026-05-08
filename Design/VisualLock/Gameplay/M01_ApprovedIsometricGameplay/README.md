# M01 Approved Isometric Gameplay VisualLock

## Purpose

This folder is the gameplay VisualLock package for the approved M01 true-isometric AAA art direction.

Use it to lock all visual item families before runtime implementation:

- strategic map,
- tactical map/background,
- road, sidewalk, building, and map tile treatment,
- player rifle squad atlases,
- enemy patrol atlases,
- idle/run/aim/fire/death/destroyed atlas states,
- selection, move, attack, enemy, objective, and hover markers,
- scale and grounding rules.

## Source Approval

Approved source package:

- `Design/AgentReports/2026-05-08_art-atlas_m01-aaa-isometric-gameplay-visual-target-package.md`

Approved quality reference:

- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Gameplay_Target.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Isometric_Grid_Proof.png`

User approval:

- The user approved this quality/style and wants the image, background, map, soldiers, markers, strategic/tactical maps, and atlases to follow this high-quality direction.

## Required VisualLock Files

Art/Atlas should create or update this folder with locked references for:

- `VL_M01_TacticalMap_Target.png`
- `VL_M01_TacticalMap_GridProof.png`
- `VL_M01_StrategicMap_Target.png`
- `VL_M01_MapTiles_RoadSidewalkBuildings.png`
- `VL_M01_PlayerRifleSquad_Atlas_Target.png`
- `VL_M01_EnemyPatrol_Atlas_Target.png`
- `VL_M01_Atlas_StateGuide_IdleRunAimFireDeathDestroyed.png`
- `VL_M01_Markers_SelectionMoveAttackEnemyObjectiveHover.png`
- `VL_M01_ScaleGrounding_Rules.png`
- `VL_M01_GameplayVisualLock_Manifest.md`

## Runtime Rule

Gameplay must not treat the single approved scene target as enough for implementation. Runtime work should wait until this VisualLock package exists and then compare captures against both:

- the approved scene target package under `Design/VisualTargets/Gameplay/M01_SelectedReadability/`,
- this gameplay VisualLock package.
