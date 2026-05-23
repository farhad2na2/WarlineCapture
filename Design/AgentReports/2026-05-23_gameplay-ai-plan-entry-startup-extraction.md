# WarlineCapture Gameplay Handoff

## Lane
Gameplay

## Task
Extract AI default build/production fallback entries out of `AIStartupSystem` into a narrower startup helper.

## Files changed
- `Assets/Game/Scripts/Systems/AIStartupSystem.cs`
- `Assets/Game/Scripts/Systems/AIPlanEntryStartupSystem.cs`
- `Assets/Game/Scripts/Systems/AIPlanEntryStartupSystem.cs.meta`
- `Assets/Tests/Editor/AIPlanEntryStartupSystemValidationTests.cs`
- `Assets/Tests/Editor/AIPlanEntryStartupSystemValidationTests.cs.meta`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/gamebootstrap_responsibility_audit.md`
- `Design/AgentReports/2026-05-23_gameplay-ai-plan-entry-startup-extraction.md`

## Contracts touched
- `AIPlanEntryStartupSystem` now owns preferred/default build and production plan-entry buffer population.
- `AIStartupSystem` still creates and configures AI plan components, but delegates entry population through `AIPlanEntryStartupSystem`.
- Architecture tests now reject hardcoded default building/unit ids inside `AIStartupSystem`.
- Architecture docs now state that AI default build and production fallback entries are owned by `AIPlanEntryStartupSystem`.

## User-visible behavior
- No intended gameplay behavior change.
- AI build plans still fall back to `Tent_Regular`, `Building_Barrack`, `Building_OilPump`, `Building_Fuel_Bladder`, and `Building_Ammunition_Depot` when preferred building ids are empty.
- AI production plans still fall back to `Unit_Chr_Soldier_Male_02_Alt_04` when preferred unit/vehicle ids are empty.

## Validation run
- `git diff --check -- Assets/Game/Scripts/Systems/AIStartupSystem.cs Assets/Game/Scripts/Systems/AIPlanEntryStartupSystem.cs Assets/Game/Scripts/Systems/AIPlanEntryStartupSystem.cs.meta Assets/Tests/Editor/AIPlanEntryStartupSystemValidationTests.cs Assets/Tests/Editor/AIPlanEntryStartupSystemValidationTests.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/gamebootstrap_responsibility_audit.md`
- Unity EditMode `AIPlanEntryStartupSystemValidationTests`
- Unity EditMode `AIStartupSystemValidationTests`
- Unity EditMode `GameplayArchitectureContractTests`
- Unity EditMode `AI`

## Validation result
- `git diff --check`: passed.
- `AIPlanEntryStartupSystemValidationTests`: passed `4/4`.
- `AIStartupSystemValidationTests`: passed `1/1`.
- `GameplayArchitectureContractTests`: passed `61/61` after syncing updated architecture docs into the validation clone.
- `AI`: passed `38/38`.

## Known gaps
- The fallback ids are isolated in `AIPlanEntryStartupSystem`, but they are still code-authored rather than stored in a ScriptableObject/config asset.
- `AIStartupSystem` still owns several AI config projection responsibilities; it is smaller but still broad.

## Cross-lane impacts
- No Art, UI, or scene files were modified for this slice.
- Existing unrelated visual-lock/generated files were left untouched.

## Next recommended task
Move AI default plan fallback ids from `AIPlanEntryStartupSystem` into authored AI config assets or a dedicated AI plan-entry config, then leave `AIPlanEntryStartupSystem` as the ECS buffer writer only.
