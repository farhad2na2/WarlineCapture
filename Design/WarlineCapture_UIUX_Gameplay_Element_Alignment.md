# WarlineCapture UI/UX Gameplay Element Alignment

Date: 2026-05-21

## Purpose

This document connects every planned UI element to gameplay design so screens cannot drift into decorative or mismatched UI. It aligns:

- `WarlineCapture_Gameplay_Features_High_Level_Spec.md`
- `WarlineCapture_Gameplay_Features_Detailed_Spec.md`
- `WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md`
- `WarlineCapture_Level_And_Mission_Content_Plan.md`
- `WarlineCapture_Combat_Catalog_And_Upgrade_Design.md`
- `BalanceConfigs/WarlineCapture_Combat_Balance_Config_v0_1.json`
- `VisualConfigs/WarlineCapture_Combat_Visual_Config_v0_1.json`
- `WarlineCapture_UIUX_Implementation_High_Level_Spec.md`
- `WarlineCapture_UIUX_Implementation_Detailed_Spec.md`
- `WarlineCapture_Economy_Reward_Design.md`
- `WarlineCapture_Visual_Feedback_VFX_Recommendations.md`
- `WarlineCapture_FTUE_And_Command_Assistant_Design.md`
- `WarlineCapture_3D_SingleMap_Gameplay_Direction.md`
- `WarlineCapture_M01_FirstContact_Production_Contract.md`

## Global Rule

Every visible UI element must have a `UIElementGameplayContract` before implementation.

Mission-facing UI must use the current content terminology:

```text
Mission -> ScenarioSetup -> OperationMap
```

The Campaign map selects a Mission node, Mission Briefing previews the Mission and its referenced ScenarioSetup, and HUD/result surfaces bind to the active ScenarioSetup and OperationMap runtime data. Do not use `Level` as a synonym for a player-facing Mission label.

For mission, district, result, reward, and tactical HUD surfaces, the contract must also preserve the gameplay north star in `WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md`: tactical command, city pressure, civilian safety, district consequence, readable mobile RTS feedback, and fair progression. Mission-specific surfaces should use `WarlineCapture_Level_And_Mission_Content_Plan.md` to confirm objectives, star goals, reward preview, consequence text, and validation states.

Assistant and tutorial UI must follow `WarlineCapture_FTUE_And_Command_Assistant_Design.md`. ARIA surfaces must be data-bound to tutorial state, recommendation state, route context, objective state, Operation state, and explicit control ownership. Assistant takeover controls must always expose a visible cancel/resume affordance.

Any UI element that displays a unit, building, ability, skill, upgrade, unlock part, portrait, icon, or tier badge must resolve gameplay values from `BalanceConfigs/WarlineCapture_Combat_Balance_Config_v0_1.json` and visuals from `VisualConfigs/WarlineCapture_Combat_Visual_Config_v0_1.json`. UI must not bake cost, damage, HP, cooldown, or tier values into generated art.

Required fields:

| Field | Meaning |
|---|---|
| `ElementId` | Stable id used by prefab/controller/tests. |
| `SurfaceId` | Screen, popup, or prefab that owns the element. |
| `GameplayPurpose` | Why the element exists for gameplay, UX, or feedback. |
| `RouteOrEffect` | Route, modal, state mutation, command issued, local display state, or read-only feedback. |
| `GameplayData` | Config/state/service it reads or writes. |
| `EnableRule` | The exact enabled, disabled, locked, hidden, read-only, decorative, or designed-unavailable state. |
| `Feedback` | Toast, tooltip, modal, sound, error, selected state, result update, motion, or VFX. Use `WarlineCapture_Visual_Feedback_VFX_Recommendations.md` for shared feedback patterns and `WarlineCapture_Audio_Design_Guidelines.md` for paired audio event ids. |

If an element ships before its full runtime system, it must still have a completed design and one of these explicit implementation states:

- `Locked`: feature exists in design but player has not unlocked it.
- `DesignedUnavailable`: feature has a completed gameplay design but is disabled in the current build phase.
- `DevOnly`: debug or internal control; hidden in release builds.
- `ReadOnly`: selectable-looking target is intentionally non-interactive.

Do not ship silent inert UI elements, including buttons, icons, cards, badges, meters, labels, images, maps, or panels.

## UI Element Type Rules

These rules apply to visible UI elements even when they are not clickable.

| Element Type | Required Gameplay Alignment |
|---|---|
| Screen title / route label | Must identify the current route, mode, mission, popup, or gameplay state. |
| Header/resource counter | Must bind to profile, wallet, match resources, operation resources, or explicit disabled state. Use only the canonical names in `WarlineCapture_Economy_Reward_Design.md`. |
| Key art / background art | Must represent the active route or gameplay context and follow the 3D single-map / command-base direction for gameplay-facing content. |
| Icon | Must map to a real action, resource, status, category, reward type, or explicit decorative role. |
| Badge / lock / notification marker | Must bind to unread count, lock reason, rarity, warning severity, reward claim state, or unlock state. |
| Meter / progress bar | Must bind to a named value such as XP, health, readiness, trust, threat, heat, capacity, timer, loading progress, or objective progress. |
| Card / tile | Must represent a mode, mission, unit, gear item, reward, district, warning, objective, squad, or store item with a real backing config/state. |
| List row | Must be generated from data or shown as an authored empty state with a gameplay reason. |
| Map / minimap / district region | Must bind to `OperationMapId`, operation-map metadata, mission node config, district state, camera/minimap state, or operation event data. |
| Modal scrim / frame | Must communicate blocking state and input ownership. Decorative chrome must not receive raycasts. |
| Button group / tabs / segmented control | Must have a selected state and a mapped value in the ViewModel or config. |
| Slider / stepper / dropdown / toggle | Must write a typed setting/config field or be disabled with a reason. |
| Tooltip / detail affordance | Must expose supporting gameplay data, not duplicate vague marketing copy. |

## Global Non-Interactive Element Contracts

