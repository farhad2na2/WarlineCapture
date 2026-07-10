# WarlineCapture Saga Chapter Design Index

Date: 2026-05-21

2026-07-10 narrative amendment: all five chapter files now implement the high-level `Shattered Relay` story contract. Detailed implementation planning remains deferred.

This folder owns Campaign chapter-specific design. `../Campaign_Narrative_Bible.md` owns the setting, characters, central mystery, Protocol Fragments, and 25-mission story map. The root `../Level_And_Mission_Content_Plan.md` owns the shared mission template and acceptance gates. Each chapter file may expand its own story arc, mission matrix, feature exposure, reward pacing, balance direction, and validation focus without contradicting those authorities.

## Reading Order

1. `../AAA_Mobile_Game_Design_Document_v0_2.md`
2. `../Campaign_Narrative_Bible.md`
3. `../Gameplay_North_Star_And_Content_Grammar.md`
4. `../Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md`
5. `../Level_And_Mission_Content_Plan.md`
6. `../First_Player_Experience_And_Story_Onboarding_Design.md`
7. `../Narrative_Presentation_And_Cutscene_Design.md`
8. `../3D_SingleMap_Gameplay_Direction.md`
9. `../Combat_Catalog_And_Upgrade_Design.md`
10. `../BalanceConfigs/Combat_Balance_Config_v0_1.json`
11. `Saga_Chapter01_First_Response.md`
12. `Saga_Chapter02_Broken_Grid.md`
13. `Saga_Chapter03_Hidden_Network.md`
14. `Saga_Chapter04_Air_And_Armor.md`
15. `Saga_Chapter05_Citywide_Command.md`

## Chapter Docs

| Internal Chapter | Player-Facing Name | File | Current Detail Level |
|---|---|---|---|
| Chapter 1 | First Response | `Saga_Chapter01_First_Response.md` | Mission matrix plus detailed specs for all five Chapter 1 missions. |
| Chapter 2 | Broken Grid | `Saga_Chapter02_Broken_Grid.md` | Detailed high-level story, feature, character, and mission arc. |
| Chapter 3 | Hidden Network | `Saga_Chapter03_Hidden_Network.md` | Detailed high-level story, feature, character, and mission arc. |
| Chapter 4 | Air And Armor | `Saga_Chapter04_Air_And_Armor.md` | Detailed high-level story, feature, character, and mission arc. |
| Chapter 5 | Citywide Command | `Saga_Chapter05_Citywide_Command.md` | Detailed high-level story, feature, character, ending, and mission arc. |

## Update Rules

- Keep the root level-and-mission plan high-level.
- Preserve the chapter question, Protocol Fragment, character arc, and mission story beat from `../Campaign_Narrative_Bible.md`.
- Check every required mechanic against `../Gameplay_Feature_Maturity_And_Campaign_Exposure_Matrix.md` before detailed design.
- Keep first-launch M01 presentation aligned with `../First_Player_Experience_And_Story_Onboarding_Design.md`.
- Use the sequence tiers and cultural review rules in `../Narrative_Presentation_And_Cutscene_Design.md`.
- Put mission-by-mission details in the relevant chapter file.
- Do not create mission specs that skip the template in `../Level_And_Mission_Content_Plan.md`.
- Every playable mission must resolve operation-map ids: `OperationMapId`, `PlanningCameraId`, `MinimapProjectionId`, and operation-map metadata anchors.
- Keep reward names aligned with `../Economy_Reward_Design.md`.
- Resolve unit, building, support ability, upgrade-part, and gear reward target ids through `../BalanceConfigs/Combat_Balance_Config_v0_1.json`.
- Keep balance targets aligned with `../Balancing_Automated_Test_Plan.md`.
