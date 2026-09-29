# Prepared map integration guide

These are isolated candidate physical sources. Check `STATUS.md` and `prepared-source-manifest.json`
for current gate results before adoption. Mission bindings and production Addressables remain unchanged.

## Assets and identities

For each map, `<Map>/Candidate/output-manifest.json` is the authoritative inventory of generated paths,
GUIDs, content hashes, owners, classifications, surface restrictions and source/runtime coordinates.
`<Map>/Candidate/runtime-content.json` identifies the definition, renderer-free runtime binding and packed
content outputs. A content-build report alone does not certify loading or movement.

| Variant | Definition | Runtime binding | Entity source |
|---|---|---|---|
| RefineryDistrict | `Assets/Game/GeneratedOperationMaps/Variants/RefineryDistrict/Candidate/Definition.asset` | `Assets/Game/Scenes/OperationMaps/Variants/RefineryDistrict/RuntimeBinding.unity` | `Assets/Game/Scenes/OperationMaps/Variants/RefineryDistrict/PreparedEntities.unity` |
| CityEdgeAirfield | `Assets/Game/GeneratedOperationMaps/Variants/CityEdgeAirfield/Candidate/Definition.asset` | `Assets/Game/Scenes/OperationMaps/Variants/CityEdgeAirfield/RuntimeBinding.unity` | `Assets/Game/Scenes/OperationMaps/Variants/CityEdgeAirfield/PreparedEntities.unity` |
| AshLinePort | `Assets/Game/GeneratedOperationMaps/Variants/AshLinePort/Candidate/Definition.asset` | `Assets/Game/Scenes/OperationMaps/Variants/AshLinePort/RuntimeBinding.unity` | `Assets/Game/Scenes/OperationMaps/Variants/AshLinePort/PreparedEntities.unity` |

Bind an OperationMapDefinition through the existing OperationMap loading helper. Its source-scene reference
points to the runtime binding; its navigation metadata points to the entity source. Load one presentation,
with one binding lighting owner. Keep the raw prototype and entity-source GameObject hierarchy out of the
runtime binding bundle. Candidate build layouts check this exclusion.

Medium maps use a 1 m, 2048×1024 simulation grid with playable X/Z 400,300 through 1000,700.
Backdrop remains outside gameplay. Frontier uses the full grid and source-to-runtime translation
(-176,0,-176); its full generation is guarded by successful medium runtime and old-map switching proof.
The bridge surface is the actual 0.08 m driving pivot, with water excluded outside crossings.

## Reproduce content

Keep Unity Hub open and signed in; wait until this checkout has no running Editor validation.
Use the repository wrapper with explicit logs and timeouts, never batchmode or a direct Editor command.

```bash
rtk proxy Tools/CI/invoke_unity_macos.sh --timeout 1800 --log /private/tmp/map-medium-authoring.log -- \
  -quit -executeMethod MapVariantPreparedCandidateTests.RunMediumPreparation
rtk proxy Tools/CI/invoke_unity_macos.sh --timeout 3600 --log /private/tmp/map-integration-content.log -- \
  -quit -executeMethod Game.Editor.MapVariants.MapVariantCandidateRuntimeBuilder.BuildIntegrationContent
rtk proxy Tools/CI/invoke_unity_macos.sh --timeout 3600 --log /private/tmp/map-medium-packed.log -- \
  -runTests -testPlatform StandaloneOSX -testFilter MapVariantPackedPreparationPlayModeTests \
  -assemblyNames Game.Tests.MapPreparation.Native \
  -buildPlayerPath Build/MapVariantPreparedPlayer/WarlinePreparation.app \
  -testSettingsFile Design/MapVariants/Preparation/Evidence/native-test-settings.json \
  -playerHeartbeatTimeout 600 -testResults /private/tmp/map-medium-packed.xml
```

Candidate content is built explicitly for StandaloneOSX into `Library/MapVariantPreparedContent/<Map>/`.
These rebuildable output catalogs contain local machine paths and are not distribution artifacts.
Acceptance requires the standalone player: Editor SceneSystem resolves Editor artifacts.
The dedicated native assembly uses a test-player build define only beneath
`Build/MapVariantPreparedPlayer/` to exclude legacy suites that use Editor-only APIs. Normal Editor
regression builds keep those suites enabled. The native map fixtures retain their full assertions.
The isolated packer restores production settings and the shared Addressables output. Use a new log path
for each run and retain failures. Require per-map markers, aggregate markers and exit 0; native test-player runs
also require passing NUnit XML. Record revision, dirty source hashes, target and command.

## Gameplay policy for later integration

Mapped houses, shops and civic buildings are independent neutral owners. Rubble remains blocked.
Industrial plants, depots, hangars and other unsupported destruction classes are documented static
obstacles with no health, enemy targeting, production or objective role. Mission designers must supply
explicit policies for any role changes. Grass, pebbles and placement reservations are not blockers.

Zones retain original source bounds and deliberate candidate corrections. Camera overview is the only
preparation anchor. Deployment, objectives, boarding, launchers and other mission anchors require later
surface and whole-footprint clearance checks. The route fixture uses real infantry, hauler and armor
prefabs with normal movement orders; fuel is disabled only inside that fixture.

## Acceptance and adoption

Native packed load/reload, damage, routes, invalid destinations, clean unload, old-map switching and
visual review must pass for the exact candidate content hash. Desktop timings are diagnostics.
Actual Android device measurements must meet the existing ten-minute p95 budgets before qualification.
Player visual acceptance is separate from automated evidence.

Consume the accepted manifest in `../HANDOFF_Existing_Mission_Rework.md` for later campaign migration.
Each migrated mission still needs normal-input play, ARIA, result, return and player/device acceptance.
Use `../FUTURE_CONTENT_MAP_PLAN.md` for subsequent mode-specific derivatives. Neither handoff authorizes
claiming mission readiness from preparation tests.

## Native fixture catalog and result handling

The preparation fixture enables catalog hash updates on its ContentCatalogProvider after startup and
loads each catalog with a provider suffix containing the published catalog SHA-256. A reused local
catalog path can otherwise select a stale cached catalog when startup updates are disabled. Native03
proved this: the published catalog contained CRC 1788a72e, while the cached catalog loaded CRC eec9e225.
No CRC check was disabled and no shared application cache was deleted. Integrators must load the current
versioned catalog and retain bundle CRC checks.

The isolated native test assembly records the actual NUnit tree through TestRunCallback. The test player
quits after writing that result; the Editor watcher copies it to the requested testResults path and exits
with 0 only when every executed test passed and none were skipped. This file channel avoids a lost
PlayerConnection concealing results. It is scoped to Game.Tests.MapPreparation.Native, StandaloneOSX
and Build/MapVariantPreparedPlayer; other test runs keep their usual behavior. Full Editor/player logs,
NUnit results and all individual map/route markers are still required. Desktop profiler recorders remain
active; automatic profiler streaming is disabled for this fixture after Native03 exhausted its stream buffer.
