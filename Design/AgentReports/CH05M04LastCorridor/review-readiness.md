# CH05-M04 Last Corridor — review readiness

Ready for player review in the primary project. Native Editor gates passed; real player/device acceptance remains pending.

## Scope

The next approved campaign mission, saga.ch05.m04.last_corridor. Uses the documented independently versioned bounded Urban logistics fallback because Frontier device qualification remains conditional. Main and alternate delivery lanes share one physical artery; they are not two separate streets. Preserves connected entry/midpoint/exit proof, five original delivery categories, physical key custody, finite Fuel and a fifteen-minute deadline. Existing Campaign, ARIA, controls, selection/ground markers and comic art direction reused. No new gameplay buttons or voice requests.

## Mission contract

Clear seven military guards with the original escort. Bring the original engineer to repair the visible broken lane for six seconds. Deliver the original medical truck on the safer lane. Choose a certified lane for the Fuel truck; the original forty-barrel manifest is debited from the real 160-barrel source depot, then credited once into the separate city receiving depot after route proof and a six-second receiving hold. Move the original reinforcements to reception, return the original engineer to the original key APC, Board through existing controls and deliver engineer plus physical authority keys along the chosen route. Protect the civic receiving site, four original civilian staff and twenty barrels of civilian Fuel.

Supply is optional through existing controls: one earned charge, three Fuel and forty actual Materials collected from the landed crate. Unqualified air/long-range abilities remain visibly disabled. Caption-only English/Persian comics; no audio generation or sending.

## Gates

- Visual direction: existing approved direction reused. Native English Campaign, briefing, comics, HUD and three-star result inspected. Persian briefing copy was shortened after a clipped earlier screenshot; final native briefing and HUD inspection passed. Final Persian comics and three-star result inspected and readable.
- Automated map, rules and objectives: passed (`core-validation-06.log`), including original cargo/custody, physical recovery, single Fuel transfer, settlement and tutorial progression. Final native packed content passed (`packed-content-03.log`, wrapper exit0, zero Burst/C# errors), matching refreshed map hash `f2cb98c48c10370d47d65c0494dfd4051ef22f2738159ad62d2048c5adca536c`.
- Complete normal-input English manual journey: passed on the refreshed separated-receiver candidate (`input-en-08.log`, wrapper exit0). Actual five deliveries, original engineer/APC custody, physical repair, optional Supply approve/collect, seven captions, three-star result, rewards and Campaign return passed.
- Complete normal-input Persian ARIA journey: passed (`input-fa-02.log`, wrapper exit0) without assistance. Supply proposal/decline had no effects; normal Stop/Select/Move chose the alternate Fuel route and ARIA resumed. Actual five deliveries and original custody, all seven captions, three-star result, rewards and Campaign return passed. Native completion time10:19, within fifteen minutes.
- Persian optional Supply: proposal/decline passed without effects (`input-fa-01.log`). One ordinary generated camera drag moved the target clear of a HUD button; no gameplay/camera-state commands or outcomes were injected. Probe framing now keeps ground gestures within the central visible area. That candidate later stopped when ARIA reported Blocked. FA02 subsequently passed the whole corrected candidate without assistance.
- Real player/device acceptance: pending, separate from Editor automation.
- Voices: excluded by latest user instruction; local captions only.

## Failed evidence retained

- `input-en-01.log`: briefing stalled because the mission was missing from the narrative policy; actual policy and regression corrected.
- `input-en-02.log`: missing prompt-to-step mapping blocked the native action flow; mapped prompts125–132 to player steps1–8 and added a regression.
- `input-en-03.log`: Supply selection attempted an off-screen Select-mode drag; probe now uses the existing camera-only Show Me control.
- `input-en-04.log`: normal legal junction turn at1195.06,440.4 reset the eight-metre route proof twice. The first correction allowed ten metres; the complete turn trace later reached11.5m. The final bound is strictly below twelve metres; ordered five-metre waypoint visits remain required and twelve-metre departure still resets. Native surface at the captured turn supports wheeled movement; no source terrain, blockers or pathfinding were changed.
- `input-en-05.log`: valid Supply release did not reach Preview because the early input helper used cached pointer-over state. It now raycasts the actual release position against UI; existing drag/modal/eligibility checks remain. Probe retries a missed ground touch while Targeting. No gameplay outcomes were injected.
- `input-en-06.log`: final candidate Supply passed, but the fuller legal junction turn exceeded the first ten-metre tolerance. Captured full-turn regression now preserves11.5m and rejects12m; a fresh complete journey is required.
- `input-fa-01.log`: native Supply decline and alternate Fuel delivery passed, but ARIA stopped during the engineer return because stage*64 goal identifiers collided in its modulo64 visited-milestone mask. Distinct real milestone IDs now renew the existing watchdog; native regression also confirms a stationary objective still stops. Fresh FA02 passed all remaining gates.
- `import-aria-01-failed.txt`: helper initially crossed an assembly boundary (CS0234); moved to UI.Contracts. Fresh compilation succeeded and core06 passed. Finish04 used old assemblies and is not final evidence; Finish05 regenerated the corrected Persian copy.
- `input-en-07.log`: complete successful older saved candidate; receiver refresh was still needed. `complete-prepared-03.log` updated the native map to the separated receiving layout; EN08 qualifies that refreshed map.
- `core-validation-02-wrapper.txt`: scheduling was cleared during script reload; wrapper exit124 preserved the Editor. Freshly compiled native checks passed in runs03/04.

These failed candidates do not establish readiness. Later successful evidence must cover the corrected candidate.

## Final receipt — 2026-10-03

[verification-receipt.json](verification-receipt.json) records exact successful native logs, wrapper exit codes and source preservation. Review Chapter V, M04 Last Corridor. Native English completion9:01 and Persian ARIA completion10:19; both earned3/3stars with0escort/stafflosses. Caption-only mission by request. Normal Editor/UI testing and content packing do not establish real-device acceptance.
