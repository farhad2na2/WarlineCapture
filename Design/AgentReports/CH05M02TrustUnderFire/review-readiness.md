# CH05-M02 Trust Under Fire — review readiness

Updated 2026-10-02. Ready for user review. Native validation is complete; real player/device acceptance remains pending.

## Mission

The approved AshLinePort sector uses existing Campaign controls, ARIA Play/Stop and approved markers. Two original protected evacuation vehicles must cross their assigned bridges and stop at living shelters for six seconds. Four original relief staff must survive. Defeat nine actual military attackers and move the original noncombat engineer to verify the preserved broadcast source. Continuous bridge passage requires west entry, middeck and east exit in the assigned lane; incomplete proof resets off route.

This is the canonical named convoy fallback because ordinary boarding controls do not accept neutral refugees. No passenger-manifest or boarding claim is made. The finite reserve supplies 100 Fuel, protects 20 for shelters and has 240 capacity. Previously earned Smoke is optional and requires consent. Supply Drop is earned exactly once on first clear for the following mission. No invented persistent Trust meter gates evidence.

## Separate evidence

- **Visual direction and native review:** existing UI/map/comic/marker direction approved. Campaign, corrected briefing, minimap, HUD and actual English/Persian result screens inspected. Results show protected convoys, verified source, all shelter staff safe, three stars, nine attackers stopped, no escort/staff losses, 2700 XP, 12000 credits and one Supply Drop. The Persian timer correction is independently captured in `presentation-fa-01.log` (exit 0); explicit minute/second units avoid RTL colon reversal.
- **Automated checks:** latest `core-rules-02.log` and `support-refresh-02.log` pass with neutral broadcast ownership and protected Support regions. Scene has 195 gameplay owners, 2063 render-only identities, 2258 unique identities, one Grid/Surface, two vehicle routes and four qualified plots. Six source assets remain unchanged. Rules cover crossing bypasses, actual objectives, initialization, failure precedence, terminal idempotence and exactly one first-clear Supply grant.
- **Content packing:** `packed-content-04.log` exits 0 with required UnitFixture/PackedContent markers, zero Burst errors and 87,609,449 bytes of isolated output. Deterministic clearing of this task's old output removed 126,242,820 bytes of obsolete archives, preserving shared caches and source. Packaging is separate from gameplay evidence.
- **Voice generation and installation:** fourteen EN/FA clips generated once, independently audited against canonical dialogue, cast and file hashes, then installed. `voice-install-01.log` exits 0 with `clips=14 lines=7 locales=2 preload=0 runtimeNetworkTts=0`. Caption duration uses the longer locale duration plus margin. Failed approval attempts and successful exact-question/direct-human-reply authorization evidence are retained in Narrative reports.
- **Complete normal-input English:** `voiced-journey-en-02.log` exits 0 with `scope=VoicedJourney voices=7 dialogue=7 normalInput=Passed result=Passed settlement=Passed return=Passed`. Manual orders, optional Smoke approval, actual victory at 08:41, full natural playback of seven clips, earned rewards and Campaign return passed. Evidence: `Evidence/20261002-174518-en-manual`.
- **Complete normal-input Persian:** `voiced-journey-fa-01.log` exits 0 with the same required seven-voice/seven-dialogue and gameplay/result/settlement/return markers. ARIA, optional Smoke decline, actual victory at 09:49, full natural playback and Campaign return passed. Evidence: `Evidence/20261002-175946-fa-IR-aria`.
- **Earlier captioned journeys:** `gameplay-en-04.log` and `gameplay-fa-01.log` passed real gameplay with native comic Skip. These remain separate from the later complete voiced runs. Isolated profiles seed prerequisite missions only; target outcomes are not injected.
- **Player/device acceptance:** pending user review and device testing; Editor automation does not establish device acceptance.

## Preserved failures


1. `build-core-01.log.dispatch.json`: dispatch failed during script reload, before validation ownership.
2. `build-core-02.log` (exit 1): shelter footprint crossed preserved road at world 940,519. Source sample was Road/type1/mask15/road flag. Shelters relocated to qualified origins 922,519 and 922,399. Corrected full native gate passed in build-core-03.
3. `packed-content-01.log`: output marker and exit 0 occurred, but Burst BC1016 in guidance string append makes this run unacceptable. Typed FixedString fix retained Burst; packed-content-02 had zero Burst errors.
4. `gameplay-en-01.log` (exit 1): actual launch rejected Fuel depot request 4 at world 815,630 as Blocked, before readiness. Shelters and broadcast requests 1–3 succeeded with exact 14×12 footprints and 1000 health; all 26 original units were initialized and alive. Live placement diagnostics and an unchanged-gate candidate survey identified the corrected qualified lot. No alternate placement or validation bypass is enabled.

5. `gameplay-en-02.log` (exit 1): exact live rejection was 182 blocker cells, first world 832,630; road, sidewalk, water and bounds checks were clear. Passive unchanged-gate survey qualified the full depot lot at world 780,650. `build-core-04.log` and `packed-content-03.log` subsequently passed with the relocated lot; packing had zero Burst errors.
6. `gameplay-en-03.log` (exit 1): all four actual building requests succeeded, mission readiness reached 1 and normal manual controls entered Engage. BroadcastLost occurred around 25 seconds because all three relay guards were actually attacking broadcast Entity(802:7), which incorrectly had player ownership. Neutral protected ownership was corrected; health, damage and guards remain real. Subsequent complete journeys passed.

7. `voiced-journey-en-01.log` (exit 1): actual victory, both convoy holds, broadcast verification, debrief and result occurred. The final gate lacked a near-end sample for `trust_under_fire-debrief-01`. One-second Editor hitches can skip the observer's 0.45-second window. Production now exposes an exact natural-finish receipt only after audible samples and the existing real completion condition, rejecting pause/cancel/mute/premature stop. The probe requires exact clip evidence from this run before Next. Fresh complete English and Persian voiced journeys passed; this failed evidence is retained.

## Git delivery

Citywide checkpoint `fa4d943` and Trust captioned checkpoint `1cf6185` are pushed to `farhad2na2/WarlineCapture`, branch `main`. The subsequent voice delivery commit includes fourteen clips, bindings, exact natural-playback observation and the complete EN/FA evidence above. Git history records exact delivery IDs.
