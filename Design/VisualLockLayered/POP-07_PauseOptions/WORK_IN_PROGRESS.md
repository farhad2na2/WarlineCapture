# POP-07 Pause Options V3 Work In Progress

Status: target/current comparison completed and source rebuild staged. Unity
build, focused validation, and exact-size live comparison are pending.

## Target Lock

`reference/POP-07_PauseOptionsV3_Target.png`

## Rejected Legacy Capture

`../_CanvasTargetLockVisualMatch/PauseMenu/iteration_01/shadow_canvas_pause_menu_sprite_pass_1920x1080.png`

The legacy capture is rejected because it uses the menu-world background,
ornate/gold chrome, an oversized panel, placeholder-quality icons, flat actions,
and omits the target's objective, squads, civilian-risk, and autosave column.

## Staged V3 Source

- `Assets/Game/Scripts/Editor/PauseOptionsV3PrefabBuilder.cs`
- `Assets/Game/Scripts/UI/Screens/PauseOptionsV3PopupView.cs`
- `Assets/Tests/Editor/PauseOptionsV3PrefabTests.cs`
- exact 1920x1080 and 4800x2160 capture entry points in
  `Assets/Game/Scripts/Editor/CanvasMenuFallbackValidation.cs`

The five action rows use visibly directional procedural gradients and independent
3 px borders. The outer modal uses one 3 px frame; internal status rows use
single 3 px dividers so adjacent borders do not overlap. Icons reuse shared V3
atlas sources. Resume, Options, Exit, and close keep shell action dispatch.
Restart uses the existing mission launch/cleanup path after confirmation, and
Help opens a real in-popup controls panel.

Do not create an immutable iteration until both exact live captures pass visual
comparison against the target.

