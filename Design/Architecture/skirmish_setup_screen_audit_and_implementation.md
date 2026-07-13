# SCN-13 Skirmish Setup Audit And Implementation

Date: 2026-07-13
Status: Complete

## Goal

Route the player-facing `Skirmish` entry from the Main Menu to a real setup screen where the player can configure the opposing factions, difficulty, economy, rules, and map seed before launching a match.

The Main Menu `DEPLOY` command remains a direct match launch. Only the Skirmish card/navigation entry opens SCN-13.

## Existing Information Audit

### Active product direction

- `Design/Skirmish_Mode_Implementation_Spec.md` is the active feature contract.
- `Design/3D_SingleMap_Gameplay_Direction.md` requires player-facing `Skirmish` language and the current 3D operation-map runtime.
- `Design/UIUX_MainMenu_Visual_Contract.md` requires command-base visual continuity and a persistent shared header.
- Internal names such as `QuickCustom`, `QuickGame`, and `QuickCustomSetup` remain valid compatibility names.

### Existing runtime contracts

- `QuickGameConfig` already stores enemy profile/count, difficulty, starting credits, income multiplier, build and production speed, attack behavior, expansion, target priority, win condition, fog/intel rules, starting resources, and map seed.
- `IQuickCustomGameConfigStore` already exposes current/default setup and applies the selected setup to runtime state.
- `IMatchLaunchCommand` already queues the match scene and start request.
- `QuickCustomScreenView` already translates visible controls into `UiQuickCustomGameConfig` and launches through those contracts.
- `UIRoute.QuickCustomSetup` already exists.

### Existing UI state

- No current SCN-13 prefab is routed by `UIShellContentView`.
- The Main Menu Skirmish entry currently has no setup-screen route.
- The general `DEPLOY` buttons directly enter the match and must remain direct.
- Historical SCN-13 targets/captures exist, but their old full-screen prefab and builder were deleted and do not match the current shell composition boundary.

## Recommendations

1. Keep the shared Main Menu header and background; SCN-13 owns only the body below the header.
2. Use the existing `QuickGameConfig`/UI contracts instead of creating a second Skirmish configuration model.
3. Show first-slice controls prominently: enemy factions, difficulty, starting credits, starting resources, income, aggression, win condition, intel, and map seed.
4. Keep advanced/unimplemented controls visible but clearly locked only when the reason is useful. Fog of War displays `REQUIRES FOG RUNTIME` and cannot be changed.
5. Clamp enemy factions to the supported 1-3 range.
6. Keep only `Tutorial Intercept` launchable. Other preset cards remain visible and locked until runtime validation exists.
7. Apply changes through `IQuickCustomGameConfigStore`; launch only through `IMatchLaunchCommand`.
8. Keep direct Deploy-to-match behavior separate from configurable Skirmish launch.
9. Build the screen from real Canvas controls and sliced/reusable sprites. The target-lock image is a reference, never a baked clickable screen.
10. Validate both route behavior and config behavior, not only prefab appearance.

## Visual Target

Canonical target:

`Design/VisualLockLayered/SCN-13_SkirmishSetup/reference/SCN-13_SkirmishSetup_CommandBase_TargetLock_V02.png`

The target uses:

- shared command-base header
- olive/gold selection and action hierarchy
- left preset rail
- central current-map operation preview
- right opposing-force and economy/rules controls
- bottom reset, randomize seed, and launch actions
- generous internal padding and 80 px minimum touch targets at 1920x1080

## Implementation Progress

Overall: 100%

| Stage | Status | Percent | Notes |
| --- | --- | ---: | --- |
| 1. Audit and recommendations | Complete | 100% | Active specs, runtime contracts, historical targets, and route gap reviewed. |
| 2. New visual target | Complete | 100% | Command-base V02 target generated and selected by the user. |
| 3. Runtime Canvas prefab | Complete | 100% | Body-only interactive SCN-13 prefab built with real controls, reusable sliced chrome, preset thumbnails, operation preview, rules, seed, reset, randomize, and launch. |
| 4. Main Menu and shell route | Complete | 100% | Skirmish card routes to `QuickCustomSetup`; shared header is preserved; direct Deploy routes remain `EnterMatch -> Match`. |
| 5. Focused validation | Complete | 100% | Builder, focused SCN-13 tests, shared-shell tests, and routed 1920x1080/2400x1080 captures passed on the final prefab. |

## Validation Record

- `[SkirmishSetupPrefabBuilder] result=Passed` after the final prefab composition pass.
- `[SkirmishSetupScreenValidation] result=Passed tests=4` validates shell mounting, serialized Skirmish/Back/Deploy routing, actual Skirmish and Back button requests through ECS route history, visible configuration reads, config-store application, and launch command use.
- `[UIShellCurrentContentLoadValidation] result=Passed tests=14` validates the broader shell after adding SCN-13.
- Initial routed 1920x1080 capture passed with `[CanvasRouteCaptureValidation] result=Passed`.
- Visual inspection rejected the first preview because its source contained a white unrendered quadrant. It was replaced with `scn13_operation_preview_sahrin_v02.png`, then the prefab was rebuilt successfully.
- Final routed captures passed at 1920x1080 and 2400x1080 after the owning lane restored its unrelated shared compile baseline. Visual inspection confirmed no overlap, clipping, blank preview areas, or header replacement.

## Acceptance

- Clicking the Main Menu Skirmish entry opens SCN-13.
- Clicking Main Menu Deploy still launches the match directly.
- Back returns to Main Menu without launching.
- Enemy faction count, difficulty, starting credits/resources, income, aggression, win condition, intel, and seed are readable and interactive.
- Launch applies the selected setup and enters the existing match path.
- Locked fog behavior has an explicit reason.
- The screen preserves the shared header and matches the new command-base target at 16:9 and 20:9.
