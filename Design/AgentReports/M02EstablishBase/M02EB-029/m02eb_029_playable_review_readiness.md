# M02EB-029 Playable Review Readiness

Date: 2026-08-26
Mission: `saga.ch01.m02.establish_base`
Status: Ready for project-owner Editor review; acceptance remains open

## Review Build

- The existing Campaign selector and deploy pipeline launch the exact M02 mission, scenario, and logical operation map.
- Match preserves that correlated Campaign selection and loads accepted source GUID `dad0bd13fb20943dfb2f881cbe225f05`; it no longer substitutes the serialized Skirmish fallback after leaving Menu.
- The provisional brief, delayed-wave comms, and first-clear debrief reuse the established narrative presenter and pause mission simulation while brief/comms are visible.
- The complete typed guidance path remains active: Build, Barracks selection, valid footprint, confirmation, resource review, rifle production, warning, and defense.
- Final comic panels, bilingual copy polish, and voices remain intentionally unproduced until the owner accepts this playable timing gate.

## Graphics Evidence

The checked macOS wrapper ran `MobileVisualQualityPlayModeCapture.CaptureFromEnvironment` with M02 selected, the current quality profile, Metal, and a 1920x1080 target. The first live run exposed a real integration defect: the large mission-briefing payload exceeded the real HUD root archetype capacity. The payload now resides in one dedicated ECS read-model singleton owned by the existing Campaign projection and gateway. The repeated capture passed.

- Wrapper log: `/private/tmp/warline-m02eb-029-graphics-capture2.log`
- Pass marker: `[MobileVisualQualityPlayModeCapture] result=Passed profile=current mission=saga.ch01.m02.establish_base`
- Overview: `current_gameplay_zoom.png`, SHA-256 `0c8c757fbe0eae5893f6eeccd519417c7d104fa089a8c9374b1d42f4f8a13806`
- Forward-post detail: `current_max_zoom_out.png`, SHA-256 `1133cfb2c83295ddcb3dd5bb090d75c4eb1ad680ee11965edd824feb1701bf16`
- Environment diagnostics confirm authored fog, both directional lights, and the `Military_Demo` global volume.
- Grounding diagnostics report all seven mission units grounded on the accepted surface with no `grounded=0` row.
- The capture uses an isolated temporary progress store under `/tmp`; it does not mutate the project owner's Campaign save.

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
- Final accepted runs contain zero compiler errors and no failed validation marker.

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

The first owner route attempt exposed two additional launch blockers after the original review-readiness capture: an older M01-complete profile could remain on M01, and the provisional M02 briefing failed because its intentionally absent comic was treated as required. Both paths now fail closed only for real missing authored media, retain the existing Campaign/narrative owners, and pass the focused and aggregate gates above. Visible owner review remains open.

## Owner Review Boundary

M02EB-029 remains unchecked until the project owner plays the full path and accepts or reports findings for visible map framing, camera, controls, tutorial pacing, construction, production, combat, result, and debrief transitions. Android/Samsung validation remains explicitly deferred and is not represented as passed.
