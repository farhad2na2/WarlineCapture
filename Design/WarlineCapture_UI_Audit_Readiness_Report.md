# WarlineCapture UI Audit Readiness Report

Last updated: 2026-05-07

## Purpose

This report tells us when the current Codex-built UI is ready for manual audit and what kind of audit is valid.

There are two different gates:

- Interaction audit: routes, buttons, runtime bindings, popups, saved state, and gameplay launch paths work without obvious exceptions.
- Visual-lock audit: rendered Unity captures match the approved VisualLock target and focused crops object by object.

Do not treat an interaction-audit pass as final visual approval.

## Current Phase Position

The numbered UI implementation phases in `Design/WarlineCapture_UIUX_Implementation_Detailed_Spec.md` currently run through Phase 12:

| Phase | Area | Current audit status |
| --- | --- | --- |
| 1 | App shell and router | Interaction audit ready after EditMode gate passes. |
| 2 | Shared UI components | Interaction audit ready after EditMode gate passes. Visual reuse should continue through atlas/source-mapping checks. |
| 3 | Main menu / mode select | Manual visual and interaction audit ready after EditMode gate passes. |
| 4 | Settings and accessibility | Manual visual and interaction audit ready after EditMode gate passes. Remaining work: apply settings beyond local persistence and add high-contrast variants screen by screen. |
| 5 | Quick custom setup | Manual visual and interaction audit ready after EditMode gate passes, including launch path to legacy gameplay. |
| 6 | Tactical HUD / match overlay | Interaction audit ready after EditMode gate passes. Latest manifest-rect pass aligned command rail/buttons, squad tray/cards, minimap, and top buttons closer to SCN-08 layered rects. Final visual-lock audit still needs gameplay-background-aware comparison because the HUD target includes battlefield art while the prefab correctly keeps the world render separate. |
| 7 | Popups | Interaction audit ready after EditMode gate passes. Fresh popup captures were regenerated after the full UI rebuild. |
| 8 | Objectives and results | Interaction audit ready after EditMode gate passes for first-slice result/reward flow. |
| 9 | Saga campaign | Interaction audit ready after EditMode gate passes for first-slice Saga map, briefing, deploy, result, and progress save path. |
| 10 | Persistent Operation | Interaction audit ready after EditMode gate passes for dashboard, district actions, operation popups, inbox/events/feed data. |
| 11 | Profile and progression | Interaction audit ready after EditMode gate passes for profile tabs, wallet/stats/history binding, and reward-track claim feedback. |
| 12 | Splash / loading | Manual visual and timing audit ready after EditMode gate passes. |
| VFX | Shared UI feedback layer | First implementation pass complete: shared accepted pulse, locked wiggle, invalid flash, modal/drawer motion helpers, toast chip prefab, resource flyout prefab, and world feedback marker prefab. |

## Screen-by-Screen Visual Status

This table separates route/runtime readiness from visual-lock confidence. A screen marked `First-slice shell` can still route and bind live data, but it should not be expected to match its VisualLock target.

