# CH05-M01 Citywide Alert review readiness

Updated 2026-10-02. Focused native gates pass; complete player readiness remains pending.

## Mission

Protect the clinic and water/power fronts. Recruit actual Barracks reinforcements, move supplied air defense into coverage, defeat eleven original military attackers, move both original engineers to service recovery gates and place a produced reinforcement at the perimeter. Keep four original service staff alive. Existing UI, approved ground markers and ARIA Play / Stop are reused. Optional Smoke allows one existing Propose / Approve / Decline action; declining or ignoring it does not block victory.

## Evidence categories

- Visual direction: existing Campaign UI, comics, maps and markers already approved; no new gameplay buttons. Native URP derivative minimap is readable and was inspected separately.
- Automated native checks: `build-wrapper-06.log` passed v6 generation with six source files preserved, 325 gameplay owners, 2,979 unique Dense identities and 2,654 render-only objects. `validation-wrapper-05.log` passed map, routes, rules, objectives and all28 installed EN/FA comic voices. Own Grid/Surface, binding GUIDs and21 mission/scenario catalog entries pass. `packed-content-wrapper-04.log` passed with zero Burst errors.
- Partial normal input: `normal-input-en-wrapper-05.log` confirmed all six actual starting buildings placed successfully, then failed on service entity binding. Complete English manual and Persian ARIA journeys remain pending, including actual production, both responses/recovery, optional Support behavior, fourteen natural voice playbacks, real result/rewards and Campaign return.
- Production player/device acceptance: normal player build and device qualification remain pending; user review remains separate from Editor evidence.

## Failed evidence retained

- `build-wrapper-01.log`: stale destroyed-visual reference and refresh-counter assertions; native scene copy/ownership and counter handling corrected.
- `build-wrapper-02.log` and `03.log`: road permissions recomputed from noisy normals, then old assembly execution; exact canonical movement masks retained and revision imports verified.
- Build04 black minimap / omitted catalog: corrected with native URP capture and Chapter05 catalog registration. Failed raster retained in `Evidence/minimap-build04-before-repair.png`.
- `validation-wrapper-01.log`: five native authoring ownership failures; corrected Grid/Surface and own identity contracts. `validation-wrapper-02.log`: rule fixture lacked deterministic launch seed; provenance fixture corrected.
- `packed-content-wrapper-01.log`: Burst BC1016 managed-string guidance comparison; replaced with FixedString, fresh packed04 has zero Burst errors.
- `normal-input-en-wrapper-01.log`: stale catalog hash after ownership repair; repair now re-registers catalog/content before its pass marker.
- `normal-input-en-wrapper-02.log`: whole-city accepted descriptors/source vehicles survived alongside Dense identities (434/323 buildings,22/0 vehicles,9283/1123 render-only). v5/v6 migrate retained visuals before cropping/static masks and remove unrelated source vehicles; readiness checks remain SceneTag-scoped and strict.
- `normal-input-en-wrapper-03.log`: startup read a roster buffer after ECB structural playback; lifecycle corrected and actual27-actor initialization regression passed.
- `normal-input-en-wrapper-04.log` and `debug-captioned-en-wrapper-01.log`–`04.log`: initial six-building requests rejected by inherited BuildCatalog/BuildZone, visual footprint shrinkage and actual reserve road overlap. Exact authored starting-lot policy and three configured footprints corrected. Debug04 qualified full26×25 reserve lots west `(1076,378)` and east `(1234,356)` against native road/sidewalk/water/blockers/flatness; v6 uses those lots without erasing roads or placement guards. Own building prefabs now persist independent14×12/health1000 clinic,20×16/health700 utility,26×25/Fuel240 reserve values.
- `normal-input-en-wrapper-05.log`: all six placements succeeded, but service binding matched prepared map owner RuntimeBuildingId2 (health900) instead of the actual utility request. The reported900 was an ID collision, not a saved-prefab health error. Source binding now excludes OperationMapBuildingComponent and matches successful request origin, footprint and prefab key; its binding regression passed in `rules-wrapper-06.log`.

Current recipe: `urban-civic-v6-qualified-reserves-independent-prefabs`. Coverage and engineer recovery require actual Move commands. Compilation, packing and partial startup success do not establish a completed normal-input mission.

## Latest native progress

- `rules-wrapper-06.log`: exact building request binding regression passed. `normal-input-en-wrapper-06.log`: all six owners bound correctly and ten opening/briefing panels played naturally; recruitment failed because actual producer readiness was not projected to the existing Soldiers drawer. Failed native screen `live-production-06.png` remains retained.
- `rules-wrapper-07.log`: producer-readiness regression passed. EN07 reached the existing Soldiers card, but confirmation required10000 Credits/20 Materials despite supplied0 Credits/180 Materials. Citywide now uses the existing DefenseMaterialsV1 canonical30 Materials order and resource initialization dispatch.
- `rules-wrapper-08.log`: Materials and surviving-response fallback regressions passed. `presentation-wrapper-02.log`: existing HUD passed in two locales with zero new buttons.
- `recruitment-wrapper-01.log`: four soldiers were actually produced by supplied Barracks3/faction1, but the observer expected uppercase while the production pipeline emitted canonical `unit_chr_soldier_male_02_alt_04`. Exact normalized-key comparison is corrected; `rules-wrapper-09.log` passed its real-normalizer regression. Recruitment02 is running; no complete journey is established by this partial production result.

Full English manual and Persian ARIA journeys, final result/settlement/return and production player/device gates remain pending. Failed receipts remain retained.

EN08 passed actual recruitment and coverage, then failed EngineerLost at hostile release because the old forward engineer start was inside actual tank120m/rifle85m ranges. Narrow anchor revision `citywide-anchor-v2-rear-engineers-970-1130` now authors clinic `(970,426)` and utility `(1130,426)` behind supplied armor on the qualified boulevard. External recovery gates and threat health/count/timing remain unchanged; native refresh and noncombat-engineer behavior proof are pending.

## Checkpoint requested before next mission

2026-10-02: rear-staging-wrapper-01.log passed the native anchor update to clinic (970,426) and utility (1130,426), including five unchanged physical-map hashes, six unchanged source hashes and native 3×3 walkability. Noncombat engineer behavior is compiled but its new regression and complete native gameplay remain pending. Recruitment-only normal input passed in recruitment-wrapper-02.log with an actual produced reinforcement and exactly 30 Materials spent. The last full voiced route, normal-input-en-wrapper-08.log, failed EngineerLost at threat release; this failure prompted the rear staging/noncombat correction. Full English/Persian journeys, real result/return and player/device acceptance are still pending. This checkpoint does not claim player readiness.
