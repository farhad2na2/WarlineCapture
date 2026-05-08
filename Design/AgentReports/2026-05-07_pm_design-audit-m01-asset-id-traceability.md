Status: advisory
Topic: M01 contract asset ids are not all traceable as asset-register rows
Docs reviewed:
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/WarlineCapture_Art_Asset_Requirements_Register.md`
- `Design/WarlineCapture_Art_Asset_Requirements_Register.csv`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
Finding:
- The M01 contract names concrete implementation ids for `iso.ch01.district_edge_01.ground`, `unit.player.rifle_squad_01`, `unit.enemy.patrol_01`, and `decor.command_point`.
- The asset register has broader related rows such as `level.ch01.district_edge_01`, `saga.ch01.m01.first_contact` map/minimap rows, `iso.ch01.district_edge_01.metadata`, validation scene, markers, and VFX.
- This audit did not find explicit asset-register rows for `iso.ch01.district_edge_01.ground`, `unit.player.rifle_squad_01`, `unit.enemy.patrol_01`, or `decor.command_point` by those exact ids.
Why it matters:
- Gameplay can wire runtime ids and produce captures while PM/Art cannot cleanly mark those exact M01 ids as `missing`, `exists_needs_review`, `approved`, or `complete`.
- The current sprite-renderer work depends on these exact ids, so missing traceability can let temporary AI-generated PNGs or validation sprites be mistaken for final asset-register completion.
- QA/HCI needs the same ids to decide whether a failed visual check is an implementation bug, a missing final asset, or an unapproved placeholder.
Recommended fix:
- Add explicit rows, aliases, or notes in the asset register for the exact M01 runtime ids:
  - `iso.ch01.district_edge_01.ground`
  - `unit.player.rifle_squad_01`
  - `unit.enemy.patrol_01`
  - `decor.command_point`
- Keep their status at `exists_needs_review` or `missing` until the final AI-generated or authored assets are approved at close tactical scale.
- Link `unit.enemy.patrol_01` to the separate hostile-readability audit so it cannot be completed with color tint alone.
- If `level.ch01.district_edge_01` is intended to be the canonical row for `iso.ch01.district_edge_01.ground`, document that alias directly in the CSV notes.
Affected lanes:
- Gameplay
- QA/HCI
- Art/PM
- UI/VFX for marker and minimap readability checks
Needs user decision:
- No immediate user decision is required.
- Before final M01 art approval, PM should decide whether to add exact rows or formal alias notes in the register.
Next task update needed:
- Not needed for the current Gameplay capture framing fix.
- Needed before marking M01 tactical ground, player squad, hostile patrol, or command/decor proxy art complete.
