# FirstLaunch Architecture Alignment Refactor Tracker

Date: 2026-07-12

Status: Complete - Phases 0-9 validated; unrelated global audio source-size debt remains tracked outside FirstLaunch

Scope: Refactor the existing FirstLaunch narrative runtime so it follows the project's SOLID, ECS-edge, assembly, and file/type naming contracts without changing approved art, authored sequence content, player-visible behavior, save compatibility, or the Menu-to-Match handoff.

## Goal

Bring FirstLaunch into the same architecture used by the rest of WarlineCapture:

- Keep gameplay and shell routing state in ECS boundaries.
- Keep Canvas, AudioSource, Addressables, and comic-panel motion in managed presentation edges.
- Keep `*View` MonoBehaviours passive: serialized references, visual projection, and UI-intent emission only.
- Keep `Game.Composition` limited to dependency wiring and edge adaptation.
- Move reusable narrative state and contracts out of concrete UI ownership.
- Use approved runtime helper suffixes from `file_naming_architecture_contract.md`.
- Preserve every Unity `.meta` GUID during file renames.
- Preserve all approved FirstLaunch behavior and evidence unless a changed contract is explicitly recorded here.

## Authority

Read these documents before changing this tracker or the implementation:

1. `Design/Architecture/gameplay_solid_ecs_contract.md`
2. `Design/Architecture/file_naming_architecture_contract.md`
3. `Design/Architecture/non_ecs_system_helper_naming_refactor_tracker.md`
4. `Design/Architecture/architecture_performance_hardening_implementation_tracker.md`
5. `Design/NarrativeVision/FirstLaunch/IMPLEMENTATION_TRACKER.md`
6. This tracker

If the narrative tracker conflicts with an architecture contract, preserve product behavior and repair the implementation boundary. Do not weaken the architecture contract to make FirstLaunch pass.

## Progress Legend

| Mark | Meaning |
|---|---|
| `[x]` | Complete and validated. |
| `[~]` | In progress or implemented without complete validation. |
| `[ ]` | Not started. |
| `[!]` | Blocked by a named external decision or prerequisite. |

## Progress Summary

| Phase | Status | Evidence |
|---|---|---|
| 0. Tracker and baseline | Complete | Baseline failures reproduced and target locked. |
| 1. Runtime naming alignment | Complete | GUID-preserving rename batch; FirstLaunch gate `34/34`. |
| 2. Enforcement repair | Complete | Focused architecture `3/3`, broad-shell `1/1`, non-ECS naming `9/9`. |
| 3. Composition responsibility split | Complete | Profile, shell, reviewer, and pure route-policy boundaries validated; integrated gate `43/43`. |
| 4. Sequence runtime isolation | Complete | Pure runtime `4/4`, managed compatibility `5/5`, integrated gate `48/48`. |
| 5. Contract boundary | Complete | Dependency-free narrative contracts assembly; integrated gate `49/49`. |
| 6. Data-driven content policy | Complete | Authored cues, route roles, completion/evidence metadata; integrated gate `51/51`. |
| 7. Async panel residency | Complete | Non-blocking current/next residency with stale-transition rejection; integrated gate `55/55`, async suite `7/7`. |
| 8. View passivity and size review | Complete | Context/default/commit policy extracted; passive-view guard `10/10`, integrated gate `56/56`. |
| 9. Validation and closeout | Complete | Final gate `56/56`, PlayMode `1/1`, naming `1/1` and `9/9`, assembly `31/31`; unrelated global source-size debt recorded. |

## Baseline Audit

Baseline commit: `5bd79e034`

### Confirmed Violations

| ID | Severity | Finding | Evidence | Required end state |
|---|---|---|---|---|
| `FLA-001` | High | `FirstLaunchNarrativeCoordinator` owns startup, profile persistence, skip policy, reviewer flow, concrete views, and shell ECS mutation. | `Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeCoordinator.cs` | Composition wires narrow owners and contains no sequence or mission policy. |
| `FLA-002` | High | `FirstLaunchNarrativePlayer` owns state indexing, timeline transitions, presentation, audio, asset residency, UI binding, and route completion. | `Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativePlayer.cs` | Sequence progression, presentation, asset lifetime, and route completion have explicit owners. |
| `FLA-003` | High | Runtime type `FirstLaunchNarrativeAudioController` violates the broad-shell naming guard. | `ScriptArchitectureAlignmentContractTests.RunBroadShellValidation` fails at baseline. | No FirstLaunch runtime class uses `Manager`, `Controller`, `Presenter`, `Facade`, `Installer`, or `Orchestrator`. |
| `FLA-004` | High | Recorded `31/31` assembly validation does not run broad-shell or non-ECS naming checks. | `RunAssemblyBoundaryValidation` and `RunBroadShellValidation` are separate runners. | FirstLaunch closeout runs assembly, broad-shell, non-ECS naming, source-growth, and focused narrative gates. |
| `FLA-005` | High | The non-ECS naming escape regex does not recognize normally indented namespace members and currently reports the full allowlist stale. | `NonEcsSystemConversionArchitectureTests.TopLevelTypeDeclarationRegex` and baseline focused failure. | Naming escape discovery is correct, its allowlist is current, and the focused runner passes. |
| `FLA-006` | Medium | Narrative completion, commander identity, guidance, and handoff data live in `Game.UI.Contracts`. | `Assets/Game/Scripts/UI/Contracts/Narrative/NarrativeUiContracts.cs` | UI intents remain UI contracts; narrative/application contracts live in a focused non-UI contract boundary. |
| `FLA-007` | Medium | Audio selection, route destinations, evidence IDs, and mission flags are hard-coded against state ID strings. | `FirstLaunchNarrativeAudioController`, `FirstLaunchNarrativeCoordinator`, and `FirstLaunchNarrativeModelFactory` | Authored config owns content policy; runtime consumes typed records. |
| `FLA-008` | Medium | Panel acquisition uses `Addressables.LoadAssetAsync(...).WaitForCompletion()` during state entry. | `NarrativePanelAssetResidency.cs` | State entry never synchronously blocks on Addressables; preload/cancellation ownership is explicit. |
| `FLA-009` | Medium | Reviewer request state is a public runtime static type backed by editor-only `PlayerPrefs`. | `FirstLaunchNarrativeReviewSession.cs` | Editor request production stays in Editor; runtime receives an injected review-start flag. |
| `FLA-010` | Medium | A line-budget repair split a 595-line class into generic `AudioController` and `ModelFactory` types without repairing ownership. | Commit `597f02f76`; APH tracker line 1568. | Source-size compliance follows cohesive responsibility extraction, not generic helper creation. |

