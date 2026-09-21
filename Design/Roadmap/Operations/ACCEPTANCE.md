# Operations acceptance and required ARIA wins

**User requirement, 2026-09-21: ARIA must be able to play and win.** This is a mandatory product acceptance criterion for **every Operations mission**, plus the complete persistent city run. All gates currently Pending; this planning task ran no game or ARIA sessions.

## Non-negotiable mission gate

An O001–O060 entry is Accepted only when ARIA starts from that mission's normal player-facing briefing, deploys through the normal controls, performs its required objectives, achieves a legitimate **Victory**, receives correct district/reward settlement once, and returns to the Operations dashboard. A recorded human win, direct command injection, green objective unit test, or result-screen screenshot alone cannot replace this evidence. Partial/Defeat/Withdrawn do not count as a win.

ARIA uses the shipping public observation and visible-input pipeline described in the [ARIA roadmap](../Aria_Demonstration/PLAN.md). It may choose groups/routes, recruit only if that scenario supports recruitment, issue supported interactions, pan/focus, board/unload, and use ordinary pause according to the same player rules. It may not mutate ECS facts, inject orders behind the input gateway, read hidden enemy positions, reveal the whole map, spawn units, change health/resources/timers, bypass gates or call settlement directly. It must not rely on a mission-ID-specific solution script. Human tactical correction or a mid-attempt restart invalidates that attempt as an unassisted win.

Isolated test fixtures may initialize a valid **pre-mission** city state/offer to reach a late mission without replaying the whole mode. Disclose that setup, preserve normal runtime deployment, and require a separate full-run test to prove real progression. The initial fixture is not permitted to set completed tactical objective facts or extra forces/resources. ARIA setup shortcuts do not count as a city-run win.

## ARIA coverage matrix

| Level | Required evidence |
|---|---|
| Every mission, Regular | Five predeclared attempts using seeds `1101+n`, `2101+n`, `3101+n`, `4101+n`, `5101+n`, where n is 1–60. At least **4/5 legitimate unassisted wins** and at least one canonical-seed win. Retain all failed attempts and reasons. Each attempt is a fresh normal start; no retry cherry-picking within the matrix. |
| Every exposed difficulty | At least one full unassisted win per mission on each additional exposed difficulty, using seed `6101+n` Recruit, `7101+n` Veteran, `8101+n` Commander. Do not expose a difficulty that cannot pass. |
| English and Farsi | At least one unassisted visible-control win in each language for every mission. English Regular canonical evidence may serve the EN row; use seed `9101+n` for a dedicated FA pass unless another required run was explicitly conducted in FA. Verify localized prompts, RTL layout, timer/counter legibility and target selection. |
| Family generalization | In each of the 12 families, vary approach route, legal force losses and launch district modifiers on at least two missions where available. No hidden objective/wave input. AIRLIFT has three entries; cover all three. |
| Interruption | ARIA safely yields to manual input and resumes/replans through documented Watch controls. One suspend/restore win per family; every mission still needs its own checkpoint correctness fixture. |
| Full city run | From a genuine new profile/run, ARIA chooses visible offers/actions, wins required missions, manages AP/Intel/recovery, ends days, wins six local finales and reaches two consecutive stable days. At least one complete Regular run in EN and one in FA on different declared city seeds, using Operations Run Watch opt-in. No prefilled milestones or city metrics. |

The basic four-difficulty catalog matrix requires 300 Regular attempts plus 180 additional-difficulty wins; EN/FA can overlap applicable runs. Dedicated FA runs can add 60. This is deliberate acceptance work, not content count. Schedule automated observation and human review of representative trajectories; retain traces and review every failure. A pass rate below the threshold is a readiness defect to diagnose in strategy, affordances, movement, UI, or mission balance. Never compensate by secretly increasing ARIA power or auto-completing an objective.

Every trace records code/config/content hashes, mission/district/offer/run IDs, deterministic seed, difficulty, language, force and enemy budgets, starting city modifiers, platform, input source, restart count, objective completion ticks, terminal reason, casualty/cargo/evidence facts, before/after district values, transaction IDs, rewards and settled revision. Record an uninterrupted screen capture or equivalent chronological screenshots plus input trace; a result-only image is insufficient.

## Per-mission acceptance rows

| Area | Required behavior |
|---|---|
| Content distinction | Brief's tactical decision is visible and useful; two viable approaches change exposure/timing/protection, not merely visual skin. Mission has a meaningful difference from others in its family. |
| Manual play | A normal-input full win with required interactions and honest failure feedback; human review can explain objective, risk and district consequence. ARIA evidence complements this, not replaces it. |
| Objective truth | Every graph node has a real world predicate and bound role/anchor. Optional nodes cannot grant Victory. All mandatory conditions, survivor/cargo floors and extraction state are checked on the terminal tick. |
| Outcome coverage | Test Victory, the specified Partial where reachable, Defeat, Withdraw and TechnicalFailure separately. Same-tick loss beats success; a terminal result is immutable. |
| State consequences | Full/half/failure deltas, caps, actual harm, final site/route facts and Success milestones match STRATEGIC_RULES. No Campaign stars or tactical-wallet export. |
| Cost/reward accounting | Deploy charges AP once; technical failure refunds once; retry after Defeat is a new paid-AP attempt; practice grants nothing. Repeated callback/result/report UI grants no duplicate Credits/XP. |
| Recovery | Checkpoint round trip during every critical phase, lost carrier/repair/wave/zone state, result-before-save and save-before-return crash windows. No invalid entity references, timer reset exploit or duplicate cargo. |
| Input/UI | Actual pointer/touch hit tests, camera focus, select/order/interaction/progress/cancel, pause/withdraw and back navigation. No invisible required action or disabled control without a reason. |
| ARIA | Mandatory win matrix above, legitimate visible input, preserved player interruption, generic role/route/objective reasoning. |
| Enemy behavior | Finite roster budget, truthful warnings, valid approach spawns, legal fog observations, appropriate counter access; no unseen teleport reinforcements. |
| Localization/narrative | EN/FA keys cover all new labels, objectives, warnings, outcomes and refusal reasons. Supplemental district fiction does not contradict Campaign revelations. |
| Map | Infantry/cargo/largest vehicle reach required roles; height/surface/bridge/landing/obstacle state agrees with visuals; camera/minimap usable on supported phone sizes. |
| Device | Exact candidate build, supported hardware tier and quality preset; no blockers in full mission or repeated transitions; performance and memory evidence against current project targets. |

