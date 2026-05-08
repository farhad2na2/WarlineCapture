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

Do not make smaller soldiers, smaller buildings, different building designs, or different soldier styles. The production assets must be the approved visual family turned into usable assets, not a new interpretation.

Create real production PNG assets:

- big zoomed-out strategic/background map matching the approved `M01_SelectedReadability_*` isometric reference style; do not generate Tehran,
- all M01 zoomed-in tactical map plates,
- high-quality transparent marker PNGs,
- player rifle squad sprite atlas frames,
- enemy patrol sprite atlas frames,
- all required building PNG atlas states,
- manifests with asset ids, paths, import usage, scale anchors, contact-shadow rules, and prompt/source notes.

Include short user review steps. Do not commit or push.
