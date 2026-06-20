# UI Toolkit Armory Phase 7 Handoff

Lane
UI

Task
UI Toolkit Canvas Replacement Phase 7 - Armory migration, retained roster binding, category read-model sync, selected/default/locked item state, and locked-row action safety.

Files changed
- `Assets/Game/UI Toolkit/SCN19_ArmoryContent/SCN19_ArmoryContent.uxml`
- `Assets/Game/UI Toolkit/SCN19_ArmoryContent/SCN19_ArmoryItemView.uxml`
- `Assets/Game/UI Toolkit/SCN19_ArmoryContent/SCN19_ArmoryContent.uss`
- `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs`
- `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellApplySystem.cs`
- `Assets/Tests/Editor/UiToolkitCanvasMigrationValidationTests.cs`
- `Design/Architecture/ui_toolkit_canvas_replacement_plan.md`

Contracts touched
- `IUiShellRuntimeGateway.TryReadArmoryCategory`.
- `IUiShellRuntimeGateway.TryEnqueueArmoryCategory`.
- `ArmoryCatalogCategory` presentation state.
- `UiToolkitShellView` retained Armory item and category bindings.
- `UiToolkitShellApplySystem` managed apply edge for Armory category read-model state.

User-visible behavior
- The UI Toolkit Armory screen mounts once under `ArmoryScreenSlot` and is revealed by the Armory route while the Main Menu body is hidden.
- Category buttons enqueue Armory category changes through the shell runtime gateway and update selected category styling.
- Retained roster rows preserve selected/default/locked visual states without recreating item rows.
- Selecting an unlocked row updates selected frame state and inspection title/type.
- Locked rows keep locked frame/text/badge state, reject selection, preserve the prior valid selection, and do not enqueue unavailable UI actions.

Validation run
- Unity batchmode: `UiToolkitCanvasMigrationValidationTests.RunBatchValidation`.
- Log: `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.

Validation result
- Passed: `[UiToolkitCanvasMigrationValidation] result=Passed tests=71`.
- Focused coverage includes Armory UXML parity bindings, retained item template bindings, new-art asset references, dedicated screen mount, category request/read-model sync, retained scroll row stability, selected/default/locked frame swapping, and locked-row rejection behavior.

Known gaps
- Full in-editor visual target-match capture remains a later visual QA gate.
- Filter/sort/upgrade/equip are retained actionable surfaces but still require deeper gameplay data integration in a future phase if those actions become active gameplay features.

Cross-lane impacts
- ECS remains the owner of route/category state; UI Toolkit is only the managed presentation edge.
- No old-art-direction sprites were introduced for Armory.
- No parallel gameplay path was added for Armory item behavior.

Next recommended task
Start Phase 8 - Commander/Profile: convert the commander/profile content screen to UI Toolkit using the new-art-direction assets, then bind commander/profile read-model fields through the same ECS-to-managed-apply split.
