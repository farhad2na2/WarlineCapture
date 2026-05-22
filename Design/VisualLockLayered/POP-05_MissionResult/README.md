# POP-05 Mission Result Visual Lock

Status: Target-lock mockup and V01 implementation layer pack generated.
Date: 2026-05-22

## Active Target

- Reference target: `reference/POP-05_MissionResult_Landscape_Target.png`
- Candidate source: `reference/POP-05_MissionResult_TargetLock_V01.png`
- Canonical size: `2400 x 1080`

This target is the active result/debrief screen for the 3D single-map WarlineCapture direction. It reports tactical success, star scoring, objective completion, performance stats, deterministic rewards, and district/civilian consequences after a match.

## Layer Pack

Active implementation pack:

- Manifest: `layer_manifest.json`
- Layers: `layers/`
- Source sheets: `generated_v01/source/`
- Contact sheet: `validation/POP-05_MissionResult_layers_contact_sheet.png`
- Generated V01 manifest: `generated_v01/layer_manifest.json`

The generated pack contains separate source groups for no-UI background art, mission snapshot art, blank result frames, buttons, stars, reward icons, consequence icons, stat icons, route icons, and progress/status elements. Text and numbers should be live in Unity.

## Layer Rules Applied

- Do not crop or cut the target-lock mockup into implementation assets.
- Generate clean independent source assets for the layer pack.
- Parent frames/backgrounds must not bake child icons, stars, checkmarks, reward rows, progress fills, numbers, or text.
- Keep rewards deterministic and explicit. Do not use loot-box or hidden-odds presentation.
- Keep civilian safety and district consequence visible even when deltas are zero.
- Use `#00ff00` green-source sheets only for extraction assets, not for the target-lock mockup.

## Design Source

- `Design/WarlineCapture_3D_SingleMap_Gameplay_Direction.md`
- `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md`
- `Design/SagaChapters/WarlineCapture_Saga_Chapter01_First_Response.md`
- `Design/WarlineCapture_Economy_Reward_Design.md`
- Gameplay scene references: `Assets/Game/Scenes/Demo.unity` and `Assets/Game/Scenes/Demo2.unity`

## Target Prompt Summary

The target asks for a AAA mobile RTS mission result screen with:

- `VICTORY` / `FIRST CONTACT COMPLETE` result header
- Campaign / Chapter 01 / First Response / Duration metadata
- three-star result row for Objective Complete, Civilians Protected, and Losses Low
- objective checklist with completion states
- performance stats for enemies defeated, units lost, civilians saved, and supplies spent
- deterministic reward rows for Commander XP, Credits, Supplies, and Intel
- consequence rows for Civilian Safety, District Trust, Hostile Influence, and Infrastructure
- bottom route/action bar with Replay, Continue to Campaign Map, and Continue CTA

No 2.5D isometric map, separate strategic/tactical-map framing, random reward chest language, or old teal/cyan visual language should appear in this active target.
