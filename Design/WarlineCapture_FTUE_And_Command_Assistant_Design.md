# WarlineCapture FTUE And Command Assistant Design

Date: 2026-05-06

## Purpose

This document designs WarlineCapture's first-time user experience, contextual tutorials, and reusable command assistant. It is grounded in the current Unity project, especially:

- `Assets/Game/Scripts/Campaign/ChapterOneMissionCatalog.cs`
- `Design/SagaChapters/WarlineCapture_Saga_Chapter01_First_Response.md`
- `Assets/Game/Scripts/Operation/OperationService.cs`
- `Assets/Game/Scripts/UI/Shell/WarlineCaptureRouter.cs`
- `Assets/Game/Scripts/UI/Screens/MissionBriefingScreenController.cs`
- `Assets/Game/Scripts/UI/Screens/MatchObjectivePanelController.cs`
- `Assets/Game/Scripts/UI/Screens/OperationDashboardScreenController.cs`
- `Assets/Game/Scripts/UI/Screens/DistrictDetailScreenController.cs`
- `Assets/Game/Scripts/Systems/AISettingsRuntimeState.cs`
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Design/WarlineCapture_Audio_Design_Guidelines.md`
- `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md`
- `Design/WarlineCapture_Command_Offensive_Premise_Alignment.md`
- `Design/WarlineCapture_LargeScale_Grid_Movement_Design.md`
- `Design/WarlineCapture_3D_SingleMap_Gameplay_Direction.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`

## Short Answer

WarlineCapture should not have a one-time tutorial bolted onto Mission 1. It should have a persistent assistant layer that can:

- Teach the first five Campaign missions step by step.
- Recommend what to do now in Campaign, Operations, Mission Briefing, Loadout, and live combat.
- Explain why an action matters to the city, not only how to click it.
- Demonstrate an action by taking temporary controller ownership.
- Stop instantly when the player touches input, cancels, pauses, or disables assistance.
- Continue to work later as a command advisor for Operations and mission mastery.

Recommended assistant identity:

```text
Name: ARIA
Full name: Adaptive Response Intelligence Assistant
Role: WarlineCapture command room assistant assigned to the player's emergency command unit.
Tone: calm, direct, tactical, never cute, never sarcastic.
Visual identity: compact operations officer portrait, radio waveform, city-status badge, recommendation chips.
```

ARIA is not the commander. The player is the commander. ARIA is the staff officer who explains, recommends, and can briefly execute orders when asked.

## Player Role And Story Frame

Player-facing premise:

```text
You are the newly appointed Field Commander of WarlineCapture Response Command.
Hostile factions are embedded across civilian districts, using infrastructure, logistics, and public routes as cover.
Your job is to prepare and execute targeted operations that neutralize those factions without losing civilian trust, infrastructure, or long-term district control.
```

ARIA's story role:

```text
ARIA is the command network's surviving decision assistant. She has city telemetry, district reports, and limited tactical automation rights.
She can show you how command tools work, recommend priorities, and execute simple orders with your permission.
She cannot replace judgment. She follows your command and yields control instantly.
```

Chapter 1 story arc:

| Mission | Player Role Beat | ARIA Teaching Role |
|---|---|---|
| M01 First Contact | First confirmed hostile patrol near a civilian corridor. | Teach selection, move, attack, objectives, result. |
| M02 Establish The Base | Prepare a forward operating point for district operations. | Teach build, placement, production, resources. |
| M03 Radar Warning | Prepare for a detected convoy before it hits the district. | Teach threat alerts, defense timing, radar ping. |
| M04 Airlift | Insert, reinforce, or extract under district pressure. | Teach transport, landing zone, extraction, fuel. |
| M05 Breach Assault | Remove the first fortified hostile node. | Teach breach route, combined arms, high-risk objective. |

Movement teaching note: `Design/WarlineCapture_LargeScale_Grid_Movement_Design.md` does not add extra early FTUE steps. It clarifies the acceptance bar for the existing Chapter 1 lessons: movement only counts as taught when selection state, destination/attack markers, invalid target feedback, current order state, operation-map anchors, and objective/result flow are readable at mobile landscape scale.

## Commander Identity

Because the player is the Field Commander and WarlineCapture already has a Commander Profile route, the player should have a visible commander identity. This should be separate from ARIA. ARIA has her own assistant portrait and voice; the commander portrait represents the player.

### Identity Creation Flow

First launch should include a short identity step after ARIA's welcome and before the first Campaign mission:

```text
ARIA welcome -> Commander Identity -> Choose guidance level -> Campaign highlighted
```

Required fields:

- Commander name.
- Commander portrait.
- Optional commander frame.
- Optional commander title, defaulting to `Field Commander`.

The first implementation should keep the flow short: name plus portrait, with frame/title using defaults. Frames, titles, and badges can expand later through rewards, events, and cosmetics.

### Commander Portrait Picker

The portrait picker should be reachable in two places:

| Entry | Behavior |
|---|---|
| FTUE identity step | Shows 3-6 default portraits, lets the player choose one, then confirms. |
| Commander Profile | Tapping the portrait or edit icon opens the same picker as `POP-11 Commander Identity`. |

Portrait picker rules:

- Always show several free default portraits at first launch.
- Locked portraits remain visible but show the unlock reason.
- Portraits are cosmetic only and never change tactical stats.
- The player can change the selected portrait later without cost.
- If save data has no portrait id, use a default fallback portrait.
- The commander portrait appears in Main Menu, Commander Profile, level-up/reward moments, and optional profile/history surfaces.
- The commander portrait should not appear as ARIA; assistant/tutorial cards use ARIA's own portrait.

Suggested save fields:

```csharp
public string commanderPortraitId = "portrait.commander.default_01";
public string commanderFrameId = "frame.commander.default";
public string commanderTitle = "Field Commander";
```

Suggested portrait ids:

```text
portrait.commander.default_01
portrait.commander.default_02
portrait.commander.default_03
portrait.commander.default_04
portrait.commander.default_05
portrait.commander.default_06
portrait.commander.veteran_responder
portrait.commander.night_ops
portrait.commander.operation_founder
```

Suggested frame ids:

```text
frame.commander.default
frame.commander.iron_guard
frame.commander.operation_stabilizer
frame.commander.founder
```

Unlock guidance:

| Identity Item | Unlock Rule |
|---|---|
| Default portraits | Available immediately. |
| Chapter portraits | Complete Campaign chapter milestones. |
| Operation portraits | Stabilize districts or complete Operation week milestones. |
| Frames and badges | Reward track, events, store cosmetics, or profile milestones. |
| Premium cosmetics | Cosmetic only; no tactical stat effect. |

### POP-11 Commander Identity

Add a reusable identity popup:

```text
POP-11 Commander Identity
  Header: COMMANDER IDENTITY
  CurrentPortraitPreview
  CommanderNameInput
  PortraitGrid
  FrameGrid
  TitleSelector
  ConfirmButton
  CancelButton
