# WarlineCapture Saga Chapter Design Index

Date: 2026-05-21

This folder owns Campaign chapter-specific design. The root `WarlineCapture_Level_And_Mission_Content_Plan.md` owns shared mission templates, high-level campaign structure, Operations hooks, Skirmish mapping, and acceptance gates. Each chapter file owns its own chapter arc, mission matrix, unlock/reward pacing, balance direction, and validation focus.

## Reading Order

1. `../WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md`
2. `../WarlineCapture_Level_And_Mission_Content_Plan.md`
3. `../WarlineCapture_3D_SingleMap_Gameplay_Direction.md`
4. `../WarlineCapture_Combat_Catalog_And_Upgrade_Design.md`
5. `../BalanceConfigs/WarlineCapture_Combat_Balance_Config_v0_1.json`
6. `WarlineCapture_Saga_Chapter01_First_Response.md`
7. `WarlineCapture_Saga_Chapter02_Broken_Grid.md`
8. `WarlineCapture_Saga_Chapter03_Hidden_Network.md`
9. `WarlineCapture_Saga_Chapter04_Air_And_Armor.md`
10. `WarlineCapture_Saga_Chapter05_Citywide_Command.md`

## Chapter Docs

| Internal Chapter | Player-Facing Name | File | Current Detail Level |
|---|---|---|---|
| Chapter 1 | First Response | `WarlineCapture_Saga_Chapter01_First_Response.md` | Mission matrix plus detailed specs for all five Chapter 1 missions. |
| Chapter 2 | Broken Grid | `WarlineCapture_Saga_Chapter02_Broken_Grid.md` | High-level chapter arc and mission slots. |
| Chapter 3 | Hidden Network | `WarlineCapture_Saga_Chapter03_Hidden_Network.md` | High-level chapter arc and mission slots. |
| Chapter 4 | Air And Armor | `WarlineCapture_Saga_Chapter04_Air_And_Armor.md` | High-level chapter arc and mission slots. |
| Chapter 5 | Citywide Command | `WarlineCapture_Saga_Chapter05_Citywide_Command.md` | High-level chapter arc and mission slots. |

## Update Rules

- Keep the root level-and-mission plan high-level.
- Put mission-by-mission details in the relevant chapter file.
- Do not create mission specs that skip the template in `../WarlineCapture_Level_And_Mission_Content_Plan.md`.
- Every playable mission must resolve operation-map ids: `OperationMapId`, `PlanningCameraId`, `MinimapProjectionId`, and operation-map metadata anchors.
- Keep reward names aligned with `../WarlineCapture_Economy_Reward_Design.md`.
- Resolve unit, building, support ability, upgrade-part, and gear reward target ids through `../BalanceConfigs/WarlineCapture_Combat_Balance_Config_v0_1.json`.
- Keep balance targets aligned with `../WarlineCapture_Balancing_Automated_Test_Plan.md`.