| Element | Gameplay Purpose | Gameplay Data | State / Validation Rule |
|---|---|---|---|
| App shell frame | Establish safe mobile landscape route shell. | Current route, safe area. | Decorative chrome only; no raycasts unless it owns modal blocking. |
| Screen title | Confirm current screen/mode/popup. | Route id, localized string key. | Must match route and never be baked into background art. |
| Header profile block | Show account identity and progression. | `PlayerProfileState`, `CommanderProgression`. | Shows default profile only if save data is missing. |
| Resource strip | Show wallet/match/operation resources. | `ResourceWallet`, canonical resource definitions, `FactionResources`, `OperationState.ResourceState`. | Main Menu uses Credits, Supplies, and Command. Runtime/economy internals may map these to existing wallet fields until configs are renamed. |
| Mode card image | Preview route gameplay fantasy. | `GameModeDefinition`, art id. | Gameplay-facing art follows 3D command-base / 3D operation-map direction. |
| Mission/key art image | Preview scenario content. | `MissionConfig.OperationMapId`, `ScenarioSetup.OperationMapId`, planning camera/key-art id. | Replaceable per mission; no baked objectives/rewards text. Art may show the 3D town/base context, but interactive buildings/objectives must remain runtime/state data. |
| Objective row | Explain active requirement. | `ObjectiveConfig` or `ObjectiveRuntimeState`. | Required, bonus, complete, and failed states must be visually distinct. |
| Star goal row | Explain bonus scoring. | `StarGoalConfig` or `StarGoalState`. | Must be visible before and after mission. |
| Reward icon/tile | Preview or show deterministic grant. | `RewardConfig`, `RewardItemConfig`, `RewardGrantResult`. | Uses only canonical reward types from `WarlineCapture_Economy_Reward_Design.md`; no hidden odds or unexplained containers. |
| Unit/building/gear thumbnail | Identify selectable gameplay object. | Unit/building/gear config id and art id. | Locked/unavailable state must show reason. |
| Warning row/icon | Communicate threat or operation urgency. | `ThreatEvent`, `OperationEvent`. | Severity state required; critical warnings cannot be color-only. |
| Map region/node marker | Represent selectable spatial/progression object. | `DistrictState` or `SagaMissionNodeConfig`. | Locked/current/completed/selected states required. |
| Metric tile/meter | Summarize gameplay state. | Named metric from profile, operation, objective, or result data. | Value, label, and icon must agree. |
| Empty state panel | Explain missing data. | ViewModel empty state reason. | Must not look like a broken/unfinished screen. |

## 3D Operation Map UI Rules

Gameplay-facing map art should represent the same 3D operation world the player enters. UI must treat planning, briefing, minimap, deployment, threat, and battle views as projections or camera states over metadata-backed 3D gameplay.

- Chapter 1 operation-map UI surfaces must follow `WarlineCapture_3D_SingleMap_Gameplay_Direction.md` for `OperationMapId`, planning camera ids, minimap projection ids, objective anchors, route anchors, deployment zones, civilian zones, and runtime marker ownership.
- The first concrete tactical UI validation target is `WarlineCapture_M01_FirstContact_Production_Contract.md`.
- Missing 3D operation controls include selected entity feedback, command mode feedback, world command markers, invalid command toasts, minimap camera jumps, and metadata-backed build placement.
- Planning/zoomed-out art is a camera or command-table representation of the same operation map. It must not imply a separate strategic map product layer.
- Build placement UI must show approved sockets/pads/zones clearly in 3D space.
- Invalid placement feedback must explain why a socket or zone is unavailable.
- Mission previews may show roads, plazas, and empty pads, but not baked interactive buildings/objectives.
- Minimap and tactical markers must come from metadata/runtime state, not from baked map pixels.
- Destruction state belongs to runtime building entities and VFX overlays, not to static background art.

## Screen Element Coverage Matrix

Every listed element must be data-bound, explicitly decorative, authored empty state, locked, disabled, read-only, or designed-unavailable.

