# GC07 Demo Military Playable Scene

Lane: Gameplay
Task: Build a visually good top-down military RTS playable scene from the authored Demo military-base composition.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureGc07DemoMilitaryPlayableSceneBuilder.cs`
- `Assets/Game/Scenes/Generated/GC07_DemoMilitaryPlayable_2048.unity`
- `Design/AgentReports/Captures/GeneratedScenes/GC07_DemoMilitaryPlayable_2048/gc07_target_style_overview_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC07_DemoMilitaryPlayable_2048/gc07_base_close_1920x1080.png`
- `Design/AgentReports/Captures/GeneratedScenes/GC07_DemoMilitaryPlayable_2048/gc07_runway_close_1920x1080.png`

Contracts touched: Gameplay playable scene generation workflow contract.
User-visible behavior: none in shipped flow; generated scene is available for visual review.
Validation run: Unity batchmode `WarlineCaptureGc07DemoMilitaryPlayableSceneBuilder.BuildGc07DemoMilitaryPlayableScene`.
Validation result: passed dense authored-scene clone validation.
Known gaps: this is the visual/playable basis pass; internal walkability masks still need to be authored after visual acceptance.
Cross-lane impacts: PM/Design can review the new scene against the supplied top-down military RTS target before ECS/runtime conversion.
Next recommended task: tune camera, crop, and any blocked-lane cleanup based on visual review.

Source roots cloned: 4225

Validation log:
- PASS: GC07 cloned a dense authored Demo military compound into a flat generated playable scene.
