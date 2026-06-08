# WarlineCapture Sample Marketing Video QA

- Video: `Design/Marketing/SampleVideo/Sample_Marketing_Video.mp4`
- Preview: `Design/Marketing/SampleVideo/Sample_Marketing_Video_Preview.png`
- Manifest: `Design/Marketing/SampleVideo/Sample_Marketing_Video_Manifest.json`
- Runtime: 20.0s at 24 fps
- Size: 1920x1080
- File size: 40435302 bytes

## Checks

- [x] MP4 exists
- [x] File is non-empty
- [x] Preview contact sheet exists
- [x] Resolution is 1920x1080
- [x] Duration is 20 seconds
- [x] No blank sampled frames
- [x] No banned economy/monetization terms

## Scenes

- City Command: active replacement should use a 3D command-base or operation-map capture from `Design/VisualLockLayered/SCN-02_MainMenu/reference/` or a gameplay capture package.
- Battle HUD: `Design/VisualLockLayered/SCN-08_RTSBattleHUD/reference/SCN-08_RTSBattleHUD_Landscape_Target.png`
- Operation Dashboard: `Design/VisualLockLayered/SCN-11_OperationsDashboard/reference/SCN-11_OperationsDashboard_Landscape_Target.png`
- Commander Store: `generated:fair_store_panel`
- Mission Result: `Design/VisualLockLayered/POP-05_MissionResult/reference/POP-05_MissionResult_Landscape_Target.png`

The current MP4 was produced from legacy visual references and should be treated as an archived sample until these active 3D-direction sources are regenerated.

## Next AI Swap-In Points

- Replace one or more `sourceImages` with generated Firefly/Sora/Luma shots.
- Keep the same scene names, copy, banned-term checks, duration, and economy-safe claims.
- Re-run this script before human validation.
