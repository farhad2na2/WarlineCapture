# Settings Panel Audio Runtime Implementation Tracker

## Goal

Make the Settings popup audio controls real runtime controls instead of visual-only settings.

The user-facing result should be:

- Music toggle actually mutes/unmutes music playback.
- Sound toggle actually mutes/unmutes gameplay SFX, UI SFX, alerts, and ambience unless a more specific toggle overrides it.
- Voice/ARIA toggle actually mutes/unmutes ARIA voice playback.
- Volume sliders update the matching audio bus volumes.
- Opening Settings reflects the current runtime state instead of resetting visual controls.
- Apply/Reset behavior remains explicit and predictable.

## Progress Dashboard

Last updated: 2026-07-09

Overall progress: 100% - 45 / 45 tracked items complete

Current phase: Complete

Current focus:

- Settings panel audio runtime implementation is complete.

Blocked:

- None for the current code slice. Local generated-project validation needed temporary `.csproj` include fixes for existing UI files before tests could build; source assemblies now validate with the listed focused builds.

| Phase | Status | Progress | Done / Total | Completion evidence |
| --- | --- | ---: | ---: | --- |
| Phase 0 - Tracker And Audit | Complete | 100% | 1 / 1 | This tracker created after auditing current audio settings and bus code. |
| Phase 1 - Audio Settings Data Model | Complete | 100% | 7 / 7 | Implemented in `UISettingsModels.cs`, `SettingsService.cs`, and `AudioSettingsUiProjectionTests.cs`; `Game.Runtime`, `Game.UI.Runtime`, and `Game.Tests.Editor` builds pass. |
| Phase 2 - Persistence And Runtime Apply Contract | Complete | 100% | 6 / 6 | Implemented in `SettingsService.cs`; persistence/reset/runtime projection tests compile in `Game.Tests.Editor`. |
| Phase 3 - Settings Panel UI Binding | Complete | 100% | 7 / 7 | `SettingsPanelView`, `SettingsScreenView`, and `SCN_SettingsPopup.prefab` now bind/read/listen to Music, Sound, and Voice toggles; no active UI Toolkit settings surface exists in this branch. |
| Phase 4 - ECS Projection And Audio Bus Mapping | Complete | 100% | 8 / 8 | Implemented in `UiAudioSettingsProjectionSystem.cs` and `AudioSettingsUiProjectionTests.cs`; focused source builds pass. |
| Phase 5 - Music State And Fade Behavior | Complete | 100% | 5 / 5 | Route music is no longer suppressed; music disable now relies on the `Music` bus mute and active-source volume reapply with a short managed fade; focused shell route validation proves menu/match music requests and duplicate-loop suppression. |
| Phase 6 - Immediate Feedback Samples | Complete | 100% | 4 / 4 | Sound/Voice enable interactions now raise settings-specific audio requests through the existing UI audio gateway; focused UI audio validation passed. |
| Phase 7 - Validation And Tests | Complete | 100% | 7 / 7 | Focused validation plus Menu and Match runtime smokes passed; match smoke confirms the HUD remains active while audio settings project. |

## Progress Update Rules

Every implementation update must update this document before reporting completion.

- Update `Last updated`.
- Update `Overall progress` as completed tracked items divided by total tracked items.
- Update the phase table row for any touched phase.
- Mark phase status as `Open`, `In progress`, `Blocked`, or `Complete`.
- Update each touched phase's `Progress`, `Done / Total`, and `Completion evidence`.
- Add short completed-step bullets under the touched phase.
- If blocked, write the exact blocker under that phase and keep the dashboard `Blocked` field in sync.
- A phase is `Complete` only when every task and validation bullet in that phase is done and evidence is linked.

## Existing Code Findings

