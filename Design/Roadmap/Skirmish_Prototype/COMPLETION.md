# Base Assault — internal play readiness

Decision: **ready for internal Editor play in English and Farsi**, 2026-09-18. One authored map, one AI opponent and the complete setup → battle → result → replay/setup/menu loop are implemented. No known progression blocker remains in the exercised routes. This is a functional prototype decision, not Android release approval or proof of player enjoyment.

The tested working tree is based on `b0b4dc72f2b41a36c943955b6a8709aee86227e0`, branch `codex/m03-radar-warning`, with uncommitted changes. [Input hashes](../../AgentReports/SkirmishPrototype/Readiness/tested-inputs.sha256) identify the tested source/assets; HEAD alone does not. Unity 6000.5.2f1, macOS Editor, isolated QA saves. Setup was inspected at 1280×720 and 2400×1080; final gameplay/guide captures use 1920×1080. No Android build or touch-device claim is made.

## What is ready

- Setup exposes the supported Base Assault rules and AI seed, persists valid settings, and launches one isolated session. Invalid/startup-failure paths provide recovery instead of falling into campaign.
- Both designated Main Bases, automatic supply chains and the limited starting forces spawn at their authored positions. All ten starting buildings pass road, sidewalk and water exclusion checks after regeneration.
- Tap squad cards, Move/Hold/Attack, paid recruitment, construction, cancellation/refunds, logistics, AI reinforcement/attacks and real base destruction work together. Successful recruitment closes Build. Active infantry moves survive passing-vehicle displacement; new soldiers avoid disconnected terrain pockets.
- A visible 15-minute limit and both base-health readouts explain progress. ARIA provides a stable concise briefing and a working four-page localized Field guide. The guide pauses the battle and resumes it when closed.
- Victory, defeat, simultaneous-destruction draw, timeout draw and surrender use explicit reasons. Finished matches stop combat, economy, clock and gameplay voices; Replay starts a fresh world at normal speed.
- Campaign ownership and player progression remain separate from skirmish. No persistent rewards are awarded by this prototype.

## Verification

**309 checks passed after asset regeneration:** [303 focused/shared checks](../../AgentReports/SkirmishPrototype/Readiness/skirmish-final-regressions.txt) and [six setup/binding/navigation checks](../../AgentReports/SkirmishPrototype/Readiness/skirmish-regenerated-setup-tests.txt). Counts include parameterized cases. Tests were invoked in the connected Editor; they are not an Android or standalone test run.

Ten consecutive UI sessions covered replay, setup adjustment, pause/restart/surrender and return to menu without accumulating session entities or stuck pause ownership. Startup failure/retry and cancellation/refunds were exercised separately. All ten EN/FA campaign entries passed after skirmish; only M1's full opening story was replayed. M2–M5 comics were skipped through their UI and their first actionable guidance checked. Shared sensitive-system regressions passed; this was not ten fresh complete campaign playthroughs.

The final tactical matrix used normal-speed player controls and screen-position world commands. Health, positions, resources and outcome facts were not changed to manufacture results.

| Approach | Locale / AI seed | Observed result | Time |
|---|---|---|---|
| Unsupported starting attack | EN / 7919 | Enemy destroyed player Main Base | 2:55.632 |
| Unsupported starting attack | FA / 37 | Enemy destroyed player Main Base | 2:45.519 |
| Two towers, recruitment, late frontal counterattack | EN / 104729 | Draw; both bases survived | 15:00 |
| Defense, supply rebuild, frontal counterattack | FA / 37 | Surrender after army loss | 8:30.423 |
| Gathered infantry/car flank; tower then base | EN / 104729 | Victory | 6:02.973 |
| Gathered infantry/car flank; tower then base | FA / 7919 | Victory | 5:59.043 |

The EN flank preceded the terminal-freeze correction; its combat tuning/layout were unchanged. The subsequent FA victory verified twelve seconds of identical survivor health/positions, economy and clock after the result, with only music playing. Replay released the freeze correctly. Both flank runs brought all 25 combat units to both rally points without reissuing orders.

Supply recovery used the actual Destroy command and paid replacement construction. Trucks delivered 8 Oil to the new depot; a completed cycle consumed 4 Oil and produced 20 Materials (20 → 40 balance, lifetime fabrication 80 → 100). The enemy economy also continued fabricating/recruiting. [Supply evidence](../../AgentReports/SkirmishPrototype/Readiness/skirmish-fa-supply-recovery.txt).

## Acceptance case coverage

“Editor verified” means the recorded functional check passed in the Editor. Qualified rows explicitly limit evidence; they do not silently close the broader requirement. Detailed traces and screenshots are in the [readiness report](../../AgentReports/SkirmishPrototype/Readiness/README.md).

