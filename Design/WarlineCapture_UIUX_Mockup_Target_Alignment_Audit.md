# WarlineCapture UI Mockup Target Alignment Audit

Date: 2026-05-05

## Purpose

This audit checks whether the UI mockup targets, UI design documents, and current Chapter 1 gameplay designs are aligned. It should be read with:

- `WarlineCapture_UIUX_Gameplay_Element_Alignment.md`
- `WarlineCapture_UIUX_Screen_Popup_Implementation_Spec.md`
- `WarlineCapture_UIUX_Mockup_To_Canvas_Conversion_Plan.md`
- `WarlineCapture_Level_And_Mission_Content_Plan.md`
- `SagaChapters/WarlineCapture_Saga_Chapter01_First_Response.md`
- `WarlineCapture_Economy_Reward_Design.md`

## Audit Result

The core Chapter 1 gameplay UI is covered by existing visual targets and design contracts. The ability/upgrade expansion adds one required non-mission screen and one reusable popup: `SCN-19 Armory` and `POP-09 Ability / Upgrade Detail`. Both now have high-end VisualLock targets and final layered packages.

The target image inventory is complete for SCN-01 through SCN-14, SCN-19, POP-01 through POP-09, and PREFAB-01 through PREFAB-03, and the target PNGs use the expected 1672 x 941 landscape size. Some high-quality target PNGs still contain older economy, reward, or Chapter 1 sample content. Those images remain useful as quality/style references, but they are not final aligned production targets until regenerated as proper high-end layered target packs.

When a target is regenerated, gameplay-facing art inside the target should follow the premium 2D isometric direction in `WarlineCapture_2D_Isometric_Production_Direction.md` and `WarlineCapture_2D_Isometric_Art_Bible.md`; the existing dark graphite/cyan/gold HUD chrome remains the accepted UI style. Regenerated targets must follow the existing SCN-08 layered workflow: flattened reference plus separate reusable PNG layers, contact sheet, manifest, Unity destination mapping, 9-slice hints, and a dry-run copy helper.

The audit found three alignment fixes, now reflected in the connected UI documents:

1. The detailed UI implementation spec had stale wording that only Mission 1 needed to be playable and later nodes could be locked/placeholder. Chapter 1 now has five complete mission specs, so the UI implementation plan points to the Chapter 1 document and lists the required surfaces per mission.
2. Saga Map and Mission Briefing needed explicit `Mission -> ScenarioSetup -> Level / Map` binding, including `LevelId`, `IsoMapId`, `MapPreviewArtId`, and minimap/preview art data.
3. Mission Result needed an explicit civilian/district consequence row so Chapter 1 star goals and city-pressure outcomes are not hidden behind reward grids.

## Visual Target Coverage