| Area | Current state | Needed change |
| --- | --- | --- |
| Audio ECS settings | `AudioSettingsComponent` already has master/UI/SFX/alerts/music/ambience/voice volumes and mute bytes. | Use the existing component instead of adding a parallel settings state. |
| Audio bus volume resolution | `AudioPlaybackPresentationSystemHelper.ResolveBusVolume` already respects `MasterMuted`, per-bus mute flags, and per-bus volumes. | Feed it correct settings from the Settings popup. |
| Settings model | `AudioSettingsModel` has volume fields but no explicit music/sound/voice toggle state. | Add persisted toggle state. |
| Settings projection | `UiAudioSettingsProjectionSystem.ToAudioSettingsComponent` maps volumes but hard-codes `MusicMuted = 1`. | Remove hard-coded music mute and project actual user settings. |
| Runtime apply | `SettingsService.ApplyRuntime` sets `AudioListener.volume` and fires `RuntimeApplied`. | Keep as shell persistence boundary, but make the event project full bus settings. |
| Settings UI | `SettingsPanelView` binds sliders for master, music, SFX, alerts, and voice. | Add live toggle refs and read/write them into the model. |
| Audio event buses | Recent ARIA work already routes voice events through bus `"Voice"`. UI and gameplay SFX use event bus IDs. | Make toggles affect those bus IDs centrally. |

## Architecture Contract

Settings UI is a shell/UI edge. Gameplay and audio event policy stay ECS-driven.

- `*View` classes may cache raw UI references, register callbacks, read visible control values, and apply visual state.
- `*View` classes must not directly play/stop gameplay audio, mutate ECS gameplay state, or own polling loops.
- `SettingsService` remains the persistence and explicit apply boundary for user settings.
- `UiAudioSettingsProjectionSystem` should remain an `ISystem` projection from `UISettingsModel` to `AudioSettingsComponent`.
- `AudioSettingsSystem` remains the ECS normalization point.
- Managed audio presentation can read `AudioSettingsComponent` and apply it to Unity audio sources/mixer groups because Unity audio objects are managed.
- Do not add new runtime class names containing or ending in `Controller`, `Presenter`, `Bridge`, `Manager`, or `Button`.
- Prefer `ISystem` for settings projection and request handling. Use `SystemBase` only for unavoidable managed UI/audio-object presentation edges.
- Do not create a second audio settings model that competes with `AudioSettingsComponent`.

## Target Audio Semantics

| UI control | Model field recommendation | ECS projection |
| --- | --- | --- |
| Master volume slider | `MasterVolume` percent | `MasterVolume = 0..1` |
| Music volume slider | `MusicVolume` percent | `MusicVolume = 0..1` |
| Sound/SFX volume slider | `SfxVolume` percent | `SfxVolume`, `UiVolume`, `AmbienceVolume` default from SFX unless later split |
| Alerts volume slider | `AlertsVolume` percent | `AlertsVolume = 0..1` |
| Voice/ARIA volume slider | `VoiceVolume` percent | `VoiceVolume = 0..1` |
| Music toggle | `MusicEnabled` or `MusicMuted` | `MusicMuted = !MusicEnabled` |
| Sound toggle | `SoundEnabled` or `SfxMuted` | `SfxMuted`, `UiMuted`, `AlertsMuted`, `AmbienceMuted = !SoundEnabled` unless separate toggles exist |
| Voice/ARIA toggle | `VoiceEnabled` or `VoiceMuted` | `VoiceMuted = !VoiceEnabled` |

Recommendation:

- Use user-positive model names in UI settings: `MusicEnabled`, `SoundEnabled`, `VoiceEnabled`.
- Convert them to muted bytes only when projecting into `AudioSettingsComponent`.
- Keep master volume as a volume, not a mute, unless a master mute toggle is explicitly added.

## Phase 0 - Tracker And Audit

Goal:
Document the implementation path and current code anchors before runtime edits.

| Status | Task | Notes |
| --- | --- | --- |
| Complete | Create implementation tracker. | Current document. |

Completed steps:

- Audited `AudioSettingsComponent`, `AudioPlaybackPresentationSystemHelper`, `UiAudioSettingsProjectionSystem`, `SettingsPanelView`, `UISettingsModels`, and `SettingsService`.

## Phase 1 - Audio Settings Data Model

Goal:
Add explicit toggle state to the settings model without duplicating ECS audio settings.

| Status | Task | Notes |
| --- | --- | --- |
| Complete | Add `MusicEnabled` to `AudioSettingsModel`. | Default is `true`. |
| Complete | Add `SoundEnabled` to `AudioSettingsModel`. | Controls SFX/UI/alerts/ambience as a grouped user-facing toggle. |
| Complete | Add `VoiceEnabled` to `AudioSettingsModel`. | Controls ARIA and other voice bus playback. |
| Complete | Keep existing volume fields intact. | Existing PlayerPrefs values and tests preserved. |
| Complete | Add migration defaults for missing old prefs. | Existing users default enabled, not muted. |
| Complete | Update settings model equality/test helpers if present. | Current focused tests cover the new fields directly. |
| Complete | Add model-level EditMode tests. | Defaults and read/write round trip compile in `Game.Tests.Editor`. |

