# SCN-02 Commander Scene Variant Contract

Each Main Menu commander uses one cohesive baked scene containing that
commander, their environment, tactical table, contact shadows, lighting, and
occlusion. Separating the character from the table produced a visible pasted-on
edge and is no longer the runtime strategy.

Live UI remains independent: logo, resource panels, ARIA, mode cards, labels,
borders, gradients, telemetry, buttons, and icons must never be baked into a
commander scene.

## Current roster

| Stable UI ID | Gameplay source | Runtime scene |
| --- | --- | --- |
| `field_commander_01` | `Unit_Chr_Leader_Male_01` | `SCN02_FieldCommander_01_Scene_V3.png` |

The six selectable First Launch faces in `commander_portrait_choices.png` are
player-profile portraits, not gameplay commander definitions. They must not be
silently reused as Main Menu commander variants.

## Asset and binding rule

For each new gameplay commander:

1. Author one complete `SCN02_<StableName>_Scene_V3.png` under
   `Assets/Game/Art/UI/V3Shared/CommanderScenes/` at the 1672x941 reference
   composition.
2. Bake the commander, environment, tactical table, contact shadows, scene
   lighting, and all commander-specific foreground occlusion together.
3. Keep every live UI element out of the scene plate. The plate must contain no
   text, logo, panel, border, ARIA, mode-card UI, resource UI, footer, or icon.
4. Register the scene once against its stable commander ID in
   `MainMenuCommanderVariantView` through `MainMenuV3PrefabBuilder`. Never
   select by array position or display name.
5. Keep each full-screen commander scene standalone. Do not put full-screen
   scenes in a sprite atlas and do not create a duplicate transparent commander
   cutout or commander-free runtime plate for the same variant.
6. Capture a new immutable Main Menu iteration at 16:9 and 20:9. Check hair and
   shoulder lighting, both hands, table contact, hologram overlap, scene depth,
   and all live UI safe areas against the target.

The intentional per-commander scene variation is limited to the large scene
plate. Small reusable art remains deduplicated in shared logical atlases.
