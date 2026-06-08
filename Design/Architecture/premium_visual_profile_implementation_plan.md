# Premium Visual Profile Implementation Plan

## Summary

Create a desktop-focused Premium Visual Profile for the Unity project using generic names only, with validation in `/Users/farhad/Projects/WarlineCapture-CodexUnity2`. The upgrade centralizes URP quality, global volume grading, lighting, fog, shadows, and screenshot validation without changing mobile quality or adding project-name-prefixed assets.

## Naming Rule

New files, assets, and classes for this work must use generic names. Do not prefix new names with the project name.

Accepted examples:

- `VisualQualityConfig.asset`
- `PremiumGlobalVolumeProfile.asset`
- `PremiumLightingRig.prefab`
- `VisualQualityProfileAsset`
- `VisualQualitySettingsSystem`

Avoid project-name-prefixed variants.

## Implementation Steps

1. Create `Assets/Game/Rendering/`, `Assets/Game/Rendering/Profiles/`, and `Assets/Game/Rendering/Prefabs/`.
2. Duplicate current PC URP assets into `PC_Premium_RPAsset.asset` and `PC_Premium_Renderer.asset`.
3. Tune premium URP with HDR, 4096 main shadows, 2048 additional shadows, 4 cascades, render scale 1.0, SRP Batcher, and stronger clean SSAO around intensity 0.75 and radius 0.6.
4. Create `PremiumGlobalVolumeProfile.asset` using ACES/Filmic tonemapping, controlled contrast/saturation, subtle bloom, neutral low vignette, warm daylight grade, cool shadows/warm highlights, and no strong green tint.
5. Create `PremiumLightingRig.prefab` with a warm low-angle sun, optional weak cool fill, a global volume using the premium profile, and a gameplay-area reflection probe.
6. Add generic runtime config/system files: `VisualQualityProfileAsset`, `VisualQualityConfig.asset`, and `VisualQualitySettingsSystem`.
7. Integrate with existing scene ownership: `MatchSceneView` remains the raw reference holder and runtime application happens through system-style code.
8. Map existing settings so `Ultra` uses the premium profile, `High` can remain the existing PC profile, and `Mobile` remains unchanged.

## Validation

Run Unity validation from `/Users/farhad/Projects/WarlineCapture-CodexUnity2`.

- Unity compile has no errors and no new warnings.
- Gameplay scene smoke starts successfully.
- Premium profile applies to camera, global volume, and render pipeline.
- Mobile quality still uses the mobile pipeline.
- Before/after screenshots are captured at 16:9 and 20:9.
- Shadows, contrast, lighting depth, terrain readability, unit readability, and UI readability improve without excessive bloom or darkening.

## Assumptions

- Premium profile targets PC/desktop first.
- Mobile remains conservative.
- Existing ECS/bootstrap boundaries remain: views hold references and systems apply behavior.
