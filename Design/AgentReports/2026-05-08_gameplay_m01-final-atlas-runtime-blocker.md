Lane:
Gameplay

Task:
Continue `Design/AgentTasks/gameplay_current.md` after PM follow-up review of the M01 opening-control and golden-playthrough handoff.

Files changed:
- `Design/AgentReports/2026-05-08_gameplay_m01-final-atlas-runtime-blocker.md`

Contracts touched:
- M01 runtime ids remain `unit.player.rifle_squad_01` and `unit.enemy.patrol_01`.
- Current accepted gameplay handoff remains `Design/AgentReports/2026-05-08_gameplay_m01-opening-control-window.md`.
- PM follow-up review remains `Design/AgentReports/2026-05-08_pm_gameplay-m01-opening-control-window-followup-review.md`.

User-visible behavior:
- No runtime behavior changed in this heartbeat.
- Existing validated behavior still stands: M01 first-control survival window, public Campaign golden path through result popup, infantry-only scope, ECS presenter state ownership, and no visible legacy ECS mesh/model bodies.

Validation run:
- Read `Design/AgentTasks/gameplay_current.md`.
- Read `Design/AgentReports/2026-05-08_pm_gameplay-m01-opening-control-window-followup-review.md`.
- Inspected available runtime presentation paths and assets:
  - `Assets/Game/Scripts/Systems/UnitImpostorRenderSystem.cs`
  - `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
  - `Assets/Game/Scripts/Campaign/Chapter01M01SpritePresenterCatalog.cs`
  - `Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs`
  - `Packages/manifest.json`
  - `Assets/Game/Art/Generated/2DISO/Units/Unit_Chr_Soldier_Male_02/SpriteSheets/Transparent/Unit_Chr_Soldier_Male_02_FullSetup_4Facing_8State_UnityGrid_960x1680.png`

Validation result:
- Blocked for final Gate 4 runtime-presentation acceptance.
- PM accepted the playable-loop/golden-path proof but still rejected final presentation readiness because the current runtime uses `MissionRuntimeSpriteRendererRuntime`, a temporary ECS-driven Unity `SpriteRenderer` adapter.
- The repo has `com.unity.entities.graphics` available and an existing `UnitImpostorRenderSystem`, but that system is a camera-side `Graphics.DrawMeshInstanced` impostor path keyed from prefab/source data. It does not consume the M01 `MissionRuntimeSpritePresenter` idle/move/attack/damaged/destroyed state contract and is not a direct replacement for `MissionRuntimeSpriteRendererRuntime`.
- A 4-facing/8-state infantry sprite sheet exists, but it is not currently wired into the Chapter 1 M01 tactical manifest/atlas contract as final or milestone-approved per-state frames for `unit.player.rifle_squad_01` and `unit.enemy.patrol_01`.

Known gaps:
- Exact blocker: final or milestone-approved multi-frame M01 infantry atlas frames are not available through the Chapter 1 M01 runtime manifest/contract for:
  - `unit.player.rifle_squad_01.idle`
  - `unit.player.rifle_squad_01.move`
  - `unit.player.rifle_squad_01.attack`
  - `unit.player.rifle_squad_01.damaged`
  - `unit.enemy.patrol_01.idle`
  - `unit.enemy.patrol_01.move`
  - `unit.enemy.patrol_01.attack`
  - `unit.enemy.patrol_01.damaged`
- Exact missing file/report/command: no PM/user waiver report accepting the temporary ECS-driven `SpriteRenderer` adapter for Gate 4, and no Art/PM report approving the existing `Unit_Chr_Soldier_Male_02_FullSetup_4Facing_8State_UnityGrid_960x1680.png` as the M01 milestone atlas source for both M01 player and enemy infantry.
- Exact owner lane: PM owns waiver/acceptance. Art owns final or milestone-approved atlas frames. Gameplay owns wiring the approved frames into a DOTS-compatible M01 presenter renderer after the asset/waiver decision.
- Whether another lane can continue: yes. PM can decide waiver vs final atlas route. Art can approve or produce the M01 infantry atlas frames. QA/HCI should not perform final Gate 4 acceptance until PM resolves the presentation decision, but can run early blocker classification if PM requests it.

Cross-lane impacts:
- UI and Support/FTUE have no new action unless QA/HCI finds a concrete regression.
- QA/HCI final Gate 4 remains blocked by PM's presentation decision.
- Gameplay should not start unrelated M02-M05 or broad visual polish.

Next recommended task:
PM should either:
- approve the temporary ECS-driven `SpriteRenderer` adapter as a milestone waiver for Gate 4, or
- route Art to approve/produce final M01 multi-frame infantry atlas frames, then route Gameplay to replace `MissionRuntimeSpriteRendererRuntime` with a DOTS-compatible renderer that consumes the existing `MissionRuntimeSpritePresenter` state ids.