## Shared automated suites to implement

These are **proposed new validation entry points**, not commands available today. Implement the suite before using its example command. Each reports a full pass marker only if every selected assertion actually runs and passes.

| Suite / planned type | Essential cases |
|---|---|
| `OperationsCatalogValidation` | 60 unique missions/scenarios; six sets of ten; 12 families; graph DAG; IDs <=60 bytes; typed role/route binding; budgets, feature gates, EN/FA keys; no dangling prerequisite; all finale milestones reachable |
| `OperationsStrategicValidation` | Fixed-seed deterministic offers/day ticks; simultaneous adjacency; incident cap/cooldown/expiry; clamp/report deltas; AP limits; recovery at all metric floors; finite path to city win without purchases |
| `OperationsObjectiveValidation` | Every rule, timer pause/contest, preactivation object loss, cargo/people identity, escaped target, dropped evidence, protected-site precedence, graph activation/terminal latching |
| `OperationsPersistenceValidation` | Old profile migration; unknown version; stale writer; duplicate/conflicting receipt; interrupted replace; checkpoint reference publish; active/Reserved/terminal recovery; Campaign and quickgame data preserved |
| `OperationsMissionValidation` | Data-driven cases for each catalog entry and its unique negative fixture, actual supported runtime components, exact success/partial/fail predicates |
| `OperationsUiValidation` | Real button/input path and visible world result; correct dashboard aliases; confirmation cancel; End Day request vs report navigation; EN/FA layout and no double binding |
| `OperationsAriaValidation` | Observe-only public state; legal visible action traces; every entry/difficulty/language win evidence; seed robustness; intervention detection; full city-run threshold proof |
| Existing architecture/performance suites | Assembly direction, ECS ownership/naming/Burst classification, config readiness, changed hot-path allocation and affected shared Campaign/Skirmish regressions |

Tests may use component fixtures to prove edge conditions, but clearly separate those from normal manual/ARIA acceptance. A direct call to set a fact can test an outcome reducer; it cannot certify ARIA gameplay.

## Unity execution and evidence

Follow the current root AGENTS.md exactly. Keep Hub open/signed in. On macOS every executeMethod/test/build/capture uses `rtk proxy Tools/CI/invoke_unity_macos.sh`; no direct Unity executable, no `-batchmode`, no IPC reset or process termination. On Windows use the checked PowerShell wrappers with log/timeout/pass marker. Do not use CLI build/test as a substitute for those wrappers. CLI-connected authoring follows the Unity skill/repository rules.

Example **after implementing** `Game.Tests.Editor.OperationsCatalogValidation.RunFocusedValidation`:

```bash
rtk proxy Tools/CI/invoke_unity_macos.sh --timeout 600 --log /private/tmp/operations-catalog.log -- \
  -quit -executeMethod Game.Tests.Editor.OperationsCatalogValidation.RunFocusedValidation
rtk proxy rg -F '[OperationsCatalogValidation] result=Passed' /private/tmp/operations-catalog.log
```

Check wrapper exit status, actual suite result, full retained log, and exact pass marker. Timeout, project lock, nonzero exit or missing marker fails validation. A grep line by itself does not establish success. Do not run a quit-style validation against a user's active project session without resolving project ownership through the permitted wrapper/isolated setup. Never kill an active Editor to make room. These commands are documentation; none was executed by the planning task.

## Final mode decision

### Demo 2 environment evidence

For every adapted kit used by a published mission, close [D2-V1–V6](../../Demo2_Asset_Integration_Guide.md#6-validate-and-record-acceptance) and attach the source/output manifest revision to its content evidence. D03 must retain separate freight/service routes, truck turns and repair-site access. D04 must prove two traversable land crossings, correct deck/ramp/water surfaces, independent alternatives and normal convoy/evacuation travel; neither a visible boat nor a broken bridge grants an unsupported mechanic. Interactive utility props must follow real Repair/Scan/Interact/protection facts and checkpoint restoration. Recheck manual/ARIA and persistent settlement behavior after relevant art/geometry changes. Art acceptance never substitutes for mission acceptance.

Full-mode acceptance requires 60 complete mission rows, six reviewed district maps, all 12 family implementations, the free recovery path, versioned interrupted-session support, EN/FA manual and ARIA evidence, full city-run wins, account-economy integrity, supported-device performance and affected Campaign/Skirmish regression protection. A partial development slice must state its actual playable count and pending gates. **ARIA being unable to play and win any released mission keeps that mission out of Accepted.**