| Surface | Required Visible Elements | Required Data Alignment |
|---|---|---|
| SCN-01 Splash | Logo/emblem, loading progress bar, status text, tip text, background/key art. | Brand asset, loading progress, loading tips, next route. |
| SCN-02 Main Menu | Profile block, level badge, resource strip, mode card images/titles/subtitles, side-nav icons, notification badges, footer/status strip. | Profile, Credits, Supplies, Command, mode definitions, unread counts, live event state, social feed state. |
| SCN-03 Commander Profile | Portrait, name, alliance line, level badge, XP bar, tab row, stat tiles, reward track nodes, badges/locks. | Profile, progression, account stats, unlock state, reward track state. |
| SCN-04 Settings | Category tabs, setting rows, sliders, toggles, dropdowns, segmented controls, reset/apply/footer states. | Settings models, platform capability, accessibility model, localization state. |
| SCN-05 Campaign Map | Chapter selectors, operation/map viewport, route line, mission nodes, node stars, selected/locked badges, difficulty selector, chapter rewards counter. | Campaign progress, chapter config, mission node configs, mission ids, scenario setup ids, operation map ids, star counts, difficulty unlocks, reward thresholds. |
| SCN-06 Mission Briefing | Mission art, operation-map preview, mission title, briefing text, objective rows, star rows, enemy intel tiles, reward strip, CTA state. | Mission config, scenario setup, OperationMapId, PlanningCameraId, MinimapProjectionId, objective configs, star goals, AI profile/intel data, reward config. |
| SCN-07 Loadout | Power line, selected unit cards, support slots, locked slots, gear cards, mission summary, deploy cost. | Player roster, selected loadout, support inventory, gear inventory, mission restrictions, deploy cost. |
| SCN-08 Battle HUD | Objective panel, threat feed, resource bar, squad tray, command bar, build/pause buttons, minimap, selected states. | Objective runtime, threat feed, faction resources, selected units, command capabilities, build catalog, minimap/camera state. |
| SCN-09 Build Drawer | Category tabs, build/production item rows, cost rows, timers, queue panel, capacity meter, rush state, close state. | Allowed build catalog, faction resources, producers, production queue, build capacity, rush rules. |
| SCN-10 Command Wheel | Selected entity card, radial command segments, center icon, disabled segments, context hint. | Selected entity, command capability set, transport state, target/context validation. |
| SCN-11 Operation Dashboard | District map/regions, metric sidebar, daily briefing, active warning rows, resource/day header, bottom action bar. | Operation state, district states, operation metrics, event list, resource state, day/time, action availability. |
| SCN-12 District Detail | District key art, map inset, stat list, intel confidence meter, known threat card, recent activity list, action grid. | Selected district, metrics, intel confidence, threat estimate, recent activity, action availability/costs. |
| SCN-13 Skirmish | Preset selector, AI/rules controls, sliders/steppers/dropdowns/toggles, operation-map preview, special-rule/dev-only group, launch state. | QuickGameConfig or SkirmishConfig, AI profiles, rule configs, operation map definitions, debug permission, scenario validation. |
| SCN-14 Command Exchange | Store category tabs, product cards, selected product detail, deterministic contents, disabled purchase/restore states. | Store catalog, wallet, product config, receipt state, reward config, target item ids. |
| SCN-19 Armory | Roster category rail, Owned/Upgrade/Parts/Gear tabs, unit/building/support cards, lock badges, inspection panel, ability list, upgrade track, source links. | Prefab catalog display names/descriptions, combat catalog ids, visual catalog ids, unlock state, item level/tier, ability configs, upgrade track configs, BlueprintParts, GearModule inventory. |
| POP-01 Threat Alert | Warning icon, threat title/body, ETA/route row, strength meter, jump/close buttons. | Threat event id, type, ETA, route, estimated strength, world position. |
| POP-02 Confirm Raid | District/target thumbnail, intel confidence meter, collateral risk meter, warning text, cost row, buttons. | District id, suspected target, intel confidence, collateral risk, civilian density, raid cost. |
| POP-03 Build Placement | Building preview, footprint label, socket/pad/zone status, cost row, rotate/cancel/confirm buttons. | Building definition, footprint, 3D operation-map building zone, rotation, resources, build validity. |
| POP-04 Reward Unlock | Unlock image, reward title/subtitle, reward icon grid, continue state. | Reward grant result, unlock id, reward item configs, profile/inventory deltas. |
| POP-05 Mission Result | Outcome header, mission metadata, star row, stats grid, objective checklist, reward grid, replay/continue buttons. | MissionResultData, objective results, star results, combat/economy/civilian stats, reward grants. |
| POP-06 End Of Day | Day header, district deltas, trust/stability/threat meters, resource delta row, save status, save/continue button. | Operation day summary, district deltas, operation metrics, resource deltas, save state. |
| POP-07 Pause | Current route/mission line, time row, button stack, destructive/primary states, background identity art. | Pause state, route, current mission, restart availability, save/exit safety. |
| POP-08 Intel Reveal | Evidence cards, evidence icons, confidence delta, archive destination, view/close buttons. | Intel event, evidence items, district id, intel confidence delta, archive route. |
| POP-10 Assistant Takeover | Control ownership banner, ARIA state, cancel/resume affordance, current command intent. | `AssistantControlOwner`, active command plan, player input override state, tutorial/recommendation context. |
| POP-11 Commander Identity | Commander name input, current portrait preview, portrait grid, frame grid, title selector, confirm/cancel controls. | `PlayerProfileSaveData`, commander portrait/frame/title config, unlock state, cosmetic ownership. |
| PREFAB-01 Objective Tracker | Objective rows, star rows, timer/progress meter, frame. | Objective runtime, star goal state, mission timer. |
| PREFAB-02 Squad Tray | Squad cards, portraits, HP bars, selected outline, status icons, transport badges. | Selected squad set, unit health, status effects, transport state. |
| PREFAB-03 Build Drawer | Category tabs, item grid, lock states, costs, queue rows, capacity/resource strip. | Build catalog, allowed categories, resources, queue, unlock state. |
| PREFAB-04 Assistant Button | Persistent ARIA entry point, recommendation status, critical warning state, muted/takeover state. | Assistant service state, assistance level, recommendation availability, critical alert state, control ownership. |
| PREFAB-05 Assistant Panel | Recommended next actions, explanations, objectives, city context, controls help, Show Me / Do It / Stop buttons. | Assistant recommendations, active route, mission/objective state, Operation state, selected entity state, executable command plans. |
| PREFAB-06 Tutorial Card | Contextual guided instruction, speaker portrait, localized text, target highlight, skip/show/do controls. | `TutorialDirector`, `TutorialStepDefinition`, completion rule, assistance level, localization and VO ids. |
| PREFAB-07 Tutorial Highlight | UI/world target ring, path preview, pulse state, blocked-action feedback. | Highlight targets from tutorial steps and recommendations; world targets resolve through typed gameplay references, not screen coordinates. |

`WarlineCapture_AssistantPanel_M01_Implementation_Contract.md` is the concrete M01 handoff for `PREFAB-05 Assistant Panel`, including required element ids, recommendation state ids, runtime data fields, Show Me / Do It / Stop boundaries, player-input cancellation, and `BattleHudGameplayBridge` integration. This alignment table stays high level; implementation tests should use the M01 contract for exact prefab and runtime expectations while treating 3D single-map direction as the current product target.

## App Shell And Shared Controls

| Control | Gameplay Purpose | Route Or Effect | Gameplay Data | Enable Rule |
|---|---|---|---|---|
| Back | Return to previous safe route without losing state. | `WarlineCaptureRouter.Back()` or caller route. | Route stack, unsaved state marker. | Enabled when previous route exists; asks confirmation if match/progress may be lost. |
| Home | Return to Main Menu hub. | Route to SCN-02, with confirmation from mission/loadout/operation flows. | Route stack, save status. | Disabled during blocking saves or loading. |
| Gear/Settings | Adjust player-facing settings. | Opens SCN-04 or settings modal. | Settings models. | Always available except during critical modal locks. |
| Close X | Dismiss modal/drawer safely. | Close current modal/drawer. | Modal state, pending action. | Disabled if save/purchase/critical action is in progress. |
| Primary CTA | Advance the current gameplay flow. | Screen-specific route or state mutation. | Screen ViewModel and validation state. | Enabled only when required data is valid. |
| Secondary CTA | Cancel, replay, retry, inspect, or route back. | Screen-specific. | Screen ViewModel. | Must not silently discard progress. |

