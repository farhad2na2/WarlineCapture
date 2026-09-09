# M3 Editor QA record

Updated combined-build audit: [M3/M4 readiness](../M04Airlift/m03_m04_readiness_audit.md) reports **106/137 architecture tests passing and 31 failing**. This supersedes the older source-growth-only architecture scope below.

Design-folder review images referenced here remain local and are excluded from Git at the user's request. Production game artwork under `Assets` is included.

Checkpoint: 2026-09-09 · `codex/m03-radar-warning` · base `b6b8153d241786b8e368a3910f07fb435396805e`.

**Playable implementation and substantial Editor QA are delivered; full Editor acceptance remains open.** This earlier checkpoint's behavior audit has 30 passing suites and one missing-voice failure; its source-growth subset has 11 passing and six failing checks. The combined architecture audit above covers 137 tests with 31 failures. The inclusive zero-allocation gate, final voice/listening work, independent Persian editorial review and human learning/fun rounds are unaccepted. Android QA is excluded by the user's instruction.

Source was uncommitted at this M3 QA snapshot; a later user instruction authorized committing and pushing the combined M3/M4 work. [Source snapshot](source_snapshot.json) records the final changed/new runtime, Editor, test and asset hashes; [run ledger](editor_validation_ledger.json) preserves compact result markers and full-log hashes. Earlier captures/runs are identified below and must not be interpreted as having identical source bytes to every later fix. Existing user font edits are preserved and excluded from the M3 source snapshot. No architecture baseline, identity guard, exception or performance budget was relaxed.

## Implemented scope

- A finite two-element convoy defense with eight rifles, one unarmed Ground Radar Tank, four civilians and seven hostiles; optional paid Tower/Barrier and four-rifle reinforcement; authoritative core/post defeat and independent stars.
- Seven final comic panels, fourteen aspect crops, English/Persian text, existing M1/M2 camera ownership: captured RTS view → simultaneous smooth zoom/pan to important areas → exact RTS return before the mission clock starts.
- Twelve typed ARIA lessons and guide topics, actual warning/Jump/Return View/Move/Hold/Stop/Ping controls, and 57 canonical class identities (51 real configs and six explicitly unavailable future entries).
- Attempt-scoped warning truth and radar coverage, two Ping charges, real resource transactions, outcome-aware finale/debrief/results, first-clear/replay rewards, visible atomic-save recovery, and restart/exit cleanup.

See [implemented mission contract](implemented_mission_contract.md), [architecture and ownership](implemented_architecture.md), [class coverage](../../M03_Radar_Warning_Class_Coverage.md) and [acceptance tracker](../../Architecture/m03_radar_warning_implementation_tracker.md). The tracker records 32/47 individual verified deliverables; unchecked items include implemented features whose broader acceptance requirements remain unmet.

## Latest regressions and live journeys

All logs are under `/private/tmp/`. A wrapper exit of zero alone is insufficient: the required aggregate marker determines acceptance. Live journeys use an isolated temporary campaign profile and actual menu/UI/command surfaces. Deliberate sensor damage is labeled fault injection. Accelerated combat does not establish performance.

