# APH-509 Package Usage Inventory

Date: 2026-07-10

Workspace: `/Users/farhad/Projects/WarlineCapture`

Scope: static, read-only prerequisite proof; no package removal, manifest edit, Unity launch, compile, import, test, or build.

## Result

This inventory is intentionally fail-safe. `candidate-unused` means only that the searches below found no first-party code, assembly, searchable serialized-GUID, lock reverse-dependency, or documented-workflow evidence. It is not removal approval.

- `Packages/manifest.json` declares 46 packages.
- `Packages/packages-lock.json` resolves 67 packages: 46 manifest declarations, one manifest-absent embedded depth-zero direct package-state discrepancy, and 20 ordinary lock-only transitive entries.
- The project has strong runtime proof for Entities, Entities Graphics, Input System, URP, uGUI, Burst, Collections, Mathematics, Render Pipelines Core, audio, JSON serialization, and `com.sniveler-code.gpu-animation`.
- ProBuilder, Shader Graph, the Test Framework, image conversion, Vector Graphics, and the Timeline-adjacent Playables tool have editor/build/workflow evidence. The Playables API evidence does **not** prove the Timeline package itself.
- 16 direct declarations are `candidate-unused`; five more are `unproven`. None may be removed without the validation ladder below.
- `com.unity.modules.vectorgraphics` has deterministic importer-workflow proof: all 276 tracked SVG assets under `Assets/` have tracked `.svg.meta` files with `ScriptedImporter` and `svgType` settings.
- `com.sniveler-code.gpu-animation` is an embedded, depth-zero direct package-state discrepancy with extensive runtime evidence, but it is absent from `manifest.json`. It is not ordinary parent-controlled transitive content and must not be removed or expected to disappear through parent cleanup.

## Classification Rules

| Classification | Static threshold used here |
|---|---|
| proven runtime dependency | Runtime asmdef/API evidence or strong runtime serialized references, with concrete first-party consumers. |
| editor/workflow dependency | Editor-only assembly/API, authored source asset, test, build, IDE, or documented workflow evidence. |
| transitive/required | Current lock graph requires the package, even if first-party code does not directly consume it. |
| candidate-unused | No positive static evidence and no current reverse lock dependency. Removal still requires all applicable validation. |
| unproven | Static analysis cannot safely resolve likely external-tool or built-in-engine/serialized usage. |

## Direct Manifest Packages

