# SCN-08 Match HUD V3

Approved working direction for the WarlineCapture gameplay Match HUD V3 concept.

## Target

- Final reference image: `reference/SCN-08_MatchHudV3_Final_Target.png`
- Active reference image: `reference/SCN-08_MatchHudV3_SharpSolid_Target.png`
- Current iteration mirror: `reference/SCN-08_MatchHudV3_SharpSolid_Target_v17.png`
- Current source generation: `/Users/farhad/.codex/generated_images/019e0cb1-e941-7eb0-b318-63b09c645a05/call_Zy8qlRxc9QcKIJDRSqwReiVY.png`
- Transport passengers final reference: `reference/SCN-08_MatchHudV3_TransportPassengers_Final_Target.png`
- Transport passengers active target: `reference/SCN-08_MatchHudV3_TransportPassengers_Target.png`
- Transport passengers iteration mirror: `reference/SCN-08_MatchHudV3_TransportPassengers_Target_v01.png`
- Transport passengers source generation: `/Users/farhad/.codex/generated_images/019e0cb1-e941-7eb0-b318-63b09c645a05/call_pWQ6Mh2cHSuLQY0JoI6MHfdy.png`
- Prior sharp-solid v16 reference image: `reference/SCN-08_MatchHudV3_SharpSolid_Target_v16.png`
- Prior sharp-solid v15 reference image: `reference/SCN-08_MatchHudV3_SharpSolid_Target_v15.png`
- Prior sharp-solid v14 reference image: `reference/SCN-08_MatchHudV3_SharpSolid_Target_v14.png`
- Prior sharp-solid v13 reference image: `reference/SCN-08_MatchHudV3_SharpSolid_Target_v13.png`
- Prior sharp-solid v12 draft reference image: `reference/SCN-08_MatchHudV3_SharpSolid_Target_v12.png`
- Prior sharp-solid v11 reference image: `reference/SCN-08_MatchHudV3_SharpSolid_Target_v11.png`
- Prior sharp-solid v10 reference image: `reference/SCN-08_MatchHudV3_SharpSolid_Target_v10.png`
- Prior sharp-solid v09 reference image: `reference/SCN-08_MatchHudV3_SharpSolid_Target_v09.png`
- Prior sharp-solid v08 reference image: `reference/SCN-08_MatchHudV3_SharpSolid_Target_v08.png`
- Prior sharp-solid v07 reference image: `reference/SCN-08_MatchHudV3_SharpSolid_Target_v07.png`
- Prior sharp-solid v06 reference image: `reference/SCN-08_MatchHudV3_SharpSolid_Target_v06.png`
- Prior sharp-solid v05 reference image: `reference/SCN-08_MatchHudV3_SharpSolid_Target_v05.png`
- Prior sharp-solid v04 reference image: `reference/SCN-08_MatchHudV3_SharpSolid_Target_v04.png`
- Prior sharp-solid v03 reference image: `reference/SCN-08_MatchHudV3_SharpSolid_Target_v03.png`
- Prior sharp-solid v02 reference image: `reference/SCN-08_MatchHudV3_SharpSolid_Target_v02.png`
- Original sharp-solid v01 backup image: `reference/SCN-08_MatchHudV3_SharpSolid_Target_v01.png`
- Prior reference image: `reference/SCN-08_MatchHudV3_Target.png`
- Approved baseline source: `Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab`
- Live layout reference: `Design/AgentReports/Captures/GameUI/MatchHud/CleanTargetLock/Responsive/GameUI_MatchHud_2400x1080.png`
- Style reference: `Design/VisualLockLayered/SCN-02_MainMenuV3/reference/SCN-02_MainMenuV3_SharpSolid_Target.png`
- Generated date: 2026-08-25

## Lockup Rule

The actual gameplay HUD prefab and live capture are the source of truth. Main Menu V3 is only a style reference.

V17 is the hard-rectangular V3 HUD target. It keeps the expanded ARIA/map and bottom-command layout while moving the footer controls fully flush to the bottom screen edge.

Preserve these prefab/gameplay lockups:

- `CurrentOrderBanner`
- `ResourceStrip`
- `ObjectivesPanel`
- `SelectedSquadPanel`
- `SquadTray`
- `CommandRail`
- `RightQuickRail`
- `MinimapPanel`
- `ThreatJumpPanel`
- `AriaAssistantButton`

## Visual Direction

