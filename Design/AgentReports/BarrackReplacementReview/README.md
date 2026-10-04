# Barrack replacement visual review

2026-10-04. User requested solid Demo 2 alternatives and a choice before replacement.

## Review images

- Barrack-Candidates.png: four shortlisted original native prefabs, unchanged proportions/materials.
- Barrack-Footprints.png: current Barrack plus shortlist and warehouse alternate at identical camera scale; yellow boundary is the current 28 × 15 footprint on the existing 1 m grid.
- Full-Source-Gallery.png: all eight inspected prefabs, including the small utility buildings.
- Native/: full-size angle and top renders.

A is SM_Bld_House_03, B is SM_Bld_House_02, C is SM_Bld_House_01, D is SM_Bld_SmallBuilding_03. These sources occur in Assets/Game/Scenes/Demo2.unity, verified against the existing resolved Demo 2 prefab inventory. All candidate prefabs, the Demo 2 scene and the current Barrack were byte-identical between the main checkout and the connected preview checkout before review. Source hash records confirm prefab parity after capture.

## Footprint considerations

The existing Building_Barrack has 28 × 15 authored footprint cells and renderer bounds 27.10 × 14.86 horizontal units, height 10.28. measured bounds include roof, steps and other source visual details. See measurements.json for full precision and source GUIDs.

- A: rotate the native block 90° to align its 10.61 × 5.62 length across the current footprint. Suggested next preview: two linked wings with a training yard; preserve human-scale doors rather than enlarging 2.64 times to fill the envelope.
- B: 13.38 × 12.48, height 7.20. Suggested next preview: one substantial block and side courtyard or subordinate service structure.
- C: 10.97 × 10.44, height 7.59. Suggested next preview: main block with a service annex or yard.
- D: 10.70 × 7.10, height 3.89. Suggested next preview: multiple low wings with a connecting entrance and yard.
- Warehouse alternate: approximately 22.61 × 21.72 after rotation, height 9.16. Too deep at native scale; approximately 0.69 uniform scale fits the original envelope but reduces door size and makes the identity more industrial.
- SmallBuilding_01/02: about 5.1/5.4 × 3.4/3.2 after rotation. Appropriate supporting offices, weaker primary Barrack silhouettes.

These layouts are proposals, not implemented or collision-certified. After selection, a project-owned visual wrapper should retain the current Barrack gameplay identity, footprint, production, selection, ownership and spawn paths. Keep the vendor prefab unmodified. Validate the selected compound's clear entrances, destruction state and bounds before replacing the shared production asset.

## Evidence and limits

Native capture ran through the mandatory GUI Unity wrapper using the connected T7 Editor in Edit mode. Temporary preview scenes were closed after capture; the active Menu scene and its dirty state were preserved. No production prefab, scene or config was saved by this review.

Logs/capture-01 is retained as a failed attempt while Editor compilation/import was settling. Capture-02 passed; framing was then improved. Capture-03 exited 0 with both [BarrackCandidateReview] result=Passed prefabs=8 views=16 and [ExistingEditorValidation] result=Passed. Camera target cleanup emitted harmless temporary RenderTexture release warnings; all source images were written and temporary preview scenes closed.

This is source-art selection evidence. No replacement, gameplay validation, full mission playthrough or device acceptance is claimed. User selection remains pending.
