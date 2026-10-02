# CH05-M03 Network Collapse — review readiness

Ready for Editor review in the primary checkout. English manual and Persian ARIA native journeys passed. Final player-content compilation qualification and real player/device acceptance remain pending.

## Scope

Build the next mission from the approved Chapter V story, Urban map plan and existing UI/marker/comic direction. The user requested **no voice sending** for this mission. EN/FA dialogue remains local and captioned; no provider requests, credentials or prior-mission voice reuse are authorized by this build.

## Planned normal-input contract

Three distinct command nodes must be verified and disabled in order. Defeat each actual guard group and bring the original evidence engineer to its external reconnaissance gate for six seconds before the node becomes a legal target. Use the approved ground-raid fallback through existing Move/Attack controls. Preserve two civic structures, four civilian staff and the audit owner. After all three nodes and actual military guards are disabled, recover the audit through the original engineer’s six-second custody hold, board the real APC and extract it through normal Move plus a six-second hold with that engineer actually aboard.

Supply Drop is optional: one previously earned charge, three tactical Fuel and a collectible crate of forty Materials through existing Support controls. Required objectives and stars work without Support. ARIA Play/Stop remains visible. No new gameplay button, purchased replacement, unverifiable scan or long-range risk control is introduced.

## Gates

- Visual direction: existing direction approved. Native EN and Persian briefing, captioned comic, HUD and result captures inspected; fourth briefing objective is populated, no new command buttons, timer units visible and Persian text shaped.
- Automated map/rules/objectives checks: passed in final finish-core-04.log; initial packed-content-01.log passed; final packed-content-02/03/04.log emit persistent Burst assembly-hash errors and are FAILED qualification despite content/bridge pass markers, with wrapper exit 0 and exact completion markers. Six source hashes preserved; 2972 identities, 314 owners, own 440×220 map; actual objective writer and original-passenger rules exercised.
- English manual normal-input journey: passed (input-en-03.log, wrapper exit 0), seven captioned panels, actual ordered recon/combat/audit/Board+Move extraction, optional Supply approval and real 40 Materials collection, 3/3 stars at 07:16, rewards settled and Campaign return. Persian ARIA normal-input journey: passed (input-fa-02.log, wrapper exit 0), all seven captioned panels, actual nine-target combat/recon/audit/boarding/extraction, optional Supply declined with no effect or charge consumption, 3/3 stars at 07:27, rewards settled and Campaign return.
- Real player/device acceptance: pending, separate from Editor automation.
- Voices: not requested; caption-only build.

Failed evidence retained: import-01-failure.txt (two fixed compilation errors) and finish-core-01-dispatch-failure.txt (scheduled request lost during script reload; no validation pass). Fresh finish-core-02.log passed. No gameplay pass is implied by these checks.

## Native integration failures retained

- input-en-01.log: saved transport-enabled scenario was rejected by the generic spawn gate; no roster initialized. A narrow Network restriction gate and actual authored-config regression fixed this; finish-core-03.log passed.
- input-en-02.log: node one was actually verified and disabled, but optional Supply was unavailable because the catalog flag was still false; Materials capacity displayed 80 rather than authored 240 because the resource totals utility overwrote the candidate capacity. Full playthrough is pending these fixes.

- input-fa-01.log: normal ARIA combat/recon reached node-one disable, but the probe camera drag inverted when the Supply ground point projected behind the camera. Failed run retained; normal camera drag correction in the probe, no gameplay outcome injection. Fresh full journey input-fa-02.log is pending.

## Final candidate fixes

Network has a narrow transport-enabled spawn contract. Materials initialization applies the 240 capacity after the common totals helper. Only Supply readiness is authored; Strike and Paratroopers remain unqualified and disabled for this mission. The real Supply approval, 3-Fuel receipt, landed 40-Materials crate, ordinary engineer collection and empty stock passed in English; decline with no effect passed in Persian. The ground raid uses existing Move/Attack/Board controls. No voices were uploaded, generated or installed; no human/device acceptance is claimed.

- packed-content-02.log: wrapper returned 0 and content marker, but Burst reported an internal stale assembly/type hash error in AISquad BuildAvailableUnitCandidatesJob; this run is treated as failed packaging qualification. Recovery requests Unity supported CleanBuildCache script compilation (also used by installed BurstLoader.cs:128), preserves Editor and Burst enabled, followed by the same checked wrapper.

## Pending player-content gate

The final candidate still encounters a Burst internal cache/type hash error in AISquad BuildAvailableUnitCandidatesJob while packaging. Editor clean script compilation and Scriptable Build Pipeline cache purge did not resolve it. Packaging now observes native compiler errors and fails closed before writing a pass report. Earlier reports with pass markers plus Burst errors are explicitly not qualification evidence. A primary Editor restart for compiler-environment recovery requires user authorization under AGENTS.md; Hub and the unrelated Fig Editor must remain open. Source and complete native mission are reviewable; a final clean player-content pass is pending.

Cache-helper failure evidence: import-02-failure.txt and clean-player-build-cache-01.log. This Editor requires an explicit Unity.ScriptableBuildPipeline.Editor assembly reference for the actual installed BuildCache.PurgeCache API. clean-player-build-cache-02.log requested that API successfully; it did not fix the Burst error.