### Current Runtime Inventory

| Layer | Current owners | Baseline concern |
|---|---|---|
| Config | `NarrativeSequenceConfig`, `NarrativeSpeakerCatalog`, baseline `NarrativePunctuationProfile` | Correct assembly, but punctuation used the wrong ScriptableObject suffix and route/audio/completion policy is incomplete. |
| Composition | `FirstLaunchNarrativeCoordinator`, `FirstLaunchNarrativePlayer`, `FirstLaunchNarrativeAudioController`, `FirstLaunchNarrativeModelFactory`, `FirstLaunchNarrativeReviewSession`, `NarrativePunctuationAdapter` | Broad names and mixed policy/presentation responsibilities. |
| UI Runtime | `NarrativeSequenceView` and child `*View` types | Mostly correct passive edge; large views require focused review. |
| UI Runtime behavior | dialogue reveal, voice playback, panel motion, panel residency, sequence presentation, subtitle style resolution | Managed presentation is valid, but naming and ownership must be explicit. |
| UI Contracts | UI actions plus identity, guidance, completion, and handoff | Narrative domain data is coupled to UI ownership. |
| Shell ECS | startup disposition and route request components | Correct integration direction; composition should only publish/read this boundary. |

### Assembly Baseline

- `Game.UI.Runtime` references contracts and presentation packages only. It does not reference `Game.Configs`, `Game.Runtime`, or Unity Entities. Preserve this repaired boundary.
- `Game.Composition` references concrete UI, configs, runtime persistence, and shell ECS. FirstLaunch must use this only as the outer integration edge.
- No FirstLaunch-specific runtime or contracts assembly exists.
- FirstLaunch behavior tests directly instantiate concrete composition and UI implementation types, so they do not currently prove dependency inversion.

## Fixed Architecture Decisions

| ID | Decision |
|---|---|
| `D-001` | Do not convert Canvas, AudioSource, Addressables, localization projection, or comic-panel motion into ECS. They are intentional managed presentation edges. |
| `D-002` | Do not place narrative or mission policy in `*View` MonoBehaviours. Views expose serialized references, apply presentation models, and emit typed UI intents. |
| `D-003` | High-level menu/match route publication continues through existing shell ECS request components. FirstLaunch must not bypass that boundary. |
| `D-004` | No new broad `Manager`, `Controller`, `Player`, `Coordinator`, `Presenter`, `Facade`, `Installer`, `Orchestrator`, generic `Factory`, or generic `Cache` owner may be introduced. A narrow literal cache is allowed only when it stores and validates cached data and owns no policy. |
| `D-005` | Plain managed helpers use the approved reason suffix: `PresentationSystemHelper`, `CompositionSystemHelper`, or `UtilitySystemHelper` for this feature. |
| `D-006` | Behavior-preserving renames keep both `.cs` and `.cs.meta` together. Serialized MonoBehaviour script GUIDs must not change. |
| `D-007` | State IDs remain serialized stable identities, but code may not use scattered string comparisons to select audio, completion, or mission behavior. |
| `D-008` | The existing save fields and serialized sequence assets remain backward compatible throughout this refactor. |
| `D-009` | A dedicated `Game.Narrative.Contracts`/`Game.Narrative.Runtime` split is introduced only after the current responsibilities are isolated and tests prove the dependency direction. |
| `D-010` | Every batch is independently compilable and behavior-preserving; no all-at-once rewrite is permitted. |

## Target Ownership

### Managed Presentation Edge

Allowed responsibilities:

- Resolve a presentation model supplied by narrative state/config.
- Apply sprites, text, accessibility state, motion, and audio to passive views.
- Emit typed user intents.
- Preload/release Addressables with explicit cancellation and bounded residency.
- Pause/resume/stop voice and ambience without deciding mission routes.

### Narrative Runtime Boundary

Target responsibilities:

- Validate and index authored sequence state.
- Advance deterministic sequence/line state from elapsed time and typed intents.
- Produce typed presentation and completion outputs.
- Contain no concrete `NarrativeSequenceView`, `AudioSource`, Addressables handle, `SaveService`, `PlayerPrefs`, or `EntityManager` dependency.

