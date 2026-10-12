# Campaign visual fixes — implementation in progress

Approved scope: every finding in the 25-mission Campaign visual audit. Original evidence remains unchanged. Work is isolated in `WarlineCapture-CampaignVisualAudit`; not merged or accepted as complete.

| Pass | Status | Evidence / remaining work |
|---|---|---|
| 0: Launch and required placements | Native opening checks passed for repaired failures | Four Chapter 5 source ownership contracts reconciled. Citywide, Steel Push, Supply Line, Network Collapse, Last Corridor and Command Node reach Engage/None with their required buildings. Full normal-input journeys remain a separate gate. |
| 1: Civic/bazaar and roads | Applied; final road-entry correction running | Civic forecourt and 39 stock props; 54 paired joins, 240 grounded terminals, 22 false end bands removed; 12 destination groups cleared from roads. Residual mounds were isolated to raised DirtRoad exit meshes; their correction and refreshed packed parity are pending. |
| 2: Runway ends | Source visual pass complete | Five modules, six outer thresholds, two internal joins, corrected 09/27 identifiers, shoulder/end transitions and 17 loose-prop moves. Native remaining mission captures and aircraft/player acceptance are separate gates. |
| 3: Destination identity | Applied and reviewed in source previews | 46 groups across 24 missions plus civic pilot; three grounded stone foundations. Supply Line's two obstructing groups were corrected and verified natively. Remaining native destination captures pending. |
| 4: Cameras and lighting | Corrective native review in progress | Authored source-sun ownership applied; Steel and no-sun Citywide verified. Supply Line and Establish Base framing reviewed. First Contact camera-only margin, Power Relay tour state, and tighter Chapter 5 first-action views require fresh verification. |
| 5: Ground and landscape | Applied; final shared-source review pending | Rotated anti-repeat normals, district surfaces, grouped scenery, canal-end corrections and 270 dune/sand-edge repairs. Source dirt-road entry grading is the remaining known surface intrusion repair. |

## Native launch evidence

- `../captures/saga.ch04.m02.steel_push/run-05/receipt.json`: Passed capture, Outcome=None, Engage, camera settled. No reserve-depot or barracks placement failure in the corresponding native log. Recurring generic Tent_Regular requests still fail.
- `../captures/saga.ch05.m01.citywide_alert/run-04/receipt.json`: Passed capture, Outcome=None, Engage. Six required building requests begin at authored origins without Citywide placement rejection. The opening camera still needs correction; it frames the road rather than recruitment.
- Earlier failed dispatches, immediate defeats and bad diagnostic runs are retained. A subsequent pass does not erase those receipts.

## Acceptance gates

Compilation, source invariants, deployed screenshots, normal-input mission journeys, and real device/player acceptance remain separate gates. Neither successful launch nor a screenshot receipt establishes completion of objectives or player readiness. No normal-input end-to-end or Android acceptance is claimed here.

## Rejected approach

Automatic approval review rejected a proposed broad suppression of Campaign initial-building spawning because it could suppress required buildings. That patch was never applied. `InitialUnitsSpawnSystem.cs` remains unchanged. Tent request ownership and a narrower correction remain under investigation; no runtime readiness predicate was weakened.

## Review corrections

- First curb pass: rejected despite execution pass markers. Mirrored scale reversed the endpoint direction and made ramp length negative; two connected CityEdge corner sections visibly disappeared. `RestoreRejectedCurbPass` passed with 204 original mesh references restored across 11 maps. The original images and log remain evidence of failure.
- Camera role census: several missions use market/power/route/escort roles instead of `role.friendly.command_squad`; explicit coverage for all 25 is compiled and awaits native review.

## Corrective source pass

The first landscape pass saved ten of eleven sources, then failed on a DenseCity water mesh without bank vertex colors. Its log is retained as `validation/remaining-visuals-01-failed.log`. DenseCity landscape work was not saved. The preceding runway-clearance pass saved 17 render-only prop moves (DenseCity 12, CityEdgeAirfield 3, GroundedSignal 2), with zero unresolved clearance candidates.

Canal-end preview review then exposed a generated-asset key collision: prefab instances share `GlobalObjectId.targetObjectId`. Generated canal meshes, road-end materials and curb meshes must use the full GlobalObjectId, including instance identity. V1 canal/road-end and V2 curb assets are rejected. The correction restores exact source meshes/materials, rebuilds with unique keys, and rechecks protected gameplay invariants. Original failed evidence remains available. Compilation passed; corrected source rebuild and new visual review are pending.

## Resume on 2026-10-12

The Editor is no longer running and the temporary files/logs from the last dispatch are absent. The ten completed source-final receipts and previews remain; DenseCity has no completion receipt, so that portion is not claimed as passed. Pending helpers were restored into the checkout. The resumed pass skips completed source receipts, then checks every source for actual visible-road clearance, paired road-edge heights and grounded terminal surfaces. The native all-25 capture and packed render-data rebuild remain pending.

