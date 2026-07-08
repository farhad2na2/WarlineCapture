# WarlineCapture Config-Driven Audio Implementation Spec

Date: 2026-07-08
Status: Draft for implementation

This document defines the implementation plan for WarlineCapture audio: UI sounds, match sounds, alerts, feedback cues, ambience, and music. It turns `Audio_Design_Guidelines.md` into a config-driven runtime system aligned with the SOLID/ECS architecture and performance contracts.

Parent design source:

- `Audio_Design_Guidelines.md` defines the audio identity, bus list, event ids, naming rules, playback rules, screen matrix, and recommended asset names.

Related architecture sources:

- `Architecture/gameplay_solid_ecs_contract.md` defines ECS ownership, naming, UI view boundaries, config rules, services, and no-singleton guardrails.
- `Architecture/performance_regression_contract.md` defines hot-path, allocation, runtime asset-loading, pooling, and validation requirements.
- `Visual_Feedback_VFX_Recommendations.md` defines shared feedback primitives that audio cues must reinforce.
- `Match_HUD_And_Gameplay_Implementation_Spec.md` defines match command feedback.
- `Match_Unit_Command_Behavior_Spec.md` defines detailed `HOLD`, `STOP`, and `SCAN` status/feedback text that audio must support.

## Product Goal

Audio must be data-driven. A designer should be able to change a global sound, such as every accepted primary button click, by changing one catalog entry instead of opening every prefab that contains a button.

The runtime contract is:

```text
UI or gameplay event -> semantic audio event id -> catalog lookup -> pooled playback on configured bus
```

Examples:

- A primary button emits `UI.Button.Primary.Click`.
- A disabled button emits `UI.Button.Disabled.Tap`.
- A selected squad emits `Gameplay.Unit.Select.Infantry`.
- A rejected match command emits `UI.Feedback.Toast.Error` or the command-specific reject event.
- A critical threat emits `Alert.Threat.Critical`.

No UI prefab should directly own the final click `AudioClip`. Prefabs may own semantic event ids only when the event is not inferable from a shared view type.

## Non-Negotiable Rules

1. Audio clips are assigned through central config, not duplicated across prefabs.
2. UI `*View` components emit semantic event ids or call a narrow audio request boundary. They do not own gameplay policy or direct clip selection.
3. Gameplay systems publish audio request data through ECS components/buffers. They do not call `AudioSource`, `Resources.Load`, or scene lookups directly.
4. Managed playback lives at the presentation/shell edge and uses pooled `AudioSource` instances.
5. Runtime playback uses preloaded clip references from config. No gameplay-frame runtime asset loading.
6. Frequent audio events must have cooldown, priority, and concurrency limits.
7. Critical audio cues must always have visual equivalents.
8. Placeholder generated clips are allowed for implementation, but final AAA-quality audio must be replaceable without code/prefab edits.

## Naming And Folder Contract

Event ids use PascalCase namespaces:

```text
UI.Button.Primary.Click
UI.Button.Disabled.Tap
Gameplay.Command.Move.Accepted
Gameplay.Command.Scan.Rejected
Alert.Threat.Critical
Music.Match.Combat
Ambience.City.Day
```

Runtime asset files use lowercase snake case:

```text
ui_button_primary_click_01.wav
game_command_move_accepted_01.wav
alert_threat_critical_01.wav
music_match_combat_loop_01.wav
```

Recommended Unity folder layout:

```text
Assets/Game/Audio/
  Mixers/
  Config/
  Events/
  UI/
  Gameplay/
  Alerts/
  Music/
  Ambience/
  Voice/
  GeneratedSource/
```

Recommended tools folder:

```text
Tools/Audio/
  generate_placeholder_audio.py
  validate_audio_catalog.py
```

`GeneratedSource` stores prompts, synthesis source, uncompressed masters, and notes. Runtime clips live in the category folders.

## Runtime Architecture

### Config Assets

