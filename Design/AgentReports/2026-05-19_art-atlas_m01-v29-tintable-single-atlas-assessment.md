# Art/Atlas M01 V29 Tintable Single Atlas Assessment

Date: 2026-05-19
Owner: Art/Atlas
Status: PM/user decision needed
Priority: P0

## Summary

User asked whether M01 can use one soldier atlas with tintable areas for different factions.

Yes, this is feasible and is the better scalable contract, but it should be implemented as a masked tint system, not as a whole-sprite material tint.

## Current Runtime Finding

`MissionRuntimeAtlasQuadPresentationSystem` already applies a material color through `ApplyColorToSoldiers`, but the current V28 path resolves enemy tint to `Color.white`.

Whole-sprite tint is not sufficient for final art because it tints:

- baked shadows;
- black/blue armor shading;
- metal highlights;
- muzzle flashes;
- anti-aliased alpha edges.

That would reduce the V28 target-match quality and likely recreate the earlier "fake tint" issue.

## Recommended Art Contract

Use one neutral soldier body+shadow atlas plus one matching mask atlas:

- Base atlas: current V28-quality neutral/dark soldier body+shadow frames.
- Faction mask atlas: same dimensions, same `256x256` cells, same `16x7` used frame area, same frame order, transparent everywhere except tintable armor/cloth panels.
- Runtime faction color is applied only through the mask.
- Baked shadows and source highlights stay neutral.

Expected Art package paths if PM/user approves this pivot:

- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_body_shadow_atlas_v29.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_animation_faction_mask_atlas_v29.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_idle_direction_locked_facings_body_shadow_atlas_v29.png`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/Units/DirectionLockedV29/SharedSoldier/soldier_idle_direction_locked_facings_faction_mask_atlas_v29.png`

## Gameplay Impact

Gameplay would need a small binding/render change:

- load both base atlas and mask atlas for the same frame rect;
- draw base atlas normally;
- draw the mask atlas over it using the unit faction color, or use a shader with base texture + mask texture + faction color;
- keep existing pivot, scale, frame timing, direction keys, and baked-shadow frame rects.

This should replace the one-off "red enemy atlas" requirement if PM/user approves the tintable system.

## Recommendation

Approve the tintable-mask pivot for V29 unless the immediate milestone requires no Gameplay renderer change. If no Gameplay change is allowed, Art should still deliver a baked red enemy atlas as originally dispatched.
