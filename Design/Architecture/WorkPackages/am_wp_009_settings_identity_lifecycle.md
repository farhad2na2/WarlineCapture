# AM-WP-009 - Settings Identity And Lifecycle

Status: draft, dependency-blocked, and not dispatchable. Do not implement until Phase 2 (`AM-020` through `AM-025`) is accepted, `AM-027` accepts cross-domain settings versions, `AM-028` accepts lifecycle/invalidation ownership, and separately owned audio/FirstLaunch consumers explicitly accept any required contract change.

Umbrella task: `AM-033`

Inventory source: `Design/Architecture/ui_projection_allocation_inventory.md`, rows `UI-005` and `UI-016`.

## 1. Current Ownership And Risk

- `SettingsService` owns PlayerPrefs persistence, platform defaults, runtime quality/frame pacing, master listener volume, and a static `RuntimeApplied` event reset at subsystem registration.
- `SettingsScreenFlowUiSystemHelper` loads, saves, resets, applies runtime settings, and binds the supplied controls view.
- `SettingsPopupView` provides Menu/Match context, button actions, and a `SettingsPanelView`.
- `SettingsPanelView` and `SettingsScreenView` independently duplicate much of the same control binding, model extraction, event wiring, labels, and interaction logic.
- Both active paths are event-driven; no view-local polling loop was found.
- Assistant and audio ECS projections maintain separate versions/fingerprints, but there is no one accepted settings generation covering persisted model, runtime-applied model, localization, accessibility, graphics, controls, notifications, assistant, narrative, and audio domains.

Risks are lifecycle/version drift, duplicate control behavior, repeated full binds when only one domain changes, subscriber leaks/duplicates, and inconsistent Menu versus Match behavior. This package must not replace a correct event-driven path with polling or a general-purpose service container.

## 2. Accepted Future Ownership

- `SettingsService` remains the platform persistence/runtime-apply boundary. Its static event remains subsystem-reset and publishes an explicit immutable settings identity.
- One plain `UISettingsFingerprintUtilitySystemHelper` computes a stable complete fingerprint and narrow domain generations for Audio, Graphics, Controls, Notifications, Accessibility, Localization, Assistant, and Narrative.
- One reusable settings-controls projection/binding helper owns duplicated field-to-control mapping. Screen and popup remain separate views/context shells.
- Views retain control state and apply only changed domains. User input mutates the local draft; Save publishes once, Reset publishes once, and Close preserves the documented unsaved-draft behavior.
- Runtime consumers subscribe/unsubscribe through explicit lifecycle owners and ignore equal identities.
- Audio and FirstLaunch remain separately owned consumers. This package cannot edit them without exact handoff; tests characterize their existing event contract as read-only integration.
- Menu and Match Settings use the same model semantics, persistence, validation, and runtime apply order while preserving their titles, close behavior, and visual targets.

No new `SystemBase`, polling loop, mutable global model, broad manager/controller/provider/service type, or default-World lookup is allowed. The existing `SettingsService` name is grandfathered platform-boundary ownership, not precedent for new service types.

## 3. Version And Invalidation Contract

Minimum identity:

- complete settings generation/fingerprint;
- narrow Audio, Graphics, Controls, Notifications, Accessibility, Localization, Assistant, and Narrative generations;
- persisted generation and runtime-applied generation;
- platform/defaults schema version;
- Menu/Match view instance and binding generation;
- localization catalog, resolution/layout, and scene lifecycle generations;
- explicit invalidation generation and reason.

Rules:

- Saving a model equal after normalization does not publish a new runtime generation or rebuild controls.
- Platform normalization occurs before fingerprint comparison and publication.
- One changed domain updates only its runtime consumers and affected controls.
- Load/bind does not emit samples, save, or publish user-change events.
- Reset applies defaults once and publishes one normalized identity.
- Repeated open/close/rebind cannot duplicate listeners/subscribers.
- Subsystem registration clears static subscribers and identity state deterministically.
- Version rollover uses equality-based invalidation under `AM-028`.

## 4. Exact File Allowlist

Production files allowed after dispatch:

- `Assets/Game/Scripts/UI/Settings/UISettingsModels.cs`
- `Assets/Game/Scripts/UI/Settings/ISettingsControlsView.cs`
- `Assets/Game/Scripts/UI/Settings/SettingsService.cs`
- `Assets/Game/Scripts/UI/Settings/SettingsScreenFlowUiSystemHelper.cs`
- `Assets/Game/Scripts/UI/Settings/SettingsPopupView.cs`
- `Assets/Game/Scripts/UI/Settings/SettingsPanelView.cs`
- `Assets/Game/Scripts/UI/Settings/SettingsScreenView.cs`
- `Assets/Game/Scripts/UI/Shell/UIAccessibilityApplier.cs`
- `Assets/Game/Scripts/UI/Settings/UISettingsFingerprintUtilitySystemHelper.cs` and its `.meta` if required
- `Assets/Game/Scripts/UI/Settings/UISettingsControlsProjectionUiSystemHelper.cs` and its `.meta` if required
- `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs` only for Settings popup lifecycle integration

