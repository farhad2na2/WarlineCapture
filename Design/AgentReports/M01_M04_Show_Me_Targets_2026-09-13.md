# ARIA Show Me: current action and visible world targets

## Reported failure

M3 lesson 5 displayed an enabled Show Me button but did not expose a useful click target after placing a defense. Show Me and the automatic next-action guide used separate decisions: the button requested Move directly, while the lesson might still require selecting a squad. The world cue could be off-screen because that path did not request camera focus.

## Changes

- Managed M2 construction/production and M3/M4 lessons use the same next-action resolver for automatic guidance and explicit Show Me.
- An explicit world-target reveal requests a smooth camera focus through the UI gateway. The gateway resolves the live canonical selection or destination; the view does not move units or directly manipulate the camera.
- Selection → command button → destination follows current selection and command state. Control targets retain the existing large yellow focus frame. World targets retain the world-space ring behind the HUD.
- The embedded Show Me button is disabled when there is no actionable target, including pending arrival, missing targets, disabled placement confirmation, and passive extraction waits. It becomes available again when a usable next target exists.
- The ARIA popup uses the same availability state, and closes when Show Me reveals a HUD/world target so that it cannot cover the requested click.
- M3 explanation steps point at the Continue/Do It control instead of clearing the cue.
- Existing M1 and M2 legacy tutorial command sequencing is preserved and regression tested.

## Verification

The new Editor harness checks M3/M4 selection, command and destination transitions, camera-focus requests, missing targets, waiting states, and the invariant that every enabled Show Me in their 12 lessons exposes a target. Existing M1, M2, ARIA, M3 and M4 guidance suites run alongside it.

The live probe enters M3 through the campaign/tutorial, uses the normal Skip Optional Lesson button for defense construction, then presses Show Me before selecting a squad, after selection, and after choosing Move. Edit Mode suites and the Play Mode probe run in separate Editor processes to avoid synthetic input state from unit tests contaminating the playthrough. It checks marker visibility and that world targets are inside the playable camera viewport. Screenshots and logs are stored only in `/private/tmp`.

A stale ARIA test still expected a 683-pixel rail. It now verifies the existing adaptive empty-body layout rather than requiring the removed blank area.

Validation log: `/private/tmp/warline-show-me-final.log`.

## Results

- M1 guidance: 14 passed; M2 guidance: 42 passed; shared ARIA UI: 24 passed; M3 rules: 12 passed; M4 integration: 10 passed.
- All 139 architecture checks passed. Two helper growth issues in the preceding road-gate work were resolved with method extraction, without changing the baselines.
- Final popup/embedded Show Me scenarios and the source-growth guard passed in `/private/tmp/warline-show-me-popup-checks.log` (wrapper exit 0).
- Live M3 validation passed with wrapper exit 0 in `/private/tmp/warline-show-me-live.log`. It checked both world-ring position and the target's camera viewport, with separate frame captures visually inspected for squad selection, the Move button, and the destination.
- Captures: `/private/tmp/warline-m03-editor-probe/show-me-select-squad.png`, `show-me-move-button.png`, and `show-me-destination.png`. No evidence images were added to Design.

This is Editor-only guidance validation. M4 is covered by its integration/target tests; this change does not claim a new full M4 playthrough.

The captures also show clipped/scrolling ARIA body text and an invalid-attack toast during the automated Move sequence. Those observations are outside this target-reveal fix and remain follow-up items; this result is not a claim that the entire HUD or mission is defect-free.


## Follow-up: instruction clipping resolved

The hidden placement bar kept its GameObject active while hiding through CanvasGroup alpha. The minimap dock incorrectly reserved that invisible bar's height, reducing ARIA's instruction viewport to 56 units although the rendered M3 movement instruction needed 108. The dock now reserves placement space only when placement is pending. The live M3 viewport is now 112 units, with the full Farsi instruction above the action buttons.

Four regression cases cover English/Farsi at 16:9 and 20:9 with the real hidden placement prefab, checking full instruction visibility, button separation, and the minimap margin. Existing content/dock and source-growth checks also passed. Logs: `/private/tmp/aria-text-regression.log` and `/private/tmp/aria-text-live-fixed.log`; both wrapper runs exited 0. The final live screenshot was visually inspected. The previously observed invalid-attack toast remains a separate follow-up.
