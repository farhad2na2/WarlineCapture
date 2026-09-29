# Map preparation status

Updated: 2026-09-29. Work is in progress; no map foundation is qualified.

## Current gates

| Map | Status | Authoring, deterministic replay, full bake | Packed runtime and actual routes | Visual and device acceptance |
|---|---|---|---|---|
| RefineryDistrict | In progress | Passed: 217 independent neutral building owners | Packed scene loads and all nine gate/depot/ridge unit routes passed in Native15; outside-destination rejection and full reload pending | Authoring reviewed; native and device pending |
| CityEdgeAirfield | In progress | Passed: 344 independent neutral building owners | Clean content build and ambient/fog parity passed; per-map native routes pending | Authoring reviewed; native and device pending |
| AshLinePort | In progress | Passed: 232 independent neutral building owners | Clean content build and ambient/fog parity passed; per-map native routes pending | Authoring reviewed; native and device pending |
| Frontier | In progress | Deterministic inventory exported; conversion supports explicit offset | Full candidate waits for the three medium maps to pass runtime gates | Pending |

The first inventory and 24-house fixture stage was committed and pushed as `5154a7717`.
The historical stage report is retained in `Evidence/inventory-stage-status.md`; its outstanding items
are historical and superseded by this status and each final Candidate manifest.

## Integration candidates

Each medium map has independent `PreparedEntities.unity`, generated surface/grid and ground assets,
exact source classifications, owner identities, prefab provenance, proposed corrected zones and captures.
Actual paths, GUIDs, SHA-256 hashes and regeneration entry points are in `<Map>/Candidate/output-manifest.json`.
The generator is `map-prepared-candidate-v6`. No production catalog or mission binding is changed.

Unsupported industrial, warehouse, hangar and tower classes are explicitly non-destructible static
obstacles. They have no health, production or targeting role. Validated house/shop/civic structures use
existing definitions and matching source destruction geometry. Intact/destroyed normalization applies
only to validated hierarchy branches. Composed kits retain explicit source child recipes and state policies.
A building whose footprint crosses the playable boundary blocks its overlapping cells as static scenery.
Grass and pebbles remain nonblocking. Authoritative buildings are not also baked into generic static blockers.

Each full scene baked successfully: Refinery 25,158 entities; Airfield 26,093; Port 28,574.
Independent damage keeps neighbors intact and rubble blocked. Fresh state, identity uniqueness,
three presentation roots, zero active scenery vehicles and serialized 2,097,152-cell surfaces passed.
Actual owner counts and content hashes are retained in the manifests and full validation log.

## Coordinates and restrictions

Simulation remains 1 m / 2048 × 1024. Medium gameplay remains 400,300 through 1000,700.
Outside gameplay is blocked. Ground follows triangulation; upward walkable mesh triangles compose
runways, pads and piers with bridge deck precedence. Water outside valid crossings remains excluded.
The Port bridge deck sample is 0.08 m; canal ground is -4 m.

All seven known boundary exceptions retain their source bounds and explicit candidate decisions in the
output manifest. These are district/location proposals, not accepted mission deployment footprints.
Spawn/build radius and actual movement remain runtime gates. Frontier conversion exports (-176,0,-176)
and preserves separate source/runtime coordinates; full translated candidate validation is pending.

## Evidence and reproduction

Full authoring evidence: `Evidence/medium-content-06-authoring-passed-packing-failed.log`.
Required marker observed: `[MapPreparedValidation] result=Passed maps=3 deterministicRebuild=Passed fullBake=Passed scope=AuthoringSurfacesAndDestruction`.
Wrapper exit: 0; subsequent packing failed its required marker. Per-run code identities and failed logs are retained under `Evidence/`.
Early stage validation also passed preparation, record factory, attachment, realization, destruction and
48 virtualization regression checks. Resident presentation no longer requests virtualization implicitly;
explicit stripping without a render database remains rejected.

Keep Hub open and signed in. Run through the mandatory macOS wrapper:

```bash
rtk proxy Tools/CI/invoke_unity_macos.sh --timeout 900 --log /private/tmp/map-prepared-medium.log -- \
  -quit -executeMethod MapVariantPreparedCandidateTests.RunMediumPreparation
rtk proxy Tools/CI/invoke_unity_macos.sh --timeout 900 --log /private/tmp/map-prepared-content.log -- \
  -quit -executeMethod Game.Editor.MapVariants.MapVariantCandidateRuntimeBuilder.BuildMediumContent
```