Test files allowed:

- `Assets/Tests/Editor/SettingsPopupValidationTests.cs`
- `Assets/Tests/Editor/SettingsAudioRuntimeSmokeValidation.cs` as read-only integration coverage
- `Assets/Tests/Editor/UiAudioEventViewTests.cs` as read-only interaction coverage
- `Assets/Tests/Editor/AISettingsValidationTests.cs`
- `Assets/Tests/Editor/AudioSettingsUiProjectionTests.cs` as read-only audio projection coverage
- `Assets/Tests/Editor/UISettingsIdentityLifecycleTests.cs` and its `.meta` if required
- `Assets/Tests/Editor/UISettingsOpenSurfacePerformanceValidation.cs` and its `.meta` if required

Evidence files allowed:

- `Design/AgentReports/ArchitectureMaturity/am_wp_009_settings_identity_lifecycle_evidence.json`
- `Design/AgentReports/ArchitectureMaturity/am_wp_009_settings_identity_lifecycle_acceptance.json`
- task-owned compressed logs under `Design/AgentReports/ArchitectureMaturity/Logs/`
- the `AM-033` tracker record and progress snapshot

Read-only dependencies:

- audio production/config/projection, FirstLaunch/narrative production, UI visual-lock assets, settings prefabs, and scenes;
- quality settings and platform APIs as runtime behavior fixtures.

Hard exclusions:

- operation-map/static-map production, scenes, trackers, and evidence;
- FirstLaunch, audio production, visual-lock art, prefabs, packages, and `ProjectSettings`;
- setting defaults/labels/options, frame ceilings, quality mapping, balance, UX, layout, art, audio samples, or persistence-key changes;
- any production file outside this allowlist without a reviewed amendment and active-owner handoff.

## 5. Characterization Matrix

Required before edits:

1. Menu screen and popup, Match popup, repeated open/close, scene transition, and subsystem registration;
2. load, equal save, changed save, reset, close without save, and reopen;
3. every settings domain changed independently and multiple domains changed together;
4. Android versus Editor normalization, especially `120 -> 60` frame-rate normalization;
5. load/bind without audio samples or change callbacks;
6. repeated bind/unbind and subscriber attach/detach proving one callback per action;
7. accessibility, localization, assistant, narrative, graphics/frame pacing, controls, notifications, and read-only audio integration;
8. malformed/out-of-range persisted values and defaults fallback;
9. view destroyed during apply, missing controls, and duplicate open-view scenarios.

Record PlayerPrefs reads/writes, complete/domain versions, runtime publications, subscriber counts, control binds, changed controls, Unity quality/frame calls, audio sample events, allocations, and apply time.

## 6. Baseline And Acceptance Gates

Measure:

- `180` warmup plus `300` unchanged open frames for Menu screen, Menu popup, and Match popup;
- `100` open/close and bind/unbind cycles;
- equal save, each single-domain change, multi-domain change, reset, lifecycle replacement, and platform normalization separately.

Acceptance:

- exactly zero recurring production-owned managed bytes while any Settings surface is open and unchanged;
- no polling owner is introduced;
- equal normalized save causes zero runtime publication and zero control rebind;
- one runtime publication per changed save/reset and only affected domain consumers apply;
- one callback per interaction after repeated lifecycle cycles;
- load/bind emits no settings sample or user-change event;
- Menu and Match retain behavioral parity and context-specific titles/close flows;
- static subscribers are cleared at subsystem registration with no stale consumer;
- focused tests, zero compiler errors, integrated architecture checks, and `git diff --check` pass;
- evidence binds baseline/implementation commit and tree, source hashes, compressed logs, metrics, and focused review.

## 7. Maximum Slices And Rollback

At most three independently stable commits:

1. fingerprint/domain versions and lifecycle characterization without behavior change;
2. shared controls projection and changed-domain apply while preserving both views;
3. subscriber/equal-publication hardening and complete Menu/Match acceptance matrix.

Rollback if persistence, defaults, normalization, frame pacing, quality, accessibility, localization, assistant/narrative/audio integration, samples, close/save/reset behavior, or visual state changes; if callbacks duplicate; or if the slice introduces polling, `SystemBase`, mutable global model authority, protected-file edits, or non-allowlisted overlap.
