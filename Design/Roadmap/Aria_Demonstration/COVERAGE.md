# Game-mode, match and skill coverage

Updated: 2026-09-20. **M1–M5 guided baseline and both Skirmish Base Assault scenarios have EN/FA Editor wins. Broader release certification remains open.** See [implementation evidence](../../AgentReports/AriaWatchPlay/implementation-status.md). [PLAN.md](PLAN.md) defines the complete release; [DELIVERY.md](DELIVERY.md) owns implementation.

## Planned expanded Skirmish catalog

The [Skirmish expansion catalog](../Skirmish_Expansion/BATTLE_CATALOG.md) now targets 120 selectable battles on five maps, with a path to 200. Its [CSV](../Skirmish_Expansion/SCENARIO_CATALOG.csv) lists Planned entries; none inherit current S01/S02 acceptance. [Expanded AI/ARIA rules](../Skirmish_Expansion/AI_AND_ARIA.md) require reusable goal/route/role reasoning, matching skill capabilities, multiple seeds and EN/FA evidence per scenario. Existing S01/S02 labels below identify only the current small snapshots. Operations design is deferred to the owner's next separate planning task.

## Source inventory

Reviewed at `c69865400`. Campaign catalog: `Assets/Game/Configs/Campaign/CampaignMissionCatalog.asset`. Definitions: `Assets/Game/Configs/Missions/Chapter01`. Current Skirmish launch identity comes from `Assets/Game/Scripts/Composition/SkirmishLaunchProjection.cs`; preset from `Assets/Game/Resources/SkirmishBaseAssault.asset`.

| Coverage ID | Current match identity | Required demonstration | Status |
|---|---|---|---|
| C01 | `saga.ch01.m01.first_contact` | Understand corridor objective; select via the real group controls; move, attack and protect the command squad; complete result/story handoff | Guided EN/FA baseline passed; recovery/device gates open |
| C02 | `saga.ch01.m02.establish_base` | Follow current base tutorial, place correctly, recruit/deliver soldiers, respond to patrol and finish without reopening completed Build steps | Guided EN/FA baseline passed; recovery/device gates open |
| C03 | `saga.ch01.m03.radar_warning` | Place a gate on the carriageway matching preview; use optional construction deliberately; deploy/Hold defenders; understand Scan and convoy warnings; explain waits and handle both threats | Guided EN/FA baseline passed; recovery/device gates open |
| C04 | `saga.ch01.m04.airlift` | Select the four specialists despite overlapping vehicles; board/unload ground transport; land and board helicopter; defend landing zone and depart; verify passenger counts and timers | Guided EN/FA baseline passed; recovery/device gates open |
| C05 | `saga.ch01.m05.breach_assault` | Select a capable force, attack the visible gate between walls, enter the compound, neutralize radar/guards, secure the archive for the visible duration | Guided EN/FA baseline passed; recovery/device gates open |
| S01 | `skirmish.base_assault`; scenario `scenario.skirmish.desert_base_standard`; map `opmap.skirmish.desert_base_01` | Defend own base, manage economy and recruitment/delivery, select/build legally, attack enemy base, handle victory/defeat/draw | EditorVerified default EN/FA wins (5:38 / 5:20); development preview, wider release gates open |
| S02 | `skirmish.base_assault`; scenario `scenario.skirmish.city_crossroads`; map `opmap.skirmish.city_crossroads` | Select second battlefield, recruit and defend, navigate the city approach, destroy enemy base, cancel input on result | EditorVerified default EN/FA wins (6:17 / 6:22); development preview, wider release gates open |
| O00 | Operations dashboard/district tactical routes | A0 must identify which public actions actually launch a tactical match and enumerate its ruleset/map/objectives; split into one row per playable type | Discovery required |

Campaign rows describe intended capabilities against the current mission goals; A0 must verify the exact current instruction sequence and prerequisites in a live run. They do not authorize adding obsolete Stop/Radar buttons or optional mandatory steps.

Operations source reviewed: `OperationsDashboardScreenView.cs`, `DistrictDetailActionsScreenView.cs`, `ConfirmRaidV3PopupView.cs` under `Assets/Game/Scripts/UI/Screens`. District action kinds are Patrol, DroneScan, Aid, Raid and Repair. The view publishes an action event and marks a control queued; this is not proof of a tactical simulation or completed result flow. The source search did not establish a complete tactical launch receiver. **Do not rename an S01 run an Operations pass.** A0 traces actual bindings/routes, records the finding and assigns missing gameplay to a named dependency. Every route that does launch a match must be supported in A5.

Dashboard transactions that never enter a tactical match are outside current-match autonomy. They remain player-controlled. Do not invent a new Operations mode just to check O00 off.

## Inventory completion rule

A0 compares the catalog inventory with reachable public entry points, replay/result routes, saved setup options and scenario overrides. One record per match/ruleset includes:

`CoverageId | public route | mode | mission/ruleset ID + version | map ID | supported setup variants | objective semantics | required skills | public controls | normal terminal outcomes | content readiness | ARIA readiness | missing dependency owner | evidence`

