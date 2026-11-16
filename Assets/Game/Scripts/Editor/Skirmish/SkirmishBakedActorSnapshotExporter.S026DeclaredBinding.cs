#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishBakedActorSnapshotExporter
    {
        // Captures only original authoring/compiler/provider facts for this exact
        // disposable candidate. No scene asset, mission or readiness is modified.
        public static void RunS026DeclaredBindingQualification()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Idle Editor required");
            var args=Environment.GetCommandLineArgs();
            string Arg(string name){int i=Array.IndexOf(args,name);if(i<0||i+1>=args.Length||!Path.IsPathRooted(args[i+1]))throw new ArgumentException("Explicit absolute "+name+" required");return Path.GetFullPath(args[i+1]);}
            string output=SkirmishOfflineDataExporter.ValidateOutputDirectory(Arg("-skirmishS026DeclaredOutput"));
            string layoutPath=Arg("-skirmishS026MetadataLayout"),candidatePath=Arg("-skirmishS026DeclaredCandidate");
            if(SkirmishOfflineDataExporter.HashFile(layoutPath)!="669c8b20f55890b37657605f9c0d67087f6d4da672d2e0eae2ab9d78ca2ca14e"||SkirmishOfflineDataExporter.HashFile(candidatePath)!="0a412bf51e718e3fba00d1a89f3699a9b8959c200933eb8d20c097fc0db86f61")throw new InvalidOperationException("Exact frozen layout and external candidate required");
            var candidate=JObject.Parse(File.ReadAllText(candidatePath),new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
            if((string)candidate["kind"]!="S026ExternalDeclaredTypedPadCandidate"||(bool?)candidate["runtime_admission"]!=false||(bool?)candidate["source_fit_certified"]!=false||(bool?)candidate["mission_ready"]!=false)throw new InvalidOperationException("Unqualified external candidate required");
            var delta=(JObject)candidate["candidate_additions"];var layout=SkirmishFrozenLayoutFieldImporter.ReadS026FrozenFixture(JObject.Parse(File.ReadAllText(layoutPath)),layoutPath);
            var definition=ScriptableObject.CreateInstance<SkirmishScenarioDefinitionConfig>();SkirmishMissionLogicalBindingConfig binding=null;SkirmishMissionViewProfile view=null;var originalDefault=World.DefaultGameObjectInjectionWorld;
            try
            {
                var addedAnchors=((JArray)delta["anchors"]).ToObject<SkirmishLayoutAnchorConfig[]>();var addedPads=((JArray)delta["pads"]).ToObject<SkirmishLegalPadConfig[]>();var bindings=((JArray)delta["instance_bindings"]).ToObject<SkirmishStructureInstancePadBinding[]>();var removed=((JArray)delta["removed_pad_ids"]).Values<string>().ToArray();
                if(addedAnchors.Length!=20||addedPads.Length!=20||bindings.Length!=20||removed.Length!=10||removed.Distinct(StringComparer.Ordinal).Count()!=10||removed.Any(id=>layout.Pads.Count(p=>p.PadId==id)!=1)||layout.StructureInstancePads.Length!=0||(string)delta["declared_size"]!="Standard"||(string)delta["declared_difficulty"]!="Regular"||(bool?)delta["measured_deployments"]!=false||(bool?)delta["bind_all_configurations"]!=false)throw new InvalidOperationException("Exact declared candidate domain required");
                var af=typeof(SkirmishMapLayoutConfig).GetField("anchors",BindingFlags.Instance|BindingFlags.NonPublic);var pf=typeof(SkirmishMapLayoutConfig).GetField("pads",BindingFlags.Instance|BindingFlags.NonPublic);var df=typeof(SkirmishMapLayoutConfig).GetField("declaredConfigurations",BindingFlags.Instance|BindingFlags.NonPublic);if(af==null||pf==null||df==null)throw new InvalidOperationException("Original layout fields required");
                af.SetValue(layout,layout.Anchors.Concat(addedAnchors).ToArray());pf.SetValue(layout,layout.Pads.Where(p=>!removed.Contains(p.PadId)).Concat(addedPads).ToArray());layout.BindStructureInstancePads(bindings);
                var player=(JArray)delta["player_deployment"];var enemy=(JArray)delta["enemy_deployment"];if(player.Count!=2||enemy.Count!=2)throw new InvalidOperationException("Exact declared deployments required");df.SetValue(layout,new[]{new SkirmishDeclaredLayoutConfiguration{SizeId=SkirmishSizeId.Standard,DifficultyId=SkirmishDifficultyId.Regular,PlayerDeployment=new Vector2((float)player[0],(float)player[1]),EnemyDeployment=new Vector2((float)enemy[0],(float)enemy[1])}});var frame=layout.WorldBinding;frame.HasMeasuredDeployments=false;frame.BindAllConfigurations=false;var wf=typeof(SkirmishMapLayoutConfig).GetField("worldBinding",BindingFlags.Instance|BindingFlags.NonPublic);if(wf==null)throw new InvalidOperationException("Original world-binding field required");wf.SetValue(layout,frame);
                if(SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(layout)!="d2d1dbb4825bb8ba692d4ed7090de65299cda119cce9af46c869fcebb70fb5b9")throw new InvalidOperationException("Candidate layout fingerprint differs from source compiler");
                const string shared="Assets/Game/Configs/SkirmishExpansion/Shared/",sourcePath="Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset";
                var profiles=new[]{shared+"SkirmishObjective_BA.asset",shared+"SkirmishArmy_GroundManeuver.asset",shared+"SkirmishStart_Established.asset",shared+"SkirmishEconomy_V1.asset",shared+"SkirmishUpgrade_V1.asset",shared+"SkirmishIntel_SharedFog.asset",shared+"SkirmishRoleCatalog_V1.asset"};
                var source=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(sourcePath);if(source==null||SkirmishOfflineDataExporter.HashFile(sourcePath)!="68a631ca108e01c9a17839d2359e409226cdb009193f68ba910fa6f0f0a082b8")throw new InvalidOperationException("Exact Campaign source required");
                definition.Configure("S026",AssetDatabase.LoadAssetAtPath<SkirmishObjectiveConfig>(profiles[0]),AssetDatabase.LoadAssetAtPath<SkirmishArmyProfileConfig>(profiles[1]),AssetDatabase.LoadAssetAtPath<SkirmishStartPackageConfig>(profiles[2]),AssetDatabase.LoadAssetAtPath<SkirmishEconomyConfig>(profiles[3]),AssetDatabase.LoadAssetAtPath<SkirmishUpgradeConfig>(profiles[4]),AssetDatabase.LoadAssetAtPath<SkirmishIntelConfig>(profiles[5]),AssetDatabase.LoadAssetAtPath<SkirmishRoleCatalogConfig>(profiles[6]),layout);definition.BindCampaignMap(source);
                var baseline=SkirmishPresetConfig.Load(SkirmishPresetConfig.DesertBaseScenarioIndex);var scene=AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(AssetDatabase.GUIDToAssetPath("b2010000000000000000000000000004"));var expansion=SkirmishExpansionCatalogConfig.Load();var s2=expansion.Definitions.Single(d=>d!=null&&d.CatalogId=="S002");
                var pins=SourcePins(profiles.Concat(new[]{sourcePath,AssetDatabase.GetAssetPath(expansion),AssetDatabase.GetAssetPath(s2),AssetDatabase.GetAssetPath(baseline),AssetDatabase.GetAssetPath(baseline.buildingPlacement),AssetDatabase.GetAssetPath(scene),AssetDatabase.GetAssetPath(scene.UnitPrefabRegistryConfig),"Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv",SourceScene}).ToArray());pins[layoutPath]=SkirmishOfflineDataExporter.HashFile(layoutPath);pins[candidatePath]=SkirmishOfflineDataExporter.HashFile(candidatePath);
                if(!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error))throw new InvalidOperationException(error);var manifest=new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds};
                using(var world=new World("S026DeclaredBindingAuthoringReferenceOnly",WorldFlags.Game))
                {
                    var systems=DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Streaming,true);foreach(var system in systems)if(TypeManager.GetSystemName(system).ToString().StartsWith("Game.",StringComparison.Ordinal))throw new InvalidOperationException("No gameplay systems during logical authoring");DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world,systems);
                    var loaded=SceneSystem.LoadSceneAsync(world.Unmanaged,new Unity.Entities.Hash128(AssetDatabase.AssetPathToGUID(SourceScene)),new SceneSystem.LoadParameters{Flags=SceneLoadFlags.BlockOnImport|SceneLoadFlags.BlockOnStreamIn});world.Update();if(!SceneSystem.IsSceneLoaded(world.Unmanaged,loaded))throw new InvalidOperationException("Actual registry scene not loaded");var em=world.EntityManager;
                    if(!SkirmishSetupCompiler.TryCaptureMissionLogicalAuthoringInputs(definition,SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,104755,manifest,matrix,out var inputs,out var inputReasons))throw new InvalidOperationException("Original authoring inputs: "+string.Join(";",inputReasons));
                    var targets=SkirmishGroundSourceFitSurvey.CreateRefinementAircraftEngineeringTargets(new[]{inputs},em,out var targetFacts);var variant=SkirmishGroundSourceFitSurvey.CreateRefinementEngineeringView(definition,inputs,em,null,targets);view=variant.View;
                    var wrong=new SkirmishMissionLogicalVariant{Size=SkirmishSizeId.War,Difficulty=SkirmishDifficultyId.Regular};if(SkirmishGroundSourceFitSurvey.TryAttachRefinementLogicalBinding(definition,new[]{wrong},matrix,out var unexpected,out _,104755))throw new InvalidOperationException("Wrong declared tuple accepted");
                    if(!SkirmishGroundSourceFitSurvey.TryAttachRefinementLogicalBinding(definition,new[]{variant},matrix,out binding,out error,104755))throw new InvalidOperationException("Original declared binding authoring failed: "+error);
                    if(!SkirmishSetupCompiler.TryCompile(definition,SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,104755,manifest,matrix,out var setup,out var reasons)||!SkirmishMissionLogicalBindingCompiler.TryCompile(definition,setup,out var compiled,out error)||compiled.Fingerprint!=setup.MissionLogicalBindingFingerprint)throw new InvalidOperationException("Original declared compile failed: "+error+" "+string.Join(";",reasons));
                    var session=em.CreateEntity();try{var state=new SkirmishExpandedSessionComponent{SessionId=new FixedString64Bytes("logical-S026-declared"),CatalogId=new FixedString64Bytes(setup.CatalogId),DefinitionId=new FixedString64Bytes(setup.DefinitionId),ContentVersion=setup.ContentVersion,SetupHash=setup.SetupHash,Seed=setup.Seed,SizeId=setup.SizeId,DifficultyId=setup.DifficultyId,ObjectiveKind=setup.ObjectiveKind,Phase=SkirmishSessionPhase.Queued};em.AddComponentData(session,state);em.AddComponentObject(session,new SkirmishResolvedSetupRecord{Setup=setup});em.AddComponentObject(session,new SkirmishLaunchDefinitionRecord(definition,setup,true));if(!SkirmishMissionLogicalBindingProvider.TryAttachCompiled(em,session,definition,setup,compiled,out error)||!SkirmishMissionLogicalBindingProvider.TryRead(em,session,out var read)||!ReferenceEquals(compiled,read))throw new InvalidOperationException("Original provider failed: "+error);}finally{em.DestroyEntity(session);}
                    foreach(var pin in pins)if(SkirmishOfflineDataExporter.HashFile(pin.Key)!=pin.Value)throw new InvalidOperationException("Authoring input drift: "+pin.Key);
                    Directory.CreateDirectory(output);var portable=new JObject{["schema_version"]=1,["kind"]="DeclaredOriginalLogicalBindingFieldsWithInlineViews",["float_encoding"]="IEEE754_float32_hex_bits",["unity_object_ids_exported"]=false,["fields"]=PortableLogicalValue(binding,0)};
                    WriteCameraReferenceFile(Path.Combine(output,"logical-binding.json"),new JObject{["kind"]="NativeAuthoredS026DeclaredLogicalBindingReference",["candidate_sha256"]=SkirmishOfflineDataExporter.HashFile(candidatePath),["layout_fingerprint"]=SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(layout),["portable_binding"]=portable,["provider_contexts"]=new JArray(new JObject{["size"]="Standard",["difficulty"]="Regular",["setup_hash"]=setup.SetupHash,["logical_fingerprint"]=compiled.Fingerprint,["actual_provider_passed"]=true,["definition_instance_shared"]=true}),["aircraft_engineering_target_facts"]=targetFacts,["wrong_declared_tuple_rejected"]=true,["source_pins"]=JObject.FromObject(pins),["authoring_world_created"]=true,["mission_initialized"]=false,["positions_or_state_transferred"]=false,["public_admission_changed"]=false,["native_visual_acceptance"]=false});
                    WriteCameraReferenceFile(Path.Combine(output,"metadata-context.json"),new JObject{["kind"]="NativeS026DeclaredMetadataContext",["context"]=CaptureCompiledMetadataContext(104755,baseline.buildingPlacement,scene,definition,"S026",SkirmishDifficultyId.Regular,SkirmishSizeId.Standard),["logical_binding_reference_sha256"]=SkirmishOfflineDataExporter.HashFile(Path.Combine(output,"logical-binding.json")),["source_pins"]=JObject.FromObject(pins),["full_mission_initialized"]=false,["runtime_state_transferred"]=false,["public_admission_changed"]=false});
                    foreach(var pin in pins)if(SkirmishOfflineDataExporter.HashFile(pin.Key)!=pin.Value)throw new InvalidOperationException("Authoring postflight drift: "+pin.Key);
                    Debug.Log("[SkirmishS026DeclaredBinding] result=Passed contexts=1 provider=1 wrong_tuple_rejected=True missionRun=0 readiness=0");
                }
                if(!ReferenceEquals(originalDefault,World.DefaultGameObjectInjectionWorld))throw new InvalidOperationException("Default World changed");
            }
            finally{if(view!=null)UnityEngine.Object.DestroyImmediate(view);if(binding!=null)UnityEngine.Object.DestroyImmediate(binding);UnityEngine.Object.DestroyImmediate(definition);UnityEngine.Object.DestroyImmediate(layout);}
        }
    }
}
#endif
