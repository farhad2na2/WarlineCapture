Lane
Gameplay

Task
Move `AITargetingSystem` diagnostics off the static `AILog` facade and onto the ECS AI diagnostic event buffer.

Files changed
- `Assets/Game/Scripts/Systems/AITargetingSystem.cs`
- `Assets/Tests/Editor/AITargetingValidationTests.cs`
- `Assets/Tests/Editor/AIEndToEndValidationTests.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`
- `Design/AgentReports/2026-05-22_gameplay-ai-targeting-diagnostic-events.md`

Contracts touched
- `AITargetingSystem` now queues `AIDiagnosticLogComponent` entries instead of calling `AILog`.
- `AIDiagnosticLogFlushSystem` remains the shell-edge logging boundary that performs `Debug.Log`.
- `GameplayArchitectureContractTests` now asserts that `AITargetingSystem` remains off `AILog` and uses ECS diagnostic events.
- `AITargetingSystem` was removed from the static `AILog` debt allowlists.

User-visible behavior
- No intended gameplay behavior change.
- Existing `[AITarget]` validation logs still appear when AI squads select targets.

Validation run
- `rg -n "AILog\\." Assets/Game/Scripts/Systems/AITargetingSystem.cs`
- `git diff --check -- Assets/Game/Scripts/Systems/AITargetingSystem.cs Assets/Tests/Editor/AITargetingValidationTests.cs Assets/Tests/Editor/AIEndToEndValidationTests.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/rts_selection_system_responsibility_audit.md`
- Unity EditMode `AITargetingValidationTests`
- Unity EditMode `GameplayArchitectureContractTests`
- Unity EditMode `AI`

Validation result
- `AITargetingSystem` has no remaining `AILog` calls.
- Diff whitespace check passed.
- `AITargetingValidationTests`: passed 2/2.
- `GameplayArchitectureContractTests`: passed 48/48.
- Broad Unity EditMode `AI`: passed 25/25.

Known gaps
- `AICombatOrderSystem`, `AIEconomySystem`, `AIFactionControlSystem`, and `GameBootstrap` still remain on the temporary static `AILog` debt allowlist.
- The current ECS diagnostic event path stores formatted messages. Typed diagnostics can be introduced later if QA/performance tooling needs structured fields.

Cross-lane impacts
- QA keeps the existing `[AITarget]` log surface.
- Architecture now has four AI domain systems migrated to the ECS diagnostic event path: build planning, production, squad formation, and target selection.

Next recommended task
Migrate `AICombatOrderSystem` diagnostics to the same ECS diagnostic event path, then remove it from the static `AILog` debt allowlist.
