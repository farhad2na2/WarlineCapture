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

- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_TacticalMap_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_GameplayVisualLock_Manifest.md`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_StrategicMap_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_PlayerRifleSquad_Atlas_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_EnemyPatrol_Atlas_Target.png`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_Atlas_StateGuide_IdleRunAimFireDeathDestroyed.png`
- `Design/AgentReports/2026-05-08_art-atlas_m01-aaa-isometric-gameplay-visual-target-package.md`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Gameplay_Target.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Isometric_Grid_Proof.png`

Primary production target:

- `VL_M01_TacticalMap_Target.png` is the approved tactical production target. Every close tactical map, soldier, building, marker, lighting choice, camera angle, rotation, and visible material family must match it.
- The selected-readability package is supporting evidence. It does not override `VL_M01_TacticalMap_Target.png`.

Latest user correction:

- Do not generate a Tehran map. The user stopped that direction.
- The approved `M01_SelectedReadability_*` visual target is now the source style for the world/background/map direction.
- Need a big zoomed-out strategic/background map in this approved isometric style, not a real Tehran replacement.
- The strategic/background map must preserve the previous city-like strategic map language. The user asked to cover a bigger city-like area, not to switch to a closed walled compound/base.
- Do not generate a fortress, enclosed compound, perimeter-walled base, island base, or isolated military installation as the strategic map.
- Keep the open city/urban-road-grid feel from the prior strategic/city-like direction: public roads, city blocks, damaged urban fabric, open industrial lots, and reserved build/staging spaces integrated into the city map.
- Zoom level, camera angle, map density, visual scale, marker footprint, soldier/building proportions, background treatment, and composition must follow the previously approved reference package. Do not invent a new zoom level or camera.
- The close tactical zoom must match `VL_M01_TacticalMap_Target.png`.
- The zoomed-out strategic/base-layout map must cover more area than the tactical target, contain no finished buildings baked into the map, and provide readable empty placement zones for later separate tents, vehicles, refinery/fuel module, command/support structures, roads, pads, and unit staging.
- The produced `m01_isometric_strategic_background.png` direction is rejected if it is a dense small-block map. The user needs a much larger base-layout area, not many small lots.
- The later produced `m01_isometric_strategic_background.png` direction is also rejected if it becomes a closed walled compound. Bigger area must be achieved by expanding the city-like map, not by switching concept.
- The zoomed-out base-layout must include enough contiguous open area for a refinery/fuel module, soldier tents/camp, soldier vehicle motor-pool area, command/support pad, staging/training area, road/service lanes, and defensive/perimeter space.
- Need all zoomed-in tactical maps.
- Need high-quality marker PNGs.
- Need these soldiers as actual sprites / sprite atlas frames.
- Need all buildings as high-quality PNG atlases.
- No smaller soldiers, no smaller buildings, no different building designs, and no different soldier styles than the approved reference package.
- No soldier rotation/facing drift from the approved target. Output frames must use consistent isometric facings that line up with the target camera and grid axes.
- Do not combine player and enemy/faction variants into one mixed atlas. Player rifle squad and enemy patrol must be separate sheets/manifests.
- Each unit atlas must contain complete animation frames for every required facing/direction for idle, run, aim, shoot/fire, hit/damaged, and die/death.
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
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/README.md`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_GameplayVisualLock_Manifest.md`
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
  - no mixed player/enemy faction content in one atlas sheet,
  - no soldiers angled or rotated differently from the approved target camera,
  - no alternate map/building/soldier family just because it is also high quality.
- Match the approved reference package for zoom/camera/readability, not just the theme:
  - `VL_M01_TacticalMap_Target.png` for the primary close tactical map, visual family, camera angle, building style, soldier style, and runtime target match,
  - `VL_M01_StrategicMap_Target.png` for the broader strategic/base-layout mood only; the new zoomed-out base-layout asset must cover more playable area and reserve placement zones without finished buildings baked in,
  - `VL_M01_PlayerRifleSquad_Atlas_Target.png` for player soldier style only,
  - `VL_M01_EnemyPatrol_Atlas_Target.png` for enemy soldier style only,
  - `VL_M01_Atlas_StateGuide_IdleRunAimFireDeathDestroyed.png` for animation-state coverage and pose language,
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

- Big zoomed-out strategic/base-layout background map in the approved isometric gameplay style.
- Do not use Tehran as the subject, layout, or replacement target.
- Preserve the previous strategic/city-like map direction while covering a larger area.
- Do not switch to a closed walled compound, fortress, island base, or isolated military-base map.
- Keep the map open and city-like: roads should continue through/around the scene, city blocks should remain part of the composition, and reserved spaces should feel like urban lots/industrial yards/staging areas inside the city fabric.
- Cover more area than `VL_M01_TacticalMap_Target.png`.
- Do not bake finished buildings, destroyed buildings, building shells, refinery art, tents, vehicles, or command buildings into the reserved placement zones.
- Do not make a dense grid of small lots, and do not replace the city-like map with one huge enclosed pad complex. The map must be a larger city-like operational area with broad, contiguous reserved zones.
- Minimum layout requirement:
  - one large refinery/fuel-module zone that can visibly fit a separate refinery/fuel module asset plus service clearance,
  - one soldier tents/camp zone that can fit multiple separate tents,
  - one soldier vehicle motor-pool zone that can fit multiple separate vehicles and parking/service lanes,
  - one command/support zone,
  - one staging/training/open maneuver zone,
  - perimeter/defensive lanes and roads connecting the zones.
- At least three zones must be visibly larger than the largest M01 building footprint, not tiny pads.
- The user should be able to point at the image and say where refinery, tents, vehicles, command/support, and staging will go before any separate assets are placed.
- Use `VL_M01_TacticalMap_Target.png` for style, material quality, camera language, and lighting; use `VL_M01_StrategicMap_Target.png` only as overview mood/supporting reference.
- Runtime path: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Strategic/m01_isometric_strategic_background.png`
- Include a review mirror and an annotated placement-zone overlay/contact sheet naming the refinery, tents/camp, vehicle motor pool, command/support, staging, perimeter/defense, roads, and the city-block continuity.

### Tactical Maps

- All required M01 zoomed-in tactical map plates.
- Minimum set until contract says otherwise: three close-up tactical plates.
- Tactical map zoom/crop must follow `VL_M01_TacticalMap_Target.png`. Do not create a more zoomed-out strategic view or a more zoomed-in character-art view.
- Tactical maps must preserve the approved building size/design language from `VL_M01_TacticalMap_Target.png` and `VL_M01_MapTiles_RoadSidewalkBuildings.png`. Do not replace the buildings with a different architecture family or shrink them to fit more content.
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
- Player rifle squad frames must match `VL_M01_PlayerRifleSquad_Atlas_Target.png`.
- Enemy patrol frames must match `VL_M01_EnemyPatrol_Atlas_Target.png`.
- Soldier frames must match the approved soldier style, proportions, scale intent, lighting, facing/rotation, and pose language. Do not create smaller soldiers, different outfits, different proportions, or a different art family.
- Do not combine player and enemy patrol/faction frames on the same atlas sheet. Use separate atlas sheets and manifests.
- Required states:
  - idle
  - run
  - aim
  - fire
  - damaged
  - death / destroyed
- Required facing coverage: every state above must exist for every required gameplay-facing direction. If runtime currently uses four facings, all four facings are required; if the implementation contract requires eight facings, all eight are required. Do not hand off a partial single-angle or mismatched-rotation atlas.
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
