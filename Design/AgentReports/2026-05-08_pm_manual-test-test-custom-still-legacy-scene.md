Status: blocked
Topic:
Manual Test/Custom launch still shows old scene after prior launch-path fix

Source:
User manual test feedback on 2026-05-08.

Finding:
The user launched the Test and Custom game modes and still saw the old scene. This means the public/manual launch blocker is not fixed for user testing.

Why the prior evidence was insufficient:
The earlier UI launch-path fix proved that Quick Custom could keep `WarlineCaptureRouter` on `WarlineCaptureRoute.Match` and keep `UI_Canvas` inactive in focused PlayMode evidence. That does not prove the first rendered gameplay scene is the current M01 production slice. `WarlineCaptureGameLaunchUtility.StartM01ProductionRoute` still calls `GameBootstrap.BeginGameplay()`, which may initialize the same legacy 3D gameplay scene while the new WarlineCapture router/HUD remains active.

Likely gap:
The team validated route state and some ECS sprite-presenter components, but did not validate the actual player-visible scene/camera/rendered gameplay after pressing Test/Custom/Launch. The acceptance gate must check what the player sees, not only router state or inactive legacy UI canvas.

Affected paths:
- Test launch path reported by user.
- Custom game mode / Quick Custom path reported by user.
- Campaign path remains unaccepted until independently proven.

Revised acceptance requirement:
Gameplay/UI must prove that the first player-visible scene after public launch is the current M01 production visual direction, not the old 3D prototype. Evidence must include one of:
- player-visible screenshot/capture after launch;
- graphics-enabled Unity capture after launch;
- manual QA report explicitly confirming the visible scene/camera/rendered content.

The report must state:
- entry path used;
- active mission id;
- route state;
- whether `UI_Canvas` is active;
- whether legacy 3D prototype visuals are visible;
- whether current M01 2D/isometric sprite-presenter/sprite-renderer visuals are visible;
- screenshot/capture/log evidence path.

Rejected assumption:
Do not treat `WarlineCaptureRoute.Match` plus `UI_Canvas inactive` as sufficient proof. That is only route/HUD proof.

Affected lanes:
- Gameplay
- UI
- QA/HCI
- PM

Needs user decision:
No. This is a failed manual validation. The agents need to fix/prove the visible production scene before asking the user for another broad test.

Next task update needed:
Yes. Gameplay/UI task files must explicitly say the prior route-only evidence is insufficient and the visible old scene remains the blocker.
