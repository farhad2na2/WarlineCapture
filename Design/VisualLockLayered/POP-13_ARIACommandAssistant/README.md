# POP-13 ARIA Command Assistant Visual Lock

Status: target-lock reference saved, audited implementation-ready functional tracker locked, and the approved single-layout production prefab authored. Runtime migration from the older code-built popup remains. A separated production layer pack is deferred and is not required for functional acceptance.

This pack defines the V01 target reference for the in-match ARIA Command Assistant popup. The reference PNG is saved under `reference/`; runtime implementation should use real Unity UI objects and live TMP bindings, not the target PNG.

Current target-lock request:

`prompts/POP-13_ARIACommandAssistant_TargetLock_V01.md`

Accepted saved reference:

`reference/POP-13_ARIACommandAssistant_TargetLock_V01.png`

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

- The popup must be readable at match-HUD scale and must not appear as a tiny side panel.
- The only supported layout is `LandscapeLayout`, authored at `2460 x 1510`, `(0, 156)`, on the authoritative `4800 x 2160` `Menu.unity` canvas. Do not add a compact/mobile variant.
- The popup must sit left of the right quick rail and bring itself to front when opened.
- The visual language must read as a Build Popup / Resource Exchange sibling: dark brushed-metal panels, gold command chrome, cyan target telemetry accents, readable tactical typography.
- The target-lock recommendation area must be a prominent ARIA-specific surface, not a generic text block.
- Buttons must be large enough to read and click: SHOW ME, DO IT, STOP, CLOSE.
- Do not ship the flattened target PNG as runtime UI.

## Next Steps

1. Replace the current runtime code-built ARIA popup with the approved `LandscapeLayout` prefab binding when the functional row contracts are ready.
2. Follow `IMPLEMENTATION_TRACKER.md` before enabling mockup-only details, so every panel row and metric is backed by real ECS data.
3. Complete functional/visual validation with the serialized prefab and existing approved sprites. Treat any later separated green-key layer pack as optional production-art replacement, not a blocker for the functional POP-13 pass.