| Surface Group | Target Status | Alignment Result |
|---|---|---|
| SCN-01 Splash / Loading | VisualLock target and notes exist. | Covered. |
| SCN-02 Main Menu / Mode Select | Canonical target exists at `Design/VisualLock/MainMenu/MainMenu_Landscape_Visual_Target.png`. | Covered; target path uses `MainMenu` folder instead of an `SCN-02_` folder. |
| SCN-03 Commander Profile | VisualLock target and notes exist. | Covered. |
| SCN-04 Settings / Accessibility | VisualLock target and notes exist. | Covered. |
| SCN-05 Saga Map | VisualLock target and notes exist. | Covered for Chapter 1 node selection after dynamic chapter/mission binding. |
| SCN-06 Mission Briefing | VisualLock target and notes exist. | Covered after adding explicit Level / Map preview binding. |
| SCN-07 Loadout / Squad Prep | VisualLock target and notes exist. | Covered for missions that allow or require squad/support choice. |
| SCN-08 RTS Battle HUD | VisualLock target, notes, and layered pack exist. | Covered; implementation must map layer/icon names to canonical resource labels. |
| SCN-09 Build Drawer / Production | VisualLock target and notes exist. | Covered for Chapter 1 build/production missions. |
| SCN-10 Unit Command / Command Wheel | VisualLock target, notes, layered pack, and Canvas overlay exist. | Covered for transport, extract, rope drop, breach, and contextual commands through `CommandWheelCanvas` inside `Screen_MatchOverlay`. |
| SCN-11 Operation Dashboard | VisualLock target and route shell exist. | Covered for Persistent Operation as a designed-unavailable shell; full service-backed implementation is not required for Chapter 1 core Saga flow. |
| SCN-12 District Detail / Actions | VisualLock target and route shell exist. | Covered for Persistent Operation as a designed-unavailable shell; full district/action service integration is not required for Chapter 1 core Saga flow. |
| SCN-13 Quick Custom Game Setup | VisualLock target and notes exist. | Covered for custom/probe flows, not required for Chapter 1 Saga progression. |
| SCN-14 Store / Command Exchange | Monetization visual target exists at `Design/Monetization/Images/SCN-14_Store_CommandExchange_Target.png`; layered pack exists at `Design/VisualLockLayered/SCN-14_StoreCommandExchange`. | Covered as a monetization surface; ability/upgrade products open POP-09. |
| SCN-19 Armory | VisualLock target, notes, and layered pack exist at `Design/VisualLockLayered/SCN-19_Armory`. | Covered as the implementation surface for 38 ability/upgrade entries. |
| POP-01 through POP-08 | VisualLock targets and notes exist. | Covered. |
| POP-09 Ability / Upgrade Detail | VisualLock target, notes, and layered pack exist at `Design/VisualLockLayered/POP-09_AbilityUpgradeDetail`. | Covered as the shared detail popup for ability and upgrade ids. |
| PREFAB-01 through PREFAB-03 | VisualLock targets and notes exist. | Covered. |

## Layered Target Regeneration Queue

