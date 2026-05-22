Lane
Gameplay

Task
Move `GameBootstrap` AI config diagnostics off the temporary static `AILog` compatibility facade and onto the ECS AI diagnostic event buffer.

Files changed
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Game/Scripts/Components/AIDiagnosticLogComponents.cs`
- `Assets/Game/Scripts/Systems/AIDiagnosticLogFlushSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`
- `Design/AgentReports/2026-05-22_gameplay-gamebootstrap-ai-config-diagnostic-events.md`

Contracts touched
- `GameBootstrap` no longer calls `AILog` for AI config diagnostics.
- `GameBootstrap` now queues `AIDiagnosticLogComponent` entries and flushes them through `AIDiagnosticLogFlushSystem` at gameplay start.
- `AIDiagnosticLogComponent` now carries a severity byte so missing AI config diagnostics can remain warnings.
- `GameplayArchitectureContractTests` now has an empty static `AILog` call-site debt allowlist and asserts that `GameBootstrap` AI config diagnostics use ECS diagnostic events.

User-visible behavior
- No intended gameplay behavior change.
- Existing `[AIConfig]`, `[AIConfigSummary]`, and `[AISettings]` startup diagnostics remain visible when AI diagnostics are enabled.
- Missing AI config diagnostics still emit as warnings through the ECS flush system.

Validation run
- `rg -n "AILog\\." Assets/Game/Scripts/Bootstrap/GameBootstrap.cs Assets/Game/Scripts/Systems Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `git diff --check -- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs Assets/Game/Scripts/Components/AIDiagnosticLogComponents.cs Assets/Game/Scripts/Systems/AIDiagnosticLogFlushSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/rts_selection_system_responsibility_audit.md`
- Unity EditMode `GameplayArchitectureContractTests`
- Unity EditMode `AI`

Validation result
- `GameBootstrap` has no remaining `AILog` calls.
- Production static `AILog` call-site debt allowlist is now empty.
- Diff whitespace check passed.
- `GameplayArchitectureContractTests`: passed 52/52.
- Broad Unity EditMode `AI`: passed 29/29.

Known gaps
- The temporary `AILog` facade type still exists but has no production callers outside its own file.
- The current ECS diagnostic event path still stores formatted messages. Typed diagnostics can be introduced later if QA/performance tooling needs structured fields.

Cross-lane impacts
- QA keeps the existing AI startup diagnostic strings.
- Architecture now has all tracked AI diagnostic call sites migrated off the static `AILog` facade.

Next recommended task
Remove the now-unused temporary static `AILog` compatibility facade once no external validation or tooling still depends on that type existing.