### Composition Edge

Allowed responsibilities:

- Read serialized references from `MenuBootstrapView`.
- Construct and connect narrative runtime, presentation, persistence, and shell adapters.
- Forward lifecycle ticks and dispose owners.
- Publish typed completion into existing shell ECS requests.

Not allowed:

- State-specific audio decisions.
- Mission evidence construction.
- Skip destination policy.
- Reviewer navigation policy.
- Concrete panel selection or Addressables loading.

## Naming Migration Map

Batch 1 applies behavior-preserving names. Later batches may delete or further split these transitional owners.

| Status | Current type/file | Batch 1 type/file | Final responsibility |
|---|---|---|---|
| `[x]` | `FirstLaunchNarrativeCoordinator` | `FirstLaunchNarrativeCompositionSystemHelper` | Transitional outer composition; split persistence, review, and shell publication. |
| `[x]` | `FirstLaunchNarrativePlayer` | `FirstLaunchNarrativeSequencePresentationSystemHelper` | Transitional sequence/presentation owner; split pure progression from concrete presentation. |
| `[x]` | `FirstLaunchNarrativeAudioController` | `FirstLaunchNarrativeAudioPresentationSystemHelper` | Audio presentation only. |
| `[x]` | `FirstLaunchNarrativeModelFactory` | `FirstLaunchNarrativeModelUtilitySystemHelper` | Transitional pure mapping; split location and completion mapping. |
| `[x]` | `FirstLaunchNarrativeReviewSession` | `FirstLaunchNarrativeReviewUtilitySystemHelper` | Transitional review request; move request production to Editor. |
| `[x]` | `NarrativePunctuationAdapter` | `NarrativePunctuationUtilitySystemHelper` | Pure config-to-presentation conversion. |
| `[x]` | `NarrativeSequencePresentation` | `NarrativeDialoguePresentationSystemHelper` | Dialogue reveal, voice, and auto-advance presentation. |
| `[x]` | `NarrativeDialogueReveal` | `NarrativeDialogueRevealPresentationSystemHelper` | Deterministic character reveal calculation. |
| `[x]` | `NarrativeVoicePlayback` | `NarrativeVoicePlaybackPresentationSystemHelper` | One AudioSource voice lifecycle. |
| `[x]` | `NarrativePanelMotion` | `NarrativePanelMotionPresentationSystemHelper` | Comic panel transform motion. |
| `[x]` | `NarrativePanelAssetResidency` | `NarrativePanelAssetResidencyPresentationSystemHelper` | Bounded current/next panel residency. |
| `[x]` | `NarrativeSubtitleStyleResolver` | `NarrativeSubtitleStyleUtilitySystemHelper` | Pure settings-to-style projection. |
| `[x]` | `NarrativePunctuationProfile` | `NarrativePunctuationConfig` | ScriptableObject configuration. |

## Implementation Phases

### Phase 0 - Tracker And Baseline

- [x] Record current assemblies, owners, responsibilities, and architecture violations.
- [x] Reproduce `RunBroadShellValidation` failure for `FirstLaunchNarrativeAudioController`.
- [x] Reproduce the non-ECS naming runner failure and identify the declaration-regex defect.
- [x] Lock behavior-preserving, GUID-preserving migration rules.
- [~] Tracker and Batch 1 changes are ready in the working tree; commit/push remains pending.

Exit gate: another agent can continue from this document without repeating the audit.

### Phase 1 - Runtime Naming Alignment

- [x] Rename transitional composition owners to approved reason suffixes.
- [x] Rename managed UI behavior owners to `PresentationSystemHelper` or `UtilitySystemHelper`.
- [x] Update all runtime, editor tooling, and test references.
- [x] Preserve every moved `.meta` GUID.
- [x] Remove stale filenames whose top-level type no longer matches the file.
- [x] Pass compile and focused FirstLaunch behavior tests with no behavior change.

Exit gate: no FirstLaunch runtime owner uses broad or unapproved helper naming.

### Phase 2 - Enforcement Repair

- [x] Correct top-level declaration discovery in `NonEcsSystemConversionArchitectureTests` without weakening its intended guard.
- [x] Reconcile stale naming escape allowlist entries based on actual current types.
- [x] Add a FirstLaunch-specific runtime naming and layer test.
- [x] Add broad-shell and non-ECS naming validation to the FirstLaunch closeout runner.
- [x] Ensure the documented validation matrix distinguishes assembly checks from naming checks.

Exit gate: an intentionally reintroduced FirstLaunch `Controller`, broad `Player`, or unapproved top-level helper causes CI failure.

### Phase 3 - Composition Responsibility Split

- [x] Extract profile load/save/default normalization from the transitional composition owner.
- [x] Extract shell ECS startup disposition, handoff lifecycle, and route publication into a narrow composition adapter.
- [x] Extract reviewer navigation and evidence refresh from production sequence flow.
- [x] Keep Menu bootstrap integration limited to construct, tick, shell projection, route acceptance, and dispose calls through one FirstLaunch boundary.
- [x] Remove hard-coded mission/state and Skip policy from the composition layer.

Exit gate: composition contains wiring and edge adaptation only.

### Phase 4 - Sequence Runtime Isolation

