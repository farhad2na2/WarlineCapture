Lane
Gameplay

Task
Move `AIEconomySystem` diagnostics off the static `AILog` facade and onto the ECS AI diagnostic event buffer.

Files changed
- `Assets/Game/Scripts/Systems/AIEconomySystem.cs`
- `Assets/Tests/Editor/AIEconomyValidationTests.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`
- `Design/AgentReports/2026-05-22_gameplay-ai-economy-diagnostic-events.md`

Contracts touched
- `AIEconomySystem` now queues `AIDiagnosticLogComponent` entries instead of calling `AILog`.
- `AIDiagnosticLogFlushSystem` remains the shell-edge logging boundary that performs `Debug.Log`.
- `GameplayArchitectureContractTests` now asserts that `AIEconomySystem` remains off `AILog` and uses ECS diagnostic events.
- `AIEconomySystem` was removed from the static `AILog` debt allowlists.

User-visible behavior
- No intended gameplay behavior change.
- Existing `[AIEconomy]` validation logs still appear when faction economy diagnostics are emitted.

Validation run
- `rg -n "AILog\\." Assets/Game/Scripts/Systems/AIEconomySystem.cs`
- `git diff --check -- Assets/Game/Scripts/Systems/AIEconomySystem.cs Assets/Tests/Editor/AIEconomyValidationTests.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/rts_selection_system_responsibility_audit.md`
- Unity EditMode `AIEconomyValidationTests`
- Unity EditMode `GameplayArchitectureContractTests`
- Unity EditMode `AI`

Validation result
- `AIEconomySystem` has no remaining `AILog` calls.
- Diff whitespace check passed.
- First focused `AIEconomyValidationTests` run failed because the diagnostic queue entity was created during `SystemAPI.Query` iteration, which Unity rejects as a structural change while iterating.
- Fixed by resolving or creating the diagnostic queue before entering the economy query loop.
- `AIEconomyValidationTests`: passed 2/2 after the fix.
- `GameplayArchitectureContractTests`: passed 50/50.
- Broad Unity EditMode `AI`: passed 27/27.

Known gaps
- `AIFactionControlSystem` and `GameBootstrap` still remain on the temporary static `AILog` debt allowlist.
- The current ECS diagnostic event path stores formatted messages. Typed diagnostics can be introduced later if QA/performance tooling needs structured fields.

Cross-lane impacts
- QA keeps the existing `[AIEconomy]` log surface.
- Architecture now has six AI domain systems migrated to the ECS diagnostic event path: build planning, production, squad formation, target selection, combat orders, and economy.

Next recommended task
Migrate `AIFactionControlSystem` diagnostics to the same ECS diagnostic event path, then remove it from the static `AILog` debt allowlist.
