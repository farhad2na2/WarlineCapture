# Campaign mission data prevalidation

`skirmish_mission_data.py` reads an actual native asset export and reports all 120 canonical Skirmish IDs × three sizes × four difficulties. It does not open Unity, download LFS payloads, modify assets, publish catalog entries or generate a simulated mission from incomplete inputs.

The native exporter reads authored Menu Campaign references, complete referenced physical grids/surfaces, actual selected expansion definitions, native compiler results and static authoring budgets. Default packaged and explicit candidate catalog exports are separate snapshots. A missing explicit `CampaignMap` on an existing definition is reported as **LegacyRuntimeBindingNotCaptured**; this does not establish that its existing runtime fallback is broken. Missing definitions remain explicit records with twelve unavailable vectors.

Create the snapshot through the checked macOS wrapper while Unity Hub remains open and signed in (new output directory required):

```sh
rtk proxy Tools/CI/invoke_unity_macos.sh --timeout 1200 \
  --log Logs/<new-export-run>.log -- \
  -quit -executeMethod Game.Tests.Editor.SkirmishOfflineDataExportTests.RunFocusedValidation \
  -skirmishOfflineExport Logs/<new-export-directory>
```

The focused suite writes both the requested bundle and a `-repeat` bundle and compares exact exported bytes. Add `-skirmishOfflineCatalog Assets/<exact-candidate-catalog>.asset` only when intentionally checking that existing candidate catalog; it does not publish or change packaged selection. Export-only uses `Game.Editor.SkirmishOfflineDataExporter.RunFromCommandLine` with the same explicit output/catalog arguments. Windows execution must use the checked repository PowerShell wrapper described in `AGENTS.md`. Never invoke the Editor directly or add macOS batchmode.

Run once after the repository's checked Unity wrapper produces an export and a verified native run receipt:

```sh
rtk proxy python3 -B Tools/CI/skirmish_mission_data.py \
  --bundle Logs/<export-directory> \
  --native-receipt Logs/<verified-native-run>.json \
  --require-native-verified \
  --output Logs/<new-report>.json
```

The report destination must be a **new** JSON file under project `Logs` or the system temporary directory. Existing files, source folders, the bundle and all input paths are protected. Exit `0` means export integrity is valid; it does **not** mean placements or missions are ready. Exit `2` means malformed/stale/unverified required evidence. Placement and coverage blockers are reported separately, with mission, vector, source, field and reason. JSON output is deterministic; elapsed timing is printed only in the concise console summary.

Two receipt levels remain separate:

- `bundle/receipt.json` pins `manifest.json` and records that native exporter source hashes stayed unchanged.
- `--native-receipt` additionally requires `schema_version:1`, exact `manifest_sha256`, project-relative `native_log_path`, `native_log_sha256`, integer `wrapper_exit:0`, `reaped:true`, and `required_pass_marker:"[SkirmishOfflineDataExporter] result=Passed"`. The fixed marker must occur in the hashed log. Without this proof, `native_run_verified` stays false. These receipts are local evidence bindings, not signed attestations.

The tool reuses the existing canonical CSV/matrix validator; verifies source bytes, GUID metadata, required gameplay `.cs`/`.asmdef`, settings/package and canonical CSV closure; rejects newly added unpinned policy sources; and verifies every complete binary artifact before use. Accurately exported missing/empty/LFS-pointer sources are blockers. The complete Resources inventory is conservative: an unavailable unrelated resource is a global snapshot blocker, not evidence that every mission is broken. Virtual Unity package paths resolve only to one installed cache with the exact pinned lockfile name/version and matching `package.json`; missing, ambiguous or drifted payloads fail without downloads. Changed status or bytes invalidate the snapshot. Duplicate JSON keys, nonfinite JSON/binary surface values, path escapes, inconsistent table sizes, frame disagreement and tampered receipts fail closed. Parsing is tied to the exact hashed bytes, with input file stamps rechecked at completion.

Physical Campaign membership requires a current authored Campaign reference with the same scene GUID **and** grid/surface asset identity. Scene reuse alone is separately observable. Counts distinguish Campaign missions, physical scene GUIDs, exported logical map definitions and reusable mask analyses. There is no assumption that 120 missions need 120 physical maps.

## Fast logical draft iteration

Keep one frozen export and edit a separate draft JSON. This avoids rerunning Unity for each proposed anchor/route cell change:

```json
{
  "schema_version": 1,
  "manifest_sha256": "<exact export manifest SHA256>",
  "missions": [{
    "canonical_id": "S074",
    "map_key": "<exact exported Campaign-member map key>",
    "points": [
      {"id":"entry","role":"infantry_anchor","cell":[12,18]},
      {"id":"objective","role":"objective_point","cell":[28,18]}
    ],
    "routes": [{"id":"approach","waypoints":["entry","objective"]}]
  }]
}
```

Add `--draft /absolute/path/draft.json` to the command above. Only these fields are accepted. Map/manifest identities, canonical IDs and referenced point IDs must match exactly. Integer cells are explicit proposal coordinates, not invented world projections or final spawn positions. No stats, budgets, geometry or compiler outputs can be overridden.