## Legacy Migration Controls

These controls exist in the current practical UI and must remain deliberately handled until the routed app shell replaces them.

| Control | Gameplay Purpose | Route Or Effect | Gameplay Data | Enable Rule |
|---|---|---|---|---|
| `Button_Game` | Development fallback to current tactical match. | Calls current no-argument `GameBootstrap.BeginGameplay()` path. | Current scene defaults and AI runtime state. | `DevOnly` after Skirmish launch flow exists; hidden in release builds. |
| `Button_Stats` | Inspect current tactical/camp stats. | Opens legacy stats panel until profile/result stats replace it. | Runtime stats/current camp state. | `DevOnly` once Commander Profile owns account stats. |
| Legacy `Button_Back` | Return within old panels. | Legacy panel navigation. | Current legacy panel state. | Kept only while legacy panels remain. |
| Legacy `Button_Settings` | Open current practical settings. | Routes to new SCN-04 when available. | Settings/runtime state. | Migrates to SCN-04; do not expand legacy settings surface. |
| Camp category buttons | Legacy camp/build filters. | Filter current camp/build views. | Existing camp/build catalogs. | Replace with SCN-09/PREFAB-03 build categories over time. |

## SCN-01 Splash / Loading

| Control | Gameplay Purpose | Route Or Effect | Gameplay Data | Enable Rule |
|---|---|---|---|---|
| Optional Skip/Continue | Let player advance after required loading is complete. | Route to SCN-02 Main Menu. | Loading progress, minimum splash time, next route. | Hidden by default; enabled only after required assets and save data are ready. |

## SCN-02 Main Menu / Mode Select

| Control | Gameplay Purpose | Route Or Effect | Gameplay Data | Enable Rule |
|---|---|---|---|---|
| Campaign card | Enter curated campaign progression. | Route to SCN-05. | `SagaProgress` or Campaign progress, `ChapterConfig`, `ModeUnlockState`. | Enabled when campaign data exists; `DesignedUnavailable` state opens a modal explaining Campaign unlock path. |
| Operations card | Enter long-running district operation. | Route to SCN-11. | `OperationState`, `DistrictState[]`, `ModeUnlockState`. | Enabled when Operations save/default state exists; `DesignedUnavailable` state opens a modal explaining Operations unlock path. |
| Skirmish card | Configure and launch skirmish. | Route to SCN-13. | `QuickGameConfig` or Skirmish config, `AIProfileConfig[]`, last-used setup. | First fully playable mode path. |
| Profile | Inspect account progression and reward track. | Route to SCN-03. | `PlayerProfileState`, `CommanderProgression`, `AccountStats`. | Enabled after profile save/default profile exists. |
| Inbox | Read system messages, reward claims, tutorial notices, and Operation reports. | Route/modal `SCN-15 Inbox`. | Inbox/message service, reward claim queue, operation report queue. | `DesignedUnavailable` shows empty Inbox with message categories and no silent button. |
| Store | Open Command Exchange. | Route `SCN-14 Command Exchange`. | Wallet, catalog, purchase/receipt service, `RewardService`. | `DesignedUnavailable` shows the designed Command Exchange shell with purchases disabled and restore hidden. |
| Events | Open scheduled event operations and challenge modifiers. | Route/modal `SCN-16 Events`. | `LiveEventState`, event configs, reward configs. | `DesignedUnavailable` shows the event calendar empty state and next event rule. |
| Ranking | Open local/account rankings. | Route/modal `SCN-17 Ranking`. | Account stats, ranking categories. | `DesignedUnavailable` shows local stats ranking categories without network leaderboard dependency. |
| Resource plus | Explain/acquire the selected resource. | Opens wallet/resource detail or Store category. | `ResourceWallet`, canonical resource definitions, economy definitions. | Enabled only for defined wallet resources; no invented resource names in UI. |
| Chat/Social | Social/status feed and system feed. | Opens `SCN-18 Command Feed`. | Local system feed, social provider integration state. | `DesignedUnavailable` shows local system feed entries only. |
| Commander shortcut | Fast profile access. | Route to SCN-03. | `PlayerProfileState`. | Same as Profile. |

Alignment note: the Main Menu resource strip is `Credits`, `Supplies`, and `Command`.

## SCN-03 Commander Profile

| Control | Gameplay Purpose | Route Or Effect | Gameplay Data | Enable Rule |
|---|---|---|---|---|
| Back | Return to caller. | Router back. | Route stack. | Enabled unless profile save is in progress. |
| Gear | Account/profile settings. | Opens settings/account options. | Settings, account/profile data. | Enabled when account options exist; otherwise routes to SCN-04. |
| Overview tab | Show summary stats. | Switch local tab. | `PlayerProfileState`, `AccountStats`. | Always enabled. |
| Upgrades tab | Show unit/building/support upgrade state. | Switch local tab. | `PlayerInventory`, `UnlockState`, upgrade configs. | Locked until upgrade data exists. |
| Stats tab | Show lifetime performance. | Switch local tab. | `AccountStats`. | Enabled after profile default exists. |
| Badges tab | Show earned identity cosmetics. | Switch local tab. | Badge/cosmetic ownership. | Enabled; may show empty state. |
| Reward track nodes | Inspect/claim deterministic rewards. | Open reward detail or claim via `RewardService`. | `RewardTrackProgress`, `RewardConfig`. | Claim enabled only for earned unclaimed nodes. |

## SCN-04 Settings / Accessibility

