# Art/Atlas Current Task

Date: 2026-05-09
Status: active
Priority: P0 create AI-generated ready-to-implement M01 production asset pack

## Assignment

The user rejected the Gameplay VisualLock package as insufficient for implementation.

The failure is process-level: Art/Atlas produced reference/review boards, but the user asked for ready-to-use implementation assets. Do not continue with board-only VisualLock outputs.

Rejected / insufficient package:

- `Design/AgentReports/2026-05-08_art-atlas_m01-gameplay-visual-lock-package.md`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_GameplayVisualLock_Manifest.md`

Approved quality reference remains:

- `Design/AgentReports/2026-05-08_art-atlas_m01-aaa-isometric-gameplay-visual-target-package.md`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Gameplay_Target.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Isometric_Grid_Proof.png`

Latest user correction:

- Need a big zoomed-out Tehran strategic map in this style.
- Need all zoomed-in tactical maps.
- Need high-quality marker PNGs.
- Need these soldiers as actual sprites / sprite atlas frames.
- Need all buildings as high-quality PNG atlases.
- Need ready-to-use implementation assets, not reference boards.
- Assets must be AI-generated high quality, not deterministic placeholder/vector/diagram output.

Read first:

- `Design/AgentReports/2026-05-09_pm_art-atlas-ai-production-asset-pack-routing.md`
- `Design/AgentTasks/art-atlas_pm_message.md`
- `Design/AgentTasks/user_feedback_review_gate.md`
- `Design/WarlineCapture_Tactical_Map_AI_Workflow.md`
- `Design/WarlineCapture_Art_Asset_Requirements_Register.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/WarlineCapture_Chapter01_Tactical_Production_Implementation_Plan.md`
- `Design/VisualReferences/2DIsometricProduction/GoldenAssets/README.md`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/README.md`

## Required Runtime Asset Folder

Create the ready-to-implement asset pack under:

`Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/`

Create the review mirror under:

`Design/VisualLock/Gameplay/M01_AIProductionAssets/`

The review mirror is only for user/PM review. Runtime-consumable PNGs and manifests must exist under the `Assets/Game/Art/Generated/...` folder.

## Hard Quality Rules

- All visuals must be AI-generated or AI-assisted at high production quality.
- Do not use deterministic vector drawings, simple geometric marker boards, placeholder crops, stretched/upscaled source images, low-detail diagrams, or board-only mockups.
- Keep the approved true-isometric AAA quality/style.
- Keep maps, soldiers, buildings, markers, and atlases consistent with one lighting/material/perspective language.
- Use ready-to-import transparent PNGs for sprites/markers/atlases.
- Use POT-padded Unity-ready PNGs for tactical map plates when required by `WarlineCapture_Tactical_Map_AI_Workflow.md`.
- Do not bake gameplay units/buildings into clean tactical ground plates.

## Required Outputs

### Strategic Map

- Big zoomed-out Tehran strategic map in the approved isometric style.
- Runtime path: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Strategic/tehran_zoomed_out_map.png`
- Include review mirror/contact sheet.

### Tactical Maps

- All required M01 zoomed-in tactical map plates.
- Minimum set until contract says otherwise: three close-up tactical plates.
- Each plate needs native AI source and Unity-ready POT-padded PNG.
- Follow `Design/WarlineCapture_Tactical_Map_AI_Workflow.md`.
- Clean tactical ground only: no soldiers, no vehicles, no buildings, no UI, no labels.

### Markers

- High-quality transparent PNG marker sprites.
- Required marker ids:
  - `marker.selection.ring`
  - `marker.move.destination`
  - `marker.attack.target`
  - `marker.enemy.readability`
  - `marker.objective.focus`
  - `marker.hover.preview`
  - `marker.invalid.blocked`
- Include individual PNGs, atlas sheet, and manifest.

### Soldier Atlases

- Player rifle squad transparent sprite atlas frames.
- Enemy patrol transparent sprite atlas frames.
- Required states:
  - idle
  - run
  - aim
  - fire
  - damaged
  - death / destroyed
- Must be actual sprite frames, not a screenshot board.
- Must support ECS atlas-backed runtime; no SpriteRenderer/MeshRenderer placeholder acceptance.

### Building Atlases

- High-quality transparent PNG atlases for all required M01 buildings/props.
- Required states:
  - intact
  - damaged
  - destroyed
- Destroyed visuals must be atlas states, not separate `Destroyed` child objects.

### Manifests

Include manifests with:

- stable asset id,
- runtime file path,
- review mirror path,
- intended Unity import type,
- atlas/state id,
- scale anchor,
- contact shadow rule,
- prompt/source notes proving AI-generated high-quality workflow,
- approval status.

## Waiting On

Waiting on lane:
none

Owner of next action:
Art/Atlas

Can my lane still continue fallback work? no. Produce the AI production asset pack first.

## Completion Report

Write:

`Design/AgentReports/2026-05-09_art-atlas_m01-ai-production-asset-pack.md`

Use the standard WarlineCapture handoff format. Include every runtime asset path, every review mirror path, manifest paths, generation/source notes, and short user review steps.