| Check | Exact log | Result and scope |
|---|---|---|
| Final behavior regression | `warline-m03-behavior-audit-07.log` | **30 suites pass / 1 fails**, wrapper exit 0. Rules, warnings, Ping, commands, campaign entry/cache, persistence, cleanup and affected M1/M2/Skirmish boundaries pass. The new M1 HUD suite passes all eight tests, including empty-session cinematic lock. Full shared narrative fails because `m03-brief-01` has no voice clip. Aggregate remains failed. |
| Final architecture | `warline-m03-architecture-audit-04.log` | **11 pass / 6 fail**, wrapper exit 0. Fourteen reported paths are byte-identical to base. The remaining UIShellContentView identity guard already failed at base; M3 also changes that file while reducing its size. Exact attribution: [JSON](architecture_attribution.json) / [explanation](architecture_attribution.md). Aggregate remains failed. |
| Ten lifecycle cycles | `warline-m03-lifecycle-04.log` | **Passed, exit 0.** Ten actual menu → M3 → 57-class guide → restart → Pause Exit → menu cycles. Removes the purchased Tower/Barrier, four produced rifles and old roster; duplicate restart rejected; fresh budget and Ping; no stale defense/camera/request/member state; guide handles released and global clock restored. |
| Cross-mission isolation | `warline-m03-cross-mission-04.log` | **Passed, exit 0.** M1 → menu → M2 → menu → M3 → menu → Skirmish → menu. Correct physical/logical map identity and live simulation in every mode; M3 dormancy, units, warnings, camera and HUD restrictions do not leak. |
| Automatic acquisition | `warline-m03-acquisition-01.log` | **Passed, exit 0, four tests.** Real system-owned ECB singleton is included; Hold acquires in-range hostiles, neutral/suppressed targets excluded, Scan area respected. Final behavior audit repeats this suite. |
| Idle balance after acquisition fix | `warline-m03-convoy-09.log` | **Passed, exit 0.** This run stops 3/7 then loses to a core breach at 221.119 simulated seconds. Later defeat-result-03, also idle, wins at 217.112 seconds; idle balance is variable and unaccepted. Starting balance unchanged. |
| Full Guidance / ordinary Hold defense | `warline-m03-guidance-large-comics-01.log` | **Passed, exit 0.** Actual SHOW ME, warning Jump/RETURN VIEW, optional build preview/close/skip, Select All Rifles, ARIA Hold/Stop, then normal Hold. All eight rifles defend without the separate rifle Attack driver. Three-star Victory at 210.154 seconds, full 50,000 Credits/100 Materials and both Ping charges retained. Optional steps preempt correctly; finale/debrief/Victory/guide return passes. |
| Paid road Barrier / rear defense | `warline-m03-road-barrier-05.log` | **Passed, exit 0.** Actual pointer drag+Confirm spends 6,000 Credits/15 Materials; footprint `(877,426)`, `7×1`. Both armed cars and the unarmed APC take actual detours. Rear rifles win at 229.728 seconds, 7/7 stopped, safe core/post. No position, health or path-buffer edits. |
| Sensor-loss recovery | `warline-m03-recovery-02.log` | **Passed, exit 0.** Explicit sensor-health fault leaves the scout warning; invalid Ping retains two charges. Ordinary Move/Attack corrects rearward positioning at 35 simulated seconds. Victory at 191.538 seconds, 7/7, safe core/post. |
| Atomic-save recovery | `warline-m03-save-recovery-02.log` | **Passed, exit 0.** Genuine failed atomic write leaves the isolated profile unchanged; visible error in both languages/aspects. Actual Retry Save rejects duplicate/stale requests and grants exactly +2,000 Credits/+400 XP once before debrief/Victory. Guide restores the same result. Persian 20:9 capture visually inspected. |
| Current defeat/result/retry | `warline-m03-defeat-result-04.log` | **Passed, exit 0.** All eight rifles physically retreat via ordinary Move (observed at 28.803 seconds); actual core-breach defeat at 117.369 seconds, 0/7 stopped, zero stars/rewards, no victory debrief. Both languages/aspects, guide return and actual Retry pass; fresh 20-member attempt, full budget/two Ping. Persian 20:9 capture visually inspected. No health/position/outcome edits. Defeat-result-03 remains a failed probe assumption: idle actually won. |

## Presentation evidence

| Check | Exact log(s) | Accepted observation / limitation |
|---|---|---|
| Opening tour | `warline-m03-camera-01.log`, `warline-m03-camera-wide-01.log` | Both aspects: real RTS start, simultaneous pan/zoom, key-area holds and exact return; 14.190/14.136 seconds, mission time zero. |
| Skip / reduced motion | `warline-m03-camera-skip-02.log`, `warline-m03-camera-reduced-02.log` | Live skip restores RTS; reduced motion uses the captured RTS pose; no mission time consumed. |
| Guide / active classes | `warline-m03-ui-proof-09.log`, `warline-m03-ui-large-03.log`, `warline-m03-commands-08.log` | Every topic/class, both languages/aspects, normal/large text, real stats, filters/search, Persian digit order, one class handle, paused exact return; eight rifles and sensor actually Move/Hold/Stop, unarmed sensor rejects Attack. |
| Normal comics | `warline-m03-comics-05.log` | 24 opening/debrief panel × language × aspect checks, full captions and current/next panel handles; no overflow, truncation or missing glyphs. |
| Large captions | `warline-m03-guidance-large-comics-01.log` | All 24 full-caption bounds pass. Visual inspection found a duplicate old Story header; the builder was corrected afterwards. This run alone is not full visual acceptance. |
| Extra Large / corrected header | `warline-m03-extra-large-comics-02.log` | **Passed, exit 0:** all 24 combinations preserve requested 43.2 font, one correctly localized Story header/page indicator, no clipping/missing glyphs. Persian B03/D03 at 20:9 visually inspected. Current/next handles remain 1–2 then release to zero. |
| ARIA card | `warline-m03-behavior-audit-07.log` | 48 presentations: 12 lessons × two languages × two sizes; actual bounds and glyph checks; prior M1 card geometry restored. |

Opening/debrief residency is scoped to those narrative handles; menu preview, radio archive and result art are separate bindings. These checks do not establish fluent Persian editorial approval, speech quality or human comprehension.

## Performance and allocation

`warline-m03-performance-03.log` runs real-time combat without capture overhead during sampling. Victory at 195.935 seconds, 7/7 stopped. All four existing 20 ms p95 frame budgets pass over **17,153 frames**:

| Phase | Frames | p95 ms | p99 ms |
|---|---:|---:|---:|
| Preparation | 3,078 | 12.409 | 16.549 |
| Vanguard | 4,702 | 15.885 | 20.610 |
| Main warning | 3,880 | 12.800 | 14.871 |
| Main active | 5,493 | 12.029 | 15.413 |

