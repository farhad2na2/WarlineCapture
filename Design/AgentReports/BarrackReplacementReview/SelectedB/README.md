# Selected B — native compound preview

User selected option B (SM_Bld_House_02) on 2026-10-04.

The isolated candidate was authored through Unity PrefabUtility, with linked vendor-prefab children and project-owned materials. All five vendor sources retain native scale. The candidate contains a masonry Barrack, a small service office, a generator, loaded supply pallet and medical crate on a shallow foundation and marked muster/service yard. It is a visual-only preview with no gameplay authoring and no physics colliders. It does not change production, navigation, spawn behavior or ownership.

Candidate location:
`/Volumes/T7/Projects/WarlineCapture-MenuUiUx/Assets/Game/Prefabs/Buildings/Candidates/BarrackB/Building_Barrack_B_Masonry_Visual_Preview.prefab`

The shared production Building_Barrack.prefab dependency hash remained unchanged. The active Menu scene and its dirty state were preserved. Foundation bounds are 27.6 × 14.6, leaving a margin inside the existing 28 × 15 grid footprint; the native B shell measures 13.38 × 12.48, height 7.20. All candidate renderer bounds passed the original footprint envelope check.

Native capture through invoke_unity_macos.sh --reuse exited 0 with:
- [BarrackBCompoundPreview] result=Passed footprint=28x15 sources=Linked productionBarrack=Unchanged activeScene=Preserved
- [ExistingEditorValidation] result=Passed executeMethod=Game.Editor.BarrackBCompoundPreview.Run

Angle, reverse-angle and top renders are saved alongside the manifest and full logs. Top view shows the 28 × 15 footprint in yellow; that outline and the surrounding ground belong only to the capture scene, not the saved candidate. The temporary capture utility was archived here and removed from Assets after the render.

Candidate composition adds renderers compared with the original Barrack. Art/layout review remains pending. Production replacement, a matching destruction visual, ownership/bake propagation, normal-input interaction and unit production/exit, and mobile performance acceptance have not been performed. No commit, push or merge was requested for this selection.