Completed steps:

- Added `MusicEnabled`, `SoundEnabled`, and `VoiceEnabled` to `AudioSettingsModel`.
- Preserved existing audio volume fields and defaults.
- Added default enabled state for missing old prefs through `SettingsService.Defaults` and fallback loads.
- Extended `AudioSettingsUiProjectionTests` coverage for defaults, persistence, reset, and mute projection.
- Revalidated source builds after local generated-project include gaps were cleared.

Validation:

- `dotnet build Game.Runtime.csproj --no-restore /clp:ErrorsOnly` passed with existing warnings only.
- `dotnet build Game.UI.Runtime.csproj --no-restore /clp:ErrorsOnly` passed with existing warnings only.
- `dotnet build Game.Tests.Editor.csproj --no-restore /clp:ErrorsOnly` passed with existing warnings only.

## Phase 2 - Persistence And Runtime Apply Contract

Goal:
Persist toggle state and make `ApplyRuntime` publish complete audio settings.

| Status | Task | Notes |
| --- | --- | --- |
| Complete | Add PlayerPrefs keys for music/sound/voice enabled state. | Uses generic keys without project-name prefixes. |
| Complete | Load toggle values in `SettingsService.Load`. | Missing keys use defaults. |
| Complete | Save toggle values in `SettingsService.Save`. | Persists only explicit settings model fields. |
| Complete | Reset restores enabled defaults. | Reset updates the saved model back to enabled defaults. |
| Complete | Keep `SettingsService.RuntimeApplied` as the projection trigger. | No new event bus introduced for settings. |
| Complete | Add persistence tests. | Defaults, save/load, reset, runtime apply event compile in focused tests. |

Completed steps:

- Added PlayerPrefs keys for `Audio.MusicEnabled`, `Audio.SoundEnabled`, and `Audio.VoiceEnabled`.
- Loaded toggle values with default-enabled migration behavior for old prefs.
- Saved toggle values alongside the existing volume settings.
- Confirmed reset writes enabled defaults through `SettingsService.ResetToDefaults`.
- Kept `SettingsService.RuntimeApplied` as the existing projection trigger.
- Extended persistence/reset tests in `AudioSettingsUiProjectionTests`.
- Revalidated source builds after local generated-project include gaps were cleared.

Validation:

- `Game.Tests.Editor.csproj` builds the persistence/reset coverage successfully.
- Reset sets Music/Sound/Voice toggles back to enabled in the focused test coverage.

## Phase 3 - Settings Panel UI Binding

Goal:
Make the visible settings controls reflect and mutate the settings model.

| Status | Task | Notes |
| --- | --- | --- |
| Complete | Add serialized toggle references to `SettingsPanelView`. | Music, Sound, Voice rows added to both settings view paths. |
| Complete | Bind toggle visuals from `UISettingsModel`. | Opening Settings reflects the model when rows are assigned. |
| Complete | Read toggle values back into `UISettingsModel`. | Apply uses current UI state. |
| Complete | Add listener cleanup. | Toggle listeners are wired and unwired with existing view lifecycle. |
| Complete | Ensure labels are clear. | Uses `MUSIC`, `SOUND`, and `VOICE`. |
| Complete | Update Canvas settings prefab if this remains active runtime. | Existing settings popup exposes and serializes the toggles. |
| Complete | Update UI Toolkit settings popup if active runtime path needs parity. | Audited assets; no active UI Toolkit settings popup/screen exists in this branch. |

Completed steps:

- Added `musicEnabledRow`, `soundEnabledRow`, and `voiceEnabledRow` to `SettingsPanelView`.
- Added the same toggle row support to `SettingsScreenView`.
- Bound toggle visual state from `UISettingsModel.Audio`.
- Read toggle values back into `UISettingsModel.Audio`.
- Added lifecycle-safe toggle listeners and model update handlers.
- Updated `SettingsPopupPrefabBuilder` so `SCN_SettingsPopup.prefab` rebuilds with `MusicEnabledRow`, `SoundEnabledRow`, and `VoiceEnabledRow`.
- Rebuilt `SCN_SettingsPopup.prefab` through Unity batchmode and verified the rows serialize into `SettingsPanelView`.
- Strengthened `SettingsPopupValidationTests` to require the audio toggle rows and serialized field references.
- Audited UI Toolkit assets for a settings parity target; the branch currently has no `.uxml`, no `.uss`, and no `*toolkit*` asset directory under `Assets`.

