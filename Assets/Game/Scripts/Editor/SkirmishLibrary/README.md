# Skirmish library capture tools

Two Editor tools generate the Skirmish battle library art under `Assets/Game/Art/SkirmishLibrary/`.

## Battlefield plates (Edit Mode) — `SkirmishLibraryPreviewCapture`

Renders DB/CC/IB/MP/AP plates (1920x1080, 2x supersample, 8x MSAA, match grade, no HUD/fog).

```
Tools/CI/invoke_unity_macos.sh --timeout 1800 --log /private/tmp/skirmish-library-preview.log -- \
  -quit -executeMethod Game.Editor.SkirmishLibraryPreviewCapture.Run
```
Pass marker: `[SkirmishLibraryPreviewCapture] result=Passed`

## Scenario cards (Play Mode) — `SkirmishLibraryPlayCapture`

Launches every published scenario through the shipping Skirmish queue (`SkirmishLaunchProjection`,
same path as the S002 harness) with seed 104729, Normal difficulty, Standard resources, fog off.
For each scenario it waits for setup complete (`Phase=Playing`, simulation active, enemy base
entities spawned; 240 s timeout per scenario, logged as `card=<id> result=Failed reason=...`),
freezes time, hides HUD/UI and renders a 1024x576 card (2x supersample, 8x MSAA) framed on the
enemy base and player start. PNGs are imported as uncompressed sprites (no mipmaps).

```
WARLINE_PLAY_CAPTURE_QUIT=1 Tools/CI/invoke_unity_macos.sh --timeout 2400 \
  --log /private/tmp/skirmish-library-play.log -- \
  -executeMethod Game.Editor.SkirmishLibraryPlayCapture.Run
```
Do not pass `-quit` (Play Mode is asynchronous; the tool exits the Editor itself when
`WARLINE_PLAY_CAPTURE_QUIT=1`). Never pass `-batchmode` on macOS.

Pass marker: `[SkirmishLibraryPlayCapture] result=Passed cards=S001,...`
Per card: `[SkirmishLibraryPlayCapture] card=<id> enemyBase=... playerStart=... output=...`
