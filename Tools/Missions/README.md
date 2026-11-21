# Quick mission checks

Use `quickcheck.py` while authoring Skirmish or Operations content. It runs shared game rules with the installed .NET 8 SDK, Python 3.9+ and RTK. It needs no Editor, player build, package download or running scene. It writes new reports under `Logs/MissionQuickcheck/runs/`. Existing outputs are never overwritten.

From the repository root:

```sh
# Stock catalog: all 120 Skirmishes at three sizes; all 60 Operations catalog entries.
rtk proxy python3 -B Tools/Missions/quickcheck.py

# Authoring batch: a new convoy rule case across nine setups, three Operations starts
# per authored mission, and four public-command cases from the external O004 draft.
rtk proxy python3 -B Tools/Missions/quickcheck.py \
  --skirmish-trace Tools/Missions/examples/convoy-dwell.json \
  --operation-draft Tools/Missions/examples/o004-quiet-archive.json \
  --operation-seed-offsets 0,1,7

# While editing a draft, run only the relevant missions and sizes.
rtk proxy python3 -B Tools/Missions/quickcheck.py --missions S019,O004 --sizes War \
  --skirmish-trace Tools/Missions/examples/convoy-dwell.json \
  --operation-draft Tools/Missions/examples/o004-quiet-archive.json
```

| Check | What the result means |
| --- | --- |
| Skirmish setup | The 120 catalog tuples and 360 size rows form the required combinations; the packaged and design matrix rows agree, including budgets. Selected rows are exported in `skirmish-setups.json`. This does not invoke the native setup compiler. |
| Skirmish objectives | The existing [objective harness](../CI/skirmish_offline_sim.md) executes production base assault, frontline, breakthrough and convoy rules on explicitly supplied facts. Five stock probes per size exercise pause, deadline, win, loss and surrender. An expectation can be wrong; each failure names its case. There is no combat, economy, difficulty, ARIA or autonomous Skirmish battle in this check. |
| Operations catalog | The CSV is compared with the production C# catalog for all 60 IDs, map, scenario, district, slot, family, packages, deadline, intel and canonical seed. An entry without an authored graph is marked `catalog_only`; the generic launch fixture never stands in for a mission. |
| Authored Operations | O001–O003 use the existing Regular host planner, tactical session, settlement and return. Seed offsets vary the campaign run seed. These are host model results; inspect the intents, terminal reason and settlement revision in `operations.json`. |
| External Operations draft | The game's tactical compiler checks the graph, anchors, routes, budgets and deadline in its **abstract district model**. The tactical session executes a fresh public-command trace per case. It uses its existing tactical rules, including its simplified attack behavior. This path does not enter the strategic loop or run autonomous ARIA. |
| Campaign source | Drafts reference a reusable packet of pinned Campaign, physical and logical map definitions. The CLI compares their serialized scene, grid, surface and coordinate frame, and reports missing physical anchor names in `operations-source-bindings.json`. It does not test walkability, occupancy or physical route fit. |

**Author without changing C#.** Add a Skirmish [fact trace](examples/convoy-dwell.json) for one objective family and select the catalog IDs and sizes it should cover. The same interrupted convoy delivery example runs against S019, S020 and S043 in Standard, War and LargeWar. The reducer owns the five-second delivery rule; the file supplies observations and assertions. For BA traces the fixed deadline comes from the selected setup row. `--skirmish-draft` separately accepts the logical input formats documented by the existing [Skirmish authoring diagnostics](../CI/skirmish_catalog_diagnostics.md).

For Operations, start with the [O004 graph and cases](examples/o004-quiet-archive.json). Its source packet is relative to the draft, so drafts for the same district can share it. Supply nodes, spawns/waves, and public orders at tactical ticks. A spawn group repeats a typed spawn with numbered IDs. The production catalog supplies map, player/enemy package, materials, seed and deadline; the runner rejects package reductions. The assembler accepts no commands that kill an actor, move an enemy by fiat, or assign a terminal result. `expect` is checked after the session runs.