| Screen | Current status | Notes |
| --- | --- | --- |
| `Screen_Splash` | Visual-audit candidate | Built from the accepted splash/loading pass. Still needs fresh target-vs-capture comparison for final sign-off. |
| `Screen_MainMenu` | Visual-audit candidate | Main Menu received the most detailed visual pass. Top-right plus now routes to Command Exchange instead of a placeholder modal. |
| `Screen_Settings` | Visual-audit candidate | Settings received a dedicated visual pass and control cleanup. Needs final capture comparison before visual-lock sign-off. |
| `Screen_QuickCustomSetup` | Visual-audit candidate | Quick Custom received a dedicated visual pass and launch-path tests. Needs final capture comparison before visual-lock sign-off. |
| `Screen_MatchOverlay` | Target-match in progress | Uses layered HUD assets and source-mapping tests. Latest manifest-rect pass corrected command rail/buttons, squad tray/cards, minimap sizing, and top icon buttons. Latest graphics-enabled comparison: `/private/tmp/warlinecapture-ui-target-match-comparisons/matchoverlay-comparison-graphics-current.png`, MSE 1245.77. Numeric score remains inflated because the target includes battlefield render art and the prefab intentionally does not bake that background. |
| `Screen_SagaMap` | Target-match in progress | Has layered pack/runtime routing, but not yet final-approved against target capture. |
| `Screen_MissionBriefing` | Target-match in progress | Has layered pack/runtime reward/objective binding, but not yet final-approved against target capture. |
| `Screen_LoadoutSquadPrep` | Target-match in progress | Added isolated target-derived underlay to restore target depth while keeping controls/text live. Latest graphics-enabled comparison: `/private/tmp/warlinecapture-ui-target-match-comparisons/loadout-comparison-graphics-current.png`, MSE 760.89. |
| `Screen_Armory` | Target-match in progress | Reworked card structure, resource values, inspection panel, footer placement, and added isolated target-derived underlay. Latest graphics-enabled comparison: `/private/tmp/warlinecapture-ui-target-match-comparisons/armory-comparison-graphics-current.png`, MSE 520.12. |
| `Screen_CommandExchange` | Target-match in progress | Added clean target-derived starter-card content layers while preserving live text/buttons/frames. Latest comparison: `/private/tmp/warlinecapture-ui-target-match-comparisons/commandexchange-comparison-pass3.png`, MSE 897.82. |
| `Screen_CommanderProfile` | Target-match in progress | Added isolated target-derived underlay and target-derived reward node/plaque composites for the reward track. Latest graphics-enabled comparison: `/private/tmp/warlinecapture-ui-target-match-comparisons/commanderprofile-comparison-graphics-current.png`, MSE 529.21. |
| `Screen_Inbox` | Target-match in progress | Designed-unavailable shell with Operation feed binding; rework against target/layer pack. |
| `Screen_Events` | Target-match in progress | Designed-unavailable shell with event-ledger binding; rework against target/layer pack. |
| `Screen_Ranking` | Target-match in progress | Designed-unavailable shell; rework against target/layer pack. |
| `Screen_CommandFeed` | Target-match in progress | Designed-unavailable shell with Operation feed binding; rework against target/layer pack. |
| `Screen_OperationDashboard` | Target-match in progress | Replaced the generic shell with a composed operation dashboard layout. Latest graphics-enabled comparison: `/private/tmp/warlinecapture-ui-target-match-comparisons/operationdashboard-comparison-graphics-current.png`, MSE 654.89. Still needs exact frame/content polish before visual approval. |
| `Screen_DistrictDetail` | Target-match in progress | Replaced the generic shell with composed district panels and target-derived content-art layers for the overview/action/threat areas. Latest graphics-enabled comparison: `/private/tmp/warlinecapture-ui-target-match-comparisons/districtdetail-comparison-graphics-current.png`, MSE 414.21. Still needs exact frame/content polish before visual approval. |

## Latest Capture Score Snapshot

Fresh screen and popup captures were regenerated with graphics-enabled Unity batch mode after the full UI rebuild and feedback component pass on 2026-05-07. `-nographics` produced invalid flat-gray render textures for this visual QA path, so final visual comparisons must use graphics-enabled batch capture. The complete score table is at `/private/tmp/warlinecapture-ui-target-match-scores.tsv`.

Highest remaining numeric deltas:

| Surface | MSE | Notes |
| --- | ---: | --- |
| `Screen_MainMenu` | 16287.28 | Numeric score is not useful for approval because the stored target/capture use different accepted background/compositing assumptions. Main Menu had already received the strongest manual visual pass. |
| `Screen_MatchOverlay` | 1245.77 | HUD target includes battlefield render art; prefab keeps world art separate. UI chrome received a manifest-rect pass. |
| `Screen_CommandWheel` | 965.81 | Needs focused radial/card/hint crop comparison. It is an overlay child of MatchOverlay. |
| `Screen_LoadoutSquadPrep` | 760.89 | Needs focused header/card/panel crop comparison; current structure and routing are present. |
| `Screen_QuickCustomSetup` | 668.17 | Previously accepted as close enough for interaction audit; still needs final visual-lock sign-off. |
| `Screen_OperationDashboard` | 654.89 | First composed operation dashboard pass; not final visual approval. |

Lowest current popup/screen deltas:

| Surface | MSE |
| --- | ---: |
| `POP-02 ConfirmRaid` | 131.00 |
| `POP-04 RewardUnlock` | 229.68 |
| `POP-07 PauseOptions` | 247.36 |
| `POP-05 MissionResult` | 265.52 |
| `Screen_Splash` | 268.80 |
| `Screen_SagaMap` | 272.46 |

## UI Feedback / VFX Pass

Implemented first shared feedback primitives from `Design/WarlineCapture_Visual_Feedback_VFX_Recommendations.md`:

