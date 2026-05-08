# PM Message To Gameplay

Date: 2026-05-08
Priority: P0 Game_Legecy scene isolation

The user assigned a new priority task.

Read:

- `Design/AgentReports/2026-05-08_pm_game-legecy-scene-isolation-routing.md`
- `Design/AgentTasks/gameplay_current.md`

Implement:

- create `Assets/Game/Scenes/Game_Legecy.unity`
- move or copy the old legacy 3D prototype world/content and its legacy gameplay canvas into `Game_Legecy.unity`
- keep `Assets/Game/Scenes/Game.unity` clean for the 2D/isometric production version
- preserve the production M01/isometric route through `Game.unity`
- do not delete legacy assets outright
- do not wait for QA/HCI approval; user will validate this task directly

Write:

`Design/AgentReports/2026-05-08_gameplay_game-legecy-scene-isolation.md`

If blocked, write:

`Design/AgentReports/2026-05-08_gameplay_game-legecy-scene-isolation-blocked.md`

Include exact command/Unity action attempted, failure reason, and unblock owner.

Do not commit or push.
