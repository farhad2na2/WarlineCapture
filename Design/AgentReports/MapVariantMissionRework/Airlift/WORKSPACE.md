# CH01-M04 Airlift: CityEdgeAirfield candidate

Branch `codex/airlift-airfield-review`, based on `2fc905ca1`. This isolated
candidate follows MR-03 in the migration handoff. No production promotion or
player/device acceptance is implied.

## Identity and source

Mission `saga.ch01.m04.airlift`, scenario `scenario.ch01.m04.airlift`, story,
four required specialists, supplied APC/helicopter, eight escorts, four
pursuers, rewards and progression are preserved. No new player control is
added. Ordinary transports retain their supplied-resource policy; Support is
not required.

Legacy logical map: `opmap.ch01.airlift_01`, retained at
`OperationMap_Ch01_Airlift01.asset` for rollback.
Candidate logical map: `opmap.ch01.airlift_airfield_review`, generated at
`OperationMap_Ch01M04_AirfieldReview.asset`.
Prepared physical map: `opmap.skirmish.cityedgeairfield_prepared`, pinned to
`e02f51b03a20b145a5ef0f31779770fd4b63fbff20d11272d2b0476b41bc4a22`.

The new logical ID prevents old saved coordinates being interpreted on the
airfield. Campaign progress and unlocks retain their canonical mission key.
Rollback restores the legacy map ID through the builder and catalog while
retaining earned receipts.

## Layout

| Role | X,Z | Purpose |
|---|---:|---|
| APC | 850,610 | Apron-road deployment |
| Escorts | 845,610 / 835,610 | Protect the approach |
| Specialists | 910,655 | Field hospital frontage |
| Pickup | 905,650 | Beside the team, with boarding space |
| Operational helipad/aircraft | 760,648 | Western existing pad |
| APC transfer | 774,648 | Within the landing zone, beside the aircraft |
| Departure | 730,590 | Airborne departure toward the runway |
| Pursuers | 940,480 | Southern apron approach, outside opening firing range |

Conservative grid survey: 103 cells deployment to pickup, 217 cells pickup to
transfer, with two-cell vehicle clearance. Seven-cell square formation checks
cover escorts, specialists, pursuers and the pad. Native composed-surface,
dynamic traffic and actual elapsed travel remain validation requirements.
The existing 120-second pursuit release, 20-second secure hold, 600-second
deadline and 420-second star threshold are unchanged pending measured play.
The final playable sector is x700–950, z470–690. The pursuit roster's maximum
weapon range is 75 cells; its final deployment clears the pickup by more than
173 cells before formation dispersion. Its route to the first patrol waypoint
passes conservative two-cell clearance in 140 cells.

## Aircraft ownership

The prepared pad has one decorative transport helicopter. Its exact baked
identity is
`densecity.7ee80dd1b1b10cf9b43fe1dad88c423f238837f91c9a347dd9615ee4d45f38e4`.
The identity baker exposes only this owner as a mission presentation socket.
During Airlift, the socket system hides its render hierarchy; the scenario
supplies the single interactive helicopter. It restores only visibility
changes it owns when the mission changes. The pad, neighboring attack
helicopter, scenery and source geometry remain separate owners.

This uses the existing entity-source baker and loading pipeline. Repacking
must include the socket metadata. The public-input probe requires one socket
and retired renderers on the exact physical source before counting a result.

## Shared dependency audit

Evidence Chain's map and landmark generators are pinned to Airlift's retained
legacy urban map. Its required-anchor list is regenerated from its own map,
so the new Airlift pickup/transfer anchors cannot leak into an urban scenario.
The legacy camera-padding utility is also pinned to the legacy map.

The extraction spawner consumes Airlift's optional `transfer` anchor; the
tutorial consumes its optional `pickup` anchor. Both are restricted to the
canonical Airlift mission. Other extraction missions and old Airlift maps
retain their existing fallbacks. Native validation requires tutorial lessons
3 and 6 to resolve exactly to the authored pickup and transfer coordinates.
The minimap keeps the prepared raster's full physical projection for correct
marker alignment while playable and camera bounds remain mission scoped.