| Package | Version | Classification | Evidence and removal gate |
|---|---:|---|---|
| `com.unity.2d.sprite` | 1.0.0 | transitive/required | Required by AI Assistant in the lock; no independent first-party assembly/API/GUID proof. Validate sprite import/editing after any upstream cleanup. |
| `com.unity.ai.assistant` | 2.13.0-pre.2 | candidate-unused | No first-party API, asmdef, serialized GUID, reverse dependency, or workflow hit. Validate any team AI Assistant workflow and a clean editor import. |
| `com.unity.collab-proxy` | 2.12.4 | candidate-unused | No first-party/static workflow evidence. Confirm no Unity Version Control/Plastic workflow before isolated removal. |
| `com.unity.entities` | 6.5.0 | proven runtime dependency | 12 first-party asmdefs consume package assemblies; 606 first-party C# files contain Entities/Transforms tokens; four serialized files reference package GUIDs. |
| `com.unity.entities.graphics` | 6.5.0 | proven runtime dependency | Five first-party asmdefs, 28 C# files, runtime `Unity.Rendering`/`Unity.Entities.Graphics` usage. |
| `com.unity.ide.rider` | 3.0.40 | unproven | IDE integration is external/editor-state dependent and leaves little repository evidence. Confirm the selected external editor and Rider project-generation/debugging workflow. |
| `com.unity.ide.visualstudio` | 2.0.27 | unproven | Same external IDE limitation; also currently requires Test Framework. Confirm Visual Studio project generation/debugging use. |
| `com.unity.inputsystem` | 1.19.0 | proven runtime dependency | Three runtime/editor asmdefs, six C# files, `Assets/Game/InputSystem_Actions.inputactions`, four serialized GUID consumers, and `activeInputHandler: 1`. |
| `com.unity.multiplayer.center` | 1.0.1 | candidate-unused | No first-party API, asmdef, GUID, reverse dependency, or specific workflow evidence. Validate multiplayer tooling is not used by the team. |
| `com.unity.probuilder` | 6.1.2 | editor/workflow dependency | `ProjectTools.Editor` references both ProBuilder assemblies; `ProBuilderShapeBakerWindow.cs` uses `ProBuilderMesh`; six serialized consumers include `Assets/Game/Prefabs/Shapes/Pipe.prefab`. |
| `com.unity.render-pipelines.universal` | 17.5.0 | proven runtime dependency | Four first-party asmdefs, six C# files, 444 serialized GUID consumers, and `GraphicsSettings.asset` selects a custom render pipeline. |
| `com.unity.serialization` | 6.5.0 | transitive/required | Required by Entities; no independent first-party API proof. |
| `com.unity.shadergraph` | 17.5.0 | editor/workflow dependency | Required by URP and ProBuilder; authored `.shadergraph`/`.shadersubgraph` files exist and 321 serialized files reference package GUIDs. Required for graph import/build even though player runtime behavior was not tested. |
| `com.unity.test-framework` | 1.7.0 | editor/workflow dependency | 227 first-party test C# files use NUnit/TestTools; documented `-runTests` and focused validation workflows exist. |
| `com.unity.timeline` | 1.8.12 | candidate-unused | No Timeline assembly/API, `.playable` asset, or package GUID hit. `PoseMeshBakerWindow.cs` uses `UnityEngine.Playables`, supplied by the Director module, so that hit does not prove Timeline. |
| `com.unity.ugui` | 2.5.0 | proven runtime dependency | Four first-party asmdefs, 106 C# files, and 395 serialized package-GUID consumers. |
| `com.unity.visualscripting` | 1.9.11 | candidate-unused | No first-party Visual Scripting API, asmdef, serialized GUID, or reverse dependency. Validate graph assets and editor menus after isolated removal. |
| `com.unity.modules.accessibility` | 1.0.0 | candidate-unused | No first-party API or reverse lock dependency. Serialized built-in component usage cannot be conclusively excluded statically. |
| `com.unity.modules.adaptiveperformance` | 1.0.0 | candidate-unused | No first-party API or reverse lock dependency. Run Android thermal/performance smoke validation if removed. |
| `com.unity.modules.ai` | 1.0.0 | candidate-unused | No `UnityEngine.AI` first-party hit or reverse lock dependency. Validate scenes/prefabs for built-in NavMesh components after reimport. |
| `com.unity.modules.androidjni` | 1.0.0 | candidate-unused | No `AndroidJava*`/`AndroidJNI*` first-party hit or reverse lock dependency. Android plugins may use JNI without C# evidence; build/device proof is mandatory. |
| `com.unity.modules.animation` | 1.0.0 | transitive/required | Required by Timeline, Director, and UIElements; project also contains animation assets, whose built-in use is not package-GUID-addressable. |
| `com.unity.modules.assetbundle` | 1.0.0 | transitive/required | Required by Entities, Scriptable Build Pipeline, and UnityWebRequest bundle/WWW packages. |
| `com.unity.modules.audio` | 1.0.0 | proven runtime dependency | 18 first-party C# files use `AudioSource`, `AudioClip`, or `AudioMixer`; also required by seven resolved packages. |
| `com.unity.modules.cloth` | 1.0.0 | unproven | Broad `Cloth` text hits are ambiguous and built-in components use class IDs rather than package GUIDs. Inspect scenes/prefabs in Unity before deciding. |
| `com.unity.modules.director` | 1.0.0 | transitive/required | Required by Timeline; `PoseMeshBakerWindow.cs` directly uses Playables. |
| `com.unity.modules.imageconversion` | 1.0.0 | editor/workflow dependency | Three first-party editor C# files use image conversion/loading; also required by screenshot, vector graphics, and web-request texture packages. |
| `com.unity.modules.imgui` | 1.0.0 | transitive/required | Required by six resolved editor/UI/test packages. |
| `com.unity.modules.jsonserialize` | 1.0.0 | proven runtime dependency | 11 first-party C# files use `JsonUtility`; also required by ten resolved packages. |
| `com.unity.modules.physicscore2d` | 1.0.0 | transitive/required | Required by the resolved Physics2D module. |
| `com.unity.modules.screencapture` | 1.0.0 | candidate-unused | No first-party `ScreenCapture` hit or reverse lock dependency. Validate all visual-proof/capture workflows. |
| `com.unity.modules.tilemap` | 1.0.0 | candidate-unused | No first-party Tilemap API or reverse lock dependency. Inspect scenes/prefabs and import before removal. |
| `com.unity.modules.ui` | 1.0.0 | transitive/required | Required by uGUI, UIElements, and Video. |
| `com.unity.modules.uielements` | 1.0.0 | transitive/required | Required by six resolved packages, including Input System, AI Assistant, Entities, and Multiplayer Center. |
| `com.unity.modules.umbra` | 1.0.0 | unproven | No source/reverse dependency evidence, but occlusion data and built-in renderer behavior are not reliably attributable with GUID scanning. Inspect occlusion settings and player rendering. |
| `com.unity.modules.unityanalytics` | 1.0.0 | transitive/required | Required by Entities. No independent first-party analytics API proof. |
| `com.unity.modules.unitywebrequest` | 1.0.0 | transitive/required | Required by eight resolved packages, including Entities and AI Assistant. |
| `com.unity.modules.unitywebrequestassetbundle` | 1.0.0 | transitive/required | Required by the resolved WWW module. |
| `com.unity.modules.unitywebrequestaudio` | 1.0.0 | transitive/required | Required by the resolved WWW module. |
| `com.unity.modules.unitywebrequesttexture` | 1.0.0 | candidate-unused | No first-party API hit or reverse lock dependency. Validate any remote-image loading path. |
| `com.unity.modules.unitywebrequestwww` | 1.0.0 | candidate-unused | No first-party API hit or reverse lock dependency. Validate legacy networking/content workflows. |
| `com.unity.modules.vectorgraphics` | 1.0.0 | editor/workflow dependency | Exactly 276 tracked SVG assets under `Assets/` have tracked importer metas containing both `ScriptedImporter` and `svgType`. This is direct SVG import/reimport workflow proof even without first-party C# API or package-GUID hits. |
| `com.unity.modules.vehicles` | 1.0.0 | candidate-unused | No first-party `WheelCollider` hit or reverse dependency. Inspect vehicle prefabs for built-in wheel components. |
| `com.unity.modules.video` | 1.0.0 | candidate-unused | No first-party Video API or reverse dependency. Inspect scenes/prefabs for `VideoPlayer`. |
| `com.unity.modules.wind` | 1.0.0 | unproven | No source/reverse evidence; built-in `WindZone` and shader-side use are not package-GUID-addressable. Inspect scenes and foliage shaders. |
| `com.unity.modules.xr` | 1.0.0 | candidate-unused | No first-party XR API or reverse dependency. Confirm XR is disabled in editor/player settings and run a player build. |

