# M3 / M4 readiness audit

> Current completion work and superseding results: [M3 / M4 Editor completion QA, 10 September](../M03_M04_Editor_Completion_2026-09-10.md). This older report retains its original checkpoint scope.

Starting implementation: `60252791d6f9c3bebb759e4fe2aef27547d5dc6d` on `codex/m03-radar-warning`. The ending fixes described below are part of this audit's delivery. Editor only, as requested. Review images remain under `/private/tmp` and are not committed.

**Both missions have playable implementations; full completion and an all-green architecture signoff are not established.** The prior M4 core acceptance is narrower than repository-wide architectural acceptance. This audit keeps those conclusions separate.

## What exists and has prior Editor evidence

| Area | M3 | M4 |
|---|---|---|
| Opening camera | Captured RTS start, simultaneous zoom/pan, key-area holds, exact RTS return; normal/wide/skip/reduced-motion journeys | RTS start, specialist pickup and landing-zone tour, RTS return in approximately 14 seconds |
| Mission gameplay | Eight rifles and radar defend a two-element convoy route; optional Ping, Tower, Barrier and reinforcements | Four specialists actually ride the APC, transfer to the helicopter, hold a clear LZ for 20 seconds and depart airborne |
| Guidance/reference | 12 ARIA lessons and guide topics, 57 class identities, English/Persian | 12 transport lessons and guide topics, 57 class identities, English/Persian |
| Ending | Outcome-aware finale, three debrief panels on victory, result, guide return, defeat without victory debrief, retry | Extraction debrief, visible victory/defeat result, campaign return, replay and clean retry |
| Rewards | Atomic first-clear/replay settlement and recovery from failed save; no reward on defeat | First clear: 500 XP, 2,500 Credits and three unit unlocks; replay: 300 Credits; duplicate requests do not duplicate grants |
| Art/captions | Approved M3 visual identity, seven panels and fourteen aspect crops; bilingual captions | Matching seven panels, fourteen aspect crops and Laila portrait; bilingual captions |

The M4 source snapshot still matches all 199 recorded source/asset hashes at the start of this audit. M3's older snapshot differs in dozens of shared paths after the M4 integration; its earlier full QA cannot be represented as a full retest of the final combined build. See the individual run records for exact scope.

## Checks in this audit

The new `MissionReadinessArchitectureValidation.Run` enumerates every ordinary `[Test]` in the nine production architecture fixtures. It continues after each failure and rejects unsupported lifecycle/parameterized test shapes. It does not regenerate assets, alter baselines or relax thresholds.

An initial run of the older 23-suite closeout surface plus later guards found additional failures, including a missing resource-header target, a build-placement test context exception and the FirstLaunch prefab builder expecting an absent `SafeArea/PlaybackControls/PauseButton/Label`. Two wrapper results were false failures caused by runners that throw on failure but do not set `ValidationExit`; that initial 13/15 aggregate is not accepted as a correct suite count. The individual architecture run supersedes it. The initial legacy run modified the narrative font asset; that one audit-generated change was restored to the exact committed asset before further checks.

**Individual architecture audit: FAILED, 106 passed / 31 failed across 137 tests in nine fixtures**, wrapper exit 0, zero C# compiler errors. Initial exact log: `/private/tmp/warline-missions-readiness-architecture-02.log`; the combined M4 readiness run repeated the same counts. A zero wrapper exit never overrides an explicit failed result marker. This is every test in the nine named architecture fixtures, not the entire Unity test inventory.

[Final run ledger and source hashes](readiness_runs.json) record the wrapper exits, exact result markers, log hashes, current source snapshot, and locally reviewed capture hashes. No production scene, prefab, config, font or artwork changed during this delivery. No guard, baseline, exception or allocation target was relaxed.

### Current live ending checks

- **Final M3 victory/Continue passed**, wrapper exit 0: `/private/tmp/warline-m03-readiness-victory-06.log`. Real combat won at 205,277 mission milliseconds. The corrected finale, all three debrief panels, bilingual results/guide return, actual visible Campaign destination, disposal of all 20 prior actors, durable first-clear receipt/M4 availability, and unchanged settled rewards passed. The Campaign capture was visually inspected. The probe uses an isolated save for the mission; the recreated menu binds the normal profile, so its displayed progression is not used as evidence of the temporary profile's unlocks. Those were checked by reopening the isolated save.
- **Final M4 regression/live journey passed**, wrapper exit 0: `/private/tmp/warline-m04-readiness-final-02.log`. All 14 regression suites passed. HUD/guidance checks covered English/Persian at 16:9 and 20:9, 12 topics and 57 classes. Actual APC transport, helicopter transfer and airborne rescue completed at 75,392 ms after 20,009 ms of uninterrupted LZ clearance. Debrief, visible bilingual results, the actual visible/interactive Campaign destination, durable unlocks, Replay carrier-loss defeat and clean Retry passed. The idle retry lost at 600,025 ms with no passenger/escort loss; rewards did not duplicate. The Persian Campaign return and victory captures were visually inspected. The same run's architecture result remained **Failed: 106/31**; its zero process exit reflects the independent live probe, not an all-green combined signoff.
- **M3 victory passed**, wrapper exit 0: `/private/tmp/warline-m03-readiness-victory-02.log`. Real rifle movement/attack and one Ping stopped all seven hostiles at 196,286 mission milliseconds; three stars, no civilian losses. The 3-second finale froze mission time, the three final debrief panels preceded the result, English/Persian results at 16:9 and 20:9 were visible and readable, and guide return restored the actionable result.
- The first current victory capture exposed a stale threat warning behind the finale camera-return control. Warning visibility/expiry now lives in the existing `MainMenuPlayUI` owner’s `ThreatWarning` partial and hides that strip during camera tours. The second live run asserted the strip was hidden; the corrected finale capture was visually inspected. No warning timing, combat facts or camera ownership was moved into a new runtime owner.
- The new committed-assets probe entry points skip prefab/config builders and write captures under `/private/tmp`, preserving the shipped assets and prior Design evidence. The matching M4 resource test entry point also skips its repair builder.

