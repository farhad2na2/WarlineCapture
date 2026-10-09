# Campaign map visual improvement plan

Owner request: 2026-10-09. Document the review findings, implement improvements on existing maps, and commit, push, merge to main, and pull the main checkout after every validated step.

## Scope and visual direction

Improve the eleven existing Campaign physical sources and their shared consumers. Preserve mission bindings, budgets, playable bounds, functional building footprints, navigation, collision, and objective/spawn data. This is explicitly requested environment art work, not a request for new Skirmish or Operations maps. Follow `Design/MapVariants/CAMPAIGN_MAP_REUSE_POLICY.md`.

The intended look is a lived-in desert city with purposeful military and industrial districts. Use quieter ground variation, connected surfaces, coherent prop groups, and restrained environmental detail. Keep tactical silhouettes and movement lanes readable at the normal mobile camera height.

## Review findings

| Priority | Observed issue | Planned treatment |
| --- | --- | --- |
| 1 | Sculpted dirt strips form raised, folded edges; runway/apron seams and shoulders look unfinished. Some decorative poles appear in streets. | Inspect native mesh bounds and ownership. Flatten inappropriate decorative road relief while preserving road footprints/colliders; align surface treatment and shoulders. Relocate only verified decorative intrusions. |
| 2 | Many buildings sit on separate rectangular paving patches, like mats. | Connect suitable adjacent courtyards/frontages; use irregular worn edges and sand accumulation. Retain roads, entrances, and tactical clearances. |
| 3 | Large concrete areas are empty; wrecks, barrels, rubble and buildings repeat in regular rows. | Create small loading, repair, market, and service groupings using existing art. Add visual purpose near structures; avoid filling routes or increasing gameplay obstruction. |
| 4 | Districts share similar facades, rooftops and small props. | Controlled material and rooftop/frontage variation within existing blocks: residential, market, administrative, military and industrial. |
| 5 | Large yellow/green soil blotches compete with the scene. | Reduce macro color contrast; strengthen local gravel, compacted dirt and building-edge transitions. |
| 6 | Rocks and vegetation look evenly scattered; rock bases are poorly integrated. | Review actual meshes and placement; improve rock material variation, base gravel and sheltered vegetation groupings without changing traversable land. |
| 7 | Visible cliffs have simple uniform faceting. Distant mountains were not yet reviewed closely. | Inspect before changing. Add restrained strata/crevice/base variation to existing rock surfaces; retain the stylized silhouettes. |
| 8 | Dense-city water is a bright blue strip; port shorelines are uniform. | Inspect water materials and refine palette/surface detail and existing shore transitions. Add dock detail only where legal space exists. |
| 9 | Some detail captures have a strong yellow wash or lens flare. | Check authored gameplay lighting before tuning. Preserve readable units and shadows; do not infer gameplay exposure from inspection overviews. |

### Map-specific emphasis

- CityEdgeAirfield / Grounded Signal: runway edges, apron continuity, hangar servicing and parking details.
- RefineryDistrict / Armor Break: loading yards, maintenance areas, warehouse entrances and industrial palette.
- AshLinePort / Trust Under Fire: container palette, quay surfaces, loading markings, dock/water relationship.
- Shared dense city / Chapter 5 city maps: connected courtyards, neighborhood identity and compound entrances.
- Rocky edges: existing rock silhouette, base integration, gravel and restrained planting.

Overviews include land outside playable bounds. Empty areas are not automatically missing playable decoration. Potential aircraft overlap in the military yard must be distinguished from destroyed/runtime-only visuals before being treated as a placement defect.

## Publication steps

1. **Surface foundation and documented plan.** Publish the already reviewed ten-map ground/paving pass and this plan. Keep original failed captures and validation evidence. Synchronize with the other agent's committed work without publishing its old T7 snapshot.
2. **Road and connected-ground pass.** Inspect and fix decorative road relief/seams; improve connected paving and local ground transitions. Validate saved native assets and compare matched before/after captures.
3. **City and military/industrial detail pass.** Introduce restrained, purposeful decorative groups and material variation. Check building entrances, road clearance, collision ownership and mobile draw/mesh cost.
4. **Natural and waterfront finish.** Inspect rocks/cliffs/mountains, vegetation and water. Apply supported surface/base improvements and only evidence-backed lighting adjustments. Capture the completed map set and update this record.

