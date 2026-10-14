# Skirmish production capability audit

Date: 2026-10-14. Audited tracked source at `f8e0ab2c3` and the bounded lookup change described below. This is an engineering/source audit, not native gameplay or player acceptance evidence.

## Result and scheduling consequence

The first useful production slice remains the six ground Base Assault pilots. The other three objective families require shared implementation before their 90 catalog entries can qualify. Asset generation cannot close those capability gates. S005 remains separately owned; this work changes no S005 candidate asset, runtime, source map, publication row or evidence.

| Family | Direct source evidence | Remaining capability gate |
|---|---|---|
| BA | `Assets/Game/Scripts/Runtime/Skirmish/SkirmishBaseAssaultObjectiveSystem.cs:49` explicitly processes only `BaseAssault`; terminal evaluation and same-tick facts exist. `SkirmishBaseAssaultHudProjection`, BA fact projection and checkpoint services exist. | Per-entry source fit, native normal-input journeys, edge/recovery/result/return, EN/FA and device/player acceptance remain separate. Existence of runtime code is not certification. |
| FC | Enum/parsing exists in `Assets/Game/Scripts/Skirmish/Contracts/SkirmishObjectiveContracts.cs`. The actual objective update system above skips every non-BA kind. `SkirmishObjectiveConfig` provides only `ConfigureBaseAssault`. | Implement three zones, eligible dismounted infantry, capture/neutralize/grace state, ticket fractions and simultaneous terminal facts, then HUD/public planner/enemy observation and checkpoint/edge coverage. |
| BT | Enum/parsing/end-reason/role declarations exist in the same contracts. The active BA objective system cannot resolve BT; native checkpoint codec is explicitly BA-only (`SkirmishNativeCheckpointCodec.cs:34-36`). | Implement 12 stable designated soldiers, two corridor holds, latched exit, individual dwell/evacuated tombstones, impossibility result, control/public policy and persisted edge state. |
| CE | Enum/parsing exists; BA terminal/checkpoint paths above exclude CE. | Implement three original objective trucks and legal normal commands, separate allowance/fuel/sale/boarding restrictions, delivery/dwell/death tombstones, routes, repair command/persistence, HUD and policies. |

Repository-wide `git grep` over `Assets/Game/Scripts` and `Assets/Tests` found FC/BT/CE kind references only in contracts (plus unrelated menu-row names), no named FC/BT/CE systems or `VehicleFieldRepair` implementation. That negative search supplements the positive BA-only dispatch/checkpoint evidence; it does not prove that no unrelated Campaign primitive can be reused. Inspect shared capture, transport, evacuation, vehicle repair and damage boundaries before implementing each family.

The same BA coupling appears in enemy strategy/planner/economy, `SkirmishEnemyCheckpointPolicy`, and `SkirmishAriaPublicProjection` (BA fact reading). Each new objective needs integration through these existing shared boundaries, not an S-ID strategy switch.

## Production foundation gaps

1. Before this change, packaged `ToAuthoredSet` discarded every definition except S002-S005 and factory lookup checked only four concrete fields. A valid newly authored pilot could therefore exist in the packaged array yet be invisible to the shared resolver.
2. `SkirmishDefinitionBuilder.Rebuild` rewrites shared/legacy definitions and invokes `SkirmishS004CampaignMapBuilder.Build`; it is unsuitable for a config-only batch. Add a separate narrow authoring entry point using explicit sources and typed layouts, preserving GUIDs and unrelated content. Do not run the broad rebuild for new missions.
3. Ordinary `SkirmishLaunchProjection` dispatches S002-S005 by scenario index, hardcodes Regular/Standard and chooses the Desert Base registry. `SkirmishExpandedLaunchProjection.ScenarioIndex` still has map-based/Desert Base fallback. Generalize identity, selected setup and source/registry resolution before publishing pilots; never assign work ordinals as runtime indices.
4. `SkirmishPublicationValidator.cs:67` accepts only S002-S005; S003/S004 and S005 have explicit air/role gates. Generalization must preserve these gates as applicable capability requirements.
5. `SkirmishSetupCompiler.TryCompileCatalog` returns `compiled.Count > 0` at line 113. A batch owner must inspect the complete expected row set and reasons, not treat that boolean as full success. Preserve all 360 setup rows and all fixed budgets/deadlines.
6. `SkirmishSetupCompiler.SupportsGroundCommanderRollout` currently selects Combined Arms/Field/BA only. Ground Maneuver pilots must be evaluated against their actual existing enemy path rather than inheriting S005 commander evidence.

