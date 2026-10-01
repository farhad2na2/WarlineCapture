# Steel Push: RefineryDistrict outer approach

Date: 2026-10-01. Mission CH04-M02 / `saga.ch04.m02.steel_push`.
Scenario `scenario.ch04.m02.steel_push`. Branch `codex/airlift-airfield-review`.

Status: final desktop native validation passed. Steel Push is ready for player review; human and target-device acceptance remain pending. Split Front / MR-07 has not been started.

## Integration

The authoritative config builder creates independent logical map `opmap.ch04.steel_push_refinery_approach`, hash `6a5fc500346082a96a449e2b161a2e4e7481f2d1be024c634237ed6dc60a3a08`. Its exact physical source is `opmap.skirmish.refinerydistrict_prepared`, hash `2d4fd91a415a68f0299a4075e37730ecd7b746e94e5a2a1e2a6cd7baca687778`.

The crop is x535–900 / z585–685, distinct from Supply Line's x500–980 / z400–580 and Power Relay's z360–535 sectors. The approach corridor is x538–878 / z622–628, with 2,387 qualified surface/grid samples. Surface heights, camera bounds and minimap come from this prepared source. Canonical three tanks, one APC, four rifles and five hostiles remain. Global health, weapon range, movement speed, wave timing and ARIA timeouts are unchanged.

Anchor suffixes use `anchor.ch04.m02.`:

| Role | x / z |
|---|---|
| Forward post / reserve | 805 / 650 |
| Barracks / build zone | 740 / 650; 775 / 655 |
| Front tank | 760 / 625 |
| Support tanks | 748 / 618; 748 / 632 |
| APC / infantry / civilians | 778 / 625; 790 / 625; 835 / 625 |
| Vanguard car / command APC | 580 / 625; 592 / 625 |
| Main tanks / command APC | 545 / 625; 557 / 625; 569 / 625 |
| Contact / fork / Relay | 630 / 625; 756 / 625; 875 / 625 |
| Return focus | 770 / 625 |
| Optional tower / barrier | 700 / 650; 725 / 625 |

The normal spawn resolver places the support tanks at744.5/622.5 and748.5/632.5. Native checks measure actual centers: friendly minimum separation10.77 cells, enemy11 cells. The lateral support line replaced a longitudinal formation that could fight sequentially and fail ARIA completion. Preparation now uses normal Move followed by the existing Hold command to guard the reserve without chasing away. Physical-source geometry remains unchanged.

The finite reserve uses normal building placement in the off-road yard: origin812/650 or808/647, footprint24×23. Its120 military barrels remain separate from40 protected civilian barrels and ambient storage. Build area x710–840 / z630–680. A mission-specific socket hides only render-only vegetation with mesh bounds intersecting the actual depot footprint. Native checks observed8 hidden renderers, zero gameplay health owners and scenery restored on Campaign return. Yard captures show the depot roof clear.

Existing wave delays25/75 seconds, warning timing,240-second deadline, Materials200 and first-clear9000 Credits/2000 XP/one Smoke unlock remain. Replay retains its authored300 Credits reward, with no repeated XP or Smoke. Smoke is not required in its unlock mission. Shortage guidance uses existing Hold; no new gameplay button. Result rows show real completion, friendly-loss and undamaged-reserve conditions, including red unearned states. The Retry PNG is an initialization-frame actor witness; its temporarily gray HUD is not claimed as a settled UI review. Fresh actors/reserve/progress are verified natively. The final English/Persian first-clear results each show two earned stars, two actual friendly losses and an intact reserve; the withdrawal result shows zero earned stars and no reward. Approved selection markers, game typography, portraits and colorful controls remain.

## Final revision evidence