The O004 example implements the rule sequence in the [mission brief](../../Design/Roadmap/Operations/Missions/D01_old_quarter.md#o004--the-quiet-archive): scan the archive, clear its guards, recover the record, protect the archive and extract. It uses all 16 player and 20 enemy actors and the 780-second deadline. Public-command cases reach Victory (tick 49), Partial (34), deadline Defeat (780) and Withdrawn (3). This is an external design draft. On the actual [Old Quarter map](../../Assets/Game/Resources/Operations/OldQuarter.asset), the archive, evidence pickup, protected home and two approaches still need physical bindings. No shipped mission or map asset changes.

**Read the exit status.** `0` means the selected checks passed. `1` means a build, data check, command or assertion failed; the run keeps its logs. `2` means selected Operations lack an executable graph. An explicit ID list requires coverage; the default catalog survey lists gaps and succeeds when its checks pass. Use `--strict-coverage` to fail a whole-catalog survey on those gaps, `--out` for a fresh destination, and `--json` for machine-readable stdout.

Both hosts are reused when their source inputs are unchanged. Mission/trace edits are read on the next run. On this Mac, the mixed batch of 1,809 Skirmish objective probes, nine authored Operations runs and four O004 draft cases took **3.64 seconds with a fresh cache** and **0.70–0.73 seconds end to end** across three warm repetitions. To repeat the cold measurement, point `--build-cache` at an empty directory under project `Logs`, `T Logs` or system temp, then reuse it. This measures authoring feedback, not full-world simulation speed.

**Escalate selectively.** Use the [existing Skirmish source simulator](../CI/SkirmishSourceSimulation/README.md) for representative fixed-seed battles or a discrepancy in movement, production, economy or AI. Use the [Campaign map reuse policy](../../Design/MapVariants/CAMPAIGN_MAP_REUSE_POLICY.md) and the repository's Unity wrappers for source-fit, Burst/native checks and the normal-input, ARIA, result/return and device gates. These remain separate from a passing authoring run.

For a Skirmish budget experiment, copy the full setup CSV, edit the desired rows and run `rtk proxy python3 Tools/Missions/quickcheck.py --missions S002,S026,S050,S074,S098 --sizes Standard --skirmish-matrix path/to/candidate.csv`. The summary records the exact file digest and field changes for each selected row. This verifies CSV selection. The objective probes exercise supplied facts, while a compiler result or a battle snapshot provides separate evidence that the game used the edited roster.

Run the end-to-end controls with `rtk proxy python3 -B Tools/Missions/test_quickcheck.py -v`.

Run an explicit ARIA queue: `python3 Tools/Missions/run_aria_queue.py --jobs S002:130365:en,S006:130370:en --log-dir /tmp/aria-queue --dry-run`.

New checkout: before its first counted queue, run one uncounted scene-cache warm-up match (for example a single `--jobs S031:130366:en` queue). Unity's first match on a fresh checkout writes untracked `Assets/SceneDependencyCache/*.sceneWithBuildSettings*.meta` files, and the queue's candidate source hash (`SkirmishCandidateSourceHash`) counts every `.meta` under `Assets`. A `result=Failed reason=candidateSourceChangedDuringQueue` on a fresh checkout therefore means those cache metas appeared during the queue: warm up, then rerun. The hash is deliberately unchanged, because stored identities and candidate directories must not re-key. Duplicate jobs are refused (`Duplicate job`); run a repeat of the same catalog, seed and locale in a separate queue.

Long-session runtime regression (required once per runtime candidate before mission rows are accepted): S026 Regular/Standard seed 104729 from the real menu, ARIA off, one infantry purchase every 30 s through presented touch controls, no troop orders, until the 18-minute deadline; then the result screen and MAIN MENU. Needs a fresh `--log-dir` outside the repository; about 22–26 minutes wall clock.

```sh
rtk proxy python3 -B Tools/Missions/run_aria_queue.py --jobs S026:104729:en --menu-journey --validation-library \
  --long-session --timeout 2400 --log-dir /Volumes/T7/Projects/WarlineCapture-program-logs/<date>/long-session-<candidate>
```

Exit 0 only for one `[SkirmishLongSessionRegression] catalog=S026 seed=104729 result=Passed allocatorErrors=0 exceptions=0 … resultScreen=1 returnToMenu=1 expiry=1` line. `result=Inconclusive` means a clean run whose base fell after 10+ minutes (no deadline); rerun. `[EcsQueryBudget]` lines report the World's query block allocator (archetypes, query shapes, blocks used/budget) every minute and a `toggles=` census of structurally toggled components at the end.