## Embedded Direct Package-State Discrepancy

This package is not an ordinary lock-only transitive. Its lock entry has depth zero and source `embedded`, its package root exists directly under `Packages/`, and first-party runtime content depends on it, but `manifest.json` does not declare it. Preserve the embedded directory and reconcile the direct package state in Unity Package Manager before any cleanup experiment. Do not infer that changing a parent package will remove it.

| Package | Resolved version/depth | Classification | Current proof |
|---|---:|---|---|
| `com.sniveler-code.gpu-animation` | `file:com.sniveler-code.gpu-animation`, d0 embedded | proven runtime dependency | Four first-party asmdefs, 17 C# files, and 137 serialized GUID consumers. It is direct embedded package-state drift, not parent-controlled transitive content. |

## Ordinary Lock-Only Transitive Packages

These 20 packages are not independent manifest-removal targets. An ordinary lock-only transitive can disappear only after its direct parent graph changes and Unity resolves a new lockfile.

| Package | Resolved version/depth | Classification | Current proof |
|---|---:|---|---|
| `com.unity.burst` | 1.8.29, d1 | proven runtime dependency | Two runtime asmdefs, 67 first-party C# files, many `[BurstCompile]` sites, and Android build tooling that controls Burst AOT. |
| `com.unity.collections` | 6.5.0, d1 | proven runtime dependency | 13 first-party asmdefs and 368 C# files; required by Entities, Serialization, and Render Pipelines Core. |
| `com.unity.mathematics` | 1.4.0, d1 | proven runtime dependency | 11 first-party asmdefs and 410 C# files; required by Burst and AI Assistant. |
| `com.unity.render-pipelines.core` | 17.5.0, d1 | proven runtime dependency | Three first-party asmdefs, 49 C# files, 20 serialized GUID consumers; required by URP, Shader Graph, and URP Config. |
| `com.unity.ext.nunit` | 2.1.0, d1 | transitive/required | Required by Test Framework and Rider integration. |
| `com.unity.nuget.mono-cecil` | 1.11.6, d1 | transitive/required | Required by AI Assistant, Collections, and Entities. |
| `com.unity.nuget.newtonsoft-json` | 3.2.2, d1 | transitive/required | Required by AI Assistant. |
| `com.unity.profiling.core` | 1.0.3, d1 | transitive/required | Required by Entities. |
| `com.unity.render-pipelines.universal-config` | 17.5.0, d1 | transitive/required | Required by URP. |
| `com.unity.scriptablebuildpipeline` | 2.6.1, d1 | transitive/required | Required by Entities. |
| `com.unity.searcher` | 4.9.4, d1 | transitive/required | Required by Shader Graph. |
| `com.unity.settings-manager` | 2.1.1, d1 | transitive/required | Required by ProBuilder. |
| `com.unity.test-framework.performance` | 3.5.0, d1 | transitive/required | Required by Collections and Entities. |
| `com.unity.modules.hierarchy` | 1.0.0, d1 | transitive/required | Required by Entities. |
| `com.unity.modules.hierarchycore` | 1.0.0, d1 | transitive/required | Required by Hierarchy and UIElements. |
| `com.unity.modules.particlesystem` | 1.0.0, d1 | transitive/required | Required by Entities Graphics and Timeline. |
| `com.unity.modules.physics` | 1.0.0, d1 | transitive/required | Required by Entities, ProBuilder, uGUI, Cloth, UIElements, Vehicles, and XR. |
| `com.unity.modules.physics2d` | 1.0.0, d1 | transitive/required | Required by uGUI and Tilemap. |
| `com.unity.modules.subsystems` | 1.0.0, d1 | transitive/required | Required by Adaptive Performance and XR. |
| `com.unity.modules.terrain` | 1.0.0, d2 | transitive/required | Required by Render Pipelines Core. |

