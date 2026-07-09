# POP-13 ARIA Command Assistant Functional Implementation Tracker

Date: 2026-07-09
Status: Implementation-ready specification; runtime phases remain in progress
Last contract audit: 2026-07-09

## Purpose

Make the ARIA command assistant popup match the V01 target-lock mockup while keeping every displayed detail backed by real gameplay data, ECS read models, and bounded command mechanics.

This tracker is intentionally stricter than a visual spec. A panel detail is not considered complete unless it is connected to a real data source or clearly marked as static chrome.

Normative language in this document is intentional. `Must` and `must not` are acceptance requirements. When a required gameplay fact is unavailable, the implementation must hide the field or disable the action with a truthful reason. It must not synthesize a plausible value from placeholder text, screen position, camera position, or unrelated combat state.

## Target Outcome

- The ARIA button moves to the top-left match HUD slot currently occupied by the objective panel.
- The always-visible objective panel is removed or hidden from the normal HUD surface.
- Current objectives move into the ARIA popup as first-class, readable rows.
- The popup uses the target-lock visual language from `reference/POP-13_ARIACommandAssistant_TargetLock_V01.png`.
- Goals, alerts, reports, recommendation, target lock state, ARIA voice state, and command buttons are all backed by ECS data.
- `SHOW ME`, `DO IT`, and `STOP` are truthful. Disabled buttons must explain why through the recommendation/alert rows, not by implying unavailable control.
- Threat/under-attack alerts identify the affected friendly unit, hostile source when known, and reason.
- The implementation remains ECS/SOLID aligned: data in components and buffers, logic in ECS systems, presentation in narrow UI helpers.
- No hot-path managed string churn, LINQ, per-frame broad scans, unbounded buffers, or UI rebuilding when versions have not changed.

## Progress Summary

Overall implementation progress: **5% (5 / 106 checklist items complete)**.

Progress is checklist-based. Each markdown checklist item in the phase sections below counts as one item. Partial `[~]` milestone rows are useful status notes, but they do not count as complete until their exit criteria are met.

| Phase | Status | Complete | Total | Progress | Notes |
|---|---|---:|---:|---:|---|
| 0. Visual target and contract setup | In Progress | 5 | 7 | 71% | Functional pass uses code-built/runtime UI; optional production sprite replacement is deferred. |
| 1. Baseline tests before feature work | Not Started | 0 | 7 | 0% | Remove actionable placeholders, add runtime gates/contracts, and lock current behavior. |
| 2. Top-left HUD relocation | Not Started | 0 | 7 | 0% | Move ARIA into the exact objective-panel slot with fallback/restore behavior. |
| 3. Structured goal rows | Not Started | 0 | 8 | 0% | Publish mission-owned structured objectives and elapsed whole seconds. |
| 4. Structured alerts and reports | Not Started | 0 | 7 | 0% | Split versioned high-priority alerts from lower-priority reports. |
| 5. Real threat telemetry | Not Started | 0 | 13 | 0% | Capture bounded damage observations and identify player target/source/impact. |
| 6. Recommendation scoring and target lock | Not Started | 0 | 7 | 0% | Make target-lock graphic fully data-driven. |
| 7. Complete command mechanics | Not Started | 0 | 13 | 0% | Execute and correlate concrete select, move, attack, focus, and stop intents. |
| 8. ARIA voice state panel | Not Started | 0 | 6 | 0% | Show true narration request/audio status. |
| 9. Popup visual implementation | Not Started | 0 | 10 | 0% | Bind stable rows and wide/compact lifecycle behavior. |
| 10. Gateway publishing and caching | Not Started | 0 | 7 | 0% | Preserve explicit-version, cached no-allocation managed publishing. |
| 11. Validation and acceptance | Not Started | 0 | 14 | 0% | Compile, source-truth, command/audio, visual, play, and performance gates. |

Progress update rule: update this table in the same change that completes or adds tracker items. Do not count a row complete until code, tests or validation notes, and docs are updated.

## Source References

| Source | Required use |
|---|---|
| `reference/POP-13_ARIACommandAssistant_TargetLock_V01.png` | Visual direction only. Do not ship as runtime UI. |
| `Design/ARIA_Assistant_ECS_Design.md` | Architecture, ownership, performance, naming guardrails. |
| `Design/AssistantPanel_M01_Implementation_Contract.md` | Existing assistant interaction semantics. |
| `Design/FTUE_And_Command_Assistant_Design.md` | ARIA product role and takeover limits. |
| `Assets/Game/Scripts/Components/AssistantComponents.cs` | Current assistant state, buffers, recommendation, message, narration, command intent, preview highlight contracts. |
| `Assets/Game/Scripts/UI/Shell/Ecs/AssistantReadModelSystems.cs` | Current goal and recommendation source systems. |
| `Assets/Game/Scripts/UI/Shell/Ecs/AssistantMessagePrioritySystem.cs` | Current alert/message source system. |
| `Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.cs` | Current cached UI publishing path. |
| `Assets/Game/Scripts/UI/Screens/MatchHudAssistantUiSystemHelper.cs` | Current code-built ARIA surface. |
| `Assets/Game/Scripts/UI/Screens/AssistantPanelUiSystemHelper.cs` | Current panel binding helper. |
| `Assets/Game/Scripts/Components/UnitCombatComponents.cs` | Combat enrichment data for real threat rows. `RecentAttacker` remains retaliation state and is not the ARIA event source. |
| `Assets/Game/Scripts/Components/AudioComponents.cs` | Audio request/result statuses used by the truthful ARIA voice state. |
| `Assets/Game/Scripts/Components/SelectionInputRequestComponents.cs` | Existing selection command request/result boundary and canonical tactical reason codes. |
| `Assets/Game/Scripts/Systems/UnitMoveOrderRequestSystem.cs` | Existing typed single-source move order boundary. |
| `Assets/Game/Scripts/Systems/UnitAttackOrderRequestSystem.cs` | Existing typed single-source/selected attack order boundary. |
| `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs` | Current match HUD projection contract. It is not authoritative mission/combat state. |
| `Assets/Game/Scripts/UI/Shell/Ecs/UiShellStateSystem.cs` | Current placeholder defaults that must become empty and non-actionable before live ARIA publishing. |
| `Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab` | Existing `HeaderContent/ObjectivesPanel` placement and objective/elapsed visual that ARIA replaces. |
| `Assets/Game/Scripts/Components/FactionAIComponents.cs` | `Faction` ownership id. |
| `Assets/Game/Scripts/Components/FactionIdentity.cs` | Player/hostile faction rules. |

## Progress Legend

| Mark | Meaning |
|---|---|
| `[x]` | Implemented or already available. |
| `[~]` | Partially implemented. |
| `[ ]` | Not implemented. |
| `[!]` | Blocked until a prerequisite is complete. |

## Milestone Tracker

| Status | Milestone | Exit criteria |
|---|---|---|
| `[x]` | V01 target reference saved | Mockup and prompt exist under this folder. |
| `[~]` | Readable code-built popup pass | Current popup is enlarged and styled, but some details are decorative/static. |
| `[~]` | Thin ECS ARIA model exists | Current model has text goals, alerts, narration, one recommendation, command buttons, preview highlight. |
| `[ ]` | Structured panel model | Popup binds structured row models instead of plain multiline strings. |
| `[ ]` | Top-left HUD relocation | ARIA button owns current objective-panel space; objective panel presentation is removed from always-visible HUD. |
| `[ ]` | Objectives inside popup | Existing objective ECS data is rendered as real ARIA goal rows. |
| `[ ]` | Real threat telemetry | Threat rows name friendly target, attacker when known, threat kind, distance, damage/health state, last hit age. |
| `[ ]` | Real target-lock panel | Target-lock graphic shows selected recommendation target, validity/readiness, distance, faction, health, and preview/dispatch state. |
| `[ ]` | Complete command mechanics | `SHOW ME`, `DO IT`, and `STOP` route to existing typed command boundaries for select, move, attack, focus, and cancel. |
| `[ ]` | Narration state is visible and true | ARIA voice panel reflects correlated queued/accepted/presented/failed/text-only/off state without claiming clip completion. |
| `[ ]` | Performance validation | Focused tests prove no repeated allocations/rebuilds when versions are unchanged. |
| `[ ]` | Visual validation | Desktop/mobile screenshots show readable popup, top-left ARIA button, and no overlapping HUD. |