## Road and frontage review, 2026-10-12

The resumed source-final pass completed DenseCity: 272 loose presentation groups were regrouped, and the source ledger explained 52 market-stock material rows plus 82 destination rows. Ten road-review sources then saved successfully. The first DenseCity review failed honestly at mission 10's market frontage: the existing group touched a visible road, and all clear replacement candidates failed terrain support. Detailed rejection evidence is retained in `destination-ground-failure-DenseCity-10.txt`; 366 candidates failed navigation/slope checks and four had 20–32 cm of surface relief.

The corrective pass adds a shallow render-only stone foundation only where a DenseCity frontage has 15–35 cm of verified relief. Road/build-lot clearances, navigation eligibility, collision and functional owners remain protected. New foundation material rows must be explicitly included in the next source ledger and packed-data rebuild. Execution is in `validation/road-review-04.log`; final acceptance awaits its completion and previews.

Visual inspection of the new AshLinePort grade-join image confirms continuous asphalt and sidewalk heights. Citywide's utility props now clear the carriageway. Chapter 5 terminal previews show curbs descending to the ground. Runway previews for all three sources show cleared surface intrusions and coherent thresholds; these source previews do not establish aircraft gameplay acceptance.

The recurring Tent_Regular warnings are now traced to the generic AI plan's default base coordinates and first tent entry, rather than established as initial-building failures. The empty Match initial-faction asset and the repeated search timing support this diagnosis. The AI agent has been notified; required Campaign building spawning remains unchanged.

## Source visual completion checkpoint

`road-review-04.log` passed the owned wrapper: 54 paired road joins graded, 240 asphalt/sidewalk terminal surfaces grounded, 22 false terminal bands removed, and 12 destination groups moved off visible traffic surfaces across eleven sources. Three compact DenseCity sites received shallow stone foundations, adding exactly three material rows.

`road-ground-01.log` passed with 14 graded dune meshes. Its visual review found separate raised SandEdge props still crossing asphalt, so it was not accepted as the final road appearance. `road-ground-02.log` then passed with 256 additional sand-edge mesh repairs across the same existing sources. The DenseCity market-frontage closeup in `road-ground-v2/` was inspected: the carriageway and sidewalks are now clear, with terrain ending behind the curb. No collider, road footprint or gameplay owner changed.

The final source ledger is 124,305 rows: baseline 124,168 plus 52 market-stock and 85 destination/foundation rows. Source SHA-256 is `2cf73f3a79e7cf6996d3e3c3ce75f2245833f21d5c3ca6fa9c5ff6c33eef5a18`. The inventory gate now pins this exact revision and branch counts; packed rebuilding/determinism/source parity are still pending.

The other AI agent confirmed no overlap with generic AIBuildPlanner work. A narrow Campaign catalog eligibility preflight is implemented with focused tests, awaiting successful compilation and execution. It filters generic AI choices before placement search; it does not edit the initial-spawn system or discard authored starting grants.

## Packed and native verification checkpoint, 2026-10-12

Packed rebuilding, determinism, and source parity passed in `validation/packed-and-native-25-01.log`: 124,305 source rows; 59,501 eligible and 64,804 resident; 38,909 placements and 7,403 capacity slots. Content hash `a80c796f71c058dfdddcacbd1b624fc0aa8a4fa0b22785fbd29ecaf40f178684`.

Focused AI eligibility, 57 Supply Line regression tests, and Steel Push rules passed in their dedicated logs. Native captures are still running. Citywide, Steel Push, and Trust Under Fire reached Engage with no failed required requests. Supply Line has two required requests blocked by new destination props; correction is staged outside Assets pending completion of the capture sequence. Network Collapse, Last Corridor and Command Node reach an immediate Defeat with one, two and three required-building failures respectively. Their failed evidence is preserved; diagnosis and repair remain open.

Fresh visual review also found First Contact's final introductory camera return overwrites HUD-safe framing; its correction is staged. Airlift's opening now shows its squad and rescue APC above the command tray. Establish Base still needs a more useful legal-build-area composition. The native capture process has dirtied two Match HUD prefab properties (material/interactable); those are capture side effects and must be restored, not integrated as visual work. No commit, merge, normal-input win, or device acceptance is claimed.

## User-requested pause

Capture sequence stopped through its normal Finish/cleanup path at explicit user request. Latest receipt: `captures/saga.ch03.m05.network_break/run-visual-fix-01/receipt.json`; status Passed; . Completed captures: 18/25. Corrections under `staged-placement-repair/` are preparation only and have not been copied into Assets or compiled. No commit/push/merge. Resume from this checkpoint; preserve prior capture directories.

