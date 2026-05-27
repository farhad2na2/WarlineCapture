# SCN02 Main Menu Clean Target-Lock Verification

Date: 2026-05-27

Source of truth:
- `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/`
- `Design/VisualLockLayered/SCN-02_MainMenu/SCN02_MainMenu_TargetLock_LayoutContract.md`

Unity verification project:
- `D:\Projects\WarlineCapture-CodexUnity1`

Generated captures:
- `Design/AgentReports/Captures/GameUI/MainMenu/CleanTargetLock/GameUI_MainMenu_Stable.png`
- `Design/AgentReports/Captures/GameUI/MainMenu/CleanTargetLock/GameUI_ReturnedMainMenu_Stable.png`
- `Design/AgentReports/Captures/GameUI/MainMenu/CleanTargetLock/Responsive/GameUI_MainMenu_1920x1080.png`
- `Design/AgentReports/Captures/GameUI/MainMenu/CleanTargetLock/Responsive/GameUI_MainMenu_2400x1080.png`
- `Design/AgentReports/Captures/GameUI/MainMenu/CleanTargetLock/Responsive/GameUI_MainMenu_3840x2160.png`
- `Design/AgentReports/Captures/GameUI/MainMenu/CleanTargetLock/Responsive/GameUI_MainMenu_4800x2160.png`

Verification result:
- Main menu composition is clean and target-like against the active layered target mockup.
- The reopened visual mismatch items were corrected after the first capture review: header backing/continuity, center card vertical rhythm, visible segmented card progress, and deploy CTA placement.
- Header, background, left nav, center cards, commander panel, comms panel, and deploy CTA are built from the active layer assets, not old generated Unity scenes or stale mockups.
- Header action buttons are anchored to the visible right edge for narrow and wide aspect ratios.
- Left nav includes Campaign, Operations, Skirmish, Store, Commander, and Settings without overlap.
- Comms panel sits below the nav rows and no longer hides Settings.
- Mode cards are children of `MiddleContent/ModeCardsContainer`; thumbnail art is childed inside each card viewport and clipped through `RectMask2D`.
- Mode card progress bars now have visible local segmented fills matching the target visual language.
- Commander portrait, identity, readiness, locked rows, portrait hotspot, and deploy CTA are children of `RightContent` or `RightContent/CommanderPanel` as appropriate.
- Unity `BuildStep9` validated all 9 capture files in the shadow project.
- `SCN01_LoadingContent.prefab` was not regenerated or edited.
- Result is target-like and clean, not pixel-perfect to the reference PNG. Remaining differences are limited by available sliced layer assets and runtime prefab composition.
