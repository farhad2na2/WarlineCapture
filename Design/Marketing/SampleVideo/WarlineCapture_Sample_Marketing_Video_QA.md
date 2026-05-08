# WarlineCapture Sample Marketing Video QA

- Video: `Design/Marketing/SampleVideo/WarlineCapture_Sample_Marketing_Video.mp4`
- Preview: `Design/Marketing/SampleVideo/WarlineCapture_Sample_Marketing_Video_Preview.png`
- Manifest: `Design/Marketing/SampleVideo/WarlineCapture_Sample_Marketing_Video_Manifest.json`
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

- City Command: `Design/VisualReferences/2DIsometricProduction/ISO-01_CityCommand_Target/ISO-01_CityCommand_ProductionTarget.png`
- Battle HUD: `Design/VisualLock/SCN-08_RTSBattleHUD/SCN-08_RTSBattleHUD_Landscape_Target.png`
- Operation Dashboard: `Design/VisualLock/SCN-11_OperationDashboard/SCN-11_OperationDashboard_Landscape_Target.png`
- Commander Store: `generated:fair_store_panel`
- Mission Result: `Design/VisualLock/POP-05_MissionResult/POP-05_MissionResult_Landscape_Target.png`

## Next AI Swap-In Points

- Replace one or more `sourceImages` with generated Firefly/Sora/Luma shots.
- Keep the same scene names, copy, banned-term checks, duration, and economy-safe claims.
- Re-run this script before human validation.
