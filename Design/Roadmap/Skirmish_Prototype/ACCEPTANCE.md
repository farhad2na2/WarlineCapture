# Skirmish prototype acceptance matrix

Status: **implementation and internal validation in progress; acceptance is not complete**.

Current evidence: [implementation report](../../AgentReports/SkirmishPrototype/README.md). Session/rule/persistence/population tests pass (9), and ARIA update stability passes in English and Farsi. Partial live evidence covers the setup/launch, starting roster, actual supply delivery, selection/movement, surrender, one replay and a normal-speed timeout. The earlier timeout run failed AI pressure/pacing review; it is not a gameplay acceptance pass. All matrix rows below require their complete evidence before sign-off. Implements the completion contract in [PLAN.md](PLAN.md). Baseline findings are in [BASELINE.md](BASELINE.md).

## Evidence rules

Use isolated QA progress. Record commit/input hashes, preset revision, seed, locale, aspect/resolution, run entry path, outcome, duration and failures. Gameplay evidence uses normal player controls and screen-position world commands. Do not edit health, positions, objectives, resources or result facts to manufacture victory.

Use normal speed for pacing/readability. Accelerated timeout tests or lower-level direct requests must be labeled mechanics tests. Keep exact pass markers/results for automated checks and captures for visual judgments. A visible button is not proof that its command affected the world.

## Required cases

| ID | Action / condition | Expected result | Evidence |
|---|---|---|---|
| SK-A01 | Open Skirmish from Main Menu in EN and FA | Correct map/preset, objective, roster and supported controls; no English fallback in Farsi | Setup captures at 1280×720 and 2400×1080 |
| SK-A02 | Change supported setup, leave/reopen, restart application | Same last valid full settings; no silent seed/rule reset | Saved JSON plus UI and runtime comparison |
| SK-A03 | Load old/invalid setup | Safe versioned migration; supported preset and visible explanation; no save loss | Migration tests; screenshot |
| SK-A04 | Deploy and rapidly tap Deploy again | One session, one scenario load and one initial spawn; applied snapshot matches setup before simulation starts | Request counts and startup readback |
| SK-A05 | Missing preset/map/anchor or failed load | Localized error with a usable Back/Retry; no M1 fallback, spending or partial active match | Failure-path check |
| SK-A06 | Start a normal match | Main Base/forces visible at zoom 40; valid build ground; no unexplained camera tour or return-camera control | Opening capture and world inspection |
| SK-A07 | Select via quick-group card during idle/combat/production | Correct stable group selected once; card stays under finger; no compulsory rectangle gesture | UI interaction recording/captures and selected entity IDs |
| SK-A08 | Move, Hold and Attack; wrong target; repeat taps | Accepted orders change real behavior; rejection explains reason; Hold is not immediately contradicted by help | Orders and world-result trace |
| SK-A09 | Build from several camera positions; rotate/cancel/reopen/confirm | Preview centered, drag owns pointer, correct footprint, final location/rotation matches preview; canceled reservation refunded once | EN/FA placement captures and transaction assertions |
| SK-A10 | Queue rifle production, close/reopen and cancel | Real four-soldier group appears; Build closes on accepted request; time/cost/cancel feedback correct | Queue/ledger/spawn evidence |
| SK-A11 | Reach population/capacity/insufficient Materials/Fuel state | Clear reason; no deducted resources on rejected request; successful recovery updates availability | UI and resource ledger |
| SK-A12 | Run logistics; block a route; lose and rebuild a supply structure | Only delivered usable resources credited; stall explained; recovery works where affordable; no hidden grants | Storage/delivery ledger and normal-speed gameplay |
| SK-A13 | Let AI develop, then attack its flank and base | Opponent spends/produces, dispatches reachable units and responds; no inert army or endless order thrashing | AI plan/orders and observed battle |
| SK-A14 | Destroy an ordinary unit/building and the enemy Main Base | Dead target cues clear immediately; destroyed art used when authored; only designated base destruction wins | Damage/outcome trace and capture |
| SK-A15 | Lose own Main Base while extra Barracks/units survive | Immediate defeat with correct reason; no accidental objective transfer to another Barracks | Real combat defeat and result |
| SK-A16 | Destroy both Main Bases on the same update; decisive hit at deadline | Draw for simultaneous base deaths; destruction before timeout policy honored | Focused authoritative-rule tests |
| SK-A17 | Reach 15:00 without decisive destruction | Visible countdown leads to one draw; no extra scoring rule | Rule test and at least one normal-speed timeout route |
| SK-A18 | Pause/resume; confirm/cancel Restart or Surrender | Clock/AI/production paused; cancel preserves attempt; restart resets all match state; surrender ends once | Lifecycle tests and visible controls |
| SK-A19 | Victory/defeat/draw → Replay / Adjust Setup / Main Menu | Correct route; same replay setup/seed, new session; no stale selections/cues or hidden modal | Complete routes in both languages |
| SK-A20 | Switch EN → FA → EN; reach outcome during speech | All visible strings and new voice requests use active language; gameplay speech stops at outcome | Playing asset-path audit and rendered text |
| SK-A21 | Show Me/help/waits under narrow/wide layouts | Every visible guide points to a usable current action; disappears/updates after use; wait clearly names cause/progress | Geometry checks and captures, including bottom-right corners |
| SK-A22 | Ten consecutive replay/exit sessions | No accumulated factions, queues, markers, audio, native storage or stuck pause ownership | Entity/allocation counts and cleanup checks |
| SK-A23 | Campaign → Skirmish → Campaign, including M1 replay and M3/M4/M5 sensitive steps | No setup/AI/camera/roster/rule leakage; original tutorial and objective behavior intact | Targeted mission regression plus saved-state comparison |
| SK-A24 | Win, lose, draw, surrender and retry with an existing profile | Campaign stars/progress, XP, persistent wallet, unlocks and Operations state unchanged | Before/after save fields and duplicate-result tests |
| SK-A25 | Quit during an unfinished match; reopen | Setup returns with last valid choices and truthful “new match” behavior; no fake resume/result/reward | Application/session persistence check |
| SK-A26 | Regenerate affected prefabs/configs and reload | Supported controls, localization, Main Base role and rules survive regeneration | Builder tests and source/asset comparison |

