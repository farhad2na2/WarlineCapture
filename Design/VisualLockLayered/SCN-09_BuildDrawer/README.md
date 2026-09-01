# SCN-09 Match Build Popup V3

Current visual direction for the WarlineCapture in-match build popup.

## Current Target

- Final V3 reference: `reference/SCN-09_BuildDrawer_MatchBuildPopupV3_Final_Target.png`
- Active implementation target: `reference/SCN-09_BuildDrawer_MatchBuildPopupV3_SharpSolid_Target.png`
- Final iteration mirror: `reference/SCN-09_BuildDrawer_MatchBuildPopupV3_SharpSolid_Target_v04.png`
- Source generation: `/Users/farhad/.codex/generated_images/019e0cb1-e941-7eb0-b318-63b09c645a05/call_0zfUkcawxKEqwOuXCQ2njF3E.png`
- Prior full-screen V3 references: `reference/SCN-09_BuildDrawer_MatchBuildPopupV3_SharpSolid_Target_v01.png`, `reference/SCN-09_BuildDrawer_MatchBuildPopupV3_SharpSolid_Target_v02.png`, `reference/SCN-09_BuildDrawer_MatchBuildPopupV3_SharpSolid_Target_v03.png`
- Prior drawer reference: `reference/SCN-09_BuildDrawer_NewMainMenuArtDirection_TargetLock_V03.png`
- Generated date: 2026-08-29

## Direction

- Treat this screen as a full-screen match popup opened from the bottom `BUILD` command.
- Keep the match battlefield visible only as a dimmed context layer around the popup.
- Use the Main Menu V3 sharp-edge language: large rectangular buttons, solid color blocks, strong shadows, and no ornate gold frame system.
- Use the actual game tabs on a left-side vertical rail: `BUILDINGS`, `VEHICLES`, `AIRCRAFTS`, and `SOLDIERS`.
- Use large mobile touch targets for tabs, structure cards, queue controls, close, and final build confirmation.
- Treat catalog cards as selection tiles only; do not place repeated `BUILD` buttons inside each item.
- Use the removed card-button space for larger structure artwork and clearer selected-card state.
- Keep build choices readable and concrete: `GUARD TOWER`, `BARRACKS`, `AIRFIELD`, and `SUPPLY DEPOT`.
- Keep match resources to `Materials`, `Oil`, and `Fuel`.
- Keep the primary `PLACE` / `BUILD` action as a large standalone footer button, separate from the queue/detail panel.
- Do not include a separate `QUEUE` footer button.
- Include clear build feedback such as `SELECT A VALID BUILD AREA`.
- Avoid rounded corners, diagonal chamfers, thin black/gold borders, tiny drawer controls, account resources, diamonds, gems, water, sea, coast, or naval imagery.

## Implementation Notes

- Use this as a visual target only. Do not bake live labels, costs, timers, queue values, or feedback text into runtime sprites.
- Reuse the unit/building illustrations already authored in the runtime catalogs. Build cards bind `portraitCardSprite`, detail binds `portraitActionSprite`, and production rows resolve their thumbnail from the same catalog; do not request or create four screen-local replacement images for the example cards in the mockup.
- Keep catalog illustration aspect ratios intact and allow the data-driven grid to show whichever units/buildings the current mission exposes.
- Preserve the older V03 drawer image as historical reference until V3 implementation slices are produced.
