# APH-708 UI Shell Gateway Decomposition

Date: 2026-07-12
Status: Complete

## Result

`UiShellEcsGateway` remains the only public registered implementation of `IUiShellRuntimeGateway` and `IUiAssistantPanelStateGateway`. Its public static API, explicit interface implementation, runtime registration attribute, source GUID, namespace, contracts, and all view call sites remain unchanged.

The former 2,802-line gateway is now a 252-line facade backed by private adapters:

- `UiShellRouteAdapter` for route requests, presentation command consumption, and transition completion;
- `UiShellActionAdapter` for typed UI actions, assistant intents, and queued loading progress;
- `UiShellSettingsAdapter` for assistant-panel and armory presentation state;
- `UiShellReadModelAdapter`, split into bounded partial files for shell, selection, command/header, assistant, minimap/build economy, presentation mapping, and default ECS state.

All new production files are below 500 lines; the largest is the 487-line command/header read-model adapter. No adapter is public, no adapter is referenced by a view, no new `SystemBase`, `*SystemHelper`, broad application shell, asmdef edge, prefab change, or serialized MonoBehaviour was introduced.

## Integrated Corrections

- Empty authoritative usable-fuel summaries now produce `0` rather than leaking the mock header default `2,860`; live usable storage still takes precedence and Oil teaching visibility remains driven by active economy summaries.
- The versioned header test reacquires its dynamic buffer after structural initialization, matching Entities safety rules.
- The UI shell content contract now reflects the established HUD behavior: Hold and Stop remain pressable when unavailable so their rejection feedback can be shown; the command-wheel Stop control still reflects capability directly.
- The stale broad-shell name `NarrativeSequencePresenter` was renamed to `NarrativeSequencePresentation` with its source GUID preserved. Narrative presentation behavior is unchanged.

## Validation

| Gate | Result | Evidence |
|---|---|---|
| UI shell content/loading | Passed `11/11` | `/private/tmp/warline-aph708-content-r3.log` |
| Assistant command gateway | Passed `6/6` | `/private/tmp/warline-aph708-assistant-command.log` |
| Assistant panel cache/allocation | Passed `2/2` | `/private/tmp/warline-aph708-assistant-panel.log` |
| Resource header cache/allocation | Passed `8/8` | `/private/tmp/warline-aph708-header-r2.xml` |
| Resource exchange header routing | Passed `5/5` | `/private/tmp/warline-aph708-resource-routing.log` |
| Menu to Match to Menu lifecycle | Passed `1/1` | `/private/tmp/warline-aph708-lifecycle.xml` |
| Narrative presentation | Passed `6/6` | `/private/tmp/warline-aph708-narrative.log` |
| Production source growth | Passed `15/15` | `/private/tmp/warline-aph708-growth-final.log` |
| Assembly boundaries | Passed `31/31` | `/private/tmp/warline-aph708-boundary.log` |
| Broad-shell names | Passed `1/1` | `/private/tmp/warline-aph708-broad-r2.log` |
| ECS/Burst architecture | Passed `10/10` | `/private/tmp/warline-aph708-burst.log` |
| Dependency report | Passed; 19 assemblies and 82 first-party edges | `/private/tmp/warline-aph708-aph700.log` |
| UI Shell ECS build | Passed with zero errors | `dotnet build Game.UI.Shell.Ecs.csproj --no-restore` |
| Editor/PlayMode test builds | Passed with zero errors | `dotnet build Game.Tests.Editor.csproj` and `Game.Tests.PlayMode.csproj` |
| Diff hygiene | Passed | `git diff --check` |

The regenerated APH-700 report now covers 1,229 owned source files, 2,573 declared types, and 30,542 resolved cross-domain type occurrences with zero unowned source files.

## Residual Risk

The adapters remain in one assembly and share the existing static cache fields to preserve exact lifecycle and zero-allocation behavior. Replacing `World.DefaultGameObjectInjectionWorld`, moving lazy default-state creation out of reads, and addressing the separate steady-state UI presentation allocation belong to APH-710 and APH-711. APH-708 does not claim a Match FPS or GC improvement.