| Control | Gameplay Purpose | Route Or Effect | Gameplay Data | Enable Rule |
|---|---|---|---|---|
| Back | Return to caller. | Save/apply valid settings then route back. | Settings service. | Enabled after pending writes complete. |
| Settings category tabs | Switch settings category. | Local tab state. | Settings ViewModel. | Always enabled for implemented categories. |
| Audio sliders | Tune playback mix. | Persist volume settings. | Audio settings model. | Enabled when audio service is available; otherwise preview-only. |
| Graphics quality / FPS | Tune performance/readability. | Persist quality and target framerate. | Graphics settings model. | Enabled for supported platform options. |
| Control settings | Tune input behavior. | Persist control model. | Controls settings model. | Enabled as controls are implemented. |
| Notification toggles | Control noncritical alerts. | Persist notification settings. | Notification settings model. | Does not suppress critical threat/objective alerts. |
| Accessibility toggles/dropdowns | Improve readability. | Apply large text, high contrast, colorblind mode, reduced motion. | Accessibility settings model. | Always visible; unsupported options disabled with explanation. |
| Language dropdown | Select localization. | Persist language. | Localization settings. | Enabled for English in the first authored build; additional languages use `DesignedUnavailable` rows with locked labels. |
| Reset | Restore category/default settings. | Resets visible settings category after confirmation if destructive. | Settings service defaults. | Enabled when category has changed values. |
| Apply | Commit pending settings changes. | Persists and applies settings. | Settings service. | Enabled only when pending values are valid and changed. |

## SCN-05 Campaign Map

| Control | Gameplay Purpose | Route Or Effect | Gameplay Data | Enable Rule |
|---|---|---|---|---|
| Back | Return to Main Menu. | Route to SCN-02. | Route stack. | Enabled. |
| Chapter dropdown | Select available chapter. | Reload map nodes. | Campaign progress; existing `SagaProgress` may remain as compatibility storage. | Only unlocked chapters selectable. |
| Chapter name dropdown | Select region/chapter variant if supported. | Reload map and node list. | `ChapterConfig`. | Disabled if only one chapter region exists. |
| Mission node | Select mission. | Opens SCN-06 for playable node. | Campaign mission node config, Campaign progress. | Locked nodes show requirements; completed nodes remain replayable. |
| Map preview/node art | Preview the mission's 3D operation context. | Updates selected node panel and briefing handoff. | `ScenarioSetup.OperationMapId`, `MissionConfig.PlanningCameraId`, key-art id. | Read-only; shows authored empty art state only if a mission intentionally has no unique preview. |
| Difficulty dropdown | Choose mission difficulty. | Writes launch difficulty modifier. | Difficulty config, mission rules. | Only unlocked/allowed difficulties selectable. |
| Chapter Rewards | Inspect/claim star-threshold rewards. | Opens reward detail/POP-04 claim. | `RewardConfig`, Campaign progress. | Claim enabled only at met thresholds. |

## SCN-06 Mission Briefing

| Control | Gameplay Purpose | Route Or Effect | Gameplay Data | Enable Rule |
|---|---|---|---|---|
| Back | Return to source map/dashboard/setup. | Route to source route. | `GameLaunchPayload.SourceRoute`. | Enabled. |
| Start Mission | Enter loadout or match. | Route to SCN-07 or Match depending mission rules. | `MissionConfig`, `ScenarioSetup`, `ObjectiveConfig`, `RewardConfig`. | Enabled when mission config and scenario setup are valid. |
| Operation map preview | Explain where the mission plays and verify the selected ScenarioSetup. | Opens map detail tooltip or remains read-only. | `ScenarioSetup.OperationMapId`, `MissionConfig.PlanningCameraId`, minimap projection id. | Always visible for Campaign missions; uses a designed empty state only for generated modes without unique preview art. |
| Enemy intel tile | Explain expected threat. | Opens threat detail tooltip/modal. | `AIProfileDefinition`, enemy intel data. | Read-only tile still shows faction, unit family, strength, and detection confidence. |
| Reward tile | Explain reward source and grant rule. | Opens reward detail tooltip/modal. | `RewardConfig`. | Read-only tile still lists exact canonical reward type, amount, and grant rule. |

## SCN-07 Loadout / Squad Prep

| Control | Gameplay Purpose | Route Or Effect | Gameplay Data | Enable Rule |
|---|---|---|---|---|
| Back | Return to briefing. | Route to SCN-06. | Source mission. | Enabled. |
| Home | Abandon setup and return to hub. | Confirmation then SCN-02. | Unsaved loadout state. | Requires confirmation. |
| Unit card | Inspect/change selected squad or vehicle. | Open roster selector. | `PlayerUnitRoster`, `SelectedLoadout`, mission restrictions. | Disabled for locked or mission-banned units. |
| Support slot | Choose or inspect tactical support ability. | Open support selector; detail/long-press opens `POP-09`. | `SupportAbilityInventory`, slot unlock state, ability availability spec. | Locked slots show unlock requirement and route to detail instead of failing silently. |
| Gear card | Choose deterministic gear/module or inspect upgrade requirement. | Open equipment selector; detail/long-press opens `POP-09` for linked upgrade tracks. | `GearInventory`, mission restrictions, upgrade availability spec. | Disabled if gear type is incompatible; detail still explains the requirement. |
| Deploy | Launch tactical match. | Build `GameLaunchPayload` and call `BeginGameplay(payload)`. | `SelectedLoadout`, `MissionConfig`, `ScenarioSetup`. | Enabled only when loadout validates and deploy cost is payable. |

## SCN-08 RTS Battle HUD