Draft `role` uses **StaticProbeRoleV1**, an analysis-purpose vocabulary: `infantry_anchor`, `vehicle_anchor`, `air_anchor`, `structure_anchor`, `objective_point`, `route_waypoint`, `logical_anchor`. These are not native mission `RoleId` values or proof that a corresponding native role is configured. Only `infantry_anchor` adds a single-cell admission rejection diagnostic. Other roles retain observations without incorrectly requiring aircraft, structures or objective areas to be walkable infantry cells.

Each proposed cell reports full-grid containment, authored walkability, native infantry single-cell surface admission and conservative **geometric cell-envelope** containment inside exported world/playable rectangles. The envelope is a real-number geometry diagnostic, not Unity rounded-center arithmetic, footprint clearance or native placement legality. An entire cell must fit for this check to pass. Existing exported desired points use native projected cells and report their actual world-point rectangle containment separately.

The draft-only console/JSON summary separates proposed bounds/admission findings from global missing-definition blockers. Only a route segment whose **both** endpoints use `infantry_anchor` produces an analytical infantry-mask rejection; other route types retain observations without assuming infantry semantics.

Route segments report whether anchor cells belong to the same four-neighbour component of the joint walkability/admission mask. This is a geometric diagnostic, **not native pathfinding**: it excludes layered surface-edge validity, diagonals/corner rules, actor footprints, dynamic occupancy, runtime building blockers/openings and road projections. Shared grid/surface analyses are cached within an invocation across missions; no stale persistent cache bypasses source hashing.

## Limits and evidence

The consumer retains full exported maps and never crops them to fit the existing bounded simulator. Parsing is bounded to 128 MiB JSON, 256 MiB per artifact, 16 million cells per map, 32 million total distinct analysed topology cells and 16 GiB aggregate source/artifact hashing. Drafts allow 120 entries, 4,096 points/routes per entry and 65,536 total points. Exceeding a limit fails closed.

Unavailable native `setup` values may serialize as default objects rather than JSON null; only `compile_success:true` establishes setup authority. Native compile success, exact static authoring budget, authored grid observations and live mission readiness are separate facts. Every variant currently remains `replay_eligible:false`: exports do not contain complete runtime actor statistics/initialization, final spawn placement, session/fog state, runtime blockers, economy/production or complete objective/system orchestration. The report also identifies unsupported role kinds/objectives and maps exceeding the existing 64×64 bounded world replay. Desired force/structure positions are authored compiler anchors, never final spawn receipts. Typed route/anchor observations do not replace normal-input ARIA, result/return, visual or device acceptance.

Run adversarial synthetic controls separately:

```sh
rtk proxy python3 -B Tools/CI/skirmish_mission_data_test.py
```

Synthetic integrity fixtures are not real Campaign qualification. Actual native export coverage and repeat timings must be recorded against the exact exported manifest, current source pins and verified wrapper receipt.

## Verified snapshot, 2026-10-23

The checked native run passed nine checks and produced byte-identical export/repeat bundles. The verified receipt is `Logs/MacCampaignExportAudit20261023/verified-receipt-r1.json`; bundle `export-r1` has manifest SHA256 `4c7fc96d98f447e383cccfe9092ae24896a3471a29da3badfe31c9653d1c119d`.

The actual default packaged snapshot contains 120 explicit catalog records and 1,440 vectors, with 48 successful compiler/static-budget vectors across S002–S005. Only S005 currently has an explicit serialized CampaignMap binding in that selected catalog. The other existing definitions' legacy runtime bindings are not captured by this exporter; the remaining 116 definitions are absent from that packaged selection. This is a selection/coverage observation, not a new runtime regression or a claim about another candidate catalog.

All 26 exported logical map definitions had full matching grid/surface facts; they reuse 11 physical Campaign scene GUIDs through 25 Campaign rows. The consumer reused 11 joint-mask analyses. Native source pins plus complete artifacts and verification inputs required approximately 376 MB of hashing. Full verified coverage took 5.59 seconds end-to-end, with an exact report repeat in 5.32 seconds on this Mac.

A separate batch of **120 proposed logical drafts** sampled 240 real exported anchor cells and 120 mask-topology segments across those maps. Full integrity + draft analysis took 5.41 seconds and repeated identically in 5.57 seconds. Nine mask segments had an unadmitted endpoint; the report preserves them as unresolved observations. The selected `logical_anchor` purpose does not assert infantry suitability. These are proposal probes, not 120 compiled, simulated or completed missions. All 1,440 vectors remain ineligible for automatic full-world replay.

Seventeen synthetic adversarial groups passed. Complete stdout/stderr, actual reports, deterministic comparisons and the draft input are retained under `/private/tmp/skirmish-mission-data-20261023/`; the aggregate receipt is `offline-result.json`. The first actual run's nested-JSON decoding failure and second run's virtual-package path failures were preserved and corrected before final verification.
