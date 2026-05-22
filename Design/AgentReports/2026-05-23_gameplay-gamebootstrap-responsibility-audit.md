Lane
Gameplay

Task
Create a bootstrap responsibility audit and architecture ratchet before refactoring the large `GameBootstrap`.

Files changed
- `Design/Architecture/gamebootstrap_responsibility_audit.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/AgentReports/2026-05-23_gameplay-gamebootstrap-responsibility-audit.md`

Contracts touched
- Added a required bootstrap migration audit document.
- Updated the gameplay SOLID/ECS contract to point bootstrap work at the audit.
- Added architecture tests that require the audit to exist and cover bootstrap responsibility buckets.
- Added a ratchet that prevents new AI, mission, tactical, camera/framing, faction economy/control, and spawn-cell policy method names from being added to `GameBootstrap` beyond the current legacy baseline.
- Added a direct `Debug.Log*` count/category ratchet so bootstrap diagnostics debt cannot grow before it is moved to a logging boundary.

User-visible behavior
- No intended gameplay behavior change.
- No bootstrap runtime behavior was refactored in this slice.

Validation run
- `git diff --check -- Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/gamebootstrap_responsibility_audit.md`
- Unity EditMode `GameplayArchitectureContractTests`

Validation result
- Diff whitespace check passed.
- `GameplayArchitectureContractTests`: passed 57/57.

Known gaps
- `GameBootstrap` remains large and still owns legacy domain behavior. This slice intentionally mapped and guarded the debt before changing behavior.
- The ratchet blocks obvious new domain-policy method names, but existing debt still needs slice-by-slice extraction.
- Existing direct bootstrap performance diagnostics remain as baseline debt until moved to a diagnostics/logging boundary.

Cross-lane impacts
- Gameplay, UI, and QA should use `Design/Architecture/gamebootstrap_responsibility_audit.md` as the source map for future bootstrap refactors.
- New bootstrap code should be composition-only; domain policy belongs in ECS systems/configs or shell services.

Next recommended task
Start the first bootstrap extraction slice: move AI startup policy and plan mutation out of `GameBootstrap` into an ECS `AIStartupSystem` while preserving the current generated `FactionControlEntry`, `AIBuildPlan`, `AIProductionPlan`, `AISquadPlan`, and `AITargetPrioritySetting` data.