| Priority | Target | Reason | Required Update |
|---|---|---|---|
| P0 | `Design/Monetization/Images/SCN-14_Store_CommandExchange_Target.png` | The image still shows `TOKENS`, `120 TOKENS`, and `INTEL KEYS`. Those are not canonical WarlineCapture economy resources/rewards. | Regenerate with Credits, Materials, Fuel, Intel, Command Authority, Rush Tickets, BlueprintParts, GearModule, Cosmetic, UnitUnlock, BuildingUnlock, SupportAbilityUnlock, and OperationSupply only. Store purchases must not show SagaStars or Operation metric grants. |
| P0 | `Design/VisualLock/SCN-05_SagaMap/SCN-05_SagaMap_Landscape_Target.png` | The image shows `Chapter 03`, `Shattered Harbor`, nodes `3-1` through `3-6`, `24/30` stars, and `18/21` chapter rewards. Current detailed content is Chapter 1 / `First Response` with five missions. | Regenerate as Chapter 1 / First Response with five nodes: First Contact, Establish The Base, Radar Warning, Airlift, Breach Assault. Use Chapter 1 star totals and reward thresholds. |
| P0 | `Design/VisualLock/SCN-06_MissionBriefing/SCN-06_MissionBriefing_Landscape_Target.png` | The image shows `3-6 Downtown Breakthrough`, old objectives, and no explicit Level / Map preview line. | Regenerate with a current Chapter 1 mission example, preferably M05 Breach Assault for the full late-Chapter surface or M01 First Contact for the first vertical slice. Include Mission, ScenarioSetup, Level / Map, objectives, star goals, enemy intel, and canonical reward preview. |
| P0 | `Design/VisualLock/POP-05_MissionResult/POP-05_MissionResult_Landscape_Target.png` | The image shows `Downtown Breakthrough`, `Supply Crate`, and `Unlock Fragments`, and it does not show the required civilian/district consequence row. | Regenerate with a Chapter 1 result example and a visible consequence row. Replace noncanonical reward labels with CommanderXP, Credits, Materials, Fuel, Intel, Rush Tickets, BlueprintParts, GearModule, UnitUnlock, BuildingUnlock, SupportAbilityUnlock, Cosmetic, or OperationSupply. |
| P1 | `Design/VisualLock/POP-04_RewardUnlock/POP-04_RewardUnlock_Landscape_Target.png` | The image is conceptually aligned for a Ranger Squad unlock, but the header/resource strip and reward cards are not clearly labeled with canonical resource/reward names. | Revise labels so the unlock grants are explicit: CommanderXP, Credits/Materials/Fuel/Intel/RushTickets where applicable, plus UnitUnlock, GearModule, or BlueprintParts. |
| P1 | `Design/VisualLock/MainMenu/MainMenu_Landscape_Visual_Target.png` | The third top resource appears as a blue gem instead of a canonical Command Authority treatment, and the Persistent Operation subtitle says `Global war`, which is broader than the city-operation fantasy. | Update top strip to Credits, Materials, Command Authority and revise Persistent Operation copy toward district/city control and operation pressure. |
| P1 | `Design/VisualLock/SCN-13_QuickCustomGameSetup/SCN-13_QuickCustomGameSetup_Landscape_Target.png` | The image uses `Starting Money` instead of `Starting Credits`, and includes `Super Weapons`, which is not an approved core gameplay option. | Rename to Starting Credits. Replace Super Weapons with a designed custom/probe rule from the Quick Custom plan, or mark it DevOnly in the target. |
| P1 | `Design/VisualLock/SCN-07_LoadoutSquadPrep/SCN-07_LoadoutSquadPrep_Landscape_Target.png` | Route-ready VisualLockLayered pack and Canvas prefab now exist. The source target still uses generic old objective text, so the current implementation should be treated as route-ready until a fully mission-specific high-end target is regenerated. | Keep the generated layer pack and prefab as the functional Loadout baseline; later regenerate with M04 Airlift or M05 Breach Assault if exact mission-specific target art is required. |
| P2 | `Design/VisualLock/SCN-08_RTSBattleHUD/SCN-08_RTSBattleHUD_Landscape_Target.png` and layered pack | The HUD layout is strong, but the top resource icons/layer names still map to generic money/crate/population concepts. | Keep as the tactical HUD layout target, but map imports and labels to Credits, Materials, Fuel, Build Capacity, or mission-specific supply labels before Unity implementation. Regenerate only if final visible labels need to appear in the mockup. |
| P2 | `Design/VisualLock/SCN-09_BuildDrawerProduction/SCN-09_BuildDrawerProduction_Landscape_Target.png` and `PREFAB-03` | The production surface is aligned, but top resource icons and some costs are generic. | Keep layout, but ensure costs bind to Materials, Build Capacity, production time, and Rush Tickets. Regenerate only if final visible labels are required in target art. |

Required output package for every regenerated target:

| Required File Or Folder | Purpose |
|---|---|
| `Design/VisualLock/<SurfaceId>/<SurfaceId>_Landscape_Target.png` | Flattened high-end visual target for design review. |
| `Design/VisualLockLayered/<SurfaceId>/reference/<SurfaceId>_Landscape_Target.png` | Copy of the accepted flattened target used by the layer pack. |
| `Design/VisualLockLayered/<SurfaceId>/generated_one_go/source/generated_layer_atlas_chromakey.png` | One-go atlas source on chroma key or equivalent separated source. |
| `Design/VisualLockLayered/<SurfaceId>/generated_one_go/source/generated_layer_atlas_alpha.png` | Atlas after alpha cleanup. |
| `Design/VisualLockLayered/<SurfaceId>/layers/` | Separate implementation PNGs for frames, fills, buttons, icons, markers, cards, previews, and state backgrounds. |
| `Design/VisualLockLayered/<SurfaceId>/generated_one_go/layers_contact_sheet.png` | Review sheet showing every separated layer. |
| `Design/VisualLockLayered/<SurfaceId>/layer_manifest.json` | Layer roles, sprite import settings, 9-slice border hints, Unity destinations, and object bindings. |
| `Design/VisualLockLayered/<SurfaceId>/README.md` | Screen-specific notes, source target, validation state, and copy/import instructions. |
| `Design/VisualLockLayered/<SurfaceId>/copy_layers_to_unity.py` | Dry-run-first helper for staging layers into `Assets/Game/Art/UI/Generated/...` without overwriting current UI assets by default. |