- `Assets/Game/Scripts/UI/Components/UiMotionFeedback.cs`
- `Assets/Game/Scripts/UI/Components/FeedbackToastView.cs`
- `Assets/Game/Scripts/UI/Components/ResourceFlyoutView.cs`
- `Assets/Game/Scripts/UI/Components/WorldFeedbackMarker.cs`
- `Assets/Game/Prefabs/UI/Components/FeedbackToastView.prefab`
- `Assets/Game/Prefabs/UI/Components/ResourceFlyoutView.prefab`
- `Assets/Game/Prefabs/UI/Components/WorldFeedbackMarker.prefab`

The full UI builder now attaches `UiMotionFeedback` to generated animated buttons through the existing `ConfigureAnimatedButton` path and to generated flat buttons through `CreateFlatButton`, so accepted clicks, locked/disabled taps, invalid feedback, and selected pulses are reusable instead of screen-specific one-offs. The new prefabs provide the first reusable targets for reason chips, reward/resource flyouts, and tactical world markers.

## Manual Audit Scope

When the EditMode gate passes, audit these flows in this order:

1. Splash starts, remains visible long enough to read, then routes to Main Menu.
2. Main Menu buttons route correctly: Settings, Profile, Inbox, Store, Events, Ranking, Command Feed, Saga, Operation, Armory where exposed, and Quick Custom.
3. Settings opens, tabs/controls are readable, Back returns to caller, and controls do not clip at 16:9 or 20:9.
4. Quick Custom opens from the green mode card, fields are readable, and Launch Mission starts the existing gameplay path.
5. Saga opens, unlocked mission node opens Mission Briefing, briefing reward/objective text is readable, Deploy starts gameplay, and mission result returns to the configured route.
6. Operation Dashboard opens, district cards select correctly, District Detail actions open the correct first-slice popups, Scan updates intel, End Day opens the report, and Raid confirmation routes toward briefing.
7. Commander Profile opens, tabs switch local content, mission history shows saved recent reports, and claimable reward rows open modal detail/claim feedback.
8. Match Overlay displays resource bar, objective panel, threat panel, minimap, command bar, and squad tray without blocking required world input except where UI is touched.
9. Popups open under ModalOverlay, close correctly, and blocking popups prevent tactical world clicks.

## Visual Audit Rules

For final visual approval of a screen or popup, use:

- `Design/WarlineCapture_UIUX_Target_To_Canvas_Workflow_Guide.md`
- `Design/WarlineCapture_UIUX_Mockup_To_Canvas_Conversion_Plan.md`
- The matching `Design/VisualLock/<SurfaceId>/..._Landscape_Target.png`
- The matching `Design/VisualLockLayered/<SurfaceId>/layer_manifest.json`

A surface is not visually locked unless it has:

- Valid layered source pack.
- Fresh Unity rendered capture at target aspect.
- Fresh 20:9 capture.
- Full-screen target-vs-capture comparison.
- Focused crop comparisons for high-risk panels/buttons/icons.
- Passing source-mapping and hierarchy tests.
- No remaining visible mismatch, or a documented `not target-matched` item.

## Known Audit Boundaries

- The current UI is suitable for interaction and route-flow audit once EditMode passes.
- The current UI is not automatically final-approved for every VisualLock target just because tests pass.
- Visible placeholder modal triggers are not acceptable for manual audit. A visible button must route to a designed screen, a designed disabled state, or a real popup. Hidden development placeholders are allowed only when inactive.
- Gameplay-facing UI art should continue to wait for approved 2D isometric UI render assets where the design documents call for portraits, thumbnails, mission art, minimap art, or battlefield content.
- Side-nav shells and later surfaces are acceptable for first-slice route/content audit, but production visual sign-off still requires the full layered capture workflow.

## Gate Result

Passed on 2026-05-06:

- Results: `/private/tmp/warlinecapture-editmode-ui-audit-results.xml`
- Total: 242
- Passed: 240
- Failed: 0
- Skipped: 2

The current UI is ready for manual interaction audit across the flows listed above. Final per-screen visual-lock approval still requires the capture comparison workflow described in this report.

Additional validation on 2026-05-07:

