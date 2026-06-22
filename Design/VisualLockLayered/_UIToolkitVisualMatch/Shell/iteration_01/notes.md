# Shell - Iteration 01

Status: Satisfied for structural/static pass.

## Scope

Verified the shared UI Toolkit shell after the target-lock surface passes. No visual shell rewrite was required.

Required shell regions and slots remain present:

- `UIShellAppCanvas`
- `SafeAreaRoot`
- `ContentRoot`
- `MenuBackgroundRegion`
- `HeaderRegion`
- `LeftRegion`
- `MiddleRegion`
- `MainMenuScreenSlot`
- `MatchScreenSlot`
- `ArmoryScreenSlot`
- `CommanderProfileScreenSlot`
- `ResultScreenSlot`
- `RightRegion`
- `FooterRegion`
- `ModalOverlay`
- `PopupScreenSlot`
- `TooltipLayer`
- `LoadingLayer`
- `LoadingScreenSlot`

Runtime scaling contract remains intact in `RuntimePanelSettings.asset`:

- `m_ScaleMode: 2`
- `m_ReferenceResolution: {x: 4800, y: 2160}`
- `m_ScreenMatchMode: 2`
- `m_Match: 0.5`

## Shadow Validation

- Synced `UIShellAppCanvas.uxml`, `UIShellAppCanvas.uss`, and the editor-only static preview tooling to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Confirmed the shadow shell UXML/USS files match the main repository copies.
- Parsed the shadow shell UXML and confirmed all required region and slot names are present.
- Verified the shadow project `RuntimePanelSettings.asset` still uses the approved 4800x2160 Scale With Screen Size contract.
- Did not enter PlayMode.
- Did not open or validate in the main Unity project.

Note:

- The already-open shadow Unity editor had not imported the newly synced `Open Shell Static Preview` menu item during this slice, so validation used synced-file and PanelSettings checks rather than a UI Builder shell screenshot. This is acceptable for this pass because the shell root is a transparent slot scaffold and no shell visual styling changed.

## Validation

- `git diff --check` passes.