EN/FA story, tutorial and mission location/intel copy now names the airfield
clinic and western helipad. The two changed briefing lines have caption-only
presentation; their old geographic recordings are detached in both languages
and full rebuilds. Replacement EN/FA voice recordings remain a production
audio gate. Existing art, the other five comic voices and all controls remain.

## Validation and open gates

The first configuration run found a rendering-assembly reference error in
the new socket system. The source was moved into `Game.Rendering`; the failed
log remains `/private/tmp/airlift-airfield-config-01.log`. The second run found
the new probe's missing Tactical contracts namespace; corrected before run
three. `/private/tmp/airlift-airfield-config-02.log` remains failed. Wrapper
exit zero without the required marker is not a pass.

Configuration run three passed with both `[AirliftRoutes] result=Passed` and
`[M04AirliftConfig] result=Passed`, wrapper exit 0. Focused validation passed
15 passenger-rule cases, two socket cases and 13 Show Me cases, including neighboring scenery
and pre-existing visibility ownership, with
`[AirliftAirfieldFocused] result=Passed`, wrapper exit 0.

Packed airfield source passed with semantic hash pinned above, 120,711,507
entity-content bytes, `[MapVariantPackedContent] result=Passed` and
`[AirliftPackedSource] result=Passed`, wrapper exit 0. Log:
`/private/tmp/airlift-airfield-packed-01.log`.

Native ARIA run one failed before extraction: the original candidate's
940,610 hostile deployment was within the raider's 55-cell firing range of
the unboarded specialists. No result is credited. The run's exact log is
`/private/tmp/airlift-airfield-watch-en-01.log`, and its last screenshot is
preserved at `/private/tmp/warline-airlift-airfield/failed-watch-en-01/last.png`.
The intermediate layout moved hostiles to 940,585. A later audit included the
roster's 75-cell rifle range and moved the final deployment to 940,560;
combat rules and pursuit release timing remain unchanged.

ARIA runs two and three both achieved native four-passenger victory, a full
APC leg, airborne departure, three stars, 500 XP, 2,500 credits and Campaign
return. They remain **failed validation runs** because the new probe expected
seven comic panels. Source audit established the existing policy activates
three briefing and three debrief panels; Airlift's configured comms sequence
is inactive under `CampaignMissionNarrativePolicy.UsesBlockingComms`.
The probe now requires all six named active panels and reports the dormant
comms policy explicitly. The mission policy was not changed to satisfy a test.

Manual Persian runs one and two failed due to probe assumptions: first it
waited for a removed selection shortcut, then a conservative screen rectangle
rejected an actionable selection-box corner. The fixture now presses the
normal Select control, uses actual world taps/drags and UI raycast reachability,
and observes radial command controls at their public wedge touch point.
Both failures and screenshots are retained. Neither is a manual acceptance pass.

Manual Persian run three then exposed a missing travel duration in the probe's
touch drag. It failed without acceptance credit. The corrected gesture sends
press, movement and release events over 1.4 seconds.

**Manual Persian run four passed** on the final 940,560 pursuit layout:
Campaign entry, exact prepared source/hash, one scenery socket with 11 retired
renderers, both route destinations, APC boarding of all four, transfer/unload,
helicopter boarding, 20-second clear hold, airborne departure, six active comic
panels, debrief, result and Campaign return. Wrapper exit 0; mission time
96,163 ms, three stars, zero escort/civilian losses. The normal settlement
profile records 500 XP, 2,500 credits, pilot/APC/helicopter unlocks and
CH01-M05 availability. The source scene's normal Addressables unload completed.
Log: `/private/tmp/airlift-airfield-manual-fa-04.log`.

ARIA runs four and five failed on the intermediate 940,560 layout at lesson six. Run five's
read-only APC diagnostics found a normal world-input order to 891,681 instead
of the displayed 774,648 transfer point. This initially suggested a camera/input
problem. Extraction Watch now observes the public Show Me control and waits
for the rendered cue to settle before world taps or selection drags.

