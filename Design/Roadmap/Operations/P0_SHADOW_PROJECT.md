# Operations shadow project

Admin rule, 2026-09-21. Operations Unity work uses a **shadow worktree**, not the shared Windows checkout.

| Role | Path | Allowed use |
|---|---|---|
| Shared / Programmer 1 / other tracks | `D:\Projects\WarlineCapture` | Leave it alone. Do not open it, lock `Library`, or wait on its Editor. |
| Operations shadow (this package) | `D:\Projects\WarlineCapture-Operations` | Git worktree of the same repo on the Operations branch. Programmer 2 validates here later. |

This Cursor VM continues P0 on `cursor/operations-p0-b466`. It does not open either Windows path. Programmer 2 later uses the shadow worktree only.

## Create or refresh the shadow worktree

Run from a shell that can see the shared **git** checkout. The script reads git metadata only and does not launch Unity.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Ensure-OperationsShadowWorktree.ps1
```

That command:

- fetches `cursor/operations-p0-b466`
- adds or refreshes `D:\Projects\WarlineCapture-Operations`
- leaves `D:\Projects\WarlineCapture\Library` untouched
- tells Programmer 2 to validate only the shadow project

## P0 Unity validation (shadow only)

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsP0Validation.ps1
```

The helper hard-codes `-ProjectPath D:\Projects\WarlineCapture-Operations`, resolves the Editor from that project's `ProjectVersion.txt`, and calls the checked `InvokeUnityExecuteMethodValidation.ps1` wrapper with:

- `-ExecuteMethod Game.Tests.Editor.Operations.OperationsP0Validation.RunFocusedValidation`
- `-RequiredPassMarker [OperationsP0Validation] result=Passed checks=13`
- `-GuiLicensing` by default

If Unity is accidentally opened on the shared checkout, `OperationsP0Checks.ShadowProjectIsIsolatedFromSharedCheckout` fails closed.

## Still forbidden

Do not edit Skirmish Expansion trees, `SaveDataModel`, `MatchSceneView`, existing asmdefs, or the localization catalog. Shared seams stay Game PM blockers in [P0_SHARED_SEAMS.md](P0_SHARED_SEAMS.md).