- Full UI prefab rebuild completed without C# errors or Unity exceptions: `/private/tmp/warlinecapture-build-ui-full-feedback2.log`
- Graphics-enabled screen capture queue completed: `/private/tmp/warlinecapture-capture-targetmatch-graphics.log`
- Graphics-enabled popup capture queue completed: `/private/tmp/warlinecapture-capture-popups-graphics.log`
- Focused UI component EditMode gate passed: `/private/tmp/warlinecapture-ui-component-results2.xml` (`17/17` passed)
- Focused Main Menu EditMode gate passed: `/private/tmp/warlinecapture-ui-mainmenu-results2.xml` (`7/7` passed)
- Focused Saga Campaign EditMode gate passed: `/private/tmp/warlinecapture-ui-saga-results2.xml` (`8/8` passed)
- Focused designed-route EditMode gate passed: `/private/tmp/warlinecapture-ui-designed-routes-results3.xml` (`3/3` passed)

## Resume Marker - 2026-05-07 10:19 CEST

Current UI work was paused at the user's request after finishing the in-progress Operation Dashboard header pass and regenerating the affected operation route prefabs.

Completed immediately before pause:

- Re-ran focused validation after the light-font normalization:
  - `/private/tmp/warlinecapture-ui-component-results-lightfont.xml` (`17/17` passed)
  - `/private/tmp/warlinecapture-ui-mainmenu-results-lightfont.xml` (`7/7` passed)
  - `/private/tmp/warlinecapture-ui-saga-results-lightfont.xml` (`8/8` passed)
  - `/private/tmp/warlinecapture-ui-designed-routes-results-lightfont.xml` (`3/3` passed)
- Patched `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs` to improve the shared operation header layout: wider title area, adjusted day/time placement, non-truncated population counter, and safer settings button spacing.
- Regenerated operation route prefabs with:
  - `WarlineCaptureUiPhase1PrefabBuilder.BuildDesignedUnavailableRouteScreens`
  - Log: `/private/tmp/warlinecapture-build-designed-routes-pausepoint.log`
  - Result: no C# errors or Unity exceptions; only unrelated `FindFirstObjectByType` editor deprecation warnings from island/AI validation builders.
- Added `UiMotionFeedback.ConfigureOpenMotionDefaults(...)` in `Assets/Game/Scripts/UI/Components/UiMotionFeedback.cs` as a helper for the next VFX wiring pass. This helper is not yet broadly wired into generated modal roots.

Current safe state:

- `Screen_OperationDashboard.prefab` and `Screen_DistrictDetail.prefab` have been regenerated from the current builder.
- Visual comparison after the first operation header pass was captured at `/private/tmp/warlinecapture-ui-target-match-comparisons/operationdashboard-comparison-headerfix.png`, but it was not final visual-lock approved.
- The next resume should not assume all screens are target-matched. Continue with target-match passes and fresh captures.

Resume here after the user's review task:

1. Re-open the user's requested change first and apply/review it.
2. Return to UI visual-lock work from `Screen_OperationDashboard` and operation-family screens.
3. Re-run graphics-enabled capture after each visual pass, not `-nographics`:
   - `WarlineCaptureUiPhase1PrefabBuilder.CaptureDesignedUnavailableRouteVisuals`
   - Compare `Screen_OperationDashboard` against `Design/VisualLockLayered/SCN-11_OperationDashboard/reference/SCN-11_OperationDashboard_Landscape_Target.png`.
4. Continue the VFX pass from `Design/WarlineCapture_Visual_Feedback_VFX_Recommendations.md`:
   - Wire `ConfigureOpenMotionDefaults(UiMotionFeedback.MotionKind.Modal)` into generated popup/modal roots.
   - Prefer a builder-level rule so modal open motion is applied consistently and survives prefab regeneration.
5. After changes, regenerate the relevant prefabs and run focused EditMode tests again.

## Design Change Review - 2026-05-07 Tactical / Strategic Map Split

The user requested a pause before resuming visual-lock implementation to audit the recent gameplay/UI design changes. That review is captured in:

- `Design/WarlineCapture_UIUX_Tactical_Strategic_Target_Update_Audit.md`

Important resume rule:

- Do not resume tactical HUD, command wheel, build drawer, threat popup, build placement popup, or mission result visual-lock work from only the older base targets.
- Use the refreshed state targets listed in the audit, then create matching `VisualLockLayered` packs before Unity prefab implementation.
- Operation-family visual-lock work can resume from the previous marker, but any screen that shows tactical gameplay, minimap, mission preview, ARIA guidance, or command feedback must honor the strategic/tactical split.