## Bounded implemented change

Reserved files: `Assets/Game/Scripts/Configs/Skirmish/SkirmishExpansionCatalogFactory.cs`, `SkirmishExpansionCatalogConfig.cs`; focused new tests under `Assets/Tests/Editor/SkirmishExpansion/SkirmishAuthoredDefinitionLookupTests.cs`.

- Authored sets now carry a complete definition-list snapshot from the packaged catalog.
- Factory lookup uses exact ordinal IDs for arbitrary authored entries, rejects unknown/empty IDs and fails ambiguous generic duplicate IDs.
- Existing S002-S005 compatibility fields and their explicit replacement precedence remain intact. No new publication or launch behavior is enabled.
- Five focused tests cover all six pilot IDs surviving conversion, list snapshot ownership, exact/unknown rejection, compatibility field replacement, null slots/lists and duplicate generic IDs.

Native validation is assigned to the coordinator. Execute method: `Game.Tests.Editor.SkirmishAuthoredDefinitionLookupTests.RunFocusedValidation`. Required pass marker: `[SkirmishAuthoredDefinitionLookupTests] result=Passed`. This report does not claim the method has run; append actual wrapper/log evidence after execution.

## Six-mission pilot inputs

All six retain BA/G, Standard first visit, Regular default and `ground;intel;transport;advanced_ground;objective_ba`. All three setup sizes require exact accounting. Logical map identities remain fixed; the physical source is a separate explicit binding.

| Mission | Logical/start | First existing source to assess | Intended distinct approaches | Regular EN and FA seeds |
|---|---|---|---|---|
| S026 | CC/E | Existing shared Campaign city sector; Citywide Alert/Network Collapse if required | Central boulevard pressure; siege via west courtyards | 104755, 130389, 155947 |
| S049 | MP/F | Existing Armor Break approach sector | Paid tanks through measured main approach; APC infantry via independent bypass | 104778, 130412, 155970 |
| S050 | MP/E | Same existing Armor Break source, separately checked placements | Established ground pressure; scouted siege flank | 104779, 130413, 155971 |
| S074 | IB/E | Existing RefineryDistrict; Armor Break sector if capacity requires | Freight avenue pressure; warehouse lane siege | 104803, 130437, 155995 |
| S097 | AP/F | Existing CityEdgeAirfield or Grounded Signal | Paid tank development on logistics route; APC infantry through covered flank | 104826, 130460, 156018 |
| S098 | AP/E | Existing CityEdgeAirfield or Grounded Signal | Established ground pressure; scouted southern sweep | 104827, 130461, 156019 |

Approach labels are packet intent, not measured coordinates. Adapt compass/landmark copy to actual existing geography. No physical map or route certification has been performed by this audit. Pin definition/scene/surface/grid/source hashes, legal bounds, native facility footprints/exits, connected routes and camera frame before creating counted candidate jobs. Preserve S025/S073 and S004 bindings and existing Campaign sources; do not create terrain, roads, runway compounds or map derivatives.

Field Standard starts each side with 8 rifle + 4 rocketeer + CAR + armored APC, 450/120/350 usable M/Oil/Fuel, 2 logistics and 7 structures at R1. Established Standard starts each side with 12 rifle + 4 gunner + 4 rocketeer + CAR + armored APC + tank, 900/240/700, 3 logistics and 10 structures at R2. Both Standard deadlines are 1080 s. War/LargeWar remain 1500/1800 s with their exact original larger matrix rows. G excludes offensive air but retains optional transport helicopter/recon, so ground-only pilot matches do not close every later role/source gate.

## Next bounded work

Coordinator should validate/commit this lookup slice, then reserve one owner for a config-only BA authoring API with source-bound typed inputs and complete compile reporting. Prepare six source/spec records without exposing missions; prove repeated import produces no diff and no Campaign/legacy asset changes. In parallel, keep the parameterized driver/queue and S005 owner independent. Assign FC/BT/CE as explicit shared capability tickets when worker slots open. Native source-fit evidence precedes long pilot matches, and immutable candidate batches precede counted qualification.

## Next slice proposal after the current C# freeze

Keep authoring and ordinary launch as two separately validated steps. The smallest authoring slice does not need to change player launch or publication.

### A. Config-only BA batch authoring