- [x] Introduce a pure sequence state/progression owner with no concrete UI dependency.
- [x] Represent input as typed intents and output as typed transition/presentation events.
- [x] Move line timing, transition tokens, continue/skip validation, and route-completion state into that owner.
- [x] Keep config indexing deterministic and reject duplicate/disconnected state IDs.
- [x] Add pure unit tests that require no prefab, scene, AudioSource, or AssetDatabase.

Exit gate: sequence progression tests run without constructing `NarrativeSequenceView`.

### Phase 5 - Contract Boundary

- [x] Separate UI-only intent contracts from narrative identity, guidance, completion, and handoff contracts.
- [x] Add a focused narrative contracts assembly.
- [x] Keep contracts free of concrete UI, configs, persistence, Addressables, and ECS implementation types.
- [x] Update asmdefs and namespace guardrails.
- [x] Add assembly tests preventing narrative runtime and contracts from referencing concrete UI.

Exit gate: narrative state/completion contracts are not owned by `Game.UI.Contracts`, and runtime does not depend on concrete UI.

### Phase 6 - Data-Driven Content Policy

- [x] Add typed authored audio profile/cue data to sequence state config.
- [x] Add typed authored route/completion metadata for handoff and debrief states.
- [x] Remove state-ID switches from audio presentation.
- [x] Remove scattered literal comparisons for skip and completion behavior.
- [x] Preserve existing IDs, evidence IDs, flags, and save compatibility through generated/config migration validation.

Exit gate: adding a new sequence does not require editing FirstLaunch audio or completion switch statements.

### Phase 7 - Async Panel Residency

- [x] Replace `WaitForCompletion()` with asynchronous preload/state-entry behavior.
- [x] Keep only current and next panel resident.
- [x] Cancel stale loads by transition token.
- [x] Preserve direct-sprite fallback for editor/development diagnostics.
- [x] Add missing, failed, cancelled, rapid-seek, and shutdown tests.

Exit gate: no FirstLaunch runtime state transition synchronously blocks on Addressables.

### Phase 8 - View Passivity And Size Review

- [x] Verify every narrative `*View` contains only serialized binding, visual projection, and intent emission.
- [x] Move validation, defaults, sequence context, or profile decisions out of views where found.
- [x] Review `NarrativeCommanderIdentityView` and `NarrativeGuidanceChoiceView` for cohesive child-view extraction.
- [x] Preserve accessibility, dynamic dialogue sizing, interaction lockout, and no-double-submit behavior.

Exit gate: UI architecture tests identify any policy introduced into narrative views.

### Phase 9 - Validation And Closeout

- [x] `git diff --check` passes.
- [x] Changed runtime/editor/test assemblies build with zero errors.
- [x] `FirstLaunchGate89Validation.RunFocusedValidation` passes.
- [x] `FirstLaunchNarrativePlayerTests` equivalent sequence validation passes after renaming/extraction.
- [x] `FirstLaunchNarrativeMenuIntegrationTests.RunFocusedValidation` passes.
- [x] `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation` passes.
- [x] `ScriptArchitectureAlignmentContractTests.RunBroadShellValidation` passes.
- [x] `NonEcsSystemConversionArchitectureTests.RunFocusedValidation` passes.
- [!] Global `ProductionSourceGrowthArchitectureTests.RunFocusedValidation` remains red only for the pre-existing unrelated `AudioPlaybackPresentationSystemHelper.cs` 548-line review debt; exact FirstLaunch helper and authorization checks pass.
- [x] FirstLaunch PlayMode Addressables, Skip/debrief, commander/guidance, narration, and handoff flow passes; fresh-profile production Skip/profile branches pass in the integrated Menu tests.
- [x] Existing approved visual/audio behavior is confirmed unchanged.
- [x] Update `Design/NarrativeVision/FirstLaunch/IMPLEMENTATION_TRACKER.md` with the final architecture evidence.

Exit gate: FirstLaunch is behaviorally stable, architecture-green, and no longer depends on transitional broad owners.

## Validation Commands

```bash
Tools/CI/invoke_unity_macos.sh --timeout 300 --log /private/tmp/warline-firstlaunch-architecture-boundary.log -- -quit -executeMethod ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation
Tools/CI/invoke_unity_macos.sh --timeout 300 --log /private/tmp/warline-firstlaunch-broad-shell.log -- -quit -executeMethod ScriptArchitectureAlignmentContractTests.RunBroadShellValidation
Tools/CI/invoke_unity_macos.sh --timeout 300 --log /private/tmp/warline-firstlaunch-non-ecs-naming.log -- -quit -executeMethod NonEcsSystemConversionArchitectureTests.RunFocusedValidation
Tools/CI/invoke_unity_macos.sh --timeout 300 --log /private/tmp/warline-firstlaunch-gate89.log -- -quit -executeMethod FirstLaunchGate89Validation.RunFocusedValidation
Tools/CI/invoke_unity_macos.sh --timeout 300 --log /private/tmp/warline-firstlaunch-menu-integration.log -- -quit -executeMethod FirstLaunchNarrativeMenuIntegrationTests.RunFocusedValidation
Tools/CI/invoke_unity_macos.sh --timeout 300 --log /private/tmp/warline-firstlaunch-source-growth.log -- -quit -executeMethod ProductionSourceGrowthArchitectureTests.RunFocusedValidation
```

## Batch Log

### 2026-07-12 - Baseline Audit