Each step gets a scoped commit/PR, push, merge to main and a fast-forward pull into the main checkout before the next step. Preserve unrelated dirty/untracked files with hashes and backups where needed. Do not use broad staging or overwrite another agent's work. Add publication links and results below as each step completes.

## Validation and acceptance

- Use Unity's native Editor APIs for assets/scenes and the repository macOS GUI wrapper for all executeMethod work and captures. Keep Hub and unrelated Editors running.
- Capture consistent native before/after views with actual authored lighting. CH05 bindings without a sun require the Match scene's lighting; retain original unlit evidence separately. Inspection overviews disable distance fog/post-processing, so they are not final gameplay lighting acceptance.
- Assert preserved mission/navigation/collision/functional-building data. Decorative changes must not alter gameplay ownership or routes. Check material/shader errors, missing meshes, invalid bounds and saved references.
- Review close views and overviews; document concrete measured changes and unresolved findings. Keep failed runs with their original logs.
- Visual review, automated validation, full normal-input mission playthroughs, and Android/player acceptance are separate gates. This environment art pass does not certify all 25 Campaign missions or their reused Skirmish/Operations consumers.

## Progress

- Earlier shared desert/dense-city road and district surface foundation: PR [#58](https://github.com/farhad2na2/WarlineCapture/pull/58), merged as `024768787b6a05e590bd2cffa9ca59c8985fb6bd` and pulled to main.
- Ten-map surface foundation: native detail review complete; saved scene/material validation passed for 10 scenes and 50 materials. Scene differences are material references only. Full normal-input mission and Android acceptance remain pending. Evidence: `../OtherMapSurfacePolish20261009/validation-summary.json`.
- New improvement steps: in progress. Record actual results here as work is completed; recommendations above are not claims of implementation.

### Step 1 published

- Commit `4213848753926368a750cce82a59cfd32bb00258`; PR [#59](https://github.com/farhad2na2/WarlineCapture/pull/59); main merge `7bba4c19748f9b52da6712a312511b5c1ffb9855`.
- Main checkout fast-forwarded to the merge. Both unrelated dirty tracked files were hash-verified unchanged; all 7,112 unrelated untracked paths remained present.
- A fresh validation dispatch did not run because T7 Pipeline could not bind its configured port 7801, already used by main. Original passing asset validation remains valid and all ten saved scene hashes match it. The owner explicitly approved restarting only T7. T7 was gracefully restarted through the repository wrapper, connected on port 7802, and the fresh foundation revalidation passed (`warline-map-foundation-revalidate-02.log`, exit 0). Main and other Editors were left running.

### Step 2 validated

- Lowered inappropriate dirt-strip relief on 1,989 pieces, rebuilt eight runway sections with continuous straight surfaces and aligned markings. Preserved exact XZ footprints, authored ramps, transforms, collision and mission data. Road triangle count fell from 336,056 to 334,680.
- Connected small gaps between existing paved frontages while protecting road coverage, reduced yellow-green ground contrast, and added sparse concrete fractures and worn limestone sets. No extra scene objects.
- Eleven native source/capture checks and the connected-ground checks passed. See `step2-validation.json` and `step2-gallery.html`. Full normal-input missions and Android acceptance remain pending.

- Step 2 published: commit `f1ec95e833b49fabefc9855ea0cae7fbd6af226e`, PR [#60](https://github.com/farhad2na2/WarlineCapture/pull/60), main merge `ce537469aeaa6b9d58bc2a07b449d5e341d6980e`. Main fast-forwarded; ten unrelated dirty files hash-verified unchanged and all 7,170 untracked paths preserved.

### Step 3 validated

- City and industrial detail: 193 groups, 2,701 buildingRenderers, 12,055 variedContainers, 606 movedProps.
- All eleven native scene checks passed. Renderers and triangles unchanged. Recorded decorative transforms and material references are the only source changes; collision, gameplay data, building placements and authored lighting retained.
- Evidence: `step3-validation.json`, native receipts, full wrapper logs and `step3-gallery.html`. Normal-input mission and Android acceptance remain pending.
