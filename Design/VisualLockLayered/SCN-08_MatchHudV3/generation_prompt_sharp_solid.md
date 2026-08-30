# Match HUD V3 Sharp Solid Generation Prompt

```text
Generate a more creative WarlineCapture in-game Match HUD that keeps the actual gameplay HUD structure from the live capture, but redesigns the visual language to match the sharp, solid, bright Main Menu V3 direction.

References:
- Layout source of truth: Design/AgentReports/Captures/GameUI/MatchHud/CleanTargetLock/Responsive/GameUI_MatchHud_2400x1080.png
- Style source: Design/VisualLockLayered/SCN-02_MainMenuV3/reference/SCN-02_MainMenuV3_SharpSolid_Target.png
- Prefab source of truth: Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab

This must look like actual gameplay HUD, not a menu mockup.

Preserve gameplay lockups:
- Top center: MOVE ORDER / Rifle Squad.
- Top right: Materials, Oil, Fuel, Civilian Risk, then large Settings, Pause, Menu buttons.
- Left side: Objectives above Selected Rifle Squad.
- Bottom left: numbered squad tray cards.
- Bottom center: command rail.
- Right side: vertical quick rail and zoom controls.
- Bottom right: minimap.
- Upper right: HOSTILE CELL SPOTTED feedback.
- ARIA assistant as a cyan tactical button/panel.

Mobile control direction:
- Bottom command rail uses large sharp rectangular action tiles.
- Labels: SELECT, MOVE, ATTACK, HOLD, STOP, BUILD, SCAN, SUPPORT.
- MOVE is active with bright green/cyan fill and clear drop shadow.
- ATTACK is red-orange, BUILD is yellow/industrial, SCAN is cyan, SUPPORT is teal, HOLD is olive, STOP is dark red.
- Squad tray cards are taller and clearer with visible health bars.
- Right quick rail buttons are bigger stacked rectangles.
- Objectives and selected-squad panels use solid dark panels with colored headers and subtle shadows.
- Resource chips are clean rectangular chips with bright badges.

Avoid:
- Old thin black-and-gold HUD borders.
- Ornate gold filigree or bevel frames.
- Tiny buttons.
- Rounded pill-heavy UI.
- Main menu layout, campaign cards, commander hero portrait.
- Credits, Command, Supply, diamonds, gems.
- Water, river, sea, coast, lake, naval maps.
```