| Gate | Result |
|---|---|
| Config/rules/media | Preparation13 passed exact source, formation ground cells,2,387 corridor samples, finite reserve rules, seven panels,14 locale voices and narrative coverage. |
| English ARIA first clear | Replay09 passed: native5/5 at124,416ms, six ARIA actions,9000 Credits/2000 XP/Smoke once, result and Campaign return. |
| Fresh replay | Replay09 passed: native5/5 at125,323ms, fresh actors/reserve,300 additional Credits, no extra XP/Smoke, successful replay receipt and Campaign return. |
| Persian manual | Manual08 passed: native5/5 at120,741ms, public Select/Move/Hold, all seven settled panels/voices, exact settlement, result and return. |
| ARIA Play/Stop | Manual08 passed public Play/Stop handback before any ARIA action, followed by manual control. English native HUD visibly shows Stop ARIA. |
| Defeat / Retry | Retreat14 passed without live assistance: normal armored withdrawal, real reserve destruction/core breach, visible Defeat with3/5 hostiles stopped and5 friendly losses, unchanged rewards/Smoke, fresh actors and restored120+40 reserve on Retry. |
| Shortage fault fixture | Shortage02 passed native5/5 at113,948ms using normal Select/Hold after explicitly injecting0 military fuel and preserving40 civilian barrels. Separate fixture, not ordinary readiness. |
| Native visual review | Final English HUD, first-clear/replay result, all seven captions and depot reviewed. Persian HUD/result/yard and all seven fully revealed captions reviewed. Partial typewriter captures remain historical. |
| Human acceptance | Pending. Fresh isolated review entry available. |
| Target-device acceptance | Pending. Desktop evidence does not establish mobile performance or map foundation qualification. |

English final first-clear tank travel3.75/19.70/4.50 cells, actual fuel1.45 barrels; replay3.69/27.67/4.40 cells, actual fuel1.90 barrels. Persian manual08 travel2.73/28.62/0 cells, actual fuel1.75 barrels. Protected floor40 throughout. Actual first combat occurred28,924ms /28,658ms /28,743ms respectively. Both enemy APCs were intercepted before Relay x875; observed maximum x632.49 across these journeys. Supporting tanks need not be forced to move when already defending effectively.

Normal tactical orders use the shipping touch-input helper and Select/Move/Attack/Hold controls; menu and narrative navigation use ordinary EventSystem pointer clicks. Isolated saves seed only predecessor availability. No health, positions, mission progress or outcomes are injected to establish ordinary-input wins or the withdrawal defeat. The shortage fixture changes only reserve fuel; its logged120-barrel reduction is the injected fault, not evidence of actual fuel spending.

Final positive evidence: [English ARIA first clear/replay](Evidence/20261001-113429-steel-push-en/), [Persian manual](Evidence/20261001-114616-steel-push-fa-IR/), [shortage fixture](Evidence/20261001-115528-steel-push-en/). [Defeat and fresh Retry](Evidence/20261001-131456-steel-push-en/) passed in Retreat14 without live assistance. All four native wrappers and Preparation13 exited0. Each ordinary first-clear run captured all seven fully revealed panels.

## Retained failures and recovery

- Persian manual07 failed a later normal-input battle despite prior successes: the moving front tank chased away from its support, and the remaining armor could not protect the reserve. The marked defensive position was moved from720 to756 beside the support line, and preparation now requires the player to press existing Hold after moving. Stats and wave timing remain unchanged. Preparation13 passed. ARIA08 failed at the Hold instruction because Watch observed only Move/Attack directly. Its defense-preparation observation now targets the existing visible Hold button; final English ARIA09 and Persian manual08 reruns passed. Caption capture now also observes fully revealed text during AdvanceReady, including while the voice is still playing.

Full closed logs are byte-preserved in `Logs/`, with lengths and SHA256 in `Logs/manifest.json` and explicit overall classifications in `Logs/run-classifications.json`. Historical screenshots/profiles remain in `Evidence/`. Earlier-candidate victories are not promoted to final acceptance.