## Normal-speed play matrix

Run each approach in English and Farsi after S4. Record a victory, defeat or draw honestly. Obtain at least one real victory and defeat in each language across the matrix; add a run if necessary.

| Approach | Player choices to exercise | Questions for review |
|---|---|---|
| Direct pressure | Move starting group toward enemy, use cover, reinforce through production | Can the player act early? Is the defended approach understandable and beatable? |
| Defensive buildup | Hold near own base, build a defense, reinforce, then counterattack | Is there useful activity rather than a long blank wait? Is turtling an automatic win? |
| Flank / mixed force | Split groups, use longer route, combine infantry and starting car | Does the extra route create a meaningful tradeoff? Can one unit trivialize the match? |

Use at least three supported seeds for AI choices while keeping map geometry fixed. Target 8–12-minute normal matches; report actual median/range, first meaningful order, first enemy pressure and periods without decisions. Do not increase enemy health merely to hit a duration target. Any losing state that cannot progress must still have clear time-limit or surrender resolution.

## Campaign protection suite

Retain current M1–M5 acceptance evidence as the baseline. Re-run affected shared-system tests for selection/guidance, placement/resource transactions, production, audio lifecycle, localization and native cleanup. For changes to shared launch/HUD/resource/AI ownership, include:

- M1 full story replay with saved commander followed by the first actionable lesson.
- M2 construction and recruitment through result without a repeated Build cue.
- M3 gate preview/commit on carriageway and defense countdown through completion.
- M4 tap-based specialist selection, transport handoff and clearance recovery.
- M5 gate between walls, attack/death cue lifecycle and archive completion.

Preserve existing frame/allocation budgets; do not relax failing thresholds to obtain a pass. No Unity launch/test/capture bypasses the repository execution contract.

## External acceptance, recorded separately

Android gameplay is deferred for the current planning milestone. Later record phone model, build, OS, real touch/safe-area behavior, frame p95/p99 after warmup, memory across replays, suspend/resume/audio focus and thermal behavior. A build passing or an Editor capture does not close these cases.

Observe 3–5 unfamiliar players without coaching. Ask them to explain the objective before play and the result afterward. Note failed selections, wrong expectations, unreadable messages, waiting confusion and willingness to replay. Fix repeated confusion before adding maps, difficulties or rewards.

## Exit report

Publish a short completion report with pass/fail case IDs, exact tested revision, chosen balance values, completed EN/FA play matrix, screenshots, test logs and unresolved issues. Mark internal prototype acceptance, device acceptance and player acceptance separately. Any P0/P1 remains a failure for internal completion; external cases remain pending until actually observed.
