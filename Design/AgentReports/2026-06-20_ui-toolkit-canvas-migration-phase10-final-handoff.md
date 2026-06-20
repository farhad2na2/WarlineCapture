# UI Toolkit Canvas Migration Phase 10 Final Handoff

Lane
UI

Task
UI Toolkit Canvas Replacement Phase 10 - remove active Canvas runtime dependency for migrated UI Toolkit surfaces, lock architecture validation, classify the remaining managed apply edge, and prepare fallback-removal approval.

Files changed
- `Assets/Game/Scripts/UI/Shell/Ecs/UiDiagnosticsReadModelSystem.cs`
- `Assets/Tests/Editor/UiToolkitCanvasMigrationValidationTests.cs`
- `Design/Architecture/ui_toolkit_canvas_replacement_plan.md`
- `Design/AgentReports/2026-06-20_ui-toolkit-canvas-migration-phase10-final-handoff.md`

Contracts touched
- `RuntimeUiConfig` remains the mode switch: Canvas fallback stays default until explicit approval changes it.
- `MenuBootstrapView.ApplyRuntimeUiMode()` now has validation coverage proving UI Toolkit mode disables the legacy Canvas renderer path, `UIShellEcsPresentationSystem`, `UIShellContentView`, and `UIRouterView`.
- `UiToolkitShellApplySystem` is classified as the only intentional managed UI Toolkit presentation boundary.
- `UiDiagnosticsReadModelSystem` is now an `ISystem`; managed Unity log subscription is isolated in `UiDiagnosticsRuntimeLogBuffer`.
- UI Toolkit runtime assembly boundaries are locked by validation across `Game.UI.Contracts`, `Game.UI.Shell.Contracts.Ecs`, `Game.UI.Shell.Ecs`, and `Game.UI.Toolkit`.

User-visible behavior
- No user-visible UI behavior was intentionally changed in this phase.
- In UI Toolkit mode, migrated UI surfaces should no longer require direct runtime references to legacy Canvas view classes.
- Canvas prefabs and fallback code are intentionally still present and archived as fallback. They were not deleted because fallback removal requires explicit user approval.

Validation run
- Unity batchmode: `UiToolkitCanvasMigrationValidationTests.RunBatchValidation`.
- Log: `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.

Validation result
- Passed: `[UiToolkitCanvasMigrationValidation] result=Passed tests=91`.
- Coverage now includes:
  - No old-art-direction references in UI Toolkit migration files.
  - No active Canvas/TMPro runtime type dependencies in migrated UI Toolkit runtime paths.
  - Legacy Canvas references restricted to approved fallback/bootstrap surfaces.
  - Migrated UI Toolkit runtime does not directly reference legacy Canvas view classes.
  - UI Toolkit mode disables the legacy Canvas presentation stack.
  - Only `UiToolkitShellApplySystem` may remain as managed `SystemBase`.
  - Migrated UI Toolkit runtime class names avoid forbidden `Controller`, `Presenter`, `Bridge`, `Manager`, and `Button` names.
  - UI runtime sources stay under explicit assembly definitions.
  - UI Toolkit ECS/apply hot paths avoid known recurring allocation and element-lookup patterns.
  - Managed apply edge classification is documented and validated against code shape.

Known gaps
- Canvas fallback deletion is not performed. It needs explicit user approval after review.
- `RuntimeUiConfig` still defaults to Canvas until the user approves switching default runtime mode.
- Full visual QA at 16:9 and 20:9 remains a separate visual acceptance pass, not part of Phase 10 dependency-removal validation.
- Some gameplay data integrations remain future work where read models currently expose defaults.

Cross-lane impacts
- ECS/gameplay lanes should populate UI Toolkit read models and request buffers instead of calling Canvas views.
- UI lane should keep UI Toolkit object mutation inside `UiToolkitShellView` and `UiToolkitShellApplySystem` only.
- Art lane should continue using only new-art-direction assets for UI Toolkit screens. Missing assets must go through the documented imagegen workflow.
- QA/PM should approve whether to remove or retain Canvas fallback before any deletion work starts.

Next recommended task
PM/user review: approve either keeping Canvas fallback archived for a longer stabilization period or opening a separate cleanup task to delete Canvas fallback prefabs/components and flip `RuntimeUiConfig` default to UI Toolkit.