- Baseline `RunBroadShellValidation`: failed because `FirstLaunchNarrativeAudioController` uses forbidden broad token `Controller`.
- Baseline `NonEcsSystemConversionArchitectureTests.RunFocusedValidation`: failed because top-level naming discovery returns no current escape types and reports all approved entries stale.
- Recorded `31/31` assembly-boundary evidence remains useful but is not evidence of naming or SOLID alignment.
- No production files were changed during the audit.

### 2026-07-12 - Batch 1

- Status: Complete.
- Intended behavior change: none.
- Scope: approved helper/config naming, references, test names, source-growth governance, and architecture enforcement only.
- GUID result: all renamed `.cs.meta` files moved with their source and retained their original GUIDs.
- Focused FirstLaunch architecture: passed `3/3` in `/private/tmp/warline-firstlaunch-architecture-batch1-r2.log`.
- Project broad-shell naming: passed `1/1` in `/private/tmp/warline-firstlaunch-broad-shell-batch1.log`.
- Repaired non-ECS naming: passed `9/9` in `/private/tmp/warline-firstlaunch-non-ecs-naming-batch1-r2.log`.
- FirstLaunch behavior/integration gate: passed `34/34` in `/private/tmp/warline-firstlaunch-gate89-batch1-r2.log`; generated Unity 6.5 serialization-only scene/prefab/import diffs were removed afterward.
- Assembly/namespace boundary: passed `31/31` in `/private/tmp/warline-firstlaunch-boundary-batch1.log`.
- Exact source helper path and authorization tests: both passed through filtered EditMode runs in `/private/tmp/warline-firstlaunch-source-helper-exceptions.xml` and `/private/tmp/warline-firstlaunch-source-authorizations.xml`.
- Full source-growth runner: FirstLaunch and Menu-bootstrap violations are cleared; the runner remains red only for the unrelated pre-existing `Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationSystemHelper.cs` 548-line review violation. Evidence: `/private/tmp/warline-firstlaunch-source-growth-batch1-r3.log`.
- Builds: `Game.Configs`, `Game.UI.Runtime`, `Game.Composition`, and `Game.Tests.Editor` passed with zero errors; existing package/reference warnings remain.
- `git diff --check`: passed.
- Next batch: split profile persistence, shell ECS publication, and reviewer behavior out of `FirstLaunchNarrativeCompositionSystemHelper` without changing route or save semantics.

### 2026-07-12 - Phase 3 Ownership Batch

- Status: Stable partial Phase 3 batch; Phase 3 remains active for route/Skip policy.
- Intended behavior change: none.
- `FirstLaunchNarrativeProfileCompositionSystemHelper` now owns profile loading, startup disposition reads, production/reviewer persistence rules, commander/guidance projection, Skip/watched completion writes, and Match-HUD completion.
- `FirstLaunchNarrativeShellCompositionSystemHelper` now owns startup disposition, requested/published handoff lifecycle, one-shot route-buffer publication, and shell-boundary reset.
- `FirstLaunchNarrativeReviewPresentationSystemHelper` now owns reviewer controls, navigation, safe-area preview, completion evidence, and reviewer-only placeholder routing.
- The transitional `FirstLaunchNarrativeCompositionSystemHelper` shrank from the audited `453` lines to `260` lines without increasing its existing source-growth ceiling.
- Dedicated composition-boundary validation passes `4/4` in `/private/tmp/warline-firstlaunch-composition-boundaries-r3.log`.
- Integrated FirstLaunch architecture/behavior validation passes `39/39` in `/private/tmp/warline-firstlaunch-gate89-phase3-r2.log`.
- Assembly/namespace boundary validation passes `31/31` in `/private/tmp/warline-firstlaunch-boundary-phase3-final.log`.
- Final exact helper-path and APH-712 authorization tests pass in `/private/tmp/warline-firstlaunch-source-helper-phase3-final.xml` and `/private/tmp/warline-firstlaunch-source-authorizations-phase3-final.xml`.
- `Game.Composition` and `Game.Tests.Editor` build with zero errors; existing dependency/package warnings remain.
- Generated Unity 6.5 serialization-only scene/prefab/import diffs from the integrated runner were removed after validation.
- Next batch: isolate route/Skip decisions from the composition owner, then begin the pure sequence-progression boundary in Phase 4.

### 2026-07-12 - Phase 3 Route Policy Batch

- Status: Complete; Phase 3 exit gate is green.
- Intended behavior change: none.
- Added `Game.Narrative.Runtime`, a focused assembly with no UI, composition, runtime-gameplay, or ECS references.
- Added `FirstLaunchNarrativeRouteUtilitySystemHelper`, which returns typed decisions for watched handoff, debrief arrival, fresh-profile Skip confirmation, committed-identity direct Skip, reviewer continuation, and confirmed Skip.
- `FirstLaunchNarrativeCompositionSystemHelper` and `FirstLaunchNarrativeReviewPresentationSystemHelper` now apply typed route outcomes and contain no handoff, gameplay-placeholder, debrief-arrival, or debrief-opening state literals.
- Pure route-policy validation covers production, reviewer, debrief, confirmation-pending, fresh-profile, and committed-identity decisions.
- Focused FirstLaunch architecture validation passes `5/5` in `/private/tmp/warline-firstlaunch-route-architecture-r3.log`.
- Integrated FirstLaunch architecture/behavior validation passes `43/43` in `/private/tmp/warline-firstlaunch-gate89-route-policy.log`.
- Assembly/namespace boundary validation passes `31/31` in `/private/tmp/warline-firstlaunch-boundary-route-policy.log`.
- Exact helper-path and APH-712 authorization tests pass in `/private/tmp/warline-firstlaunch-source-helper-route-policy.xml` and `/private/tmp/warline-firstlaunch-source-authorizations-route-policy.xml`.
- `Game.Narrative.Runtime`, `Game.Composition`, and `Game.Tests.Editor` build with zero errors; existing Unity dependency warnings remain.
- Unity validation-generated scene, prefab, config, Addressables, and imported-art metadata rewrites were removed after the integrated gate.
- `git diff --check`: passed.
- Next batch: Phase 4 pure sequence state/progression extraction with typed intents and transition outputs, preserving the managed presentation edge.