## Current Baseline

### Already implemented infrastructure

- `AssistantGoalReadModelSystem` reads the current match HUD projection and builds `AssistantGoalReadModelElement` rows. The projection is not yet an authoritative objective source.
- `AssistantRecommendationSystem` builds one top recommendation from threat visibility, fuel logistics, selection state, focused unit state, or active objective.
- `AssistantMessagePrioritySystem` converts threat/feedback status surfaces into `AssistantMessageElement` rows.
- `AssistantNarrationRequestSystem` converts eligible messages into `AssistantNarrationRequestElement` rows with cooldown and priority rules.
- `AssistantNarrationAudioRequestSystem` enqueues a one-shot `Voice` audio event when a narration request has an audio event id.
- `UiShellEcsGateway.TryReadMatchHudAssistantPanel` caches the managed `UiAssistantPanelModel` and only rebuilds it when source versions/counts change.
- `AssistantPanelUiSystemHelper.ApplyReadModel` skips unchanged model versions.
- `AssistantCommandIntentSystem` supports preview/highlight/focus behavior and a safe selection execution path.

### Not yet real enough for the mockup

- `UiMatchHudStatusSurfacesComponent`, `UiMatchHudStatusSurfacesModel.Default`, and gateway fallback creation still contain sample objectives, elapsed time, threat, and feedback values. Those values are fixtures, not gameplay truth.
- No active runtime mission/objective publisher currently owns objective ids, body text, priority, primary state, completion, or match elapsed time.
- Goals and alerts are flattened to strings before UI binding.
- The mockup row chips, target-lock radar, telemetry markers, and voice waveform are currently presentation scaffolding.
- The current recommendation model does not expose rich target telemetry.
- Threats are mostly text-oriented through HUD status data. ARIA cannot yet reliably state which exact friendly unit is under attack and which entity attacked it.
- `DO IT` does not yet execute all recommended action families. Move/attack are mapped in intent enums, but unsupported execution currently rejects non-preview intents.
- The button is still in the header/right area, not the top-left objective-panel slot.

## Locked Implementation Contracts

The decisions in this section resolve the implementation audit. An implementation change that needs to violate one of these contracts must update this tracker first and explain the replacement contract.

### Runtime activation and safe defaults

- Assistant gameplay systems may publish actionable goals, threats, recommendations, or narration only while `UiShellStateComponent.ActiveRoute == UIRoute.Match`, `CurrentMode == UiShellMode.MatchHud`, no route transition is active, and `MatchStartQueueComponent.HasStarted != 0`.
- A missing `UiShellStateComponent` or `MatchStartQueueComponent` is inactive, not implicitly started.
- Loading, main-menu, and match-startup frames publish an empty assistant model. They must not carry the previous match's rows or audio requests.
- `DefaultMatchHudStatusSurfaces()`, `EnsureMatchHudStatusSurfacesState`, and `UiMatchHudStatusSurfacesModel.Default` must use non-actionable defaults for ARIA-owned inputs: empty objective rows, empty elapsed text, `ThreatVisible = 0`, empty threat strings/audio id, `JumpEnabled = 0`, `FeedbackVisible = 0`, and empty feedback strings/audio id.
- Unit tests may create explicit synthetic objective/threat fixtures. Production defaults must never contain `Neutralize hostile patrol`, `HOSTILE CELL SPOTTED`, `Blocked: civilian zone`, or any other gameplay claim.
- Route exit sets objective runtime inactive, clears objective/goal/threat/recommendation/message/narration/dispatch/highlight/target-lock rows, and clears panel-open/control state. It does not issue gameplay stop orders to units.
- At a new match start, initialize the assistant combat-observation cursor to the queue's current `LastEventId` before enabling threat publication so observations retained from an earlier route cannot become new alerts.

### Runtime data provenance

| Displayed fact | Authoritative source | ARIA behavior when unavailable |
|---|---|---|
| Objective id/title/body/state/priority/primary | Mission-owned `MatchObjectiveRuntimeElement` buffer described below | Hide the row. Do not use prefab or UI fallback text. |
| Match elapsed time | `MatchObjectiveRuntimeStateComponent.ElapsedWholeSeconds`, updated once per second while the match is active | Hide elapsed text. |
| Friendly unit under attack | `CombatDamageObservationElement.TargetEntity` after player-faction validation | Do not create a threat row. |
| Attacker/source | `CombatDamageObservationElement.SourceEntity` plus captured source position | Show `SOURCE UNKNOWN` only when the target fact is valid but source identity cannot be resolved. |
| Damage/health | Applied damage and post-hit health captured by the damage producer | Hide the individual metric if not present; do not derive it from weapon configuration. |
| Unit names | `UnitDisplayInfo.Name`; fixed `FRIENDLY UNIT`/`HOSTILE SOURCE` fallback | Never use Unity object names in retail UI. |
| Faction relation | `Faction` and `FactionIdentity` | Hide relation and disable attack execution if ownership cannot be proven. |
| Distance | Captured source/target world positions, or current transforms when both entities still exist | Hide distance. Camera distance is never a substitute. |
| Recommendation target | Concrete target/entity/cell/world position stored in `AssistantRecommendationElement` | `CanExecute = 0`; show a fixed rejection reason. |
| Target-lock state | Recommendation validity, preview row, command dispatch row, and command result | Hide the lock block when no target exists. |
| Voice status | Correlated `AssistantNarrationRequestElement`, audio request, and `AudioPlaybackResultElement` | Show `TEXT ONLY`, `OFF`, or the exact failure state. |
| Button enabled state | The typed recommendation plus current validation result | Disable the button and expose the fixed reason in the recommendation block. |

### Objective ownership contract

`UiMatchHudStatusSurfacesComponent` is a UI projection, not mission authority. Add a mission-neutral runtime contract in a focused gameplay component file:

```csharp
public enum MatchObjectiveState : byte
{
    Active = 0,
    Complete = 1,
    Warning = 2,
    Blocked = 3,
    Failed = 4
}

public struct MatchObjectiveRuntimeStateComponent : IComponentData
{
    public uint Version;
    public FixedString64Bytes MissionId;
    public float MatchStartedAt;
    public int ElapsedWholeSeconds;
    public byte MatchActive;
}

[InternalBufferCapacity(3)]
public struct MatchObjectiveRuntimeElement : IBufferElementData
{
    public int GoalId;
    public FixedString64Bytes ObjectiveId;
    public MatchObjectiveState State;
    public byte Priority;
    public byte IsPrimary;
    public FixedString64Bytes Title;
    public FixedString128Bytes Body;
    public Entity TargetEntity;
    public int2 TargetCell;
    public float3 WorldPosition;
    public byte HasTargetCell;
    public byte HasWorldPosition;
}
```

Rules:

