# SCN-01 Splash / Loading V3 Visual Lock

Status: V3 sharp-solid loading target generated and saved. V02 updates the loading screen to use the Main Menu V3 logo/font language and an integrated full-width footer loading bar. Older TargetLockV04 layer pack remains historical until new V3 slices are produced.
Date: 2026-08-29

## Active Target

- Final V3 reference: `reference/SCN-01_SplashLoadingV3_Final_Target.png`
- Active implementation target: `reference/SCN-01_SplashLoadingV3_SharpSolid_Target.png`
- Final iteration mirror: `reference/SCN-01_SplashLoadingV3_SharpSolid_Target_v02.png`
- Source generation: `/Users/farhad/.codex/generated_images/019e0cb1-e941-7eb0-b318-63b09c645a05/call_Orp5GnVutLSfdViigOylf9zi.png`
- Prior V3 iteration: `reference/SCN-01_SplashLoadingV3_SharpSolid_Target_v01.png`
- Runtime state preview: `validation/SCN-01_SplashLoading_runtime_states_preview.png`
- Prior target: `reference/SCN-01_SplashLoading_NewMainMenuArtDirection_TargetLock_V04.png`
- Prior V01 layer manifest: `layer_manifest.json`
- Prior layer contact sheet: `validation/SCN-01_SplashLoading_layers_contact_sheet.png`
- V3 target size: `1672 x 941`

This screen is the shared WarlineCapture loading surface. It appears on app start for the polished fake/minimum loading duration, and it is reused before entering a match while operation-map/session data loads.

## Runtime Use

| Runtime State | When Shown | Status Text Example | Progress Binding | Next Route |
|---|---|---|---|---|
| App boot fake load | First screen after app launch. | `INITIALIZING COMMAND NET...` | Timed 0-100 over minimum 2 seconds plus required config/save readiness. | `SCN-02 Main Menu` |
| Match loading | After Deploy/Start before the 3D match appears. | `LOADING OPERATION MAP...` | Async scene/session/map load progress. | `SCN-08 RTS Battle HUD` |

## Layering Rules For Implementation

- Do not flatten the target-lock into Unity.
- Use the background art, approved logo, frame, loading bar frame, loading fill, status text, percent text, tip text, and bottom status rail as separate Unity children.
- The V3 screen should follow the sharp solid rectangle language from Main Menu V3, Match HUD V3, and Build Popup V3.
- The logo and headline typography must match the approved Main Menu V3 logo lockup: dark rectangular plate, left gold bar, white `WARLINE`, gold `CAPTURE`, and right-side rank/star emblem.
- The old V01 pack is historical and should not be treated as V3 implementation-ready.
- Status text, percent, tip, and progress value must be live runtime bindings.
- The visual must remain neutral enough to support both app boot and pre-match loading.
- No buttons are shown by default; optional Continue/Skip is hidden unless loading is complete and the design explicitly enables it.

## Design Source

- `Design/UIUX_Gameplay_Element_Alignment.md`
- `Design/UIUX_Implementation_Detailed_Spec.md`
- `Design/VisualLockLayered/SCN-02_MainMenu/README.md`
- `Design/VisualLockLayered/README.md`

## Target Prompt Summary

The target asks for a AAA mobile RTS loading screen with:

- WarlineCapture brand lockup shown in the target only; implementation logo layer remains pending fresh isolated approval
- dry Sahrin forward operating base / command scene with light 3D holographic tactical elements
- exact live loading status, progress bar, percent, and tip text
- Main Menu V3 logo/font treatment
- sharp rectangular graphite UI panels with cyan/green/gold accents
- full-width footer loading rail that reaches the left, right, and bottom screen edges instead of a separate floating box
- no old ornate gold frame language
- no water, sea, river, coast, or naval imagery
- no route buttons or menu choices on the loading state
