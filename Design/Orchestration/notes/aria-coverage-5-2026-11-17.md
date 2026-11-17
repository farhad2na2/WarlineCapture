# ARIA coverage-5: lane-8 regressions (2026-11-17)

Evidence: `/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-17/lane8/{qa,qb}/unity.log` and the decision rows under
`Design/AgentReports/SkirmishExpansion/S0xx/_Evidence/candidates/a815df95…/` (qb) and `78520559…`-era qa candidates.

1. **S050 BlockAllocator budget (qb:149119).** The exception is raised in the harness's `StartTerminalProbe` when the match
   ended at 346.5 s: the 16 MB query-data budget was already used. Entities caches query data by shape
   (`EntityQueryManager.CreateEntityQuery` hash lookup), and coverage-4 added no new query shape (its one new per-frame query
   had the same shape as existing ones), so the leak is not proven to be ARIA's. Only lane 8 qb ever logged it, in the longest
   match so far. Done anyway: every ARIA per-frame query is now created once — `AriaPlayInputSystem` uses two system-owned
   queries from `OnCreate` (8 per-frame `CreateEntityQuery` calls removed), and the projection's unit-role query is created
   once per World (`UnitRoleQuery`). Test: `AriaInputSystemCreatesNoQueryOutsideOnCreate` (source check).
2. **S050 slow win / Abort.** The generic coverage bought five kinds back to back (t=97–189, materials 900 → 20, radar and
   transport heli included); every new group reopened the roster sweep, and the enemy base took no damage from t=128 to 317
   (lane 6, without generic coverage, won at 181.9 s). Fix: coverage is affordable only above a 250-materials reserve and
   purchases are at least 25 s apart. Also the radar's support order released with Move still armed (`move=True` at
   `state=released`): a release now toggles the armed mode off first (stage 5, one press).
3. **S026 130365 Blocked (qb:153652, 56.8 s).** Pre-existing generic assault-map inspection: with the full map open, Close
   (-268224) was pressed three times without the map closing (attempts 1→3). Fix: after two completed Close presses the
   popup's "center on HQ" control is pressed once, then Close again; never a third Close in a row; after four, nothing more
   is pressed. Test: `MapCloseVariesTheControlAndNeverPressesCloseThreeTimesInARow`.
4. **S002 155923 en Defeat (qa).** Same blind home defence as the Windows lane: a tank raid from t≈69, rounds 1–3 with
   `homeGround=False`, base lost. Those values were logged before the camera reached home; the round gave up in the frame the
   re-centre Home tap completed. Covered by 8cf04be55 (4 s camera settle, all-direction home ground), which lane 8 did not
   have. The `gave_up` trace now also fires for the re-centre → observe transition.
5. **S002 104731 en consent timeout (qa:806050).** Harness consent flow (Play / Confirm before ARIA starts) timed out at the
   60 s wall-clock limit; ARIA was not active, the other four jobs passed the same flow, and the only log lines before it
   are Editor background (licensing 404, command cache cleanup). Not ARIA code; no change.