- A mission/startup system owns this buffer. ARIA and managed UI are read-only consumers.
- `GoalId` is stable for the lifetime of a match. `ObjectiveId` is the authored id such as `objective.destroy_patrol_group`.
- The active mission may publish zero to three visible rows. No active mission means zero rows.
- On the transition to a started match, the mission/startup owner sets `MatchStartedAt` from `SystemAPI.Time.ElapsedTime`, resets elapsed to zero, and sets `MatchActive = 1`. Route entry alone does not start/reset the timer.
- `Version` increments only when a visible objective field changes or `ElapsedWholeSeconds = floor(currentElapsedTime - MatchStartedAt)` advances. Elapsed time may update once per second, never every frame.
- The first playable M01 publisher must use the authored tactical objective id and its real patrol completion state. It must only publish that M01 objective when `MissionId` identifies M01. Empty/unknown mission identity produces zero objectives; it must not assume M01 from the current map or prefab.
- `AssistantGoalReadModelSystem` maps this buffer into assistant goals. The old three status-surface text slots remain compatibility output during migration, not input to ARIA after Phase 3.

### Combat observation and threat lifecycle

`RecentAttacker` remains retaliation state and is not reliable telemetry because combat logic consumes/removes it. Add a neutral, bounded combat observation queue that can also be reused by diagnostics or later HUD systems:

```csharp
public enum CombatDamageSourceKind : byte
{
    Unknown = 0,
    DirectFire = 1,
    BuildingDefense = 2,
    GroundMissile = 3,
    AirMissile = 4,
    Explosion = 5
}

public struct CombatDamageObservationQueueComponent : IComponentData
{
    public int LastEventId;
    public uint Version;
}

[InternalBufferCapacity(8)]
public struct CombatDamageObservationElement : IBufferElementData
{
    public int EventId;
    public int Frame;
    public Entity SourceEntity;
    public Entity TargetEntity;
    public CombatDamageSourceKind SourceKind;
    public int DamageApplied;
    public int TargetHealthAfter;
    public int TargetMaxHealth;
    public float ObservedAt;
    public float3 SourceWorldPosition;
    public float3 TargetWorldPosition;
}
```

Producer and retention rules:

- Every code path that decreases `UnitHealth` must append one observation after clamping health. Initial required producers are direct unit fire, building-defense fire, ground-missile direct/splash damage, and air-missile impact damage.
- `DamageApplied` is `previousHealth - newHealth`; it is never copied from `UnitAttack.Damage` or weapon configuration because armor/clamping may change applied damage.
- Producers increment `LastEventId` and `Version`. The buffer is pre-sized once and retained as a ring of at most 64 observations; appending the 65th removes the oldest row.
- The queue is gameplay-neutral. Damage producers do not reference assistant components, UI types, narration, or recommendation policy.

Threat-consumer rules:

- `AssistantThreatReadModelSystem` processes only observations with `EventId` greater than its stored cursor. It must not scan all units each frame.
- Only observations whose target is player-controlled may become ARIA threats. Damage to enemy-only or neutral entities is ignored unless an authoritative objective explicitly marks that entity as player-protected.
- The system resolves at most the bounded new observations, then upserts at most four visible threats.
- Stable `ThreatId` is a deterministic non-zero hash of friendly entity index/version, hostile entity index/version, and threat kind. A repeated hit from the same source updates the same row.
- A row is `Critical` when the target is destroyed, health is at or below 25%, or one observation removes at least 25% of maximum health. Other verified damage rows are `High`.
- `UnitAirComponent` classifies an air source; `RuntimeBuildingCombatTag`/`BuildingDefenseWeapon` classifies building defense; otherwise the source is ground/unknown according to the captured source kind.
- Distance is horizontal Euclidean distance between captured source and target positions. Display rounds to the nearest whole world unit and uses `m` only under the existing one-world-unit/one-meter gameplay convention.
- Rows expire 6 seconds after the latest observation. Expiry is evaluated when the observation queue version changes or when the earliest row expiry boundary is reached; it is not a reason for a broad per-frame query.
- Threat narration uses a suppression key derived from `ThreatId`. It may narrate on first insertion or priority escalation, but not on every hit. The same threat cannot narrate again within 8 seconds.
- There is no fallback generic warning beep. If a matching ARIA voice event is unavailable, the alert remains text-only.
- Threat narration may reference only a cataloged ARIA event on the `Voice` bus. It must never substitute `alert_*`, `game_resource_*`, mission timer, or generic unit-under-attack SFX.

### Recommendation target and command execution contract

The initial functional slice executes one bounded source entity per recommendation. Group-wide autonomous control is outside this popup pass.

Recommendation targeting rules:

- `Select`: target a concrete selectable player entity when one is supplied by mission/FTUE state; otherwise `DO IT` may only enter existing selection mode.
- `Move`: requires a living, player-owned `SourceEntity` and a concrete objective/tactical `TargetCell` or `WorldPosition`. The destination must come from mission/objective/preview data. ARIA must not choose a random point, camera point, or position offset.
- `Attack`: requires a living, player-owned attack-capable `SourceEntity` and a living hostile `TargetEntity`. The target may come from an active objective or the top verified threat. ARIA must not scan the map to invent an attack target.
- `FocusCamera`: requires a valid entity transform or objective world position and never issues a gameplay order.
- A move/attack recommendation without these facts remains preview-only with `CanExecute = 0` and a fixed reason such as `No valid destination` or `No verified hostile target`.
- Before dispatch, revalidate entity existence, ownership, health, target relation, target cell bounds, and recommendation id/source version. A stale recommendation is rejected rather than recomputed inside the command system.
- For move, an authored `TargetCell` is authoritative. When only `WorldPosition` exists, convert it through the active `GridConfig`/`GridUtils.WorldToCell` and reject an out-of-bounds result. Do not issue a move when neither form is valid.

Dispatch mapping:

| Assistant intent | Existing downstream boundary | Completion source |
|---|---|---|
| `SelectEntity` | `RtsSelectionCommandIntentRequestElement` with `FocusUnit` or `EnterSelectionMode` | Matching `RtsSelectionCommandResultElement` |
| `MoveToWorldPosition` | `UnitMoveOrderRequestSystem.EnqueueImmediateMoveOrder(SourceEntity, TargetCell)` | Matching `UnitMoveOrderResultElement` |
| `AttackEntity` | `UnitAttackOrderRequestSystem.EnqueueDirectAttackTarget(SourceEntity, TargetEntity, TargetCell, WorldPosition)` | Matching `UnitAttackOrderResultElement` |
| `FocusCamera` | Existing `RtsCameraRequestElement` queue | Completed when the request is accepted into the camera queue |
| `StopAssistantControl` | Existing assistant control/preview cancellation | Completed when preview/dispatch ownership returns to `Player` |

Add a bounded dispatch row:

```csharp
public enum AssistantDownstreamCommandKind : byte
{
    None = 0,
    Selection = 1,
    MoveOrder = 2,
    AttackOrder = 3,
    Camera = 4
}

[InternalBufferCapacity(4)]
public struct AssistantCommandDispatchElement : IBufferElementData
{
    public int AssistantRequestId;
    public int RecommendationId;
    public AssistantCommandIntentKind IntentKind;
    public AssistantDownstreamCommandKind DownstreamKind;
    public int DownstreamRequestId;
    public AssistantCommandIntentStatus Status;
    public float RequestedAt;
}
```

Keep at most eight dispatch rows and remove oldest terminal rows first. `AssistantCommandIntentSystem` writes `Accepted` only after downstream enqueue succeeds. A result bridge writes `Completed` or `Rejected` from the correlated downstream result and copies canonical `TacticalCommandReasonCode` values into `ReasonCode`. Do not create a second assistant-specific reason-code taxonomy for failures already represented by `TacticalCommandReasonCode`.

Pending dispatches time out after 5 seconds of elapsed simulation time. `STOP` cancels undispatched assistant work, preview/highlight state, and assistant ownership. It does not silently undo an atomic move/attack order that gameplay already accepted; the player may issue the normal unit `STOP` command separately.

