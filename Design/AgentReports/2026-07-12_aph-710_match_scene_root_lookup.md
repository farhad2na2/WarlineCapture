# APH-710 Match Scene Root Lookup

Date: 2026-07-12

Baseline: `ca663b6ef`

Status: Stable partial slice; APH-710 remains active

## Result

`MatchSceneReferenceSceneSystemHelper.TryGetLoadedSceneView` used
`Scene.GetRootGameObjects()` twice per Match frame through static-map
presentation and runtime UI binding. The array-returning API produced two
38,272-byte rows across 299 measured frames.

The existing helper now owns a capacity-stable `List<GameObject>` and calls the
non-allocating `Scene.GetRootGameObjects(List<GameObject>)` overload. The list
is cleared before every validity check, so unloaded scenes release root
references immediately. The implementation deliberately does not cache the
resolved `MatchSceneView`; unload, reload, scene replacement, and same-scene
view replacement remain observable on the next call.

No scene event subscription, static state, public API, caller, serialized type,
static-map behavior, UI binding rule, or scene lifecycle ordering changed. The
frozen `*SystemHelper` shrank from 63 to 48 lines and remains below its source
growth ceiling.

## Evidence

- Focused scene-reference validation passes `5/5`.
- Twelve-root authored-scene parity is covered.
- After one warm lookup, 300 repeated lookups allocate zero current-thread
  managed bytes.
- Unloaded-scene, replacement-scene, and same-scene view replacement behavior
  are covered.
- Production Menu-to-Match-to-Menu lifecycle PlayMode validation passes `1/1`.
- The fresh 180-warmup/300-frame raw capture contains neither scene-reference
  allocation row.
- Previously removed audio and tactical-camera rows remain absent.
- Player-relevant Match GC improves from 235,496 to 167,712 bytes in the final
  exact-source capture. The unchanged 1,024-byte global budget remains red.

## Validation

- `MatchSceneReferenceFocusedValidation`: passed `5/5`.
- APH-805 Menu-to-Match-to-Menu lifecycle PlayMode: passed `1/1` in 9.49 s.
- `ProductionSourceGrowthArchitectureValidation`: passed `15/15`.
- `ScriptArchitectureBoundaryValidation`: passed `31/31`.
- `Game.Composition.csproj` and `Game.Tests.Editor.csproj`: zero compiler
  errors.
- `git diff --check`: passed.

## Residual Risk

Root scanning still occurs twice per Match frame. At the authored 12-root size
it is not a measured CPU concern; adding result caching would introduce scene
lifecycle risk without evidence. A future scene exceeding list capacity can
allocate once on first discovery, after which capacity is retained.

APH-710 remains active for transport helper construction, UI shell attribution,
road/building command queries, pathfinding diagnostics, intermittent selection
panel allocations, and hidden default-world lookups.
