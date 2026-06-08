# PM Unity Licensing Workaround Reaffirmed

Date: 2026-05-18
Lane: PM
Task: Correct Unity licensing routing for active lane validation
Status: accepted correction

## Decision

The prior M01 V29 routing that treated Unity batchmode licensing as a PM/user local-environment blocker is superseded.

Unity licensing/client handshake loops are a known Codex sandbox/workspace issue with an existing project workaround. Agents must use the workaround before reporting that Unity licensing blocks their lane.

## Source Rules

- `Design/AgentReports/2026-05-08_pm_unity-batchmode-licensing-escalation-rule.md`
- `Design/AgentReports/2026-05-08_workflow_unity-licensing-loop-stop-rule.md`
- `Design/AgentReports/2026-05-07_pm_unity-workspace-allocation-update.md`
- `Design/AgentReports/2026-05-07_pm_persistent-unity-approval-instruction.md`
- `Design/AgentTasks/AUTO_CONTINUE.md`
- `Design/Agent_Coordination_Workflow.md`

## Mandatory Agent Behavior

If required Unity validation, prefab building, or capture work enters `LicenseClient-farhad`, unsupported protocol, reconnect, or timeout loops before tests/capture start:

- use the lane's assigned Unity workspace;
- rerun the same required Unity batchmode command with Codex escalation/out-of-sandbox execution;
- request persistent narrow Unity executable plus `-batchmode` approval when the tool offers it;
- stop stuck licensing-loop Unity processes when needed as product-approved cleanup;
- include exact command, workspace, log path, and symptom in the lane report if the escalated retry still stalls.

Do not route a licensing-loop complaint back to PM/user until this documented workaround has been attempted and reported.

For graphics/runtime capture proof, do not use `-nographics` when the capture path needs GPU/window access or when prior attempts show blank/headless output or missing headless package errors.

## Current M01 V29 Routing Correction

Gameplay owns the next action.

Gameplay must run the fresh V29 recapture in:

- `/Users/farhad/Projects/WarlineCapture-CodexUnity1`

Expected report:

- `Design/AgentReports/2026-05-18_gameplay_m01-v29-final-recapture-proof.md`

Art/Atlas, UI, and QA/HCI remain held for this specific in-game recapture until Gameplay delivers the proof or documents a real post-workaround blocker.

## Validation Result

Coordination-only update. No runtime validation was run by PM for this correction.

## Next Recommended Task

Gameplay applies the Unity licensing workaround and delivers the final V29 recapture proof.
