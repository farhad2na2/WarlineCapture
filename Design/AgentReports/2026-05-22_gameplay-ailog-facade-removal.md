Lane
Gameplay

Task
Remove the now-unused temporary static `AILog` compatibility facade after confirming no external tooling still references it.

Files changed
- `Assets/Game/Scripts/Systems/AILog.cs`
- `Assets/Game/Scripts/Systems/AILog.cs.meta`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`
- `README.md`
- `Design/AgentReports/2026-05-22_gameplay-ailog-facade-removal.md`

Contracts touched
- Deleted the retired `AILog` static facade type and its Unity meta file.
- Removed `AILog.cs` from the static log facade debt allowlist.
- Added architecture contract coverage that fails if `Assets/Game/Scripts/Systems/AILog.cs` is reintroduced.
- Updated architecture docs and README language so `AILog` is no longer described as grandfathered debt.

User-visible behavior
- No intended gameplay behavior change.
- AI diagnostics continue through ECS diagnostic events and `AIDiagnosticLogFlushSystem`.

Validation run
- `rg -n "\\bAILog\\b|AILog\\." Tools`
- `rg -n "\\bAILog\\b|AILog\\." Tools Packages ProjectSettings Assets -g '!Library/**' -g '!Temp/**' -g '!Logs/**'`
- `git diff --check -- Assets/Game/Scripts/Systems/AILog.cs Assets/Game/Scripts/Systems/AILog.cs.meta Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/rts_selection_system_responsibility_audit.md README.md`
- Unity EditMode `GameplayArchitectureContractTests`
- Unity EditMode `AI`

Validation result
- No `AILog` references were found in `Tools`.
- No production `AILog` call sites remain; remaining repo references are architecture tests/docs/history.
- Diff whitespace check passed.
- `GameplayArchitectureContractTests`: passed 53/53.
- Broad Unity EditMode `AI`: passed 30/30.

Known gaps
- `RuntimeDiagnosticsSystem` still mirrors legacy `InitialUnitsRuntimeState.VerboseAILogs` and `TransportBoardingDiagnostics` into `RuntimeDiagnosticsStateComponent`; removing that compatibility mirror is a separate migration.
- Existing non-AI direct `Debug.Log*` diagnostics remain in several gameplay systems and should be migrated by domain slice.

Cross-lane impacts
- Architecture no longer allows `AILog` as a runtime static logging facade.
- QA and tooling should rely on ECS diagnostic event output rather than the removed facade.

Next recommended task
Continue migrating remaining direct `Debug.Log*` gameplay diagnostics into ECS diagnostic event buffers or shell-injected logging services by domain slice, starting with transport boarding diagnostics if that lane still needs structured log events.