### Target-lock truth contract

- Target priority is: current recommendation target, active preview target, active command-dispatch target, then top verified player-affecting threat. Do not fall back to the camera or an arbitrary focused unit.
- Lock state is `Candidate` when a real target exists, `Preview` while `SHOW ME` is active, `Executable` when all dispatch validation passes, `Executing` while a downstream request is pending, and `Invalid` after a validation/rejection result until the source model changes.
- Target name, source name, faction relation, health, and distance follow the provenance table. Each unavailable metric is hidden independently.
- Do not display a numeric confidence percentage. The project has no probabilistic confidence source. Use a truthful readiness label: `PREVIEW`, `READY`, `ACTIVE`, or `BLOCKED` from lock/command state.
- Camera visibility is not tactical visibility. Only explicit intel such as `ScanIntelRevealedTag`, a verified damage observation, or an authoritative objective/command target may justify a visible/detected label.

### Narration and audio-state truth contract

- Extend `AssistantNarrationRequestElement` with the returned `AudioPlaybackRequestId` so narration can correlate to `AudioPlaybackResultElement`.
- Extend `AssistantNarrationStateComponent` with `ActiveAudioPlaybackRequestId`, `LastAudioStatus`, `LastAudioFailureReason`, and `LastPresentedAt`. `IsSpeaking` remains false because clip-finished state is not currently observable.
- Add `AssistantNarrationAudioResultProjectionSystem`. It updates narration state/version only when the correlated latest audio result changes; it never scans unrelated entities.
- Add a generic `AudioPlaybackResultQueueComponent.Version` and increment it whenever simulation or presentation appends a result. Bound terminal audio request and result history to the newest 256 rows, trimming only after a new terminal result. ARIA caches the result version and searches newest-to-oldest for its one active request id.
- Resolve the latest result for that request id by buffer order/`ProcessedAt`; a later presentation result supersedes the earlier simulation-accept result.
- UI states are limited to facts currently observable by the audio pipeline:
  - `OFF`: narration mode is off, or effective master/voice volume from `AudioSettingsComponent` is zero.
  - `TEXT ONLY`: narration text exists but no audio event id is available.
  - `QUEUED`: narration/audio request is pending and has no terminal audio result.
  - `ACCEPTED`: the simulation audio queue accepted the request but presentation has not confirmed it.
  - `PRESENTED`: `AudioPlaybackRequestStatus.Presented` confirms playback was started.
  - `FAILED`: rejected, cooldown skipped, missing event, missing clip, or culled; show a user-safe reason.
- Do not label a request `Completed` or keep `IsSpeaking` active from enqueue time alone. Actual clip-finished state is out of scope until the audio presentation layer publishes source completion.
- The waveform may pulse for at most 0.8 seconds on transition to `PRESENTED`. It must be still in every other state and must not imply that playback is continuing.
- Retail UI never displays raw event ids or hashes. Tests and diagnostics may inspect them.

### Version ownership and system ordering

- Do not add `AssistantPanelReadModelVersionComponent`; it would duplicate existing state and gateway cache ownership.
- Add focused version state only where none exists: `AssistantMessageReadModelComponent`, `AssistantThreatReadModelStateComponent`, and `AssistantTargetLockReadModelComponent`.
- `AssistantMessageReadModelComponent` stores `Version`, visible count, last consumed command-feedback version, and `NextAgeBoundaryAt`; age/expiry is reevaluated only when a source changes or that boundary is reached.
- Existing `AssistantStateComponent.SourceVersion` owns goal-source changes, `AssistantRecommendationReadModelComponent.Version` owns recommendations, and `AssistantNarrationStateComponent.Version` owns narration/audio-state changes.
- Add `AssistantRecommendationEvaluationStateComponent` to cache the last goal, threat, focused-unit command-state, fuel, route, and control versions. `AssistantRecommendationSystem` returns before rebuilding/scoring when that tuple is unchanged.
- The initial single-source slice uses `FocusedUnitUiReadModelComponent` only. Remove the per-frame `SelectedUnitTag.CalculateEntityCount()` dependency; group recommendation/execution requires a later versioned selection-summary contract.
- Existing `UiDirty` fields are compatibility/diagnostic flags. New correctness must not depend on setting or clearing them.
- `UiShellEcsGateway` compares the explicit source-version tuple and fixed row counts. When the tuple changes, it rebuilds the fixed-slot managed model once and increments a monotonic cached panel version. It must not hash/scan all row contents on unchanged polls.
- `AssistantPanelUiSystemHelper` compares that one managed model version before touching TMP, images, buttons, or row visibility.

Required order and attributes:

1. Simulation damage systems append combat observations before `PresentationSystemGroup` begins.
2. `AssistantThreatReadModelSystem`: `[UpdateInGroup(typeof(PresentationSystemGroup))]` and `[UpdateBefore(typeof(AssistantRecommendationSystem))]`.
3. `AssistantGoalReadModelSystem`: existing presentation group; `AssistantRecommendationSystem` declares `UpdateAfter` for both goals and threats.
4. `AssistantRecommendationSystem` returns early when its locked input-version tuple is unchanged.
5. `AssistantCommandIntentSystem` and `AssistantCommandResultBridgeSystem` run after recommendations and update dispatch/result rows.
6. `AssistantTargetLockReadModelSystem` declares `UpdateAfter` for recommendation, command intent, and command-result bridge systems.
7. `AssistantMessagePrioritySystem` declares `UpdateAfter` for threats, target lock, and command-result bridge systems.
8. `AssistantNarrationRequestSystem` and `AssistantNarrationAudioRequestSystem` retain their ordered run after messages.
9. The gateway and UI consume the completed presentation read models.

### Popup placement and lifecycle contract

- Resolve `HeaderContent/ObjectivesPanel` from the installed match HUD header. The current prefab rect is top-left anchored at `(16, -16)` with a `670 x 520` footprint.
- Create the ARIA button as a sibling using the objective panel's top-left anchor/position. Initial button size is `228 x 78`. Disable the old `ObjectivesPanel` visual root after successful ARIA binding and restore it during `Unbind` if the object still exists.
- If `ObjectivesPanel` cannot be resolved, keep it visible and bind ARIA to the current header fallback. Log one diagnostic in development builds; never hide objectives without a working replacement.
- The popup remains parented to the shell overlay, not the disabled objective panel. Wide layout target is `1040 x 760` reference pixels, clamped to the canvas safe area with at least 24 reference pixels of margin and with the right quick rail excluded.
- At less than 1200 reference pixels of usable width or 820 of usable height, use the compact layout: safe-area width minus 16-pixel margins, safe-area height minus 16-pixel margins, one vertical scroll region, and a stable two-by-two command-button grid. Text does not shrink below 18 reference pixels.
- The popup is non-modal. Its button/panel block world clicks inside their rects; clicking the world outside does not close it.
- Only one large tactical popup may be open: ARIA, resource exchange, full map, or build drawer. `MainMenuPlayUI` closes the currently open sibling before opening another through their existing narrow view methods; do not add a popup manager/controller.
- Escape/back closes ARIA first. `CLOSE` and Escape do not acknowledge messages or stop accepted unit orders. `STOP` follows the command contract above.
- Panel open/closed state is mirrored into `AssistantStateComponent.PanelOpen` through the ECS gateway/request boundary so button unread state and accessibility behavior remain truthful.

## Mockup-To-Data Mapping