### 2026-07-12 - Phase 4 Sequence Runtime Batch

- Status: Complete; Phase 4 exit gate is green.
- Intended behavior change: none.
- Added `FirstLaunchNarrativeSequenceUtilitySystemHelper` to `Game.Narrative.Runtime`; it owns graph validation, current state/index/line, authored timing, pause/resume/navigation, transition tokens, stale-intent rejection, continue/skip policy, and route-reached state.
- Added passive typed runtime models for sequence intents, outputs, and state definitions, separate from the mutable progression owner.
- `FirstLaunchNarrativeSequencePresentationSystemHelper` now maps authored config into pure definitions, sends typed intents, and applies typed outputs to panel, dialogue, audio, motion, interactive views, and handoff payload adaptation.
- The pure runtime source contains no concrete UI, composition, Unity object, Addressables, ECS, or editor API dependency; the assembly references only `Game.Catalog.Contracts`.
- Pure sequence-runtime tests pass `4/4` without loading a prefab, scene, `AudioSource`, or `AssetDatabase`: `/private/tmp/warline-firstlaunch-sequence-runtime-tests.xml`.
- Existing managed sequence compatibility tests pass `5/5` in `/private/tmp/warline-firstlaunch-sequence-presentation-phase4.log`.
- Focused architecture validation passes `6/6` as part of the integrated runner.
- Integrated FirstLaunch architecture/behavior validation passes `48/48` in `/private/tmp/warline-firstlaunch-gate89-phase4-sequence.log`.
- Assembly/namespace boundary validation passes `31/31` in `/private/tmp/warline-firstlaunch-boundary-phase4-sequence.log`.
- Exact helper-path and APH-712 authorization tests pass in `/private/tmp/warline-firstlaunch-source-helper-phase4-sequence.xml` and `/private/tmp/warline-firstlaunch-source-authorizations-phase4-sequence.xml`.
- `Game.Narrative.Runtime`, `Game.Composition`, and `Game.Tests.Editor` build with zero errors; existing Unity dependency-version warnings remain.
- Unity validation-generated scene, prefab, config, Addressables, and imported-art metadata rewrites were removed after validation.
- `git diff --check`: passed after generated metadata cleanup.
- Next batch: Phase 5 narrative contracts assembly and UI-contract separation without changing serialized config or save compatibility.

### 2026-07-12 - Phase 5 Narrative Contract Batch

- Status: Complete; Phase 5 exit gate is green.
- Intended behavior change: none.
- Added dependency-free `Game.Narrative.Contracts` with `NarrativeCommanderIdentityData`, `NarrativeGuidanceMode`, `NarrativeCompletionPayload`, and `NarrativeHandoffResult` under `Game.Narrative.Contracts`.
- `Game.UI.Contracts` now retains UI intents and interactive-view state only; it no longer owns narrative identity, guidance, completion, or handoff data.
- Added direct contract references to the actual consumers: `Game.UI.Runtime`, `Game.Composition`, `Game.Editor`, and editor/play-mode tests. The editor edge is required by typed handoff-event capture and was confirmed by compiler evidence.
- Added architecture enforcement that keeps narrative contracts free of composition, configs, gameplay runtime, UI contracts/runtime, and ECS dependencies, and prevents the four domain types from returning to the UI contract source.
- Focused architecture validation passes `7/7` in `/private/tmp/warline-firstlaunch-contracts-architecture-r2.log`.
- Final integrated FirstLaunch architecture/behavior validation passes `49/49` in `/private/tmp/warline-firstlaunch-gate89-phase5-contracts-final.log`.
- Assembly/namespace boundary validation passes `31/31` in `/private/tmp/warline-firstlaunch-boundary-phase5-contracts.log`.
- `Game.Narrative.Contracts`, `Game.UI.Runtime`, `Game.Composition`, `Game.Editor`, and `Game.Tests.Editor` build with zero errors; existing Unity dependency-version warnings remain.
- Existing source-growth ceilings remain valid after removing obsolete UI imports and keeping the typed presentation adapter within its approved ceiling.
- Unity validation-generated scene, prefab, config, Addressables, and imported-art metadata rewrites were removed after validation.
- `git diff --check`: passed.
- Next batch: Phase 6 authored audio, route, completion, and evidence policy migration out of state-ID switches.

### 2026-07-12 - Phase 6 Authored Content Policy Batch