Layering rules:

- TMP text must remain live text; do not bake labels, values, mission names, or reward counts into reusable sprites.
- A frame sprite must not include content art, icons, text, or button labels.
- A button/card state background must not include the icon or label.
- Content art such as mission previews, portraits, minimaps, and store item art must be separated from its frame.
- Resource and reward icons must use canonical names from `WarlineCapture_Economy_Reward_Design.md`.
- Alpha and 9-slice behavior must match the SCN-08 `layer_manifest.json` pattern.
- Each layer pack must include a contact sheet before any Unity import work starts.

P0 package status on 2026-05-05:

| Surface | Package Path | Status |
|---|---|---|
| SCN-14 Store / Command Exchange | `Design/VisualLockLayered/SCN-14_StoreCommandExchange` | High-end regenerated target, alpha atlas, layer contact sheet, and 28 extracted candidate layer PNGs generated; Unity import/crop QA still pending. |
| SCN-19 Armory | `Design/VisualLockLayered/SCN-19_Armory` | Final high-end target, alpha atlas, layer contact sheet, manifest, copy helper, and separated implementation PNGs generated. |
| POP-09 Ability / Upgrade Detail | `Design/VisualLockLayered/POP-09_AbilityUpgradeDetail` | Final high-end target, alpha atlas, layer contact sheet, manifest, copy helper, and separated implementation PNGs generated. |
| SCN-05 Saga Map | `Design/VisualLockLayered/SCN-05_SagaMap` | Chapter 1 route-ready layer pack generated; Unity prefab, Main Menu route, shell coverage, focused tests, and 16:9 / 20:9 captures complete. |
| SCN-06 Mission Briefing | `Design/VisualLockLayered/SCN-06_MissionBriefing` | Chapter 1 Breach Assault route-ready layer pack generated; Unity prefab, Start Mission route, shell coverage, focused tests, and 16:9 / 20:9 captures complete. |
| SCN-07 Loadout / Squad Prep | `Design/VisualLockLayered/SCN-07_LoadoutSquadPrep` | Route-ready layer pack generated; Unity prefab, Mission Briefing route, Deploy-to-Match route, shell coverage, focused tests, and 16:9 / 20:9 captures complete. |
| SCN-10 Unit Command / Command Wheel | `Design/VisualLockLayered/SCN-10_UnitCommandWheel` | Overlay layer pack generated; Unity `CommandWheelCanvas` implemented inside `Screen_MatchOverlay`, Special-button open/close behavior, focused tests, and 16:9 / 20:9 captures complete. |
| SCN-03 Commander Profile | `Design/VisualLockLayered/SCN-03_CommanderProfile` | Designed-unavailable route shell layer pack generated; Unity prefab, Main Menu Profile route, shell coverage, focused tests, and 16:9 / 20:9 captures complete. |
| SCN-11 Operation Dashboard | `Design/VisualLockLayered/SCN-11_OperationDashboard` | Designed-unavailable route shell layer pack generated; Unity prefab, Main Menu Operation route, shell coverage, focused tests, and 16:9 / 20:9 captures complete. |
| SCN-12 District Detail / Actions | `Design/VisualLockLayered/SCN-12_DistrictDetailActions` | Designed-unavailable route shell layer pack generated; Unity prefab, shell coverage, focused tests, and 16:9 / 20:9 captures complete. |
| POP-05 Mission Result | `Design/VisualLockLayered/POP-05_MissionResult` | Layered regeneration package scaffolded; high-end generated target and layer PNGs still pending. |

