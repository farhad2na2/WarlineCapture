# PM Review: Art/Atlas Individual Soldier Frame Review

Date: 2026-05-08
Status: accepted; routes concrete source fix to Gameplay

## Lane

PM

## Task

Review Art/Atlas's focused individual-soldier frame/source report after PM rejected the QA selected first-control captures.

## Files changed

- `Design/AgentReports/2026-05-08_pm_art-atlas-individual-soldier-frame-review.md`
- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/gameplay_pm_message.md`

## Contracts touched

- No source contract changed.
- Confirms the M01 readability blocker is not only formation spacing: the current runtime source maps to a squad/group sprite, so duplicating it four times creates four mini-squads.
- Gate 4 remains blocked until Gameplay rewires M01 infantry presentation to individual-soldier atlas cells and QA/HCI reruns selected first-control captures.

## User-visible behavior

No runtime behavior changed by PM. Expected next user-visible fix:

- player squad reads as four individual soldiers
- not four duplicated cluster sprites
- selected markers remain small and grounded under each soldier

## Validation run

PM reviewed:

- `Design/AgentReports/2026-05-08_art-atlas_m01-individual-soldier-frame-review.md`
- `Design/AgentReports/2026-05-08_pm_qa-hci-rejected-art-rerun-review.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/gameplay_pm_message.md`

## Validation result

Accepted.

Art/Atlas identified the concrete source issue:

- `unit.player.rifle_squad_01` and `unit.enemy.patrol_01` still resolve to `Assets/Game/Art/Generated/IsometricMaps/TacticalProductionBatch_A/Sprites/infantry_squad.png`.
- That PNG is a squad/group image.
- `Chapter01M01SpriteAssetResolver` falls back state ids to the base asset because state-specific manifest entries are missing.
- Gameplay must use individual `Unit_Chr_Soldier_Male_02` cells from the temporary setup sheet instead.

## Known gaps

- Gameplay report `Design/AgentReports/2026-05-08_gameplay_m01-soldier-readability-selection-fix.md` is still missing.
- QA/HCI cannot rerun selected readability until Gameplay lands the source/layout/marker fix.
- Final Art/Atlas gaps remain: multi-frame run/walk loops, final enemy variant, final impact/death VFX.

## Cross-lane impacts

- Gameplay owns the source remap and runtime proof.
- Art/Atlas waits unless Gameplay discovers a missing sprite/manifest blocker.
- QA/HCI waits for Gameplay.
- UI and Support/FTUE have no current action.
- User does not need to review yet.

## Next recommended task

Gameplay should immediately deliver:

`Design/AgentReports/2026-05-08_gameplay_m01-soldier-readability-selection-fix.md`

Required source mapping for the next temporary pass:

- `unit.player.rifle_squad_01.idle` -> `Unit_Chr_Soldier_Male_02_Idle_SE`
- `unit.player.rifle_squad_01.move` -> `Unit_Chr_Soldier_Male_02_Run_SE` or `Unit_Chr_Soldier_Male_02_Walk_SE`
- `unit.player.rifle_squad_01.attack` -> `Unit_Chr_Soldier_Male_02_Aim_SE` or `Unit_Chr_Soldier_Male_02_Fire_SE`
- `unit.player.rifle_squad_01.damaged` -> `Unit_Chr_Soldier_Male_02_Hit_SE`
- destroyed/death stays atlas-state based, not separate `Destroyed` child.