- Preparation01 failed camera bounds. Preparation04 printed pass markers but contained C#/Burst errors and is failed. Preparation06 failed a Rendering-to-Authoring enum reference. These code errors were corrected. Preparation11 rejected the first off-road support placement; Preparation12 qualified the final road cells. Preparations02/03/05/07/08/09/10 are historical checkpoints.
- Retreat13 withdrew three supporting vehicles and reached genuine reserve-loss/core-breach Defeat with no rewards. A normal full-map navigation assist through Pipeline was needed to reframe the front tank; no entity, health, mission progress or outcome was injected. The overall run then timed out during Retry and remains failed. The probe now reselects the front tank through the same native viewport drag before pressing existing Hold. Retreat14 passed the complete sequence without live assistance.
- Retreat12 moved and held all three support vehicles normally but its infantry formation stopped a fraction below the probe's x860 threshold. The corrected negative withdraws the armored counterforce only; the four infantry remain at their authored post and must face actual armored combat. No unit/mission changes are made.
- Retreat11 selected a tank correctly by viewport drag but its895/650 retreat point lay outside the viewport at the map pan limit. A Pipeline-dispatched normal held touch on the public minimap Zoom Out control changed only map projection, as designed, and did not resolve the world framing. This run is failed, not acceptance. The corrected probe withdraws each unit to865/665, beyond the reserve and within a reachable native camera frame, then presses existing Hold. No mission assets or outcomes are changed.
- Retreat10 showed that clicking inside the full map's current viewport intentionally does not recenter it. The probe now uses a real touch drag of the visible viewport, followed by individual world selection and movement. Production map behavior is unchanged.
- Retreat09 could not fit the seven-unit drag inside the unobstructed viewport during zero-clock preparation. Read-only Pipeline diagnostics confirmed the projection/HUD overlap; the next probe selects and withdraws each support unit separately by normal touch, requiring actual x>860 positions before beginning the defense.
- Retreat08 also reached Victory after late withdrawal and is failed. Retreat09 withdraws supporting units by normal touch during the zero-clock preparation phase, then uses existing Hold to start battle; no entities, positions, health or outcomes are injected.
- Retreat07 reached real Victory because normal withdrawal happened too late; its fail-closed assertion correctly rejected the run. The probe now uses two existing public Zoom Out clicks to place normal retreat input in view; no mission geometry, stats or outcome is changed.
- Retreat01 lacked a pass marker. Retreat02 wrongly printed pass after an unintended victory and is failed. Retreat03/04 correctly failed unintended victories. Retreat05 passed the previous formation; Retreat06 passed the pre-Hold formation; the final Hold revision is checked separately below.
- English replay02 and saved-replay03 reached native victories but failed the collector's incorrect zero-replay-credit expectation. Production's authored300-credit settlement was correct. Saved-replay04 passed the corrected assertion on the older formation. Replay07 passed the earlier support-line hash; final replay09 passed first clear and replay together on the current hash.
- Persian manual02 had a probe helper compile error. Reuse03 could not dispatch. The user explicitly authorized recovery of the failed task validation Editor; only verified task PID86137 was closed. The same task recovery authorization later closed failed socket-compilation Editor PID89248. Hub, Fig and the separate blank Editor remained untouched. These were validation failures, not licensing blockers.
- ARIA05/06 failed on prior hash `0c3c7db0...`: separated tanks fought sequentially and a surviving foe outlasted ARIA's wait. Persian manual05 won that old formation with two losses. The support-line placement was revised; stats, deadline and ARIA watchdog were preserved.
- Persian manual06 gameplay passed, but some PNGs captured partial typewriter reveal. The helper now saves separate `-ready` captures after a settled AdvanceReady phase. Partial captures remain historical.

## Regression scope and rollback

`retained-missions-baseline.json` verifies11 unchanged mission definitions and logical maps (22 files) against `aeffb64c1a7dda27f90dedc244d5aff5454f7cb1`. Their native acceptance is unchanged, not rerun or promoted. Catalog registration is additive. Shared Watch observation targets the visible Hold button only during defense preparation. Shared tutorial mapping uses Hold for DefensiveAlert in the existing Steel/AirCorridor/Split branch; AirCorridor/Split have no matching current guidance. The new scenery socket is gated by Steel mission ID, logical map ID and exact prepared-source hash. No broader build/refund or device claim is made.

Original rollback map remains catalogued: `opmap.ch04.steel_push_01`, hash `6be39a43dfb6e837d3c9eb0290ef79b9818f646e5bdb97b0222b08904baed291`, source `opmap.skirmish.desert_base_01`, source hash `2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c`. Revert mission/scenario bindings and builder together. Existing saved-map identity/version safeguards remain; old coordinates are not accepted as new-candidate readiness.

## Reproduction / review

Keep Hub signed in. Use only `Tools/CI/invoke_unity_macos.sh`, explicit logs/timeouts, GUI licensing, no macOS batchmode. Entrypoints:

- `Game.Editor.CH04M02SteelPushConfigBuilder.PrepareRefineryReview`
- `Game.Editor.CH04M02SteelPushInputProbe.RunRefineryEnglishReplay`
- `Game.Editor.CH04M02SteelPushInputProbe.RunPersianManual`
- `Game.Editor.CH04M02SteelPushInputProbe.RunRetreatLossEnglish`
- `Game.Editor.CH04M02SteelPushInputProbe.RunShortageFixtureEnglish` — injected fuel fault only.
- `Game.Editor.CH04M02SteelPushInputProbe.RunSavedReplayEnglish` — copies a retained real completed profile, grants only300 replay Credits.
- `Game.Editor.CH04M02SteelPushInputProbe.OpenEnglishReview` — stops automation at a fresh interactive briefing, restores ordinary input and leaves only this task Editor open for human review. This is not a validation-pass claim.
