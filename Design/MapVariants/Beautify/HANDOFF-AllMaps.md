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

## Map directions (mockups awaiting user approval)

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

## How to extend to a map
1. After approval, generalise `MapVariantBeautify.Supports` and the per-map palettes and densities. Mission clearance is already derived from every `OperationMapDefinition` whose source is the prepared map.
2. Add `Rebuild<Map>` / `Capture` views to `MapVariantBeautifyPipeline`, using each mission's own camera focus and offsets.
3. Pin and check each map's current prepared `semanticHash` the same way `RebuildRefinery` does.
4. Use only the wrapper (`Tools/CI/invoke_unity_macos.sh`, `--reuse` for an open worktree Editor); never `-batchmode`. Report the visual, automated, playthrough and device gates separately.
