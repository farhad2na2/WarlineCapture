# Read-only Skirmish catalog diagnostics

This additive Python tool collects multiple inspectable config/data errors. It reuses the current `build_skirmish_authoring_batches.validate` canonical gate and retains the entire `skirmish_ground_objective30_contract.validate` gate for eligible Ground30 inputs. It does not modify, repair, regenerate or replace any admission validator, asset, authority, pointer or file. Output is JSON on stdout; exit0 means **no diagnostic errors**, not admission/readiness. Exit1 means errors were found.

```sh
GIT_LFS_SKIP_SMUDGE=1 GIT_LFS_SKIP_PUSH=1 PYTHONDONTWRITEBYTECODE=1 rtk proxy python3 -B Tools/CI/skirmish_catalog_diagnostics.py
GIT_LFS_SKIP_SMUDGE=1 GIT_LFS_SKIP_PUSH=1 PYTHONDONTWRITEBYTECODE=1 rtk proxy python3 -B Tools/CI/skirmish_catalog_diagnostics.py --input Design/AgentReports/SkirmishExpansion/Full120/source-fit-authoring-batches-20261015-v2.json --input Tools/CI/SkirmishProductionDrafts/GroundObjective30/ground-objective-thirty.authoring.json
GIT_LFS_SKIP_SMUDGE=1 GIT_LFS_SKIP_PUSH=1 PYTHONDONTWRITEBYTECODE=1 rtk proxy python3 -B -m unittest discover -s Tools/CI -p skirmish_catalog_diagnostics_test.py -v
```

`--root`, `--catalog`, `--matrix` select explicit authority files; defaults are current SCENARIO_CATALOG.csv / INITIAL_SETUP_MATRIX.csv. Repeated `--input` accepts version1 `UnqualifiedFullCatalogLogicalAuthoringInputs` and `UnqualifiedGroundObjective30Authoring`. Other schemas are rejected explicitly, not guessed. No family/AirCombined80 admission is implied.

Checks cover exact120 IDs and tuple product,360 size vectors, duplicates/missing vectors and tuple joins; supplied mission IDs/tuples/exact complete canonical size rows and hashes; packet/source/grid/surface pin drift; Full120 logical requirements; Ground30 typed objective roles/IDs/shapes/finite points, unmeasured views, pending gates and publication restrictions. Locations are CSV logical record rows (row2 is first data record) or JSON-pointer-style paths; invalid JSON has a file-level location. JSON duplicate keys/nonfinite literals are rejected.

Malformed canonical data blocks authored budget/identity joins. Invalid/missing pinned Ground30 authority blocks dependent joins and strict admission, while independent structural/pin errors remain reportable. `blocked_checks` and per-input `strict_contract` distinguish those paths. Only after clean diagnostics, the original Ground30 validator still checks its remaining authority/source joins; its first remaining error is retained. Other preflight errors explicitly block its downstream file reads. No errors from a partial scan must be read as a successful skipped check. Full120 diagnostics never claim a corresponding strict admission gate was executed.

Pins resolve inside root (including symlink targets); missing/escaping files, hash drift, pointers without LFS payload, and files larger than64MiB are errors. Files are read once per invocation for diagnostic hash caching. This is not an atomic snapshot or security/TOCTOU admission: existing strict operational validators must recheck exact bytes before use. No subprocess except CLI tests, no Unity, downloads, LFS invocation, transport or source parsing from large scenes. No missing-payload hydration.

Limits: this does not establish canonical numeric balance independently of the supplied canonical CSV; authored budgets are compared exactly with that authority. It does not validate every possible authored schema or every runtime semantic invariant. It does not resolve Unity imports/scene packaging, execute objectives, create combat/movement, establish full placement prefixes/path independence/service/fog correctness, certify manual/ARIA/result-return, or grant APK/device/player acceptance. Empty authored area lists remain allowed exactly as in the existing unqualified Ground30 contract; their explicit pending gates must remain. No tests execute gameplay.

Tests use actual current canonical/index/Ground30 data plus synthetic temporary negative controls. Synthetic pinned payloads exercise diagnostics only, never pretend to be real map/source acceptance. Current-data drift/missing receipts are reported as observations rather than repaired or waived.

## Shared serialized-grid newline portability

Only source `gridPath` pins resolving to `Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset` may use the narrow LF/CRLF diagnostic proof. The bounded UTF-8 Unity YAML must have the exact grid name/script identity and uniform LF or CRLF endings; the expected raw SHA256 must equal the in-memory alternate representation exactly. Other content differences, mixed/bare-CR endings, binary data, other files and non-grid/authority contexts retain raw-byte failure. No file is normalized or repinned.

`representation_equivalences` reports actual/expected raw hashes, byte sizes, exact transformation and proof per consumer. This is a diagnostic representation finding, never a native provenance substitute or source-fit certification. Strict Ground30/native validators retain their original raw checks, and missing survey authority still blocks downstream admission.

## AirCombined80 metadata adapter

`--input Tools/CI/SkirmishProductionDrafts/AirCombined80CoherentAuthority/air-combined-eighty.coherent.authoring.json` accepts the existing version1 schema as diagnostics only. It reuses the pinned Full120 and Ground30 diagnostics, checks all80 A/C identities/240 canonical row copies,77 generic versus3 protected owners, exact family/template map/start/non-BA objective joins, source/requirement/profile pins, unmeasured role-view metadata, and explicit retained route/anchor/pad references in the30 reused ground templates. Protected S003/S004/S005 keep their original binding and no family/template replacement. Profile role checks read only the small pinned serialized profile assets; they do not assert a native catalog selection.

The original a869 authoring file remains a required raw native-envelope input and is never reconstructed. Missing survey authority and changed requirement-source pins remain errors. Expected after-state/native results, budgets and pins are not rewritten. Dependency inputs appear in the report; static internal references cannot establish native anchor existence, physical route independence, facility/aircraft fit, lifecycle, readable camera or complete missions. `NotAdmission_AirCombined80DiagnosticsOnly` and explicit blocked native gates remain even when static metadata has no errors. No native successor-equality validator or collector is replaced.
