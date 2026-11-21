# Base Assault ARIA stops, 2026-11-21

The seven original stops used seed `130365`, locale `en`. The logs are under `/Volumes/T7/Projects/WarlineCapture-program-logs`. `Smoke` is `codexlab/author-ba-fix-cc-mp/aria/unity.log` and `Lane 50` is `2026-11-21/lane50/a/unity.log`.

All seven stopped at planner reason `101`. Expanded Base Assault uses `AriaGroundProgressPolicy` to stop after 180 seconds without a change in base health or infantry, a new economy milestone, or an improved public assault approach. The final intent and last control describe the frame when this watchdog fired. The preceding trace shows which action repeated or stalled.

| Stop | Cause and evidence | Disposition |
| --- | --- | --- |
| S026, Lane 50:327855 | The roster alternated between pages 0 and 1 earlier in the run. After the structure-capable force reached zero, the final loop alternated the closed drawer's Build button and the open drawer's Close button without a pending recruit. Lane 50:327871 shows the same depleted state with the drawer open. The candidate input trace shows the earlier page changes at lines 150–159 and the drawer loop at lines 270–286. | Corrected the optimistic production quote used to open the drawer. The earlier page cycling remains open. |
| S050, Lane 50:468330 | A base alert interrupted at Lane 50:468068–468085. Afterward, the trace shows the same Build/Close loop with zero infantry and zero structure attackers. Candidate input lines 226–242 show the loop with the tray on page 1. The last control is CloseDrawer at Lane 50:468346. | Same production quote fix. |
| S051, Smoke:528279 | The generic building attempt had already released the helipad step (Smoke:493553–493598). The late trace shows the same Build/Close loop with nine infantry, zero structure attackers, 100 materials and no pending rifle or rocketeer recruitment. See archived input lines 266–278. | Same production quote fix. |
| S027, Smoke:108321 | The generic helipad attempt released at Smoke:25140. A later standalone attempt opened the preview, offered no legal site, canceled and remained in pad stage 8 with the preview closed. See archived input lines 129–137. | Open. `StepHomePadPlacement` waits in stage 8 for a player or ARIA restart. |
| S028, Smoke:209365 | The starting pad was placed at Smoke:109763 and later stopped being presented. The replacement's standalone preview and cancellation followed the same pattern as S027 (archived input lines 82–95). Neither mission's trace establishes that a candidate site could legally be confirmed. | Open. Same standalone pad wait. |
| S006, Lane 50:241266 | A persistent home alert restarted defense rounds. Lane 50:235888, :239259 and :241231 record order, give-up and restart while base HP stayed 800. The final stop was in defender selection with Attack still armed (Lane 50:241282). `AriaHomeDefensePolicy` gives up a round, waits, and starts another while a home threat remains presented. | Open. |
| S053, Smoke:625307 | Repeated `find_base_given_up` events had no offered base or advance ground, and the roster showed no accepted attack orders (Smoke:624670–625284). `StepFindBaseRecovery` reports Inspect when it offers Attack to disarm. `inspectStage=0` and a closed drawer at Smoke:625323 place this in FindBase recovery. | Open. W41a owns the S002 FindBase work. W41b owns stale group selection. |

The Smoke input files live under `aria/harness-evidence/Design/AgentReports/SkirmishExpansion/<id>/_Evidence/`. Lane 50 input files are candidate artifacts named `s<nnn>-aria-rs-130365-en-1.jsonl.input.jsonl` under the candidate hash `af5a43a406aa8a14e36fc7350022a29f27d586b978fdacbc3e369037a4b55aef`.

The shared drawer loop occurs because the closed Build button stands in for each recruit card. The planner can select it using `SkirmishStrategyScoring.TryAfford`, which assumes a living Barracks and fixed capacity. Once the drawer opens, `StepGenericStructureReplacement` and the generic drawer branch require a currently offered control and can fall through to Close. The original input traces do not carry each attempted role's native rejection reason. For Base Assault with zero living structure-capable attackers, the patch quotes rifle, breacher and rocketeer through `SkirmishProductionService.EvaluateQueue`. The existing native quote uses the actual living producer, stock and capacity. The EditMode test removes the Barracks from a depleted S003 fixture and exercises the planner with the real public projection on both sides of that removal.