## Reproducible Commands

Run from `/Users/farhad/Projects/WarlineCapture`:

```sh
python3 Tools/CI/aph509_package_usage_inventory.py

jq -r '.dependencies | to_entries[] | [.key,.value] | @tsv' Packages/manifest.json
jq -r '.dependencies | to_entries[] | [.key,.value.version,(.value.depth|tostring),.value.source,((.value.dependencies // {})|keys|join(","))] | @tsv' Packages/packages-lock.json
jq -r --slurp '.[0].dependencies as $m | .[1].dependencies | to_entries[] | select($m[.key] == null) | [.key,.value.version,(.value.depth|tostring),.value.source] | @tsv' Packages/manifest.json Packages/packages-lock.json

find Assets/Game/Scripts Assets/Tests Assets/Editor -name '*.asmdef' -type f -print | sort
rg -n --glob '*.cs' 'Unity\.Entities|Unity\.Rendering|Unity\.Burst|Unity\.Collections|Unity\.Mathematics|UnityEngine\.InputSystem|UnityEngine\.Rendering\.Universal|UnityEngine\.UI|NUnit\.Framework|SnivelerCode\.GpuAnimation|UnityEngine\.ProBuilder|UnityEngine\.Playables|UnityEngine\.Timeline|Unity\.VisualScripting' Assets/Game/Scripts Assets/Tests Assets/Editor
find Assets -type f \( -name '*.shadergraph' -o -name '*.shadersubgraph' -o -name '*.playable' -o -name '*.inputactions' \) -print | sort
git ls-files -z -- 'Assets/**/*.svg' | tr -cd '\0' | wc -c
git ls-files -- 'Assets/**/*.svg' | while IFS= read -r svg; do meta="$svg.meta"; if test -f "$meta" && rg -q '^ScriptedImporter:' "$meta" && rg -q '^  svgType:' "$meta"; then printf '%s\n' "$svg"; fi; done | wc -l
rg -n 'activeInputHandler|m_CustomRenderPipeline' ProjectSettings
rg -n -i --glob '*.md' --glob '*.sh' --glob '*.command' --glob '*.yml' --glob '*.yaml' 'runTests|testPlatform|executeMethod|BuildScript|ProBuilder|Package Manager|Input System|Shader Graph|Timeline' README.md Design Tools .github

git diff --check -- Design/AgentReports/2026-07-10_aph-509_package_usage_inventory.md Tools/CI/aph509_package_usage_inventory.py
git status --short -- Design/AgentReports/2026-07-10_aph-509_package_usage_inventory.md Tools/CI/aph509_package_usage_inventory.py
```

