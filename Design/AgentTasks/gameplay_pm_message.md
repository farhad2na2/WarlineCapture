# PM Message For Gameplay

Date: 2026-05-08

The user approved the `Game_Legecy` scene-isolation fix. Resume the selected-readability/ECS visual rejection gate now.

Use these delivered inputs:

- `Design/AgentReports/2026-05-08_art-atlas_m01-marker-animation-scale-package.md`
- `Design/AgentReports/2026-05-08_designer_m01-rejection-scale-marker-contract.md`
- `Design/AgentReports/2026-05-08_ui_m01-marker-selection-overlay-audit.md`

This is a P0 process failure, not polish. The important correction is that public M01 visible units/buildings must be ECS entity visuals. The user saw `MeshRenderer` objects and rejected that. Existing validation that only blocks `SpriteRenderer` is not enough.

Fix and prove these items:

- no visible unit/building presentation through scene/runtime `MeshRenderer`, `MeshFilter`, or `SpriteRenderer` GameObjects,
- no `M01RuntimeEcsAtlasQuads` style GameObject renderer wrapper as the accepted visible path,
- small target marker, about two soldier footsteps wide,
- idle and moving soldiers animate correctly,
- no crouched/sitting run frames, no stray foot artifact,
- no vertical squash and current visual scale near the user's readable `0.15` target unless Art/Atlas gives a verified replacement,
- red flashing sitting object/enemy identified and fixed,
- selection works on the soldier/body/formation, not only foot pixels,
- placeholder yellow marker is replaced or explicitly blocked on Art/Atlas.

Expected report:

`Design/AgentReports/2026-05-08_gameplay_m01-ecs-visual-marker-animation-reset.md`

Do not commit or push.