| Mockup area | Runtime source | Required implementation | Status |
|---|---|---|---|
| Top-left ARIA access button | `HeaderContent/ObjectivesPanel`, `MatchHudAssistantUiSystemHelper` | Reuse the locked objective-panel anchor/position, hide old visuals only after successful binding, and add unread/top-priority state. | `[ ]` |
| Current goals | Mission-owned `MatchObjectiveRuntimeElement` through `AssistantGoalReadModelElement` | Render structured rows with authored id/state/priority/title/body/primary marker and elapsed whole seconds. | `[~]` |
| Alerts and reports | `AssistantMessageElement` from verified threat/command/feedback facts | Split high-priority alerts from low-priority reports. Hide acknowledged/expired rows; show bounded age state, not a per-second string. | `[ ]` |
| Recommended next action | `AssistantRecommendationElement` | Render title, reason, priority, action label, can-show/can-execute state, target summary. | `[~]` |
| Target-lock telemetry | `AssistantRecommendationElement`, `AssistantPreviewHighlightElement`, dispatch/results, verified threat rows | Show real target kind, display name, faction relation, distance, health, lock/preview state, and readiness label. Never show fake confidence. | `[ ]` |
| ARIA voice panel | `AssistantNarrationStateComponent`, correlated narration/audio request/result | Show `OFF`, `TEXT ONLY`, `QUEUED`, `ACCEPTED`, `PRESENTED`, or `FAILED`. Raw event ids remain diagnostics-only. | `[~]` |
| SHOW ME | `UiAssistantCommandIntentKind.ShowRecommendation` | Preview highlight, camera focus/nudge, target-lock active state. No gameplay order. | `[~]` |
| DO IT | `UiAssistantCommandIntentKind.ExecuteRecommendation` | Execute one revalidated source entity through the locked select/move/attack/focus boundary and correlate the downstream result. | `[ ]` |
| STOP | `UiAssistantCommandIntentKind.StopAssistantControl` | Cancel undispatched assistant work/preview/takeover, clear highlight, and return ownership to player. It does not undo an accepted atomic unit order. | `[~]` |
| CLOSE | Local UI helper | Close panel only. Does not clear messages or cancel commands unless user presses STOP. | `[x]` |

## ECS Data Contract

### Keep gameplay sources authoritative

Do not move mission objective state into ARIA. Objectives are owned by the mission-neutral runtime objective contract defined above. `UiMatchHudStatusSurfacesComponent` is a compatibility UI projection only and must not become the source for ARIA after Phase 3.

Do not move combat ownership into UI. Threat facts come from `CombatDamageObservationElement` and are enriched from gameplay ECS components such as `UnitHealth`, `Faction`, `UnitDisplayInfo`, `UnitAirComponent`, `RuntimeBuildingCombatTag`, and transform data. `RecentAttacker` remains retaliation state only.

### Extend assistant buffers

Add these ECS buffer rows to `Assets/Game/Scripts/Components/AssistantComponents.cs` or a focused assistant component file if the existing file becomes too large.

```csharp
public enum AssistantThreatKind : byte
{
    None = 0,
    FriendlyUnderAttack = 1,
    AirAttack = 2,
    GroundAttack = 3,
    BuildingDefenseAttack = 4,
    MissileAttack = 5
}

public enum AssistantTargetLockState : byte
{
    None = 0,
    Candidate = 1,
    Preview = 2,
    Executable = 3,
    Executing = 4,
    Invalid = 5
}

[InternalBufferCapacity(4)]
public struct AssistantThreatReadModelElement : IBufferElementData
{
    public int ThreatId;
    public int SourceEventId;
    public AssistantThreatKind Kind;
    public AssistantMessagePriority Priority;
    public Entity FriendlyTarget;
    public Entity HostileSource;
    public byte FriendlyFactionId;
    public byte HostileFactionId;
    public float3 FriendlyWorldPosition;
    public float3 HostileWorldPosition;
    public float Distance;
    public int Damage;
    public int FriendlyHealth;
    public int FriendlyMaxHealth;
    public float LastObservedAt;
    public float ExpiresAt;
    public FixedString64Bytes FriendlyName;
    public FixedString64Bytes HostileName;
    public FixedString128Bytes Reason;
}

public struct AssistantThreatReadModelStateComponent : IComponentData
{
    public uint Version;
    public uint LastObservedQueueVersion;
    public int LastConsumedEventId;
    public float NextExpiryAt;
    public int VisibleCount;
}

public enum AssistantFactionRelation : byte
{
    Unknown = 0,
    Friendly = 1,
    Hostile = 2,
    Neutral = 3,
    Protected = 4
}

public struct AssistantMessageReadModelComponent : IComponentData
{
    public uint Version;
    public int VisibleCount;
    public int LastConsumedCommandResultVersion;
    public float NextAgeBoundaryAt;
}

public struct AssistantRecommendationEvaluationStateComponent : IComponentData
{
    public uint LastGoalVersion;
    public uint LastThreatVersion;
    public uint LastFocusedUnitVersion;
    public uint LastFuelVersion;
    public int LastRouteTransitionSequenceId;
    public AssistantControlState LastControlState;
    public byte Initialized;
}

public struct AssistantTargetLockReadModelComponent : IComponentData
{
    public uint Version;
    public int RecommendationId;
    public int ThreatId;
    public AssistantTargetLockState State;
    public AssistantTargetKind TargetKind;
    public AssistantFactionRelation FactionRelation;
    public Entity SourceEntity;
    public Entity TargetEntity;
    public int2 TargetCell;
    public float3 WorldPosition;
    public float Distance;
    public int HealthCurrent;
    public int HealthMax;
    public byte Visible;
    public byte HasTargetCell;
    public byte HasWorldPosition;
    public byte HasDistance;
    public byte HasHealth;
    public FixedString64Bytes SourceName;
    public FixedString64Bytes TargetName;
    public FixedString128Bytes Reason;
}
```

Guidelines:

- Keep threat buffers bounded to 4 visible rows.
- Use entity references and fixed strings only.
- Reuse `AssistantMessagePriority`, `AssistantRecommendationKind`, and existing command intent enums.
- Use the combat event cursor and monotonically increasing read-model versions. Never infer changes only by rebuilding strings.
- Upsert by stable `ThreatId`; remove at `ExpiresAt` only on the locked expiry boundary.
- Use the focused message/evaluation/target-lock state above. Do not add a combined ECS panel-version component.

### Evolve managed UI contracts

Evolve `UiAssistantPanelModel`; do not introduce a parallel `V2` public model unless compatibility requires it.

Use fixed row slots instead of `List<T>` or arrays in the managed contract to avoid allocations and make version equality simple.

```csharp
public readonly struct UiAssistantGoalRowModel
{
    public readonly bool Visible;
    public readonly int GoalId;
    public readonly string Title;
    public readonly string Body;
    public readonly byte State;
    public readonly byte Priority;
    public readonly bool IsPrimary;
}

public readonly struct UiAssistantMessageRowModel
{
    public readonly bool Visible;
    public readonly int MessageId;
    public readonly string Title;
    public readonly string Body;
    public readonly byte Priority;
    public readonly byte RelatedKind;
    public readonly byte AgeState;
    public readonly bool RequiresNarration;
    public readonly bool Acknowledged;
}

public readonly struct UiAssistantTargetLockModel
{
    public readonly bool Visible;
    public readonly byte LockState;
    public readonly byte TargetKind;
    public readonly string TargetName;
    public readonly string SourceName;
    public readonly string DistanceText;
    public readonly string HealthText;
    public readonly string FactionRelationText;
    public readonly string ReadinessText;
    public readonly string ReasonText;
}

public readonly struct UiAssistantNarrationModel
{
    public readonly byte State;
    public readonly byte Priority;
    public readonly string StatusText;
    public readonly string SubtitleText;
    public readonly string FailureReasonText;
    public readonly bool WaveformPulse;
}
```