| Asset / Type | Kind | Owner | Purpose |
|---|---|---|---|
| `AudioEventCatalogConfig` | ScriptableObject config | Shell/audio edge | Maps event ids to clips, bus, volume, pitch, cooldown, priority, and playback behavior. |
| `AudioMixerBusConfig` | ScriptableObject config | Shell/audio edge | Defines bus ids, mixer group references, volume setting keys, ducking flags, and default levels. |
| `AudioMusicStateConfig` | ScriptableObject config | Shell/audio edge | Maps route/gameplay state to music event ids, transition time, loop behavior, and intensity layers. |
| `AudioClipImportProfileConfig` | ScriptableObject config or editor-only data | Editor/audio edge | Defines import expectations for UI, gameplay, alerts, music, ambience, and voice. |

Config rules:

- Config assets describe data only. They do not execute playback or gameplay decisions.
- Event ids should also be generated as constants or hashes so hot paths do not allocate strings.
- Config validation must fail if an event id is duplicated, missing its clip, missing its bus, has invalid cooldown, or references a non-runtime clip path.

### ECS Data

| ECS Type | Kind | Purpose |
|---|---|---|
| `AudioEventIdComponent` | `IComponentData` or generated value helper | Stores stable event id/hash values where authored on entities. |
| `AudioPlaybackRequestComponent` | `IBufferElementData` | One-shot request buffer for gameplay/UI-to-audio boundary. |
| `AudioPlaybackResultComponent` | `IBufferElementData` | Optional result/debug buffer for accepted, rejected, cooldown-skipped, or missing-event outcomes. |
| `AudioSettingsComponent` | `IComponentData` | Runtime master/music/sfx/voice/alerts volume and mute state. |
| `AudioMusicStateComponent` | `IComponentData` | Current requested music state, target intensity, and transition request. |
| `AudioListenerStateComponent` | `IComponentData` | Optional listener/camera position data for spatial event culling. |

ECS rules:

- Gameplay systems publish audio requests as data.
- ECS systems may perform request normalization, cooldown gating, priority gating, and event hash routing.
- ECS systems must not hold `AudioClip`, `AudioSource`, `AudioMixer`, or GameObject references in unmanaged gameplay components.
- Managed object references are allowed only in managed ECS references or shell-edge presentation systems when necessary.

### ECS Systems

| System | ECS / Managed Edge | Purpose |
|---|---|---|
| `AudioEventRequestSystem` | ECS `*System` | Consumes domain gameplay events and writes `AudioPlaybackRequestComponent` entries. |
| `AudioCooldownSystem` | ECS `*System` | Applies event cooldowns and spam protection where request volume can be high. |
| `AudioMusicStateSystem` | ECS `*System` | Converts route/match state into music state requests. |
| `AudioSettingsSystem` | ECS `*System` | Mirrors persisted settings into `AudioSettingsComponent` and publishes changed settings. |
| `AudioPlaybackPresentationSystemHelper` | Managed presentation edge | Drains accepted playback requests and plays through pooled `AudioSource` objects. |
| `AudioMixerPresentationSystemHelper` | Managed presentation edge | Applies volume/mute/ducking to Unity mixer groups. |

Naming rule:

- Only ECS lifecycle systems use the bare `*System` suffix.
- Plain managed helpers use approved helper suffixes, such as `PresentationSystemHelper`.
- Do not create `AudioManager`, `AudioController`, `AudioFacade`, or singleton/static audio access.

### UI Views

| View | Purpose |
|---|---|
| `UiAudioEventView` | Serialized-reference binder for explicit UI audio event ids when needed. |
| `ButtonAudioEventView` | Optional narrow view for button semantic events: accepted, disabled, negative, hold/repeat, tab/card variants. |
| `SliderAudioEventView` | Emits `UI.Slider.Tick` with step/cooldown rules. |
| `ToggleAudioEventView` | Emits `UI.Toggle.On` or `UI.Toggle.Off` after state changes. |

UI rules:

- UI views emit event ids, not final clips.
- Button audio should play after accepted action/release, not on pointer down if the action may be rejected.
- Disabled/locked UI emits a disabled/reject event only when the player intentionally taps it.
- Touch devices should not use hover sounds.
- Existing prefab buttons can be migrated in batches by view type and style class, not one-by-one clip assignment.

## Event Catalog Schema

Recommended catalog entry fields:

```text
eventId
eventHash
assetIds
busId
priority
volumeDb
pitchMin
pitchMax
cooldownSeconds
cooldownScope
maxConcurrent
spatialMode
minDistance
maxDistance
loop
preload
stream
duckingGroup
fallbackEventId
description
implementationStatus
```

`assetIds` may reference one or more clip variants. Runtime chooses a variant by deterministic or random rotation depending on the event rule.

Recommended enum values:

```text
priority: Critical, High, Medium, Low
cooldownScope: None, GlobalEvent, SourceEntity, MessageKey, ScreenRoute, ThreatType
spatialMode: None, CameraRelative, WorldPosition, ListenerRelative
implementationStatus: Planned, PlaceholderGenerated, Implemented, FinalCandidate, FinalApproved
```

## Core Event Set For First Implementation

Start with a small complete set rather than a large partial catalog.

### UI Events

| Event Id | Required Clip | Notes |
|---|---|---|
| `UI.Button.Primary.Click` | `ui_button_primary_click_01.wav` | Global accepted primary action. |
| `UI.Button.Secondary.Click` | `ui_button_secondary_click_01.wav` | Global accepted secondary action. |
| `UI.Button.Negative.Click` | `ui_button_negative_click_01.wav` | Back, close, cancel. |
| `UI.Button.Disabled.Tap` | `ui_button_disabled_tap_01.wav` | Locked/disabled tap. |
| `UI.Tab.Select` | `ui_tab_select_01.wav` | Tab/segmented selection. |
| `UI.Card.Select` | `ui_card_select_01.wav` | Cards, unit roster, mode cards. |
| `UI.Card.Locked` | `ui_card_locked_01.wav` | Locked card. |
| `UI.Popup.Open` | `ui_popup_open_01.wav` | Modal/drawer open. |
| `UI.Popup.Close` | `ui_popup_close_01.wav` | Modal/drawer close. |
| `UI.Slider.Tick` | `ui_slider_tick_01.wav` | Rate limited. |
| `UI.Toggle.On` | `ui_toggle_on_01.wav` | Toggle accepted. |
| `UI.Toggle.Off` | `ui_toggle_off_01.wav` | Toggle accepted. |
| `UI.Feedback.Toast.Error` | `ui_feedback_toast_error_01.wav` | Invalid command/validation. |
| `UI.Feedback.Toast.Positive` | `ui_feedback_toast_positive_01.wav` | Soft success/info. |

### Match Command Events

| Event Id | Required Clip | Notes |
|---|---|---|
| `Gameplay.Unit.Select.Infantry` | `game_unit_select_infantry_01.wav` | Squad/unit selection. |
| `Gameplay.Unit.Select.Vehicle` | `game_unit_select_vehicle_01.wav` | Vehicle selection. |
| `Gameplay.Unit.Select.Air` | `game_unit_select_air_01.wav` | Helicopter/jet/drone selection. |
| `Gameplay.Command.Move.Accepted` | `game_command_move_accepted_01.wav` | Move order accepted. |
| `Gameplay.Command.Attack.Accepted` | `game_command_attack_accepted_01.wav` | Attack order accepted. |
| `Gameplay.Command.Hold.Accepted` | `game_command_hold_accepted_01.wav` | Hold command accepted. |
| `Gameplay.Command.Stop.Returning` | `game_command_stop_returning_01.wav` | Fixed-wing stop/return feedback. |
| `Gameplay.Command.Scan.Targeting` | `game_command_scan_targeting_01.wav` | Scan mode entered. |
| `Gameplay.Command.Scan.Accepted` | `game_command_scan_accepted_01.wav` | Scan route/pulse accepted. |
| `Gameplay.Command.Rejected` | `game_command_rejected_01.wav` | Command rejected. |
| `Gameplay.Build.Place.Valid` | `game_build_place_valid_01.wav` | Valid building placement confirm. |
| `Gameplay.Build.Place.Invalid` | `game_build_place_invalid_01.wav` | Invalid placement. |
| `Gameplay.Production.Queued` | `game_production_queued_01.wav` | Unit/building queued. |
| `Gameplay.Production.Complete` | `game_production_complete_01.wav` | Production complete. |

