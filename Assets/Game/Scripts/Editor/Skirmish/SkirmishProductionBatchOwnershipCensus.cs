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
    // Read-only ownership census. Never creates or closes a preview scene.
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
        static string Handle(Scene scene) => scene.handle.GetRawData().ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
#endif
