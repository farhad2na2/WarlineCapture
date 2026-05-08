# PM Message For Gameplay

Date: 2026-05-08

The user rejected the `Game_Legecy` scene split and said Gameplay should complete it before continuing the selected-readability/ECS visual fixes.

Fix this first:

- `Game.unity` must be clean for the 2D/isometric production scene.
- Remove old prototype `UI_Canvas`, old legacy canvas/menu setup, duplicate old prototype directional lights, and old prototype `Global Volume` from `Game.unity`.
- The user still saw old `UI_Canvas`, two directional lights, and `Global Volume` in `Game.unity`; that is rejected.
- `Game_Legecy.unity` must be playable as the legacy prototype.
- Pressing Play in `Game_Legecy.unity` must not load the new 2D/isometric game.
- `Game_Legecy.unity` should run only the legacy playable setup with legacy `UI_Canvas`.

The previous handoff said `Game_Legecy` may include production bootstrap objects and required `Game.unity` to keep `Global Volume`/`Directional Light`. Treat that as superseded by this message.

Expected report:

`Design/AgentReports/2026-05-08_gameplay_game-legecy-scene-isolation-fix.md`

Do not commit or push.
