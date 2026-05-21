# PM M01 Gameplay Art Asset Resume Dispatch

Date: 2026-05-17
Lane: PM
Status: Art/Atlas dispatched; Gameplay held on Art assets

## Decision

Resume the M01 gameplay target-match pipeline by routing Art/Atlas first.

Gameplay should not continue implementation yet because the latest Gameplay v5 proof identified real Art-owned target-match blockers. Runtime tuning against the current background/soldier assets would continue the mismatch loop.

## Current Routing

Art/Atlas owns:

- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/art-atlas_pm_message.md`

Expected Art/Atlas report:

- `Design/AgentReports/2026-05-17_art-atlas_m01-gameplay-target-match-assets.md`

Gameplay is held:

- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/gameplay_pm_message.md`

UI continues separate SCN-02 main menu, SCN-08 match HUD, and POP-05 result work. This M01 Art dispatch does not change UI ownership.

## Source Assessment

Latest Gameplay checkpoint:

- `Design/AgentReports/2026-05-15_gameplay_m01-01-target-match-proof-v5.md`

Accepted progress from v5:

- normal Splash/Main Menu/Quick Custom/Match flow is preserved
- M01 launches through the existing designed route
- eight ECS/runtime soldiers render
- idle animation advancement is proven
- enemy readability/health overlays exist through ECS/runtime presentation
- `GameplayArchitectureContractTests` passed

Rejected as final visual match:

- exact approved no-HUD/no-unit M01-01 tactical background/source plate is still missing or not approved for final binding
- current soldier atlas does not match the target mockup angle, silhouette, scale, and baked contact-shadow treatment
- current runtime background can drift to old/substitute battlefield art
- Gameplay must not compensate with transform hacks, fake shadow quads, camera distortion, pasted mockup pixels, or a different map identity

## Art/Atlas Assignment

Art/Atlas must deliver a target-match M01 gameplay asset package under:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/`

Reference package:

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/`

Required assets:

- approved no-HUD/no-unit M01-01 tactical background/source plate matching `M01-01_TacticalStart_1920x1080.png` and `CameraLock_M01_DefaultStart.json`
- target-matched player rifle squad source frames/atlas with true-isometric angle, small RTS scale, grounded feet, transparent bounds, foot pivots, and baked contact shadows
- target-matched enemy patrol source frames/atlas at the same projection scale as the player squad, with restrained hostile readability
- runtime-ready marker/readability assets or metadata for M01-02 selected rings, selected squad shield/status bar, enemy red foot readability, and enemy segmented above-head health bars
- updated manifests/contact sheets and a Gameplay binding checklist with exact file paths, asset ids, frame keys, pivots, anchors, intended Unity destinations, and remaining non-Art blockers

## Rules

- Use imagegen for every replacement/final visual.
- Deterministic tooling is allowed only after imagegen source selection for cleanup, resizing, atlas/contact-sheet/manifest packaging, inspection, and validation.
- Do not use deterministic/programmatic final art, programmer placeholders, manual shape overlays, vector substitutes, target crops, pasted flattened mockups, screenshots, or contact sheets as runtime art.
- Do not modify Unity runtime code, prefabs, scenes, UI implementation, or `Assets/` imports.
- Keep contracted M01 map identity `iso.ch01.district_edge_01`; fix the source art behind that path instead of inventing a new id.

## Validation Required From Art/Atlas

The Art/Atlas handoff must include:

- changed file list
- imagegen provenance for every replacement visual
- PNG dimensions for every new/revised final asset
- JSON manifest parse confirmation
- file-exists confirmation for every manifest-declared file
- contact sheet paths
- confirmation that final art is not target-cropped/pasted/composited from mockups
- confirmation that final art is not deterministic/vector/programmatic placeholder output
- exact blocker list if anything remains missing

## Next Step After Art Delivery

If PM/user accepts the Art/Atlas package, route Gameplay to:

- bind the approved M01 source plate and atlases through the existing ECS/runtime presentation path
- preserve loading/main menu/custom game flow
- preserve architecture contract compliance
- regenerate M01-01 target-match proof against the approved mockup with side-by-side comparison and written match/mismatch assessment

QA/HCI remains held until runtime proof exists.