The aggregate is **failed / wrapper exit 1** because inclusive Unity frame allocation averages 31–32 KB/frame and the zero-allocation requirement is unaccepted. Peak Unity allocation is 4,268.29 MB; Mono 1,800.35 MB. The current-thread allocation API fails its positive control and cannot be used as zero-allocation proof. [Exact report](LaunchProbe/editor-performance-03.json).

`warline-m03-allocation-05.log` passes the raw Profiler measurement-validity gate, with a genuine 8,224-byte positive control in each 210/211-frame window. All **ten measured ECS owners allocate 0 B** in all four windows. The two HUD instances together allocate 0 B when text is unchanged; changing Ping cooldown text allocates **5,064 / 4,830 B** across six invocations in each changing window (maximum 844 / 818 B per invocation). Nested markers for the same owner are counted once. This is attribution, not an all-zero result or frame-time benchmark. [Exact report](LaunchProbe/allocation-attribution-05.json).

`warline-m03-scale-baseline-01.log` passes the existing large-scene runner: 541 frames, p95 8.774 ms against 20 ms, 4,977 runtime buildings and 5,082 source-key entities. The source-key count includes buildings; the added neutral entities have no UnitAttack. **This does not prove 700 or 5,082 simultaneous combatants.** Its unsupported current-thread allocation zero is not accepted. [Preserved report](LaunchProbe/scale-baseline-01.json). Original shared baseline evidence was restored byte-for-byte after this run.

## Significant fixes found by QA

- Automatic target acquisition was never updating because its ECB singleton query excluded system entities. The installed Entities contract requires `IncludeSystems`; the real combat regression now proves acquisition.
- Three starting rifles/civilian area overlapped blockers. All 20 current footprints and the 21-segment, five-cell-clearance convoy route now pass the actual loaded grid. Original physical map hashes remain unchanged.
- Retry/exit left paid buildings, produced units and later defense-member/request records behind. Cleanup now removes only attempt-owned objects and clears presentation state; ten-cycle and cross-mode runs prove the boundary.
- Empty campaign tokens falsely matched the opening cinematic and locked Skirmish HUD controls. Active phase/nonempty-session checks and an M1 regression fix this.
- Failed durable saves had no visible recovery, and result UI could cover the guide. Actual save-failure, Retry Save and exact result-return runs verify the fixes.
- Persian guide navigation/search, comic caption bounds, duplicate Story headers and ARIA geometry were corrected from live captures and bounds checks.
- Campaign UI read the profile every frame. Revision-aware cache invalidation now handles successful writes/deletes and replaced stores; failed writes do not publish success. Nested tuple boxing and repeated convoy-query creation were also removed and measured.

## Remaining acceptance

1. **Voice production:** 23 English/Persian script pairs, 46 clips, are prepared in [the exact approval payload](voice_payload_review.json). Automatic approval review rejected transmission to `api.elevenlabs.io`; the existing explicit approval question is pending. No scripts were sent and no clips generated. Narrative/tutorial voice, listening, interruption and final speech/camera timing remain open.
2. **Architecture:** six repository guard checks remain failed. The exact attribution distinguishes prior violations and the M3-modified guarded UI file; none is waived.
3. **Performance:** frame budgets pass; full zero-allocation acceptance does not. No simultaneous 700-combatant stress acceptance is claimed.
4. **Review:** independent fluent Persian editing and real participant learning/fairness/fun rounds are absent; the observed idle loss and later idle victory leave idle difficulty unresolved. [Playability assessment](playability_and_fun_review.md) separates observed strategies/recovery from hypotheses about player experience.

The [historical QA journal](editor_qa_history.md) and [implementation chronology](implementation_status.md) preserve earlier runs, failures and fixes. Earlier combat timings before the target-acquisition correction are historical, not current balance proof.

## Reproduce

Keep Hub open/signed in and follow repository AGENTS.md. Every invocation uses `Tools/CI/invoke_unity_macos.sh`, GUI licensing, explicit timeout/log. Use `-quit` only for synchronous audits; the asynchronous probes exit their own Editor. Exact methods and log markers are recorded in the run ledger. Never accept a missing/failed aggregate marker because the wrapper returned zero.

```sh
Tools/CI/invoke_unity_macos.sh --timeout 700 --log /private/tmp/m03-behavior.log -- -quit -executeMethod M03EditorRegressionValidation.RunEditorBehaviorAudit
Tools/CI/invoke_unity_macos.sh --timeout 300 --log /private/tmp/m03-architecture.log -- -quit -executeMethod M03EditorRegressionValidation.RunArchitectureAudit
Tools/CI/invoke_unity_macos.sh --timeout 1650 --log /private/tmp/m03-lifecycle.log -- -executeMethod Game.Editor.M03RadarWarningEditorLaunchProbe.RunPaidLifecycleValidation
Tools/CI/invoke_unity_macos.sh --timeout 650 --log /private/tmp/m03-defeat.log -- -executeMethod Game.Editor.M03RadarWarningEditorLaunchProbe.RunDefeatResultValidation
```