The extended Continue test then found that M3 routed to MainMenu while M4 routed to Campaign. `CampaignMissionHudResultBinder` now sends both defense and extraction mission results back to Campaign. The first extended run also exposed a diagnostic-only assumption that `EntityManager.Exists` implied all unit components were present; the probe now tolerates entities already in cleanup, while final acceptance still waits for every original actor to be disposed.

The next capture exposed a shared presentation defect: even with the logical Campaign route, `EnterMenu` installed the main-menu body. `UIShellView` now installs the requested Campaign body while loading is still opaque, before the transition reveals it. Both probes now require the actual active, visible, interactive `CampaignOperationsScreenView`; route state alone cannot pass. The earlier M3 victory-05 and M4 final-01 results establish logical return and gameplay only, not visible Campaign presentation.

- **M3 defeat/retry passed**, wrapper exit 0: `/private/tmp/warline-m03-readiness-defeat-01.log`. All eight rifles physically retreated through normal Move requests; core breach at 118,290 ms produced a visible localized zero-star/no-reward result without victory debrief. Actual Retry produced a new 20-member attempt with two Ping charges and the full starting budget. English 16:9 and Persian 20:9 captures were visually inspected.

### Architecture detail

| Fixture | Passed | Failed |
|---|---:|---:|
| ProductionSourceGrowthArchitectureTests | 11 | 6 |
| ScriptArchitectureAlignmentContractTests | 43 | 15 |
| EcsBurstHotPathArchitectureTests | 8 | 4 |
| NonUiSystemBaseMigrationArchitectureTests | 17 | 2 |
| NonEcsSystemConversionArchitectureTests | 8 | 1 |
| ResourceExchangeArchitectureGuardrailTests | 8 | 0 |
| TacticalResourceOwnershipArchitectureTests | 1 | 0 |
| M01FirstContactBurstAotArchitectureTests | 2 | 1 |
| FirstLaunchArchitectureAlignmentTests | 8 | 2 |

[Exact test names, failure details and log hash](readiness_architecture.json) preserve all 31 failures. The source-growth subset remains 11/6; the earlier “six pre-existing failures” statement covered only that subset and is not a complete architecture audit.

## Remaining completion work

1. Resolve the architecture failures without increasing budgets or adding speculative exceptions. Source-growth decomposition alone is insufficient: ECS inventories, managed/Burst classification, runtime hierarchy discovery, static registry ownership, assembly dependencies and narrative boundaries also require review. Guarded exact source identities need a deliberate compatibility decision; silently replacing hashes is not an architectural fix.
2. Reconcile M3's remaining acceptance rows with the final integrated assets and repeat the implicated behavior/presentation checks. Guidance modes, outcome/camera teardown and both-language end-to-end acceptance need their full stated coverage.
3. Close the allocation target. Prior M3 frame-time p95 budgets passed, but inclusive frame allocation was 31–32 KB/frame; ten measured ECS owners allocated zero, while changing Ping HUD text still allocated. The allocation gate remains failed, not waived.
4. Finish the intended voice scope or explicitly revise that product requirement. Both missions have bilingual captions. M3's narrative regression fails for missing voice and M4 has no recorded voice clips. No external voice submission is attempted by this audit.
5. Validate pacing and learning with actual players. M3 has two successful defensive strategies and a recoverable sensor-loss run, but idle results varied between loss and victory. M4's expert rescue takes about 75 seconds, before pursuit activates at 120 seconds; the observed idle run survives until the 600-second deadline. Those observations do not establish engaging combat pressure, a 6–9 minute first-time experience, or replay interest.
6. Keep the reported APC movement fragmentation open as not reproduced. The earlier bounded vehicle review observed coherent geometry and did not deliver a vehicle geometry fix; it is not evidence that the original report is resolved.
7. Obtain fluent Persian editorial and listening review where required. Automated glyph, bounds and shaping checks do not establish natural wording or spoken quality. Android remains excluded by the user and is not a completion blocker for this Editor-only review.
8. Finish Campaign-card objective and star-goal presentation. The visible M3 return capture still shows generic secondary objectives and “no unit losses / under 15:00” goals, while M3 actually scores completion, civilian safety and an undamaged post. The final Persian M4 Campaign capture also shows template secondary objectives and the 15-minute goal despite its configured seven-minute time star. Both mission-specific card partials currently replace only the primary objective and leave other template labels. This is a presentation gap, not a failure of the tested result settlement rules.

“Satisfying” endings and “fun” cannot be certified by asserting a result enum. Technical acceptance requires visible, readable, actionable results with correct mission facts and saved rewards; experiential acceptance still needs player observations.
