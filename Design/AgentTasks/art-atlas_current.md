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

- Do not generate a Tehran map. The user stopped that direction.
- The approved `M01_SelectedReadability_*` visual target is now the source style for the world/background/map direction.
- Need a big zoomed-out strategic/background map in this approved isometric style, not a real Tehran replacement.
- Zoom level, camera angle, map density, visual scale, marker footprint, soldier/building proportions, background treatment, and composition must follow the previously approved reference package. Do not invent a new zoom level or camera.
- Need all zoomed-in tactical maps.
- Need high-quality marker PNGs.
- Need these soldiers as actual sprites / sprite atlas frames.
- Need all buildings as high-quality PNG atlases.
- No smaller soldiers, no smaller buildings, no different building designs, and no different soldier styles than the approved reference package.
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
- Do not reinterpret the approved visual family:
  - no smaller soldiers than the approved reference,
  - no smaller buildings than the approved reference,
  - no different building designs/style family,
  - no different soldier art style, outfit family, pose language, or proportions,
  - no mixed soldier styles inside the same squad,
  - no alternate map/building/soldier family just because it is also high quality.
- Match the approved reference package for zoom/camera/readability, not just the theme:
  - `M01_SelectedReadability_Gameplay_Target.png` for overall gameplay zoom, background density, and composition,
  - `M01_SelectedReadability_Isometric_Grid_Proof.png` for isometric camera/parallel axes,
  - `M01_SelectedReadability_Scale_Board.png` for soldier, road, door, and building proportions,
  - `M01_SelectedReadability_Selected_Marker_Target.png` for selected marker size and ground contact,
  - `M01_SelectedReadability_Move_Attack_Marker_Target.png` for move/attack marker footprint and restraint,
  - `M01_SelectedReadability_Enemy_Readability_Target.png` for hostile readability and red feedback restraint,
  - `M01_SelectedReadability_Idle_Run_Pose_Guide.png` for unit pose/contact direction.
- Keep maps, soldiers, buildings, markers, and atlases consistent with one lighting/material/perspective language.
- Use ready-to-import transparent PNGs for sprites/markers/atlases.
- Use POT-padded Unity-ready PNGs for tactical map plates when required by `WarlineCapture_Tactical_Map_AI_Workflow.md`.
- Do not bake gameplay units, markers, labels, UI, or approval annotations into clean tactical ground plates. Buildings/props needed by the scene must be supplied as separate atlas assets with anchors unless a specific map plate explicitly requires static background structures.

## Required Outputs

### Strategic Map

- Big zoomed-out strategic/background map in the approved isometric gameplay style.
- Do not use Tehran as the subject, layout, or replacement target.
- Use the approved visual target/background language as the reference for composition, lighting, roads, terrain, buildings, scale, camera, and zoom level.
- Runtime path: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Strategic/m01_isometric_strategic_background.png`
- Include review mirror/contact sheet.

### Tactical Maps

- All required M01 zoomed-in tactical map plates.
- Minimum set until contract says otherwise: three close-up tactical plates.
- Tactical map zoom/crop must follow the approved gameplay target and scale board. Do not create a more zoomed-out strategic view or a more zoomed-in character-art view unless the approved reference already supports it.
- Tactical maps must preserve the approved building size/design language. Do not replace the buildings with a different architecture family or shrink them to fit more content.
- Each plate needs native AI source and Unity-ready POT-padded PNG.
- Follow `Design/WarlineCapture_Tactical_Map_AI_Workflow.md`.
- Clean tactical plate outputs must exclude soldiers, vehicles, markers, UI, labels, and approval notes.

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
- Soldier frames must match the approved soldier style, proportions, scale intent, lighting, and pose language. Do not create smaller soldiers, different outfits, different proportions, or a different art family.
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
- Building frames must match the approved building size, door/road scale relationship, lighting, and architecture style. Do not create smaller buildings or a different building family.
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
