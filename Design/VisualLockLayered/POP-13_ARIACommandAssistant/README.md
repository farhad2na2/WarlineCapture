# POP-13 ARIA Command Assistant Visual Lock

Status: V3 Iteration 3 is review-frozen in the production prefab. The old
2460x1510 gold modal has been removed from the runtime path. Explicit user
acceptance remains pending.

This pack defines the in-match ARIA Command Assistant popup. The current V3
reference is `reference/POP-13_ARIACommandAssistantV3_Final_Target.png`; runtime
implementation uses real Unity UI objects and live TMP bindings, not the target
PNG.

Current target-lock request:

`prompts/POP-13_ARIACommandAssistant_TargetLock_V01.md`

Current V3 target lock:

`reference/POP-13_ARIACommandAssistantV3_Final_Target.png`

Functional implementation tracker:

`IMPLEMENTATION_TRACKER.md`

Approved Unity presentation prefab:

`Assets/Game/Prefabs/UI/Shell/Popups/POP13_ARIACommandAssistantPopup.prefab`

## Design Source

- `../../ARIA_Assistant_ECS_Design.md`
- `../../FTUE_And_Command_Assistant_Design.md`
- `../POP-12_ResourceLogisticsExchange/reference/POP-12_ResourceLogisticsExchange_NewMainMenuArtDirection_TargetLock_V01.png`
- `../SCN-09_BuildDrawer/reference/SCN-09_BuildDrawer_NewMainMenuArtDirection_TargetLock_V03.png`
- `../../UI_Imagegen_Target_Mockup_To_Layered_Unity_Workflow.md`
- `../../UIUX_Target_To_Canvas_Workflow_Guide.md`

## Acceptance Gate

- The 1672x941 reference-space layout must keep the 510x690 assistant panel on
  the true top-right edge at both 16:9 and 20:9.
- The popup must be readable at Match HUD scale and leave the battlefield and
  the rest of the HUD interactive outside the visible panel.
- Header resources, Settings, and Pause must compact without touching the
  assistant frame; the embedded compact ARIA panel must hide while POP-13 is
  open and restore on close.
- The visual language uses dark/cyan directional gradients, shared V3 icons,
  and one constant 3 px frame-border system.
- The target-lock recommendation area must be a prominent ARIA-specific surface, not a generic text block.
- The V3 ARIA portrait must use an aspect-preserving crop and must never stretch.
- SHOW ME and Close remain live actions. The voice switch reads and writes the
  shared persisted voice setting and updates runtime audio projection.
- Do not ship the flattened target PNG as runtime UI.

## Next Steps

1. Obtain explicit user acceptance of the review-frozen Iteration 3 evidence.
2. Keep `IMPLEMENTATION_TRACKER.md` authoritative for live ECS data and command
   semantics; empty production data must hide rather than synthesize facts.
3. Re-run the focused V3, 23-test behavior, Tutorial regression, and exact-size
   Play Mode gates after any shared POP-13 change.