Validation:

- `Game.UI.Runtime.csproj` builds with the new serialized view fields.
- `dotnet build Game.Editor.csproj --no-restore /clp:ErrorsOnly` passed with existing warnings only.
- `dotnet build Game.Tests.Editor.csproj --no-restore /clp:ErrorsOnly` passed with existing warnings only.
- Unity batchmode `Game.Editor.SettingsPopupPrefabBuilder.Build` completed successfully.
- Unity batchmode `SettingsPopupValidationTests.RunFocusedValidation` passed `8` focused tests.
- `find Assets -name '*.uxml' -print`, `find Assets -name '*.uss' -print`, and `find Assets -type d -iname '*toolkit*' -print` returned no active UI Toolkit settings target to update.

## Phase 4 - ECS Projection And Audio Bus Mapping

Goal:
Make toggles and sliders affect real audio playback through the existing ECS audio bus settings.

| Status | Task | Notes |
| --- | --- | --- |
| Complete | Update `UiAudioSettingsProjectionSystem.ToAudioSettingsComponent`. | Removed `MusicMuted = 1` hard-code. |
| Complete | Project `MusicEnabled` to `MusicMuted`. | Disabled music means bus volume resolves to `0`. |
| Complete | Project `SoundEnabled` to SFX/UI/alerts/ambience mute bytes. | Matches user expectation for a single sound toggle. |
| Complete | Project `VoiceEnabled` to `VoiceMuted`. | ARIA clips are silenced when off. |
| Complete | Preserve per-bus volume mapping. | Master, music, SFX, alerts, voice preserved. |
| Complete | Normalize through `AudioSettingsSystem.NormalizeSettings`. | Volume ranges remain clamped. |
| Complete | Avoid direct playback from UI controls. | Settings update settings only, not command logic. |
| Complete | Add focused projection tests. | Model -> component coverage added for enabled/disabled and volumes. |

Completed steps:

- Removed the hard-coded `MusicMuted = 1` projection.
- Projected `MusicEnabled` to `AudioSettingsComponent.MusicMuted`.
- Projected `SoundEnabled` to `SfxMuted`, `UiMuted`, `AlertsMuted`, and `AmbienceMuted`.
- Projected `VoiceEnabled` to `VoiceMuted`.
- Preserved existing per-bus volume mapping.
- Kept projection normalization through `AudioSettingsSystem.NormalizeSettings`.
- Added focused projection tests for volume mapping and mute flags.
- Revalidated source builds after local generated-project include gaps were cleared.

Validation:

- `Game.Runtime.csproj`, `Game.UI.Runtime.csproj`, and `Game.Tests.Editor.csproj` builds passed with existing warnings only.
- Music off results in `MusicMuted = 1` in focused test coverage.
- Sound off results in `SfxMuted`, `UiMuted`, `AlertsMuted`, and `AmbienceMuted = 1` in focused test coverage.
- Voice off results in `VoiceMuted = 1` in focused test coverage.
- Master volume still affects all bus playback through existing resolver.

## Phase 5 - Music State And Fade Behavior

Goal:
Make music toggle behavior feel intentional and avoid hard cuts where practical.

| Status | Task | Notes |
| --- | --- | --- |
| Complete | Confirm route music is currently enabled or intentionally disabled. | Removed stale route-music suppression from `UiShellFlowSystem`; catalog contains menu and match music events. |
| Complete | When music is disabled, suppress new music-state requests or let bus mute silence them. | Central rule is now bus mute: route music still requests, `AudioSettingsComponent.MusicMuted` drives audible volume. |
| Complete | When music is re-enabled, resume/request the correct route music. | Active loop sources reapply settings by version, so unmuting restores the current route loop without needing a new request. |
| Complete | Use short fade if supported by existing music state. | Active Music bus sources now fade over `0.35s` when settings mute/volume changes apply. |
| Complete | Add smoke validation for menu and match music routes. | `UiShellAudioRoutePopupTests` now validates route music requests and duplicate-loop suppression. |

Completed steps:

- Removed the stale `MusicPlaybackEnabled = false` suppression from `UiShellFlowSystem`.
- Kept route music flowing through the existing ECS audio event path rather than adding a Settings-specific playback path.
- Added `AudioPlaybackPresentationSystemHelper.ApplySettingsToActiveSources` so active music loops and other active sources re-resolve volume from `AudioSettingsComponent`.
- Updated `AudioPlaybackPresentationBridgeSystemHelper` to reapply active-source settings when `AudioSettingsComponent.Version` changes.
- Added focused helper and bridge tests proving Music bus mute drops an active music source to `0` volume and unmute restores volume.
- Added timed fade support to `AudioPlaybackPresentationSystemHelper` for active Music bus sources while preserving immediate updates for non-fade callers.
- Updated `AudioPlaybackPresentationRuntimeView` to advance active-source fades using `Time.unscaledTime`.
- Updated the audio presentation bridge to apply Settings music changes with a `0.35s` fade.
- Added focused helper and bridge tests proving Settings music mute starts at current volume, fades down over time, then reaches silence.
- Updated focused shell route audio tests so menu and match routes emit route music through the ECS audio event path even when the Music bus is muted.
- Added duplicate-loop smoke coverage proving repeated menu/match route requests do not enqueue another Music loop once that loop is current.

Validation:

- Music off silences current music through active-source bus-volume reapply.
- Music on resumes the current route music when an active route loop exists.
- Settings-driven music mute/unmute fades active Music sources instead of jumping immediately.
- No duplicated music sources after repeated toggles.
- `dotnet build Game.Runtime.csproj --no-restore /clp:ErrorsOnly` passed with existing warnings only.
- `dotnet build Game.UI.Shell.Ecs.csproj --no-restore /clp:ErrorsOnly` passed with existing warnings only.
- `dotnet build Game.Tests.Editor.csproj --no-restore /clp:ErrorsOnly` passed with existing warnings only.
- Unity batchmode `AudioPlaybackPresentationSystemHelperTests.RunFocusedValidation` passed `7` focused tests.
- Unity batchmode `AudioPlaybackPresentationBridgeSystemHelperTests.RunFocusedValidation` passed `4` focused tests.
- Unity batchmode `AudioSettingsUiProjectionTests.RunFocusedValidation` passed `6` focused tests.
- Unity batchmode `UiShellAudioRoutePopupTests.RunFocusedValidation` passed `8` focused tests.

## Phase 6 - Immediate Feedback Samples

Goal:
Give the player confidence that settings changed, while respecting disabled buses.

| Status | Task | Notes |
| --- | --- | --- |
| Complete | Play a short UI confirm when Sound is enabled. | `SettingsSoundConfirm` uses existing `UI.Feedback.Toast.Positive` on the `UI` bus and is only raised on enable. |
| Complete | Play a short ARIA sample when Voice is enabled. | `SettingsVoiceSample` uses existing `VO.ARIA.Message.TacticalFeedbackRtsCameraRestored` on the `Voice` bus and is only raised on enable. |
| Complete | Avoid samples on initial bind. | `UIToggleRowView.Bind` uses `SetIsOnWithoutNotify`; focused test verifies settings bind stays silent. |
| Complete | Add cooldown to prevent spam. | Settings samples carry gateway cooldowns and reuse `AudioEventRequestSystem` cooldown handling. |

Completed steps:

- Added `SettingsSoundConfirm` and `SettingsVoiceSample` to `UIAudioEventKind`.
- Routed Sound confirm samples through the existing `UIAudioEventGateway` and `UiAudioEventBridgeSystem` path on bus `UI`.
- Routed Voice samples through the same gateway/bridge path on bus `Voice`.
- Updated `SettingsPanelView` and `SettingsScreenView` to raise samples only when the user enables Sound or Voice.
- Kept settings views as raw UI references and visual/model binding only; no direct audio playback was added to views.
- Added focused tests for settings sample event IDs, buses, cooldowns, and initial-bind silence.

Validation:

- Clicking Sound on produces one UI sample request.
- Clicking Voice on produces one ARIA sample request.
- Opening/binding Settings does not play samples.
- `dotnet build Game.UI.Runtime.csproj --no-restore /clp:ErrorsOnly` passed with existing warnings only.
- `dotnet build Game.UI.Shell.Ecs.csproj --no-restore /clp:ErrorsOnly` passed with existing warnings only.
- `dotnet build Game.Tests.Editor.csproj --no-restore /clp:ErrorsOnly` passed with existing warnings only.
- Unity batchmode `UiAudioEventViewTests.RunFocusedValidation` passed `7` focused tests in `/private/tmp/warline-unity-20260709-125242.log`.