### Alerts And Feedback Events

| Event Id | Required Clip | Notes |
|---|---|---|
| `Alert.Threat.Minor` | `alert_threat_minor_01.wav` | Non-critical threat feed. |
| `Alert.Threat.Critical` | `alert_threat_critical_01.wav` | Cooldown-limited, can duck music/ambience. |
| `Alert.Unit.UnderAttack` | `alert_unit_under_attack_01.wav` | High priority. |
| `Alert.Base.Breached` | `alert_base_breached_01.wav` | Critical. |
| `Gameplay.Objective.Progress` | `game_objective_progress_01.wav` | Meaningful progress threshold. |
| `Gameplay.Objective.Complete` | `game_objective_complete_01.wav` | High priority success. |
| `Gameplay.Objective.Failed` | `game_objective_failed_01.wav` | Critical/defeat. |

### Music And Ambience Events

| Event Id | Required Clip | Notes |
|---|---|---|
| `Music.Splash.Intro` | `music_splash_intro_01.wav` | 2-4 s intro. |
| `Music.Menu.Loop` | `music_menu_loop_01.wav` | Main menu loop. |
| `Music.Briefing.Loop` | `music_briefing_loop_01.wav` | Mission briefing. |
| `Music.Match.CalmLoop` | `music_match_calm_loop_01.wav` | Low intensity match. |
| `Music.Match.CombatLoop` | `music_match_combat_loop_01.wav` | Combat intensity layer. |
| `Music.Result.Victory` | `music_result_victory_01.wav` | Result stinger/loop. |
| `Music.Result.Defeat` | `music_result_defeat_01.wav` | Result stinger/loop. |
| `Ambience.City.DayLoop` | `amb_city_day_loop_01.wav` | Low volume map bed. |
| `Ambience.Base.DistantLoop` | `amb_base_distant_loop_01.wav` | Base/menu/loading bed. |

## Actual Audio Clip Strategy

### Recommendation

Use generated placeholder clips for implementation and iteration, then replace with production audio later.

Reasons:

- The architecture work can start immediately.
- UX timing and feedback can be tuned before final assets.
- The central catalog makes final replacement cheap.
- Generated clips are good enough for implementation tests, but should not be treated as final AAA sound design.

### Placeholder Generation Scope

Generate first-pass WAV clips for:

- 14 UI one-shots.
- 14 match command/gameplay one-shots.
- 7 alert/objective cues.
- 9 short music/ambience placeholders.

Total first batch: 44 catalog event assignments.

### Generation Methods

Preferred local method:

- Use a deterministic Python synthesis script to generate clean placeholder `.wav` files from simple oscillators, filtered noise, envelopes, pitch sweeps, and procedural layers.
- No network dependency.
- Deterministic outputs for repeatable rebuilds.
- Good for UI and alert placeholders.

Optional later method:

- Use licensed source libraries or a human sound designer for final AAA-quality impacts, weapon layers, vehicle loops, and music.
- AI generation can be used for concept stems, but final licensing must be verified before shipping.

Music recommendation:

- Do not rely on procedural placeholder music for final product.
- Use placeholder loops only for route/music-state implementation.
- Final music should be composed/licensed in stems: menu, briefing, calm match, combat intensity, victory, defeat.

## Implementation Progress Tracker

Use this section as the work checklist. Update status after each implementation pass.

### Current Completion

Overall tracked completion: **70%**.

- Completed steps: 14 / 20.
- Asset and placeholder catalog preparation: **Done**.
- Runtime playback implementation: **In Progress**.
- UI/gameplay wiring: **In Progress**.
- Validation/performance test coverage: **In Progress**.
- Latest stable slice: shell route, popup, and drawer requests emit semantic UI audio events through the ECS audio request buffer.