Require the corresponding aggregate pass marker, every per-map marker and wrapper exit 0.
The initial packing attempt failed because AssetDatabase did not return a saved scene-component local ID;
the exporter now uses GlobalObjectId as the existing repository probes do. The failed log is preserved.
A fresh Editor packing run exposed a grid asset without a reloadable script; generation now uses the existing
`GridAuthoringSceneConfigAsset` class, keeps the candidate GUID and passed all authoring checks again.
The next definition check required one typed anchor; only a camera reference anchor is supplied.
The first native bundle build then exposed a target-output path error: it tried the active Android folder
instead of explicit OSX. The publication transaction now protects the full shared `aa` tree and copies the
requested OSX catalog. Android output was restored; the failed run's OSX scratch output is retained and
included in the next preservation snapshot. Pending runtime packing must prove complete source-hierarchy
exclusion, content publication and shared-output preservation. Run 08 built Refinery native content but
failed the protected production-settings check because temporary groups leaked into the default
Addressables settings. Its failed log is retained; the verified original settings bytes were restored.
The packer now uses the repository candidate rollback transaction. Run 10 passed all three native content builds, matching prototype references and the existing dense-city
read-only comparison content, with exit 0 and every required marker. All 469 original protected files
and the complete shared Addressables aa output remained unchanged. The first Editor runtime run failed with zero owners despite loader readiness. Its valid SubScene GUID
selected automatic Editor ownership while autoload was disabled. Bindings now leave the scene reference
empty and request packed loading from navigation metadata. Native standalone tests are required because
Editor SceneSystem resolves Editor artifacts. Content builds do not establish runtime acceptance. See `Evidence/packed-content-10.log` and
`Evidence/packed-content-preservation.json`.

## Visual review and budgets

The three full authoring battle and top-down reference captures were inspected. Industrial landmarks,
runways/helipads/parked aircraft, canal bridges and cargo yards remain distinct. These are authoring
captures and do not establish native runtime visuals or player acceptance.
Native packed battle frames, damage pairs, moving units, reload/unload, existing-map switching and actual
routes are pending. Desktop profiler samples are diagnostic. Device acceptance must use existing mobile
budgets: baseline Android p95 <33 ms; recommended p95 <25 ms over the prescribed steady ten-minute run.
No passing device measurements or normal-input mission evidence has been claimed.

## Ownership, rollback and next work

The existing checkout preserves unrelated resource/test/font changes and mission evidence. Source
prototypes, vendor assets, dense-city sources, mission/scenario/catalog/UI/progression remain protected.
Generation checks 469 protected files before and after. Generated outputs are under variant-specific
paths; no Unity scene YAML was hand-edited. Generated scenes, meshes and captures use LFS.

Owned additional source comprises prepared candidate generation, isolated packing, actual unit fixture,
full-map Editor and PlayMode checks, zero-active-vehicle readiness support and slice helper exposure.
Rollback reverts only these owned code changes and removes their candidate derivatives; never reset or
clean the shared checkout. Production bindings require no rollback switch because they were not changed.

Continue packed/native/movement and existing-map switching gates before full Frontier generation.
Mission migration must consume accepted physical source manifests and separately author mission anchors,
objectives and complete normal-input mission acceptance. Prepared candidates do not qualify missions.

## Latest native validation lane

Run 12 passed clean transactional content builds for all three maps and the existing map.
Run 13 preserved the entity archives and repacked the bindings with matching prototype ambient/fog
settings and explicit metadata-based packed ownership. The standalone ARM64/Metal/Mono validation
player built in Native03, but runtime catalog loading failed against stale cached catalog bytes. Its runtime pass markers and NUnit results remain pending. Editor runs cannot
certify the packed entity archives because Editor SceneSystem resolves Editor artifacts.

Native fixture settings and build identity are recorded in `Evidence/native-test-settings.json` and
`Evidence/native-player-01-code.json`. Device and player acceptance remain pending.

Native02 failed Burst AOT in the existing Split Front guidance system. An identical static FixedString
constant now supplies its battery-role comparison; no mission bindings, guidance text or rules changed.
Native03 built successfully, but loaded an older cached catalog while startup catalog updates were disabled.
Published bundle/catalog CRCs agree. The cache diagnosis and failed full logs are retained. The runner then
lost PlayerConnection and timed out; the user explicitly authorized recovery of only this failed Editor/player.
Native04 enables current catalog hash checks, separates providers by published hash, adds native top-down/
bridge captures and writes real NUnit results through a local callback. Runtime gates remain pending.

Native15 packaged the loose EntityScenes files and reached all nine Refinery unit routes, but outside-playable
destination rejection changed the test unit's grid cell. Its full native NUnit XML is failed; the following
switching test inherited the failed map scene and produced Entities Graphics errors. Isolated native switching
passed all seven variant/existing loads and unloads in Native08 and again in Native16. Native16 exited 0 with
1/1 NUnit passed and no new macOS crash report. Earlier players generated `SIGSEGV` reports in Unity's
`AssetBundleAnalytics::UnregisterAnalyticsEvents` on `Application.Quit`. The scoped native test callback now
atomically writes actual NUnit XML and exits the disposable test player without entering that crashing native
shutdown path; the Editor watcher still requires every test to pass. The crash and failed evidence are retained.
The launcher-compound/substation traversal remains an unresolved route finding; the defined gate, depot-road,
and ridge-zone fixtures passed in Native15. Full medium reload, all-map route, and Frontier gates remain pending.