## Phase 7 - Validation And Tests

Goal:
Prove the settings controls affect real runtime audio and do not regress UI architecture.

| Status | Task | Notes |
| --- | --- | --- |
| Complete | Add focused EditMode tests for settings defaults and persistence. | `AudioSettingsUiProjectionTests` covers new toggles, defaults, save/load, reset, and runtime apply event. |
| Complete | Add focused EditMode tests for model-to-`AudioSettingsComponent` projection. | `AudioSettingsUiProjectionTests` covers volumes and mute flags. |
| Complete | Add focused audio bus mute validation. | `AudioPlaybackPresentationSystemHelperTests` and bridge tests cover active source mute/unmute/fade behavior. |
| Complete | Add Settings popup UI binding validation. | `SettingsPopupValidationTests` covers popup rows and serialized references; `UiAudioEventViewTests` covers sample interactions. |
| Complete | Run Unity compile validation. | Focused Unity batchmode validation imported and compiled touched assemblies successfully. |
| Complete | Run runtime smoke: menu settings. | `SettingsAudioRuntimeSmokeValidation.RunMenuSettingsSmoke` validates live Menu-scene `SettingsService.ApplyRuntime` projection into `AudioSettingsComponent`. |
| Complete | Run runtime smoke: match settings. | `SettingsAudioRuntimeSmokeValidation.RunMatchSettingsSmoke` confirms match HUD remains open and audio buses obey settings. |

Completed steps:

- Reused existing model/default/persistence tests for the new audio toggles.
- Reused existing projection tests for `AudioSettingsComponent` mute and volume mapping.
- Reused active-source bus mute/fade tests from Phase 5.
- Added settings interaction sample coverage to `UiAudioEventViewTests`.
- Ran focused Unity batchmode validation after the Phase 6 interaction changes.
- Re-ran `SettingsPopupValidationTests.RunFocusedValidation` after the sample wiring to confirm the active Settings popup surface remains valid.
- Added `SettingsAudioRuntimeSmokeValidation.RunMenuSettingsSmoke` under editor tests.
- Ran the Menu scene in Unity play mode and verified disabled/enabled Music, Sound, and Voice settings project into the live default-world `AudioSettingsComponent`.
- Added `SettingsAudioRuntimeSmokeValidation.RunMatchSettingsSmoke` under editor tests.
- Ran the Match route from `Menu.unity` in Unity play mode and verified the Match HUD stays active while disabled/enabled Music, Sound, and Voice settings project into the live default-world `AudioSettingsComponent`.

Validation:

- Unity batchmode `SettingsPopupValidationTests.RunFocusedValidation` passed `8` focused tests in `/private/tmp/warline-unity-20260709-130154.log`.
- `dotnet build Game.Tests.Editor.csproj --no-restore /clp:ErrorsOnly` passed with existing warnings only after adding the menu smoke.
- Unity batchmode `SettingsAudioRuntimeSmokeValidation.RunMenuSettingsSmoke` passed in `/private/tmp/warline-unity-20260709-134549.log`.
- `dotnet build Game.Tests.Editor.csproj --no-restore /clp:ErrorsOnly` passed with existing warnings only after adding the match smoke.
- Unity batchmode `SettingsAudioRuntimeSmokeValidation.RunMatchSettingsSmoke` passed in `/private/tmp/warline-unity-20260709-141300.log`.

Validation command targets:

- Focused settings validation method if one exists or is added.
- `dotnet build Game.Runtime.csproj --no-restore /clp:ErrorsOnly`
- `dotnet build Game.Tests.Editor.csproj --no-restore /clp:ErrorsOnly`
- Unity batchmode focused validation after code changes.

## Acceptance Criteria

- Music toggle affects the actual `Music` bus.
- Sound toggle affects actual gameplay/UI/alert/ambience buses.
- Voice toggle affects the actual `Voice` bus used by ARIA.
- Volume sliders still work after toggles are added.
- Toggle state persists and restores correctly.
- Opening Settings reflects current settings state.
- Apply/Reset behavior is deterministic.
- No gameplay system starts reading UI controls directly.
- No new forbidden class names are introduced.
- Validation evidence is linked in the progress dashboard.
