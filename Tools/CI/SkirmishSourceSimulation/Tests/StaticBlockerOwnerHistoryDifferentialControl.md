# Static blocker owner-history scan differential

Run the standalone source control with:

```sh
dotnet run --project Tools/CI/SkirmishSourceSimulation/Tests/StaticBlockerOwnerHistoryDifferentialControl.csproj --configuration Release
```

This is an opt-in, source-only semantic fixture. It does not edit or replace
`StaticGridBlockerUpdateSystem`, does not change any default source manifest,
and does not enumerate Unity ECS queries. It checks the audited two-domain
predicate against the candidate union predicate, with SameOwner applied after
membership selection. Preflight mismatch detection completes before the
synthetic apply phases. New, existing, removed, and opening phases retain their
authored order in both controls.

The eight labeled fixture groups cover: the three component-membership cases;
matching grid identity/epoch; same grid index with a different entity version;
an applied-epoch mismatch; transitions across the incomplete-static boundary;
re-entry into each old query domain; a foreign owner; and overlapping friendly
pass rows whose order is compared. Rebuild outcome, storage-clear decision,
and eligible phase ordering are compared on each case.

The model establishes only predicate-level semantic agreement for these
fixtures. It does not execute production code, prove all possible state
equivalence, establish query enumeration cost, or make a speedup claim. The
existing audit's source pins and one-sample diagnostic evidence remain as
written in `2026-11-02_static_blocker_query_audit.md`.

## Generated owner candidate

`Recipes/static_blocker_owner_history/generate.py` is the opt-in source recipe.
It requires the exact audited production owner SHA-256 and the exact two-scan
block. `prepare_manifest.py` writes a separate alternate manifest that changes
only source ID `349_StaticGridBlockerUpdateSystem.cs` into a
`current_original` generated row and marks the manifest `experimental_only`.
The default manifest and game source remain untouched. Candidate membership is
`!has StaticGridBlocker || (has UnitGrid && has GridBlockerSize)` using
read-only component lookups. It then performs the existing `SameOwner` check.
Rebuild is still decided before clear or delta application; all other owner
source, including new/existing/removed phases, is byte-for-byte retained.

The recipe and alternate-manifest Python controls passed. The actual compiled
owner differential also passed: both the original manifest and candidate
manifest compiled in the same 708-source source host, then ran the same
`StaticBlockerOwnerHistoryHostControl` main against fresh shim `EntityManager`
worlds. Five outputs compared byte-for-byte: matching history domains with
incomplete-static exclusions, entity-version mismatch in each scanned domain,
filter-component structural transitions, and overlapping new friendly-pass
writes. Receipt and compile/run logs are in
`/private/tmp/static-blocker-owner-history-experiment-r1/compiled-differential/`.
The candidate host build is at
`/private/tmp/static-blocker-owner-history-experiment-r1/build-candidate-r2/`
and the baseline at
`/private/tmp/static-blocker-owner-history-experiment-r1/build-original/`.
No Unity execution or mission run occurred. This validates the compiled source
owner against the repository's source shim, not Unity ECS provider parity or
query-enumeration performance; no speedup is claimed. The earlier synthetic
predicate fixture remains a separate simpler control.
