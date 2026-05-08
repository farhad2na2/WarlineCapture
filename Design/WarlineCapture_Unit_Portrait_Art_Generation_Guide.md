# WarlineCapture Unit Portrait Art Generation Guide

## Purpose

This guide defines the reference style for AI-generated unit portrait icons used by WarlineCapture. The first approved reference portrait is:

- `Assets/Game/Textures/Generated/Portraits/Portrait_Unit_Chr_Soldier_Male_02.png`
- `Assets/Game/Textures/Generated/Portraits/Portrait_Unit_Chr_Soldier_Male_02_Transparent.png`

Use it as the visual quality and style target for future soldier, vehicle crew, civilian, contractor, insurgent, and named-character portraits.

Portrait production must use ids from `VisualConfigs/WarlineCapture_Combat_Visual_Config_v0_1.json`. Gameplay stats, costs, upgrades, and unlock gates stay in `BalanceConfigs/WarlineCapture_Combat_Balance_Config_v0_1.json`; do not bake those values into portrait art.

## Reference Inputs

Primary unit reference:

- `Assets/Game/Prefabs/Generated/CombinedSkinned/SM_Chr_Soldier_Male_02_CombinedSkinned.prefab`

Available visual reference atlas:

- `Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Soldier_Male_02.png`

The prefab may be in a T-pose or neutral rig pose. Generated portrait art must not preserve the T-pose. The portrait should reinterpret the unit as a polished active combat character while preserving the recognizable outfit, silhouette, palette, and role.

The transparent portrait variant is a cutout derived from the approved background portrait. It should be treated as the same art, not a second generated interpretation. Use transparent variants when UI cards, popups, hero panels, or reward screens need to place the unit over WarlineCapture UI-controlled backgrounds.

## Approved Style Direction

- Square 1:1 mobile game portrait icon.
- Stylized 3D render look, semi-realistic but compatible with the low-poly/Synty-style unit models.
- Strong readable silhouette at mobile size.
- Head, shoulders, and upper chest composition.
- Dynamic 3/4 pose, alert and ready for combat.
- No T-pose, no mannequin stance, no full-body render.
- Clean tactical military identity: helmet, dark goggles or visor, vest, gloves, boots, tan/olive armor, and compact proportions.
- Weapon may appear low across the chest, but must never cover the face.
- Dark military green, charcoal, or subtle city-combat background with high contrast behind the head.
- No text, logo, watermark, border, extra characters, gore, or photoreal actor likeness.

## Reusable Prompt Template

```text
Use case: stylized-concept
Asset type: square mobile game unit portrait icon for WarlineCapture, a modern military city strategy/RTS game.
Input reference: Use the provided unit prefab render or impostor atlas as the character identity reference. Preserve the unit's recognizable outfit, helmet/headgear, armor, role, palette, silhouette, and chunky low-poly/semi-realistic geometry language.
Primary request: Generate a cool portrait image for this unit, not a T-pose. Make the character look like an active unit ready for battle.
Subject: [unit role and identity], bust portrait, head shoulders and upper chest, posed dynamically in a confident 3/4 view. Keep the outfit, equipment, color palette, and unit role from the reference.
Pose and expression: cinematic hero-unit stance, alert and focused, shoulders turned, chin slightly down, no T-pose, no full-body mannequin pose.
Style: polished mobile strategy game UI art, stylized 3D render look, semi-realistic low-poly/Synty-compatible, sharp readable silhouette, simplified faceted surfaces, clean material separation, high contrast, premium unit-card portrait quality.
Composition: square 1:1 icon, centered bust, character fills most of frame, face/headgear clearly readable at small mobile size. No border, no text, no logo, no watermark, no extra characters.
Background: simple dark military green/charcoal gradient with a subtle blurred city-combat atmosphere, enough contrast behind the subject, not busy.
Lighting: dramatic readable key light from upper front-left, subtle cool rim light, clear face/visor visibility, game-ready contrast.
Avoid: T-pose, arms stretched sideways, full body, photoreal actor likeness, gore, excessive realism, cartoon proportions, text, UI labels, over-detailed background, weapon covering face.
```

## Soldier Male 02 Approved Prompt

```text
Use case: stylized-concept
Asset type: square mobile game unit portrait icon for WarlineCapture, a modern military city strategy/RTS game.
Input reference: The visible reference image is an impostor atlas for Assets/Game/Prefabs/Generated/CombinedSkinned/SM_Chr_Soldier_Male_02_CombinedSkinned.prefab. Use it as the character identity reference: low-poly/semi-realistic male infantry soldier, tan/olive tactical uniform, helmet with dark goggles/visor, tactical vest, gloves, boots, dark rifle, compact Synty-like proportions.
Primary request: Generate a cool portrait image for this soldier, not a T-pose. Make him look like an active combat unit ready for battle.
Subject: male soldier bust portrait, head shoulders and upper chest, posed dynamically in a confident 3/4 view. One hand may hold a dark rifle low across the chest, but the weapon must not cover the face. Keep the helmet, dark goggles/visor, tan tactical armor, shoulder pads, backpack/vest details, and chunky low-poly geometry language from the reference.
Pose and expression: cinematic hero-unit stance, alert and focused, shoulders turned, chin slightly down, no T-pose, no full-body mannequin pose.
Style: polished mobile strategy game UI art, stylized 3D render look, semi-realistic low-poly/Synty-compatible, sharp readable silhouette, simplified faceted surfaces, clean material separation, high contrast, premium unit-card portrait quality.
Composition: square 1:1 icon, centered bust, character fills most of frame, face and helmet clearly readable at small mobile size. No border, no text, no logo, no watermark, no extra characters.
Background: simple dark military green/charcoal gradient with a subtle blurred city-combat atmosphere, enough contrast behind the tan helmet and shoulders, not busy.
Lighting: dramatic readable key light from upper front-left, subtle cool rim light, clear face/visor visibility, game-ready contrast.
Avoid: T-pose, arms stretched sideways, full body, photoreal actor likeness, gore, excessive realism, cartoon proportions, text, UI labels, over-detailed background, weapon covering face.
```

## Batch Generation Rules

1. Start from an in-project prefab, generated impostor atlas, or rendered preview for each unit.
2. Preserve unit identity first: helmet/headgear, faction color, weapon class, armor silhouette, and role.
3. Change pose and presentation into a portrait-ready hero stance.
4. Keep all portraits square and compositionally consistent.
5. Save generated portraits in `Assets/Game/Textures/Generated/Portraits`.
6. Name files as `Portrait_<UnitId>.png`, matching the unit or prefab identifier.
7. If a transparent version is needed, derive it from the approved portrait and name it `Portrait_<UnitId>_Transparent.png`.
8. Import portraits as Sprite textures for UI usage.
9. Reject outputs with T-pose silhouettes, unclear faces, busy backgrounds, covered faces, text artifacts, or a style that looks unrelated to the approved Soldier Male 02 reference.