`UiAssistantPanelModel` should then expose:

- `ElapsedVisible`, `ElapsedWholeSeconds`
- `Goal0`, `Goal1`, `Goal2`
- `Alert0`, `Alert1`, `Alert2`
- `Report0`, `Report1`
- `TargetLock`
- `Narration`
- existing recommendation/control fields
- one `Version` that changes only when any visible field changes

Managed strings are acceptable only in the gateway publishing layer because that layer is cached. ECS systems must continue using fixed strings and numeric values.

Elapsed time stays numeric through `UiAssistantPanelModel`. The UI uses TMP's numeric `SetText` overload for `ELAPSED: {minutes:00}:{seconds:00}` and must not allocate a new managed elapsed-time string each second.

`AgeState` is categorical and allocation-free: `New` for the first 5 seconds, `Active` afterward, and `Expiring` only when less than 1 second remains. The gateway maps it to static labels. Acknowledged and expired messages are hidden in this slice; there is no row acknowledgement button in the V01 popup.

`Title` is a localized static presentation label derived from `RelatedKind` (`THREAT`, `COMMAND`, `LOGISTICS`, or `REPORT`); it is not gameplay telemetry. `Body` comes from the fixed-string message fact. `ExpiresAt <= 0` means the row does not enter `Expiring`; it remains `Active` until acknowledged or replaced.

## Phase Dependencies

| Phase | Hard prerequisites | Execution rule |
|---|---|---|
| 0. Visual/contract | None | The contract and layer-pack decision are locked. Final runtime visual reconciliation remains after functional rows exist. |
| 1. Baseline/source truth | Phase 0 contract | Must complete before any later phase is claimed complete. Placeholder removal and route gating are release blockers. |
| 2. HUD relocation | Phase 1 | May land before structured rows, but must preserve/restore the old objective panel until ARIA bind succeeds. |
| 3. Goals | Phase 1 objective contract | Requires a mission identity and publisher for any non-empty goal row. |
| 4. Alerts/reports | Phase 1 | Must establish explicit message version/age boundaries before gateway row binding. |
| 5. Threats | Phase 1 combat observation contract | Damage producers and observation tests land before ARIA consumes the queue. |
| 6. Target lock | Phases 3 and 5 | May hide optional metrics, but may not use placeholders while waiting for a source. |
| 7. Commands | Phase 6 plus authoritative objective/threat targets | Result correlation must land in the same change as enabling `DO IT` for that command kind. |
| 8. Voice state | Phase 4 plus bounded/versioned audio results | The waveform cannot be enabled before correlated `PRESENTED` state exists. |
| 9. Visual implementation | Phases 2, 3, 4, 6, and 8 | Replace guides only with stable controls that already have real model fields. |
| 10. Gateway/cache | Implemented alongside Phases 3-8 | Each new source must add its explicit cache version in the same change; final closeout follows all data phases. |
| 11. Validation | Phases 1-10 | No phase is done until its focused tests pass; full visual/play/performance acceptance closes the feature. |

Implementation should proceed as small vertical slices. For each slice: update components/systems, update gateway/model only if that source is ready, bind stable UI, add focused tests, run compile/focused validation, then update this tracker. Do not expose a decorative field early and promise to connect it later.

## System Implementation Steps

### 0. Visual target and contract setup

- [x] Save the V01 target-lock reference PNG under `reference/`.
- [x] Save the V01 target-lock generation prompt under `prompts/`.
- [x] Link this implementation tracker from the POP-13 README.
- [x] Add this functional implementation tracker with an overall progress summary.
- [x] Record the layer-pack decision: the functional POP-13 pass uses code-built Unity UI/existing approved sprites; separated production sprite replacement is deferred and is not an acceptance dependency.
- [ ] Reconcile the runtime code-built popup against the target-lock visual after functional model work is complete.
- [ ] Mark Phase 0 complete only after runtime parity/layer-pack decision is documented.

Exit criteria:

- Target art, prompt, tracker, and README links are present.
- Any remaining visual-lock parity work is either completed or explicitly deferred behind the functional ECS-backed implementation.

### 1. Baseline tests before feature work

- [ ] Add or update editor tests that capture the current assistant panel model, command intent behavior, and no-change version caching.
- [ ] Replace production match HUD objective/threat/feedback defaults with the locked empty, non-actionable values; keep synthetic values inside test fixtures only.
- [ ] Add route/match-start gating tests proving main menu, loading, pre-start, route exit/re-entry, and retained old combat observations publish no actionable ARIA row or narration request.
- [ ] Add the mission-neutral objective runtime component/buffer contract and focused contract tests.
- [ ] Add an explicit test showing a mission-published objective becomes an `AssistantGoalReadModelElement` and no mission produces zero goal rows.
- [ ] Add a regression test that `TryReadMatchHudAssistantPanel` returns the same cached model/version when source versions do not change.
- [ ] Add a UI helper test proving `ApplyReadModel` skips unchanged versions and does not rebuild row GameObjects.

Exit criteria:

- Tests pass before structural changes.
- Production defaults cannot emit the historical match-start threat/feedback sounds.
- Failures after this point identify real regressions.

### 2. Top-left HUD relocation

- [ ] Resolve `HeaderContent/ObjectivesPanel` and pass its `RectTransform`/fallback state through the existing match HUD bind path.
- [ ] Change `MatchHudAssistantUiSystemHelper.Bind` to create a `228 x 78` top-left button as a sibling at the objective panel anchor while keeping the popup under the shell overlay.
- [ ] Hide the old objective visual only after successful ARIA button/popup binding and restore it during `Unbind`.
- [ ] Keep the old objective panel visible and use the current header fallback if the target rect cannot be resolved.
- [ ] Ensure the ARIA button blocks world clicks through `ContainsScreenPoint` and the existing gameplay UI click sequence.
- [ ] Mirror panel open/closed state into `AssistantStateComponent.PanelOpen` through the ECS gateway/request boundary.
- [ ] Add tests for button rect position, size, and click blocking.

Exit criteria:

- In match HUD, top-left shows ARIA button instead of the old objective panel.
- Objectives remain available inside ARIA.
- No world selection occurs when pressing ARIA.

### 3. Structured goal rows

- [ ] Extend `UiAssistantPanelModel` with `UiAssistantGoalRowModel` fixed slots.
- [ ] Add the active mission objective publisher; for M01 publish only the authored M01 objective when M01 identity and completion state are available.
- [ ] Update `AssistantGoalReadModelSystem` to consume `MatchObjectiveRuntimeElement`, not the status-surface text slots.
- [ ] Publish numeric elapsed whole seconds from `MatchObjectiveRuntimeStateComponent`, update at most once per second, and bind through TMP numeric `SetText` without a managed timer string.
- [ ] Update `UiShellEcsGateway.TryReadMatchHudAssistantPanel` to copy each goal into a row slot only when objective version/count changes.
- [ ] Keep the current `GoalsText` field only as temporary compatibility if needed by tests.
- [ ] Update `AssistantPanelUiSystemHelper` to bind rows to stable child controls instead of assigning one multiline TMP string.
- [ ] Create row controls once in `MatchHudAssistantUiSystemHelper.CreatePanel`; only update text/color/visibility on model version change.

Exit criteria:

- Goal rows have individual state, priority, primary marker, title, and optional body.
- No runtime parsing of multiline text.
- No row GameObject creation after bind.

### 4. Structured alerts and reports

