# PM Finding: Commander Identity Screen Gap

Date: 2026-05-07

## Decision

Needs UI/Support task.

## Finding

The screen where players choose their commander name, icon, and frame is not implemented as a Unity popup/screen yet.

The implemented surface is `SCN-03 Commander Profile`:

- `Assets/Game/Prefabs/UI/Screens/Screen_CommanderProfile.prefab`
- `Assets/Game/Scripts/UI/Screens/CommanderProfileScreenController.cs`

That screen displays profile/progression data. It is not the first-run/edit identity chooser.

The intended chooser exists as a visual target only:

- `Design/VisualLock/POP-11_CommanderIdentity/POP-11_CommanderIdentity_Landscape_Target.png`
- `Design/VisualLock/POP-11_CommanderIdentity/POP-11_CommanderIdentity_CleanLandscape_Notes.md`
- `Design/VisualLock/POP-11_CommanderIdentity/POP-11_CommanderIdentity_Target_State_Manifest.json`

Required controls listed in the design target:

- `CommanderNameInput`
- `CommanderIconGrid`
- `FrameGrid`
- `ConfirmButton`
- `CancelButton`

## Why It Matters

The Main Menu and Commander Profile can show commander identity, but there is no accepted player-facing flow for choosing or editing that identity. Agents may assume `Screen_CommanderProfile` covers this, but it does not.

## Recommended Task

After the current UI `PREFAB-04_AssistantButton` target-lock task, assign UI to implement `POP-11_CommanderIdentity` as a modal over `Screen_CommanderProfile` or first-run profile setup.

Support/FTUE should define the first-run trigger, default fallback identity, validation rules, and save fields for commander name/icon/frame.

## Cross-Lane Notice

- UI owns the popup/prefab and visual validation.
- Support/FTUE owns first-run flow, save/session fields, name validation, and default identity behavior.
- QA/HCI should validate that the player can understand, edit, confirm, cancel, and later revisit identity settings.
