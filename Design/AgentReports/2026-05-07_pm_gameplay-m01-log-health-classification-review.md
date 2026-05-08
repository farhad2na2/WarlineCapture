Gate: Gate 4 QA/HCI M01 Smoke And Readability
Status: blocked
Reason:
Gameplay submitted the required log-health classification handoff, and the proposed source fix is correctly scoped to the project-owned generic AI plan noise. However, the handoff cannot be accepted yet because focused Unity EditMode and PlayMode validation did not reach Test Runner execution. Both validation attempts were blocked by Unity Licensing Client / package entitlement failures.
Validation accepted:
- Static scan found no banned scene-search calls introduced in the touched gameplay files.
- The report classifies the prior AI log spam as project-owned: `AIProduction MissingProducerBuilding`, `AIBuild Blocked`, and `AISquad Waiting`.
- The report classifies the prior `RenderTexture.Create failed`, Entities Graphics `NullReferenceException`, preview-scene leak, and persistent allocation warnings as package/editor/headless or editor-shutdown issues based on the scanned prior QA log stacks.
- The implementation preserves the M01 fixed tactical direction by disabling generic AI build/production/squad plans only when `Chapter01M01PlayableRuntime.IsActiveMission()` is active.
Validation still needed:
- Successful focused `Chapter01M01PlayableRuntimeTests` run.
- Successful focused `Chapter01M01PlayModeValidationTests` run.
- Log confirmation from a healthy Unity run that the three generic AI plan noise lines are gone.
- Stronger non-headless/player evidence if QA still sees render-target, Entities Graphics, leak, or freeze warnings after Unity licensing is healthy.
Cross-lane notices:
- QA/HCI remains blocked from final Gate 4 readiness until this validation rerun is green or the remaining log warnings are reclassified with healthy-editor evidence.
- UI can continue the integrated 16:9/20:9 capture matrix independently, but QA should not combine it into final Gate 4 pass until Gameplay validation is unblocked.
- Support/FTUE has no new action from this handoff.
Next gate/task:
Resolve the Unity Licensing Client/package entitlement blocker, then rerun the focused Gameplay validation. Do not start M02-M05, final atlas packaging, destroyed VFX, or broad gameplay polish from this handoff.
