# AM-WP-008 - Squad Tray And Command Wheel Lifecycle

Status: draft, dependency-blocked, and not dispatchable. Do not implement until Phase 2 (`AM-020` through `AM-025`) is accepted and `AM-028` accepts Match UI binding, invalidation, and teardown ownership.

Umbrella task: `AM-032`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, rows `UI-009` and `UI-029`.

## 1. Current Ownership And Risk

The active paths are event-driven and must remain so:

- `MatchHudSquadTrayView` binds five card clicks, retains the selected frame, and uses `Update` only while a disabled-card flash is pending.
- `MatchHudSquadTraySelectionUiSystemHelper` scans/ranks units only after a card click, then applies selection through existing selection owners.
- `CommandWheelPanelView` opens/closes from button events and applies/clears Special command feedback immediately.
- `MatchOverlayCommandInputUiSystemHelper` owns command-wheel button binding and capability application.

Risks found:

- Squad Tray bind creates five captured delegates on every bind, while `Unbind` calls `RemoveAllListeners`, which can remove listeners not owned by this view.
- Card labels are created as runtime GameObjects in `Awake`; lifecycle duplication must be characterized before any visual-lock/prefab decision.
- Disabled-flash expiry uses a view-local `Update`. It is narrow and inactive when no flash is pending, but has no allocation/lifecycle measurement.
- Click-path candidate classification converts fixed strings and lowercases them while scanning units. This is not recurring frame work, but it requires a bounded interaction allocation/time budget.
- The dormant shell squad-tray adapter reconstructs a default managed model and has no proven active apply caller.
- Command Wheel open/close and teardown have behavior coverage but no repeated lifecycle/allocation matrix.

## 2. Accepted Future Ownership

- Squad Tray and Command Wheel remain event-driven views. No recurring projection cache or ECS update system is introduced unless characterization proves a changing data source exists.
- Each view owns exact cached `UnityAction` delegates and removes only its own listeners.
- Rebind is idempotent: one click produces one action/audio event after any number of bind/unbind cycles.
- Disabled-flash expiry is owned by one bounded mechanism. A view `Update` may remain only if its zero-allocation inactive/active behavior is measured and registered; otherwise use the existing centralized Match UI lifetime tick.
- Squad selection scanning remains click-triggered, World-scoped, and behaviorally identical. Reusable scratch storage and classification metadata may remove click allocations without creating parallel unit-category truth.
- Runtime-created label hierarchy is preserved until prefab/visual-lock ownership explicitly accepts migration. The implementation may only make creation idempotent and teardown-safe.
- Command Wheel remains the Special-mode presentation/input edge. Closing, disable, destroy, Match exit, or rebind clears only the mode state it applied.
- The dormant squad-tray gateway adapter is removed or delegated only if a real caller is accepted. No duplicate active projection authority is created.

No new `SystemBase`, default-World lookup, broad manager/controller/service type, static mutable cache, or per-frame scan is allowed.

## 3. State And Invalidation Contract

Required identities:

- bound World/Match session and view lifecycle generation;
- active Squad Tray slot and selection version;
- card binding generation and exact callback owner identity;
- disabled-flash slot, start/expiry identity, and retained base color;
- command-wheel open state, applied Special-mode generation, focused-unit capability version, and command-input binding generation;
- localization, settings/accessibility, sprite catalog, and resolution/layout generations;
- explicit invalidation generation and reason.

Rules:

- Equal selected slot causes no frame/color mutation.
- A flash changes one card color once and restores it once.
- Repeated open/close is idempotent; Special mode applies once per open and clears once per owned close.
- Destroy/disable/rebind cannot leave Special mode, duplicate callbacks, stale selected frames, or foreign-listener removal.
- Unit candidate scans occur only on accepted Squad Tray clicks and never from unchanged presentation refresh.

## 4. Exact File Allowlist

Production files allowed after dispatch:

- `Assets/Game/Scripts/UI/Components/MatchHudSquadTrayView.cs`
- `Assets/Game/Scripts/Systems/MatchHudSquadTraySelectionUiSystemHelper.cs`
- `Assets/Game/Scripts/Systems/SelectionGameplayStartupSystemHelper.cs` only for Squad Tray bind/unbind integration
- `Assets/Game/Scripts/UI/Screens/CommandWheelPanelView.cs`
- `Assets/Game/Scripts/UI/Screens/MatchOverlayCommandInputUiSystemHelper.cs`
- `Assets/Game/Scripts/UI/Screens/MatchOverlayCommandControlsView.cs`
- `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.ReadModels.CommandHeader.cs`
- `Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs`
- `Assets/Game/Scripts/UI/Contracts/IMatchRuntimeUi.cs`

Test files allowed:

- `Assets/Tests/Editor/MatchHudSquadTraySelectionSystemTests.cs`
- `Assets/Tests/Editor/MatchHudSquadTrayQuickSelectTests.cs`
- `Assets/Tests/Editor/SelectionSummaryQuerySystemTests.cs`
- `Assets/Tests/Editor/UIShellCurrentContentLoadTests.cs`
- `Assets/Tests/Editor/SquadTrayCommandWheelLifecyclePerformanceValidation.cs` and its `.meta` if required

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_008_squad_tray_command_wheel_lifecycle_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_008_squad_tray_command_wheel_lifecycle_acceptance.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- the `AM-032` tracker record and progress snapshot

Read-only fixtures:

- Squad Tray/Match HUD prefabs and generated card art;
- command, selection, audio, localization, and unit configs;
- UI visual-lock references.

Hard exclusions:

- operation-map/static-map production, scenes, trackers, and evidence;
- FirstLaunch, audio production, visual-lock art, prefabs, scenes, packages, and `ProjectSettings`;
- quick-select category/count/ranking rules, camera preference, command behavior, Special-mode UX, labels, layout, art, or audio routing;
- selection projection work owned by `AM-WP-003` except through its accepted boundary;
- any file outside this allowlist without a reviewed package amendment.

## 5. Characterization Matrix

Required before edits:

1. initial slot, every slot click, repeated same-slot cycling, no candidates, disabled flash, and selection clear;
2. visible/offscreen candidate priority, soldier clustering, unit destruction, passenger exclusion, World replacement, and camera absence;
3. `1`, `10`, and `100` bind/unbind cycles followed by one click, proving exactly one callback/audio/action;
4. foreign button listener preservation across Squad Tray unbind;
5. runtime card-label creation across enable/rebind/scene reload, proving no duplicates;
6. Command Wheel open, close button, scrim close, toggle, Stop action, capability change while open, disable/destroy, Match exit/re-entry, and repeated rebind;
7. persistent/transient feedback restoration when Special mode closes;
8. proof that the shell squad-tray adapter has no active caller before retirement.

Record callback counts, listener counts, audio events, selection scans, candidate counts, fixed-string conversions, sort counts, frame/color mutations, mode apply/clear counts, GameObject creation, and allocations.

## 6. Baseline And Acceptance Gates

Measure:

- `180` warmup plus `300` unchanged closed/open frames for Squad Tray and Command Wheel;
- `300` disabled-flash active/inactive frames;
- repeated bind/unbind and open/close transitions;
- click scans at representative unit counts, reported separately from unchanged-state presentation.

Acceptance:

- exactly zero recurring production-owned managed bytes during unchanged and flash-lifetime windows after warmup;
- zero candidate scans while unchanged;
- one action/audio callback per click after repeated binding;
- foreign listeners survive unbind;
- no duplicate labels or stale Special mode across lifecycle transitions;
- click-path allocation/time remains within the captured baseline or improves, without changing selection results;
- dormant adapter removal/delegation leaves one authority and no active behavior change;
- focused tests, zero compiler errors, integrated architecture checks, and `git diff --check` pass;
- evidence binds baseline/implementation commit and tree, source hashes, compressed logs, metrics, and focused review.

## 7. Maximum Slices And Rollback

At most two independently stable commits:

1. lifecycle/listener ownership, idempotent label creation, and focused characterization;
2. bounded click-path allocation improvement, flash lifetime consolidation if proven useful, and dormant-adapter retirement.

Rollback if selection order/count/cycling, audio, Special mode, Stop capability, feedback restoration, visual state, or lifecycle behavior changes; if foreign listeners are removed; if a per-frame scan/cache authority is introduced; or if the slice touches protected/non-allowlisted files.