| Control | Gameplay Purpose | Route Or Effect | Gameplay Data | Enable Rule |
|---|---|---|---|---|
| Squad card | Select/focus combat group. | Updates selection/focus. | `SelectedUnitGroup`, ECS entity refs, health state. | Enabled for alive/available squads. |
| STOP | Cancel movement/active order. | Issues stop command. | `RTSSelectionSystem`, selected units. | Enabled when selection can receive orders. |
| HOLD | Hold position/defensive stance. | Issues hold command. | Selected unit command capability. | Enabled for controllable combat units. |
| MOVE | Enter move target mode. | Awaits map target then issues move. | Pathing/grid state. | Enabled for movable selected units. |
| ATTACK | Enter attack target mode. | Awaits target then issues attack. | Combat capability, target filters. | Enabled for combat-capable selected units. |
| SPECIAL | Use or inspect context ability. | Opens SCN-10, executes selected ability, or opens `POP-09` from detail/long-press. | Command capability set, cooldown/charges, ability availability spec. | Enabled only when a valid special exists; disabled state explains cooldown, charges, lock, mission-ban, or invalid target. |
| Build toggle | Open Build Drawer. | Opens SCN-09/PREFAB-03. | Build catalog, faction resources, mission allowed catalog. | Enabled when mission allows construction/production. |
| Minimap | Camera navigation or expand. | Focus map position or open expanded map. | Minimap state, camera controller. | Enabled once minimap data exists. |
| Pause | Pause match. | Opens POP-07. | Current match route, save/abandon state. | Enabled during active match. |
| Objective row | Show required objective progress and failure state. | Focus target area or open objective detail. | Objective runtime state. | Read-only row still shows progress, owner, threshold, and failure state. |

## SCN-09 Build Drawer / Production

| Control | Gameplay Purpose | Route Or Effect | Gameplay Data | Enable Rule |
|---|---|---|---|---|
| Category tab | Filter build/production catalog. | Local category selection. | `AllowedBuildCatalog`, unlock state. | Enabled when category has visible items. |
| Structure item row | Start placement flow. | Opens POP-03. | `BuildingDefinition`, resources, tech unlock. | Enabled when affordable/unlocked/allowed. |
| Unit item row | Queue production. | Calls production queue API. | Producer building, `FactionResources`, queue state. | Enabled when compatible producer exists and queue has room. |
| Queue cancel X | Cancel queued production. | Removes queue item and refunds per rules. | Production queue. | Enabled for cancelable items only. |
| Rush All | Accelerate queue using Rush Tickets. | Spends Rush Tickets and updates queue. | Queue state, Rush Ticket count, mission rules. | Enabled only in missions that allow queue acceleration; disabled state explains required Rush Tickets. |
| Close X | Return to HUD. | Close drawer. | Drawer state. | Enabled. |

## SCN-10 Unit Command / Command Wheel

| Control | Gameplay Purpose | Route Or Effect | Gameplay Data | Enable Rule |
|---|---|---|---|---|
| Move segment | Issue move command. | Await/select map point then move. | Pathing state. | Movable selected entity only. |
| Attack segment | Issue attack command. | Await/select target then attack. | Combat capability. | Combat-capable entity only. |
| Patrol segment | Issue patrol behavior. | Assign patrol route/area. | Command capability, pathing. | Enabled for selected entities with patrol capability; disabled segment names missing capability. |
| Breach segment | Attack/breach wall or gate. | Issue base breach order. | `BaseBreachOrder`, target wall/gate. | Enabled near valid breach target. |
| Extract segment | Board/extract unit. | Issue transport/extract flow. | Transport state, eligible unit. | Enabled for valid transport/unit pair. |
| Rope Drop segment | Disembark from helicopter. | Issue rope disembark. | Helicopter transport state. | Enabled for transport aircraft with passengers. |
| Stop/Hold segment | Cancel or hold command. | Issues stop/hold. | Selected command capability. | Controllable entity only. |

## SCN-11 Operations Dashboard

| Control | Gameplay Purpose | Route Or Effect | Gameplay Data | Enable Rule |
|---|---|---|---|---|
| Back | Return to Main Menu. | Route to SCN-02 after save check. | `OperationState`, save status. | Disabled during save/end-day resolution. |
| District region | Inspect district state/actions. | Route/open SCN-12. | `DistrictState`. | Enabled for known districts. |
| Active warning row | Inspect or launch urgent event. | Opens warning detail, briefing, or match route. | `OperationEvent`, pending mission. | Enabled for actionable events. |
| Intel Report | Open intel archive. | Route/modal for intel archive. | `IntelArchive`, evidence items. | Enabled once Operation state exists; may show empty state. |
| Black Market | Open Operation supply exchange. | Route to Command Exchange Operation category. | Operation supplies, Credits, Materials, Fuel, Intel, Command Authority, catalog. | `DesignedUnavailable` shows deterministic Operation supply catalog with purchase buttons disabled. |
| Armory | Open loadout/upgrades support. | Route `SCN-19 Armory`. | `PlayerInventory`, upgrade configs, Gear Modules, BlueprintParts. | Opens the Armory screen; upgrade CTAs stay disabled until service, inventory, and validation requirements are satisfied. |
| Command Log | Inspect operation history. | Opens history/log route. | Operation event history. | Enabled when logs exist; otherwise empty state. |
| End Day | Resolve operation simulation. | Runs `OperationSimulationService.EndDay`, saves, opens POP-06. | `OperationState`, district events. | Enabled only when no blocking required action is unresolved. |

## SCN-12 District Detail / Actions

| Control | Gameplay Purpose | Route Or Effect | Gameplay Data | Enable Rule |
|---|---|---|---|---|
| Back | Return to Operation Dashboard. | Route to SCN-11. | Selected district. | Enabled. |
| Patrol | Reduce enemy influence/heat and maybe reveal activity. | Dispatch `OperationActionRequest(Patrol)`. | `DistrictState`, action costs/cooldowns. | Enabled if district can accept action and readiness/cost is available. |
| Drone Scan | Increase intel confidence and maybe reveal evidence. | Dispatch `OperationActionRequest(DroneScan)`, maybe POP-08. | Intel confidence, heat risk. | Enabled if scan resource/cooldown permits. |
| Aid | Improve trust/stability. | Dispatch `OperationActionRequest(Aid)`. | Resources, trust/stability. | Enabled if aid cost is payable. |
| Raid | Attempt tactical/abstract raid. | Opens POP-02 or SCN-06 if already confirmed. | Intel confidence, collateral risk, district threat. | Enabled when enough district intel exists; warning if confidence is low. |
| Repair | Improve infrastructure. | Dispatch `OperationActionRequest(Repair)`. | Infrastructure, materials. | Enabled if damaged infrastructure exists and cost is payable. |
| Evacuate | Reduce civilian risk with tradeoffs. | Dispatch evacuation action/flow. | Civilian density, trust/infrastructure deltas. | Enabled in high-risk districts; shows consequence preview. |
| Build Outpost | Improve security/readiness. | Dispatch build outpost action or outpost setup. | Resources, district security, unlock state. | Enabled if district permits outpost and cost is payable. |

