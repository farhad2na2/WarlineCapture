# CH04-M01 Air Corridor — CityEdgeAirfield review

Updated 2026-10-01. Branch: `codex/airlift-airfield-review`.

## Candidate

Mission `saga.ch04.m01.air_corridor` and scenario identity remain unchanged.
The new logical map `opmap.ch04.air_corridor_airfield_review` consumes prepared
`opmap.skirmish.cityedgeairfield_prepared`, semantic hash
`e02f51b03a20b145a5ef0f31779770fd4b63fbff20d11272d2b0476b41bc4a22`.
The original logical definition remains at
`Assets/Game/Configs/OperationMaps/Chapter04/OperationMap_Ch04_AirCorridor01.asset`
for rollback. The generator uses `LegacyMapPath` to retain that reference.

Seventeen mission anchors replace the old city coordinates. Two supplied air
missile launchers, the mobile radar, two escorts, six hostile aircraft, two
waves, existing Select / Move / Attack / Hold and ARIA Play / Stop remain.
Resources (140 Materials), rewards, unit stats, ammunition, wave timings,
240-second deadline and corridor/radar failure rules remain unchanged.
No gameplay button was added. The old M03 building injection is cleared.
Airlift's decorative-helicopter visibility override is absent in this mission.

The west launcher has a native generated-grid route of 30 cells with a 5×5
clearance envelope; formations have 7×7 space. The initial Barracks origin is
(515,597), with its complete 28×15 footprint and two-cell service margin clear
of prepared-map blockers. The old center-based 19×19 check missed 32 source
blocked cells inside the actual building footprint at (515,630). The final northern wave stages at (980,690), enters
at (900,660), and uses the prepared source's existing airfield extent. This
restores interception distance after the closer approach failed natively.
The western coverage anchor is (630,610), keeping both launchers close enough
for overlapping northern defense while still covering the western approach.

## Remaining round marker

The screenshot's yellow full ring and green segmented ring belonged to ARIA's
shared guidance renderer, which had not been migrated with selection prefabs.
ImageGen reference: `MarkerReview/vehicle-guidance-imagegen.png`.
Guidance now uses four thin cyan ground corners and a short amber dash; a
waiting/defend area uses open corners without a tap instruction. Crosshairs and
circular perimeters are disabled. Airlift retains its existing suppression of a second waiting-area perimeter over the landing pad. ARIA observes cue semantics rather than the
removed crosshair. Ground selections preserve the model silhouette without a
copied cyan model overlay; airborne selection outlines remain.
The cyan hand is ARIA's existing touch feedback, not the ground-marker layer.
No marker text, font or gameplay control was added.

## Evidence gates

| Gate | Evidence / status |
|---|---|
| ImageGen direction | Generated from the user's screenshot, following the previously approved sparse marker direction; implemented selection-marker direction approved by the user on 2026-09-30 |
| Native configuration | Recovery 12 preparation passed: source identity, full Barracks footprint/service margin, reachable overlapping coverage point, routes, mission, media, narrative, rules and checkpoint |
| Marker regression checks | `ground-marker-validation-02.log`: open geometry and ARIA tap/wait semantics passed; 22 vehicle and 11 building selection checks passed |
| Final English manual / recovery journey | Recovery EN 12 passed on final coverage layout: saved Continue, real radar loss, no defeat rewards, fresh Retry, six normal input actions, six interceptions, six voice lines, victory/result/settlement/latest Campaign return; clock 74.263 s |
| Build drawer and resource journey | Budget EN 06 passed visible disabled shortage, exact refund, affordability refresh and complete manual victory/return on the relocated Barracks before the final coverage-point adjustment; UI/resource implementation and producer position are unchanged |
| Final Persian ARIA journey | Final ARIA FA 02 passed on final coverage layout: seven actions, six interceptions, live missiles, linked radar, six voice lines, victory/result/settlement/latest Campaign return; clock 75.946 s; wrapper exit 0 |
| Native visual inspection | Final English HUD, gray PLACE/shortage, refund and Campaign return inspected; final Persian guidance/selection, result and Campaign return inspected. Earlier English ARIA and Persian manual captures remain historical evidence for the prior producer placement |
| Real player / device acceptance | Pending: user map/camera/minimap/marker review and Android device acceptance |
| Release gates | Pending: packaged device content, device acceptance and other mission regressions affected by the shared marker; the real radar-loss and saved Continue/Retry gates passed |

The Burst BC1016 source was corrected by moving mission FixedString constants
out of the Burst update path. Resource validation 02 passed five actual generated
Burst update cases and eight construction mutation cases, plus the affected M02
resource contracts. Air Corridor's authored 140 Materials and zero Credits,
Barracks cost 120, shortage with no mutation, exact rollback and Guard Tower
cost 70 passed. These focused checks do not certify unrelated mission UI tests.
Resource validation 04 also passed the actual production preflight regression:
110 Materials rejects the displayed 120-Materials Barracks; refund to 140 makes
it affordable, with Credits remaining zero. Production request validation 01
passed 31 existing queue, cancellation, request and spawn regressions. Its GUI
Editor was completed through the task-owned lifecycle helper after the tests.
The preparation 03 launch hit a project lock before that Editor finished closing;
its wrapper exit 0 is not a preparation pass. Preparation 04 ran after confirmed
exit and release of the project lock.

Resource validation 01 retained a failure in M02's pre-existing legacy Support
rail visibility assertion; the focused resource run does not count that whole
suite as passed.