Use explicit states: `Discovered`, `ContentBlocked`, `Planned`, `Implemented`, `EditorVerified`, `DeviceVerified`, `PlayerReviewed`. Do not imply a whole mode passed when one entry passed. A non-match screen gets an explicit `NonMatch` disposition and a source/route reason. An unknown or playable-but-uncovered row prevents an all-current-matches completion claim.

Unexposed, unfinished content has an integration dependency and remains unavailable. If the owner wants all Operations tactical gameplay completed as part of a future implementation scope, that gameplay package is tracked separately and must finish before its ARIA verification; automation cannot supply missing objectives or win rules.

## Reusable skill catalog

Every skill uses only visible information and gestures; the game's normal handlers create orders. Each row needs success, rejected-action and cancellation cases.

| Skill ID | Real interaction | Evidence of completion | Used by |
|---|---|---|---|
| K01 ReadGoal | Inspect shown instruction/rules; open public help when needed | Correct public goal and next achievable subgoal identified | All |
| K02 NavigateCamera | Drag/pinch, minimap or existing focus control | Requested area visible; no camera-lock workaround | All |
| K03 SelectGroup | Tap appropriate squad card/group selector | Visible selected membership matches intended group | All |
| K04 SelectSubset | Hold-drag rectangle or supported additive selection | Correct members selected despite overlapping transports | M4; mixed battles |
| K05 Move | Select Move, then visible valid destination | Acceptance feedback, movement and arrival/progress observed | All |
| K06 HoldDefend | Select Hold when tactically required | Units show Hold; defend and visibly track threat/wait | M3–M5; free battles |
| K07 Attack | Select capable group, Attack, visible target | Accepted order then damage/destruction/progress | All combat |
| K08 ScanInspect | Use current Scan; inspect results/contacts | Actual scan feedback, visible cooldown/reveal and useful interpretation | M3; enabled rulesets |
| K09 OpenInspectBuild | Open Build/production, select tab/card, read requirements | Correct current panel and intended item visible | M2/M3; free battles |
| K10 PlaceBuild | Drag preview, rotate, confirm only visibly valid site | Final structure matches preview; resources/progress update | M2/M3; free battles |
| K11 Recruit | Select producer/unit/quantity and confirm | Queue/cost accepted, delivery finishes, requested squad selectable | M2/M3; Skirmish; enabled Operations |
| K12 BoardTransport | Select compatible passengers, Board, visible carrier | Expected passenger count, no helicopter selected as passenger | M4; enabled rulesets |
| K13 DeliverUnload | Move/land through current commands, unload at valid location | Correct passengers disembarked, selectable and alive | M4; enabled rulesets |
| K14 Economy | Inspect visible shortage; wait for supply or use working exchange UI | Real balance/queue change without duplicate spending | Skirmish; enabled Operations |
| K15 WaitVerify | Observe timer, production, arrival, security or known transition | Condition progresses/completes or timeout explains failure | All |
| K16 Acknowledge | Press currently required Continue/close/back within gameplay | Visible step/panel advances once; no stale click after result | Campaign; gameplay panels |
| K17 ReplanCombat | Reassess observed threats/losses, regroup/retreat/reinforce using K03–K14 | Tactical response and visible objective progress | Skirmish; Operations; campaign recovery |
| K18 UpgradeExpand | Use future existing research/facility UI through touches | Affordability/prerequisite, timer and effect visibly verified | Planned Skirmish expansion only |
| K19 ContestZone | Move eligible forces, defend/rotate using public zone/ticket UI | Capture/contest/ticket changes verified | Future Frontline / applicable Operations |
| K20 AirSupport | Use supported aircraft/support UI; observe fuel/landing/targets | Valid delivery/strike/return with feedback | Future roster / applicable Operations |

K18–K20 are extension contracts, not claims that those features currently exist. Do not add them to a current match merely to demonstrate ARIA. Destructive friendly actions and surrender are not automatic recovery skills; hand control back rather than liquidating the player's army or conceding without their choice.

## Start-state and lifecycle coverage

For every current match, test fresh start and a legal mid-match handover. Campaign also tests replay with existing progress and tutorial variants where supported. Include partial selection, open Build panel, moving units, active recruitment, low resources, visible threats and already completed subgoals. The planner must reconstruct state from observations rather than assuming the tutorial begins at step one.

Every adapter maps public success/failure/time-limit conditions, visible mandatory survival constraints and permitted commands. Known script-controlled camera transitions are waiting states. Pause/background/result invalidates contact ownership. Resume is manual; a new Start reevaluates the present match, never replays old actions.

## Future content contract

New playable content supplies localized rules/instructions and a public goal schema, required skills, touch-accessible controls, visible acceptance/failure feedback, waits/progress, termination semantics and a coverage fixture. Content/skill versions invalidate affected certification. A developer-facing validator fails a release candidate when a publicly playable entry lacks a manifest or its required skills are uncertified.

Existing skills should handle new layout/map/roster variations without coordinate scripts. New mechanics need an explicit skill plus tests. The expanded Skirmish [delivery packages](../Skirmish_Expansion/DELIVERY.md) add group/upgrade skills in E1–E3, recon/air in E4, zone objectives in E5 and advanced air/team mechanics in E6/E7. This keeps support current as those modes grow.
