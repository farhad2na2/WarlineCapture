# POP-10 Assistant Takeover Visual Lock

Status: V3 Iteration 2 is review-frozen in the production POP-13 prefab as a
runtime state. Explicit user acceptance remains pending.

Current V3 target lock:

`reference/POP-10_AssistantTakeoverV3_Final_Target.png`

Runtime presentation prefab:

`Assets/Game/Prefabs/UI/Shell/Popups/POP13_ARIACommandAssistantPopup.prefab`

The takeover is not a second prefab. `AriaCommandAssistantPopupView` switches
the shared assistant surface to a centered takeover composition when the live
control state is `ARIA CONTROL`. This preserves one ARIA portrait, one icon
set, and one runtime behavior implementation.

## Acceptance gate

- The modal remains centered at 16:9 and 20:9 while the Match HUD side groups
  stay attached to the real screen edges.
- The right ARIA tutorial/minimap panel remains visible.
- The ARIA portrait preserves aspect ratio.
- Resume Command and Stop ARIA invoke the live stop-control binding.
- Current Intent is populated from runtime recommendation and goal data.
- All gradients remain visible and every frame uses the constant 3 px V3
  border system.
- The production UI does not use the flattened target PNG.
