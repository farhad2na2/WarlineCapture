#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace Game.Editor
{
    // Read-only census plus explicit final-exit package-owner cleanup and focused controls.
    public static class SkirmishProductionBatchOwnershipCensus
    {
        public static JArray CaptureCompiledCanaryIdentities()
        {
            var catalog=SkirmishExpansionCatalogConfig.Load();var output=new JArray();
            if(catalog==null||!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error))
                throw new InvalidOperationException("Actual packaged catalog/matrix required");
            string previousLocale=GameLocalization.CurrentLocaleCode;
            try
            {
                if(!GameLocalization.SetLocale("en",false)||GameLocalization.CurrentLocaleCode!="en")
                    throw new InvalidOperationException("Native EN locale did not resolve");
                foreach(string id in new[]{"S004","S005"})
                {
                    string path="Assets/Game/Configs/SkirmishExpansion/Scenarios/"+id+"/SkirmishScenario_"+id+".asset";
                    var actual=UnityEditor.AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(path);
                    if(actual==null||!catalog.TryGetDefinition(id,out var definition)||actual!=definition)
                        throw new InvalidOperationException("Exact packaged canary registry/asset join required");
                    int seed=id=="S004"?155925:155926;
                    var manifest=new SkirmishContentManifest { RequiredFeatureIds=definition.RequiredFeatureIds };
                    if(!SkirmishSetupCompiler.TryCompile(definition,SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,
                        seed,manifest,matrix,out var setup,out var reasons))
                        throw new InvalidOperationException("Actual native canary setup compile failed: "+string.Join(";",reasons));
                    output.Add(new JObject { ["schema_version"]=1,["collector_kind"]="SkirmishNativeCompiledSetupCensus",
                        ["native_setup_identity"]=SkirmishProductionCanaryRequest.CreateNativeSetupIdentity(setup.CatalogId,setup.DefinitionId,
                            setup.ContentVersion,setup.DifficultyId.ToString(),setup.SizeId.ToString(),setup.Seed,
                            GameLocalization.CurrentLocaleCode,setup.SetupHash,setup.ContentHash),
                        ["actual_native_locale"]=GameLocalization.CurrentLocaleCode,
                        ["native_config_sha256"]=SkirmishAcceptanceCensusCapture.ComputeConfigHash(setup) });
                }
            }
            finally
            {
                if(!GameLocalization.SetLocale(previousLocale,false)||GameLocalization.CurrentLocaleCode!=previousLocale)
                    throw new InvalidOperationException("Native batch census locale restoration failed");
            }
            return output;
        }

        public static JObject Capture()
        {
            var scenes = new Dictionary<string, Scene>(StringComparer.Ordinal);
            for (int i = 0; i < SceneManager.sceneCount; i++)
            { var scene = SceneManager.GetSceneAt(i); if (scene.IsValid()) scenes[Handle(scene)] = scene; }
            foreach (var root in Resources.FindObjectsOfTypeAll<GameObject>())
                if (root != null && root.scene.IsValid()) scenes[Handle(root.scene)] = root.scene;
            var package = new JArray();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("Unity.Entities.CompanionGameObjectUtility", false);
                if (type == null) continue;
                foreach (string name in new[] { "_companionScene", "_companionSceneLiveConversion" })
                {
                    var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
                    if (field == null || field.FieldType != typeof(Scene))
                        throw new InvalidOperationException("Installed companion owner field changed; census cannot infer ownership");
                    var scene = (Scene)field.GetValue(null);
                    package.Add(new JObject { ["assembly"] = assembly.GetName().Name, ["owner_type"] = type.FullName,
                        ["owner_field"] = name, ["valid"] = scene.IsValid(), ["handle"] = Handle(scene) });
                    if (scene.IsValid()) scenes[Handle(scene)] = scene;
                }
            }
            var allScenes = new JArray(scenes.Values.OrderBy(Handle, StringComparer.Ordinal).Select(SceneRow));
            int measuredPreviews = scenes.Values.Count(EditorSceneManager.IsPreviewScene);
            var worlds = new JArray();
            var liveWorlds=new List<World>();foreach(var candidate in World.All)if(candidate.IsCreated)liveWorlds.Add(candidate);
            foreach (var world in liveWorlds.OrderBy(w => w.SequenceNumber))
            {
                var em = world.EntityManager;
                using var sessions = em.CreateEntityQuery(new EntityQueryDesc {
                    Any = new[] { ComponentType.ReadOnly<SkirmishExpandedSessionComponent>(), ComponentType.ReadOnly<SkirmishMatchState>() },
                    Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IgnoreComponentEnabledState });
                using var owners = em.CreateEntityQuery(new EntityQueryDesc {
                    All = new[] { ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>() },
                    Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IgnoreComponentEnabledState });
                using var setup = em.CreateEntityQuery(typeof(SkirmishResolvedSetupRecord));
                using var definitions = em.CreateEntityQuery(typeof(SkirmishLaunchDefinitionRecord));
                using var bindings = em.CreateEntityQuery(typeof(SkirmishNativeMissionLogicalBindingRecord));
                worlds.Add(new JObject { ["sequence"] = world.SequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["name"] = world.Name, ["flags"] = world.Flags.ToString(), ["native_sessions"] = sessions.CalculateEntityCount(),
                    ["owned_actors"] = owners.CalculateEntityCount(), ["setup_records"] = setup.CalculateEntityCount(),
                    ["launch_definition_records"] = definitions.CalculateEntityCount(), ["logical_provider_records"] = bindings.CalculateEntityCount() });
            }
            return new JObject { ["schema_version"] = 1, ["scenes"] = allScenes, ["worlds"] = worlds,
                ["input_devices"] = new JArray(InputSystem.devices.OrderBy(d=>d.deviceId).Select(d=>new JObject {
                    ["device_id"]=d.deviceId,["layout"]=d.layout,["name"]=d.name,["native"]=d.native,["enabled"]=d.enabled,["added"]=d.added })),
                ["actual_preview_count"] = EditorSceneManager.previewSceneCount, ["measured_preview_count"] = measuredPreviews,
                ["preview_census_complete"] = measuredPreviews == EditorSceneManager.previewSceneCount,
                ["companion_package_owner_fields"] = package };
        }

        static JObject SceneRow(Scene scene)
        {
            var roots = scene.isLoaded ? scene.GetRootGameObjects() : Array.Empty<GameObject>();
            return new JObject { ["handle"] = Handle(scene), ["name"] = scene.name, ["path"] = scene.path,
                ["loaded"] = scene.isLoaded, ["preview"] = EditorSceneManager.IsPreviewScene(scene),
                ["root_count"] = scene.rootCount, ["dirty"] = scene.isDirty,
                ["roots"] = new JArray(roots.OrderBy(r => r.GetEntityId().ToString(), StringComparer.Ordinal).Select(r => new JObject {
                    ["native_entity_id"] = r.GetEntityId().ToString(), ["name"] = r.name, ["hide_flags"] = (int)r.hideFlags,
                    ["active_self"] = r.activeSelf, ["descendant_count"] = r.GetComponentsInChildren<Transform>(true).Length })) };
        }

        public static string CleanupSignature(JObject census)
        {
            if (census["preview_census_complete"]?.Type != JTokenType.Boolean || !(bool)census["preview_census_complete"])
                throw new InvalidOperationException("Unknown empty preview scene ownership; batch cannot continue");
            if((int)census["actual_preview_count"]!=(int)census["measured_preview_count"])
                throw new InvalidOperationException("Actual and measured preview census counts differ");
            foreach (JObject world in (JArray)census["worlds"])
                foreach (string name in new[] { "native_sessions", "owned_actors", "setup_records", "launch_definition_records", "logical_provider_records" })
                    if ((int)world[name] != 0) throw new InvalidOperationException("Native mission ownership remains: " + name);
            // Preview handles and actual root identities must remain exact. Normal
            // Editor scene handles may be regenerated by its legitimate restore.
            var stable = (JObject)census.DeepClone();
            foreach (JObject scene in (JArray)stable["scenes"])
                if (!(bool)scene["preview"])
                {
                    scene.Remove("handle");
                    foreach (JObject root in (JArray)scene["roots"]) root.Remove("native_entity_id");
                    scene["roots"] = new JArray(((JArray)scene["roots"]).OrderBy(r => (string)r["name"], StringComparer.Ordinal));
                }
            stable["scenes"] = new JArray(((JArray)stable["scenes"]).OrderBy(r => (string)r["path"], StringComparer.Ordinal)
                .ThenBy(r => (string)r["name"], StringComparer.Ordinal));
            return SkirmishProductionCanaryRequest.Canonical(stable);
        }
        static bool HasCreatedWorld()
        {
            foreach (var world in World.All) if (world.IsCreated) return true;
            return false;
        }

        // Final owned Editor exit only: per-job baselines retain package caches unchanged.
        public static JObject CloseEmptyPackageCompanionScenesAtFinalExit()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode ||
                UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating ||
                HasCreatedWorld())
                throw new InvalidOperationException("Companion disposal requires idle final exit after every World is disposed");
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("Unity.Entities.CompanionGameObjectUtility", false)).Where(t => t != null).ToArray();
            if (types.Length != 1 || types[0].Assembly.GetName().Name != "Unity.Scenes")
                throw new InvalidOperationException("Exact installed companion owner required");
            var type = types[0];
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(type.Assembly);
            if (package == null || package.name != "com.unity.entities" || package.version != "6.5.0" ||
                SkirmishProductionCanaryRequest.FileHash(System.IO.Path.Combine(package.resolvedPath,
                    "Unity.Scenes", "Hybrid", "CompanionSceneUtility.cs")) !=
                    "a775ad88d466afbebac429d19d40b2c3bac9311e936ab9b3f4d87b6b00dac489")
                throw new InvalidOperationException("Companion cleanup implementation changed; no generic preview fallback");
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
            var fields = new[] { "_companionScene", "_companionSceneLiveConversion" }
                .Select(name => type.GetField(name, flags)).ToArray();
            var dispose = type.GetMethod("AssemblyReloadEventsOnbeforeAssemblyReload", flags, null, Type.EmptyTypes, null);
            if (fields.Any(f => f == null || f.FieldType != typeof(Scene)) ||
                dispose == null || dispose.DeclaringType != type || dispose.ReturnType != typeof(void))
                throw new InvalidOperationException("Exact package field/disposal API required");
            var scenes = fields.Select(f => (Scene)f.GetValue(null)).ToArray();
            if (scenes.All(s => s == default))
                return new JObject { ["closed_count"] = 0, ["already_disposed"] = true, ["worlds_disposed"] = true };
            if (scenes.Any(s => !s.IsValid() || !s.isLoaded || !EditorSceneManager.IsPreviewScene(s) || s.GetRootGameObjects().Length != 0) ||
                Handle(scenes[0]) == Handle(scenes[1]))
                throw new InvalidOperationException("Both exact package scenes must be valid, distinct and empty; foreign scenes cannot substitute");
            var handles = new HashSet<string>(scenes.Select(Handle), StringComparer.Ordinal);
            var before = Capture();
            JArray Foreign(JObject census) => new JArray(((JArray)census["scenes"]).OfType<JObject>()
                .Where(s => !handles.Contains((string)s["handle"])).OrderBy(s => (string)s["handle"], StringComparer.Ordinal));
            var foreignBefore = Foreign(before);
            // Invoke the actual package closer, never close arbitrary census scenes.
            dispose.Invoke(null, null);
            if (scenes.Any(s => s.IsValid()))
                throw new InvalidOperationException("Original package disposal did not close both cached scenes");
            foreach (var field in fields) field.SetValue(null, default(Scene));
            // The package callback is unconditional; remove its stale registration after final disposal.
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -=
                (UnityEditor.AssemblyReloadEvents.AssemblyReloadCallback)Delegate.CreateDelegate(
                    typeof(UnityEditor.AssemblyReloadEvents.AssemblyReloadCallback), dispose);
            var after = Capture();
            if (HasCreatedWorld() || !JToken.DeepEquals(foreignBefore, Foreign(after)) ||
                (int)before["actual_preview_count"] - (int)after["actual_preview_count"] != 2 ||
                fields.Any(f => (Scene)f.GetValue(null) != default))
                throw new InvalidOperationException("Package disposal changed foreign ownership or failed exact two-scene removal");
            return new JObject { ["closed_count"] = 2, ["already_disposed"] = false, ["worlds_disposed"] = true,
                ["owner_type"] = type.FullName, ["closed_handles"] = new JArray(handles.OrderBy(h => h, StringComparer.Ordinal)),
                ["foreign_scenes_unchanged"] = true, ["persistent_allocation_cleanliness_claimed"] = false };
        }

        public static void RunPackageCompanionShutdownControls()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || HasCreatedWorld())
                throw new InvalidOperationException("Fresh idle World-free focused control required");
            var foreign = EditorSceneManager.NewPreviewScene();
            int controls = 0;
            try
            {
                var foreignRoot = new GameObject("OwnedCompanionControlForeignRoot");
                SceneManager.MoveGameObjectToScene(foreignRoot, foreign);
                var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Unity.Entities.CompanionGameObjectUtility", false))
                    .Where(t => t != null).Single();
                const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
                var initialize = type.GetMethod("GetCompanionScene", flags, null, new[] { typeof(bool) }, null);
                if (initialize == null || initialize.ReturnType != typeof(Scene)) throw new InvalidOperationException("Actual package initializer required");
                var companion = (Scene)initialize.Invoke(null, new object[] { false });
                bool Refused()
                {
                    try { CloseEmptyPackageCompanionScenesAtFinalExit(); return false; }
                    catch (InvalidOperationException) { return true; }
                }
                using (var world = new World("OwnedCompanionShutdownRefusalControl"))
                {
                    if (!Refused() || !companion.IsValid() || !foreign.IsValid()) throw new InvalidOperationException("Live World must refuse without scene disposal");
                    controls++;
                }
                var companionRoot = new GameObject("OwnedCompanionControlBusyRoot");
                SceneManager.MoveGameObjectToScene(companionRoot, companion);
                try
                {
                    if (!Refused() || !companion.IsValid() || companionRoot == null || !foreign.IsValid())
                        throw new InvalidOperationException("Nonempty companion must refuse without deleting its root");
                    controls++;
                }
                finally { UnityEngine.Object.DestroyImmediate(companionRoot); }
                var closed = CloseEmptyPackageCompanionScenesAtFinalExit();
                if ((int)closed["closed_count"] != 2 || companion.IsValid() || !foreign.IsValid() || foreignRoot == null || foreignRoot.scene != foreign)
                    throw new InvalidOperationException("Actual owner cleanup must preserve foreign preview/root identity");
                controls++;
                var twice = CloseEmptyPackageCompanionScenesAtFinalExit();
                if ((int)twice["closed_count"] != 0 || !(bool)twice["already_disposed"] || !foreign.IsValid() || foreignRoot == null)
                    throw new InvalidOperationException("Repeated cleanup must be idempotent and preserve foreign ownership");
                controls++;
                var recreated = (Scene)initialize.Invoke(null, new object[] { false });
                if (!recreated.IsValid() || (int)CloseEmptyPackageCompanionScenesAtFinalExit()["closed_count"] != 2 || recreated.IsValid())
                    throw new InvalidOperationException("Package reinitialization after cache reset must retain valid disposal semantics");
                controls++;
            }
            finally
            {
                // Only this control's explicitly created foreign preview is owned here.
                if (foreign.IsValid()) EditorSceneManager.ClosePreviewScene(foreign);
            }
            if (controls != 5) throw new InvalidOperationException("All real owner controls required");
            Debug.Log("[PackageCompanionShutdownControls] result=Passed controls=5 actualOwner=1 foreignPreviewPreserved=1 idempotent=1 persistentAllocationsQualified=0");
        }

        static string Handle(Scene scene) => scene.handle.GetRawData().ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
#endif
