# Map beautify — visual direction and handoff for all maps

The pilot is Supply Line on Refinery District; see `SupplyLine/HANDOFF.md`. This file covers the other three maps and the tuning still needed on the pilot. The pipeline itself (`MapVariantBeautify`, `MapVariantAtmosphere`, `MapVariantBeautifyPipeline`) is shared, and each map is enabled through `MapVariantBeautify.Supports(map)`.

## Pilot result (Refinery / Supply Line)
- `RebuildRefinery` **passed**:
  - `semanticHash` is unchanged (`2d4fd91a…`).
  - 4 mission definitions found, 62 clearance circles, 3522 slabs, 132 effects.
  - Log: `/private/tmp/beautify-rebuild-02.log`.
- Captures are in `SupplyLine/before-*.png` and `SupplyLine/after-*.png`.
- **Visual review: not yet matching the mockup.** Tune before rolling out:
  1. The pads read beige and sandy. Lower the sandy-slab probability (0.55 → about 0.15) and pull concrete UVs from the grey part of the concrete swatch.
  2. The grout lines read as a dark tile grid. Use a grout colour close to the slab, or a thinner grout, and fewer full-width lines.
  3. The oil stains are too large and black. Shrink them, lower their density and use dark brown instead of black.
  4. The dust effects render as white glare blobs. Use alpha blending (not additive) for dust and lower `_BaseColor` alpha; check bloom.
  5. The regenerated scene and binding are uncommitted. Commit them only after the visuals pass review.
- Pending gates: normal-input playthrough and device/mobile performance.

## User decisions (2026-10-03)
- **City-Edge Airfield:** mockup approved.
- **Ash Line Port:** look approved, **keep the current container layout**. Render-only items only; do not regroup the containers.
- **Frontier:** approved **including the container regrouping** into blocks with lanes. No missions use Frontier yet.
- **Dense city:** street and outskirts mockups both approved. Render-only changes only.

## Map directions

| Map | Missions | Mockup |
|---|---|---|
| City-Edge Airfield | CH01M04 Airlift, CH04M01 Air Corridor | `CityEdgeAirfield/airfield-beautify-mockup-v01.jpg` |
| Ash Line Port | CH02M05 Port | `AshLinePort/ashlineport-beautify-mockup-v01.jpg` |
| Frontier | none yet | `Frontier/frontier-beautify-mockup-v01.jpg` |

**City-Edge Airfield.** These are all render-only, so the hash is safe:
- grey apron slabs with skid marks and oil stains;
- yellow taxi lines and outlines around the helipad H markings;
- sand drift on apron edges, sandy patches with grass tufts, worn tracks;
- sandbag and crate vignettes at the edges;
- palms along the fence, a windsock;
- a burning helicopter wreck in the off-play corner;
- warm low sun.

The central apron must stay open.

**Ash Line Port.**
- Render-only items:
  - weathered asphalt with white bay lines and yellow lane lines;
  - oil stains, puddles and tyre marks;
  - a gantry crane and forklifts on blocked or off-play cells;
  - bollards and boats at the quay, a burning-container vignette with smoke, palms on the sand.
- **The mockup also regroups the containers into blocks with wide lanes.** That is a gameplay layout change: it moves the prepared semantic hash and the movement grid. Check how CH02M05 pins its source before treating it as safe; if it pins the prepared hash like the Refinery missions do, it needs rebinding and revalidation. Get explicit user approval for the layout change before doing it; otherwise ship only the render-only items.

**Frontier.**
- Same treatment as the port: concrete slabs, lane markings, a sandbag checkpoint, canal quay dressing, a burning stack.
- The mockup's main fix is breaking the endless container field into blocks with lanes. That is a layout change too, but Frontier has no missions yet, so it is cheap to do now.

## Dense city map (`opmap.skirmish.desert_base_01`, 18 missions in Ch01–Ch04)
- **Scenes:**
  - prepared: `Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity` (225 MB);
  - binding: `Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity`.
- **Captures:** `MapVariantBeautifyPipeline.CaptureDenseCityBefore` renders every mission's battle camera to `DenseCity/before-*.png`; it passed for 18 missions.
  - Several Ch01 definitions share an internal object name, so their files overwrote each other. Name the files by asset file name.
  - Some missions share the same battle camera (Old Market and Signal Trace are identical).
- **Unconfirmed lighting:** the captures render dark and blue-grey. The binding probably lacks the sun and ambient lighting the game applies. Confirm against a real in-game screenshot before judging lighting.
- **Clear regardless of lighting:** flat olive ground, stamped grey blobs, featureless grey roads and sparse outskirts dressing.
- **Mockups:**
  - `DenseCity/densecity-street-beautify-mockup-v01.jpg` (Ch01 District Edge street): worn asphalt with lane paint, paved kerbs, dusty earth with rubble and weeds, a market awning with produce, laundry and wires, a sandbag barricade, wrecks, a smoking alley vignette, warm low sun.
  - `DenseCity/densecity-outskirts-beautify-mockup-v01.jpg` (shared outskirts highway and compound): two-tone sand with ripples and scrub, rock outcrops instead of grey blobs, tyre tracks to the compound, compound walls and a water tank, cracked highway with sand drift and a guard rail, a burning wreck vignette.
- **Mission safety:** this map backs 18 missions. Keep every change render-only (`RecordPlacements = false`, paint after classification) and verify that each mission's pinned source hashes are unchanged.

## How to extend to a map
1. After approval, generalise `MapVariantBeautify.Supports` and the per-map palettes and densities. Mission clearance is already derived from every `OperationMapDefinition` whose source is the prepared map.
2. Add `Rebuild<Map>` / `Capture` views to `MapVariantBeautifyPipeline`, using each mission's own camera focus and offsets.
3. Pin and check each map's current prepared `semanticHash` the same way `RebuildRefinery` does.
4. Use only the wrapper (`Tools/CI/invoke_unity_macos.sh`, `--reuse` for an open worktree Editor); never `-batchmode`. Report the visual, automated, playthrough and device gates separately.