- Status: Complete; Phase 6 exit gate is green.
- Intended behavior change: none.
- `NarrativeStateRecord` now authors music, ambience, vehicle, event cue, semantic route role, completion payload ID, evidence IDs, and mission-context flags.
- The config builder preserves the approved opening, conflict, battlefield, vehicle, radio, ARIA boot, blackout, small-arms, attack, and transition assignments while serializing them as typed state metadata.
- Mission handoff, reviewer gameplay, debrief opening/arrival, commander identity, and guidance milestones are represented by `NarrativeRouteRole` instead of production string comparisons.
- Skip requests and watched handoffs now carry typed route role and reviewer-continuation metadata through `Game.Narrative.Contracts`.
- Audio presentation selects clips only from authored cue enums. Completion payload creation selects payload/evidence/flags only from state metadata. Route decisions consume semantic requests only. Profile save compatibility receives authored milestone IDs and continues storing the same stable strings.
- Production-policy architecture validation confirms no `FL-P*` or `first_launch.*` state literals remain in audio, model/completion, profile, or pure route-policy helpers.
- Focused architecture validation passes `8/8` in `/private/tmp/warline-firstlaunch-phase6-architecture.log`.
- Generated config validation passes `5/5` in `/private/tmp/warline-firstlaunch-phase6-config.log` with `26` states, `22` panels, `17` lines, and `5` speakers.
- Integrated FirstLaunch architecture/behavior validation passes `51/51` in `/private/tmp/warline-firstlaunch-gate89-phase6-policy.log`.
- Assembly/namespace boundary validation passes `31/31` in `/private/tmp/warline-firstlaunch-boundary-phase6-policy.log`.
- Exact helper-path and APH-712 authorization tests pass in `/private/tmp/warline-firstlaunch-source-helper-phase6-policy.xml` and `/private/tmp/warline-firstlaunch-source-authorizations-phase6-policy.xml`.
- `Game.Tests.Editor` and its affected config/contracts/runtime/composition/editor dependency graph build with zero errors; existing Unity dependency warnings remain.
- The managed sequence adapter remains below the 500-line production-review threshold at `499` lines.
- Unity validation-generated scene, prefab, config, Addressables, and imported-art metadata rewrites were removed after validation.
- `git diff --check`: passed.
- Next batch: Phase 7 asynchronous current/next panel acquisition, transition-token cancellation, and failure-path tests.

### 2026-07-12 - Phase 7 Async Panel Residency Batch

- Status: Complete; Phase 7 exit gate is green.
- Intended behavior change: none.
- `NarrativePanelAssetResidencyPresentationSystemHelper` now uses Addressables completion callbacks instead of `WaitForCompletion()` and owns only current/next handles.
- Current and prepared-next slots carry transition tokens; superseded, released, and stale completions cannot publish a panel into a newer sequence transition.
- `FirstLaunchNarrativePanelPresentationSystemHelper` now owns aspect selection, next-panel traversal, direct-sprite fallback, token-checked application, and failure diagnostics outside sequence progression.
- `FirstLaunchNarrativeSequencePresentationSystemHelper` contains no `AssetReferenceSprite` or asset-load operations and shrank from `499` to `441` lines.
- Focused architecture validation passes `9/9` in `/private/tmp/warline-firstlaunch-phase7-architecture.log`.
- Focused synchronous residency validation passes `4/4` in `/private/tmp/warline-firstlaunch-phase7-residency-focused.log`.
- Full async residency validation passes `7/7`, covering success, missing keys, failed loads, release cancellation, stale completion, rapid seek, and shutdown cleanup: `/private/tmp/warline-firstlaunch-phase7-residency-all-r2.xml`.
- Integrated FirstLaunch architecture/behavior validation passes `55/55` in `/private/tmp/warline-firstlaunch-gate89-phase7-async.log`.
- Assembly/namespace boundary validation passes `31/31` in `/private/tmp/warline-firstlaunch-boundary-phase7-async.log`.
- Exact helper-path and APH-712 authorization tests pass in `/private/tmp/warline-firstlaunch-source-helper-phase7-async.xml` and `/private/tmp/warline-firstlaunch-source-authorizations-phase7-async.xml`.
- `Game.Tests.Editor` and its affected dependency graph build with zero errors; existing Unity dependency warnings remain.
- Unity validation-generated config and Addressables serialization rewrites were removed after validation.
- `git diff --check`: passed after generated serialization cleanup.
- Next batch: Phase 8 narrative-view passivity, responsibility, source-size, and accepted interaction-behavior review.

### 2026-07-12 - Phase 8 Passive Narrative View Batch

