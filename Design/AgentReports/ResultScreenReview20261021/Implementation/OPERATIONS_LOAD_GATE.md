# Operations O001 live verification gate

The result-screen implementation is native-rendered and its callback/settlement fixtures pass. A live O001 ARIA/result/return run cannot currently reach the result screen.

- Persian run: normal visible Deploy action reached the mission launch path, then reported that loading failed and the action point was refunded. Wrapper exited 1. Full log: `shared-result-operations-aria-20261021-01.log` in the validation archive.
- English diagnostic rerun: observed the existing launch reference without changing gameplay facts: `[OperationsStartupObserved] failure=Mission objective is blocked at int2(1860, 710) readOnly=1`. Wrapper exited 1 before a result screen. Full log: `shared-result-operations-startup-diagnostic-20261021-02.log` in the validation archive.
- The navigation refusal comes from `OperationsReconSpawnCompositionSystemHelper.ValidateNavigation`. The existing failure/refund flow is owned by `OperationsMissionPresentationSystem.ObserveFailedStartup`.

The result changes do not edit mission configuration, objective positions, navigation geometry, physical maps, budgets or win predicates. Per `Design/MapVariants/CAMPAIGN_MAP_REUSE_POLICY.md`, this is recorded as an unresolved objective/source-fit gate for mission work. Resolve the authored objective against legal navigation on an existing Campaign map, then repeat the complete normal-input O001 ARIA/result/return journey. A native view fixture is not a substitute for that gate.

Player/device acceptance is pending separately. No Operations mission readiness is claimed by this result-screen task.