## Chapter 1 Mission Surface Coverage

| Mission | Required Gameplay | Required UI Surfaces | Coverage |
|---|---|---|---|
| M01 First Contact | Select, move, attack, objective/result flow. | SCN-05, SCN-06, SCN-08, POP-05, POP-07, PREFAB-01, PREFAB-02. | Covered. |
| M02 Establish The Base | Build, spend, produce, base defense lite. | SCN-05, SCN-06, SCN-08, SCN-09, POP-03, POP-05, POP-07, PREFAB-01, PREFAB-02, PREFAB-03. | Covered. |
| M03 Radar Warning | Threat warning, defense prep, convoy stop, breach prevention. | SCN-05, SCN-06, SCN-08, SCN-09, POP-01, POP-05, POP-07, PREFAB-01, PREFAB-02, PREFAB-03. | Covered. |
| M04 Airlift | Transport, extraction, landing-zone safety. | SCN-05, SCN-06, SCN-07, SCN-08, SCN-10, POP-01, POP-05, POP-07, PREFAB-01, PREFAB-02. | Covered. |
| M05 Breach Assault | Combined arms, breach route, fortified core, major unlock. | SCN-05, SCN-06, SCN-07, SCN-08, SCN-10, POP-01, POP-04, POP-05, POP-07, PREFAB-01, PREFAB-02. | Covered. |

## Element Coverage Check

| Gameplay Need | UI Element Contract | Status |
|---|---|---|
| Chapter selection and mission progression | SCN-05 chapter selector, mission nodes, selected/locked/completed state, chapter rewards. | Covered. |
| Mission, scenario, and map identity | SCN-05 node data and SCN-06 Level / Map preview. | Covered after audit update. |
| Required objectives and star goals | SCN-06 objective/star rows, SCN-08 objective panel, PREFAB-01 tracker, POP-05 result stars. | Covered. |
| Runtime objective/result data | `ChapterOneMissionCatalog`, `ObjectiveManager`, `MissionResultBuilder`, `MissionResultData`, `SagaProgressStore`, and `SaveService`. | Foundation implemented and covered by focused EditMode tests; tactical match-end trigger still needs gameplay wiring. |
| Enemy intel and threat strength | SCN-06 enemy intel tiles, POP-01 threat alert, SCN-08 threat feed. | Covered. |
| Reward preview and deterministic grants | SCN-06 reward tiles, POP-04 unlock, POP-05 reward grid. | Covered; reward types must come from `WarlineCapture_Economy_Reward_Design.md`. |
| Ability/upgrade inspection | POP-09 target id, unlock moment, availability, effect rows, parts/GearModule progress, disabled reason, and source link. | Covered. |
| Armory progression inspection | SCN-19 category rail, owned roster, upgrade tracks, parts, Gear Modules, selected-item inspection, upgrade CTA disabled reason. | Covered. |
| Build and production | SCN-09/PREFAB-03 category tabs, item rows, queue, capacity, Rush Ticket rule, POP-03 placement. | Covered. |
| Transport, extract, rope drop | SCN-10 command wheel segments, SCN-07 mission restrictions/loadout, SCN-08 selected unit/squad tray. | Covered with hidden `CommandWheelCanvas` opened from the HUD Special command. |
| Breach command | SCN-10 breach segment and objective tracker target state. | Covered. |
| Civilian safety and district consequence | SCN-06 star goals, SCN-08 objective tracker, POP-05 consequence row. | Covered after audit update. |
| Persistent operation meters/actions | SCN-11 dashboard, SCN-12 district actions, POP-02 confirm raid, POP-06 end-of-day report, POP-08 intel reveal. | Service foundation implemented in `OperationService`; shells are still awaiting live binding to district state and action requests. |
| Pause/restart/exit safety | POP-07 pause/options. | Covered. |

