# CH04-M01 Air Corridor — CityEdgeAirfield review

Updated 2026-09-30. Branch: `codex/airlift-airfield-review`.

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
clearance envelope; formations have 7×7 space and the initial producer has a
19×19 service footprint. The final northern wave stages at (980,690), enters
at (900,660), and uses the prepared source's existing airfield extent. This
restores interception distance after the closer approach failed natively.

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
| ImageGen direction | Generated from the user's screenshot, following the previously approved sparse marker direction; latest implemented appearance awaits user review |
| Native configuration | Preparation 02 passed: source identity, routes, mission, media, narrative, rules and checkpoint |
| Marker regression checks | `ground-marker-validation-02.log`: open geometry and ARIA tap/wait semantics passed; 22 vehicle and 11 building selection checks passed |
| English ARIA normal-input journey | EN 04 passed: seven actions, live missiles, radar linked, all six aircraft stopped, victory, six voice lines, result, reward settlement and Campaign return; clock 75.610 s |
| Persian manual normal-input journey | FA Manual 01 passed: seven input actions with ARIA inactive, live missiles, linked radar, six aircraft stopped, six voice lines, victory/result/settlement/Campaign return; clock 75.568 s |
| Native visual inspection | EN 04 HUD/selection/movement captures inspected; circles removed and existing controls retained; Persian selection, movement, result and return captures inspected |
| Real player / device acceptance | Pending: user map/camera/minimap/marker review and Android device acceptance |
| Release gates | Pending: native negative journeys, saved-state handling, packaged device content and other mission regressions affected by the shared marker |

The pre-existing Burst BC1016 diagnostic from
`CampaignMissionAttemptResourceInitializationSystem` remains in retained logs.
The focused gates and completed journey passed despite that diagnostic; this
report does not declare a clean release compilation lane. Its separate
remediation in the primary checkout was preserved.

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