```

First vertical slice can ship with:

- current portrait preview
- name input
- six default portrait choices
- default frame only
- confirm/cancel buttons

Later expansion can add frame tabs, badge tabs, title selection, unlock reason tooltips, and cosmetic source labels.

## Design Pillars

| Pillar | Rule |
|---|---|
| Teach by doing | Every tutorial step should require a real game action, not only reading. |
| One new idea at a time | Chapter 1 introduces one primary mechanic per mission. |
| Keep the player in command | ARIA can guide or execute only with clear player permission. |
| Explain consequence | Recommendations must say why an action matters to civilians, districts, rewards, or mission success. |
| Reusable after FTUE | The same assistant system powers hints, Operation advice, warnings, comeback help, and mastery coaching. |
| Data-driven | Tutorial steps and recommendations come from authored data, not hard-coded screen scripts. |
| Interruptible | Any player input during takeover immediately pauses or cancels assistant control. |

## Assistance Levels

Expose these in Settings and in the first tutorial prompt:

| Level | Behavior |
|---|---|
| Full Guidance | ARIA shows tutorial cards, highlights targets, blocks irrelevant actions during critical FTUE steps, and offers `Show Me` / `Do It`. |
| Hints Only | ARIA shows recommendations and optional help without blocking normal input. |
| Minimal | ARIA only surfaces critical alerts, stuck-state recovery, and "Ask ARIA" responses. |
| Off | No proactive tutorial except mandatory system warnings such as save failure or critical input ownership. |

Default for first install: `Full Guidance`.

## Assistant UX

### Persistent Entry Point

Add an always-available assistant button to the app shell and battle HUD:

```text
Button: ARIA
Icon: operations officer/radio waveform
Location: shell header on menus, lower-left or left rail in match HUD
States: idle, recommendation available, critical warning, takeover active, muted
```

Tap behavior:

- Opens the Assistant Panel.
- Shows the best current recommendation first.
- Shows context tabs: `Next`, `Explain`, `Objectives`, `City`, `Controls`.
- In combat, includes `Show Me`, `Do It`, and `Stop` when an executable plan exists.

### Recommendation Chips

Use compact chips instead of long text blocks:

| Chip | Example |
|---|---|
| Primary | `Select rifle squad` |
| Tactical | `Move to marked cover` |
| City | `Protect civilians near Old Market` |
| Economy | `Build Barrack, then queue squad` |
| Risk | `Scan before raid: intel is low` |

Each chip opens a short detail panel:

```text
Recommended: Drone Scan
Why: Intel is 24 in Port Breach. A raid now has high collateral risk.
Effect: Intel +12, Supply -1.
Buttons: Do It, Show Me, Dismiss
```

### Tutorial Cards

Tutorial cards should be small and anchored near the relevant UI or world target. Avoid full-screen interruption except for the first welcome, critical input ownership, or mission result explanation.

Card fields:

- Speaker portrait.
- One short instruction.
- Optional reason line.
- Target highlight.
- Buttons: `Got It`, `Show Me`, `Do It`, `Skip`.

Example:

```text
Select the rifle squad.
They are your first response team. Orders start with selection.
```

### World And UI Highlighting

Use three highlight types:

| Highlight | Use |
|---|---|
| UI ring | Buttons, objective rows, build cards, command wheel segments. |
| World marker | Units, move target, enemy group, build socket, extraction zone. |
| Path preview | Demonstration route, convoy route, breach lane, helicopter path. |

World markers and path previews must resolve through `OperationMapDefinition` metadata, runtime entity ids, or UI element ids. ARIA must not target baked terrain details or raw screen coordinates. Planning screens can be highlighted as context; battle teaching steps must use operation-map anchors.

The audio guide already defines `Tutorial.Highlight.Pulse`, `Tutorial.Step.Open`, `Tutorial.Step.Complete`, and `VO.Tutorial.*`; reuse those event ids.

## Control Takeover Design

ARIA's takeover is not full AI autonomy by default. It is a permissioned action executor.

### Control Ownership States

| State | Meaning |
|---|---|
| `Player` | Normal input. |
| `Guided` | Player controls action while ARIA highlights and validates the step. |
| `AssistantPreview` | ARIA previews a path, target, or UI click without issuing the command. |
| `AssistantTakeover` | ARIA executes a bounded command plan with player permission. |
| `PlayerOverridePending` | Player touched input during takeover; ARIA pauses and returns control. |

### Takeover Rules

- Takeover requires an explicit `Do It` button except in non-gameplay UI demos during the first tutorial welcome.
- Takeover must show a visible banner: `ARIA controlling. Tap anywhere to resume command.`
- Any pointer/touch/keyboard/gamepad input cancels or pauses takeover.
- Takeover cannot spend premium resources, accept purchases, delete saves, change Settings, or start a raid without confirmation.
- Takeover cannot complete a full mission unattended. It should execute one bounded intent, then yield.
- In live combat, takeover should prefer safe teaching actions: select unit, move, attack marked target, build required structure, queue one unit, activate radar ping, load/unload transport, execute breach command.
- For longer hands-off testing, reuse the existing `PlayerAutoAIEnabled` path as DevOnly or explicit sandbox auto mode, not as the FTUE default.

### Executable Command Intents

Define small, auditable action primitives:

| Intent | Existing Anchor |
|---|---|
| Route to screen | `WarlineCaptureRouter.GoTo(WarlineCaptureRoute)` |
| Select mission | `new ActiveMissionSession().BeginMission(...)` and Campaign controllers |
| Start mission briefing | `MissionBriefingScreenController` state |
| Select unit/squad | `RTSSelectionSystem` |
| Move selected units | `RTSSelectionSystem` move order path |
| Attack target | `RTSSelectionSystem` attack path |
| Open command wheel | `CommandWheelPanelController.Open()` |
| Build structure | `BuildingPlacementSystem` placement path |
| Queue production | `BuildingPlacementSystem` production/camp request path |
| Apply Operation action | `WarlineCaptureOperationRuntime.ApplyAction(...)` |
| End Operation day | `WarlineCaptureOperationRuntime.EndDay()` |

Implementation detail: do not let the assistant click arbitrary screen coordinates. It should call typed game actions through a `CommandIntentExecutor`.

Map intent rule: any assistant command that focuses a location must identify whether it targets planning context (`PlanningCameraId`), minimap context (`MinimapProjectionId`), or battle gameplay (`OperationMapId` plus an operation-map anchor). `Do It` actions for move, attack, build, threat jump, objective jump, or minimap jump must clamp to operation-map camera bounds.

## Recommended Architecture

Create a new tutorial/assistant module:

```text
Assets/Game/Scripts/Tutorial
Assets/Game/Scripts/Tutorial/Assistant
Assets/Game/Scripts/Tutorial/Recommendations
Assets/Game/Scripts/Tutorial/Control
Assets/Game/Scripts/Tutorial/Data
Assets/Game/Configs/Tutorial
Assets/Game/Prefabs/UI/Assistant
```

### Core Runtime Types

| Type | Responsibility |
|---|---|
| `WarlineCaptureAssistantService` | Owns active assistance level, current context, current recommendation, panel open state. |
| `TutorialDirector` | Runs authored tutorial sequences and step state. |
| `TutorialStepDefinition` | Data for one tutorial step. |
| `TutorialSequenceDefinition` | Ordered or branching set of tutorial steps. |
| `AssistantRecommendationService` | Scores current recommended actions from game state. |
| `AssistantRecommendation` | User-facing recommendation plus optional executable plan. |
| `AssistantControlOwner` | Tracks player/assistant ownership and cancellation. |
| `CommandIntentExecutor` | Executes typed assistant actions through real game APIs. |
| `AssistantContextProvider` | Reads route, mission, objectives, Operation state, selection state, threat state, and stats. |
| `AssistantPanelController` | UI panel, portrait, chips, buttons, and transcript. |
| `AssistantHighlightController` | UI/world highlights and path previews. |

### Tutorial Step Data

Suggested ScriptableObject or JSON fields:

```csharp
public sealed class TutorialStepDefinition
{
    public string StepId;
    public string SequenceId;
    public WarlineCaptureRoute Route;
    public string MissionId;
    public string TitleKey;
    public string BodyKey;
    public string ReasonKey;
    public string VoiceEventId;
    public TutorialTrigger Trigger;
    public TutorialCompletionRule Completion;
    public TutorialInputPolicy InputPolicy;
    public TutorialHighlightTarget[] HighlightTargets;
    public AssistantCommandPlan OptionalShowMePlan;
    public AssistantCommandPlan OptionalDoItPlan;
    public string[] PrerequisiteStepIds;
    public string[] SuppressionFlags;
}
```

### Save Data Additions

Add this under `WarlineCaptureSaveData` when implementation begins:

```csharp
public sealed class TutorialSaveData
{
    public string assistanceLevel = "FullGuidance";
    public string[] completedStepIds = Array.Empty<string>();
    public string[] dismissedRecommendationIds = Array.Empty<string>();
    public bool chapterOneFtueComplete;
    public bool operationIntroComplete;
    public bool assistantMuted;
    public int takeoverUseCount;
}
```

Migration rule: default existing saves to `HintsOnly` if they already have completed Campaign progress, including legacy `SagaProgress` storage, otherwise `FullGuidance`.

## Recommendation Engine

Recommendations should be ranked by urgency, relevance, confidence, and player progress.

### Inputs

| Source | Use |
|---|---|
| `WarlineCaptureRouter.ActiveRoute` | Determine screen context. |
| `new ActiveMissionSession().ActiveMission` | Mission objectives, rewards, return route. |
| `ObjectiveManager` and `GameRuntimeStats` | In-match objective progress and stuck detection. |
| `ThreatWarningRuntimeState` | Threat alerts and jump-to-threat recommendations. |
| `WarlineCaptureOperationRuntime.State` | District pressure, supplies, latest events, selected district. |
| `CampaignProgressStore` or existing `SagaProgressStore` compatibility wrapper | Next mission, replay, star mastery. |
| `PlayerProfileSaveData` | Unlocks, resources, profile progression. |
| `AISettingsRuntimeState.PlayerAutoAIEnabled` | Dev/sandbox auto-control status. |

### Scoring

| Factor | Example |
|---|---|
| Criticality | Base breach, objective failing, no selected unit under attack. |
| Progression | Next unlocked Campaign mission, unclaimed reward, Operations day action. |
| Teaching gap | Player has not completed select/move/build/transport tutorial. |
| Economy | Not enough supplies for raid, build prerequisite missing. |
| Risk | Intel low before raid, civilian risk high, threat rising. |
| Stuck state | No command issued for 45 seconds while required objective is incomplete. |

### Recommendation Examples

| Context | Recommendation |
|---|---|
| Main Menu first launch | Start `Campaign` because it teaches command fundamentals. |
| Campaign Map M01 unlocked | Select `First Contact`. |
| Mission Briefing M01 | Review objectives, then deploy. |
| M01 no unit selected | Select rifle squad. |
| M01 squad selected, no order | Move to the marked road cover. |
| M01 enemy visible | Attack the hostile patrol. |
| M02 resources available | Open build drawer and place Barrack. |
| M03 threat warning | Jump to threat lane and prepare defense. |
| M04 transport idle | Load squad, move to landing zone, then unload. |
| Operation Dashboard high threat | Open highest-threat district. |
| District Detail low intel before raid | Drone Scan before Raid. |
| District Detail high civilian risk | Evacuate or Aid before End Day. |

## FTUE Flow

### FTUE Entry

First launch should go:

```text
Splash -> Main Menu -> ARIA welcome -> Campaign highlighted -> Campaign Map M01 -> Mission Briefing -> Match Tutorial
```

Do not force the player into combat immediately from splash. Let them see that WarlineCapture has modes, but guide them to Campaign as the training path.

### Prologue Prompt

ARIA first line:

```text
Commander, WarlineCapture Response Command is online. I can guide the first operation, explain your options, or take a single action when you ask.
```

Choice:

- `Guide Me`: Full Guidance.
- `Hints Only`: Hints Only.
- `I Know RTS`: Minimal.

### Mission 1: First Contact

Implementation handoff: `WarlineCapture_M01_FirstContact_Production_Contract.md` owns the concrete M01 operation-map anchors, runtime entity ids, FTUE target ids, and validation checks. `WarlineCapture_AssistantPanel_M01_Implementation_Contract.md` owns `PREFAB-05_AssistantPanel` element ids, M01 ARIA recommendation states, Show Me / Do It / Stop behavior, player-control cancellation boundaries, and the `BattleHudGameplayBridge` dependency for select, move, attack, invalid command, objective, and result flows.

Teaching goals:

- Understand commander role.
- Select squad.
- Move to marked point.
- Attack patrol.
- Read objective tracker.
- Understand result, stars, and rewards.

Step plan:

| Step | Trigger | Player Action | Assistant Option |
|---|---|---|---|
| `ftue.m01.welcome` | Briefing opened first time | Read mission context. | Explain objective. |
| `ftue.m01.deploy` | Briefing CTA visible | Start mission. | Show deploy button. |
| `ftue.m01.objectives` | Match starts | Notice objective tracker. | Highlight tracker. |
| `ftue.m01.select_squad` | Squad visible | Select highlighted rifle squad. | Do It selects squad. |
| `ftue.m01.move` | Squad selected | Move to marked cover. | Do It issues move order. |
| `ftue.m01.attack` | Enemy patrol visible | Attack highlighted patrol. | Do It issues attack. |
| `ftue.m01.complete` | Victory | Continue to result. | Explain stars and rewards. |

Map contract: briefing/deploy steps may use `camera.ch01.m01.planning`; match steps must use `opmap.ch01.district_edge_01` operation-map anchors for squad spawn, move target, hostile patrol, objective marker, and camera bounds.

Input policy: guided but not hard-locked after selection. If the player attacks early and succeeds, mark select/move/attack steps complete by inference.

### Mission 2: Establish The Base

Teaching goals:

- Open build drawer.
- Choose required production building.
- Place on valid socket.
- Understand resource spend.
- Queue a unit.
- Defend first attack wave.

Key assistant lines:

```text
This mission teaches infrastructure. A base is not a menu purchase; it is a position inside the 3D operation map that you must defend.
```

Recommended note: resolve the current `Building_Barrack` versus `Tent_Regular` design decision before final implementation. The assistant must teach exactly the canonical producer.

### Mission 3: Radar Warning

Teaching goals:

- Threat warnings are actionable, not decorative.
- Jump to threat lane.
- Prepare defense before contact.
- Use or understand `ability.radar_ping`.
- Keep losses low.

Recommendation behavior:

- If warning exists and no units near defense line: recommend `Move squad to convoy lane`.
- If radar support is unlocked/available: recommend `Use Radar Ping`.
- If player ignores warning for 30 seconds: escalate to a stronger chip, not a forced takeover.

### Mission 4: Airlift

Teaching goals:

- Select transport.
- Load squad or rescue group.
- Move to landing zone/extraction zone.
- Unload or rope disembark.
- Preserve aircraft.

Takeover option:

- `Show Me` previews flight path and load/unload points.
- `Do It` executes one transport segment only, then yields.

### Mission 5: Breach Assault

Teaching goals:

- Read fortified objective.
- Identify breach route.
- Use breach command or breach unit.
- Focus fire on core.
- Protect vehicle/specialist.

ARIA should stop being a step-by-step teacher here and become a command advisor. The player should feel they are commanding, not following.

## Operation Tutorial Flow

Operation unlock tutorial should begin after Chapter 1 or when the player first opens Operation Dashboard.

### Day 1

Teach:

- District metrics: stability, threat, intel, trust, security, infrastructure.
- Operation supplies.
- Select a district.
- Apply one low-risk action, preferably Patrol or Aid.
- End Day.

### Day 2

Teach:

- Intel confidence and raid risk.
- Drone Scan before Raid.
- Intel Reveal popup.

### Day 3

Teach:

- Repair, Evacuate, Build Outpost.
- Consequence tradeoffs: trust, security, infrastructure, heat, civilian risk.

Assistant recommendation examples:

```text
Old Market has high civilian risk and enough supplies for Evacuate. This reduces immediate harm but costs trust.
```

```text
Port Breach threat is high and intel is low. Scan before raid unless you are accepting collateral risk.
```

## Contextual Help Library

Add "Ask ARIA" topics that unlock as systems appear:

| Topic | Unlock |
|---|---|
| What is my role? | First launch. |
| How do objectives work? | M01 briefing. |
| How do stars work? | First result. |
| How do I move and attack? | M01 match. |
| How do I build? | M02. |
| What are resources? | M02. |
| What are threat warnings? | M03. |
| How do transports work? | M04. |
| What is breach? | M05. |
| What is Operation? | First Operation dashboard. |
| What is intel confidence? | First District Detail or Scan. |
| Why did district trust change? | First End Of Day report. |

Store these as localized text entries with optional voice events. The first pass can ship without generated VO as long as subtitles/text are complete.

## UI Surface Additions

Update `WarlineCapture_UIUX_Gameplay_Element_Alignment.md` during implementation with these new surfaces:

| Surface | Purpose |
|---|---|
| `PREFAB-04 Assistant Button` | Persistent entry point and recommendation status. |
| `PREFAB-05 Assistant Panel` | Recommendations, explanations, tutorial transcript, control buttons. |
| `PREFAB-06 Tutorial Card` | Contextual guided instruction. |
| `PREFAB-07 Tutorial Highlight` | UI/world highlight layer. |
| `POP-10 Assistant Takeover` | Visible ownership banner and cancel affordance. |
| `POP-11 Commander Identity` | Name, commander portrait, frame, and title picker. |

Suggested hierarchy:

```text
AppShell
  SafeAreaRoot
    AssistantLayer
      AssistantButton
      AssistantPanel
      TutorialCardRoot
      TutorialHighlightRoot
      TakeoverBanner
