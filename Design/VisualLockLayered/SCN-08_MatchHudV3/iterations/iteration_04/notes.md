# Match HUD V3 — Iteration 4

Status: current review candidate; explicit user acceptance pending.

Target:

- `../../reference/SCN-08_MatchHudV3_Final_Target.png`

Evidence:

- `match_hud_v3_16x9.png` — runtime-bound prefab at 1920x1080
- `match_hud_v3_20x9.png` — runtime-bound prefab at 4800x2160
- Unity log: `/private/tmp/warline-match-hud-v3-iteration-04.log`

Comparison corrections completed in this iteration:

- centered 1672x941 composition remains stable at 16:9 and 20:9;
- ARIA portrait, selected-squad portrait, minimap, and gameplay backdrop preserve aspect ratio;
- the expanded ARIA panel owns the live minimap and follows the portrait-first target hierarchy;
- the selected-unit panel retains its real selection/action/passenger bindings;
- Board moved into the selection action grid, while Support and Build moved into the eight-command footer rail;
- resource, threat, feedback, selection, squad, and command surfaces use procedural directional gradients and the constant 3 px border contract;
- the resource gradient no longer consumes a layout cell;
- squad health fills remain inside their own cards and all five target labels render;
- Move is the active command treatment and the feedback message renders in full;
- Match-only icon silhouettes are packed once in `UI_V3_MatchIcons_01.spriteatlas`; existing shared icons are referenced from their existing atlases without duplication.

Validation markers:

- `[MatchHudV3PrefabBuilder] validation=Passed commands=8 squads=5 gradients=32 aria=minimap-attached art=aspect-preserved`
- `[MatchHudV3PrefabBuilder] result=Passed layout=1672x941 gradients=procedural borders=3 atlases=shared match=runtime-bound`
- both capture sizes report `capture=Passed`.
