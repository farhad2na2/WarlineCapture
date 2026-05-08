# ISO-01 2D Isometric Tilemap Spike Report

Status: PASS

## Scope

- Manual design/balancing spike only; this is not wired into Jenkins or build validation.
- Imports the first 23 golden assets as Unity sprites.
- Builds an isometric Tilemap scene with modular road/plaza terrain, tactical overlays, and captures a 1920x1080 visual check.

## Output Paths

- Scene: `Assets/Game/Scenes/DesignTargets/ISO01_CityCommand_TilemapSpike.unity`
- Capture: `Design/VisualReferences/2DIsometricProduction/UnitySpike/ISO01_TilemapSpike_Capture.png`
- Golden assets: `Assets/Game/Art/Generated/2DISO/GoldenAssets`
- Tile assets: `Assets/Game/Art/Generated/2DISO/Tiles`

## Checks

- Sorting: PASS - lower screen-space units render in front of higher units.
- Scale/readability: PASS - minimum key sprite screen height 38.4 px.
- Modular terrain: PASS - includes straight road, intersection, turn, T-junction, end cap, curb transition, alternate plaza, and damaged road overlays.
- Tactical overlays: PASS - selection rings, move/attack markers, health bars, squad badge, and capture point marker placed as separate sprites (11 overlay instances, minimum overlay height 12.1 px).
- Performance smoke: 221 terrain tiles, 27 composed sprites, 32 extra repeated sprites.

## Timings

- Import/reimport: 1496.3 ms
- Scene build: 56.6 ms
- Capture render: 371.2 ms
- Total editor method: 2228.9 ms

## Notes

- Road and concrete assets are imported as Tile assets to verify the isometric Tilemap path.
- Damaged road overlays remain sorted SpriteRenderer objects because decals may need independent placement and gameplay state later.
- Selection, command, health, squad, and capture overlays remain separate sorted SpriteRenderer objects and are not baked into unit, terrain, or building art.
- Buildings, barricades, squads, APCs, and tanks remain sorted SpriteRenderer objects because they need per-object depth ordering and selection logic later.
- This spike validates the asset direction and Unity setup before generating the remaining game-wide asset library.
