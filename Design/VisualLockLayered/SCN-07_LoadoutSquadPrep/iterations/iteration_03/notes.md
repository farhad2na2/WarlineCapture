# SCN-07 Loadout / Squad Prep — Runtime Iteration 3

Status: review candidate only. This iteration is not accepted until the user
explicitly approves it.

Target lock:

- `../../reference/SCN-07_LoadoutSquadPrepV3_Final_Target.png`

Runtime evidence:

- `loadout_squad_prep_v3_16x9.png` — 1920x1080
- `loadout_squad_prep_v3_20x9.png` — 4800x2160

Implemented corrections:

- added the previously missing `LoadoutSquadPrep` route surface and shell wiring
- rebuilt the composition from the 1672x941 target lock
- preserved every unit portrait aspect ratio beneath a crop mask
- used procedural directional gradients and independent constant-width borders
- created one reusable eight-sprite equipment source set for airstrike, medic
  drop, EMP, lock, armor, targeting, ammo, and repair
- packed those sprites once in `UI_V3_EquipmentIcons_01.spriteatlas`
- tightened source extraction and runtime bounds after Iteration 2 showed icons
  were undersized
- extended the runtime route-capture harness to support `LoadoutSquadPrep`

Validation evidence:

- `[LoadoutSquadPrepV3PrefabBuilder] validation=Passed gradients=26 images=92`
- `[LoadoutSquadPrepV3PrefabBuilder] result=Passed layout=1672x941 gradients=procedural borders=3 atlas=equipment-shared`
- `[CanvasRouteCaptureValidation] result=Passed ... route=LoadoutSquadPrep ... size=1920x1080`
- `[CanvasRouteCaptureValidation] result=Passed ... route=LoadoutSquadPrep ... size=4800x2160`

Review notes:

- 16:9 fills the frame without missing panels.
- 20:9 keeps the locked composition centered with neutral side margins.
- No unit art or equipment art stretches at either aspect ratio.
- Header, three middle panels, cards, and footer keep separate borders with no
  intersection or frame line cutting through another panel.