## Tactical Feedback Resume Marker - 2026-05-07 10:55 CEST

Completed after the design-change audit:

- Created initial `VisualLockLayered` packs for the refreshed tactical/strategic targets.
- Created the object-level pack for `SCN-08_RTSBattleHUD_M01_TacticalFeedback`.
- Staged the M01 HUD feedback sprites under `Assets/Game/Art/UI/Generated/MatchHUD/M01TacticalFeedback`.
- Added `BattleHudTacticalFeedbackController`.
- Regenerated `Screen_MatchOverlay.prefab` with:
  - `WorldCommandMarkerLayer`
  - `SelectedEntityPanel`
  - `CommandModeBanner`
  - `InvalidCommandToast`
  - `MiniMapPanel/MinimapCameraBridge/ViewportRect`
- Focused MatchOverlay EditMode tests passed:
  - `/private/tmp/warlinecapture-matchoverlay-m01-feedback-results4.xml` (`13/13` passed)
- Graphics-enabled captures:
  - `/private/tmp/warlinecapture-screen-matchoverlay-capture.png`
  - `/private/tmp/warlinecapture-screen-matchoverlay-capture-20x9.png`

Continue next with gameplay binding, not visual decoration:

1. Wire selected unit/runtime selection state into `BattleHudTacticalFeedbackController.ShowSelectedEntity`.
2. Wire Move / Attack explicit modes into `ShowCommandMode`.
3. Wire command result reason codes into `ShowInvalidCommand`.

## Tactical Feedback Target Promotion - 2026-05-07 12:50 CEST

The earlier `SCN-08_RTSBattleHUD_M01_TacticalFeedback` target was rejected as too rough for visual lock. The clean candidate `Candidates/SCN-08_RTSBattleHUD_M01_TacticalFeedback_CleanTarget_Candidate_v2_alt.png` is now promoted to:

- `Design/VisualLock/SCN-08_RTSBattleHUD_M01_TacticalFeedback/SCN-08_RTSBattleHUD_M01_TacticalFeedback_Landscape_Target.png`

Follow-up performed:

- Regenerated `Design/VisualLockLayered/SCN-08_RTSBattleHUD_M01_TacticalFeedback` from the promoted target.
- Rebuilt `Screen_MatchOverlay.prefab`.
- Added tactical-feedback capture helpers:
  - `WarlineCaptureUiPhase1PrefabBuilder.CaptureMatchOverlayTacticalFeedbackVisual`
  - `WarlineCaptureUiPhase1PrefabBuilder.CaptureMatchOverlayTacticalFeedbackVisual20x9`
- Focused MatchOverlay EditMode tests passed:
  - `/private/tmp/warlinecapture-matchoverlay-promoted-scn08-v3-results.xml` (`14/14` passed)
- QA captures:
  - `/private/tmp/warlinecapture-screen-matchoverlay-tactical-feedback-capture.png`
  - `/private/tmp/warlinecapture-screen-matchoverlay-tactical-feedback-capture-20x9.png`

Important: the tactical feedback Canvas is now positioned for the promoted target, but it is not yet a final visual-lock match. The next pass should improve final art-layer parity while keeping the state hidden by default at runtime.

### Follow-Up Pass - Clean M01 Layers

The first promoted-target staging pass exposed baked text/map pixels in several extracted layers. That was corrected:

- M01 command banner, selected entity panel, invalid toast, and minimap bridge frames are clean frame/fill assets with no baked text or icons.
- M01 selection, attack, move, and unit proxy markers are transparent runtime-marker assets with no map pixels.
- `Screen_MatchOverlay.prefab` now includes `TacticalMapQaPreview`, inactive by default and enabled only by the tactical-feedback capture helper for visual QA.
- Updated tactical-feedback captures:
  - `/private/tmp/warlinecapture-screen-matchoverlay-tactical-feedback-capture.png`
  - `/private/tmp/warlinecapture-screen-matchoverlay-tactical-feedback-capture-20x9.png`
- Focused MatchOverlay EditMode tests passed:
  - `/private/tmp/warlinecapture-matchoverlay-clean-layers-v3-results.xml` (`14/14` passed)
4. Wire `WorldCommandMarkerLayer` marker visibility/positions to tactical map anchors and runtime entities.
5. Expand the next refreshed state layer packs before implementing `SCN-10`, `SCN-09`, `POP-03`, `POP-01`, or `POP-05`.
