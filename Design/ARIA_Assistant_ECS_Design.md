# ARIA Assistant ECS-Aligned Design

Date: 2026-07-08
Status: Active high-level design contract

## Implementation Status

Current rollout status, 2026-07-08: the ECS-aligned match HUD vertical slice is implemented and validated. The shipped slice includes the ARIA header button, assistant panel goals/recommendations, prioritized alerts/reports, `Show Me`, one safe `Do It` path through existing command boundaries, bounded `Give Control`, `Stop`, player override/cancellation, narration subtitle fallback, assistant settings persistence, aggregate validation, visual validation, and steady-state performance diagnostics.

Optional future polish remains separate from the core contract: pre-authored ARIA voice clips, platform/local TTS for low-priority dynamic reports, richer mission-specific recommendation sources, and additional bounded `Do It` command families can be added as later slices if they preserve the ECS data ownership and managed-helper boundary rules below.

## Purpose

This document updates the reusable ARIA assistant design so future implementation matches WarlineCapture's ECS/SOLID architecture, runtime naming conventions, and performance targets.

ARIA remains the command staff assistant described in `Design/FTUE_And_Command_Assistant_Design.md`, but implementation must use ECS data, Burst-compatible scoring where practical, dirty/versioned read models, narrow UI/audio presentation helpers, and no broad service/provider/controller/runtime shell.

## Source Documents

| Source | Use |
|---|---|
| `Design/FTUE_And_Command_Assistant_Design.md` | ARIA identity, teaching role, Chapter 1 assistant behavior, takeover limits. |
| `Design/AssistantPanel_M01_Implementation_Contract.md` | Existing M01 panel fields, Show Me / Do It / Stop behavior, and first assistant panel surface. |
| `Design/AssistantRuntime_M01_Wiring_Plan.md` | Historical M01 behavior flow. Use for intent/recommendation semantics, not for old service/provider/controller naming. |
| `Design/Match_HUD_And_Gameplay_Implementation_Spec.md` | Match HUD ownership, button behavior, warnings, command feedback, minimap, and match acceptance. |
| `Design/UIUX_Gameplay_Element_Alignment.md` | UI element ownership and data-source rules. |
| `Design/Audio_Design_Guidelines.md` | Voice, buses, audio event names, and playback rules. |
| `Design/Architecture/gameplay_solid_ecs_contract.md` | ECS-first runtime, naming, helper suffix, no-manager/controller/facade/service guardrails. |
| `Design/Architecture/performance_regression_contract.md` | Frame, GC, diagnostic, and validation budget rules. |
| `Design/Architecture/aria_assistant_ecs_implementation_tracker.md` | Implementation checklist and progress source for this feature. |

## Product Goals

- Put an ARIA button in the match header that opens a compact assistant panel without blocking normal command flow.
- Show current goals, priority alerts, and one recommended next action with a clear reason.
- Let the player ask ARIA to show the action, execute a bounded action, or temporarily take control.
- Let ARIA read important messages and low-priority tactical reports without spamming the player.
- Keep the player as commander. ARIA recommends and can briefly execute with permission; she does not become an unrestricted autopilot.
- Keep the system performant on mobile: no per-frame managed rebuilding, no string churn, no broad scans unless a dirty/version signal changed.

## Non-Goals

- No unrestricted mission autopilot in the first pass.
- No cloud text-to-speech dependency for core gameplay.
- No UI Toolkit or Canvas migration.
- No new `Manager`, `Controller`, `Facade`, `Service`, `Provider`, or broad `Runtime` shell for assistant ownership.
- No direct assistant calls into HUD child controls, selection internals, or legacy selection systems.
- No new updating `MonoBehaviour` loop for assistant gameplay logic.

## Player-Facing UX

### Match Header Button

The match HUD header gets an ARIA button near existing settings/pause/status controls. The button opens the assistant panel. It may show a small priority state:

| State | Meaning |
|---|---|
| Idle | No urgent recommendation. Panel still opens for goals and reports. |
| Recommendation | ARIA has a suggested next action. |
| Warning | A high-priority alert or blocked command explanation is available. |
| Control | ARIA is currently previewing or executing an approved bounded action. |

The button must be input-blocking for world selection. Tapping it must not select buildings or units behind the UI.

### Assistant Panel

The panel should be a work surface, not a lore popup. It contains:

| Area | Purpose |
|---|---|
| Current Goals | Short list of active objective, tactical objective, and optional economy/logistics goal. |
| Recommended Next Action | One primary action with title, reason, risk, expected result, and enabled state. |
| Alerts | High-priority command feedback, resource/fuel warnings, hostile threats, and blocked-action explanations. |
| Reports | Low-priority commentary such as "oil income is stable" or "air unit returning to base". |
| Controls | `Show Me`, `Do It`, `Give Control`, `Stop`, and close. |

Button behavior:

| Button | Behavior |
|---|---|
| `Show Me` | Opens a preview: UI pulse, world highlight, camera nudge, path/target hint, or objective focus. No gameplay order is committed. |
| `Do It` | Executes one approved typed command intent, such as selecting a unit, issuing a move, confirming a tutorial order, or focusing a target. |
| `Give Control` | Grants ARIA temporary bounded control for a short goal, such as guiding the next tutorial step or executing one simple support sequence. |
| `Stop` | Cancels preview/takeover and returns control to the player at the next safe boundary. |

### Control Ownership

ARIA control must be explicit and cancellable.

| State | Owner | Rule |
|---|---|---|
| `Player` | Player | Default. ARIA only observes and recommends. |
| `Guided` | Player | ARIA can show recommendations and hints. |
| `AssistantPreview` | ARIA presentation only | Highlight/camera preview only. No command commitment. |
| `AssistantTakeover` | ARIA bounded command | One bounded action or short goal. Must display ownership state. |
| `PlayerOverridePending` | Player requested cancel | ARIA stops at the next atomic boundary, then returns to `Player`. |

Player input outside the assistant panel during preview or takeover cancels or pauses ARIA control. Critical gameplay states, pause, route change, result state, unit death, invalid target, or ownership mismatch must also end takeover safely.

## ECS Architecture

### Entity Ownership

Assistant state should live on one singleton assistant entity in the match world. Mission-specific helper entities may exist for Chapter 1 or future missions, but they must carry pure data and be consumed by systems.

Recommended components:

| Type | ECS Type | Responsibility |
|---|---|---|
| `AssistantStateComponent` | `IComponentData` | Assistance level, panel open flag, active recommendation id, active control state, timestamps, dirty versions. |
| `AssistantControlOwnerComponent` | `IComponentData` | Current owner state, active intent id, cancel request, takeover timeout, and player override state. |
| `AssistantRecommendationReadModelComponent` | `IComponentData` | Current recommendation count/version, top priority, and UI dirty flag. |
| `AssistantNarrationStateComponent` | `IComponentData` | Active narration id, cooldowns, queue version, and voice settings snapshot. |
| `AssistantSettingsComponent` | `IComponentData` | Guidance level, narration mode, autoplay permission, and accessibility flags. |

Recommended dynamic buffers:

| Type | ECS Type | Responsibility |
|---|---|---|
| `AssistantGoalReadModelElement` | `IBufferElementData` | Current goal rows shown by the panel. |
| `AssistantRecommendationElement` | `IBufferElementData` | Candidate recommendations with score, priority, action type, target ids, and reason code. |
| `AssistantMessageElement` | `IBufferElementData` | Prioritized feedback/alert/report messages. |
| `AssistantNarrationRequestElement` | `IBufferElementData` | Text/audio event requests after message priority/cooldown filtering. |
| `AssistantCommandIntentRequestElement` | `IBufferElementData` | Typed assistant command requests raised by `Do It` or `Give Control`. |
| `AssistantCommandIntentResultElement` | `IBufferElementData` | Accepted/rejected/completed result records consumed by UI, recovery, and tutorial systems. |

`Element` means a dynamic-buffer row, not a class. It should contain Burst-friendly values: enums, entity references, fixed strings, numeric scores, and small ids. It must not contain managed objects, delegates, UnityEngine objects, TMP references, or strings.

Recommended enums:

| Type | Purpose |
|---|---|
| `AssistantControlState` | Player, Guided, AssistantPreview, AssistantTakeover, PlayerOverridePending. |
| `AssistantRecommendationKind` | Select, Move, Attack, Build, Produce, CameraFocus, Logistics, DefensiveAlert, Explain, Stop. |
| `AssistantMessagePriority` | Critical, High, Normal, Low. |
| `AssistantNarrationMode` | Off, CriticalOnly, Important, All. |
| `AssistantCommandIntentStatus` | Pending, Accepted, Rejected, Completed, Cancelled, TimedOut. |

### Systems

Use unmanaged `ISystem` and Burst-compatible jobs for data capture, scoring, filtering, and command request/result flow where practical.

| System | Responsibility |
|---|---|
| `AssistantGoalReadModelSystem` | Builds current goal rows from objective/resource/fuel/selection read models only when source versions change. |
| `AssistantRecommendationSystem` | Scores recommendation candidates from ECS read models and writes the top recommendation buffer. |
| `AssistantMessagePrioritySystem` | Merges command feedback, alerts, and reports into a priority/cooldown-filtered message buffer. |
| `AssistantNarrationRequestSystem` | Converts eligible messages into narration requests based on settings and cooldowns. |
| `AssistantCommandIntentSystem` | Accepts assistant command intent requests and routes to existing ECS command boundaries. |
| `AssistantControlOwnerSystem` | Handles preview/takeover timeout, player override, route/pause/result cancellation, and active-control ownership transitions. |
| `AssistantUiReadModelPublishSystem` | Publishes dirty UI read models from ECS data for managed UI helpers. |

Systems must not query or mutate Unity UI directly. Systems may write ECS read models and request/result buffers.

