# Operation Map Identity Config Slice

Date: 2026-07-16
Status: Passed
Tracker: `../Architecture/operation_map_scene_split_and_generator_tracker.md`

## Implemented

- Added sealed `Game.Configs.OperationMapDefinition` with bounded operation-map identity, schema version, content version, and content hash fields.
- Added sealed `Game.Configs.ScenarioSetupConfig` with bounded scenario-to-operation-map identity.
- Added allocation-free canonical id parsing/validation in `OperationMapIdentityRules`.
- Added focused positive, negative, config-validation, and no-`Update` tests.

The types contain no scene path, hierarchy name, Unity object reference, Addressables reference, rendering type, loader behavior, asset search, update callback, ECS system, or runtime policy.

## Validation

| Validation | Result | Evidence |
|---|---|---|
| `Game.Configs.csproj` compile | Passed, zero errors | Local `dotnet build`; existing dependency warnings only. |
| `Game.Tests.Editor.csproj` compile | Passed | `/private/tmp/opmap-identity-tests-dotnet.log` |
| Focused identity EditMode tests | `23 / 23` passed | `/private/tmp/opmap-identity-focused.xml` |
| Non-ECS naming architecture | `9 / 9` passed | `/private/tmp/opmap-identity-gates.xml` |
| Non-UI SystemBase migration | `19 / 19` passed | `/private/tmp/opmap-identity-gates.xml` |
| Production source growth | `15 / 15` passed | `/private/tmp/opmap-identity-source-growth.xml` |
| `git diff --check` | Passed | Local worktree validation. |

The broader `ScriptArchitectureAlignmentContractTests` diagnostic still reports seven pre-existing unrelated failures. Neither new config path is named by those failures, so that suite is retained as diagnostic evidence rather than acceptance for this slice.

Unity imports recreated untracked `Assets/AddressableAssetsData/Android.meta`; it was removed after validation and is not part of the change.