## Nonblocking Follow-Up Targets

These are not missing for Chapter 1 gameplay implementation, but they should be tracked before the related features ship:

- SCN-02 target path should remain indexed as Main Menu unless a later cleanup creates an `SCN-02_MainMenu` alias folder.
- SCN-14 Store / Command Exchange has its target under `Design/Monetization/Images`, not under `Design/VisualLock`. This is acceptable because the store is owned by monetization docs, but implementation plans should reference that path directly.
- SCN-15 Inbox, SCN-16 Events, SCN-17 Ranking, and SCN-18 Command Feed remain designed-unavailable route contracts in `WarlineCapture_UIUX_Gameplay_Element_Alignment.md`; they do not yet require full visual-lock targets for the Chapter 1 surface set.
- SCN-19 Armory is now a full target/layer-pack surface, but its upgrade CTAs remain disabled until inventory, upgrade, persistence, and validation services exist.
- SCN-08 layered source names such as money/crate/population must be mapped to canonical runtime resources before import: Credits, Materials, Fuel, Build Capacity, or mission-specific supply labels.

## Acceptance Gate

Before a Chapter 1 UI surface is accepted in Unity:

- It must use the canonical visual target listed in `WarlineCapture_UIUX_Mockup_To_Canvas_Conversion_Plan.md` or the SCN-14 monetization target for store work.
- It must bind all visible text, counters, icons, rows, cards, meters, and buttons to the gameplay data listed in `WarlineCapture_UIUX_Gameplay_Element_Alignment.md`.
- It must have no silent inert controls. Locked, disabled, read-only, dev-only, and designed-unavailable states must be visually clear and backed by copy/data.
- It must use canonical economy resources and rewards from `WarlineCapture_Economy_Reward_Design.md`.
- It must pass the target-to-canvas workflow in `WarlineCapture_UIUX_Target_To_Canvas_Workflow_Guide.md`.

## Final Validation

Validation run on 2026-05-05:

| Check | Result |
|---|---|
| Expected target count | 27 targets: SCN-01 through SCN-14, SCN-19, POP-01 through POP-09, and PREFAB-01 through PREFAB-03. |
| Missing target files | 0 missing. |
| Target dimensions | All checked target PNGs remain 1672 x 941. |
| Chapter 1 required surfaces | Covered by SCN-05, SCN-06, SCN-07, SCN-08, SCN-09, SCN-10, POP-01, POP-03, POP-04, POP-05, POP-07, PREFAB-01, PREFAB-02, and PREFAB-03. |
| Store/economy visual terms | SCN-14 high-end target regenerated with canonical resource/reward language; layer atlas and extracted candidate layers created. |
| Armory and ability/upgrade detail | SCN-19 and POP-09 high-end targets and final layered packs created with canonical ids, resources, availability, unlock, disabled-state, and config-source rules. |
| Saga visual terms | SCN-05, SCN-06, and SCN-07 have route-ready layered packs and Canvas prefabs. SCN-07 remains a functional route-ready implementation until a fully mission-specific high-end target is regenerated. |
| Tactical command overlay | SCN-10 has a layered pack and hidden `CommandWheelCanvas` implementation with tests and captures. SCN-09 remains covered by hidden `BuildDrawerCanvas` in `Screen_MatchOverlay`. |
| Result/reward visual terms | Needs high-end layered regeneration for POP-04 and POP-05 so canonical reward names and consequence reporting are reflected at target quality. |
| Remaining non-core routes | SCN-03 Commander Profile, SCN-11 Operation Dashboard, SCN-12 District Detail / Actions, SCN-15 Inbox, SCN-16 Events, SCN-17 Ranking, and SCN-18 Command Feed are designed-unavailable route shells until backing services are implemented. |