- [ ] Keep `AssistantMessageElement` as the canonical message buffer.
- [ ] Add `AssistantMessageReadModelComponent.Version` and increment it only on visible insert/update/remove/acknowledgement changes.
- [ ] Add row slots to `UiAssistantPanelModel`: three alert rows and two report rows.
- [ ] Split messages by priority in the gateway: `Critical`/`High` into alerts; `Normal`/`Low` into reports.
- [ ] Preserve `Acknowledged`, `RequiresNarration`, and categorical `AgeState` in row models.
- [ ] Hide acknowledged/expired rows and update panel UI with priority/body/static age-state chips for visible rows.
- [ ] Keep message buffers bounded and coalesced by `SuppressionKey`.

Exit criteria:

- Alerts and reports are independently styled and real.
- Repeated messages update existing rows instead of appending spam.
- Low-priority report rows do not trigger repeated voice unless narration rules allow it.

### 5. Real threat telemetry

- [ ] Add the neutral `CombatDamageObservationQueueComponent` and bounded `CombatDamageObservationElement` ring.
- [ ] Instrument every `UnitHealth` damage path in direct unit fire, building defense, ground missiles, and air missiles with exact applied-damage observations.
- [ ] Add `AssistantThreatReadModelElement` and `AssistantThreatReadModelStateComponent`.
- [ ] Add `AssistantThreatReadModelSystem` in `PresentationSystemGroup` before recommendations/messages and consume only new observation ids or scheduled expiry boundaries.
- [ ] Reject enemy-only/neutral-target observations unless an authoritative objective marks the target as player-protected.
- [ ] Resolve hostile source from the observation entity/position when available.
- [ ] Resolve attacker/target names from ECS `UnitDisplayInfo`, then use the locked fixed generic labels; do not expose prefab keys or call managed selection lookup code from the ECS system.
- [ ] Classify air/ground/building-defense/missile threat from locked gameplay components/source kind.
- [ ] Publish captured applied damage, post-hit health, horizontal distance, and last-observed time without weapon-config inference.
- [ ] Upsert four bounded rows using the locked stable `ThreatId` hash and priority thresholds.
- [ ] Expire rows at 6 seconds using `NextExpiryAt`, not a broad per-frame query.
- [ ] Feed top threat rows into `AssistantMessagePrioritySystem` so voice and alerts share the same fact source.
- [ ] Apply the 8-second per-threat narration suppression rule and text-only fallback when ARIA voice is unavailable.

Exit criteria:

- If ARIA says a unit is under attack, the popup shows which friendly unit and attacker/source when known.
- If the source is unknown, the popup says `source unknown` and still shows the damaged friendly target.
- No enemy-only warnings play or display unless they affect the player or a player-visible objective.

### 6. Recommendation scoring and target lock

- [ ] Extend recommendation target metadata, add the locked input-version evaluation state, and remove the per-frame selected-tag count query.
- [ ] Add `AssistantTargetLockReadModelSystem` after recommendations and threats.
- [ ] Target lock uses the locked recommendation/preview/dispatch/top-threat priority and never falls back to camera position or an arbitrary focused unit.
- [ ] Compute lock state from command capability: candidate, preview, executable, executing, invalid.
- [ ] Publish the truthful readiness label (`PREVIEW`, `READY`, `ACTIVE`, `BLOCKED`) instead of numeric confidence.
- [ ] Publish target display name, source name, faction relation, distance text, health text, readiness text, and reason text through `UiAssistantTargetLockModel`.
- [ ] Update the target-lock graphic so its markers and text are model-driven.

Exit criteria:

- The target-lock block never shows fake radar data.
- Every target name and metric has a gameplay source or is hidden.
- The lock visual changes when preview starts/stops.

### 7. Complete command mechanics

- [ ] Preserve `ShowRecommendation` as preview-only.
- [ ] Add concrete move recommendation targeting from an authoritative objective/tactical target and disable execution when none exists.
- [ ] Add concrete attack recommendation targeting from an authoritative objective or verified threat and disable execution when none exists.
- [ ] Implement `MoveToWorldPosition` through `UnitMoveOrderRequestSystem.EnqueueImmediateMoveOrder` for the recommendation's single revalidated source entity.
- [ ] Implement `AttackEntity` through `UnitAttackOrderRequestSystem.EnqueueDirectAttackTarget` for the recommendation's single revalidated source entity.
- [ ] Keep `SelectEntity` execution through existing selection command intent request flow.
- [ ] Keep `FocusCamera` execution through the existing `RtsCameraRequestQueueComponent`/`RtsCameraRequestElement` preview path.
- [ ] Add bounded assistant-to-downstream dispatch mapping and a result bridge for selection/move/attack request ids.
- [ ] Revalidate recommendation id/source version, entity existence, ownership, health, target relation, and cell bounds immediately before dispatch.
- [ ] Reuse canonical `TacticalCommandReasonCode` values for stale entity, missing source, invalid cell, non-hostile target, and unavailable command path.
- [ ] Mark assistant intents `Accepted` on downstream enqueue and `Completed`/`Rejected` only from the correlated result; time out after 5 seconds.
- [ ] `DO IT` must only be interactable when the recommendation has an executable typed path.
- [ ] `STOP` clears undispatched work, preview highlight, target-lock preview state, and assistant control owner state without undoing an accepted atomic unit order.

Exit criteria:

- `DO IT` for select/move/attack/focus either executes a real command or returns a visible ARIA rejection reason.
- No command helper bypasses ECS command systems.
- Player input still overrides assistant takeover safely.

### 8. ARIA voice state panel

- [ ] Extend narration requests with `AudioPlaybackRequestId` and the managed panel model with correlated narration state, priority, subtitle, and safe failure reason.
- [ ] Keep playback in existing audio infrastructure; add bounded/versioned 256-row terminal request/result retention without introducing an assistant-owned audio player.
- [ ] Add `AssistantNarrationAudioResultProjectionSystem`, correlate audio results, and publish only `OFF`, `TEXT ONLY`, `QUEUED`, `ACCEPTED`, `PRESENTED`, or `FAILED`.
- [ ] Pulse the waveform for at most 0.8 seconds on transition to `PRESENTED`; keep it still otherwise.
- [ ] Failed narration shows text fallback and a user-safe reason with no fake speaking/completed state.
- [ ] Do not expose debug audio event ids in retail UI. Keep them in tests/logs only.

Exit criteria:

- If ARIA voice is silent, the panel distinguishes disabled, text-only, queued, accepted-not-presented, and exact presentation failure states.
- Voice visual state is true, not decorative.

### 9. Popup visual implementation

- [ ] Replace decorative row guides with stable row views bound to goal/alert/report row models.
- [ ] Keep target-lock chrome static, but bind all text/active markers to `UiAssistantTargetLockModel`.
- [ ] Use pooled/stable child objects created once during bind.
- [ ] Use TMP autosizing only where necessary and with bounded min/max sizes.
- [ ] Preserve large readable controls matching build/resource popup scale.
- [ ] Implement the locked wide `1040 x 760` safe-area layout and compact one-column scroll layout with minimum 18-pixel text.
- [ ] Exclude the right quick rail and keep at least the locked safe-area margins.
- [ ] Enforce one-large-tactical-popup exclusivity through `MainMenuPlayUI` and existing view close methods.
- [ ] Bind Escape/back/CLOSE/STOP behavior exactly as specified in the popup lifecycle contract.
- [ ] Add high-contrast/large-text hooks from `AssistantSettingsComponent` if those settings are active.

Exit criteria:

- The panel visually resembles the mockup but has no fake data-only decorations.
- It remains readable at desktop and mobile match HUD scales.
- It remains non-modal and does not overlap the right rail controls.

### 10. Gateway publishing and caching

