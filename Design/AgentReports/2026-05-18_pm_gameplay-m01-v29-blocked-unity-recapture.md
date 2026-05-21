# PM Review - M01 V29 In-Game Pass Blocked On Unity Recapture

Date: 2026-05-18
Owner: PM
Status: V29 not accepted; blocked on fresh Unity recapture
Priority: P0

## Reviewed

Gameplay report:

- `Design/AgentReports/2026-05-18_gameplay_m01-v29-ingame-target-match-proof.md`

Proof artifacts from the last successful V29 capture before the final player-anchor tweak:

- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v29_vs_Target_Comparison.png`
- `Design/AgentReports/Captures/M01-01_V29_PlayerCrop_Target_Runtime.png`
- `Design/AgentReports/Captures/M01-01_V29_EnemyCrop_Target_Runtime.png`

PM also attempted a fresh recapture without `-nographics`:

- log: `/private/tmp/warlinecapture-m01-game-flow-v29-pm-rerun.log`

## Decision

V29 is not accepted as complete.

Accepted as progress:

- V28 soldier runtime binding remains active.
- In-game scope was correctly separated from HUD/canvas visual matching.
- Gameplay reports `GameplayArchitectureContractTests` passed 6/6.
- Last successful V29 proof shows the runtime still launches through the expected M01 flow.

Rejected as final completion:

- The final player anchor `{x: 0.22, y: 0.46}` is not visually verified.
- The latest valid image is from before that final anchor tweak.
- The last valid comparison still shows in-game/world mismatch, especially player placement and enemy spacing/overlay placement.
- A completed V29 pass requires a fresh runtime capture after the final anchor change.

## Blocker

Unity batchmode recapture is blocked by licensing/client handshake failure, not by an Art, UI, or QA issue.

Gameplay reported:

- `Error: listen EPERM: operation not permitted /tmp/Unity-Upm-827.sock`
- `HandshakeResponse reported an error: ResponseCode: 505 ResponseStatus: Unsupported protocol version '1.18.1'.`
- `Timed-out after 60.00s, waiting for channel: "LicenseClient-farhad-6000.4.0"`
- `Error: 'com.unity.editor.headless' was not found.`

PM reran without `-nographics` and hit the same licensing handshake loop:

- `HandshakeResponse reported an error: ResponseCode: 505 ResponseStatus: Unsupported protocol version '1.18.1'.`
- `Timed-out after 60.00s, waiting for channel: "LicenseClient-farhad-6000.4.0"`
- `Error: 'com.unity.editor.headless' was not found.`

## Routing

Current owner:
PM/user local environment, to restore Unity batchmode licensing for Editor `6000.4.0f1`.

Gameplay is held until Unity can run:

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV29 -logFile /private/tmp/warlinecapture-m01-game-flow-v29-final.log -quit
```

Expected Gameplay report after licensing is fixed:

- `Design/AgentReports/2026-05-18_gameplay_m01-v29-final-recapture-proof.md`

## Held

- Gameplay must not claim V29 complete without the fresh recapture.
- QA remains held.
- Art/Atlas remains held; no new Art blocker was proven.
- UI remains later owner for HUD/canvas target-lock and should not be routed from this V29 blocker.
