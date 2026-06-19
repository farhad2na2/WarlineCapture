# UI Toolkit Match HUD Phase 4 Handoff

Lane
UI

Task
UI Toolkit Canvas Replacement Phase 4 - Match HUD binding, ECS action routing, read-model apply, and final audit.

Files changed
- `Assets/Game/UI Toolkit/SCN08_MatchHudContent/SCN08_MatchHudContent.uxml`
- `Assets/Game/UI Toolkit/SCN08_MatchHudContent/SCN08_PassengerItemView.uxml`
- `Assets/Game/UI Toolkit/SCN08_MatchHudContent/SCN08_MatchHudContent.uss`
- `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs`
- `Assets/Game/Scripts/UI/Contracts/UiShellRuntimeGateway.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestSystem.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs`
- `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellApplySystem.cs`
- `Assets/Game/Scripts/UI/Toolkit/UiToolkitShellView.cs`
- `Assets/Tests/Editor/UiToolkitCanvasMigrationValidationTests.cs`
- `Design/Architecture/ui_toolkit_canvas_replacement_plan.md`

Contracts touched
- `UiActionKind` and `UiActionRequestModel` for typed Match HUD UI action requests.
- `UiActionRequestComponent`, `UiShellPopupRequestComponent`, and `UiShellRouteRequestComponent` shell boundary buffers.
- Match HUD read models for selection panel, command state, passenger drawer, squad tray, header, status surfaces, and minimap.
- `UiActionRequestSystem` processes UI actions as an ECS `ISystem`.
- `UiToolkitShellApplySystem` remains the thin managed `SystemBase` UI Toolkit apply edge.

User-visible behavior
- SCN08 Match HUD UI Toolkit surface mounts into the shell `MatchScreenSlot`.
- Bottom command rail, selected-panel actions, passenger drawer, right rail, threat jump, minimap controls, feedback actions, and five squad cards enqueue typed ECS UI actions.
- Selected panel, command selected state, passenger drawer, squad tray, header/resources, objectives/threat/feedback, and minimap bind from ECS read models.
- Build opens the Build Drawer popup request and stays visually selected from the read model while the popup is open.
- No old-art-direction assets are intentionally used by the SCN08 UI Toolkit stylesheet; SCN08 uses `TargetLockV02` assets.

Validation run
- Static audit: old-art path scan, forbidden class-name scan, ECS/contracts UI Toolkit dependency scan, and UI action processor shape scan.
- Unity batchmode: `UiToolkitCanvasMigrationValidationTests.RunBatchValidation`.

Validation result
- Passed: `[UiToolkitCanvasMigrationValidation] result=Passed tests=50` in `/private/tmp/warline-ui-toolkit-validation-execmethod.log`.
- Static audit found no forbidden class names.
- SCN08 USS image bindings use `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02`.
- ECS/contracts do not reference `VisualElement`, `UnityEngine.UIElements`, `MonoBehaviour`, or `GetComponent`; expected `GameObject` contract references remain for prefab metadata/runtime boundaries.

Known gaps
- Full in-editor visual target-match capture is still a later visual QA gate, not part of this wiring handoff.
- Some Match HUD read models currently expose defaults until gameplay systems populate live objective, threat, minimap, and squad data.
- Build Popup, Build Placement Confirmation Bar, Armory, Commander/Profile, and result popups remain future phases.

Cross-lane impacts
- Gameplay/ECS can now populate shell boundary read-model components without depending on Canvas.
- UI Toolkit remains the managed presentation edge; gameplay policy stays in ECS systems.
- Art lane should keep new Match HUD assets under the `TargetLockV02` new-art-direction path for this screen.

Next recommended task
Start Phase 5 - Build Popup UI Toolkit migration: reconcile the Build Drawer popup against Canvas behavior/text, keep the new-art-direction assets only, then bind close/build/queue actions through ECS request components.
