Lane:
Gameplay

Task:
P0 M01 playable runtime slice for metadata-driven spawn/bind, hostile patrol route, M01 objective completion, result routing guard, command squad failure guard, and M01 Build rejection feedback.

Files changed:
- Assets/Game/Scripts/Components/MissionRuntimeComponents.cs
- Assets/Game/Scripts/Components/MissionRuntimeComponents.cs.meta
- Assets/Game/Scripts/Campaign/Chapter01M01PlayableRuntime.cs
- Assets/Game/Scripts/Campaign/Chapter01M01PlayableRuntime.cs.meta
- Assets/Game/Scripts/Campaign/MissionCommandPolicySystem.cs
- Assets/Game/Scripts/Campaign/MissionCommandPolicySystem.cs.meta
- Assets/Game/Scripts/Campaign/ChapterOneMissionCatalog.cs
- Assets/Game/Scripts/TacticalMaps/Chapter01MissionTacticalRuntimeBinder.cs
- Assets/Game/Scripts/Bootstrap/GameBootstrap.cs
- Assets/Game/Scripts/UI/Components/BattleHudGameplayBridge.cs
- Assets/Game/Scripts/UI/Shell/WarlineCaptureMatchResultFlow.cs
- Assets/Game/Scripts/UI/BuildingPlacementSystem.cs
- Assets/Game/Scripts/UI/RoadBuildSystem.cs
- Assets/Game/Scripts/UI/Screens/BuildDrawerPanelController.cs
- Assets/Tests/Editor/Campaign/Chapter01M01PlayableRuntimeTests.cs
- Assets/Tests/Editor/Campaign/Chapter01M01PlayableRuntimeTests.cs.meta
- Assets/Tests/Editor/Campaign/WarlineCaptureCampaignObjectiveTests.cs

Contracts touched:
- M01 active ids remain resolved from ActiveMissionSession: MissionId, ScenarioSetupId, LevelId, IsoMapId, MapPreviewArtId, MinimapArtId.
- M01 tactical anchors now drive runtime squad/patrol/objective/camera positions on GameplayXZ.
- Runtime entity ids added: unit.player.rifle_squad_01 and unit.enemy.patrol_01.
- M01 objective now treats the hostile patrol group as one required patrol defeat and adds required command_squad_survives guard.
- BattleHud command feedback now includes MissionDoesNotAllowBuild.

User-visible behavior:
- Starting M01 binds or creates one player command squad and one hostile patrol from the tactical map metadata.
- The hostile patrol receives its initial patrol route toward route.enemy_patrol_01.b with metadata stored on the entity.
- Destroying the hostile patrol can complete M01 and trigger the result route only while the command squad is alive.
- Losing the command squad blocks M01 completion even if the patrol is destroyed.
- Build entry points reject during M01 and show "Building unlocks in the next mission."

Validation run:
- Unity EditMode: Chapter01M01PlayableRuntimeTests
- Unity EditMode: Chapter01TacticalRuntimeBindingTests
- Unity EditMode: WarlineCaptureCampaignObjectiveTests
- Unity EditMode: BattleHudGameplayBridgeConnectionTests

Validation result:
- Chapter01M01PlayableRuntimeTests: 7/7 passed, /private/tmp/warlinecapture-m01-playable-results.xml
- Chapter01TacticalRuntimeBindingTests: 4/4 passed, /private/tmp/warlinecapture-chapter01-runtime-binding-results.xml
- WarlineCaptureCampaignObjectiveTests: 7/7 passed, /private/tmp/warlinecapture-campaign-objective-results.xml
- BattleHudGameplayBridgeConnectionTests: 6/6 passed, /private/tmp/warlinecapture-battlehud-bridge-results.xml

Known gaps:
- No PlayMode visual validation was run in this task.
- The runtime binds existing spawned visual prefabs when present; fallback entities are functional but visually minimal if initial spawn prefabs are unavailable.
- Patrol continuation beyond the initial route target is represented as metadata state, not a full looping patrol system yet.

Cross-lane impacts:
- UI can now consume MissionDoesNotAllowBuild through the shared BattleHud bridge reason path.
- UI should keep the Build drawer/button disabled or show the rejection state for M01 rather than opening build tools.
- Design/objective copy now reflects a single hostile patrol group plus command squad survival requirement.

Next recommended task:
Add the PlayMode M01 validation scene/pass that confirms the real spawned visual squad and patrol are visible at the metadata anchors, attack interaction reduces patrol health, and the result popup appears only after patrol destruction with the command squad alive.
