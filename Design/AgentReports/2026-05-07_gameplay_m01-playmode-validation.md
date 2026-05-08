Lane:
Gameplay

Task:
P0 M01 PlayMode validation for the playable runtime slice in the real Game scene.

Files changed:
- Assets/Game/Scripts/Campaign/Chapter01M01PlayableRuntime.cs
- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs
- Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs
- Assets/Game/Scripts/UI/Shell/WarlineCaptureMatchResultFlow.cs
- Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs
- Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs.meta
- Assets/Tests/Editor/Campaign/Chapter01M01PlayableRuntimeTests.cs

Contracts touched:
- M01 runtime now binds real PlayMode-spawned combat units instead of creating fallback entities while the game is playing.
- M01-bound command squad and hostile patrol are repositioned to the tactical metadata anchors.
- M01 camera start now prefers `camera.default_start` and suppresses the initial-base camera override for active M01.
- Result routing exposes a preflight check so validation can prove result readiness without forcing popup persistence.

User-visible behavior:
- Starting M01 in the Game scene places the command squad and hostile patrol at their expected metadata anchors.
- The camera starts on the M01 tactical camera anchor instead of jumping to the generated base core.
- Selecting the command squad and issuing an attack-state command can drive real combat damage against the hostile patrol.
- Result readiness is allowed only after the hostile patrol is destroyed while the command squad survives.
- Destroying the command squad blocks M01 completion.
- Build remains rejected in M01 with "Building unlocks in the next mission."

Validation run:
- Unity PlayMode: Chapter01M01PlayModeValidationTests
- Unity EditMode: Chapter01M01PlayableRuntimeTests
- Unity EditMode: Chapter01TacticalRuntimeBindingTests
- Unity EditMode: WarlineCaptureCampaignObjectiveTests
- Unity EditMode: BattleHudGameplayBridgeConnectionTests

Validation result:
- Chapter01M01PlayModeValidationTests: 3/3 passed, /private/tmp/warlinecapture-m01-playmode-results.xml
- Chapter01M01PlayableRuntimeTests: 7/7 passed, /private/tmp/warlinecapture-m01-playable-results.xml
- Chapter01TacticalRuntimeBindingTests: 4/4 passed, /private/tmp/warlinecapture-chapter01-runtime-binding-results.xml
- WarlineCaptureCampaignObjectiveTests: 7/7 passed, /private/tmp/warlinecapture-campaign-objective-results.xml
- BattleHudGameplayBridgeConnectionTests: 6/6 passed, /private/tmp/warlinecapture-battlehud-bridge-results.xml

Known gaps:
- PlayMode validates the attack command state and real combat damage through the selected unit and `EngageTarget`; it does not perform a device-level tap smoke.
- The M01 patrol still has initial route metadata and first route target, not a full looping patrol behavior.
- Result popup visual presentation is covered through result-route readiness, not a screenshot or visual popup inspection.

Cross-lane impacts:
- UI can rely on the M01 camera opening at `camera.default_start` for HUD framing validation.
- UI and Support/FTUE can target stable runtime ids `unit.player.rifle_squad_01` and `unit.enemy.patrol_01` in the loaded Game scene.
- QA/HCI can use the new PlayMode suite as the technical baseline before device tap validation.

Next recommended task:
Implement the next M01 production interaction slice: objective HUD marker/jump wiring and FTUE/ARIA recommendation hooks against the validated runtime ids and anchors.
