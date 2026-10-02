using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Authoring;
using Game.Configs;
using Unity.Entities.Build;
using Unity.Entities.Content;
using Unity.Scenes.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Hash128=Unity.Entities.Hash128;

namespace Game.Editor
{
    /// <summary>Isolated native entity/Addressables content for Trust Under Fire's independent map.</summary>
    public static class CH05M02TrustUnderFireContentBuilder
    {
        public const string Output="Library/TrustUnderFirePreparedContent";
        public const string FixturePath=CH05M02TrustUnderFireConfigBuilder.SceneFolder+"/UnitPrefabFixture.unity";
        public const string FixtureConfigPath=CH05M02TrustUnderFireConfigBuilder.Folder+"/UnitFixtureRegistry.asset";
        public const string ReportPath="Design/AgentReports/CH05M02TrustUnderFire/packed-content.json";
        private const string Temp="Assets/Game/GeneratedOperationMaps/CH05M02TrustUnderFire/PackagingTemp";
        private const string AddressPrefix="campaign/trust-under-fire/";
        private static (string path,string address)[] Entries()=>new[]{
            (CH05M02TrustUnderFireConfigBuilder.MapPath,"definition"),
            (CH05M02TrustUnderFireConfigBuilder.BindingPath,"source-scene"),
            (CH05M02TrustUnderFireConfigBuilder.SurfacePath,"surface"),
            (CH05M02TrustUnderFireConfigBuilder.GridPath,"grid"),
            (CH05M02TrustUnderFireConfigBuilder.Folder+"/Minimap.png","minimap"),
            (CH05M02TrustUnderFireConfigBuilder.ScenarioPath,"scenario"),
            (CH05M02TrustUnderFireConfigBuilder.MissionPath,"mission"),
            (CH05M02TrustUnderFireConfigBuilder.SupportContextPath,"support-context")};
        public static void RegisterProductionEntries()
        {
            var settings=AddressableAssetSettingsDefaultObject.Settings??throw new InvalidOperationException("Production Addressables settings missing.");
            int added=0;
            foreach(var pair in Entries())
            {
                string guid=AssetDatabase.AssetPathToGUID(pair.path);Require(!string.IsNullOrEmpty(guid),"Missing Trust Under Fire asset "+pair.path);
                var existing=settings.FindAssetEntry(guid);
                if(existing!=null)continue;
                settings.CreateOrMoveEntry(guid,settings.DefaultGroup).address=AddressPrefix+pair.address;added++;
            }
            EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
            Debug.Log("[TrustUnderFireContentRegistration] result=Passed ownAssets="+Entries().Length+" added="+added+" GUIDLookup=Supported existingEntries=Preserved");
        }
        public static void BuildPackedContent()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Packed content generation requires edit mode.");
            Require(!AssetDatabase.IsValidFolder(Temp),"Inspect interrupted Trust Under Fire PackagingTemp before retry.");
            EditorApplication.LockReloadAssemblies();
            try
            {
                string fixtureGuid=BuildUnitFixture();var map=Load<OperationMapDefinition>(CH05M02TrustUnderFireConfigBuilder.MapPath);
                Require(map.TryValidateMetadata(out string error)&&map.TryValidateLocalContentReferences(out error),error);
                Require(!map.SourceBinding.IsConfigured,"Trust Under Fire must own its physical scene.");
                string entityGuid=map.NavigationMetadata.AuthoredSubSceneGuid;
                Require(entityGuid==AssetDatabase.AssetPathToGUID(CH05M02TrustUnderFireConfigBuilder.EntityScenePath),"Entity scene identity mismatch.");
                string entities=Output+"/Entities";ResetOwnedOutput(entities);
                var player=DotsGlobalSettings.Instance.GetClientGUID();Require(player.IsValid,"Entities client player GUID missing.");
                RemoteContentCatalogBuildUtility.BuildContent(new HashSet<Hash128>{new(entityGuid),new(fixtureGuid)},player,BuildTarget.StandaloneOSX,Path.GetFullPath(entities));
                string entityCatalog=entities+"/"+RuntimeContentManager.RelativeCatalogPath;Require(File.Exists(entityCatalog),"Own Entities catalog not produced.");
                string addressablesCatalog=BuildAddressables();
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
                var report=new ContentReport {schema="warline.trust-under-fire.packed-content.v1",result="Passed",mapId=map.OperationMapId,contentHash=map.ContentHash,
                    target=BuildTarget.StandaloneOSX.ToString(),entitySceneGuid=entityGuid,unitFixtureSceneGuid=fixtureGuid,entityContentPath=entities,
                    entityCatalogPath=entityCatalog,addressablesCatalogPath=addressablesCatalog,definitionGuid=AssetDatabase.AssetPathToGUID(CH05M02TrustUnderFireConfigBuilder.MapPath),
                    bindingGuid=AssetDatabase.AssetPathToGUID(CH05M02TrustUnderFireConfigBuilder.BindingPath),entityCatalogHash=HashFile(entityCatalog),addressablesCatalogHash=HashFile(addressablesCatalog),
                    entityContentBytes=Directory.GetFiles(entities,"*",SearchOption.AllDirectories).Sum(p=>new FileInfo(p).Length),status="Native packed content built. Normal-input mission and real device acceptance are separate gates."};
                File.WriteAllText(ReportPath,JsonUtility.ToJson(report,true));
                Debug.Log("[TrustUnderFirePackedContent] result=Passed map="+map.OperationMapId+" entityScene="+entityGuid+" fixture="+fixtureGuid+" bytes="+report.entityContentBytes+" locator=GUID+CampaignAddress scope=ContentBuildOnly");
            }
            finally {EditorApplication.UnlockReloadAssemblies();}
        }
        private static string BuildUnitFixture()
        {
            var scenario=Load<ScenarioSetupConfig>(CH05M02TrustUnderFireConfigBuilder.ScenarioPath);
            var sourceGuids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var group in scenario.UnitGroups)foreach(var unit in group.Units)sourceGuids.Add(unit.ExpectedAssetGuid);
            foreach(string path in new[]{CH05M02TrustUnderFireConfigBuilder.ReservePrefabPath,CH05M02TrustUnderFireConfigBuilder.ShelterPrefabPath,CH05M02TrustUnderFireConfigBuilder.BroadcastPrefabPath})sourceGuids.Add(AssetDatabase.AssetPathToGUID(path));
            var sources=sourceGuids.OrderBy(guid=>guid,StringComparer.Ordinal).ToArray();
            var source=Load<UnitPrefabRegistryAuthoringConfig>("Assets/Game/Configs/Scene/Game_UnitPrefabRegistry_Config.asset");
            var config=AssetDatabase.LoadAssetAtPath<UnitPrefabRegistryAuthoringConfig>(FixtureConfigPath);
            if(config==null){config=ScriptableObject.CreateInstance<UnitPrefabRegistryAuthoringConfig>();AssetDatabase.CreateAsset(config,FixtureConfigPath);}
            EditorUtility.CopySerialized(source,config);config.UnitSpawnPrefabs.Clear();config.ImpostorAtlases.Clear();
            foreach(string guid in sources)config.UnitSpawnPrefabs.Add(Load<GameObject>(AssetDatabase.GUIDToAssetPath(guid)));
            EditorUtility.SetDirty(config);AssetDatabase.SaveAssetIfDirty(config);
            var previous=SceneManager.GetActiveScene();Scene fixture=default;
            try
            {
                fixture=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(fixture);
                var root=new GameObject("TrustUnderFireUnitPrefabFixture");var registry=root.AddComponent<UnitPrefabRegistryAuthoring>();
                var data=new SerializedObject(registry);data.FindProperty("config").objectReferenceValue=config;data.ApplyModifiedPropertiesWithoutUndo();
                Require(EditorSceneManager.SaveScene(fixture,FixturePath),"Unit fixture scene save failed.");
                Debug.Log("[TrustUnderFireUnitFixture] result=Passed uniquePrefabs="+sources.Length+" combinedArms=Armor+Infantry+ProtectedConvoys shelterOwners=2 broadcast=1 Fuel=RealStorage fixtureLoadedByMission=false");
                return AssetDatabase.AssetPathToGUID(FixturePath);
            }
            finally {if(fixture.IsValid()&&fixture.isLoaded)EditorSceneManager.CloseScene(fixture,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
        }
        private static string BuildAddressables()
        {
            string output=Path.GetFullPath(Output+"/Addressables"),shared=Path.GetFullPath("Library/com.unity.addressables/aa"),backup=Path.GetFullPath(Output+"-aa-backup-"+Guid.NewGuid().ToString("N"));
            bool hadShared=Directory.Exists(shared);if(hadShared)Directory.Move(shared,backup);
            var protectedSettings=OperationMapDenseCityCandidateRuntimeContentBuilder.CandidateDirectoryTransaction.Capture(Path.GetFullPath("."),"Assets/AddressableAssetsData");
            var protectedRendering=OperationMapEntitySceneCandidateBakeAll.CandidateFileTransaction.Capture(Path.GetFullPath("."),new[]{"Assets/Settings/PC_RPAsset.asset"});
            try
            {
                ResetOwnedOutput(output);var settings=AddressableAssetSettings.Create(Temp,"TrustUnderFireValidationSettings",false,true);
                settings.BuildRemoteCatalog=false;settings.DisableCatalogUpdateOnStartup=true;
                settings.profileSettings.SetValue(settings.activeProfileId,AddressableAssetSettings.kLocalBuildPath,output+"/Bundles");
                settings.profileSettings.SetValue(settings.activeProfileId,AddressableAssetSettings.kLocalLoadPath,output+"/Bundles");
                var group=settings.CreateGroup("Trust Under Fire Content",true,false,false,null,typeof(BundledAssetGroupSchema),typeof(ContentUpdateGroupSchema));
                var schema=group.GetSchema<BundledAssetGroupSchema>();schema.BuildPath.SetVariableByName(settings,AddressableAssetSettings.kLocalBuildPath);schema.LoadPath.SetVariableByName(settings,AddressableAssetSettings.kLocalLoadPath);
                schema.UseDefaultSchemaSettings=false;schema.IncludeInBuild=true;schema.Compression=BundledAssetGroupSchema.BundleCompressionMode.LZ4;schema.BundleMode=BundledAssetGroupSchema.BundlePackingMode.PackTogether;schema.BundleNaming=BundledAssetGroupSchema.BundleNamingStyle.FileNameHash;
                foreach(var pair in Entries())settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(pair.path),group).address=AddressPrefix+pair.address;
                var builder=ScriptableObject.CreateInstance<BuildScriptPackedMode>();AssetDatabase.CreateAsset(builder,Temp+"/PackedBuilder.asset");Require(settings.AddDataBuilder(builder,false),"Addressables builder registration failed.");settings.ActivePlayerDataBuilderIndex=0;AssetDatabase.SaveAssets();
                var input=new AddressablesDataBuilderInput(settings,new BuildPlayerOptions {target=BuildTarget.StandaloneOSX}) {RuntimeCatalogFilename="catalog",RuntimeSettingsFilename="settings.json"};
                var result=builder.BuildData<AddressablesPlayerBuildResult>(input);Require(result!=null&&string.IsNullOrEmpty(result.Error),result?.Error??"Addressables returned no result.");
                string built=Path.Combine(shared,"OSX");if(Directory.Exists(built))CopyTree(built,output);
                var catalogs=Directory.GetFiles(output,"catalog.bin",SearchOption.AllDirectories);Require(catalogs.Length==1,"Expected exactly one own Addressables catalog.");return catalogs[0];
            }
            finally
            {
                if(Directory.Exists(shared))Directory.Delete(shared,true);if(hadShared)Directory.Move(backup,shared);
                if(AssetDatabase.IsValidFolder(Temp))AssetDatabase.DeleteAsset(Temp);protectedSettings.Rollback();protectedRendering.Rollback();
            }
        }
        // These are isolated, reproducible task build products. Never clear the
        // shared Addressables cache, source assets, or retained validation evidence.
        private static void ResetOwnedOutput(string path)
        {
            string root=Path.GetFullPath(Output);
            string full=Path.GetFullPath(path);
            Require(full==Path.Combine(root,"Entities")||full==Path.Combine(root,"Addressables"),"Refusing to clear non-owned content path: "+full);
            if(Directory.Exists(full))Directory.Delete(full,true);
            Directory.CreateDirectory(full);
        }
        private static string HashFile(string path){using var sha=System.Security.Cryptography.SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-",string.Empty).ToLowerInvariant();}
        private static void CopyTree(string source,string target){foreach(string file in Directory.GetFiles(source,"*",SearchOption.AllDirectories)){string destination=Path.Combine(target,Path.GetRelativePath(source,file));Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(file,destination,true);}}
        private static T Load<T>(string path)where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>(path)??throw new InvalidOperationException(path);
        private static void Require(bool condition,string error){if(!condition)throw new InvalidOperationException(error);}
        [Serializable]private sealed class ContentReport {public string schema,result,mapId,contentHash,target,entitySceneGuid,unitFixtureSceneGuid,entityContentPath,entityCatalogPath,addressablesCatalogPath,definitionGuid,bindingGuid,entityCatalogHash,addressablesCatalogHash,status;public long entityContentBytes;}
    }
}
