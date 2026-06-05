# Portrait Card/Action Generation Workflow

## Goal

Generate two additional 512x512 UI portrait sprites for every character, vehicle, and building that already has a base `portraitSprite` assigned:

- `portraitCardSprite`: same-identity, calmer unit-card portrait with a creative background.
- `portraitActionSprite`: more dynamic pose/composition for panels that need energy or drama.

Skip configs with no existing base `portraitSprite`. Do not create Card/Action portraits for skipped base portraits until the base portrait is accepted and assigned.

## Output Style

- Final art is premium tactical game UI art at exactly `512x512`.
- The subject should still read like a portrait/cutout foreground subject, but now composited onto a creative high-quality background.
- Use baked contact shadows and scene shadows where appropriate.
- Backgrounds should be unique per config and match the subject identity: desert base, runway, checkpoint, hangar, refinery yard, supply camp, or similar tactical environment.
- No transparent background, no chroma-key background, no UI frame, no text, no labels, no logos, no watermarks.

## Source Of Truth

- Existing `portraitSprite` and prefab reference plate define identity.
- Config display name and description define UI naming.
- For characters, `weaponSprite`, `weaponDisplayName`, and the prefab weapon model define the weapon. The generated weapon must match.
- For vehicles/buildings, the prefab model silhouette and colors define the identity.
- The accepted Heavy Gunner samples define quality and composition targets, not reusable identity.

## Output Paths

Save Card/Action portraits under:

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
- `UnitGridAuthoringConfig.portraitActionSprite`
- `BuildingDefinitionAuthoringConfig.portraitCardSprite`
- `BuildingDefinitionAuthoringConfig.portraitActionSprite`

Do not replace `portraitSprite`; it remains the transparent/cutout primary portrait.

## Character Rules

- Generate both a Card portrait and an Action portrait.
- Card portrait may use a standing, ready, patrol, or calm tactical pose.
- Action portrait should use a distinct pose: aiming, firing, kneeling, advancing, braced, commanding, piloting, or alert stance.
- Civilian characters should use non-combat action: walking, alert, evacuation-ready, or guarded posture.
- Do not use loaded prompt words that previously caused poster/infographic outputs. Describe concrete visual identity instead: clothing, headwear, gear, body type, weapon silhouette, color, and stance.
- Avoid terms like `insurgent` and `sidearm` in generation prompts; use neutral concrete descriptions.

## Vehicle Rules

- Card portrait should show the vehicle in a clean readable 3/4 showcase angle.
- Action portrait should use a more dynamic scene: rolling, hovering, banking, turret-ready, launcher-ready, convoy movement, or dust/smoke motion.
- Preserve full silhouette. Do not crop rotors, wings, launch rails, turrets, or cargo beds.
- Do not invent unrelated weapons.

## Building Rules

- Card portrait should show the building from a clean readable 3/4 angle.
- Action portrait should use stronger environment and lighting, but the building remains the subject.
- For walls/barriers, use composition that makes the object readable as a gameplay asset.
- For airport/runway configs, preserve the intended configured subject and avoid unrelated aircraft clutter unless it is already part of the identity.

## Per Asset Checklist

1. Resolve config path.
2. Confirm the config has a base `portraitSprite`; if not, mark skipped.
3. Resolve prefab path/reference plate.
4. Resolve display name, description, and current base portrait path.
5. For characters, resolve `weaponDisplayName`, `weaponSprite`, and prefab weapon model.
6. Generate Card portrait at 512x512-style square composition.
7. Generate Action portrait at 512x512-style square composition.
8. Copy generated sources into `Assets/Game/Art/UI/Portraits/Secondary/`.
9. Resize/crop final project PNGs to exactly `512x512`.
10. Set Unity import metadata to Sprite, no mipmaps, sRGB, readable off.
11. Assign `portraitCardSprite` and `portraitActionSprite` in the config.
12. Inspect both images against the prefab/base portrait.
13. Verify GUID assignment in the config YAML.
14. Update `Design/Architecture/portrait_card_action_generation_manifest.md`.

## Atlas Step

After all non-skipped targets are complete:

1. Extend or run the portrait atlas builder so the secondary portraits are packed.
2. Create separate atlases for Card/Action or one `Portraits_Secondary.spriteatlas` under:
   `Assets/Game/Art/UI/Portraits/Atlases/`
3. Include the atlas in build.
4. Verify all secondary sprites are packable and assigned.

## Verification Gate

Do not mark an asset complete unless:

- Both Card and Action PNGs exist in the project.
- Both are exactly `512x512`.
- Both import as Sprite.
- Both visually match the config/prefab identity.
- For characters, the weapon matches the config weapon sprite and prefab weapon.
- The config has both new fields assigned to the correct GUIDs.

## Accepted Reference

Completed sample:

- Card: `Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Chr_Soldier_Male_01_HeavyGunner_Card_512.png`
- Action: `Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Chr_Soldier_Male_01_HeavyGunner_Action_512.png`

