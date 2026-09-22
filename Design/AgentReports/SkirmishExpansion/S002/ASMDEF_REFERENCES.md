# S002 — Game.Skirmish.Contracts asmdef references

Programmer 1 shadow compile failed with CS0234/CS0246: consumer assemblies
could not see `Game.Skirmish.Contracts` types (`SkirmishObjectiveRoleKind`,
`SkirmishRoleKind`, `SkirmishSizeId`, namespace `Game.Skirmish`) despite
`autoReferenced: true`. Contracts stay in their own assembly.

These existing asmdefs now list `"Game.Skirmish.Contracts"` in `references`:

| Assembly | Why |
|---|---|
| `Game.Components` | `SkirmishExpandedSessionComponents.cs` |
| `Game.Configs` | SK-01 config / compiler / resolved setup types |
| `Game.Runtime` | session/spawn/outcome systems (also hosts `Systems/`) |
| `Game.Composition` | `SkirmishExpandedLaunchProjection.cs` |
| `Game.Editor` | `SkirmishSetupCompilerValidation.cs` |
| `Game.Tests.Editor` | `SkirmishExpandedDefinitionTests.RunFocusedValidation` |
| `Game.UI.Shell.Ecs` | expanded squad card reads `SkirmishPresentedSlot.Role` (`SkirmishRoleKind`) |

Not referenced from Operations, `SaveDataModel`, `MatchSceneView`, or the
localization catalog. `aebf974a` Industrial Basin landing is unchanged.
