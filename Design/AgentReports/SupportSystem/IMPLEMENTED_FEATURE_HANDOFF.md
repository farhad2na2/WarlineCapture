# Support implementation handoff

Implementation source: `IMPLEMENTATION.md` and `TASKS.md`, planned in commit `13f567558`, audited in `67a2fea48`. The approved visual direction is `Mockups/05-support-fullscreen-v02-project-assets.png`.

## Implemented behavior

The existing Support command opens a separate full-screen four-card popup. Scan retains its own route. Targeting accepts a fresh world gesture after the popup closes, permits camera pan, displays exact cost/target/validation feedback, and keeps ARIA Play/Stop visible. Propose, Show Target, Decline and Stop never spend; one current explicit approval is required for each ARIA action. English and Persian localization uses existing project fonts and assets.

| Ability | Mission allocation | Physical Fuel | Effect |
|---|---:|---:|---|
| Smoke Screen | 2; 35 s cooldown | 1 | 12 m, 15 s, direct ranged damage ×650/1000 for grounded units of either faction |
| Precision Strike | 1; 120 s cooldown | 4 | Real jet approach and one 250-damage impact on a visible, confirmed hostile military vehicle/building |
| Paratroopers | 1 | 6 | Existing transport and four canonical rifle soldiers with real parachute descent and ordinary command after landing |
| Supply Drop | 1; 100 s cooldown | 3 | Existing transport and one plain 40-Materials crate; explicit selected-unit collection through ordinary pathfinding |

Physical Fuel uses eligible tactical stores, outbound reservations and the civilian reserve floor. Aircraft hold Fuel during approach and consume at first release/impact. Abort before release restores a charge and surviving held Fuel exactly once. Partial delivery retains released payload and spent Fuel, removes held passengers and reports a partial receipt. Supply collection grants only accepted free Materials capacity; remaining stock persists, claim ownership prevents duplicate collectors, and a replacement order cancels collection. Unreachable path fallbacks cannot transfer stock outside the two-metre interaction radius.

Support follows the actual Campaign attempt/session, simulation clock and outcome. Missing knowledge, route, manifest, cap or matching authoring fails closed. Hidden/airborne/descent/protected targets remain ineligible where specified. No battle service/controller, account Fuel fallback, outcome injection or raw Entity save format was introduced.

## Campaign integration

| First-clear milestone | Permanent reward | First allowed use / optional lesson |
|---|---|---|
| CH04-M02 Steel Push | Smoke Screen | CH04-M03 |
| CH04-M03 Split Front | Precision Strike | CH04-M04 |
| CH04-M04 Grounded Signal | Paratroopers | CH04-M05 |
| CH05-M02 Trust Under Fire | Supply Drop | CH05-M03 |

The real Steel Push reward asset, rebuild recipe and bilingual debrief now include Smoke. All four named grant/source/amount pairs are whitelisted together with application; duplicate ownership creates no blueprint compensation. Versioned profile normalization derives earned ownership only from completed milestones and preserves legacy/unrelated IDs. Earlier replays continue hiding Support. Existing pending-resume behavior starts a fresh attempt; this feature adds no mid-mission persistence route.

Future mission hooks are implemented. The repository currently has deployable Campaign content through CH04-M02. Later canonical mission content, its actual lesson use/skip/result/return paths and production exposure remain pending. Catalog production flags remain disabled while canonical mission, player and device readiness gates remain unaccepted; test grants are explicit and do not replace the real mission/attempt. No whole replacement mission was created to mark Support ready.

### Authoring an eligible canonical mission

1. Create a `SupportMissionContextConfig` with the exact runtime MissionId, OperationMapId and positive MissionSourceVersion/revision. Set finite ground bounds and explicitly visible/protected rectangles. Do not infer visibility from the whole map.
2. Attach `SupportMissionContextAuthoring` to the actual map/mission content and assign that config. The baker produces unmanaged context/region data. At runtime exactly one matching context is projected; absent, mismatched or ambiguous contexts clear the old ground/route/cap data.
3. Author finite Entry/Release/Exit anchors and positive anti-air clearance for aircraft. The runtime revalidates known anti-air and the actual target-derived drop route. Supply uses the transport, not the emergency-crate vehicle identity.
4. Author a positive infantry population ceiling for Paratroopers. Canonical Barracks rifle prefab/count and existing parachute/plain crate are projected through the real unit registry. Population includes live, queued and held infantry once.
5. Add only the approved first-clear grant and lesson/exposure row after that mission's acceptance. The lesson is optional popup copy; Close skips it without cost, orders or an objective gate. Complete Support and no-Support mission journeys before exposing production content.

## Evidence and remaining gates

Full validation logs, exit receipts, native screenshots and failed attempts are in `Evidence/`; `implementation_progress.md` records corrections and chronology. Keep these separate:

| Gate | Status |
|---|---|
| Approved design | Approved v02 visual direction |
| Native visual review | EN/FA popup and all lesson cards rendered without truncation; actual jet, scaled transport, source canopies/crate and established Smoke reviewed in native captures. Player visual-direction acceptance remains separate |
| Automated checks | Current focused configuration/runtime/Smoke/UI/Strike/Paratroopers/Supply/campaign/context checks pass; repository-wide architecture failures are recorded separately |
| Complete normal-input journeys | Smoke English/Persian, no-Support, Strike player/ARIA, final Paratroopers and final Supply passed through actual victory, settlement and Campaign return |
| Real-player acceptance | Pending user/player review |
| Target-device acceptance | No Android device connected (`adb devices -l` on 2026-09-28); mobile playthrough, responsiveness and profiler measurements pending. 0 B/frame has not been claimed without profiler evidence |

Validation reuses the existing WarlineCapture Editor through `Tools/CI/invoke_unity_macos.sh --reuse`. Unity Hub remains open, GUI licensing remains in use, and no new project/Editor is opened. Ordinary `unity command editor_stop` exits Play while preserving the same Editor.

### Current field evidence

- Paratroopers: `Evidence/SmokeJourney/20260928-201007-en-paratroopers/`, full journey log `support-paratrooper-journey-20260928-03.log`, exit 0. Actual four descents and normal troop movement; current transport scale and scaled source-door release validated.
- Supply: `Evidence/SmokeJourney/20260928-201537-en-supply/`, log `support-supply-journey-20260928-06.log`, exit 0. Source canopy/plain crate; 40 Materials stock, actual infantry collection accepts 20 and retains 20 at full capacity. The first-release canopy shot is cropped high; landed capture establishes source asset fidelity.
- Smoke presentation: `Evidence/SmokeJourney/20260928-202247-en-smoke/smoke-established.png` shows the actual five-second source particle field while keeping units visible. Runtime cover, two direct-damage observations, real expiry/cooldown, both exact one-Fuel commits and exhausted UI have passed in this run; full result/settlement/return passed with wrapper and receipt 0 in `support-smoke-final-20260928-2023.log`.

Each completed current Paratrooper/Supply journey includes real ARIA preparation/combat, intentional normal player takeover, an actually opened Support popup, Victory interruption, first-clear settlement and Campaign return.

The 56 focused Support cases and 119 related regression cases pass. Existing repository architecture checks still fail on the HEAD UI-to-components assembly dependency, 37 baseline snapshots and nine pre-existing classification findings. Support adds no remaining snapshot/classification finding; no ceiling or assertion was weakened. The full evidence index links passes and retains failures.
