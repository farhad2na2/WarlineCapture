# Support artwork and model binding

Date: 2026-09-28

Owner direction: use models/assets already in the game. The original full-screen mockup's A-10-like strike aircraft, propeller transport and invented rocket salvo are rejected. The owner approved the full-screen v02 project-asset direction on 2026-09-28. Exact native asset fidelity and runtime readiness still require validation.

## Current mockup references

The corrected [full-screen proposal](Mockups/05-support-fullscreen-v02-project-assets.png) was edited with built-in ImageGen using these existing project images. It is a reference-grounded concept, not a literal Unity prefab render or pixel-exact composition.

| Support | Existing artwork used | Model binding / production rule |
|---|---|---|
| Precision Strike | `Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Veh_Jet_01_Action_512.png` | `Assets/Game/Prefabs/Vehicles/Unit_Veh_Jet_01.prefab`; catalog calls it Strike Jet. Use this aircraft silhouette/materials. No substitute A-10, bomber, invented weapons or rocket salvo. |
| Paratroopers | `Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Veh_Plane_Transport_Card_512.png` | `Assets/Game/Prefabs/Vehicles/Unit_Veh_Plane_Transport.prefab`; same transport used for Supply delivery. Existing artwork depicts four turbofans and a T-tail; do not substitute a propeller transport. |
| Smoke Screen | `Assets/Synty/InterfaceMilitaryCombatHUD/Sprites/Icons_Resources/ICON_SM_Wep_Grenade_Smoke_01_Military.png` | Existing source prop `Assets/PolygonMilitary/Prefabs/Weapons/SM_Wep_Grenade_Smoke_01.prefab`; smoke VFX candidate `Assets/PolygonMilitary/Prefabs/FX/FX_Smoke_Medium_01.prefab`. Presence is verified; Support runtime effect/scale is not. |
| Supply Drop | `Assets/Game/Art/UI/Icons/scn09_icon_supply_crate.png` | This is the existing UI crate icon, not evidence of a matching world mesh. Delivery aircraft is the mapped transport. Candidate existing world payload `Assets/Synty/PolygonBattleRoyale/Prefabs/Props/SM_Prop_EmergencyDrop_Crate_01.prefab` must be inspected and captured before final card/world art lock. Do not invent a new crate. |

Existing parachute source candidate: `Assets/Synty/PolygonBattleRoyale/Prefabs/Props/SM_Prop_Parachute_01.prefab`, with existing model icon `Assets/Synty/InterfaceMilitaryCombatHUD/Sprites/Icons_Resources/ICON_SM_Prop_Parachute_01_BattleRoyale.png`. The corrected mockup reuses the existing popup's parachute UI badge; it does not depict a new canopy or troop model.

Paratrooper payload: resolve the existing mission-approved rifle squad and its character prefabs from the active mission roster. Do not invent a soldier costume or infer squad size from generated pictures. No new aircraft/soldier/weapon model procurement is included in this proposal.

## Production acceptance

1. S0 records exact prefab and artwork paths/GUIDs in the final ability visual definitions. Asset paths above were checked to exist; prefab behavior and suitability still require native validation.
2. Prefer reusing the exact existing card/portrait sprites. When a new pose or delivery illustration is needed, capture the mapped prefab with correct materials and use that capture as the subject. Do not regenerate aircraft geometry from a text description.
3. Compare source prefab capture and final card side by side: silhouette, engine number/type, wing/tail shape, faction material, weapons, payload and parachute rig. Include the comparison in native visual evidence.
4. Smoke/strike effects must match implemented gameplay. Do not depict a rocket salvo or bombing pattern for the single-target Strike, or airborne soldiers before the actual descent rig is selected and verified.
5. Supply card and in-world crate must converge on the same selected existing payload model, or explicitly retain the current generic resource icon as iconography. The current UI icon must not be misrepresented as a verified render of the EmergencyDrop prefab.
6. Preserve full-screen layout, costs, mission context and ARIA controls. Asset correction is not permission to redesign the UI or change the roster.

## Rejected reference evidence

The stored chroma-green `PortraitReference_Unit_Veh_Jet_01_ChromaGreen.png`, `...Jet_02...` and `...Plane_Transport...` under `Assets/Game/Art/UI/Portraits/Generated/References/Vehicles/` were inspected and have visibly incomplete/green-contaminated rendering. They were not used for the revised mockup and are not suitable as final prefab proof. The existing Secondary artwork was used instead. This task did not launch Unity or claim a fresh native capture.

The old `05-support-fullscreen-v01.png` remains as rejected aircraft-art evidence, not the current approval target. Earlier HUD mockups demonstrate interaction layout only and are not authoritative model/asset references.