- [ ] Add explicit source-version state for messages, threats, and target lock plus cached tuple fields in `UiShellEcsGateway`.
- [ ] Replace per-poll message/narration content hashing with explicit bounded source versions.
- [ ] Increment one monotonic managed `UiAssistantPanelModel.Version` only when the cached source tuple changes.
- [ ] Convert fixed strings to managed strings only when the combined version changes.
- [ ] Do not allocate lists/arrays during `TryReadMatchHudAssistantPanel`.
- [ ] Avoid repeated `.ToString()` calls for unchanged rows.
- [ ] Keep fallback `UiAssistantPanelModel.Empty` cheap and static-like.

Exit criteria:

- Per-frame polling from `MainMenuPlayUI` is safe because unchanged gateway reads return cached models.
- UI updates happen only when model version changes.

### 11. Validation and acceptance

- [ ] Editor test: production defaults and non-match/pre-start routes publish no objectives, threats, feedback, or narration.
- [ ] Editor test: mission-owned structured goal rows publish objective ids, states, bodies, primary marker, and elapsed whole seconds correctly.
- [ ] Editor test: direct fire, building defense, ground missile, and air missile damage each append exact applied-damage observations.
- [ ] Editor test: threat rows identify the player-owned target and attacker from combat observations while enemy-only damage is ignored.
- [ ] Editor test: threat rows expire/coalesce without unbounded growth.
- [ ] Editor test: recommendation target-lock model uses top recommendation and changes state on preview.
- [ ] Editor test: command intent dispatch/result bridge accepts or rejects select, move, attack, and focus with correlated downstream request ids and canonical reasons.
- [ ] Editor test: narration state maps correlated audio pending/accepted/presented/failure/text-only/off states without a false completed/speaking state.
- [ ] Editor test: no-change gateway call returns same version and does not rebuild strings/rows.
- [ ] UI helper test: row GameObjects are created once and reused.
- [ ] UI helper test: top-left objective slot binding, missing-slot fallback, old-panel restore, popup exclusivity, and Escape/CLOSE/STOP semantics.
- [ ] Visual validation: 16:9 desktop, 16:10, ultrawide, and mobile-ish aspect screenshots.
- [ ] Performance validation: locked timing/allocation budgets pass for idle polling and a saturated bounded threat model.
- [ ] Play validation: start match, open popup, see objectives inside ARIA, issue move, issue attack, observe accurate threat/voice/target-lock rows.

Exit criteria:

- `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passes.
- Focused Unity validation passes or logs a clear pass before any unrelated Unity shutdown issue.
- Screenshots show the ARIA button in the top-left objective slot and a readable functional popup.
- Main menu, loading, and pre-start validation produces no ARIA warning/audio request.
- A changed model may allocate bounded managed strings once; the following unchanged polls and UI applies allocate zero bytes.

## Button Relocation Detail

Target placement:

- Resolve `HeaderContent/ObjectivesPanel`, copy its top-left anchor/position `(16, -16)`, and create the ARIA button as a `228 x 78` sibling.
- Keep `228 x 78` as the minimum desktop touch target; compact layout may grow it but must not shrink it.
- Show a compact status strip on the button:
  - idle: `ARIA`
  - recommendation: `ARIA READY`
  - critical alert: `ARIA ALERT`
  - preview/control: `ARIA ACTIVE`
- Do not duplicate objective text on the button. Objectives live in the popup.

Objective panel migration:

- Keep `UiMatchHudStatusSurfacesComponent.Objective0Text/1/2` and icon fields only as compatibility output during migration.
- Stop presenting the old objective panel only after ARIA has successfully bound to the same slot; restore it on bind failure/unbind.
- Present mission-owned `MatchObjectiveRuntimeElement` rows and elapsed whole seconds in ARIA `CURRENT GOALS`.
- If an objective changes while the popup is closed, show one state indicator from the highest-priority changed objective. Do not duplicate objective text on the button.

## Performance Rules

- No broad entity scans from managed UI helpers.
- Threat ingestion reads only the bounded 64-row combat observation ring and publishes at most four rows.
- No per-frame `new GameObject` or row reconstruction after bind.
- No LINQ in ECS or UI update paths.
- No `string.Format`, interpolation, concatenation loops, or repeated `.ToString()` in steady-state frame polling.
- Use `FixedString*Bytes` in ECS rows.
- Use `InternalBufferCapacity` for small bounded buffers.
- Keep assistant buffers bounded:
  - goals: 3
  - alerts: 3
  - reports: 2
  - threats: 4
  - recommendations: 1 visible/top recommendation in this scope
  - narration requests: existing 8 max is acceptable
- Update ECS rows only when source data changes or a cooldown/expiry boundary is crossed.
- Use source versions, request ids, frame ids, or coarse timestamps for dirty checks.
- Keep managed string publishing inside the cached gateway only.
- UI helpers must compare `model.Version` before touching TMP/Image/Button state.
- Preserve the existing assistant aggregate timing gates: average at or below `0.25 ms` and p95 at or below `0.75 ms` over 240 measured frames after warmup.
- Tighten steady-state managed allocation to `0` bytes for unchanged ECS source versions, 1,000 gateway polls, and 1,000 repeated UI applications of the same model version.
- Run a second saturated fixture with 64 queued damage observations, four visible threats, three goals, five visible messages, one recommendation, and one narration row. It must stay within the same timing gates after its one changed-model publication.
- One changed-model publication may allocate managed strings in the gateway. Allocation must stop on the immediately following unchanged poll; no list, array, or GameObject allocation is permitted.

## SOLID/ECS Guardrails

- ECS systems own policy and state transitions.
- UI helpers own layout and presentation only.
- Audio request systems own audio events only.
- Command intent systems route to existing command systems only.
- Do not introduce broad services, managers, providers, controllers, or facades.
- New names should follow existing patterns:
  - `AssistantThreatReadModelSystem`
  - `AssistantTargetLockReadModelSystem`
  - `AssistantPanelUiSystemHelper`
  - `AssistantHighlightPresentationSystemHelper`
  - `AssistantNarrationPresentationSystemHelper`
- Avoid names like:
  - `AriaManager`
  - `AssistantRuntimeController`
  - `CommandAssistantService`
  - `ThreatProvider`
  - `PopupFacade`

## Implementation Order

1. Replace sample production defaults and add route/match-start empty-state tests.
2. Add the mission-owned objective and neutral combat-observation contracts with producer tests.
3. Lock baseline assistant caching/UI tests before changing managed contracts.
4. Move ARIA to `HeaderContent/ObjectivesPanel` with fallback/restore behavior.
5. Replace string-only goals with mission-owned structured goal rows and elapsed whole seconds.
6. Replace string-only alerts with versioned structured alert/report rows.
7. Add event-driven real threat rows and narration suppression from combat observations.
8. Add target-lock/readiness state and bind it to the popup.
9. Add authoritative move/attack targets, downstream dispatch mapping, and result correlation.
10. Bind correlated narration/audio result state to the voice panel.
11. Implement wide/compact popup layout, sibling-popup exclusivity, and accessibility hooks.
12. Run compile, focused Unity, play, visual, and steady-state/saturated performance validation.

## Definition Of Done

- The popup visually matches the target-lock mockup closely enough to be recognized as the same UI direction.
- Every non-decorative text, chip, row, metric, button state, and voice state comes from ECS or an explicit settings/runtime state source.
- Production shell defaults are empty/non-actionable and cannot trigger main-menu/loading/match-start ARIA warnings.
- Objectives are no longer duplicated outside the popup.
- Threat alerts identify exact player-owned units from combat observations and never infer damage from weapon configuration.
- ARIA never plays or displays enemy-only warnings that do not affect the player.
- `DO IT` executes only a concrete revalidated typed target and reports the correlated downstream result.
- Voice state never equates enqueue with playback completion.
- Steady-state match HUD polling produces no avoidable managed allocations.
- All focused editor and Unity validation gates pass.
