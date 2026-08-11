# AM-WP-028 - Phase 2 Debt Reconciliation

Status: active audit and remediation package under `AM-025`. It does not change checklist arithmetic and cannot accept Phase 2 by itself.

## 1. Purpose

The first AM-025 delta reported `575` open rows. That number remains immutable historical audit intake, not the number of AM-021 ownership gaps and not a valid current production-remediation count. The current exact-head scan contains `430` reviewable rows after rejecting `61` arithmetic `+=` false positives from the event-subscription inventory. AM-021 separately owns `634` persistent resources with `561` explicit owners, `73` protected owners, and zero ownership gaps.

Bounded read-only audits reviewed the AM-025 intake as:

| Measure | Count |
|---|---:|
| Current exact-head intake rows | 430 |
| Reviewed non-debt/protected rows | 417 |
| Remaining genuine-debt rows | 13 |
| Remaining unique debt items | 12 |
| Projected unclassified rows | 0 |
| Source-growth blockers | 8 |

The reconciled current-head evidence records `412` resolved non-debt rows, `5` protected rows, and `13` remaining genuine-debt rows grouped into `12` unique file/rule remediation items. It remains non-accepting because every genuine-debt item must be closed before Phase 2 can pass. The original `575`-row evidence remains historical intake and is not rewritten as the current population.

## 2. Required Row Authority

Every historical intake row receives exactly one decision containing:

- source artifact and stable source key without line-number identity;
- decision: `resolved`, `protected-deferred`, or `genuine-debt`;
- reason code, rationale, authority, and evidence path/hash;
- current source path/hash when the source still exists;
- named protected owner ID when deferred;
- debt ID, owner domain, and remediation package for genuine debt.

The generator must reject missing or duplicate rows, unknown decisions, ambiguous matches, stale source hashes, count mismatches, renamed hazards without new-row accounting, fuzzy/path-only matches, or acceptance while any genuine debt remains.

## 3. Reconciliation Order

1. Regenerate current AM-007 and AM-018 scans at the exact capture commit/tree.
2. Reconcile exact retired identities and emit every new identity separately.
3. Match AM-021 resources only by category-compatible stable identity.
4. Classify non-persistent candidates only through explicit exclusion authority, never absence alone.
5. Classify arithmetic `+=` rows through parsed add-assignment structure, not text similarity.
6. Apply exact immutable-data, lifecycle-owner, protected-owner, and explicit-boundary decisions.
7. Emit genuine debt with stable debt IDs and group counts without losing row-level traceability.
8. Validate `historical = non-debt + protected/deferred + genuine debt` and require zero unclassified rows.

## 4. Remediation Lanes

Production remediation is serialized after row-bound evidence and occurs in separately owned packages:

| Lane | Projected debt | Ownership rule |
|---|---:|---|
| World lookup and runtime discovery | 6 hazard rows plus 5 overlapping World-owner candidates | Architecture may change only exact handed-off/unprotected composition/runtime paths; audio and all other operation-map rows remain protected. |
| Mutable static state | 1 diagnostics-policy row | Add one tested subsystem-reset owner; do not normalize application-static state from test cleanup alone. |
| Pools and lifecycle caches | 1 lifecycle row | Add exact capacity, reuse, exhaustion, and teardown evidence; do not infer closure from a method name. |
| World-owner candidates | 5 lifecycle rows after exact boundary/protected classifications | Reconcile overlaps with the hazard lane and avoid double-counting one production defect as multiple remediation items. |
| Source growth | 8 helper paths | Five exact operation-map paths are handed to this child; three non-map paths are owned directly by AM-025. |

Row count and unique debt-item count are reported separately because multiple lexical rows can map to one production fix.

Current external-gate note: the canonical Unity source-growth run reports eight exact helpers at their measured current sizes. Five operation-map paths have an explicit AM-025 handoff while the broad operation-map owner remains active; building production UI, runtime-grid storage, and footprint clone are direct AM-025 paths. FirstLaunch remains clear, and no ceiling receives spare headroom.

Completed remediation:

- Initial-unit diagnostics globals: AI verbosity, transport boarding diagnostics, and building runtime slice diagnostics now live only in the active World's ECS diagnostics component. AI systems use their existing ECS queries, Editor controls write through the diagnostics facade, and the obsolete global state file is removed.
- Performance-diagnostics reference: the menu publishes its initialized diagnostics instance through the active World, match startup resolves it directly, and menu shutdown clears only its matching owner. The former Menu-scene root scan is removed while the match-owned fallback remains available.
- Match-scene reference: match startup now registers its `MatchSceneView` once in the active World, menu and match-start composition read that exact reference, and shutdown clears only its matching owner. The former repeated root-object scene scan and its scene-manager dependency are removed.
- `InitialUnitsRuntimeState` gameplay and camera mirror: play, simulation, selection, build, fullscreen-map, pointer suppression, auto-mode, zoom, and initial-camera-focus state now live only in the match World's ECS components. Match startup, camera focus publication/consumption, shutdown, and test fixtures use that owner directly. The three diagnostic switches remain separately tracked for later cleanup.
- `AISettingsRuntimeState`: quick-game AI settings now belong to the active menu config store, cross the existing match-start boundary as ECS data, and are consumed once by match startup. Focused two-World validation proves settings cannot leak between menu instances or matches, and AI startup requires an explicit snapshot.
- Obsolete build UI ECS path: the duplicate static build-drawer/placement read-model sources, projection helper, orphaned request buffers, and unused action dispatch are removed. The active Canvas drawer and placement views remain authoritative; focused guards prove the duplicate path cannot return and preserve materials/fuel presentation checks.
- `MenuBootstrapCompositionSystemHelper`: the startup-settings World marker now belongs to the active menu composition and is released during menu shutdown. Focused validation proves two menu instances cannot share the marker and a closed menu retains no World.
- `UiShellRuntimeGateway`: the application gateway is replaced during Unity subsystem registration, and its ECS adapter already rebinds queries when the default World changes. Focused validation proves a stale registration is replaced and a later match cannot retain queries from the previous World.
- `GameText`: text and audio mappings are now built privately and published as one immutable application-session snapshot. Readers cannot observe a half-rebuilt catalog, replacement initialization drops stale mappings, subsystem reset returns to an uninitialized empty snapshot, and two mutable dictionary exceptions are removed from the architecture guard.
- `TerrainLodHeightSwitch`: the unused component, its process-wide camera scratch array, and its dormant per-frame update loop are removed after confirming there are no production, scene, prefab, asset, or test references.
- `InitialUnitsRuntimeState.WorldCamera`: the unused process-wide camera reference and reset line are removed. The live match camera remains owned by `RuntimeCameraReferenceSystem`, whose focused tests cover storage, lookup, and cleanup.
- `SharedPrefabPreviewCache`: preview textures, camera objects, framing state, configuration, and rendering metadata resolution now belong to the active match composition instead of the process. Match shutdown and Editor atlas generation dispose their own cache instances, focused validation proves independent ownership and cleanup, and the static-registry exception is removed. This closes 19 historical mutable-static rows.
- `UiDiagnosticsRuntimeLogBuffer`: multiple Worlds now share one bounded application log subscription through an exact owner count, and the last World releases it. Subsystem reset clears retained messages and focused validation proves one World cannot unsubscribe another.
- `AndroidPerformanceRecorder`: the process launch timestamp remains correctly application-owned, and focused subsystem-reset validation now proves a new session replaces stale clock state. No release capture or long device certification was activated.
- `ResourceExchangePopupRuntimeView`: the allocation-free active-popup stack now resets for a new play session and unregisters destroyed views. Focused validation preserves overlapping-popup fallback while preventing stale popup references after reload.
- `MainMenuNavigationView`: the selected tab remains stable while menu views are rebuilt in one session, but subsystem registration now restores the default tab for a new play session. Focused validation covers both behaviors.
- `CommanderProfileRouteLifecyclePresentation`: each shell now finds and owns its commander background only during route changes. It no longer retains scene objects globally, so independent shells cannot replace or leak each other's background.
- `AssistantSettingsPersistenceSystem`: the application settings-event subscriber count now resets with Unity subsystem registration. Focused replacement-World validation proves assistant settings reconnect after a reload instead of being blocked by a stale count.
- `MatchHudAssistantUiSystemHelper`: missing/invalid hierarchy diagnostic suppression now belongs to each HUD instance, so one disposed match cannot suppress diagnostics in a later match. Focused validation prevents the two flags from returning to process-wide state.
- `AM025-STATIC-MSL-005`: helicopter diagnostic one-shot state now belongs to each unmanaged ECS system instance instead of the process. Focused validation proves two active Worlds do not suppress each other's diagnostic and preserves all blade-spin behavior.
- `AM025-WORLD-031`: the tactical camera query cache was cleared as non-debt after focused validation proved idempotent disposal, rejection after disposal, and disposal of all three owners during selection shutdown.
- `AM025-WORLD-009`: the building command cache was cleared as non-debt after focused validation proved World rebinding, destroyed-entity recovery, buffer repair, and zero warm repeated-read allocation.
- `AM025-STATIC-IRC-001`: tactical attack-camera obstruction checks no longer keep a process-wide hit buffer. Focused validation proves the fallback shot still avoids blocked views and the helper retains no shared runtime state.
- `AM025-STATIC-MSL-009`: the 13 gameplay-update switches are bounded Editor performance-test state, not shipping gameplay authority. Focused validation proves only Editor code can change them and the capture resets every switch before setup and during completion.
- `AM025-WORLD-016`: the match-start request cache was cleared as non-debt after focused validation reused one helper across two disposed and recreated Worlds, proving each World receives its own independent request boundary.
- `AM025-WORLD-007`: the building-AI oil allocation query cache was cleared as non-debt after focused validation reused one helper across two Worlds with distinct resource values and proved only the replacement World was read.
- `PerformanceDiagnosticsSystemHelper`: the World reference warning was cleared as non-debt after focused validation proved diagnostics follows a replacement default World, reports its distinct entity count, and returns zero when no World exists.
- `FactionFuelLogisticsTelemetryBridgeCompositionSystemHelper`: the telemetry query cache was cleared as non-debt after focused validation reused one helper across two Worlds and proved each match receives an independent route-assignment count.
- `FocusableUnitLookupCameraSystemHelper`: the unit lookup cache was cleared as non-debt after focused validation replaced the World, rejected the old grid cell, resolved the replacement unit, and refreshed a same-count grid move.
- `FocusedUnitLifecycleCompositionSystemHelper`: the selected-unit query cache was cleared as non-debt after focused validation reused one lifecycle helper across two Worlds and proved the replacement match focused only its own selected unit.
- `TacticalFollowCameraModeSystemHelper`: the follow-camera singleton queries were cleared as non-debt after focused validation reused one helper across two Worlds and proved the replacement match started with fresh disabled mode and UI state.
- `UnitPathfindingPendingStateStore`: building and citizen startup now bind the pending-path reader to their explicit ECS World. Focused replacement-World validation proves the same reader follows only the newly bound match and never searches for the process-wide default World.
- `AudioEventRequestSystem`: the audio singleton and all request buffers remain owned by each ECS World, while the two process-wide entity/World cache fields are removed. Focused two-World validation proves requests cannot cross between matches, and the existing audio stress/pooling checks remain green.
- `RuntimeGameplayStateSystem`: the active match and selection startup now supply their exact EntityManager. Gameplay, camera-input, and initial-focus state no longer discover the process-wide default World, and focused replacement-World checks require an explicit rebind.
- `RtsSelectionInputStateCompositionSystemHelper`: match startup creates one input owner from its EntityManager and shares it with selection gameplay and UI commands. Focused replacement-World validation requires an explicit rebind, and the complete move/attack/scan/boarding/camera/HUD command suite remains green.
- Selection composition boundaries: building interaction, runtime query delegates, and managed RTS camera systems now use the EntityManager supplied to selection startup. The remaining selection default-World lookups are removed while the complete command suite remains green.
- `RuntimeGameplayStateSystem`: the cached state entity was cleared as non-debt after focused validation replaced the default World and proved the same facade created and read only the replacement match's fresh state.
- `SceneLifecycleSceneSystemHelper`: the scene-transition queue cache was cleared as non-debt after focused validation replaced the World and proved the new match began with an empty queue and accepted only its own request.
- `RuntimeDiagnosticsSystem`: the diagnostics World lookup was cleared as non-debt after focused validation replaced the default World and proved the same facade created and read only the replacement match's diagnostics state.
- `GameplayRuntimeUpdateCompositionSystemHelper`: the startup-count query cache was cleared as non-debt after focused validation reused one helper across two Worlds and proved the replacement match reported only its own spawn readiness counts.
- `RtsSelectionInputStateCompositionSystemHelper`: the player-input state cache was cleared as non-debt after focused validation reused one helper across two Worlds and proved the replacement match started with fresh input state and an empty pointer queue.
- `VisibleUnitSelectionCameraSystemHelper`: the visible-unit query cache was cleared as non-debt after focused validation reused one selector across two Worlds and proved the replacement match returned only its own visible unit.
- `BuildingRuntimeProcessingCompositionSystemHelper`: the building runtime boundary cache was cleared as non-debt after focused validation reused one helper across two Worlds and proved the replacement match published only to its own building boundary, even when a new-world entity reused the stale identity.
- `SelectedUnitOrderSnapshotCompositionSystemHelper`: the temporary selected-unit order snapshot was cleared as non-debt after focused validation proved binding a replacement World discards the previous match's saved orders before restoration can run.
- `SelectionRuntimeDiagnosticsSystemHelper`: selection diagnostics now receive the active match EntityManager explicitly; focused two-World validation proves logs route only to the supplied match and no longer depend on the global default World.
- `BattleScenarioLabVisualPlayback`: the Scenario Lab playback candidate was cleared as non-debt after a focused guardrail proved the long-lived view has no retained World, EntityManager, or EntityQuery fields and resolves ECS state only at action boundaries.
- `RtsSelectionPointerTargetCommandCompositionSystemHelper`: the pointer-target query cache was cleared as non-debt after focused validation reused one resolver across two Worlds and proved an attack tap selected only the replacement match's building.
- `SelectionGameplayStartupSystemHelper`: the selection startup query owner was cleared as non-debt after focused validation initialized one runtime closure and proved its captured World switched from the first match to the replacement match.
- `SelectionBuildingInteractionCompositionSystemHelper`: the building-selection query owner was cleared as non-debt after focused validation reused one helper across two Worlds and proved its grid query switched from the first match to the replacement match.
- `FocusedUnitUiReadModelUiSystemHelper`: the selected-unit details query owner was cleared as non-debt after focused validation reused one helper across two Worlds and proved the replacement match received a fresh read model without changing the previous match's data.
- `MatchHudSquadTraySelectionUiSystemHelper`: the squad-tray query owner was cleared as non-debt after focused validation reused one helper across two Worlds and proved a tray selection affected only the replacement match while leaving the previous match unchanged.
- `SelectionHudFeedbackUiSystemHelper`: the command-feedback and selected-count query owners were cleared as non-debt after focused validation reused one helper across two Worlds and proved each match retained only its own feedback queue and selected-unit count.
- `BuildingGameplayEcsQueryCompositionSystemHelper`: the shared building query set was cleared as non-debt after focused validation reused one helper across two Worlds and proved the replacement match reported only its own building boundary and selected units.
- Resource integration audit: exchange feedback history is bounded to the newest 32 messages; AI and player construction spend tactical materials while legacy credits remain unchanged; Build Drawer and resource helpers remain within their existing size contracts. Focused Unity validation and the combined architecture suite pass.
- `RuntimeGameplayStateSystem` static cache fields and default-World lookup are cleared as non-debt. The facade retains only its explicitly supplied EntityManager, and focused replacement-World validation proves rebinding starts with fresh match state.
- AM-021 ownership snapshot refresh: the canonical inventory is rebound to current source with the same `575` resources, `553` explicit owners, `22` protected owners, and zero gaps. The refreshed map source manifest and shifted line metadata do not change any ownership decision.
- `GameText` partial hardening: Unity subsystem registration now clears localized text, audio-event mappings, and initialization state before a new play session. The shared static dictionaries remain tracked debt and receive no closure credit.
- `UnitTransportVisualUtility`: passenger hide/restore traversal now uses call-owned temporary native memory instead of process-wide lists. Focused two-World validation proves each match restores only its own passenger, and the static-registry guard confirms all three shared fields and their allowlist exceptions are gone.
- Minimap UI lifecycle: marker images now remain owned by one minimap helper across view rebuilds and are destroyed only when that helper is disposed. The large raster buffer is instance-owned and released with the helper, while viewport corners are computed without a process-wide mutable array. Focused validation covers rebind reuse, disposal, independent buffers, and the absence of static array state.

## 5. Scope Safety

- Work directly on `main` with task-owned staging and stable commits.
- Preserve operation-map, UI visual-lock, scenes, prefabs, packages, `ProjectSettings`, and unrelated dirty work unless the exact owner provides a handoff.
- Prefer unmanaged `ISystem`; do not introduce `SystemBase`, service locators, mutable gameplay registries, broad managers/controllers/providers, or second update owners.
- Naming must follow the project convention; no `*Controller`, `*Player`, vague `*Utility`, or generic ownership shell is introduced.
- Release-only Android, thermal, cold/warm, sustained, package, and certification work remains deferred.

## 6. Validation And Acceptance

Each remediation slice requires focused tests, applicable architecture gates, Unity compilation with zero compiler errors when C# changes, deterministic projection checks, `git diff --check`, and a protected-path diff audit.

AM-025 remains unchecked until:

- all `430` current exact-head intake rows have one validated row-bound decision while the original `575`-row intake remains immutable historical evidence;
- genuine-debt and unclassified counts are zero;
- all eight source-growth blockers are closed by accepted owner action;
- the canonical AM-WP-027 suite passes without exclusions or threshold relaxation.
