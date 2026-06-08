# Designer Audit: 3D Direction Alignment

Date: 2026-05-21
Lane: Designer
Status: Complete

## Scope

Audited active design docs for wording and source-of-truth conflicts against `Design/3D_SingleMap_Gameplay_Direction.md`.

Excluded historical agent reports and VisualLock proof packages from blocker status unless they are named by an active workflow as the current target. Historical 2D/isometric docs that now have a superseded notice are acceptable as archive material.

## Findings

| Priority | File / Lines | Issue | Required Fix |
|---|---|---|---|
| P0 | `Design/README.md:223-229` | The index correctly says the 2026-05-21 3D direction wins, but line 229 still says to prefer the newer 2D isometric production direction. This is the most direct source-of-truth contradiction. | Replace the stale conflict rule with one that prefers `3D_SingleMap_Gameplay_Direction.md` for gameplay/art and `UIUX_MainMenu_Visual_Contract.md` / target-to-canvas workflow for UI implementation. |
| P0 | `Design/GAME_DESIGN_REFERENCE.md:13-19` | The compact game reference still declares premium 2D isometric as the active presentation direction. This doc is first in the Design reading order, so it can mislead future agents immediately. | Update active presentation direction to 3D single-map and point to `3D_SingleMap_Gameplay_Direction.md`; keep 2D iso only as historical. |
| P0 | `Design/AAA_Mobile_Game_Design_Document_v0_1.md:5-14, 19-38` | The AAA GDD still says production direction is premium 2D isometric, recommends Saga Map Campaign / Persistent City Operation / Quick Custom Games, and lists 2D isometric art bible as a highest-priority build item. | Add a 2026-05-21 amendment or rewrite the executive summary, mode names, and priority items around Campaign / Operations / Skirmish and 3D single-map production. |
| P0 | `Design/Project_State_Source.json:46, 91, 101-125, 131, 157, 180` and generated dashboard | Project state still tracks `2D Iso Terrain Visual Validation`, `World Gameplay Iso Assets`, macro-tile dependencies, and 2D isometric presentation work as active. The dashboard repeats this. | Update the JSON source to replace iso terrain plans with 3D single-map/menu direction work, then regenerate `Project_State_Dashboard.md`. Do not edit dashboard by hand. |
| P1 | `Design/Level_And_Mission_Content_Plan.md:7-11, 43-46, 62-89` | The active mission-authoring plan still requires strategic/tactical map separation, `IsoMapId`, `TacticalMapDefinition`, Saga Campaign, Persistent Operation, and Quick Custom terminology. | Rewrite around `OperationMapId`, 3D operation map metadata, planning/battle camera states, Campaign / Operations / Skirmish labels, and config-backed unit/building roster. |
| P1 | `Design/UIUX_Screen_Popup_Implementation_Spec.md:6-9, 38-39, 58-90` plus later SCN sections | The UI screen spec still names premium 2D isometric as active, uses old SCN-02 labels, old resources (`Materials`, `Command Authority`), and old mode names. | Update global UI rules and all affected SCN/POP sections to command-base style, 3D operation art, `Credits / Supplies / Command`, and Campaign / Operations / Skirmish. |
| P1 | `Design/UIUX_Implementation_Detailed_Spec.md:17-39` | The implementation spec says active visual direction is premium 2D isometric and defines a 2D iso parallel-work boundary. | Replace with a 3D single-map implementation boundary: UI owns Canvas/app shell/HUD; gameplay/art owns 3D operation scenes, 3D camera states, metadata, and performance validation. |
| P1 | `Design/UIUX_Gameplay_Element_Alignment.md:30-36, 73, 79, 93-94, 104-117, 126-140` | The UI gameplay contract still uses `Mission -> ScenarioSetup -> Level / Map`, `IsoMapId`, macro-tile metadata, 2D isometric key art, Saga Map, Quick Custom, and macro-tile build placement wording. | Convert the contract to `Mission -> ScenarioSetup -> OperationMap`, 3D metadata/camera/minimap bindings, config-backed unit/building thumbnails, and Skirmish labels. |
| P1 | `Design/UIUX_Mockup_To_Canvas_Conversion_Plan.md:28-32, 42-67` | The conversion plan says battlefield art is premium 2D isometric, prompts new targets with 2D iso art, and points SCN-02 to the old `VisualLock/MainMenu` target instead of `SCN-02B_MainMenuAlt`. | Update target-creation rules to use 3D command-base / 3D operation imagery and promote the SCN-02B layered target as canonical. |
| P1 | `Design/SagaChapters/Saga_Chapter01_First_Response.md:38, 62-74, 106, 168, 214, 278, 327, 392, 441, 506, 554, 619` | Chapter 1 still uses strategic/tactical split, `IsoMapId`, tactical map packages, and player-facing Saga Campaign naming. | Preserve internal file names if needed, but update player-facing wording and mission map contracts to 3D operation maps. |
| P2 | `Design/Designer_Role_And_Documentation_Workflow.md:54-56, 99` | Designer workflow still says the three-mode structure is Saga / Operation / Quick Custom and tells designers to mark old 3D assumptions as conflicts with 2D macro-tile direction. | Update role guidance so Designer audits against 3D single-map, Campaign / Operations / Skirmish, and command-base menu visual direction. |
| P2 | `Design/Art_Asset_Requirements_Register.md:31, 138, 146, 161` | Art register still treats strategic/tactical map lanes and tactical map native AI output as production rules. | Add an active 3D single-map section and mark old map-lane rows as historical or migration-only. |
| P2 | `Design/Monetization/Monetization_Visual_Targets.md:78-110` and related monetization docs | Store visual prompts still use old cyan/2D isometric item-icon language. This is lower risk because store gameplay/economy rules are mostly unaffected. | Refresh visual prompt language to command-base chrome and 3D config-backed item renders when store target regeneration starts. |

