# Match HUD V3 Generation Prompt

```text
Create a WarlineCapture SCN-08 actual gameplay Match HUD V3 concept.

Use this live gameplay capture as the source of truth for layout and lockups:
Design/AgentReports/Captures/GameUI/MatchHud/CleanTargetLock/Responsive/GameUI_MatchHud_2400x1080.png

Use this only as the color/material/style reference:
Design/VisualLockLayered/SCN-02_MainMenuV3/reference/SCN-02_MainMenuV3_Target.png

Preserve the actual gameplay HUD structure from the prefab:
- CurrentOrderBanner
- ResourceStrip
- ObjectivesPanel
- SelectedSquadPanel
- SquadTray
- CommandRail
- RightQuickRail
- MinimapPanel
- ThreatJumpPanel
- AriaAssistantButton

This must be the in-game tactical HUD over active gameplay, not a menu screen.

Strict layout:
- No large WARLINE CAPTURE logo lockup in the top-left.
- Top center: compact current-order pill with MOVE ORDER / Rifle Squad.
- Top right: match resource strip plus small square settings, pause, and menu/list buttons.
- Resource strip: Materials, Oil, Fuel, Civilian Risk only.
- Left column: Objectives panel above Selected Rifle Squad panel.
- Bottom left: numbered squad tray cards with health bars.
- Bottom center: command rail with SELECT, MOVE, ATTACK, HOLD, STOP, BUILD, SCAN, SUPPORT.
- Right side: vertical quick rail and zoom controls.
- Bottom right: minimap with markers and viewport rectangle.
- Center: playable battlefield space with selected squad ring, move path, target markers, objective pins, and hostile markers.

Style:
- Colorful high-end mobile RTS HUD.
- Dark graphite beveled panels, gold trim, amber warnings, olive military accents, cyan tactical overlays.
- Dry Sahrin urban battlefield, checkpoint, concrete barriers, roads, watchtowers, armored vehicles, squads, smoke.
- Faceted/low-poly WarlineCapture art influence, not generic photorealism.

Avoid:
- Main menu layout.
- Campaign cards.
- Commander hero portrait.
- Credits or Command in match header.
- Supply, diamonds, gems, or account-wallet resources.
- Water, sea, rivers, coast, lake, or naval map.
- Oversized decorative panels that cover gameplay.
- Unreadable tiny text or overlapping controls.
```
