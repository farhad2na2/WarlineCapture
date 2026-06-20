# UI Toolkit Build Placement Confirmation Bar Phase 6 Handoff

Lane
UI

Task
UI Toolkit Canvas Replacement Phase 6 - Build Placement Confirmation Bar migration, active placement read-model binding, confirm/cancel/rotate request routing, feedback states, and overlap safety.

Files changed
- `Assets/Game/UI Toolkit/SCN08_BuildPlacementConfirmationBar/SCN08_BuildPlacementConfirmationBar.uxml`
- `Assets/Game/UI Toolkit/SCN08_BuildPlacementConfirmationBar/SCN08_BuildPlacementConfirmationBar.uss`
- `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs`
- `Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestSystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiBuildPlacementReadModelSystem.cs`
- `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellApplySystem.cs`
- `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs`
- `Assets/Game/Scripts/Composition/MenuBootstrapSystem.cs`
- `Assets/Tests/Editor/UiToolkitCanvasMigrationValidationTests.cs`
- `Design/Architecture/ui_toolkit_canvas_replacement_plan.md`

Contracts touched
- `UiBuildPlacementConfirmationBarModel` and `UiBuildPlacementConfirmationBarComponent`.
- `IUiShellRuntimeGateway.TryReadBuildPlacementConfirmationBar`.
- `UiActionKind.BuildPlacementConfirm`, `BuildPlacementCancel`, and `BuildPlacementRotate`.
- `BuildingUiPlacementCommandRequestElement` request queue for existing placement confirm/cancel/rotate gameplay behavior.
- `UiBuildPlacementReadModelSystem` mirrors the existing `IBuildingUiCommand` active placement state into the shell ECS boundary.

User-visible behavior
- The UI Toolkit Build Placement Confirmation Bar mounts once under the Match screen slot and remains hidden until placement is active.
- The bar shows title, status, cost, duration, instruction, cancel, rotate, and confirm from the active placement read model.
- Confirm, cancel, and rotate enqueue the existing building placement gameplay requests through the UI action boundary.
- Disabled confirm remains visually disabled and does not enqueue stale gameplay requests.
- Valid and invalid placement feedback updates in-place without shifting layout.
- The bar is positioned above the Match HUD footer and command rail, and only the active bar rect blocks pointer input.

Validation run
- Unity batchmode: `UiToolkitCanvasMigrationValidationTests.RunBatchValidation`.
- Log: `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.

Validation result
- Passed: `[UiToolkitCanvasMigrationValidation] result=Passed tests=63`.
- Focused coverage includes UXML binding names, bar mounting, read-model apply, active placement state mirroring, confirm/cancel/rotate request routing, disabled action guarding, fixed feedback slots, and command-rail overlap prevention.

Known gaps
- Full in-editor visual target-match capture remains a later visual QA gate.
- Runtime placement correctness still depends on the existing building placement systems consuming `BuildingUiPlacementCommandRequestElement`.

Cross-lane impacts
- Gameplay/ECS can continue using the existing building placement command queue; no parallel UI Toolkit-specific gameplay path was introduced.
- UI Toolkit remains the managed presentation edge; placement state production is an `ISystem` read-model producer.
- No old-art-direction assets were added for this phase.

Next recommended task
Start Phase 7 - Armory: reconcile Armory UXML against current Canvas Armory behavior/text/features, keep only new-art-direction assets, then bind category/filter/item/detail/equip/upgrade/close behavior through ECS request/read-model components.
