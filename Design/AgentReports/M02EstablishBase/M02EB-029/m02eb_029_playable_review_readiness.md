# M02EB-029 Playable Review Readiness

Date: 2026-08-28
Mission: `saga.ch01.m02.establish_base`
Status: Corrected map/camera route passed current-Editor QA; owner acceptance remains open

## Review Build

- The existing Campaign selector and deploy pipeline launch the exact M02 mission, scenario, and logical operation map.
- Match preserves that correlated Campaign selection and loads accepted source GUID `dad0bd13fb20943dfb2f881cbe225f05`; it no longer substitutes the serialized Skirmish fallback after leaving Menu.
- The provisional brief, delayed-wave comms, and first-clear debrief reuse the established narrative presenter and pause mission simulation while brief/comms are visible.
- The opening brief claims the still-opaque Match entrance before `EnterMatchHud` completes; the battlefield HUD is not exposed before the comic. Later comms and debrief presentation still wait for a settled Match HUD.
- M2 actionable ARIA steps 2-6 retain one `DO IT` request until their real typed control is ready. A late assistant bind restores already-installed HUD controls, and Barracks selection and rifle production reopen the existing Build drawer when needed; no coordinate click or parallel tutorial state is used.
- Provisional M2 panels use their direct Sprite bindings until final media acceptance; they no longer disappear merely because their future Addressables references are intentionally unset.
- The complete typed guidance path remains active: Build, Barracks selection, valid footprint, confirmation, resource review, rifle production, warning, and defense.
- Final comic panels, bilingual copy polish, and voices remain intentionally unproduced until the owner accepts this playable timing gate.

## Graphics Evidence

The running macOS Editor ran the M02 playable-review capture with M02 selected, the current quality profile, Metal, and a 1920x1080 target after the authored-base correction. The resulting images replace the rejected City Hall evidence and are the only images in this directory that may be used as current map-location proof.

- Pass marker: `[MobileVisualQualityPlayModeCapture] result=Passed profile=current mission=saga.ch01.m02.establish_base`
- Compound detail: `current_gameplay_zoom.png`, SHA-256 `f53fe24070a618046befc234859a1a78c0d4bcf4f2778cc67c7c93a8b11d78f8`
- Military-base overview: `current_max_zoom_out.png`, SHA-256 `65852fffe9f70de5a3314010bbd8730996d87d80a909817810484fe649024487`
- Environment diagnostics confirm authored fog, both directional lights, and the `Military_Demo` global volume.
- Grounding diagnostics report all four opening command-squad units grounded on the accepted surface with no `grounded=0` row; the delayed hostile wave is not active during this capture.
- The capture uses an isolated temporary progress store under `/tmp`; it does not mutate the project owner's Campaign save.

## Authored Military-Base Binding Correction

The earlier M02 correction incorrectly interpreted “use the story map like M1” as “reuse M1's exact Old Market/City Hall window.” That was rejected by the owner and is not accepted evidence. M02 now uses the separately authored military-base sector `(780,270)-(1100,470)` in the same accepted physical story map. A semantic validation fails if any core M02 base anchor re-enters the Old Market/City Hall window.

The corrected evidence shows the compound, tents, military vehicles, perimeter walls, hangar, control tower, helipads, and aircraft. The Barracks tutorial lot is the reviewed clear apron `(1004,370,24,14)`, not a visually empty but blocked road cell and not the former City Hall lot. The opening route travels west-to-east from the resource side of the compound to this build apron.

QA also found that the opening timer could advance while the comic covered the world. The M02 opening owner now holds both elapsed time and camera requests through `InteractiveBrief`; the horizontal base sweep becomes eligible only after the comic's authoritative handoff to `FindSquad`. `[M02EstablishBaseGuidanceValidation] result=Passed tests=37` covers the exact location, horizontal route, M1-only panic audio, comic hold, and one-sweep completion.

The fresh current-Editor run entered M02 through the real Campaign selection/deploy boundary, reached the corrected physical source, and produced the two Metal captures above. The same post-change Editor session passed placement `8/8`, placement regressions `8/8`, guidance `37/37`, guidance regressions `15/15`, launch `15/15`, launch regressions `3/3`, narrative `20/20`, Campaign UI `10/10`, consolidated data `5/5`, M01 consolidated contracts `23/23`, and source growth `17/17`.

## Barracks Production Finding