```

Match overlay should have its own world-aware highlight root so ARIA can mark squads, enemies, build sockets, and extraction zones.

Implementation contract: `WarlineCapture_AssistantPanel_M01_Implementation_Contract.md` defines the current `PREFAB-05 Assistant Panel` UI ids, M01 recommendation state mapping, runtime data requirements, command-intent boundaries, and acceptance checks. Runtime wiring contract: `WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md` defines service ownership, context data flow, state transitions, typed intents, save/session fields, button enablement, and validation tests for the M01 assistant flow. Treat flat visual references as targets only; final production assets still need live TMP text, bound controls, and asset-register approval.

## Required Art Assets

Track all final production rows in `WarlineCapture_Art_Asset_Requirements_Register.csv`. Existing generated commander/profile images can remain placeholders until reviewed and approved.

Commander identity art:

- 6 default commander portraits, readable at profile size and small Main Menu avatar size.
- 3-4 unlockable commander portraits for Chapter, Operation, and founder/event identity.
- Commander portrait card frame, selected state, locked overlay, and edit icon.
- Commander frame cosmetics: default, Iron Guard, Operation Stabilizer, Founder.
- `POP-11 Commander Identity` layer pack and Unity prefab visuals.

ARIA assistant art:

- ARIA portrait for tutorial cards and assistant panel.
- ARIA assistant button icon / radio waveform mark.
- Assistant button state set: idle, recommendation available, critical, takeover active, muted.
- Assistant panel frame and recommendation chip states.
- Tutorial card frame, ARIA speaking state, and highlight pointer treatment.
- UI/world tutorial highlight ring, path preview arrow/line, and blocked-action pulse.
- Takeover banner art for `ARIA controlling` state.

## Audio And Voice

Reuse the audio taxonomy already in `WarlineCapture_Audio_Design_Guidelines.md`.

Add voice line groups:

| Group | Example id |
|---|---|
| Welcome | `VO.Tutorial.Welcome.CommandOnline.01` |
| Recommendation | `VO.Assistant.Recommendation.Available.01` |
| Takeover start | `VO.Assistant.Takeover.Start.01` |
| Takeover cancelled | `VO.Assistant.Takeover.Cancelled.01` |
| Objective complete | `VO.Assistant.Objective.Complete.01` |
| Stuck help | `VO.Assistant.Stuck.MoveSuggestion.01` |
| Operation risk | `VO.Assistant.Operation.RaidRisk.01` |

Voice rule: text must exist without audio, and every VO line must be subtitle-backed.

## Implementation Phases

### Phase 0: Design Lock

- Approve ARIA identity and tone.
- Approve default commander identity, portrait style, and first six default portrait slots.
- Approve FTUE step list for Chapter 1.
- Resolve M02 canonical production building.
- Decide whether Operation tutorial unlocks after M05 or appears earlier as preview.

### Phase 1: Non-Invasive Assistant UI

- Add Assistant Button and Assistant Panel.
- Add recommendation text only, no takeover.
- Bind recommendations to route, active mission, objective state, and Operation state.
- Persist assistance level and dismissed recommendation ids.

### Phase 2: Guided Chapter 1 Tutorials

- Add `TutorialDirector`.
- Add tutorial cards and highlights.
- Implement M01 and M02 steps first.
- Use real completion rules from `ObjectiveManager`, `GameRuntimeStats`, selection state, and build state.

### Phase 3: Command Intent Takeover

- Add `AssistantControlOwner`.
- Add typed `CommandIntentExecutor`.
- Implement safe intents: route, select, open command wheel, open build drawer, Operation action.
- Add live combat intents only after selection/move/attack APIs have test coverage.

### Phase 4: Strategic Recommendations

- Add Operation advisor scoring.
- Add stuck-state detection.
- Add replay/mastery recommendations after result screen.
- Add contextual help library.

### Phase 5: Polish

- Add VO and tutorial SFX.
- Add accessibility settings.
- Add localization keys.
- Add analytics events for tutorial completion, skip, takeover start, takeover cancel, and stuck help.

## Validation And Tests

Required EditMode tests:

- `TutorialStepDefinitions_HaveUniqueIdsAndLocalizationKeys`
- `TutorialSequence_ChapterOneReferencesExistingMissionIds`
- `AssistantRecommendations_DoNotReferenceLockedRoutes`
- `AssistantRecommendation_OperationUsesExistingActionTypes`
- `AssistantControlOwner_PlayerInputCancelsTakeover`
- `CommandIntentExecutor_RejectsPremiumSpendAndDestructiveActions`
- `AssistantSaveData_MigratesWithExistingSaves`
- `AssistantPanel_HasRequiredButtonsAndNoSilentInertControls`

Required PlayMode tests:

- M01 tutorial can complete select, move, attack, and result steps.
- M01 tutorial highlights resolve to operation-map metadata anchors or runtime entities after deploy, not to a separate preview image.
- M02 tutorial can reach build and produce steps without blocking normal play.
- Takeover banner appears during assistant control.
- Player tap cancels takeover and returns ownership.

Manual validation:

- New player can complete M01 without reading external instructions.
- Experienced RTS player can skip guidance within 10 seconds.
- Assistant recommendation never covers critical combat UI.
- Assistant text fits phone landscape layouts with large text enabled.
- Tutorial does not require audio.

## Open Design Decisions

| Decision | Recommendation |
|---|---|
| Assistant name | Use `ARIA` unless there is a stronger brand reason. |
| M02 producer | Make one canonical Chapter 1 producer before tutorial implementation. |
| Operation unlock timing | Unlock full Operation after M05; allow Main Menu card preview earlier. |
| Takeover depth | Start with one-step typed intents. Do not ship full mission autopilot as FTUE. |
| PlayerAuto AI usage | Keep as sandbox/dev auto mode; use typed assistant plans for tutorial. |
| VO timing | Ship text-first, then add VO once script is stable. |

## First Vertical Slice Recommendation

Build this first:

```text
ARIA button + panel
Commander Identity first-launch step
M01 First Contact guided tutorial
Recommendations on Campaign Map, Mission Briefing, and Match HUD
One safe takeover action: select highlighted squad
Player input cancels takeover
Tutorial save flags
EditMode tests for data validity
```

This is small enough to integrate with the current project and valuable enough to prove the design. After that, expand to move/attack takeover, M02 build guidance, and Operation recommendations.