Runs six and seven remain failed. Run seven's normal touch-release and move
resolution trace proved the player order correctly resolved to 774,648. Enemy
shots triggered the APC's existing `UnitAttackSystem.TryIssueFleeOrder`, replacing
that order with 899,660, 895,670 and 891,681. A repeated normal Move tap was also
overridden by retreat. The 940,560 deployment gave too little approach time
after the existing early patrol release. The final southern deployment 940,480
provides a longer clear approach while preserving enemy count, weapons, speed,
retreat behaviour and the 120-second / 15-seconds-after-boarding release rule.
None of the intermediate native wins establish readiness for the revised layout.

**Final-layout ARIA English run eight passed**, wrapper exit 0. It completed
the normal Campaign entry, exact prepared source and socket ownership, both
authored destinations, four-passenger APC and helicopter legs, 20-second secure
hold, actual aircraft departure, all six active comic panels, result and Campaign
return. Time: 163,390 ms, three stars, zero escort/civilian losses. The settlement
records 500 XP, 2,500 credits, pilot/APC/helicopter unlocks and M05 availability.
Normal source unload completed. See `Evidence/aria-en-settlement.json` and
`Evidence/airlift-airfield-watch-en-08.log.gz`.

**Final-layout manual Persian run five passed**, wrapper exit 0, with the same
source, controls, manifest, hold, departure, narrative/result/return and unload
requirements. Time: 90,386 ms, three stars, zero escort/civilian losses. Its
normal Select/world drag, Move, Board, passenger-unload and helicopter commands
were visible touch gestures. See `Evidence/manual-fa-settlement.json` and
`Evidence/airlift-airfield-manual-fa-05.log.gz`.

All nineteen completed validation logs are retained losslessly compressed.
`Evidence/logs.json` records their original SHA-256 values; `Evidence/sha256.json`
covers the delivered evidence files. Screenshots include both final results,
pad approach/boarding and representative failed-run states. The actual local
repack report is preserved separately from the physical foundation report.
Unrelated Unity material/settings/font/lighting-ID serialization and recaptured
physical minimap bytes were restored after validation. The socket baker is the
intentional shared-source change; its packed metadata was asserted natively.

Agent visual inspection checked the final EN/FA result layouts and pad approach.
This is separate from human visual approval. The compact minimap currently
shows the existing generated grid and unit markers rather than the physical
airfield raster; full raster/landmark readability remains a visual integration
gate. Result screenshots were captured during the existing star reveal animation;
settled three-star progress is verified independently in both saved profiles.

Pending production gates: native missing-passenger/transport-loss negatives
(focused rule coverage passed), human EN/FA map/camera/minimap visual approval,
replacement briefing voices, saved-resume/replay migration acceptance and
real-device acceptance/performance. The native review candidate is ready for
the user's mission review; release qualification and production promotion are
not claimed.

## Review locally

Open `/Users/farhad/.codex/worktrees/airlift-airfield/WarlineCapture` in Unity.
Use the ordinary Main Menu → Campaign → Chapter 1 → Airlift entry. Review the
clinic pickup, APC route to the western pad, passenger unload/boarding and
departure. The candidate is on `codex/airlift-airfield-review`; the primary
checkout remains separate. `M04AirliftConfigBuilder.Build` regenerates the
candidate, and `BuildPackedAirfieldSource` rebuilds the declared socket content
through the normal wrapper lane when a fresh packed cache is needed.

The existing direct-command Editor probes retain historical evidence. The new
airfield probe uses visible UI and touch gestures and must assert the active
logical and physical map identities. A temporary progress profile only opens
the mission; no passenger, health, position or outcome facts are injected.

## Approved tactical markers (2026-09-30)

The approved ImageGen direction is implemented for Airlift landing, pickup,
transfer and departure, its ARIA ground guidance and shared Move, drag,
soldier, vehicle and building selection presentation. No gameplay button was
added. Markers use sparse native geometry, compact localized labels and the
existing EN/FA fonts; building ground corners retain their canonical footprint.

Final marker candidate passed the English ARIA and Persian manual normal-input
Airlift journeys through all four passengers, secure hold, departure, debrief,
result and Campaign return. Shared soldier/vehicle checks passed 22 cases;
building checks passed 11; Airlift rules/sockets and Show Me regressions passed.
See `Markers/README.md` for exact evidence, failed building native-probe
attempts, screenshots and remaining human/device gates. User review of the
implemented markers, native contested-state review and native building
click-ownership/screens remain pending.
