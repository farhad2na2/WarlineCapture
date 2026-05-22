# SCN-01 Splash / Loading Visual Lock

Status: Target-lock mockup generated. Layer pack not generated yet.
Date: 2026-05-23

## Active Target

- Reference target: `reference/SCN-01_SplashLoading_Landscape_Target.png`
- Runtime state preview: `validation/SCN-01_SplashLoading_runtime_states_preview.png`
- No-UI background source: `generated_one_go/source/SCN-01_SplashLoading_Background_21x9_NoUI.png`
- Canonical size: `2400 x 1080`

This screen is the shared WarlineCapture loading surface. It appears on app start for the polished fake/minimum loading duration, and it is reused before entering a match while operation-map/session data loads.

## Runtime Use

| Runtime State | When Shown | Status Text Example | Progress Binding | Next Route |
|---|---|---|---|---|
| App boot fake load | First screen after app launch. | `INITIALIZING COMMAND NET...` | Timed 0-100 over minimum 2 seconds plus required config/save readiness. | `SCN-02 Main Menu` |
| Match loading | After Deploy/Start before the 3D match appears. | `LOADING OPERATION MAP...` | Async scene/session/map load progress. | `SCN-08 RTS Battle HUD` |

## Layering Rules For Implementation

- Do not flatten the target-lock into Unity.
- Use the background art, logo, frame, loading bar frame, loading fill, status text, percent text, tip text, and bottom status rail as separate Unity children.
- Status text, percent, tip, and progress value must be live runtime bindings.
- The visual must remain neutral enough to support both app boot and pre-match loading.
- No buttons are shown by default; optional Continue/Skip is hidden unless loading is complete and the design explicitly enables it.

## Design Source

- `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md`
- `Design/WarlineCapture_UIUX_Implementation_Detailed_Spec.md`
- `Design/VisualLockLayered/SCN-02_MainMenu/README.md`
- `Design/VisualLockLayered/README.md`

## Target Prompt Summary

The target asks for a AAA mobile RTS loading screen with:

- WarlineCapture brand lockup
- forward command-base background overlooking the 3D operation area
- exact live loading status, progress bar, percent, and tip text
- dark graphite/olive metal UI with gold tactical accents
- no old 2D/isometric map language
- no route buttons or menu choices on the loading state