- Status: Complete; Phase 8 exit gate is green.
- Intended behavior change: none.
- `NarrativeSequenceView` no longer owns sequence/state/line IDs, transition tokens, `NarrativeUiAction` construction, or child intent routing; it is an `88`-line serialized-reference and visual-projection boundary.
- `NarrativeCommanderIdentityView` and `NarrativeGuidanceChoiceView` now emit raw selection/continue intents and project supplied visual state. They no longer own profile defaults, input normalization, transition context, commit debouncing, or fallback guidance policy.
- `FirstLaunchNarrativeInteractivePresentationSystemHelper` owns identity/guidance defaults, normalized readback, semantic action context, accessibility copy, supported-guidance policy, and one-shot commit lockout.
- The portrait and guidance arrays remain cohesive serialized widget groups; separate per-option child components would add prefab components without removing policy or meaningful duplication, so no child-view extraction was warranted in this batch.
- `NarrativeCommanderIdentityView` shrank from `345` to `206` lines, `NarrativeGuidanceChoiceView` from `250` to `165`, and `NarrativeSequenceView` from `164` to `88`.
- Focused passive-view architecture validation passes `10/10` in `/private/tmp/warline-firstlaunch-phase8-architecture-r2.log`.
- Focused commander/guidance behavior validation passes `4/4` in `/private/tmp/warline-firstlaunch-phase8-interactive.log`.
- Integrated FirstLaunch architecture/behavior validation passes `56/56` in `/private/tmp/warline-firstlaunch-gate89-phase8.log`; this includes dynamic dialogue sizing, accessibility, interaction lockout, repeated-submit rejection, profile persistence, Skip, and handoff coverage.
- Assembly/namespace boundary validation passes `31/31` in `/private/tmp/warline-firstlaunch-boundary-phase8.log`.
- Exact helper-path and APH-712 authorization tests pass in `/private/tmp/warline-firstlaunch-source-helper-phase8-r2.xml` and `/private/tmp/warline-firstlaunch-source-authorizations-phase8-r2.xml`.
- `Game.Tests.Editor` and its affected dependency graph build with zero errors; `19` existing dependency/source warnings remain.
- Unity validation-generated scene, prefab, config, Addressables, and imported-art metadata rewrites were removed after validation.
- `git diff --check`: passed.
- Next batch: Phase 9 final broad-shell, non-ECS naming, PlayMode lifecycle, approved-behavior, documentation, and closeout validation.

### 2026-07-12 - Phase 9 Validation And Closeout Batch

- Status: Complete; the FirstLaunch architecture refactor exit gate is green.
- Intended behavior change: none. The checked-in sequence asset now materializes already-authored audio, route-role, completion, evidence, and mission-context metadata so runtime behavior does not depend on an editor installer running first.
- Final integrated FirstLaunch architecture/behavior validation passes `56/56` in `/private/tmp/warline-firstlaunch-gate89-closeout.log`.
- The equivalent sequence-presentation tests and all five Menu integration tests are included in the integrated gate, covering deterministic progression, stale-token rejection, fresh-profile Skip confirmation, committed-identity direct Skip, reviewer mode, and one-shot handoff.
- Live Menu PlayMode validation passes `1/1` in `/private/tmp/warline-firstlaunch-playmode-closeout-r4.xml`, covering async opening/next panels, reviewer navigation, reduced motion, subtitles, safe area, debrief Skip, commander/guidance commits, single-source narration, gameplay placeholder, and command-base arrival.
- The PlayMode test now uses bounded async panel/state-projection deadlines instead of requiring Addressables to complete in one frame.
- Broad-shell naming passes `1/1` in `/private/tmp/warline-firstlaunch-broad-shell-closeout.log`; non-ECS naming passes `9/9` in `/private/tmp/warline-firstlaunch-non-ecs-closeout.log`.
- Assembly/namespace boundary validation passes `31/31` in `/private/tmp/warline-firstlaunch-boundary-phase8.log`.
- Exact helper-path and APH-712 authorization tests pass `1/1` each in `/private/tmp/warline-firstlaunch-source-helper-phase8-r2.xml` and `/private/tmp/warline-firstlaunch-source-authorizations-phase8-r2.xml`.
- The global source-growth runner was executed and remains red only for the unrelated pre-existing `Assets/Game/Scripts/Audio/Runtime/AudioPlaybackPresentationSystemHelper.cs` (`548` lines, `20202` bytes) review violation: `/private/tmp/warline-firstlaunch-source-growth-closeout.log`. No FirstLaunch source-growth violation remains.
- `Game.Tests.Editor` and affected dependencies build with zero errors; existing package/reference warnings remain.
- Existing visual/audio assertions remain green inside the final gate, including readable fixed typography, dynamic long-dialogue expansion, independent audio layers, cancellation, and no duplicate dialogue clip ownership.
- Unity installer rewrites were removed after the final gate, except the required authored `FirstLaunchSequence.asset` metadata now retained for production runtime use.
- `git diff --check`: passed.
- Final handoff: FirstLaunch architecture work is complete. Product-level M01 camera continuity and physical Android profiling remain governed by the narrative tracker and are not architecture-refactor blockers.

### 2026-07-19 - Locale Ownership Follow-Up

- Status: Complete; Persian FirstLaunch integration is aligned with the accepted architecture boundaries.
- Localized voice indexing and fallback selection moved from `FirstLaunchNarrativeSequencePresentationSystemHelper` into the existing portrait/voice presentation owner.
- The sequence presentation owner is `491` lines and remains below the production review threshold.
- The locale text overlay was renamed to `FirstLaunchNarrativeLocaleTextCompositionSystemHelper`; its `.meta` GUID was preserved.
- Profile language persistence remains in the established profile composition boundary.
- Exact source limits have no unused headroom. Integrated FirstLaunch architecture and behavior passes `56 / 56`; Unity reports zero compiler errors.
- The global source-growth check now reports only four separately owned operation-map files and no FirstLaunch failure.

## Handoff Rules

Every agent continuing this tracker must:

1. Read the authority documents and current batch log before editing.
2. Check `git status` and preserve unrelated user/agent work.
3. Move Unity `.meta` files with renamed assets.
4. Keep each batch behavior-preserving unless this tracker explicitly authorizes a behavior change.
5. Run the affected focused tests plus all architecture gates listed for that phase.
6. Record exact command, result, and evidence path in the batch log.
7. Do not mark a phase complete from compilation alone.
8. Do not add allowlist debt merely to make a new FirstLaunch name pass.