## Failed candidates retained

- EN 01: first wave stopped; the close northern approach breached the corridor
  as the second wave activated. No victory credited.
- EN 02: removal of the crosshair exposed ARIA's renderer-dependent tap
  observation. Stopped through the probe's normal failure completion callback;
  no victory credited. Fixed with explicit tap versus area semantics.
- EN 03: ARIA passed selection and movement with the new marker, then the close
  northern jets destroyed the northern launcher and entered the core. Native
  positions proved that aircraft remained suppressed at their staging points
  until activation; this was approach geometry, not pre-activation movement.
- EN 04: final northeast approach completed the full normal-input journey.

Full successful and failed logs are compressed under `Evidence/Logs`, with raw
byte counts and SHA256 in `manifest.json`. Screenshots and isolated validation
profiles remain in each dated evidence folder. Historical old-map journeys are
not counted as evidence for this candidate. Main has not been promoted.

## Latest Campaign menu and recovery completion

Merged latest main `2c36f349f` via `c8665ab14`; District Atlas and comic header
remain intact. The complete main Campaign prefab was preserved. Four native
Campaign route/hierarchy checks passed; evidence is in `../MainMenuIntegration`.
Recovery 09's native Campaign capture was inspected and shows the new menu.

Recovery 09 passed through ordinary UI/touch: saved Continue from attempt 7
creates fresh attempt 8 on the current prepared map; the legacy logical map
request rejects with `mission-catalog-mismatch`. The radar was selected and
moved to an exposed position through actual map camera controls and world taps.
Real hostile combat destroyed it, produced a defeat, and awarded no Campaign
Credits, XP or first-clear reward. The visible Retry button created attempt 9,
retired all old roster entities, reset eleven members including six hostiles,
restored radar health 500 and cleared tutorial acknowledgements. The retry then
completed manual input, all six aircraft interceptions, victory, debrief voice
playback, settlement and Campaign return. Wrapper exit 0 and both aggregate
recovery/input pass markers are retained.

Recovery runs 01–08 are failed validation evidence. They exposed probe compile,
DynamicBuffer enumeration, lazy minimap-popup creation and camera/touch sequencing
errors. Run 08 completed an ordinary win but failed the recovery gate because its
single move tap was suppressed after selection; it is not a recovery pass. Run
09 repeats the ordinary destination tap until actual unit movement is observed.
No position, health, objective, roster or outcome was injected.

Recovery 10 passed saved Continue, real radar-loss defeat without rewards and
fresh Retry reset, then failed its retry victory when the northern launcher was
destroyed and the corridor breached. Its full journey is failed evidence. This
exposed insufficient early overlap from the old western coverage point (580,600).
Recovery 11 moved coverage inward to (620,600), but the native camera left its tap
point beneath SelectedSquadPanel. It was completed as failed through the probe's
normal callback. Read-only Pipeline diagnostics recorded the exact UI hit and
camera center. Coverage was moved to the clear, normally reachable (630,610)
point for the next candidate. Wave timings, aircraft and launcher stats are intact.

## Build drawer visibility and resources

Unavailable queue and resource cards retain their gray/locked visual treatment
while remaining inspectable. Existing PLACE/PRODUCE actions stay visible and
disabled when unavailable, including a labeled disabled action before selection.
Closing and reopening retains the player's selected item and refreshes its
availability. Mission Defense availability refresh now tracks resource changes.
No new gameplay control or catalog item was added.

Native budget 05 passed a normal recruitment/cancellation journey: 140 → 110
Materials, visible disabled 120-Materials Barracks action and shortage reason,
then Cancel refunds exactly 30, restoring 140 and the existing enabled action.
It subsequently won all six aircraft interceptions and returned through result,
settlement and Campaign. This run preceded the final Barracks relocation;
final-placement reruns are tracked below.

Budget EN 06 passed the same native resource checks and complete manual mission
on the final logical map hash
`46b773184200799903d2f9d21634a2c9ae7d26b634d904545667ceeb94a5c8e5`.
Final Persian ARIA 01 passed the complete normal-input mission on that same map.
Both wrappers exited 0. Successful captures are in
`20260930-215943-air-corridor-en-budget` and
`20260930-220359-air-corridor-fa-IR-aria`.

Recovery EN 12 prepared and passed the complete normal-input negative and manual
retry journey on final logical map hash
`c7f3994aec248113199a64ea5e84c084188b90c4248b8c83ccc5e6a3a4aac8fe`.
Saved Continue, legacy-map rejection, real-combat radar loss, no defeat rewards,
fresh Retry, live missile/radar defense, six interceptions, voice playback,
victory, settlement and latest Campaign return all passed, wrapper exit 0.
Its native defeat/result and normal movement captures were inspected. Evidence:
`20260930-222507-air-corridor-en-recovery`.

Budget runs 01–04 remain failed: full queue initially blocked inspection;
preflight used the old 90 component instead of 120; cancellation first stored
the old 20 component instead of 30, and the receipt path also excluded Campaign.
Resource 03 remains failed compiler evidence even though its wrapper returned 0;
the incorrect test class reference was corrected for Resource 04.
Build drawer validation 01 passed 28 focused checks.

Final Persian ARIA 02 passed on the same final logical hash with wrapper exit 0.
Guidance/selection, result and latest Campaign return captures were inspected in
`20260930-223240-air-corridor-fa-IR-aria`. Manual and ARIA outcomes are native
normal-input evidence; they do not establish real player or packaged-device acceptance.