The script uses only Python's standard library plus read-only `git ls-files`. It reads the manifest, lockfile, package metadata/asmdefs/metas, first-party asmdefs/C#, searchable serialized assets, tracked SVG importer metas, and workflow text. It writes nothing. Its `package_state` column distinguishes `embedded-depth-zero-manifest-absent` from `lock-only-transitive`, and `importer_assets` reports the deterministic SVG workflow count.

## Evidence Limitations

- Unity was explicitly not run. Static evidence cannot prove package removal compiles, imports, builds, or runs.
- The working tree already contained unrelated modifications, including build/editor code. They were read as current-worktree evidence and were not modified or reverted.
- Package GUID correlation works only for text-serialized assets and package files with `.meta` GUIDs. Binary assets, built-in class IDs, dynamically loaded resources, reflection, native plugins, shader includes, generated code, and external services/tools can evade it.
- The Vector Graphics count is limited to Git-tracked `Assets/**/*.svg` files whose tracked adjacent metas contain both `ScriptedImporter` and `svgType`. It proves an editor import/reimport workflow, not player-runtime API use.
- Assembly references prove compile coupling but do not prove every declared reference is semantically used. Conversely, predefined assemblies can consume APIs without asmdef references.
- Namespace/token hits can be false positives. The report only elevates them when names are package-specific or corroborated by assembly/serialized/build evidence.
- Documentation may be stale. Historical reports/logs were not treated as primary usage proof; current code, asmdefs, settings, and serialized references have priority.
- Lock reverse dependencies describe the current resolution, not the minimal valid graph. Unity must regenerate the lock after any manifest experiment.
- Built-in modules are especially difficult to disprove because serialized engine components use class IDs rather than package-owned script GUIDs.

## Validation Required Before Any Removal

1. Use an isolated clean worktree/branch and remove exactly one manifest-declared candidate at a time. Do not hand-edit ordinary lock-only transitive entries; let Unity Package Manager resolve `packages-lock.json`.
2. Before any cleanup, reconcile why the embedded GPU animation package is direct/depth-zero in the lock and present under `Packages/` but absent from the manifest. Preserve it and establish an explicit, reproducible direct declaration/state; do not process it as a transitive-removal candidate.
3. Start Unity with a disposable/clean `Library`, complete package resolution and full asset import, and require zero compile/import errors and no missing scripts/shaders.
4. Open and resave representative Menu/Match/build scenes plus prefabs relevant to the package; inspect Console, missing components, materials, input actions, animation, occlusion, and platform settings.
5. Run the full EditMode and PlayMode test suites, not only focused tests. Exercise editor tools for ProBuilder, Shader Graph, SVG import/reimport and rendered output, capture/image conversion, IDE integration, and version-control services where applicable.
6. Produce the release-equivalent Android build through `BuildScript.BuildAndroid`; compare build report, stripping/linker warnings, shaders, managed assemblies, package list, artifact size, and startup logs.
7. Run device smoke coverage for startup, menu/HUD input, ECS rendering/animation, audio, networking/content loading, thermals, and any feature associated with the candidate. Check player logs for missing types, shaders, native libraries, or subsystems.
8. Retain a package when any validation is ambiguous. A removal decision needs positive compile/import/test/build/device proof, reviewer sign-off, and a separately reviewed manifest/lock change.

## Validation Performed for This Proof

- Read-only inventory script: run successfully.
- Static manifest/lock/asmdef/source/serialized/settings/workflow searches: run successfully.
- Unity: not run, by task constraint.
- Package removal: not attempted.
- Git commit: not created.