The Barracks remains the correct M02 building: its canonical role is military housing and infantry training. Its current production configuration, however, contains one entry for `Unit_Chr_Soldier_Male_02_Alt_04`, and the shared runtime instantiates that character prefab once. The mission objective and UI call the result a complete rifle squad. This granularity mismatch remains open for a shared squad-production recipe; it must not be concealed by changing the building, adding an M02-only spawner, or claiming one individual soldier is a squad.

## Automated Validation

- `/private/tmp/warline-m02eb-029-hud-result-regression.log`: HUD/result `4/4`, narrative `15/15`, settlement `13/13`, Campaign UI `9/9`, M01 HUD/result `11/11` with three captures, source growth `17/17`, aggregate `6/6`.
- `/private/tmp/warline-m02eb-029-lifecycle-regression-r2.log`: lifecycle `15/15`, launch `14/14`, settlement `13/13`, M01 consolidated contracts `23/23`, source growth `17/17`, aggregate `5/5`.
- `/private/tmp/warline-m02eb-029-guidance-regression.log`: guidance `30/30`, placement `8/8`, construction transaction `6/6`, dual-resource construction `3/3`, M01 guidance `14/14`, build catalog `8/8`, production `8/8`, wave `9/9`, operation map `10/10`, command gateway `7/7`, command system `16/16`, Match HUD `21/21`, source growth `17/17`, aggregate `14/14`.
- `/private/tmp/warline-m01-progress-m02-unlock.log`: progress compatibility `16/16`; a legacy M01 first clear unlocks M02 exactly once.
- `/private/tmp/warline-m02-campaign-ui-regression.log`: Campaign UI `10/10`, M01 Campaign and briefing compatibility, source growth `17/17`, aggregate `4/4`; the selector defaults to the first available incomplete mission.
- `/private/tmp/warline-m02-panel-free-narrative.log`: narrative `17/17`; intentionally panel-free provisional M02 dialogue starts without a comic, while authored comic dialogue still requires its configured panel.
- `/private/tmp/warline-m02-lifecycle-regression-rerun.log`: lifecycle `15/15`, launch/result/M01 compatibility, source growth `17/17`, aggregate `5/5`.
- `/private/tmp/warline-m02-hud-result-regression.log`: HUD/result `4/4`, narrative `17/17`, Campaign UI `10/10`, source growth `17/17`, aggregate `6/6`.
- `/private/tmp/warline-m02eb-029-source-growth-final.log`: final post-documentation source-growth and architecture metadata check `17/17`.
- Live Unity Pipeline correction runs: focused M2 narrative `20/20`, all M2 Establish Base tests `236/236`, first-launch narrative `43/43`, and production source growth `17/17`.
- Final `DO IT` correction runs after script reload: exact control-readiness regression `4/4`, attempt-fact projection `24/24`, shared ARIA HUD `23/23`, all M2 Establish Base `242/242`, and source growth `17/17`.
- Final accepted runs contain zero compiler errors and no failed validation marker.

## Exact Editor Route Verification

The earlier live route proved the comic and typed `DO IT` plumbing but predates the authored-base location correction, so its screenshots are retained only as flow evidence and are not current map-framing proof. Current map-framing proof comes from the fresh Campaign-route Metal captures and post-change camera/interaction regression suites above. Owner full-mission acceptance remains required before closing M02EB-029.

- Briefing proof: `m02eb_029_exact_campaign_briefing_verified.png`, SHA-256 `faeea0bc7ac93ab3fd82b3bbe5ed449c2af167fbf8b679027f945497a729705e`.
- First-guidance proof: `m02eb_029_first_guidance_verified.png`, SHA-256 `b2ad39fcf770f8660bc8abd817fa08d12b166add22051e2ec1083ef9e95c027f`.
- Live ECS inspection after the briefing reported `phase=Engage`, `briefDone=1`, `guidanceActive=1`, and `prompt=EstablishBaseOpenBuild`.
- Live EditMode regression after a real script reload: M2 Establish Base `236/236`; first-launch narrative `43/43`; the exact language-choice lifecycle regression `1/1`.

## Brief-Before-HUD Ordering Correction

The owner observed the Match battlefield for one frame before the opening comic. The former readiness gate delayed every Campaign narrative stage until the complete `EnterMatchHud` motion sequence had settled, including the final curtain fade. The opening brief now becomes eligible as soon as the Match route and HUD mode are authoritative, while the opaque intro curtain still covers the transition. Comms and debrief retain the settled-transition requirement.

The exact live Campaign route records the required order:

1. `EnterMatchHud` command started;
2. `CampaignMissionNarrative` started `stage=Brief` for `seq.ch01.m02.brief`;
3. `EnterMatchHud` command completed and flushed;
4. the Metal M2 playable-review capture passed.

Durable excerpt: `m02eb_029_brief_ordering_editor_log.txt`. The focused regression explicitly rejects early loading-route presentation, accepts only `Brief` during the covered Match entrance, rejects `Comms` and `Debrief` during that transition, and accepts later stages after settlement.

## Retained DO IT Dispatch Correction

The owner confirmed that the first `DO IT` opened Build but the second did nothing. Two independent boundaries caused that visible sequence. First, the shell could bind and open the Build drawer before the late Match HUD assistant bind; that bind reset the assistant presentation helper and discarded the drawer reference needed by the second action. The owner now restores every already-installed HUD control immediately after the assistant reset through the narrow `MainMenuPlayUI.AssistantBinding` partial. Second, the real UI placement path registered the managed runtime building and updated `BuildingRuntimeOwnedBuildingSummary` without creating the ECS spawn-request record that the mission fact projector previously required. The projector now consumes the authoritative player-owned summary delta relative to an attempt-local baseline, retaining its original request/live-entity checks and monotonic fact publication.

Actionable M2 steps retain exact typed mappings for Open Build, Select Barracks, Place/Confirm Barracks, Review Resource Spend, and Queue Rifle. The focused proof covers the late-bind drawer loss, delayed control readiness, exact typed routes, and rendered-item click. Attempt-fact proof covers the UI-only placement path and ensures a Barracks owned before the attempt cannot advance the mission.

The exact live Campaign replay then exercised the real sequence rather than a test harness: the first click opened Build, the second selected the rendered Barracks and advanced to `PLACE THE BARRACKS`, and the third placed and confirmed the building, spent the canonical resources, and advanced to `REVIEW RESOURCE SPEND`. Final evidence totals are recorded in `m02eb_029_do_it_validation.txt`.

## Exact Launch-Map Correction

The latest owner replay exposed a boundary not covered by the earlier metadata-only proof. Menu correctly published `saga.ch01.m02.establish_base`, `scenario.ch01.m02.establish_base`, and `opmap.ch01.forward_post_01`, but Match independently resolved its serialized `opmap.skirmish.desert_base_01` compatibility definition and loaded the wrong scene. The M2 runtime then could not accept the physical source, which left the route inert and prevented the mission-owned brief from starting.

The existing Campaign root now carries one composition-owned, request-correlated operation-map definition reference. Match resolves it before source loading, rejects stale or ambiguous requests, loads the logical definition's accepted entity-scene reference, validates the physical scene identity through `SourceBinding.SourceOperationMapId`, and retains the old compatibility path only when no Campaign request exists.

- `/private/tmp/warline-m02-map-launch-fix-2.log`: M2 launch `14/14`; exact source GUID assertion passed.
- `/private/tmp/warline-operation-map-loading-alias.log`: logical-to-physical scene loading `20/20`.
- `/private/tmp/warline-operation-map-bootstrap-fallback.log`: Match selection and fallback `17/17`.
- `/private/tmp/warline-m01-source-binding-regression.log`: M1 source binding `14/14`.
- `/private/tmp/warline-production-source-growth-regression-2.log`: architecture/source growth `17/17`.
- `/private/tmp/warline-m02-launch-regression.log`: aggregate M2 launch regression `3/3`.
- `/private/tmp/warline-m02-launch-route-proof.log`: graphics-enabled Campaign route logged `source=Campaign`, loaded `opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity`, reached full source/metadata/surface/presentation readiness, and passed at Metal 1280x720.
- `/private/tmp/warline-m02-launch-route-proof/current_gameplay_zoom_grounding.txt`: seven mission units spawned and all seven were grounded on the accepted surface.

The first owner route attempt exposed two additional launch blockers after the original review-readiness capture: an older M01-complete profile could remain on M01, and the provisional M02 briefing could fail to present its valid direct panel because its future Addressables reference was intentionally unset. Both paths now fail closed only for real missing authored media, retain the existing Campaign/narrative owners, and pass the focused and aggregate gates above. The exact live Editor route now proves both the comic and first actionable guidance screen; the owner's remaining full-play review stays open.

## Owner Review Boundary

M02EB-029 remains unchecked until the project owner plays the full path and accepts or reports findings for visible map framing, camera, controls, tutorial pacing, construction, production, combat, result, and debrief transitions. Android/Samsung validation remains explicitly deferred and is not represented as passed.
