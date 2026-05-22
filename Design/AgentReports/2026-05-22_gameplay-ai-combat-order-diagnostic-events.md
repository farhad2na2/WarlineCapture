Lane
Gameplay

Task
Move `AICombatOrderSystem` diagnostics off the static `AILog` facade and onto the ECS AI diagnostic event buffer.

Files changed
- `Assets/Game/Scripts/Systems/AICombatOrderSystem.cs`
- `Assets/Tests/Editor/AICombatOrderValidationTests.cs`
- `Assets/Tests/Editor/AIEndToEndValidationTests.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`
- `Design/AgentReports/2026-05-22_gameplay-ai-combat-order-diagnostic-events.md`

Contracts touched
- `AICombatOrderSystem` now queues `AIDiagnosticLogComponent` entries instead of calling `AILog`.
- `AIDiagnosticLogFlushSystem` remains the shell-edge logging boundary that performs `Debug.Log`.
- `GameplayArchitectureContractTests` now asserts that `AICombatOrderSystem` remains off `AILog` and uses ECS diagnostic events.
- `AICombatOrderSystem` was removed from the static `AILog` debt allowlists.

User-visible behavior
- No intended gameplay behavior change.
- Existing `[AICombat]` validation logs still appear when AI squads issue attack orders.

Validation run
- `rg -n "AILog\\." Assets/Game/Scripts/Systems/AICombatOrderSystem.cs`
- `git diff --check -- Assets/Game/Scripts/Systems/AICombatOrderSystem.cs Assets/Tests/Editor/AICombatOrderValidationTests.cs Assets/Tests/Editor/AIEndToEndValidationTests.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/rts_selection_system_responsibility_audit.md`
- Unity EditMode `AICombatOrderValidationTests`
- Unity EditMode `GameplayArchitectureContractTests`
- Unity EditMode `AI`

Validation result
- `AICombatOrderSystem` has no remaining `AILog` calls.
- Diff whitespace check passed.
- `AICombatOrderValidationTests`: passed 2/2.
- `GameplayArchitectureContractTests`: passed 49/49.
- Broad Unity EditMode `AI`: passed 26/26.

Known gaps
- `AIEconomySystem`, `AIFactionControlSystem`, and `GameBootstrap` still remain on the temporary static `AILog` debt allowlist.
- The current ECS diagnostic event path stores formatted messages. Typed diagnostics can be introduced later if QA/performance tooling needs structured fields.

Cross-lane impacts
- QA keeps the existing `[AICombat]` log surface.
- Architecture now has five AI domain systems migrated to the ECS diagnostic event path: build planning, production, squad formation, target selection, and combat orders.

Next recommended task
Migrate `AIEconomySystem` diagnostics to the same ECS diagnostic event path, then remove it from the static `AILog` debt allowlist.
