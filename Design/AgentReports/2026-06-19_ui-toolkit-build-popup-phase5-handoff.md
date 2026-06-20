# UI Toolkit Build Popup Phase 5 Handoff

Lane
UI

Task
UI Toolkit Canvas Replacement Phase 5 - Build Drawer popup migration, retained templates, ECS action routing, popup presentation, and read-model apply.

Files changed
- `Assets/Game/UI Toolkit/SCN09_BuildDrawerPopup/SCN09_BuildDrawerPopup.uxml`
- `Assets/Game/UI Toolkit/SCN09_BuildDrawerPopup/SCN09_BuildCatalogItemView.uxml`
- `Assets/Game/UI Toolkit/SCN09_BuildDrawerPopup/SCN09_ProductionQueueItemView.uxml`
- `Assets/Game/UI Toolkit/SCN09_BuildDrawerPopup/SCN09_ProductionActiveItemView.uxml`
- `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs`
- `Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestSystem.cs`
- `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellApplySystem.cs`
- `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs`
- `Assets/Tests/Editor/UiToolkitCanvasMigrationValidationTests.cs`
- `Design/Architecture/ui_toolkit_canvas_replacement_plan.md`

Contracts touched
- `IUiShellRuntimeGateway.TryReadBuildDrawer`.
- `UiBuildDrawerModel`, retained catalog row models, retained queue row models, active production model, and build detail model.
- Build Drawer ECS read-model components and buffers for detail, active production, catalog rows, and queue rows.
- `UiActionKind` and shell request buffers for close, catalog item, primary build, rush, clear, active-production cancel, and queued-row cancel actions.
- `UiShellPopupKind.BuildDrawer` presentation commands for showing and hiding the popup.

User-visible behavior
- The UI Toolkit Build Drawer popup mounts once in the popup layer and is driven by shell popup presentation state.
- Close captures the UI click, hides only the Build Drawer popup, and clears the Build selected state without routing to Main Menu.
- Build remains visually selected while the Build Drawer popup is open.
- Catalog, active production, and production queue rows use retained templates instead of recreate/destroy refreshes.
- Build catalog and production actions enqueue ECS request components through the UI action boundary.

Validation run
- Unity batchmode: `UiToolkitCanvasMigrationValidationTests.RunBatchValidation`.
- Log: `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.

Validation result
- Passed: `[UiToolkitCanvasMigrationValidation] result=Passed tests=59`.
- Focused coverage includes Build Drawer mount/cache cleanup, retained template bindings, close routing, catalog requests, production requests, primary build requests, popup presentation, and ECS read-model apply.

Known gaps
- Runtime gameplay systems still own consuming the build catalog/production request buffers and populating live production data beyond the seeded UI shell defaults.
- Full visual target-match QA remains part of later UI Toolkit visual polish, not this ECS wiring handoff.

Cross-lane impacts
- Gameplay/ECS can now fill Build Drawer read-model buffers and consume typed build/production requests without depending on Canvas.
- UI Toolkit remains the managed presentation edge; gameplay policy stays in ECS systems.
- No old-art-direction assets were added for this phase.

Next recommended task
Start Phase 6 - Build Placement Confirmation Bar: reconcile the UXML against the current Canvas confirmation bar, then bind confirm/cancel/rotate/cost/title/validity state through the same ECS request/read-model split.