## Lab validation

- `codexlab/ba-aria-blocks/recruit-editmode.log:1872` records one passing EditMode test for the production quote change.
- `codexlab/ba-aria-blocks/editmode.log:1623` records one passing EditMode test of an exploratory pad-stage release. That experiment was reverted after the three-mission recruitment loop was identified.
- The uncounted `S031:130366:en` warm-up ended in Victory. `codexlab/ba-aria-blocks/warmup/unity.log` records the touch-menu pass at line 1223, Victory at 11346, return to MAIN MENU at 11603, and queue completion with `sourceUnchanged=1` at 11806.

All five accepted missions recorded Victory with zero ARIA blocks at `130365:en`. The completion marker is `codexlab/ba-aria-blocks/regression/unity.log:242584` (`recorded=5 wins=5 sourceUnchanged=1 exitCode=0`). The following line numbers are from that log.

| Mission | Touch menu passed | Victory recorded | Frozen result and MAIN MENU passed | Role coverage evidence |
| --- | ---: | ---: | ---: | ---: |
| S002 | 1295 | 242509 | 193583 | 193309 |
| S031 | 194433 | 242524 | 207504 | 207214 |
| S037 | 208301 | 242539 | 220404 | 220133 |
| S020 | 221201 | 242554 | 228276 | 227951 |
| S009 | 229076 | 242569 | 242321 | 242049 |

The repository's gate 3 parser passed all five role coverage checks. Its report is `codexlab/ba-aria-blocks/regression/gate3.txt`.

All seven affected missions ran once at `130365:en`. The completion marker is `codexlab/ba-aria-blocks/affected/unity.log:764785` (`recorded=7 wins=2 sourceUnchanged=1 exitCode=0`). The following line numbers are from that log.

| Mission | Touch menu passed | Queue outcome | First failure or successful return |
| --- | ---: | --- | --- |
| S027 | 1354 | Defeat, 764680 | Player base fell, 101973. MAIN MENU passed at 102232. The pad stop did not recur. |
| S028 | 103049 | Victory, 764695 | MAIN MENU passed at 241267. The pad stop did not recur. |
| S051 | 242115 | Abort, 764710 | Reason 101 at 306276. The final intent was Recruit after repeated FindBase give-ups (302779–305126). The input trace still showed two structure-capable units at the stop. |
| S053 | 307024 | Abort, 764725 | Reason 101 at 352225. The FindBase give-ups recurred (311637–352145), ending on ObserveBattle. The first historical failure is Smoke:625307. |
| S006 | 352981 | Victory, 764740 | MAIN MENU passed at 421168. The home-defense stop did not recur. |
| S026 | 421996 | Abort, 764755 | Reason 101 at 586838. Home defense cycled through focus, recenter and give-up (586299–586815), with four structure-capable units still alive. |
| S050 | 587620 | Abort, 764770 | Reason 101 at 764466. The depleted force recovered once, then entered the original Build/Close loop again. The first historical failure is Lane 50:468330. |

The sampled input traces for S026 and S051 never reached zero structure-capable units on the rerun. The S050 input trace at lines 126–131 shows the depleted force spending 100 materials and fielding a new structure-capable unit. Lines 377–407 show the Build/Close loop after a later depletion, with 320 materials and no pending rifle or rocketeer. The live quote fixes a reproducible missing-producer case in EditMode. The S050 recurrence shows that native drawer admission can still diverge from that quote. The trace does not identify the requested card or its native refusal. Both first historical failures and rerun failures are retained for the repeated S050 and S053 stops.

The lab input traces and other run artifacts are archived under `codexlab/ba-aria-blocks/harness-evidence/`.
