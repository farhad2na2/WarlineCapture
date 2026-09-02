# UI V3 — First Launch comic and Match HUD responsive pass

Date: 2026-09-02

## Completed

- First Launch comic playback now fills the physical viewport at 16:9 and 20:9. Header, timeline, dialogue, playback controls, Next panel, and footer remain pinned to the correct physical edges.
- Comic illustrations select authored aspect variants and preserve aspect instead of stretching.
- The Menu scene installer now installs the V3 First Launch narrative prefab.
- Match HUD keeps one permanent ARIA panel and conditionally presents tutorial copy/actions inside it; the duplicate tutorial panel and Skip action remain absent.
- Match HUD header controls are centered, the threat strip no longer sits under ARIA, and the footer spans the full usable width.
- The tactical feedback preview uses the real Attack command route and reusable perspective ellipse rings instead of placeholder ground markers.
- The shared Mission Result prefab was re-audited in Victory and Defeat states at 16:9 and 20:9; M1 Continue and M2 final Victory return routing are green.

## Evidence

- First Launch: `Design/VisualLockLayered/SCN-00_FirstLaunch/iterations/iteration_08/`
- Match HUD: `Design/VisualLockLayered/SCN-08_MatchHudV3/iterations/iteration_07/`
- Mission Result: `Design/VisualLockLayered/POP-05_MissionResult/iterations/iteration_05/`

## Validation

- `FirstLaunchNarrativePresentationValidation`: Passed, 11 tests.
- `CanvasRouteCaptureValidation`: Passed at 1920x1080 and 4800x2160.
- `MatchHudCurrentOrderBannerValidation`: Passed, 18 tests.
- `AriaTutorialPresentationV3Validation`: Passed, 4 tests; panels=1, actions=2, skip=absent.
- `M01FirstContactHudResultValidation`: Passed, 12 tests and 3 captures.
- `M02EstablishBaseHudResultValidation`: Passed, 7 tests.

The broad M2 aggregate reaches and passes all result/debrief/UI suites, then fails its unrelated final `ProductionSourceGrowthArchitectureValidation` because the checked-in size baseline already reports nine production files outside this change set.

## Next UI lane

- Continue the runtime audit with the remaining match-adjacent screens, then victory/defeat/end-match screens, preserving the same target-lock versus 16:9/20:9 evidence discipline.