## Resumed camera and placement checks

The 18 completed native captures are preserved. Pending camera and functional-lot corrections were copied into Assets and compiled. Focused opening-framing validation passed all three tests in `validation/opening-framing-04.log`: Supply Line frames the first squad, Power Relay frames rifles/engineers, and Establish Base keeps its full construction area inside the HUD-safe viewport. Gameplay focus is preserved. The earlier failed test runs and a mistaken fully qualified executeMethod dispatch remain in their original logs; they are not passes.

The two incidental Match HUD prefab property changes were reverted after saving their exact diff in `capture-prefab-side-effects.patch`. The capture helper now includes that prefab in its preservation list; subsequent captures must still verify it stays unchanged. Supply Line destination clearance and close inspection of the market road's outer sand patches are underway. Required Chapter 5 building placement failures and seven remaining mission captures are still open. No native playthrough or device acceptance is claimed.

## Repair review native results

`validation/placement-camera-native-02.log` completed eight captures. Supply Line now reaches Engage with Outcome=None and no failed required requests after moving two decoration groups; its first squad is visible. Establish Base reaches Engage and frames the construction area. The Match HUD prefab remains unchanged after capture.

Network Collapse, Last Corridor and Command Node still fail required placements. Fresh runtime diagnostics establish all three grid origins at (860,300); the earlier estimate that Network Collapse used (560,300) was incorrect. The blocked Network civic lot is the second one at (1199,444). Runtime mask surveys identify nearby candidates for all six failures. Matching asset/builder updates are being compiled and need native verification.

First Contact and Power Relay remain visually unaccepted: First Contact's squad moves during the tour; Power Relay's custom units appear after the initial spawn-system framing. The camera now refreshes from live units at its handoff/capture point; fresh captures remain required. Wider source closeups in `market-sand-inspection-01/` confirm residual road mounds outside the earlier accepted center crop. A temporary visibility-isolation diagnostic is being compiled to identify the remaining mesh family.

## Chapter 5 placement and road-mound diagnosis

The targeted native review `run-visual-fix-03` confirms Network Collapse, Last Corridor and Command Node all reach Engage with Outcome=None and zero failed required requests. Six functional origins were moved to nearby sites identified by actual runtime masks; matching builders and civilian center markers were updated. No budget, ownership or readiness check was weakened. Wide Chapter 5 framing still needs a tighter first-action composition.

First Contact's south camera footprint clamp is the remaining reason its squad sits too low; its camera-only south margin has been extended 24 m within the existing physical map, with playable bounds preserved. Power Relay's tour state and runtime component live on different entities; the camera helper now receives the actual runtime component explicitly. These newest corrections require native verification.

`market-sand-diagnosis-02/DenseCity-without-DirtRoad-left.png` isolates the residual market mounds to legacy raised dirt-road exit meshes. Disabling sand-edge, dune, ground, sidewalk or rock families did not remove them. Visibility changes were temporary and not saved. A corrective grading pass now targets only render-only dirt-road portions overlapping existing asphalt/curbs, retaining transforms, colliders, owners and XZ footprints. Its execution and packed-source reconciliation are pending.

## Wider market road review

Both DenseCity outer frontage previews in `road-entry-grade-01/DenseCity-after-market-left.png` and `DenseCity-after-market-right.png` were visually inspected. Asphalt and sidewalks are continuous and free of the earlier raised dirt-road intrusions. Off-road relief and authored vehicle wrecks remain. The shared-source pass is still running; packed rebuild and native runtime verification are pending.

## Mesh reuse preparation

The corrective road pass produces many byte-identical mesh assets for repeated road modules. At an intermediate checkpoint, 713 generated assets contained 205 unique serialized payloads. An exact-payload sharing helper is staged outside Assets while source mutation continues; it has not run. It will replace duplicate mesh references through the live Editor, preserving geometry, UVs, transforms and protected gameplay data. Packed validation must follow this consolidation, not precede it. CityEdgeAirfield, RefineryDistrict, AshLinePort and GroundedSignal entry previews were also visually inspected with continuous curbs and preserved dirt-lane surfaces.

## Road-entry wrapper timeout

The 2400-second `road-entry-grade-01.log` wrapper returned exit 124 before receiving completion. Ten source receipts existed at timeout, and the preserved Editor was still processing Command Node. This invocation is a failed validation even if its underlying Editor method later finishes. Do not restart or edit Assets while that method is active. A new wrapper invocation and receipt are required before accepting the completed source pass.

## Road-entry source pass validated

The preserved Editor completed all eleven sources with 1,222 corrected meshes. The initial invocation remains a timeout failure. `validation/road-entry-grade-02.log` then returned zero and passed reconciliation with all 124,305 source rows preserved. Previews from every source were inspected, including the wider left/right DenseCity market frontage. Road mesh payload consolidation is now compiling; packed and native verification remain pending.

