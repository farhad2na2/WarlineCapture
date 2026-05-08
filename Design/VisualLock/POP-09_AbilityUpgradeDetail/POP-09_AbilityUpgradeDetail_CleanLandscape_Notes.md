# POP-09 Ability / Upgrade Detail Visual Target

- Canvas: 1672 x 941.
- Canonical target: `Design/VisualLock/POP-09_AbilityUpgradeDetail/POP-09_AbilityUpgradeDetail_Landscape_Target.png`.
- Layered package: `Design/VisualLockLayered/POP-09_AbilityUpgradeDetail`.
- Direction: generated AAA popup target using the accepted WarlineCapture dark graphite, cyan edge, amber CTA military RTS UI style.
- Gameplay source: ability and upgrade availability specs in `BalanceConfigs/WarlineCapture_Combat_Balance_Config_v0_1.json`.

## Implementation Notes

- Build as a reusable modal under `ModalOverlay`.
- The popup accepts either an `AbilityConfig` or an `UpgradeTrackConfig`, plus the source surface route.
- Used by Mission Briefing, Loadout, RTS HUD, Unit Command Wheel, Reward Unlock, Intel Reveal, Store, and Armory.
- Target id, unlock moment, availability, prerequisite, cooldown, charges, parts progress, GearModule requirement, and disabled reason remain live data.
- The bottom note is implementation copy: exact values load from Balance Config and art loads from Visual Config.

## Generation Prompt

```text
Use case: ui-mockup
Asset type: AAA mobile RTS landscape popup visual-lock target for WarlineCapture, 1672x941.
Primary request: Create a polished high-end final mockup for popup POP-09 ABILITY / UPGRADE DETAIL. It must match WarlineCapture premium military RTS HUD style: dark graphite modal frame, cyan edge highlights, restrained amber/gold primary controls, Oxanium-like condensed sci-fi typography, clean mobile landscape readability, no flat wireframe.
Scene/backdrop: dimmed blurred tactical command UI background with a dark modal scrim; centered implementation-ready modal occupying about 72 percent width and 70 percent height.
Modal layout: top bar with close X, title exact text ABILITY / UPGRADE DETAIL, small badge exact text CONFIG TARGET. Left content art panel shows a high-end isometric drone scanning over city blocks with cyan scan cone. Center panel shows selected item exact text DRONE SCAN, type Support Ability, target id ability.drone_scan, unlock exact text Chapter 1 M03 Reward, availability exact text Loadout, Briefing, HUD, Intel Reveal, Store, prerequisite exact text Requires Intel and Support Slot. Right panel shows effect cards: Reveal fogged district, Mark hidden hostile, Cooldown 45s, Charges 1. Lower comparison row shows upgrade target exact text APC Armor Upgrade, target id upgrade.vehicle.apc_armor, parts 18/40, GearModule x1. Bottom actions: disabled amber button exact text NOT UNLOCKED, secondary button exact text VIEW SOURCE, tooltip row exact text Exact values load from Balance Config; art loads from Visual Config.
Layering requirement: frames, modal fill, close button, content art, detail rows, stat cards, tag chips, disabled button, secondary button, warning/tooltip row, and icons must look like separate reusable layers. Text may appear in this target but must look like live TMP labels.
Avoid: Tokens, gems, Intel Keys, SagaStars, direct Operation metric grants, fantasy magic, purple gradients, giant marketing hero composition, placeholder TBD labels, lorem ipsum, watermarks, overlapping text.
```
