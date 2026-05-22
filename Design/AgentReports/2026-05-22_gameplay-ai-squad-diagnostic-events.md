Lane
Gameplay

Task
Move `AISquadSystem` diagnostics off the static `AILog` facade and onto the ECS AI diagnostic event buffer.

Files changed
- `Assets/Game/Scripts/Systems/AISquadSystem.cs`
- `Assets/Tests/Editor/AISquadValidationTests.cs`
- `Assets/Tests/Editor/AIEndToEndValidationTests.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`
- `Design/AgentReports/2026-05-22_gameplay-ai-squad-diagnostic-events.md`

Contracts touched
- `AISquadSystem` now queues `AIDiagnosticLogComponent` entries instead of calling `AILog`.
- `AIDiagnosticLogFlushSystem` remains the shell-edge logging boundary that performs `Debug.Log`.
- `GameplayArchitectureContractTests` now asserts that `AISquadSystem` remains off `AILog` and uses ECS diagnostic events.
- `AISquadSystem` was removed from the static `AILog` debt allowlists.

User-visible behavior
- No intended gameplay behavior change.
- Existing `[AISquad]` validation logs still appear when AI forms squads.

Validation run
- `rg -n "AILog\\." Assets/Game/Scripts/Systems/AISquadSystem.cs`
- `git diff --check -- Assets/Game/Scripts/Systems/AISquadSystem.cs Assets/Tests/Editor/AISquadValidationTests.cs Assets/Tests/Editor/AIEndToEndValidationTests.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/rts_selection_system_responsibility_audit.md`
- Unity EditMode `AISquadValidationTests`
- Unity EditMode `GameplayArchitectureContractTests`
- Unity EditMode `AI`

Validation result
- `AISquadSystem` has no remaining `AILog` calls.
- Diff whitespace check passed.
- `AISquadValidationTests`: passed 1/1.
- `GameplayArchitectureContractTests`: passed 47/47.
- Broad Unity EditMode `AI`: passed 24/24.

Known gaps
- `AICombatOrderSystem`, `AIEconomySystem`, `AIFactionControlSystem`, `AITargetingSystem`, and `GameBootstrap` still remain on the temporary static `AILog` debt allowlist.
- The current ECS diagnostic event path stores formatted messages. Typed diagnostics can be introduced later if QA/performance tooling needs structured fields.

Cross-lane impacts
- QA keeps the existing `[AISquad]` log surface.
- Architecture now has three AI domain systems migrated to the ECS diagnostic event path: build planning, production, and squad formation.

Next recommended task
Migrate `AITargetingSystem` diagnostics to the same ECS diagnostic event path, then remove it from the static `AILog` debt allowlist.
