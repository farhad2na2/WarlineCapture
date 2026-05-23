# WarlineCapture Gameplay Handoff

## Lane
Gameplay

## Task
Split AI economy startup projection out of `AIStartupSystem`.

## Files changed
- `Assets/Game/Scripts/Systems/AIStartupSystem.cs`
- `Assets/Game/Scripts/Systems/FactionEconomyStartupSystem.cs`
- `Assets/Game/Scripts/Systems/FactionEconomyStartupSystem.cs.meta`
- `Assets/Tests/Editor/FactionEconomyStartupSystemValidationTests.cs`
- `Assets/Tests/Editor/FactionEconomyStartupSystemValidationTests.cs.meta`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/gamebootstrap_responsibility_audit.md`
- `Design/AgentReports/2026-05-23_gameplay-faction-economy-startup-extraction.md`

## Contracts touched
- `FactionEconomyStartupSystem` now owns startup projection for `FactionEconomy` and `FactionEconomyPolicy`.
- `AIStartupSystem` delegates economy startup projection through `FactionEconomyStartupSystem` and no longer constructs `FactionEconomy` or `FactionEconomyPolicy` directly.
- Architecture tests now require the economy startup boundary and reject direct economy component construction in `AIStartupSystem`.
- Architecture docs now state that faction economy startup projection belongs to `FactionEconomyStartupSystem`.

## User-visible behavior
- No intended gameplay behavior change.
- AI economy startup still projects starting money, enabled policy, income multiplier, sell prices, and sell interval from existing AI controller config and runtime settings.

## Validation run
- `git diff --check -- Assets/Game/Scripts/Systems/AIStartupSystem.cs Assets/Game/Scripts/Systems/FactionEconomyStartupSystem.cs Assets/Game/Scripts/Systems/FactionEconomyStartupSystem.cs.meta Assets/Tests/Editor/FactionEconomyStartupSystemValidationTests.cs Assets/Tests/Editor/FactionEconomyStartupSystemValidationTests.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/gamebootstrap_responsibility_audit.md`
- Unity EditMode `FactionEconomyStartupSystemValidationTests`
- Unity EditMode `AIStartupSystemValidationTests`
- Unity EditMode `GameplayArchitectureContractTests`
- Unity EditMode `AI`

## Validation result
- `git diff --check`: passed.
- `FactionEconomyStartupSystemValidationTests`: passed `3/3`.
- `AIStartupSystemValidationTests`: passed `1/1`.
- `GameplayArchitectureContractTests`: passed `62/62`.
- `AI`: passed `39/39`.

## Known gaps
- `AIStartupSystem` still owns faction-control, build-plan, production-plan, squad-plan, and target-priority startup projection.
- Economy startup still reads `AISettingsRuntimeState`; a future pass can move settings into ECS/config data if the static runtime settings boundary is retired.

## Cross-lane impacts
- No Art, UI, or scene files were intentionally modified in this slice.
- Existing unrelated dirty files from other lanes were not touched.

## Next recommended task
Split faction-control startup projection from `AIStartupSystem` into a dedicated `AIFactionControlStartupSystem`, keeping the player-auto result contract stable.