## Reload and memory-pressure checkpoint

Mesh reuse helper installed after the successful road-entry recheck. Recompile initially reported compiling=true/method=false, followed by repeated Pipeline main-thread timeouts during reload. No mesh-reuse executeMethod has run. macOS swap storage is 34 GiB and system free space fell to approximately 372 MiB; T7 has 256 GiB free. All eleven source scene saves and road-entry receipts exist. No Editor, Hub or licensing process was terminated; explicit recovery permission requested for only the audit Editor. No shared cache was deleted. Packed rebuild, final native checks and integration remain pending.

## Reload recovered; safer mesh sharing

The audit Editor recovered naturally, compiling=false and mesh reuse method loaded. Restart request is withdrawn; no process was terminated. Automatic approval review rejected the combined scene-sharing/deletion pass because deleting hundreds of generated mesh assets was too broad. That command never ran. The helper was revised to retain every original mesh asset and only share byte-identical payloads; no DeleteAsset operation remains. This safer alternative still needs compilation and checked execution.

The owner authorized recovery of only the audit Editor. It had already recovered naturally; no restart was needed. The safer mesh-sharing helper recompiled successfully and is executing via `road-mesh-reuse-01.log`. It retains all 1,222 assets and targets 977 duplicate references across 245 exact unique payloads. Free system space recovered to 6.4 GiB without cache deletion.

Mesh sharing finished all11 per-scene protected-data checks and replaced977 references with byte-identical canonical meshes; alloriginal assets remain. A separate cross-run hash equality check failed atCityEdgeAirfield and is retained in `road-mesh-cross-run-hash-check.txt`. That extra assertion was invalid because RefinementProtection uses Editor GetEntityId values and serialized object references across different scene loads. No protected check inside either pass failed; persisted source SHA and render-row reconciliation remain the cross-run evidence.

## Mesh reuse passed; packed rebuild resumed

`road-mesh-reuse-01.log` returned zero with977 shared references,245 exact unique mesh payloads, alloriginal assets retained, and124,305 source rows preserved. The final source SHA is`bc250a60f2fb3050c691265273df13a7bea3fa2420fe4bef1a58debad75d2ace`; the strict inventory gate now pins that revision. Under explicit owner recovery approval, only audit Editor PID31799 was gracefully shut down after completion and verified exited. A fresh GUI-licensed wrapper now runs `OperationMapRenderDatabaseBuilder.RunSourceParityValidation` in`packed-road-entries-01.log`, including two byte-compared builds. Main Editor, Unity Hub and licensing processes were untouched.

## Final corrected-source packed checks

`packed-road-entries-01.log` passed the inventory, both builds, byte determinism and source parity checks. Source rows124,305; eligible59,501; resident64,804; removed0; placements38,909; capacity slots7,403. Content hash`746887b5f55cd8b642c61cecb5aa573aa40a0817caa254a7f2120823263b72de`; ordering hash`c5da36e290255e3ace5b219571709bab73be44d219eb8bafbea23a9f74668658`. Config38,194,419bytes; report1,103bytes. Native camera and remaining mission captures are next; no gameplay/device acceptance is claimed.

### Latest opening review correction
Power Relay convoy focus is intentional: guidance step 1 chooses family member Kind=2, and tutorial focus centers the source actor. The earlier rifle/engineer concern was a mistaken assumption. The run-visual-fix-04 full HUD is accepted for opening composition. Network Collapse, Last Corridor and Command Node also keep their starting actors clear of controls; required placements passed. These are visual/startup checks, not completed normal-input playthroughs.

### Final native review progress
Targeted run visual-fix-04 completed all 8 captures with no required-placement failures. Reviewed full HUD for First Contact, Power Relay, Network Collapse, Last Corridor, Command Node, Airlift, Market Lifeline and Route Reopened. Market roadway and curb intrusion repair is visible in the final native detail view. The remaining seven-mission sequence is running through remaining-native-02.log; Air Corridor, Split Front, Grounded Signal and Armor Break have completed with Outcome=None and no rejected building requests. Air Corridor assembled runway endpoints and Grounded Signal/Armor Break destination details were visually inspected. Main branch fetched; only website commit 5e69c0e6c is new. No tracked overlap with the active main agent at this check. No main checkout modification or integration performed.

### Final visual evidence check — passed
All 25 missions have final-source native captures and manually reviewed opening HUDs. `verify_final_review.py` passed: all wrappers exit 0, mission outcomes None, required-placement failures 0, one shadow key per receipt, packed source parity and determinism pass. The updated gallery links the final captures. Full normal-input mission completion and Android acceptance remain pending. Authored code/shader diff check passes; generated Unity empty-field whitespace is classified separately.
