# Portrait Card/Action Generation Workflow

## Goal

Generate secondary 512x512 UI portrait sprites for configs that already have a base `portraitSprite` assigned:

- `portraitCardSprite`: same-identity, calmer unit-card portrait with a creative background.
- `portraitActionSprite`: already generated for completed characters and earlier completed vehicles/buildings, but no longer required for remaining vehicle/building targets.

Skip configs with no existing base `portraitSprite`. Do not create secondary portraits for skipped base portraits until the base portrait is accepted and assigned.

## Output Style

- Final art is premium tactical game UI art at exactly `512x512`.
- The subject should still read like a portrait/cutout foreground subject, but now composited onto a creative high-quality background.
- Use baked contact shadows and scene shadows where appropriate.
- Backgrounds should be unique per config and match the subject identity: desert base, runway, checkpoint, hangar, refinery yard, supply camp, or similar tactical environment.
- Do not reuse the same background pattern across consecutive targets. Rotate background families such as market lane, aid tent, checkpoint, rooftop edge, hangar bay, runway apron, convoy road, refinery yard, barricade line, interior command area, supply depot, desert ridge, and repair bay.
- No transparent background, no chroma-key background, no UI frame, no text, no labels, no logos, no watermarks.

## Source Of Truth

- Existing `portraitSprite` and prefab reference plate define identity.
- Config display name and description define UI naming.
- For characters, `weaponSprite`, `weaponDisplayName`, and the prefab weapon model define the weapon. The generated weapon must match.
- For vehicles/buildings, the prefab model silhouette and colors define the identity.
- The accepted Heavy Gunner samples define quality and composition targets, not reusable identity.

## Output Paths

Save secondary portraits under:

`Assets/Game/Art/UI/Portraits/Secondary/`

File naming:

- `Portrait_<ConfigOrPrefabId>_Card_512.png`
- `Portrait_<ConfigOrPrefabId>_Action_512.png`

Examples:

- `Portrait_Unit_Chr_Soldier_Male_01_HeavyGunner_Card_512.png`
- `Portrait_Unit_Chr_Soldier_Male_01_HeavyGunner_Action_512.png`

## Config Fields

Assign outputs to:

- `UnitGridAuthoringConfig.portraitCardSprite`
- `BuildingDefinitionAuthoringConfig.portraitCardSprite`
- `portraitActionSprite` only for already completed targets that have an accepted Action image. For remaining buildings/vehicles, do not generate or assign new Action images.

Do not replace `portraitSprite`; it remains the transparent/cutout primary portrait.

## Character Rules

- Generate both a Card portrait and an Action portrait.
- Card portrait may use a standing, ready, patrol, or calm tactical pose.
- Action portrait should use a distinct pose family, not just another forward step. Use aiming, firing, kneeling, braced cover, reloading, radio call, dragging/carrying gear, commanding, piloting, low sprint, turning alert, vaulting, crouched inspection, or environmental interaction when it fits the character.
- Civilian characters should use non-combat action with variety: shielding from dust, helping someone off-frame, carrying a small bag or water container, crouching near cover, looking up at aircraft, reaching toward a checkpoint, turning away from danger, or moving through a market lane. Do not repeat the same running/stepping pose for every civilian.
- Do not use loaded prompt words that previously caused poster/infographic outputs. Describe concrete visual identity instead: clothing, headwear, gear, body type, weapon silhouette, color, and stance.
- Never use `insurgent` in any character image-generation prompt, including as a display-name copy or exclusion term. It repeatedly pushed the generator into unrelated infographic outputs.
- Avoid terms like `sidearm`, `SMG`, and structured labels such as `Use case` or `Asset type` when a target starts producing irrelevant layouts; use plain natural-language concrete descriptions instead.

## Vehicle Rules

- Card portrait should show the vehicle in a clean readable 3/4 showcase angle.
- Do not generate new Action portraits for remaining vehicle targets.
- Preserve full silhouette. Do not crop rotors, wings, launch rails, turrets, or cargo beds.
- Do not invent unrelated weapons.

## Building Rules

- Card portrait should show the building from a clean readable 3/4 angle.
- Do not generate new Action portraits for remaining building targets.
- For walls/barriers, use composition that makes the object readable as a gameplay asset.
- For airport/runway configs, preserve the intended configured subject and avoid unrelated aircraft clutter unless it is already part of the identity.

## Per Asset Checklist

1. Resolve config path.
2. Confirm the config has a base `portraitSprite`; if not, mark skipped.
3. Resolve prefab path/reference plate.
4. Resolve display name, description, and current base portrait path.
5. For characters, resolve `weaponDisplayName`, `weaponSprite`, and prefab weapon model.
6. Review the last three completed manifest entries and choose a different Card background family before prompting.
7. Generate Card portrait at 512x512-style square composition with a target-specific background.
8. For remaining buildings/vehicles, skip Action generation entirely.
9. Copy generated Card source into `Assets/Game/Art/UI/Portraits/Secondary/`.
10. Resize/crop final project PNGs to exactly `512x512`.
11. Set Unity import metadata to Sprite, no mipmaps, sRGB, readable off.
12. Assign `portraitCardSprite` in the config. Do not add or change `portraitActionSprite` for remaining buildings/vehicles.
13. Inspect the Card image against the prefab/base portrait.
14. Verify GUID assignment in the config YAML.
15. Update `Design/Architecture/portrait_card_action_generation_manifest.md` with a short note naming the accepted Card background direction.

## Atlas Step

After all non-skipped targets are complete:

1. Extend or run the portrait atlas builder so the secondary portraits are packed.
2. Create or update one `Portraits_Secondary.spriteatlas` under:
   `Assets/Game/Art/UI/Portraits/Atlases/`
3. Include the atlas in build.
4. Verify all secondary sprites are packable and assigned.

## Verification Gate

Do not mark an asset complete unless:

- Card PNG exists in the project.
- Card PNG is exactly `512x512`.
- Card PNG imports as Sprite.
- Card visually matches the config/prefab identity.
- For characters, the weapon matches the config weapon sprite and prefab weapon.
- The config has `portraitCardSprite` assigned to the correct GUID. Existing `portraitActionSprite` assignments on completed targets can remain.

## Accepted Reference

Completed sample:

- Card: `Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Chr_Soldier_Male_01_HeavyGunner_Card_512.png`
- Action: `Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Chr_Soldier_Male_01_HeavyGunner_Action_512.png`
