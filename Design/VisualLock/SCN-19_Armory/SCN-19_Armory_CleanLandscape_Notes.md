# SCN-19 Armory Visual Target

- Canvas: 1672 x 941.
- Canonical target: `Design/VisualLock/SCN-19_Armory/SCN-19_Armory_Landscape_Target.png`.
- Layered package: `Design/VisualLockLayered/SCN-19_Armory`.
- Direction: generated AAA landscape target using the accepted WarlineCapture dark graphite, cyan edge, amber CTA military RTS UI style.
- Gameplay source: `WarlineCapture_Combat_Catalog_And_Upgrade_Design.md`, `BalanceConfigs/WarlineCapture_Combat_Balance_Config_v0_1.json`, and `VisualConfigs/WarlineCapture_Combat_Visual_Config_v0_1.json`.

## Implementation Notes

- Build as a real screen, not a baked screenshot.
- Resource counters bind to Credits, Materials, Fuel, Intel, and Command Authority.
- Category rail binds to Units, Vehicles, Air, Sea, Buildings, and Support.
- Roster cards bind to `PlayerInventory`, resolved unit/building/ability ids, upgrade-track ids, and visual catalog art.
- The right inspection panel uses the selected item id, current tier, parts progress, stat preview, unlock source, and disabled reason.
- Upgrade CTAs remain disabled until `UpgradeService`, inventory persistence, GearModule spending, and validation tests exist.
- Item card press opens `POP-09 Ability / Upgrade Detail`.

## Generation Prompt

```text
Use case: ui-mockup
Asset type: AAA mobile RTS landscape UI visual-lock target for WarlineCapture, 1672x941.
Primary request: Create a polished high-end final mockup for screen SCN-19 ARMORY. It must match the accepted WarlineCapture premium military RTS HUD style: dark graphite layered panels, cyan edge highlights, restrained amber/gold CTAs, Oxanium-like condensed sci-fi typography, crisp mobile landscape layout, no cartoon style, no flat wireframe.
Scene/backdrop: subdued premium 2D isometric city command base visible behind translucent HUD panels, with armored vehicles, hangar lights, workshop silhouettes, no busy clutter.
Layout: full screen app shell with top header bar, back button left, title exact text ARMORY, resource counters Credits, Materials, Fuel, Intel, Command Authority on right. Left vertical category rail with exact labels Units, Vehicles, Air, Sea, Buildings, Support. Center content area shows a dense roster/upgrade grid with 8 item cards: Rifle Squad, Ranger Squad, APC Armor, Drone Sensor, Guard Tower, Airlift Support, Patrol Hull, Rally Order. Right inspection panel shows selected item APC Armor Upgrade with tier meter I II III IV, blueprint parts progress 18/40, stat preview Armor +8%, Survivability +5%, Unlock source Chapter 1 M05, and a disabled amber button exact text UPGRADE LOCKED. Bottom strip shows tabs Owned, Upgrade Tracks, Parts, Gear Modules and a small disabled reason line exact text Requires BlueprintParts and GearModule.
Layering requirement: visually separate reusable layers. Frames, fills, category tabs, item card frames, tier pips, resource frames, icons, and content art must look separable. Text may appear in the target but must look like live UI labels, not baked decorative art.
Avoid: Tokens, gems, Intel Keys, SagaStars, direct Operation metric purchases, fantasy magic, purple gradients, giant marketing hero composition, rounded pill-heavy mobile store style, tiny unreadable text, overlapping UI, placeholder TBD labels, lorem ipsum, watermarks.
```
