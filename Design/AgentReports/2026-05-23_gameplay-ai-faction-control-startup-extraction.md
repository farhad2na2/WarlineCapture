# WarlineCapture Handoff Report

## Lane
Gameplay

## Task
Split faction-control startup projection into `AIFactionControlStartupSystem` while keeping the `AIStartupSystem.Result` player-auto contract stable.

## Files changed
- `Assets/Game/Scripts/Systems/AIFactionControlStartupSystem.cs`
- `Assets/Game/Scripts/Systems/AIFactionControlStartupSystem.cs.meta`
- `Assets/Game/Scripts/Systems/AIStartupSystem.cs`
- `Assets/Tests/Editor/AIFactionControlStartupSystemValidationTests.cs`
- `Assets/Tests/Editor/AIFactionControlStartupSystemValidationTests.cs.meta`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/gamebootstrap_responsibility_audit.md`
- `Design/AgentReports/2026-05-23_gameplay-ai-faction-control-startup-extraction.md`

## Contracts touched
- `AIStartupSystem` now delegates `FactionControlConfigTag` and `FactionControlEntry` projection to `AIFactionControlStartupSystem`.
- `AIStartupSystem.Result` remains the external player-auto result contract; the new faction-control startup result is converted back to that public result.
- Architecture contract now forbids direct faction-control singleton/buffer mutation in `AIStartupSystem`.
- Gameplay architecture tests now guard the new boundary.

## User-visible behavior
No intended gameplay behavior change. AI faction-control startup should produce the same player and enemy control entries as before, including default fallback player/enemy entries and player-auto enabled state.

## Validation run
- `git diff --check -- Assets/Game/Scripts/Systems/AIStartupSystem.cs Assets/Game/Scripts/Systems/AIFactionControlStartupSystem.cs Assets/Game/Scripts/Systems/AIFactionControlStartupSystem.cs.meta Assets/Tests/Editor/AIFactionControlStartupSystemValidationTests.cs Assets/Tests/Editor/AIFactionControlStartupSystemValidationTests.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/gamebootstrap_responsibility_audit.md`
- Unity EditMode `AIFactionControlStartupSystemValidationTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
- Unity EditMode `AIStartupSystemValidationTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
- Unity EditMode `GameplayArchitectureContractTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
- Unity EditMode `AI` filter in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`

## Validation result
- `git diff --check`: passed
- `AIFactionControlStartupSystemValidationTests`: passed, 3/3
- `AIStartupSystemValidationTests`: passed, 1/1
- `GameplayArchitectureContractTests`: passed, 63/63
- `AI`: passed, 43/43

## Known gaps
- `AIStartupSystem` still owns build plan, production plan, squad plan, target-priority projection, and startup diagnostics. Those remain separate follow-up slices.
- `ShouldIncludeAIConfig` filtering is duplicated in startup slice systems until AI config projection is moved into authored ECS config data or a shared non-static ECS boundary.

## Cross-lane impacts
- No UI, art, scene, or PM task-file changes.
- Validation clone `/Users/farhad/Projects/WarlineCapture-CodexUnity1` was updated with the touched scripts/tests/docs for Unity test execution.

## Next recommended task
Split the next startup projection out of `AIStartupSystem`, preferably target-priority or squad startup, with the same pattern: dedicated `*System`, focused validation, and architecture guardrail.