Status legend:

- `Not Started`
- `In Progress`
- `Blocked`
- `Done`

| Step | Status | Owner | Deliverable | Validation |
|---|---|---|---|---|
| 1. Confirm audio architecture scope | Done | Designer | This spec created and linked. | Review against audio guide, ECS contract, performance contract. |
| 2. Create audio folders | Done | UI/Audio | `Assets/Game/Audio/*` folder layout. | Folders exist; `.meta` files preserved. |
| 3. Create catalog config types | Done | Gameplay/UI | `AudioEventCatalogConfig`, entry structs, bus config, music state config. | `AudioConfigContractTests.RunFocusedValidation` passed. |
| 4. Create generated event constants | Done | Gameplay | Event id constants/hash generation path. | `AudioConfigContractTests.RunFocusedValidation` passed with catalog/hash alignment checks. |
| 5. Create placeholder audio generator | Done | Audio/Tools | `Tools/Audio/generate_placeholder_audio.py`. | Script outputs first batch WAV files and data-only catalog. |
| 6. Generate first placeholder clip batch | Done | Audio/Tools | UI, command, alert, music placeholder clips. | 44 events assigned; catalog WAV headers validated. |
| 7. Create Unity import profile rules | Done | UI/Audio | Import settings for UI, gameplay, alerts, music, ambience. | `AudioConfigContractTests.RunFocusedValidation` passed with importer profile checks. |
| 8. Create ECS request components | Done | Gameplay | `AudioPlaybackRequestComponent`, settings/music state components. | `AudioEcsDataContractTests.RunFocusedValidation` passed. |
| 9. Create request systems | Done | Gameplay | `AudioEventRequestSystem`, `AudioCooldownSystem`, `AudioMusicStateSystem`, `AudioSettingsSystem`. | `AudioRequestSystemTests.RunFocusedValidation` passed. |
| 10. Create playback presentation helper | Done | UI/Audio | Pooled `AudioSource` playback helper and mixer application helper. | `AudioPlaybackPresentationSystemHelperTests.RunFocusedValidation` passed. |
| 11. Wire settings UI | Done | UI | Master/Music/SFX/Voice/Alerts volume controls update audio settings. | `AudioSettingsUiProjectionTests.RunFocusedValidation` passed. |
| 12. Wire common UI button audio | Done | UI | Shared button/tab/card/toggle/slider audio event views. | `UiAudioEventViewTests.RunFocusedValidation` passed. |
| 13. Wire shell route/popup audio | Done | UI | Screen forward/back, popup open/close, drawer open/close. | `UiShellAudioRoutePopupTests.RunFocusedValidation` passed. |
| 14. Wire match command audio | Not Started | Gameplay/UI | Select, move, attack, hold, stop/return, scan, build valid/invalid. | Match command tests and manual M01 smoke. |
| 15. Wire alert/objective audio | Not Started | Gameplay/UI | Threat, objective, unit under attack, base breached events. | Cooldown and priority tests. |
| 16. Wire music state system | Not Started | Gameplay/UI | Splash/menu/briefing/match/result music state transitions. | Route transition test; no overlapping unintended loops. |
| 17. Add audio catalog validation tests | Not Started | QA | Missing clip, duplicate id, invalid bus, cooldown, import profile tests. | EditMode pass. |
| 18. Add performance validation | Not Started | QA/Perf | Audio stress test for UI spam and match alerts. | No recurring GC after warmup; no runtime loading; pool size stable. |
| 19. Update `Audio_Design_Guidelines.md` handoff | Done | Designer | Cross-link implementation spec and mark event set source. | Docs aligned. |
| 20. Production audio replacement plan | Not Started | Audio/Designer | Final audio asset sourcing/composition plan. | Catalog entries marked final/placeholder. |

## Step Details

### Step 2: Folder Creation

Create folders exactly under `Assets/Game/Audio`. Do not place runtime clips under `Resources`.