### Managed Presentation Boundaries

Managed code is allowed only at narrow UI, camera, highlight, audio, and persistence boundaries.

Approved helper names:

| Helper | Responsibility |
|---|---|
| `MatchHudAssistantUiSystemHelper` | Header button binding, panel open/close, and UI read-model application. |
| `AssistantPanelUiSystemHelper` | Binds goal/recommendation/message read models to existing Canvas/TMP controls. |
| `AssistantHighlightPresentationSystemHelper` | Applies UI pulses, world highlights, path previews, and camera nudges requested by ECS data. |
| `AssistantNarrationPresentationSystemHelper` | Plays pre-authored audio clips or platform TTS fallback for narration requests. |
| `AssistantSettingsPersistenceSystemHelper` | Loads/saves assistant settings through existing persistence boundaries. |

These helpers must not own assistant policy, recommendation scoring, command decision logic, or gameplay command execution. They consume read models and enqueue typed ECS requests.

## Audio And Read-Aloud Technology

Recommended phased approach:

| Phase | Technology | Reason |
|---|---|---|
| 1 | Text/subtitle messages only, with audio event ids stored in data. | Fast, deterministic, no platform dependency, safe for validation. |
| 2 | Pre-authored or generated `AudioClip` assets for common ARIA lines. | Best quality, predictable memory, localization-friendly, no runtime stalls. |
| 3 | Optional platform/local TTS for low-priority dynamic reports. | Useful for generated status messages, but must be settings-gated and optional. |

Core critical gameplay messages should use pre-authored clips or text fallback. Runtime cloud TTS should not be required for gameplay because it introduces latency, network failure modes, privacy concerns, cost, and certification risk on mobile.

Narration rules:

- Always show text when audio plays.
- Critical messages can interrupt low-priority narration.
- Low-priority reports must be coalesced and rate-limited.
- Repeated alerts must collapse into one updated message.
- Narration must obey player settings and accessibility preferences.
- Audio playback belongs in `AssistantNarrationPresentationSystemHelper`, not ECS systems.

## Performance Contract

Assistant implementation must be versioned and event-driven.

- No per-frame rebuilding of all goals, recommendations, messages, or command availability.
- No managed allocations in frame hot paths.
- No LINQ, string formatting, `new List<>`, or array churn in update paths.
- Use `FixedString*Bytes`, enums, integer ids, entity references, and blob/config references in ECS data.
- Use source version numbers for objectives, selection, command feedback, resources, fuel, threats, and route state.
- Only publish UI when the assistant read-model version changes.
- Keep dynamic buffers bounded. Drop or coalesce stale low-priority messages.
- Keep `Give Control` command chains short and measurable.
- Add focused performance diagnostics for assistant update time and allocations before enabling broad runtime behavior.

## Naming Contract

New assistant implementation must not introduce these broad names:

- `WarlineCaptureAssistantService`
- `AssistantContextProvider`
- `M01AssistantRecommendationProvider`
- `CommandIntentExecutor`
- `M01AssistantCommandRuntime`
- `AssistantPanelController`
- `AssistantHighlightController`

Use ECS components, buffers, and systems for logic and state. Use narrow `*UiSystemHelper`, `*PresentationSystemHelper`, `*PersistenceSystemHelper`, or `*UtilitySystemHelper` suffixes for managed boundaries.

`*System` is allowed for actual ECS systems or established narrow algorithm owners. Do not use plain `*System` to hide a service, controller, facade, or manager.

## First Vertical Slice

The first implementation slice should deliver:

1. Match header ARIA button.
2. Panel open/close with current goals, top recommendation, and message list.
3. One ECS-backed recommendation from real match read models.
4. `Show Me` preview through ECS request and narrow presentation helper.
5. `Do It` for one safe command intent through existing ECS command boundaries.
6. `Give Control` as a bounded one-step tutorial/action mode with visible ownership and `Stop`.
7. Priority message queue with subtitle text and audio event ids.
8. Settings for narration mode and assistant control permission.
9. Focused tests for ECS data, UI read-model dirty gating, command intent routing, and no forbidden naming drift.

## Acceptance

Feature acceptance requires:

- Header button opens the panel and blocks world clicks behind UI.
- Panel shows current goals and one relevant recommendation from live data.
- `Show Me`, `Do It`, `Give Control`, and `Stop` route through ECS data boundaries.
- ARIA stops instantly or at the next safe atomic boundary when player overrides.
- Alerts/reports are prioritized and rate-limited.
- No forbidden service/provider/controller/runtime names are added for new assistant implementation.
- `git diff --check`, focused assistant tests, architecture guardrails, and Unity visual validation pass before claiming the feature works.

Current validation closeout is recorded in `Design/Architecture/aria_assistant_ecs_implementation_tracker.md`. The rollout includes aggregate focused Unity validation, match HUD visual-surface checks, forbidden-name scans, compile validation, and assistant steady-state performance diagnostics with zero measured managed allocations in the dedicated test fixture.
