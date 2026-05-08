# PM Message For Gameplay

Date: 2026-05-08

The user rejected the selected-readability pass. This is a P0 process failure, not polish.

Do not continue broad scene cleanup or M02 work until the current rejection gate is resolved. `Game_Legecy` was reported separately; pause it unless PM/user explicitly resumes.

The most important correction: public M01 visible units/buildings must be ECS entity visuals. The user saw `MeshRenderer` objects and rejected that. Existing validation that only blocks `SpriteRenderer` is not enough.

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