Add `Assets/Game/Scripts/Editor/Skirmish/SkirmishConfigOnlyBatchBuilder.cs` with Editor-only typed `BuildBatch` inputs: catalog ID; existing Campaign definition asset; expected source identity/content hash and scene/surface/grid references; explicitly measured logical layout asset; shared BA/G/start profile references; expected matrix row fingerprints; deterministic output folder. Validate the entire requested batch before changing assets. Restrict initial input to exact pilot tuples from the actual catalog, preserving coded IDs, and require every source/layout dependency rather than choosing a default map or copying S005 coordinates.

Create/update only each requested definition/layout asset and register its reference in the packaged expansion catalog. A source map is referenced, never `CopySerialized` back into Campaign data. Preserve unrelated catalog entries and asset GUIDs. Existing publication rows remain untouched; missing rows can remain absent until a separate publication authoring change. Compare serialized target content before marking dirty/saving; use `SaveAssetIfDirty` for owned assets rather than broad `SaveAssets`. Emit all 18 pilot setup results with explicit reasons; any failed expected row fails the batch qualification result. Do not call either existing mission builder, map builder, ground-staging builder, localization builder or environment builder.

The existing broad builder writes shared objective, army, economy, roles, publication manifest and S002-S004 assets, then calls `SkirmishS004CampaignMapBuilder.Build`. The S005 builder also writes combined-arm shared data, catalog/publication and localization, and has mission-specific source/camera/pad changes. Neither is the generic batch primitive.

Tests: add `Assets/Tests/Editor/SkirmishExpansion/SkirmishConfigOnlyBatchBuilderTests.cs`. Build a disposable fixture twice and assert identical GUIDs/serialized hashes and zero second-run dirty targets; verify unrelated definition/shared-source bytes unchanged; reject missing/stale source hashes, wrong tuple, duplicate IDs, output-path escape and unmeasured binding before mutation; require exactly three distinct size-row reports per ID. Generated pilot data remains draft and grants no native placement certificate. A short focused executeMethod should run these tests through the checked wrapper.

If creating production assets requires new measured layout values, prepare the spec and stop that candidate at source fit; do not invent coordinates to make the authoring test pass. Fixture geometry supports the builder test only.

### B. Ordinary launch identity/source path

This is a bounded vertical slice, not a one-line switch replacement. `QuickGameConfig` and its UI DTO currently carry only integer `ScenarioIndex`; `NormalizeForBaseAssault` collapses unrecognized values; library `LaunchMatch` requires `entry.IsPlayable`, which itself requires a nonnegative index. The source route must not synthesize a new runtime index from a work ordinal.

Proposed exact ownership boundary:

| Files | Minimal responsibility |
|---|---|
| `Assets/Game/Scripts/Configs/QuickGameConfig.cs`; `Assets/Game/Scripts/UI/Contracts/UiRuntimeContracts.cs`; `Assets/Game/Scripts/Composition/UiRuntimeAdapters.cs` | Carry an optional catalog selection plus requested size/difficulty through the existing config-store/normal launch command. Empty selection retains existing legacy normalization. Typed UI contract mapping must be explicit. |
| `Assets/Game/Scripts/UI/Screens/QuickCustomScreenView.cs` and `.Scenarios.cs` | Populate selection from the actually selected library card; ordinary visibility/launch still requires the applicable publication/readiness gate. QA exposure uses the existing scoped Editor validation catalog, not shipped controls. |
| `Assets/Game/Scripts/Composition/SkirmishLaunchProjection.cs` | Resolve nonempty catalog selection exactly before legacy normalization; validate content access, definition and exposed combination; compile requested setup. Resolve registry from an explicit authored binding, not the Desert Base preset. Missing binding fails before session creation. |
| `Assets/Game/Scripts/Composition/Skirmish/SkirmishExpandedLaunchProjection.cs` | Preserve legacy dispatch for existing consumers and use no map-based fallback for new catalog identities. Expanded results/replay must use catalog payload, with an explicit legacy-only sentinel/index policy audited at all consumers. |

Registry binding needs an explicit config reference (either scenario definition or a small shared launch-profile asset) because `OperationMapDefinition` has source/grid/surface references but no unit-registry field. A global certified registry may be reused explicitly; the map filename alone must not imply it. Confirm which placement/startup settings depend on that registry before selecting the asset boundary.

Reuse `SkirmishS004Environment.LoadMapForSession`: despite its name, it already resolves any packaged definition's `CampaignMap` by setup catalog ID. Preserve its S004 compatibility fallback. `MatchSceneView.OperationMapLaunch` still constructs mission/scenario IDs from legacy indices, and `MatchSceneView.Skirmish` chooses production baseline by preset index; inspect/update those consumers as part of the actual vertical slice. Resume in `SkirmishPresentationSystem` independently loads the Desert Base registry, and Adjust Setup restores an integer index; these must round-trip the same generic selection before claiming lifecycle completion.