Acceptance:

- Folder layout exists.
- Unity `.meta` files are preserved.
- Runtime folders are separated from generated source/master folders.

### Step 3: Catalog Config Types

Suggested files:

```text
Assets/Game/Scripts/Audio/Config/AudioEventCatalogConfig.cs
Assets/Game/Scripts/Audio/Config/AudioMixerBusConfig.cs
Assets/Game/Scripts/Audio/Config/AudioMusicStateConfig.cs
Assets/Game/Scripts/Audio/Config/AudioEventCatalogEntry.cs
```

Architecture rules:

- `*Config` and config data may be ScriptableObjects.
- Config types must not execute gameplay behavior.
- Config validation can be editor/test code.

### Step 4: Event Constants

Generate or maintain constants such as:

```csharp
public static class AudioEventIds
{
    public const string UiButtonPrimaryClick = "UI.Button.Primary.Click";
}
```

For hot paths, prefer stable hashes:

```text
eventId -> stable uint/int hash
```

Rules:

- Do not compute hashes repeatedly per frame from raw strings.
- Keep generated constants deterministic.
- If generated, keep the generator under `Tools/Audio` or editor-only tooling.

### Step 5-6: Placeholder Clip Generation

Current asset/config pass:

- Generator: `Tools/Audio/generate_placeholder_audio.py`
- Data-only catalog: `Assets/Game/Audio/Config/audio_event_catalog_v0_1.json`
- Generation manifest: `Assets/Game/Audio/GeneratedSource/audio_placeholder_manifest_v0_1.json`
- Runtime implementation status: not implemented. The JSON catalog assigns event ids to clips only.
- Latest validation: 48 unique event ids, 48 catalog WAVs, no missing clip references.

Recommended script behavior:

- Generate 44.1 kHz or 48 kHz WAV.
- Use mono for UI, gameplay one-shots, alerts.
- Use stereo only for ambience/music placeholders.
- Use short tails and normalized peak levels.
- Write source manifest with prompt/synthesis recipe per clip.

First clip batch should prioritize:

```text
ui_button_primary_click_01.wav
ui_button_secondary_click_01.wav
ui_button_negative_click_01.wav
ui_button_disabled_tap_01.wav
ui_tab_select_01.wav
ui_card_select_01.wav
ui_popup_open_01.wav
ui_popup_close_01.wav
ui_feedback_toast_error_01.wav
game_command_move_accepted_01.wav
game_command_attack_accepted_01.wav
game_command_hold_accepted_01.wav
game_command_stop_returning_01.wav
game_command_scan_accepted_01.wav
alert_threat_critical_01.wav
game_objective_complete_01.wav
music_menu_loop_01.wav
music_match_calm_loop_01.wav
```

### Step 7: Unity Import Profiles

Current import profile pass:

- Profile config: `Assets/Game/Audio/Config/audio_import_profiles_v0_1.json`
- Apply tool: `Tools/Audio/apply_audio_import_profiles.py`
- Validation: `AudioConfigContractTests.RunFocusedValidation`
- Latest validation: 48 catalog WAVs checked through Unity `AudioImporter`; UI/gameplay/alerts use mono decompressed preload, music/ambience use stereo streaming background load.

### Step 8-10: ECS Request And Playback

Current Step 8-10 data/request/playback-helper pass:

- Components: `Assets/Game/Scripts/Components/AudioComponents.cs`
- Request systems: `Assets/Game/Scripts/Systems/AudioEventRequestSystem.cs`
- Playback helper: `Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationSystemHelper.cs`
- Validation: `AudioEcsDataContractTests.RunFocusedValidation`, `AudioRequestSystemTests.RunFocusedValidation`
- Latest validation: unmanaged request/result/settings/music/listener contracts, request system bootstrap/cooldown/music/settings behavior, and pooled playback helper behavior passed focused Unity validation.
- Runtime implementation status: playback presentation helper exists; ECS-to-helper playback system wiring is still not implemented.

Required data flow:

```text
Gameplay ECS event/request
  -> AudioEventRequestSystem
  -> AudioPlaybackRequestElement buffer
  -> AudioCooldownSystem / priority filtering
  -> AudioPlaybackPresentationSystemHelper
  -> AudioEventCatalogConfig lookup
  -> pooled AudioSource playback
```

UI edge data flow:

```text
UiAudioEventView/ButtonAudioEventView
  -> audio request boundary
  -> AudioPlaybackRequestElement buffer or shell-edge queue
  -> same playback helper/catalog
```

Allowed shortcut for first implementation:

- UI may use a shell-edge audio request queue if ECS is not yet initialized on splash/menu.
- The queue must still use event ids and the same catalog.
- Match gameplay should move toward ECS request buffers.

### Step 11: Settings UI Audio Projection

Current settings pass:

- Settings model/service: `Assets/Game/Scripts/UI/Settings/UISettingsModels.cs`, `Assets/Game/Scripts/UI/Settings/SettingsService.cs`
- Settings panel support: `Assets/Game/Scripts/UI/Settings/SettingsPanelView.cs`
- ECS projection: `Assets/Game/Scripts/UI/Shell/Ecs/UiAudioSettingsProjectionSystem.cs`
- Validation: `AudioSettingsUiProjectionTests.RunFocusedValidation`
- Latest validation: Master, Music, SFX, Alerts, and Voice values persist through `SettingsService`; `SettingsService.ApplyRuntime` projects normalized values to `AudioSettingsComponent` in the default ECS world.

Implementation rule:

- Settings UI owns percentages and persistence.
- ECS audio runtime owns normalized bus volumes.
- UI controls must not assign clips, mixer groups, or per-button audio references.

### Step 12: Shared UI Audio Event Views

Current common UI audio pass:

- UI event gateway: `Assets/Game/Scripts/UI/Components/UIAudioEventGateway.cs`
- Button audio view: `Assets/Game/Scripts/UI/Components/UIButtonAudioEventView.cs`
- Toggle audio view: `Assets/Game/Scripts/UI/Components/UIToggleAudioEventView.cs`
- Slider audio view: `Assets/Game/Scripts/UI/Components/UISliderAudioEventView.cs`
- ECS bridge: `Assets/Game/Scripts/UI/Shell/Ecs/UiAudioEventBridgeSystem.cs`
- Validation: `UiAudioEventViewTests.RunFocusedValidation`
- Latest validation: primary/disabled button, tab/card, toggle on/off, slider tick, and UI-to-ECS request enqueue paths passed focused Unity validation.

Implementation rule:

- UI views emit semantic event ids only.
- UI views must not assign clips, mixer groups, or direct `AudioSource` references.
- Catalog changes must remain centralized through the audio event catalog and playback path.

#### Common UI Migration Order

Migration order:

1. Shell/global buttons: back, close, settings, primary CTA.
2. Main menu mode cards.
3. Settings sliders/toggles/tabs.
4. Build drawer tabs/cards/place/queue/close.
5. Armory cards/detail buttons.
6. Result screen CTAs/rewards.

Rule:

- Do not assign final clip references to each button.
- Assign semantic style/event ids once through shared button/audio views.

### Step 13: Shell Route And Popup Audio

Current shell route/popup audio pass:

- Catalog events: `UI.Screen.Forward`, `UI.Screen.Back`, `UI.Drawer.Open`, `UI.Drawer.Close`.
- Existing popup events reused: `UI.Popup.Open`, `UI.Popup.Close`.
- Gateway: `Assets/Game/Scripts/UI/Components/UIAudioEventGateway.cs`.
- Shell route/popup source: `Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs`.
- HUD drawer source: `Assets/Game/Scripts/UI/Shell/Ecs/UiActionRequestSystem.cs`.
- Validation: `UiShellAudioRoutePopupTests.RunFocusedValidation`, `AudioConfigContractTests.RunFocusedValidation`, `UiAudioEventViewTests.RunFocusedValidation`.
- Latest validation: forward/back menu routes, settings popup open/close, build drawer open, and passenger drawer open enqueue semantic audio requests into the ECS audio request buffer.

