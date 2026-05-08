# PM Message For Art/Atlas

Date: 2026-05-09

The Gameplay VisualLock board package is rejected as insufficient. The user needs ready-to-use implementation assets.

Do not create more deterministic review boards, simple vector markers, placeholder crops, or low-detail diagrams.

Expected report:

`Design/AgentReports/2026-05-09_art-atlas_m01-ai-production-asset-pack.md`

Use the existing production workflows:

- `Design/WarlineCapture_Tactical_Map_AI_Workflow.md`
- `Design/WarlineCapture_Art_Asset_Requirements_Register.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/WarlineCapture_Chapter01_Tactical_Production_Implementation_Plan.md`
- `Design/VisualReferences/2DIsometricProduction/GoldenAssets/README.md`

Required output folders:

- Runtime assets: `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/`
- Review mirror: `Design/VisualLock/Gameplay/M01_AIProductionAssets/`

Hard requirement: all visual assets must be AI-generated or AI-assisted at high quality. No deterministic placeholder/vector/board filler.

Reference lock: zoom level, camera angle, composition, background density, soldier/building scale, marker footprint, and visual style must follow the approved `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_*` reference package. Do not invent a new zoom level, camera, Tehran map, or replacement city direction.

Production source of truth: `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_TacticalMap_Target.png`. The generated assets must match that image's style, camera, lighting, rotations, building language, soldier style, and scale. Use the rest of `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/` as locked references. Do not produce assets that merely look generally isometric or generally high quality.

Do not make smaller soldiers, smaller buildings, different building designs, or different soldier styles. The production assets must be the approved visual family turned into usable assets, not a new interpretation.

Create real production PNG assets:

- big zoomed-out strategic/base-layout background matching `VL_M01_TacticalMap_Target.png`; no Tehran, no finished buildings baked in, more area than the tactical target, with clear placement zones for later separate soldier tents, vehicles, refinery/fuel module, command/support structures, roads, pads, and staging,
- all M01 zoomed-in tactical map plates,
- high-quality transparent marker PNGs,
- player rifle squad sprite atlas frames matching `VL_M01_PlayerRifleSquad_Atlas_Target.png`,
- enemy patrol sprite atlas frames matching `VL_M01_EnemyPatrol_Atlas_Target.png`,
- all required building PNG atlas states,
- manifests with asset ids, paths, import usage, scale anchors, contact-shadow rules, and prompt/source notes.

Atlas rules: do not combine player and enemy factions in one atlas. Every unit atlas must include complete idle, run, aim, shoot/fire, hit/damaged, and die/death animation frames for every required facing direction. Reject partial direction sets or frames angled differently from the approved target.

Include short user review steps. Do not commit or push.
