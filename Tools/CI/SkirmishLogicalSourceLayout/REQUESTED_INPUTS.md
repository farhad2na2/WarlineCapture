# Requested logical source/layout input checks

This is a source-only opt-in for unprotected Ground/BaseAssault authoring. It reuses the existing canonical mission/profile/budget owners. An absent optional input keeps existing collector and materializer routes unchanged.

The collector may supply `WARLINE_ALL120_LOGICAL_SOURCE_LAYOUT_INPUT` and `WARLINE_ALL120_LOGICAL_SOURCE_LAYOUT_INPUT_SHA256` together. The SHA covers the original UTF8 bytes. Schema version 1 contains `sourcePins` and ordered `missions`, each with the exact Full120 `authority`, a full `layoutJson` string, its `layoutSemanticSha256`, and `layout_input.pilot_candidate.typed_pad_world_axis_candidates`. Logical requested group/order must match the immutable mission rows; the physical Campaign source comes from explicit source pins.

The pilot search rectangles have integer faction 1 or 2 and finite positive `world_rect` arrays `[minX,minZ,maxX,maxZ]`. Each must remain inside its actual typed faction compound and their hull must equal that compound. These are unqualified compound search declarations; they do not assert individual placement or clearance. The logical `source_envelope_world` is derived from the identity-frame typed layout; supplied envelope declarations must agree. Actual source, GUID/path/hash, layout semantics and full facilities are admitted by the existing typed candidate constructor.

Global and durable mission records preserve the original raw input string/SHA and source pins. A mission record also keeps the decoded original layout string/raw SHA/semantic SHA. After candidate creation it records the initial owned layout, including its candidate catalog identity. A measurement join binds those values to the final proposed layout raw/semantic hashes and the normalized authored binding graph JSON/hash. Materialization reconstructs from recorded raw input only and checks the initial clone before any measured-layout overwrite. A failed candidate or measurement retains its original record but cannot satisfy the accepted-record join.

Run only the new bounded controls from a branch checkout:

```sh
python3 Tools/CI/SkirmishLogicalSourceLayout/run_requested_inputs.py --output "$TEST_OUTPUT"
```

`TEST_OUTPUT` must name a fresh directory outside the checkout. For a separate source-only candidate tree, pass `--authority-root` for the existing repository and `--source-root` for the candidate payload tree. The runner reads pinned base source blobs through Git without changing HEAD, index or working source. It reuses the existing portable extraction/API doubles and requires an installed .NET SDK8+ and the pinned installed Newtonsoft DLL (override with `--dotnet` and `--newtonsoft`). It downloads nothing and references the DLL in place. All generated sources, synthetic input files, binaries and full logs stay in the fresh output directory.

The controls execute the actual new helper and canonical row guard. They include real FIFO and oversize-file refusal, exact request ordering, full metadata retention, compound/envelope mutations, initial clone verification, report-only reconstruction, graph float/double serialization, and raw/layout/graph/source-pin join mutations. The typed recipe and measurement graph are synthetic test fixtures. Editor APIs and diagnostic budget outputs are explicit inert doubles. Unity owning-assembly compilation, actual three-size budget inspection, native measurement, placement/service clearance, materialization and mission acceptance remain **UNRUN**.

Use the existing `SkirmishGroundSourceFitSurvey.RunAll120RequestedSourceLogicalRefinement` entry, with its existing requested logical group, ordered catalog IDs and fresh output settings. Only this opt-in requested path reads the two new input environment variables; legacy source-group, protected counterpart and default paths keep their existing authority. The new mode refuses incompatible legacy geometry-override modes before creating a source World. It does not add a native execution route.

The existing `MaterializeAll120ReviewedLogicalCandidate` entry selects an explicit input from the independently reviewed durable report and its matching mission artifact, never from current input environment variables or a replacement file. It reconstructs the original owned candidate and verifies the original/initial/final graph join before the existing measured graph overwrite. `MaterializeMeasuredAll120Candidate` and its actual three-size native placement, ordinary compiler, source, ownership and destination gates remain unchanged.
