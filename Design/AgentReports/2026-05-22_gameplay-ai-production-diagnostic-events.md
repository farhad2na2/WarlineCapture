Lane
Gameplay

Task
Move `AIProductionSystem` diagnostics off the static `AILog` facade and onto the ECS AI diagnostic event buffer.

Files changed
- `Assets/Game/Scripts/Systems/AIProductionSystem.cs`
- `Assets/Tests/Editor/AIProductionValidationTests.cs`
- `Assets/Tests/Editor/AIEndToEndValidationTests.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`
- `Design/AgentReports/2026-05-22_gameplay-ai-production-diagnostic-events.md`

Contracts touched
- `AIProductionSystem` now queues `AIDiagnosticLogComponent` entries instead of calling `AILog`.
- `AIDiagnosticLogFlushSystem` remains the shell-edge logging boundary that performs `Debug.Log`.
- `GameplayArchitectureContractTests` now asserts that `AIProductionSystem` remains off `AILog` and uses ECS diagnostic events.
- `AIProductionSystem` was removed from the static `AILog` debt allowlists.

User-visible behavior
- No intended gameplay behavior change.
- Existing `[AIProduction]` validation logs still appear when AI queues unit production.

Validation run
- `rg -n "AILog\\." Assets/Game/Scripts/Systems/AIProductionSystem.cs`
- `git diff --check -- Assets/Game/Scripts/Systems/AIProductionSystem.cs Assets/Tests/Editor/AIProductionValidationTests.cs Assets/Tests/Editor/AIEndToEndValidationTests.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/rts_selection_system_responsibility_audit.md`
- Unity EditMode `AIProductionValidationTests`
- Unity EditMode `GameplayArchitectureContractTests`
- Unity EditMode `AI`

Validation result
- `AIProductionSystem` has no remaining `AILog` calls.
- Diff whitespace check passed.
- `AIProductionValidationTests`: passed 1/1.
- `GameplayArchitectureContractTests`: passed 46/46.
- Broad Unity EditMode `AI`: passed 23/23.

Known gaps
- `AICombatOrderSystem`, `AIEconomySystem`, `AIFactionControlSystem`, `AISquadSystem`, `AITargetingSystem`, and `GameBootstrap` still remain on the temporary static `AILog` debt allowlist.
- The current ECS diagnostic event path stores formatted messages. Typed diagnostics can be introduced later if QA/performance tooling needs structured fields.

Cross-lane impacts
- QA keeps the existing `[AIProduction]` log surface.
- Architecture now has two AI domain systems migrated to the ECS diagnostic event path: build planning and production.

Next recommended task
Migrate `AISquadSystem` diagnostics to the same ECS diagnostic event path, then remove it from the static `AILog` debt allowlist.
