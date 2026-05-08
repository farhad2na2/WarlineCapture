# ISO-02 2D Isometric Runtime Prototype Report

Status: REVIEW

Technical runtime status: PASS. Visual lock status: REVIEW.

Reason: movement, camera, sorting, overlays, and capture generation pass, but the current ISO-02 map is not visually approved. Road connections and overall composition still need cleanup before this can be treated as a high-quality gameplay target.

## Scope

- Manual design/balancing prototype only; this is not wired into Jenkins or build validation.
- Builds a separate runtime scene using the generated 2D isometric golden assets and Tilemap terrain.
- Validates runtime-style depth sorting, basic unit movement, and overlay followers before integration into the real gameplay scene.

## Output Paths

- Scene: `Assets/Game/Scenes/DesignTargets/ISO02_CityCommand_RuntimePrototype.unity`
- Start capture: `Design/VisualReferences/2DIsometricProduction/RuntimePrototype/ISO02_RuntimePrototype_Start.png`
- Mid capture: `Design/VisualReferences/2DIsometricProduction/RuntimePrototype/ISO02_RuntimePrototype_Mid.png`
- End capture: `Design/VisualReferences/2DIsometricProduction/RuntimePrototype/ISO02_RuntimePrototype_End.png`
- Report: `Design/VisualReferences/2DIsometricProduction/RuntimePrototype/ISO02_RuntimePrototype_Report.md`

## Checks

- Runtime movement: PASS - 3 prototype agents move between isometric waypoints.
- Runtime sorting: PASS - moving agents recalculate SpriteRenderer sorting order during movement.
- Overlay followers: PASS - maximum target/follower offset error 0.0000 world units.
- Gameplay camera: PASS - scene includes `ISO02 Gameplay Camera` tagged `MainCamera`, orthographic size 3.45, with Play Mode pan/zoom controls.
- Scale/readability: PASS - minimum key sprite screen height 60.3 px.
- Capture output: PASS - start/mid/end captures are 1920x1080.
- Performance smoke: 187 terrain tiles, 24 SpriteRenderer objects, 13 overlay or command marker sprites.
- Visual composition: REVIEW - roads are not yet cleanly connected and the map does not yet match the high-quality target mockup.

## Agent Snapshots

- Runtime Rifle Squad - Road Advance: start (-1.50, -1.25, 0.00) order 1125, mid (-0.50, -0.39, 0.00) order 1039, end (0.00, 0.00, 0.00) order 1000.
- Runtime APC Patrol - Convoy Lane: start (-2.00, -1.00, 0.00) order 1100, mid (-0.28, -0.14, 0.00) order 1014, end (1.00, 0.50, 0.00) order 950.
- Runtime Tank Push - Breach Lane: start (-2.00, -0.50, 0.00) order 1050, mid (0.09, 0.18, 0.00) order 982, end (1.50, 0.75, 0.00) order 925.

## Timings

- Import/reimport: 5063.4 ms
- Scene build: 912.8 ms
- Capture renders: 2292.5 ms
- Total editor method: 8897.8 ms

## Notes

- The scene is intentionally isolated from `Assets/Game/Scenes/Game.unity`.
- Runtime components are lightweight MonoBehaviours under `Assets/Game/Scripts/Iso2D` and can be reused later by the real tactical gameplay implementation.
- In Play Mode, use arrow keys/WASD to pan and mouse wheel to inspect zoom levels on the prototype camera.
- The prototype captures validate movement states in editor automation; visual approval is still pending and should not be inferred from the technical PASS checks.
