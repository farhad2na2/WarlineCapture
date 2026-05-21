# PM UI Rejection: Composite Overlay Is Not Runtime UI

Date: 2026-05-16
Lane: PM
Target lane: UI
Status: active rejection

## Decision

`Design/AgentReports/2026-05-16_ui_visual-target-match-implementation-v2.md` is rejected.

The implementation added full-screen target composites as visible runtime overlays:

- `TargetMatchCompositeOverlay`
- `Assets/Game/Art/UI/Generated/MainMenu/LayeredOneGo/Content/SCN02_MainMenu_Landscape_TargetComposite.png`
- `Assets/Game/Art/UI/Generated/MissionResult/LayeredOneGo/Content/POP05_MissionResult_Landscape_TargetComposite.png`

That is not a professional AAA runtime UI implementation. It is placing the approved mockup on top of the UI to get a perfect comparison score.

## Why Rejected

This approach bypasses the actual requirements:

- no real visible layout reconstruction
- no real sliced/layered UI composition
- no visible live TMP/data ownership
- no scalable/localizable UI surface
- no interactive state fidelity
- no reusable production components
- no useful proof that the runtime UI was implemented

An `mse=0.00` result is invalid if it is achieved by rendering the target mockup itself.

## Required Correction

UI must produce:

`Design/AgentReports/2026-05-16_ui_visual-target-match-implementation-v3.md`

Required first step:

- remove the composite overlay implementation from `SCN-02_MainMenu` and `POP-05_MissionResult`
- remove runtime use of the target composite images
- confirm the new captures do not contain full-screen target mockup overlays

Then UI must implement the visible UI from:

- approved sliced/layered assets
- reusable frames/icons/buttons
- live TMP text
- real interactive components
- runtime data bindings

Target mockups may be used only as visual references for comparison. They must not be used as visible runtime layers, full-screen backplates, screenshots, flattened composites, or contact-sheet substitutes.

## Acceptance

Completion requires fresh captures and target comparisons showing real runtime UI components matching the target region by region.

If exact match is impossible because required art slices are missing, UI must still implement all nonblocked visible regions and list the exact missing Art/Atlas assets by region.
