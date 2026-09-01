# POP-03 Build Placement

Current status: Iteration 3 is review-frozen; explicit user acceptance is pending.

Canonical V3 target locks:

- `reference/POP-03_BuildPlacementV3_Final_Target.png`
- `reference/POP-03_BuildPlacementV3_MetadataValidity_Final_Target.png`

Current evidence:

- `iterations/iteration_03/`

The placement UI is split into two reusable runtime prefabs:

- `Assets/Game/Prefabs/UI/Shell/Content/SCN08_BuildPlacementConfirmationBar.prefab`
- `Assets/Game/Prefabs/UI/Popups/BuildPlacementPanel.prefab`

The first prefab owns the full-width building summary and real Rotate, Cancel,
and Place Building controls. The second is the reusable top-right footprint
validity/minimap state. The live placement command decides which state is shown;
invalid placement disables Place Building, replaces ARIA with the validity panel,
and shows the blocker reason. Closing placement restores the hidden Match HUD
surfaces.

Existing building imagery, the existing Match minimap, and shared V3 icons are
reused. No screen-local duplicate building, unit, minimap, or icon art was added.
Chrome is procedural and all nonzero frame borders use the 3 px V3 contract.
