# APH-710 Audio Gameplay-State Query Cache

Date: 2026-07-12

Baseline: `1616ef8c4`

Status: Stable partial slice; APH-710 remains active

## Result

`AudioPlaybackPresentationBridgeSystemHelper.IsGameplaySimulationActive`
created and disposed an ECS query on every audio presentation update. APH-007
and APH-213 measured this owner at 11,960 bytes across 299 of 299 frames.

The bridge now delegates the read to a lifecycle-bound
`AudioGameplayStateQueryCache`. The cache binds once per ECS world, rebinds
when the world changes, and is disposed by the existing
`AudioPlaybackPresentationRuntimeView` ownership boundary. No ECS system,
`SystemBase`, public command contract, audio event rule, mixer behavior, or
serialized field was added or changed.

The frozen bridge helper remains at its accepted 304-line ceiling. Query-cache
ownership lives in a separate 45-line production file, so the source-growth
ratchet remains intact.

## Evidence

- Focused bridge validation passes `8/8`.
- A warmed bridge performs 300 empty drains with zero current-thread managed
  allocation.
- The same bridge correctly culls gameplay voice in an inactive first world,
  then plays it after rebinding to an active second world.
- The fresh 180-warmup/300-frame raw capture contains no
  `AudioPlaybackPresentationBridgeSystemHelper.IsGameplaySimulationActive`
  row.
- Both previously removed tactical-camera allocation rows remain absent.
- Player-relevant Match GC improved from the APH-709 capture's 266,974 bytes
  to 235,496 bytes. The unchanged 1,024-byte global gate remains red because
  scene-root, transport, UI shell, road/building command-query, pathfinding,
  and intermittent selection-panel owners remain.

## Validation

- `AudioPlaybackPresentationBridgeValidation`: passed `8/8`.
- `ProductionSourceGrowthArchitectureValidation`: passed `15/15`.
- `ScriptArchitectureBoundaryValidation`: passed `31/31`.
- `EcsBurstHotPathArchitectureValidation`: passed `10/10`.
- `Game.Runtime.csproj` and `Game.Tests.Editor.csproj`: zero compiler errors.
- `git diff --check`: passed.

## Next Slice

Continue APH-710 with another isolated recurring owner. The two per-frame scene
root scans are the largest current pair, but their lifecycle cache must preserve
Menu-to-Match loading, static-map presentation, UI binding, and unload behavior.
Command-query owners are smaller independent alternatives if that lifecycle
seam is not yet safe.
