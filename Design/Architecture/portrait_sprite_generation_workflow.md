# Portrait Sprite Generation Workflow

## Goal

Generate high-quality UI portrait sprites for every requestable character, vehicle, and building while preserving the exact gameplay identity from the Unity prefab and config.

The raw Unity render is only a reference plate. It is not the final UI art style.

## Output Style

- Final art is premium tactical game UI art.
- Character portraits can be more realistic than the Synty/Polygon source, but must preserve the prefab identity.
- Vehicle and building portraits should be polished, readable, and close to the in-game silhouette.
- Use a flat solid `#00ff00` chroma-key background for later transparency cutting.
- No floor, shadows, text, frames, logos, or background scene props.

## Source Of Truth

- Config display name and description define the UI identity.
- Character weapon sprite and the weapon model in the prefab define the weapon. The generated weapon must match both.
- Prefab `Model` hierarchy defines the object shape, colors, important attachments, and silhouette.
- Generated image filenames must include the prefab/config id so assignment is traceable.
- Accepted portrait style can be reused only as a quality target. It must not be reused as character identity. Every asset must use its own prefab reference plate before generation.

## Character Rules

- Use the exact character prefab as the visual identity reference.
- Use the exact `weaponSprite` and the 3D weapon in the character hand as weapon references.
- Never substitute a generic weapon.
- If weapon type is `Machine Gun`, `Sniper Rifle`, `RPG`, `Rocket Launcher`, `Pistol`, `Compact Pistol`, `SMG`, or `Rifle`, the final image must visibly match that category.
- For pistol-equipped characters, avoid prompt wording such as `Sidearm`; describe the concrete visible equipment instead, such as one pistol, dual pistols, compact dark pistol, holster, headwear, clothing, and gear.
- Civilian configs with no weapon should not be given a weapon.

## Vehicle Rules

- Use the vehicle prefab model as the primary silhouette reference.
- If a vehicle has no weapon sprite, do not invent handheld weapons or unrelated armaments.
- Show the whole vehicle from a readable 3/4 UI angle.
- Preserve distinctive parts: turret, launcher rails, rotor blades, wings, cargo bed, tanker body, radar dish, or drone frame.

## Building Rules

- Use the building prefab model as the primary silhouette reference.
- Show the full building from a readable 3/4 UI angle.
- Preserve distinctive gameplay shape: tents, barracks, refinery tanks, oil pumps, guard towers, airport/runway/tower composition, walls, barriers, shop, house, and water/fuel storage.
- Do not add unrelated scenery or extra units.

## Generation Order

1. Characters first, because weapon correctness is highest risk.
2. Vehicles next, because silhouette and faction-neutral color are important.
3. Buildings last, because they need clean object identity and no accidental environment clutter.

## Per Asset Checklist

For each asset:

1. Resolve config path.
2. Resolve prefab path.
3. Resolve display name and description.
4. For characters, resolve `weaponDisplayName` and `weaponSprite` path.
5. Render or inspect the prefab reference.
6. Generate high-quality portrait using the prefab/reference only as identity input.
7. Save PNG under `Assets/Game/Art/UI/Portraits/Generated/`.
8. Add Unity Sprite `.meta`.
9. Inspect the saved PNG.
10. Assign the sprite to the config `portraitSprite`.
11. Re-open or scan the config to verify the assigned GUID.

## Verification Gate

Do not mark an asset complete unless:

- The saved PNG exists inside the project.
- Unity imports it as `TextureImporter.textureType: Sprite`.
- The visual matches the prefab/config identity.
- Character weapon matches the config weapon sprite and prefab weapon.
- The config asset has `portraitSprite` assigned to the generated sprite GUID.

## Current Reference Quality Target

The accepted target style is:

`Assets/Game/Art/UI/Portraits/Generated/Portrait_Unit_Chr_Soldier_Male_01_AI_RealisticMachineGun_ChromaGreen.png`

This image is accepted because it is higher-quality UI art while retaining the correct heavy-gunner identity and configured machine gun.