Implementation rule:

- Route transitions emit screen forward/back audio from route intent, not from individual screen prefabs.
- Build drawer and passenger drawer use drawer open/close events; normal modal overlays use popup open/close events.
- Shell/HUD systems must finish required ECS structural setup before processing request buffers, so audio enqueue stays data-only during command handling.

### Step 14: Match Command Audio

Use match command results, not raw button presses, as the source of truth.

Examples:

| Command Result | Audio Event |
|---|---|
| Unit selected | `Gameplay.Unit.Select.*` |
| Move accepted | `Gameplay.Command.Move.Accepted` |
| Attack accepted | `Gameplay.Command.Attack.Accepted` |
| Hold accepted | `Gameplay.Command.Hold.Accepted` |
| Fixed-wing stop/return accepted | `Gameplay.Command.Stop.Returning` |
| Scan targeting entered | `Gameplay.Command.Scan.Targeting` |
| Scan accepted | `Gameplay.Command.Scan.Accepted` |
| Command rejected | `Gameplay.Command.Rejected` or `UI.Feedback.Toast.Error` |

For `HOLD`, `STOP`, and `SCAN`, align audio feedback with `Match_Unit_Command_Behavior_Spec.md`. A fixed-wing jet or drone returning must not play a ground stop sound.

### Step 15: Alerts And Objective Audio

Rules:

- Alerts use bus `Alerts`.
- Critical alerts may duck `Music` and `Ambience`.
- Critical alerts use per-threat cooldowns.
- Visual warning/toast must always be present.
- Alert audio must not fire every frame while a condition remains active.

### Step 16: Music State

Recommended music states:

```text
SplashIntro
MenuLoop
BriefingLoop
MatchCalm
MatchTension
MatchCombat
ResultVictory
ResultDefeat
```

Rules:

- Music state changes come from route/session/match state, not arbitrary UI button views.
- Crossfade or stop/start transitions must be config-driven.
- Combat intensity changes should have hysteresis/cooldown to prevent rapid switching.

### Step 17-18: Validation

Required tests:

- Catalog ids unique.
- Every required event id has a catalog entry.
- Every implemented catalog entry has at least one clip or fallback.
- Every entry references a valid bus.
- UI shared button audio does not require per-prefab clip assignment.
- Cooldown blocks repeated invalid command spam.
- Pooled playback reuses sources.
- No runtime `Resources.Load` in gameplay frames.
- No direct `AudioSource.PlayOneShot` from gameplay ECS systems.
- No new singleton/static audio manager.

Performance scenario:

- Open main menu and tap primary/secondary/disabled buttons rapidly for 10 seconds.
- In match, select/move/attack/invalid/scan commands repeatedly.
- Trigger a threat alert burst.
- Validate no recurring managed allocation after warmup from audio request processing or playback.
- Validate pool size stabilizes.

## Acceptance Definition

The first implementation is complete when:

- Central catalog controls all implemented UI and match audio clips.
- Changing `UI.Button.Primary.Click` in one catalog entry changes all migrated primary buttons.
- Match command feedback plays from command results, not raw button taps.
- Settings screen can change Master/Music/SFX/Voice/Alerts volume.
- UI and alert cooldowns prevent spam.
- Audio playback uses pooled `AudioSource` objects.
- No gameplay ECS system directly plays Unity audio objects.
- Placeholder clips exist and are clearly marked as placeholder/generated.
- Validation tests cover catalog integrity and core playback performance.

## Future Extensions

- Voice/ARIA tutorial lines with subtitle-backed playback.
- Unit-specific barks with cooldown and accessibility controls.
- Weapon/vehicle/building loop systems with distance/importance culling.
- Music stem layering for combat intensity.
- Snapshot ducking for critical alerts and voice.
- Addressables or asset bundles for late-loading high-volume audio packs.
- Localization-aware voice catalog.
- Haptics events sharing the same semantic feedback event ids where appropriate.
