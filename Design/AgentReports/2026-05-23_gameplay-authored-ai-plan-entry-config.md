# WarlineCapture Gameplay Handoff

## Lane
Gameplay

## Task
Move AI fallback build/production ids from `AIPlanEntryStartupSystem` into an authored AI plan-entry config, leaving the helper as only the ECS buffer writer.

## Files changed
- `Assets/Game/Scripts/Configs/AIPlanEntryStartupConfig.cs`
- `Assets/Game/Scripts/Configs/AIPlanEntryStartupConfig.cs.meta`
- `Assets/Game/Configs/Scene/Game_AI_PlanEntry_Startup_Config.asset`
- `Assets/Game/Configs/Scene/Game_AI_PlanEntry_Startup_Config.asset.meta`
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Game/Scripts/Systems/AIStartupSystem.cs`
- `Assets/Game/Scripts/Systems/AIPlanEntryStartupSystem.cs`
- `Assets/Game/Scripts/Systems/AIPlanEntryStartupSystem.cs.meta`
- `Assets/Tests/Editor/AIStartupSystemValidationTests.cs`
- `Assets/Tests/Editor/AIPlanEntryStartupSystemValidationTests.cs`
- `Assets/Tests/Editor/AIPlanEntryStartupSystemValidationTests.cs.meta`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Assets/Game/Scenes/Game.unity`
- `Assets/Game/Scenes/Game2D.unity`
- `Assets/Game/Scenes/Game_2D.unity`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/gamebootstrap_responsibility_audit.md`
- `Design/AgentReports/2026-05-23_gameplay-authored-ai-plan-entry-config.md`

## Contracts touched
- Added authored `AIPlanEntryStartupConfig` for default fallback build and production ids.
- `AIPlanEntryStartupSystem` now writes ECS buffers from preferred ids and config-provided fallback ids only; it no longer hardcodes fallback ids.
- `AIStartupSystem` now accepts an `AIPlanEntryStartupConfig` and passes it through to the buffer writer.
- `GameBootstrap` now serializes and passes the AI plan-entry startup config.
- Architecture tests now reject fallback ids in both `AIStartupSystem` and `AIPlanEntryStartupSystem`, and require them in the authored config asset.

## User-visible behavior
- No intended gameplay behavior change.
- Default scenes now reference `Game_AI_PlanEntry_Startup_Config.asset`.
- Empty AI build preferences still fall back to `Tent_Regular`, `Building_Barrack`, `Building_OilPump`, `Building_Fuel_Bladder`, and `Building_Ammunition_Depot`.
- Empty AI production preferences still fall back to `Unit_Chr_Soldier_Male_02_Alt_04`.

## Validation run
- `git diff --check -- Assets/Game/Scripts/Configs/AIPlanEntryStartupConfig.cs Assets/Game/Scripts/Configs/AIPlanEntryStartupConfig.cs.meta Assets/Game/Configs/Scene/Game_AI_PlanEntry_Startup_Config.asset Assets/Game/Configs/Scene/Game_AI_PlanEntry_Startup_Config.asset.meta Assets/Game/Scripts/Bootstrap/GameBootstrap.cs Assets/Game/Scripts/Systems/AIStartupSystem.cs Assets/Game/Scripts/Systems/AIPlanEntryStartupSystem.cs Assets/Tests/Editor/AIStartupSystemValidationTests.cs Assets/Tests/Editor/AIPlanEntryStartupSystemValidationTests.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/gamebootstrap_responsibility_audit.md Assets/Game/Scenes/Game.unity Assets/Game/Scenes/Game2D.unity Assets/Game/Scenes/Game_2D.unity`
- Unity EditMode `AIPlanEntryStartupSystemValidationTests`
- Unity EditMode `AIStartupSystemValidationTests`
- Unity EditMode `GameplayArchitectureContractTests`
- Unity EditMode `AI`

## Validation result
- `git diff --check`: passed.
- `AIPlanEntryStartupSystemValidationTests`: passed `4/4`.
- `AIStartupSystemValidationTests`: passed `1/1`.
- `GameplayArchitectureContractTests`: passed `61/61`.
- `AI`: passed `38/38`.

## Known gaps
- `AIStartupSystem` still owns broad AI config projection across economy, faction control, build plans, production plans, squad plans, and target priorities.
- The plan-entry config is scene-level authored data; a future pass can split faction-specific fallback lists if design wants different fallback plans per AI role or faction.

## Cross-lane impacts
- Scene wiring changed in `Game`, `Game2D`, and `Game_2D` only to reference the new AI plan-entry config asset.
- No Art or UI behavior was intentionally changed.

## Next recommended task
Split the next `AIStartupSystem` responsibility: move AI economy startup projection or faction-control startup projection into a narrower startup system with its own focused contract test.