Tests must prove two definitions sharing one physical source retain different catalog/definition/setup/result identities; unknown/missing/duplicate identities fail with no S001 session; requested exposed setup survives; draft/unaccepted combinations remain blocked; explicit source/registry are used; legacy indices 0/1/2/3 and S002-S005 behavior survive; replay/Adjust Setup/resume retain exact selection. Then run two existing missions through the generic ordinary-input adapter before counting pilot journeys. Publication generalization and native per-mission acceptance are later gates, not consequences of this launch patch.

## Windows generator baseline investigation

The full `generate_handoff.py --check` failure is retained as a failed validation. No generator, packet, audit CSV, asset or gameplay contract was changed by this investigation.

Read-only diagnostics loaded `build()` in memory, compared each output to tracked `HEAD` UTF-8 blobs and working-tree UTF-8 text, and compared all 75 inventory source bytes to their Git blobs and recorded SHA-256 values. Findings:

- Python reports default encoding `cp1252`, `sys.flags.utf8_mode == 0`. The checker at the end of `main()` calls bare `(ROOT/name).read_text()` even though generated files are written with UTF-8. All 20 packets and `Scenarios/README.md` exactly match both `build()` and tracked HEAD when explicitly decoded UTF-8; all 21 mismatch only under the default decode. Their working-tree files contain zero CRLF sequences. There is no packet/input prose drift to regenerate.
- The only generated content mismatch under proper UTF-8 is `ROSTER_SOURCE_AUDIT.csv`. `source_config=str(p.relative_to(REPO))` generates Windows backslashes for all 75 rows instead of the tracked portable forward-slash paths.
- 72 of 75 source `.asset` working-tree files use CRLF. `raw.decode('utf-8')` followed by `re.search(r'^  displayName: (.*)', ..., re.M)` captures a trailing `\r` for these 72 display names. The three LF source files match without this issue.
- The same 72 sources generate different SHA-256 values because `roster_audit()` hashes raw checkout bytes. All 75 LF-normalized working-tree byte sequences exactly match their tracked HEAD blobs. All 75 committed audit hashes match both HEAD bytes and LF-normalized working-tree bytes. **No actual source content drift was found.** Raw working-tree bytes equal HEAD only for the three LF files. Sample `git check-attr` reports `text: auto`, `eol: lf` for the audited Bomb Suit source even though its current checkout contains CRLF; do not repair 72 source assets merely to make a documentation checker pass.

Smallest proposed correction, pending coordinator reservation: explicitly `read_text(encoding='utf-8')` in the output comparator; use `p.relative_to(REPO).as_posix()` for inventory paths; normalize CRLF to LF once for source-text extraction and the documented portable inventory hash. The recorded inventory hashes already use these canonical Git/LF bytes, so this correction should preserve every tracked generated output. Do not silently redefine existing frozen native evidence hashes: this is specifically the planning source inventory's portable hash. If raw checkout provenance is needed, retain it separately from the canonical planning hash.

Add focused offline regressions proving Unicode packet comparison is independent of system encoding, inventory output uses forward slashes, equivalent LF/CRLF source files yield the same recorded hash/display name, and a genuine byte/content change still fails. Then run the unchanged full checker through the normal entry point and retain its fresh marker/log. This diagnostic is not a checker pass and does not justify bulk regeneration.

### Correction and fresh validation

The coordinator authorized the narrow generator correction after this diagnosis. Implemented explicit UTF-8 comparison, forward-slash inventory paths, line-terminator-safe display extraction and canonical LF inventory hashing, with semantics documented in `Tools/README.md`. No generated packet/CSV or source asset was regenerated or edited. Native candidate/source guard hashing is unchanged.

`rtk proxy python Design/Roadmap/Skirmish_Expansion/Tools/test_generate_handoff_portability.py` passes four regressions with `[SkirmishPlanningPortabilityTests] result=Passed tests=4`. An initial test-fixture attempt failed because the sandbox's default temporary directory denied child-file writes; fixtures now use disposable directories inside the writable Tools folder and clean them up normally. No assertion was weakened.

`rtk proxy python Design/Roadmap/Skirmish_Expansion/Tools/generate_handoff.py --check` now exits zero with `[SkirmishHandoffValidation] result=Passed scenarios=120 remainingWorkItems=117 packets=20 setupRows=360 sourceConfigs=75 mode=check`. The original failed baseline remains evidence; this fresh pass establishes documentation consistency only.
