# CH05-M02 Trust Under Fire — review readiness

Updated 2026-10-02. Implementation and validation continue; player readiness is not established.

## Current checkpoint

Latest `core-rules-02.log`, `support-refresh-02.log` and `packed-content-04.log` pass with neutral broadcast ownership and protected Support regions. The latest pack has zero Burst errors and 87,609,449 bytes of isolated output. Deterministic clearing of this task's old output removed 126,242,820 bytes of obsolete archives; shared caches and source remain preserved.

Corrected native briefing shows CH05-M02 with matching art, objectives and rewards. `gameplay-en-04.log` exits 0 with the required GameplayOnly pass marker: all four native building grants succeeded, both convoys crossed their independent bridges and held at their shelters, all nine attackers died, the engineer verified the intact neutral broadcast, and actual victory settled exactly one first-clear Supply Drop before Campaign return. Optional Smoke approval also passed. This run used native caption skipping; voices remain pending.

Native result review found inherited Defense labels and clipped headings; these were replaced with actual protected-convoy, verified-source and shelter-staff outcomes, compact headings and actual reward amounts. Persian ARIA `gameplay-fa-01.log` exits 0 with normal-input victory, Support decline, settlement and Campaign return at 09:50. Its result screenshot has three stars, all nine attackers stopped, no escort/staff losses and one Supply Drop. The inherited preparation banner now shows actual Trust stage, hold progress and evacuation deadline. A remaining RTL countdown punctuation issue is being corrected to explicit minute/second units; a separate native presentation capture will check that correction without claiming another complete playthrough. Player/device acceptance remains separate.

The final timer correction is verified by `presentation-05.log` and `presentation-fa-01.log`, both exit 0 with required markers. Native `Evidence/20261002-164819-fa-IR-manual-captioned/last.png` displays **14 minutes and 50 seconds** correctly, avoiding RTL colon reversal. The presentation probe uses normal launch/Skip inputs and a real validated source; its marker explicitly says `gameplay=NotClaimed voices=Pending`. It is separate from the two complete gameplay wins above.

## Mission

Use the approved AshLinePort sector and existing Campaign controls, ARIA Play/Stop and approved markers. Two original protected evacuation vehicles must cross their assigned bridges and stop at living shelters for six seconds. Four original relief staff must survive. Defeat the nine actual military attackers and move the original noncombat engineer to verify the preserved broadcast source. Continuous bridge passage requires west entry, middeck and east exit in the assigned lane; incomplete proof resets off route.

This is the canonical named convoy fallback, because ordinary boarding controls do not accept neutral refugees. There is no passenger-manifest or boarding claim. The finite reserve supplies 100 Fuel, protects 20 for shelters and has 240 capacity. One previously earned Smoke is optional and requires consent. Supply Drop is earned exactly once on first clear for the following mission. No invented persistent Trust meter gates evidence.

## Separate evidence

- **Visual direction:** existing UI/map/comic/marker direction approved. Native Campaign layout inspected. Corrected native minimap visually inspected: canal, bridges, roads, warehouse yards and civic areas are distinguishable. Briefing fallback fields required correction; new native screen check pending.
- **Automated checks:** `build-core-03.log` and `core-rules-01.log` exited 0 with required core, map, rules and objective markers. Saved scene has 195 gameplay owners, 2063 render-only identities and 2258 unique identities, one Grid/Surface, two full vehicle routes and four qualified plots. All six source assets remain unchanged. Rules check crossing bypasses, actual objective publication, initialization safety, failure precedence, terminal idempotence and exactly one first-clear Supply grant.
- **Content packing:** `packed-content-02.log` exited 0 with UnitFixture/PackedContent markers and zero Burst errors. Own entity and Addressables output is 133,434,122 bytes. This establishes packaging only.
- **Normal input:** English manual gameplay passed in `gameplay-en-04.log`, including Support approval, real victory, rewards and Campaign return. Persian ARIA passed in `gameplay-fa-01.log`, including Support decline and actual result/settlement/return. Both used native comic Skip; fourteen voices and natural playback remain pending. Isolated profiles seed prerequisite missions only; target outcomes are not injected.
- **Voices:** captioned three sequences/seven panels exist. Fourteen EN/FA provider clips and natural playback remain pending. Exact upload approval is pending after automatic review rejection; no new external request occurred.
- **Player/device acceptance:** pending user review and device testing, separate from Editor automation.

## Preserved failures

1. `build-core-01.log.dispatch.json`: dispatch failed during script reload, before validation ownership.
2. `build-core-02.log` (exit 1): shelter footprint crossed preserved road at world 940,519. Source sample was Road/type1/mask15/road flag. Shelters relocated to qualified origins 922,519 and 922,399. Corrected full native gate passed in build-core-03.
3. `packed-content-01.log`: output marker and exit 0 occurred, but Burst BC1016 in guidance string append makes this run unacceptable. Typed FixedString fix retained Burst; packed-content-02 had zero Burst errors.
4. `gameplay-en-01.log` (exit 1): actual launch rejected Fuel depot request 4 at world 815,630 as Blocked, before readiness. Shelters and broadcast requests 1–3 succeeded with exact 14×12 footprints and 1000 health; all 26 original units were initialized and alive. Live placement diagnostics and an unchanged-gate candidate survey are being added. No alternate placement or validation bypass is enabled.

## Git delivery

Citywide checkpoint fa4d943 is committed. Specific approval to push that checkpoint to main is pending after automatic review rejected the broad default-branch upload. Trust's captioned gameplay checkpoint is prepared for commit, with both normal-input journeys passed and the voice gap explicit. No new push has been retried. The repository history records the exact Trust checkpoint ID.

5. `gameplay-en-02.log` (exit 1): exact live rejection was 182 blocker cells, first world 832,630; road, sidewalk, water and bounds checks were clear. Passive unchanged-gate survey qualified the full depot lot at world 780,650. `build-core-04.log` and `packed-content-03.log` subsequently passed with the relocated lot; packing had zero Burst errors.
6. `gameplay-en-03.log` (exit 1): all four actual building requests succeeded, mission readiness reached 1 and normal manual controls entered Engage. BroadcastLost occurred around 25 seconds because all three relay guards were actually attacking broadcast Entity(802:7), which incorrectly had player ownership. Correct protected civilian ownership is being implemented; health/damage/guards remain real. Complete target victory remains unproven.

The captured corrected briefing now shows CH05-M02, matching art and stars. Further compact localization addresses clipped objective/intel/reward labels. The task packing directory accumulated 126,242,820 bytes of obsolete unreferenced archives; deterministic clearing of this task's rebuildable output before future packing is being added, preserving source and evidence.

- GameplayEN03: all four supplied buildings placed successfully with exact own footprints (including native-qualified reserve780,650). Failed at25s because relay guards targeted the friendly-owned broadcast; actual protected ownership repair is pending. Complete journeys remain pending.
- Task packed-content output audit: five obsolete unreferenced Entities archives occupied126,242,820B; current catalog references three archives totaling80,419,925B. Builder now clears only its isolated `Library/TrustUnderFirePreparedContent/Entities` and `/Addressables` before rebuilding. No cleanup has been executed by the map agent; shared AA, source and retained evidence are preserved. Fresh packed receipt is pending.
