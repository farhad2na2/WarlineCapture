# Tutorial indicator rendering repair — 2026-09-15

## Reported behavior and cause

The user’s M3 capture showed a frame above the Continue button, a single yellow stroke, a weak/stationary pointer and a blinking caption. The guide was laid out before ARIA's final button reflow. Its caption had two position owners: the control guide reset its position and visibility while the pointer positioned it beside the target. Background read-model refreshes could also deactivate the direct tutorial frame and restart attention timing. M3 render inspection additionally caught active guide objects whose graphics were not visible. Geometry changes in rendering callbacks occurred after Unity had rebuilt UI meshes; active objects alone were insufficient evidence of visibility.

## Implementation

- Give tutorial guidance its own non-interactive overlay canvas, above both camera-space HUDs and overlay ARIA rails; destroy it when its owner unbinds.
- Align and render attention at the end of the existing MainMenuPlayUI presentation update, after guided content and ARIA controls reflow and before Unity rebuilds UI meshes. The view stays passive; no new MonoBehaviour update loop is introduced. Account for the parent canvas rectangle’s center so the frame follows the actual clickable button.
- Supply the HUD canvas as the overlay pointer’s text-obstacle source and reuse a list when collecting its active text. Show Me uses its own cached canvas. No global scene search is used.
- Keep direct tutorial cues alive through background highlight read-model refreshes. Lesson/target changes and explicit hides still clear them.
- Size captions in screen pixels so a scaled HUD canvas cannot shrink the instruction.
- Give caption placement and visibility to the pointer alone. Repeated style/read-model updates cannot put it back on the button.
- Draw two separate gold keylines with a dark gutter, with stable dimensions and subtle brightness modulation.
- Replace the small chevron with an outlined, filled gold arrow. Its 1.2-second motion cycle travels 16–36 screen pixels peak to peak depending on viewport size. Reserve its complete movement envelope when choosing a safe side.
- Continue using unscaled time while gameplay is paused. Reduced motion keeps the arrow stationary. Show Me retains its four-second idle delay; action changes reset that delay.
- Guide decoration remains non-interactive and does not resize button hit areas.

## Validation

- M3 English and Farsi passed after the architecture repair (`/private/tmp/warline-indicator-resume-m3-en.log`, `/private/tmp/warline-indicator-resume-m3-fa.log`). The checks observed 284 English and 119 Farsi rendered frames, centered button bounds, two visible edges, stable captions, screen-safe decoration, and 25–28 pixels of arrow movement. Pixel checks found over 4,100 gold frame pixels and over 550 arrow pixels in each language. Both final screenshots were inspected: captions sit to the left of ARIA without covering its instruction.
- Architecture passed all 139 checks in nine fixtures on the final source (`/private/tmp/warline-indicator-final-architecture.log`; wrapper exit 0). Both failures recorded at the pause were resolved without changing baselines or weakening guards: the scene-wide text search was replaced with a bound HUD source, and LateUpdate was removed from the view.
- Focused edge and timing regressions passed for all four orientations, three viewport sizes, two caption widths, blocked surroundings, delayed attention, progress reset, reduced motion, unchanged hit areas, and renderable arrow geometry.
- Final English and Farsi M2 Continue checks passed (`/private/tmp/warline-indicator-resume-m2-en.log`, `/private/tmp/warline-indicator-resume-m2-fa.log`): 27 rendered samples per language verified the double frame, stable caption and approximately 28–30 pixels of pointer travel. A real click hid Continue immediately and advanced to the rifle-production lesson in both languages.
- Final Farsi M3 passed the explicit instruction-overlap assertion across 146 frames (`/private/tmp/warline-indicator-final-m3-fa.log`), with approximately 23 pixels of arrow travel.
- Cross-canvas instruction avoidance is checked in the actual M3 play probe: neither the arrow nor the caption may overlap ARIA’s visible title or body. An initial isolated EditMode fixture failed to place an arrow while the authored Menu scene was still present; it was replaced with this gameplay assertion so the test covers the actual target, canvases and instruction.

Screenshots remain in `/private/tmp`, not in Design. Validation is Editor only.

## Status

Completed after the requested pause. All focused indicator checks, the M2 EN/FA Continue play checks, M3 EN/FA rendered checks, and 139 architecture checks passed. The main Editor required the supported Pipeline recompile command to replace its stale assembly; compilation completed with no errors and reflection confirmed the passive updated view loaded (compiling=false, playing=false). All validation wrappers have exited. Changes remain uncommitted; the two font assets already modified before this task have been preserved.

## Placement-confirm follow-up

The later placement screenshot exposed a separate next-action bug: the guide still targeted the underlying `BuildCommand`, not the visible `ConfirmButton`. The pending placement remained active after the command-mode flag changed. Captured bounds were 203.63×193.31 for that wrong guide versus 296.77×218.94 for the green confirmation control.

The assistant now receives the actual confirmation-bar reference from its owner and uses `HasPendingPlacement` when choosing guidance and executing the construction action. It no longer discovers that control through a hierarchy search or treats command mode as placement ownership. The shared construction flow covers M2 and M3.

The pointer can approach diagonally when the footer blocks one side and the minimap blocks another. Its complete rotated bounds and animation travel must fit. Minimap views are explicitly included as obstacles because they handle pointer events without being Selectable buttons. Captions remain separate from the arrow's movement envelope.

M3 English and Farsi placement checks passed (`/private/tmp/warline-placement-indicator-bound-en.log`, `/private/tmp/warline-placement-indicator-bound-fa.log`). They verified the exact bound Confirm button, a centered frame with 10 pixels of padding, visible rendered gold geometry, a stable caption, no overlap with other controls or the minimap, and animated travel over 44/33 frames. Actual confirmation advanced the lesson to squad movement. The first-frame captures were visually inspected in both languages; evidence stays outside Design.

These focused UI checks stage a preview using the existing production-validator-backed helper, then use the real confirm button. They do not certify physical dragging: two earlier pointer-driven setup attempts failed with camera drift or a queued drag not reaching its destination under Editor automation.

Final placement-follow-up validation passed all 139 architecture checks in nine fixtures plus the pointer geometry/timing regression (`/private/tmp/warline-placement-guidance-architecture.log`, wrapper exit 0). The M2 English Continue regression also passed on the final source: 34 rendered samples, approximately 30 pixels of travel, immediate removal on click, and advancement to rifle-production guidance (`/private/tmp/warline-placement-guidance-m2-regression-retry.log`, wrapper exit 0). Its preceding concurrent run failed the minimum rendered-sample requirement; the isolated rerun passed without changing the test or its thresholds.

The main project Editor's supported Pipeline recompile completed with `failed=false` and no compilation errors. After domain reload, live reflection confirmed `placementBindingLoaded=true`, `compiling=false`, and `playing=false`. Source changes remain uncommitted. Existing font-asset changes and additional Editor-generated font changes were preserved.