| Cases | Decision / evidence |
|---|---|
| SK-A01 | Editor verified: bilingual setup captures at both requested sizes; regenerated Farsi setup inspected again. |
| SK-A02 | Qualified: configuration round-trip, navigation, replay and Play-session reload passed. Full application restart remains a lifecycle acceptance check. |
| SK-A03 | Mechanics verified: migration/validation coverage and usable localized startup error. No destructive save reset. |
| SK-A04 | Editor/mechanics verified: duplicate-request protection and settled startup readbacks; repeat UI sessions retain one session. |
| SK-A05 | Editor/mechanics verified: injected startup failure → real Retry → fresh Playing session; restart/cancel cleanup. Injection was a recovery test, not a played outcome. |
| SK-A06 | Editor verified: zoom 40, normal opening, exact ten-building placement on dry ground; fresh post-regeneration Deploy passed. |
| SK-A07 | Editor verified: stable group-card selections during recruitment/battle; 24 infantry distributed across four groups, separate starting car. |
| SK-A08 | Editor/mechanics verified: selected groups move/hold/attack, fight and destroy targets; accepted/rejected command feedback tests passed. |
| SK-A09 | Qualified: actual paid tower/depot placement plus preview/origin/rotation, invalid/cancel and pointer-ownership regressions. Not an exhaustive test of every map cell/camera pose. |
| SK-A10 | Editor verified: four-soldier batches arrive, drawer closes, later batches remain available, cancellation returns only undelivered cost once. |
| SK-A11 | Editor/mechanics verified: full 24-infantry army, queue/capacity and resource rejection/recovery checks and observed economy ledger. |
| SK-A12 | Qualified: live supply removal/rebuild/delivery/fabrication passed. Blocked-route and invalid/destroyed destination cleanup use focused mechanics tests, not a live roadblock experiment. |
| SK-A13 | Editor verified: spending, recruitment, repeated attacks, tower targeting and successful flank assault observed. |
| SK-A14 | Editor/mechanics verified: tower/base combat deaths, designated-base victory, dead-target and authored-destroyed-child regressions. |
| SK-A15 | Editor verified: real enemy Main Base destruction yields defeat, including while Build is open; surviving entities do not replace the objective. |
| SK-A16 | Mechanics verified: terminal outcome/deadline and simultaneous-destruction policy. No health injection was used as gameplay evidence. |
| SK-A17 | Editor verified: normal-speed 15-minute draw plus authoritative rule tests. |
| SK-A18 | Editor verified: paused clock, cancel/confirm restart, fresh state and confirmed surrender; guide pause also checked. |
| SK-A19 | Editor verified: EN/FA outcomes and actual Replay/Adjust Setup/Main Menu routes; fresh session and released terminal freeze. |
| SK-A20 | Qualified: live language switching, bilingual guide/results, voice locale/lifecycle regressions, and terminal audio inspection. This is not a listening audit of every possible skirmish utterance. |
| SK-A21 | Editor verified for prototype: no pretend tutorial/Show Me actions; objective, clock, resource state and four-page Field guide provide actual context. Guide open/next/close and bilingual text inspected. |
| SK-A22 | Editor verified: ten consecutive UI sessions, stable settled entity counts and no menu session/squad/pause leakage. Phone memory remains pending. |
| SK-A23 | Qualified: ten campaign entries/first guidance and affected shared-system tests passed. Full fresh M1–M5 end-to-end replay was not repeated in this skirmish pass. |
| SK-A24 | Editor/mechanics verified: results are separate from campaign rewards; isolated campaign profile unchanged across skirmish runs. Intentional earlier campaign-entry attempt changes are separately recorded. |
| SK-A25 | Qualified: interrupted fresh replay then Play-session reload preserved setup and prior result, with no active match/fake resume/new result. OS process termination and device suspend/resume remain pending. |
| SK-A26 | Editor verified: config/setup/localization builders completed; all eight gameplay configs byte-identical, six regenerated-setup tests and 303 regressions pass; fresh Deploy/layout passes. |

## Current balance

| Item | Implemented value |
|---|---|
| Initial economy | 220 Materials / 600 capacity; 0 Oil; 160 usable Fuel; 0 Credits |
| Starting force per faction | 8 rifle infantry, 1 armored car, 2 tray trucks, 1 fuel tanker |
| Main Base | Designated Barracks, 1200 HP; destruction ends match |
| Infantry | 125 HP; range 32; 6 damage every 0.8 s |
| Starting armored car | 500 HP; range 40; 18 damage every 1.2 s |
| Watchtower | 700 HP; range 55; 10 damage every 0.8 s |
| Player infantry capacity | 24, stable four-group distribution |
| AI | First attack eligibility at 60 s; 8-unit attack groups; production interval 15 s; target 16 produced living infantry plus initial defenders |
| Fabrication | 4 Oil → 20 Materials per 30-second cycle; supply day 120 seconds |
| AI supply allocation | 80-Materials reserve; 160-Fuel target; emergency fuel priority below 16 |
| Match limit | 15:00; both bases surviving yields draw |

## Remaining acceptance and observations

1. **Pacing is not signed off.** Actual range was 2:45–15:00; sample median about 6:01. Defense runs include QA inspection time. The original 8–12-minute target is not demonstrated. Unsupported rushes lose quickly; coordinated flanks win; defense alone does not grant victory. Evaluate this with unfamiliar players instead of inflating health to force a target duration.
2. **Android/device and unfamiliar-player acceptance remain pending.** Check touch/safe areas, frame-time tails, thermal/memory behavior, application termination/suspend/audio focus and comprehension without coaching. Editor automation cannot establish those results.
3. **Rebuilding on nearby ground passed; same-site rebuilding did not.** The demolished depot's original footprint still reported blocked. The cause was not established as wreck persistence versus another blocker, so no claim is made that the original site immediately becomes reusable. The affordable nearby replacement restored supply.
4. **Campaign coverage is bounded.** Retain the prior complete campaign baseline alongside this pass's ten entry checks and shared regressions; do not rename them a new full campaign playthrough.

The normal WarlineCapture Editor was returned to clean `Match.unity` in Edit mode and its temporary save-root override removed. No user save was replaced. Changes remain in the working tree; this report does not claim a new commit or push.
