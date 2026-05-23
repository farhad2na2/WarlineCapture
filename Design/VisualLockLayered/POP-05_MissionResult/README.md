# POP-05 Mission Result Visual Lock

Status: Victory, partial-success, defeat, and withdrawn target-locks generated. V01 implementation layer pack exists for the shared result shell.
Date: 2026-05-23

## Active Target

- Victory reference target: `reference/POP-05_MissionResult_Landscape_Target.png`
- Partial-success reference target: `reference/POP-05_MissionResult_PartialSuccess_Target.png`
- Defeat reference target: `reference/POP-05_MissionResult_Defeat_Target.png`
- Withdrawn reference target: `reference/POP-05_MissionResult_Withdrawn_Target.png`
- Victory candidate source: `reference/POP-05_MissionResult_TargetLock_V01.png`
- Variant manifest: `target_lock_variants_manifest.json`
- Variant prompt: `prompts/POP-05_MissionResult_AllResultVariants_TargetLock_V01.md`
- Variant review sheet: `validation/POP-05_MissionResult_targetlock_variants_contact_sheet.png`
- Canonical size: `2400 x 1080`

This target is the active result/debrief screen for the 3D single-map WarlineCapture direction. `POP-05` is one reusable screen with runtime variants for victory, partial success, defeat, withdrawal, and future auto-resolved Operations outcomes. It reports tactical outcome, star scoring, objective completion/failure, performance stats, authored rewards, and district/civilian consequences after a match.

## Runtime Variants

| State Id | Header Direction | Visual Tone | Primary CTA | Target Status |
|---|---|---|---|---|
| `VictoryComplete` | `OPERATION COMPLETE` / mission success title | Gold success, olive confirmation, clear-result energy. | `CONTINUE` | Current target-lock generated. |
| `PartialSuccess` | `OBJECTIVE SECURED` / `PARTIAL SUCCESS` | Gold plus amber caution; success with visible cost. | `CONTINUE` | Target-lock generated. |
| `DefeatFailed` | `OPERATION FAILED` | Warning amber/red, damaged command readout, no celebration. | `RETRY OPERATION` | Target-lock generated. |
| `Withdrawn` | `FORCE WITHDRAWN` | Muted amber/olive, recovered assets and abandoned goals. | `RETURN TO MAP` | Target-lock generated. |
| `SimulationResolved` | `OPERATION RESOLVED` | Neutral command report for Operations auto-resolution. | `VIEW DISTRICT` | Can reuse partial-success shell until a dedicated target is requested. |

The implementation should bind all state-specific text, values, stars, rewards, deltas, CTA labels, and route buttons from `MissionResultData`. Do not create separate Unity prefabs unless a future layout requirement proves the shared shell cannot support a state.

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
- Keep rewards authored and explicit. Do not use loot-box or hidden-odds presentation.
- Keep civilian safety and district consequence visible even when deltas are zero.
- Use `#00ff00` green-source sheets only for extraction assets, not for the target-lock mockup.

## Design Source

- `Design/WarlineCapture_3D_SingleMap_Gameplay_Direction.md`
- `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md`
- `Design/SagaChapters/WarlineCapture_Saga_Chapter01_First_Response.md`
- `Design/WarlineCapture_Economy_Reward_Design.md`
- `Design/WarlineCapture_Mission_Result_State_Spec.md`
- Gameplay scene references: `Assets/Game/Scenes/Demo.unity` and `Assets/Game/Scenes/Demo2.unity`

## Target Prompt Summary

The current victory target asks for a AAA mobile RTS mission result screen with:

- `OPERATION COMPLETE` / `FIRST CONTACT COMPLETE` result header
- Campaign / Chapter 01 / First Response / Duration metadata
- three-star result row for Objective Complete, Civilians Protected, and Losses Low
- objective checklist with completion states
- performance stats for enemies defeated, units lost, civilians saved, and supplies spent
- authored reward rows for Commander XP, Credits, Supplies, and Intel
- consequence rows for Civilian Safety, District Trust, Hostile Influence, and Infrastructure
- bottom route/action bar with Replay, Continue to Campaign Map, and Continue CTA

No 2.5D isometric map, separate strategic/tactical-map framing, random reward chest language, or old teal/cyan visual language should appear in this active target.
