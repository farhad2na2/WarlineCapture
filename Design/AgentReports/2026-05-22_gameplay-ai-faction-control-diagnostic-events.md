Lane
Gameplay

Task
Move `AIFactionControlSystem` diagnostics off the static `AILog` facade and onto the ECS AI diagnostic event buffer.

Files changed
- `Assets/Game/Scripts/Systems/AIFactionControlSystem.cs`
- `Assets/Tests/Editor/AIControlModeValidationTests.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`
- `Design/AgentReports/2026-05-22_gameplay-ai-faction-control-diagnostic-events.md`

Contracts touched
- `AIFactionControlSystem` now queues `AIDiagnosticLogComponent` entries instead of calling `AILog`.
- `AIDiagnosticLogFlushSystem` remains the shell-edge logging boundary that performs `Debug.Log`.
- `GameplayArchitectureContractTests` now asserts that `AIFactionControlSystem` remains off `AILog` and uses ECS diagnostic events.
- `AIFactionControlSystem` was removed from the static `AILog` debt allowlists.

User-visible behavior
- No intended gameplay behavior change.
- Existing `[AIControlMode]` validation logs still appear when faction control diagnostics are emitted.

Validation run
- `rg -n "AILog\\." Assets/Game/Scripts/Systems/AIFactionControlSystem.cs`
- `git diff --check -- Assets/Game/Scripts/Systems/AIFactionControlSystem.cs Assets/Tests/Editor/AIControlModeValidationTests.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/rts_selection_system_responsibility_audit.md`
- Unity EditMode `AIControlModeValidationTests`
- Unity EditMode `GameplayArchitectureContractTests`
- Unity EditMode `AI`

Validation result
- `AIFactionControlSystem` has no remaining `AILog` calls.
- Diff whitespace check passed.
- First focused `AIControlModeValidationTests` run failed because the diagnostic queue entity was created after reading the `FactionControlEntry` singleton buffer, invalidating that buffer handle through a structural change.
- Fixed by resolving or creating the diagnostic queue before reading the faction-control buffer.
- `AIControlModeValidationTests`: passed 1/1 after the fix.
- `GameplayArchitectureContractTests`: passed 51/51.
- Broad Unity EditMode `AI`: passed 28/28.

Known gaps
- `GameBootstrap` still remains on the temporary static `AILog` debt allowlist for AI config diagnostics.
- The current ECS diagnostic event path stores formatted messages. Typed diagnostics can be introduced later if QA/performance tooling needs structured fields.

Cross-lane impacts
- QA keeps the existing `[AIControlMode]` log surface.
- Architecture now has seven AI domain systems migrated to the ECS diagnostic event path: build planning, production, squad formation, target selection, combat orders, economy, and faction control.

Next recommended task
Migrate `GameBootstrap` AI config diagnostics away from the temporary static `AILog` compatibility facade.