## SCN-13 Skirmish Setup

| Control | Gameplay Purpose | Route Or Effect | Gameplay Data | Enable Rule |
|---|---|---|---|---|
| Back | Return to Main Menu. | Route to SCN-02. | Last-used config save. | Enabled; saves valid config. |
| Preset dropdown | Load recommended AI/rule setup. | Replaces config fields. | `QuickGamePreset` or Skirmish preset. | Enabled when presets exist. |
| Enemy Type dropdown | Select AI profile family. | Writes `QuickGameConfig.EnemyType`. | `AIProfileConfig[]`. | Enabled for implemented profiles only. |
| Enemy Count stepper | Choose 1-3 enemies. | Writes `EnemyCount`. | `AISettingsRuntimeState.EnemyAICount`. | Clamp to supported count. |
| Difficulty dropdown | Choose AI difficulty. | Writes `Difficulty`. | AI difficulty tuning. | Enabled. |
| Economy/pacing controls | Tune starting money, income, build speed, production speed. | Writes QuickGameConfig fields. | AI settings mapper. | Clamp to supported values. |
| Aggression/expansion/target controls | Tune AI behavior. | Writes QuickGameConfig fields. | AI settings mapper. | Enabled for supported profiles. |
| Win Condition dropdown | Select scenario win condition. | Writes `ScenarioWinCondition`. | Win condition config. | Only implemented win conditions selectable. |
| Fog/Intel toggles | Toggle match visibility and intel-reveal rules. | Writes config flags. | Rule config. | Enabled for `IntelReveal`; `FogOfWar` uses `DesignedUnavailable` with locked reason until fog simulation is active. |
| Base recovery/alliances | Advanced match rules. | Writes rule flags. | Rule configs. | `DesignedUnavailable` for player builds; debug variants are `DevOnly`. |
| Cheat/debug toggles | Developer test support. | Writes debug config. | `PlayerDebugPermissions`. | Hidden in release builds. |
| Map selector/preview | Choose operation map preset/art. | Writes `OperationMapId`, planning camera, and minimap projection ids. | Operation map definitions. | Enabled for validated maps only. |
| Launch Mission | Start skirmish. | Creates `GameLaunchPayload(QuickCustom)` or equivalent Skirmish payload and starts match. | `QuickGameConfig` or Skirmish config, `ScenarioSetup`. | Enabled only when config maps to valid scenario. |

## SCN-14 Store / Command Exchange

| Control | Gameplay Purpose | Route Or Effect | Gameplay Data | Enable Rule |
|---|---|---|---|---|
| Back | Return to caller. | Router back. | Caller route. | Enabled unless purchase/receipt flow is active. |
| Category tab | Filter store products. | Local catalog filter. | `StoreCatalog`, product category. | Enabled when category has visible deterministic products. |
| Product card | Inspect product contents. | Select product; ability/upgrade target opens `POP-09`. | Product config, reward item list, target id. | Enabled for inspection even when purchase is disabled. |
| Purchase button | Buy product. | Starts receipt flow. | Wallet, platform store, receipt service, product id. | Disabled until wallet, receipt, catalog, profile persistence, and reward grant services are implemented. |
| Restore purchases | Restore non-consumable entitlements. | Starts platform restore. | Receipt service, entitlement state. | Hidden until platform receipt support exists. |
| Resource plus | Explain/acquire resource. | Opens matching store category or resource detail. | Resource wallet, canonical resource definition. | Enabled only for canonical resources. |

## SCN-19 Armory

| Control | Gameplay Purpose | Route Or Effect | Gameplay Data | Enable Rule |
|---|---|---|---|---|
| Back | Return to caller. | Router back. | Caller route, save state. | Enabled unless inventory save/apply is active. |
| Category rail | Filter roster and upgrade tracks by type. | Local category filter. | Player inventory, resolved item ids, ability/upgrade availability specs. | Enabled when category has owned, locked, or previewable entries. |
| Bottom tab | Switch Owned, Upgrade Tracks, Parts, or Gear Modules. | Local tab switch. | Player inventory, BlueprintParts, GearModule inventory. | Enabled when inventory default state exists. |
| Item card | Select unit, building, ability, or upgrade. | Updates inspection panel. | Combat catalog id, visual catalog id, unlock state, upgrade track id. | Enabled for inspection; locked entries show unlock moment and disabled state. |
| Inspection panel | Show selected unit/building/support identity and readiness. | Local detail update; no route change. | Prefab `displayName`, `description`, role/category, unlock state, item level/tier, owned parts, base stats, ability list, upgrade track id, source route. | Always visible after selection; locked items show source and disabled CTA instead of hiding the detail. |
| Item detail | Explain selected ability, upgrade, or source item in depth. | Opens `POP-09`. | AbilityConfig, UpgradeTrackConfig, target item id, source/unlock data. | Enabled for every ability and upgrade track with an availability spec; unit/building cards use the Armory inspection panel first. |
| Upgrade CTA | Apply upgrade outside active combat. | Calls `UpgradeService` after validation. | UpgradeTrackConfig, cost, inventory, persistence. | Disabled until service exists, costs are payable, target resolves, and active combat mutation is blocked. |
| Source link | Show where item unlocks. | Routes to Saga/Operation/Store source when available. | `unlockMoment`, reward source, caller route. | Enabled when source route is resolvable; otherwise shows read-only source text. |

## Popups

