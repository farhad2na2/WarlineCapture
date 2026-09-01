# SCN-14 Store / Command Exchange V3 Work In Progress

Status: canonical target audited and V3 source implementation staged. Unity
prefab generation, focused validation, and exact-size Play Mode comparisons are
pending because the macOS login session is locked and no Unity Pipeline Editor
is connected.

## Canonical Target Lock

`reference/SCN-14_StoreV3_Target.png`

No legacy image is being presented as a new iteration. There was no dedicated
SCN-14 Canvas prefab or valid runtime capture before this pass; the Store route
had no target-quality body to mount.

## Staged V3 Source

- `Assets/Game/Scripts/Editor/StoreCommandExchangeV3PrefabBuilder.cs`
- `Assets/Game/Scripts/UI/Screens/StoreCommandExchangeV3View.cs`
- `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs`
- `Assets/Game/Scripts/UI/Shell/MenuOverlayRoutePresentation.cs`
- `Assets/Tests/Editor/StoreCommandExchangeV3PrefabTests.cs`
- exact 1920x1080 and 4800x2160 capture entry points in
  `Assets/Game/Scripts/Editor/CanvasMenuFallbackValidation.cs`

The 1672x941 composition implements the target header, six-category navigation
rail, responsive 2x2 offer catalog, selected-offer detail, eligibility notice,
Back action, and Purchase action. The offer grid and center header/footer space
expand at 20:9 while resources, Settings, Close, detail, and Purchase remain
pinned to the right edge.

All visible chrome uses directional procedural gradients. Every visible stroke
uses the same 3 px border width; internal art masks add no overlapping frame.
Offer and detail art use masked `AspectRatioFitter.EnvelopeParent` crops and do
not stretch.

The screen reuses the shared V3 Store/Operations/resource icons, the shared
ARIA and Ranger imagery, and existing Armory depot/helicopter art. No
screen-local duplicate raster was created.

Category and offer selection are functional and update all visible catalog
copy, price, details, and art. Purchase intentionally remains disabled: project
monetization requirements prohibit enabling it until wallet, receipt, catalog,
profile persistence, and reward-grant services exist. The UI states this
designed-unavailable condition instead of simulating a purchase.

Offline Roslyn audits pass for the full updated UI runtime, isolated Store
builder, and focused Store tests. This is compile evidence, not a substitute for
Unity generation or live visual validation.

## Pending Commands

Run only through `Tools/CI/invoke_unity_macos.sh` after the login session is
unlocked:

```text
-quit -executeMethod Game.Editor.StoreCommandExchangeV3PrefabBuilder.Build
-quit -executeMethod StoreCommandExchangeV3PrefabTests.RunFocusedValidation
-executeMethod Game.Editor.CanvasMenuFallbackValidation.CaptureStore1920x1080
-executeMethod Game.Editor.CanvasMenuFallbackValidation.CaptureStore4800x2160
```

Do not create an immutable iteration until both exact live captures are posted,
compared against the target, and corrected for every visible mismatch.