- Keep the actual in-match tactical HUD over gameplay, not a menu screen.
- No large top-left logo lockup.
- Do not show a top current-order pill. Current order lives inside the tall selected-unit panel.
- Top right belongs to a medium ARIA assistant panel. It should be larger than the v05 strip but much smaller than the oversized v06 portrait block.
- Move the match resource/status header left of ARIA, away from the top-right corner.
- Top header uses the match resource strip plus Settings and Pause only. Do not show a top-right menu/list button.
- Warning panel is visible in the upper-right/mid-right gameplay area and reads `HOSTILE CELL SPOTTED`.
- Do not show the standalone Objectives panel in the main HUD. Objectives belong in the ARIA assistant/popup flow.
- Left column is a single tall selected-unit panel starting near the top of the screen.
- The tall selected-unit panel includes title, subtitle, portrait, health, current order/state, passenger status, and large Return, Destroy, Board, and Camera buttons.
- Bottom left keeps five numbered squad cards with visible title text.
- Bottom command bar keeps Select, Move, Attack, Hold, Stop, Scan, Support, and Build.
- Footer controls are bottom-docked. The squad tray, command buttons, Support, and Build align to the bottom screen edge with no visible battlefield gap underneath.
- Build and Support live in the bottom command bar instead of the right quick rail during this expanded-ARIA FTUE state.
- `BUILD` is the far-right primary command tile and is only moderately larger than `SUPPORT`, not a giant or detached button.
- Do not show a bottom `MAP` command tile in this state.
- Do not show a standalone minimap panel in this state. The tactical map attaches to the bottom of the expanded ARIA panel so it moves with ARIA.
- Top resource/status header must remain visible with Materials, Oil, Fuel, Civilian Risk, Settings, and Pause.
- Right side can be occupied by the expanded ARIA FTUE panel plus attached map as long as it does not cover the top header, bottom command bar, or warning panel.
- ARIA portrait should stay readable in this state, with the blue holographic side-swept-hair character treatment. Use a tighter head-and-shoulders crop and move text/buttons upward so ARIA remains present without showing too much body.
- Keep the stable current V3 HUD target at `reference/SCN-08_MatchHudV3_SharpSolid_Target.png`; preserve numbered iterations for history.
- Center remains playable battlefield space with selection rings, move paths, target markers, and objective pins.
- Current V3 direction uses bigger mobile touch controls, solid color action tiles, sharp rectangular panels, strong drop shadows, and fewer ornate borders.
- Prefer true square rectangular corners in this direction. Avoid rounded corners, diagonal chamfers, and decorative notched corners on primary HUD panels.
- Avoid the old thin black-and-gold HUD border language. Gold can remain only as a minor accent.
- Runtime feedback must be visible for invalid commands, including attack unavailable/no selected unit states such as `ATTACK UNAVAILABLE - SELECT A UNIT FIRST`.
- Tutorial indicators are ARIA-themed overlay affordances anchored to real controls, not replacement button styles. Use cyan/teal pulsing outlines, corner brackets, soft glow, thin pointer arrows, compact coach cards, and amber numbered badges such as `1 SELECT SQUAD` or `2 TAP MOVE`.
- FTUE instruction text and player choices should prefer an expanded ARIA assistant state over a detached coach card. The ARIA module can unfold into a connected drawer with longer copy, progress such as `TUTORIAL 1/3`, and large `DO IT` / `SHOW ME` action buttons.

## Transport Passengers Drawer

- The passenger drawer is a Match HUD state, not a full-screen modal.
- Keep the selected transport panel visible on the left, with passenger status and the Return, Destroy, Board, and Camera action buttons intact.
- Attach the passengers drawer beside the selected transport panel so it reads as the expanded passenger state of that unit.
- The drawer header uses runtime capacity text such as `PASSENGERS 4/10 | SOLDIERS 4/10`.
- Rows show portrait, passenger name, role, health fill, health value, and a large `EXIT` action.
- Footer actions are `EXIT ALL`, `CLOSE`, and optional transport-specific actions such as `ROPE DROP`.
- Do not cover ARIA, the map attached to ARIA, the top match resources, the warning panel, or the bottom command rail.

## Resource Rule

Match HUD resources are:

- Materials
- Oil
- Fuel
- Civilian Risk

Do not show persistent account resources in the Match HUD:

- Credits
- Command

Do not show legacy/fake resources:

- Supply
- Diamonds
- Gems

## Environment Rule

- Use dry Sahrin battlefield, checkpoint, base, town, convoy, desert-road, or ruined-urban settings.
- Do not use sea, coast, river, lake, or naval backgrounds.