| Surface / Control | Gameplay Purpose | Route Or Effect | Gameplay Data | Enable Rule |
|---|---|---|---|---|
| POP-01 Jump to Threat | Respond to detected threat. | Focus camera/map on threat location. | `ThreatEventId`, `WorldPosition`. | Enabled when threat has valid location. |
| POP-01 Close | Dismiss alert while keeping warning in feed. | Close popup. | Threat feed state. | Enabled unless alert is critical/blocking. |
| POP-02 Confirm Raid | Commit risky raid decision. | Builds operation raid result or tactical mission route. | District intel, collateral risk, raid cost. | Enabled after costs and target are valid. |
| POP-02 Cancel / Close | Abort raid. | Return to SCN-12. | District route. | Enabled. |
| POP-03 Rotate | Rotate building ghost. | Updates placement rotation. | `BuildingDefinition`, grid validator. | Enabled if building supports rotation. |
| POP-03 Confirm | Spend resources/place building. | Calls building placement API. | Footprint, placement cell, resources. | Enabled only on valid placement. |
| POP-03 Cancel | Exit placement mode. | Return to SCN-09/SCN-08. | Placement state. | Enabled. |
| POP-04 Continue | Acknowledge rewards/unlock. | Close and route to caller/result/map/menu. | `RewardGrantResult`. | Enabled after reward grant is resolved. |
| POP-04 Reward icon | Inspect reward. | Tooltip/detail. | `RewardItemConfig`. | Always displays exact reward type, amount, source, and duplicate-conversion rule. |
| POP-05 Replay/Retry | Restart same mission. | Builds payload from original mission/config. | `MissionResultData`, source payload. | Enabled for replayable missions. |
| POP-05 Continue | Apply rewards and route forward. | `RewardService` grant, save, route to source mode. | `MissionResultData`, `RewardGrantResult`. | Enabled after reward grant/save completes. |
| POP-06 Save & Continue | Commit end-day simulation. | Save Operation state and route to SCN-11/next day. | `OperationDaySummary`, save service. | Enabled after summary generation. |
| POP-07 Resume | Resume simulation. | Close pause and unpause. | Pause service. | Enabled if current route is resumable. |
| POP-07 Restart Mission | Reload current mission. | Confirmation then rebuild payload. | `MissionConfigId`, source payload. | Enabled for tactical missions only. |
| POP-07 Options | Open settings. | Opens SCN-04/modal. | Settings. | Enabled. |
| POP-07 Help | Show contextual controls/objectives help. | Opens help modal. | Current route, selected mission/objectives. | Displays mission objective help, command help, and active mode glossary. |
| POP-07 Exit to Main Menu | Abandon route safely. | Confirmation, save/abandon, route SCN-02. | Save status, mission progress. | Requires confirmation if progress may be lost. |
| POP-08 Evidence magnifier | Inspect evidence. | Open evidence detail. | `IntelEvidenceItem`. | Enabled for evidence with detail text. |
| POP-08 View Intel | Open intel archive. | Route/modal to intel archive. | `IntelArchive`, district id. | Always opens archive view filtered to the revealed district/evidence set. |
| POP-08 Close | Return to caller. | Close popup. | Caller route. | Enabled. |
| POP-09 Close | Return to caller without changing selection. | Close popup. | Caller route. | Enabled. |
| POP-09 View Source | Inspect unlock/source surface. | Route to source screen or focus source row. | `unlockMoment`, source route, caller route. | Enabled when source route resolves; otherwise disabled with source text still visible. |
| POP-09 Primary CTA | Apply/claim/use only when caller provides a valid non-combat action. | Calls caller-provided action. | Ability/upgrade config, inventory, charges, cooldown, cost, active route. | Disabled by default; enabled only when lock, resource, mission, cooldown, service, and active-combat rules pass. |

## Reusable Panels

| Prefab / Control | Gameplay Purpose | Route Or Effect | Gameplay Data | Enable Rule |
|---|---|---|---|---|
| Objective row | Track objective progress. | Optional focus target/open detail. | `ObjectiveRuntimeState`. | Read-only unless focus/detail data exists. |
| Star row | Show visible bonus scoring. | Opens star-goal detail tooltip. | `StarGoalState`. | Read-only row still shows goal, threshold, completion, and failure reason. |
| Squad tray card | Select/focus squad. | Selection/focus command. | Selected squad, health, transport state. | Alive and controllable squads only. |
| Squad micro-button | Filter/select/assign group. | Selection helper. | Selection state. | Buttons are visible only for implemented selection helpers; no empty icon slots. |
| Build drawer tab | Filter category. | Local view state. | Build catalog. | Category has visible items. |
| Build drawer item | Place building or queue unit. | Placement/production effect. | Build catalog, resources, unlocks. | Affordable, unlocked, mission-allowed. |
| Build drawer queue row X | Cancel queue item. | Queue cancellation. | Production queue. | Cancelable item only. |

## Mismatch Guardrails

- If a UI element appears in visual mockups but not in gameplay specs, add it here before implementation or remove/disable it from the production prefab.
- If gameplay specs add a new action, add a matching UI contract before creating visible UI elements.
- DesignedUnavailable elements must never look like working gameplay-critical controls without feedback.
- Dev/debug controls must be hidden in release builds.
- Store/monetization controls use the completed Command Exchange design; runtime purchase buttons are disabled until wallet, receipt, catalog, profile persistence, and reward grant services are implemented.
- The active gameplay art direction is 3D single-map; new gameplay-facing thumbnails, maps, minimaps, and key art should not be generated in the old 2D isometric, macro-tile, or generic low-poly/desert direction unless PM explicitly reopens that work.
- Paid or premium controls must not override mission objectives, star goals, or Operation consequences.

## Test Requirements

For every implemented screen or popup, add an EditMode validation that checks:

- Every visible UI object, including `Button`, `Toggle`, `Dropdown`, `Slider`, clickable item, icon, image, label, badge, meter, card, map region, list row, and modal panel has an `ElementId` or stable object name listed in this document or the screen-specific implementation spec.
- Disabled elements show `DesignedUnavailable`, `Locked`, `DevOnly`, or `ReadOnly` feedback.
- Release builds do not expose `DevOnly` controls.
- Primary CTAs are disabled when their required gameplay payload/config is invalid.
- Route buttons target the route listed here.