## Alignment Notes

- `Design/2D_Isometric_Production_Direction.md`, `Design/2D_Isometric_Art_Bible.md`, `Design/2D_Isometric_Implementation_Validation_Plan.md`, `Design/MacroTile_Terrain_Production_Plan.md`, and `Design/Strategic_Tactical_Map_Gameplay_Alignment.md` now have superseded notices. Their body text can remain historical as long as the notices stay prominent.
- Agent reports before 2026-05-21 contain many stale terms. They should remain immutable history unless a specific current task incorrectly points to them as active direction.
- Existing runtime route/class names such as Saga or QuickCustom can remain until implementation work changes them. The audit concerns player-facing design wording and active docs.

## Recommended Fix Order

1. Fix `Design/README.md` conflict rule and `GAME_DESIGN_REFERENCE.md`.
2. Update the AAA GDD and project-state JSON/dashboard.
3. Rewrite the mission/content plan and UI gameplay element contract.
4. Update UI screen/popup spec and mockup-to-canvas workflow.
5. Sweep Saga chapter docs, Designer workflow, art register, and monetization visual prompts.

## Fix Pass Completed

Completed on 2026-05-21.

- P0 source-of-truth docs were updated: `Design/README.md`, `Design/GAME_DESIGN_REFERENCE.md`, `Design/AAA_Mobile_Game_Design_Document_v0_1.md`, and `Design/Project_State_Source.json`.
- The generated project dashboard was regenerated from JSON on 2026-05-21.
- Active mission/content, UI gameplay, detailed UI implementation, target-to-canvas, Main Menu, large-scale movement, combat catalog, chapter, Designer workflow, art register, and monetization docs were aligned to full 3D single-map direction.
- Lower-priority or obsolete request/audit docs were marked historical/legacy instead of rewritten where preserving process history is more useful than converting the body text.
- Focused scan of active source docs now leaves only intentional compatibility references where the doc explicitly explains that old runtime names such as QuickCustom may remain until code migration.
