#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishGenericMissionDispatchTests
    {
        // Disposable logical fixtures against actual Campaign metadata. Synthetic
        // compact frames exercise compiler/dispatch only, never source-fit proof.
        private sealed class Fixture:IDisposable
        {
            public readonly List<UnityEngine.Object> Owned=new();
            public readonly SkirmishExpansionCatalogConfig Authored;
            public readonly SkirmishBattleCatalogConfig Library;
            public readonly SkirmishScenarioDefinitionConfig Definition;
            public readonly int Index;
            private readonly IDisposable authoredScope,libraryScope;
            public Fixture(string id="S026",bool published=true,bool activateScopes=true,bool attachView=true)
            {
                var actual=Resources.Load<SkirmishExpansionCatalogConfig>(SkirmishExpansionCatalogConfig.ResourceName);Assert.IsNotNull(actual);
                var profiles=actual.ToAuthoredSet();Assert.IsNotNull(profiles.DefinitionS005,"Packaged template must not come from a scoped authored catalog.");var layout=UnityEngine.Object.Instantiate(profiles.DefinitionS005.MapLayout);Owned.Add(layout);
                string path=id=="S049"||id=="S050"?"Assets/Game/GeneratedOperationMaps/CH04M05ArmorBreak/Definition.asset":
                    id=="S074"?"Assets/Game/GeneratedOperationMaps/Variants/RefineryDistrict/Candidate/Definition.asset":
                    id=="S097"||id=="S098"?"Assets/Game/GeneratedOperationMaps/Variants/CityEdgeAirfield/Candidate/Definition.asset":
                    "Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset";
                var source=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(path);Assert.IsNotNull(source,path);
                var data=new SerializedObject(layout);data.FindProperty("layoutId").stringValue="layout.dispatch.fixture."+id;
                data.FindProperty("operationMapId").stringValue="opmap.dispatch.fixture."+id;
                var frame=data.FindProperty("worldBinding");var min=source.Bounds.WorldMin;var max=source.Bounds.WorldMax;
                frame.FindPropertyRelative("SourceHash").stringValue=source.ContentHash;
                frame.FindPropertyRelative("HasMeasuredDeployments").boolValue=true;frame.FindPropertyRelative("BindAllConfigurations").boolValue=true;
                frame.FindPropertyRelative("OriginX").floatValue=min.x+(max.x-min.x)*.4f;frame.FindPropertyRelative("OriginZ").floatValue=min.z+(max.z-min.z)*.5f;
                frame.FindPropertyRelative("ForwardX").floatValue=1;frame.FindPropertyRelative("ForwardZ").floatValue=0;
                frame.FindPropertyRelative("AcrossX").floatValue=0;frame.FindPropertyRelative("AcrossZ").floatValue=1;
                frame.FindPropertyRelative("ForwardMetres").floatValue=(max.x-min.x)*.2f;frame.FindPropertyRelative("AcrossMetres").floatValue=(max.z-min.z)*.2f;
                data.ApplyModifiedPropertiesWithoutUndo();
                layout.TryGetAnchor("base.player",out var player);layout.TryGetAnchor("base.enemy",out var enemy);
                layout.TryProjectToMap(player.NormalizedU,player.NormalizedV,out float px,out float pz);layout.TryProjectToMap(enemy.NormalizedU,enemy.NormalizedV,out float ex,out float ez);
                data=new SerializedObject(layout);frame=data.FindProperty("worldBinding");
                frame.FindPropertyRelative("PlayerDeployment").vector2Value=new Vector2(px,pz);frame.FindPropertyRelative("EnemyDeployment").vector2Value=new Vector2(ex,ez);data.ApplyModifiedPropertiesWithoutUndo();
                Definition=ScriptableObject.CreateInstance<SkirmishScenarioDefinitionConfig>();Owned.Add(Definition);
                bool field=id=="S049"||id=="S097";
                Definition.Configure(id,profiles.ObjectiveBa,profiles.ArmyGround,field?profiles.StartField:profiles.StartEstablished,
                    profiles.Economy,profiles.Upgrades,profiles.Intel,profiles.Roles,layout);Definition.BindCampaignMap(source);
                if(attachView)SkirmishMissionLogicalBindingTestFixture.AttachGenericViewOnly(Definition,Owned);
                Authored=UnityEngine.Object.Instantiate(actual);Owned.Add(Authored);data=new SerializedObject(Authored);
                var definitions=data.FindProperty("definitions");definitions.arraySize=1;definitions.GetArrayElementAtIndex(0).objectReferenceValue=Definition;data.ApplyModifiedPropertiesWithoutUndo();
                Library=ScriptableObject.CreateInstance<SkirmishBattleCatalogConfig>();Owned.Add(Library);Index=1000+int.Parse(id.Substring(1),System.Globalization.CultureInfo.InvariantCulture);
                Library.Configure(new[]{Entry(Definition,Index,published)});
                if(activateScopes)
                {
                    authoredScope=SkirmishExpansionCatalogConfig.UseEditorValidationCatalog(Authored);
                    libraryScope=SkirmishBattleCatalogConfig.UseEditorValidationCatalog(Library);
                }
            }
            public SkirmishResolvedSetup Compile()
            {
                Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error),error);
                Assert.IsTrue(SkirmishSetupCompiler.TryCompile(Definition,SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,104729,
                    new SkirmishContentManifest{RequiredFeatureIds=Definition.RequiredFeatureIds},matrix,out var setup,out var reasons),string.Join("\n",reasons));return setup;
            }
            public void Dispose(){libraryScope?.Dispose();authoredScope?.Dispose();for(int i=Owned.Count-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(Owned[i]);}
        }
        // Nested recovery needs another native World, not another global catalog
        // override. Keep the exact outer frozen definition alive until both finish.
        private static Fixture activeQueuedFixture;
        public static void WithViewBoundQueuedSession(Action<EntityManager,Entity> assertion)
        {
            if(activeQueuedFixture!=null)
            {
                WithFreshQueuedWorld(activeQueuedFixture,assertion);
                return;
            }
            using var fixture=new Fixture();
            activeQueuedFixture=fixture;
            try{WithFreshQueuedWorld(fixture,assertion);}
            finally{activeQueuedFixture=null;}
        }
        private static void WithFreshQueuedWorld(Fixture fixture,Action<EntityManager,Entity> assertion)
        {
            Assert.AreSame(fixture.Authored,SkirmishExpansionCatalogConfig.Load());
            Assert.AreSame(fixture.Library,SkirmishBattleCatalogConfig.Load());
            var defaultWorld=World.DefaultGameObjectInjectionWorld;
            using var world=new World("ViewBoundGenericQueuedFixture");
            var config=QuickGameConfig.Defaults;config.ScenarioIndex=fixture.Index;
            Assert.IsTrue(SkirmishLaunchProjection.TryQueue(world.EntityManager,config));
            using var sessions=world.EntityManager.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            var session=sessions.GetSingletonEntity();
            Assert.AreSame(fixture.Definition,world.EntityManager.GetComponentObject<SkirmishLaunchDefinitionRecord>(session).Definition);
            assertion(world.EntityManager,session);
            Assert.AreSame(fixture.Authored,SkirmishExpansionCatalogConfig.Load());
            Assert.AreSame(fixture.Library,SkirmishBattleCatalogConfig.Load());
            Assert.AreSame(defaultWorld,World.DefaultGameObjectInjectionWorld);
        }
        public static void WithSourceBoundUnqueuedDefinition(string catalogId,Action<SkirmishScenarioDefinitionConfig,List<UnityEngine.Object>> assertion)
        {
            using var fixture=new Fixture(catalogId,activateScopes:false,attachView:false);
            assertion(fixture.Definition,fixture.Owned);
        }
        private static SkirmishBattleCatalogEntry Entry(SkirmishScenarioDefinitionConfig definition,int index,bool published=true)=>
            new SkirmishBattleCatalogEntry{ScenarioId=definition.CatalogId,DefinitionId=definition.DefinitionId,ContentVersion=definition.ContentVersion,
                Status=published?SkirmishBattleCatalogStatus.Playable:SkirmishBattleCatalogStatus.Planned,PlayableScenarioIndex=published?index:-1};
        private static SkirmishLaunchPayload Payload(SkirmishResolvedSetup setup)=>new SkirmishLaunchPayload{SessionId="dispatch-fixture-attempt",
            CatalogId=setup.CatalogId,DefinitionId=setup.DefinitionId,ContentVersion=setup.ContentVersion,AllConfigHashes=setup.SetupHash.ToString("X8"),
            MapId=setup.OperationMapId,LayoutId=setup.LayoutId,Seed=setup.Seed,SizeId=setup.SizeId,DifficultyId=setup.DifficultyId};

        [Test] public void ExistingIndicesAndUnknownNormalizationRemainWhileExplicitGenericIndexSurvives()
        {
            using var f=new Fixture();
            for(int index=0;index<=7;index++){var config=QuickGameConfig.Defaults;config.ScenarioIndex=index;Assert.AreEqual(index,config.NormalizeForBaseAssault().ScenarioIndex);}
            foreach(int unknown in new[]{8,999,int.MaxValue,-1}){var config=QuickGameConfig.Defaults;config.ScenarioIndex=unknown;Assert.AreEqual(0,config.NormalizeForBaseAssault().ScenarioIndex);}
            var selected=QuickGameConfig.Defaults;selected.ScenarioIndex=f.Index;Assert.AreEqual(f.Index,selected.NormalizeForBaseAssault().ScenarioIndex);
        }
        [Test] public void CatalogAmbiguityPlannedAndReservedMissionRebindingFailClosed()
        {
            using var f=new Fixture();var valid=Entry(f.Definition,f.Index);
            f.Library.Configure(new[]{valid,valid});Assert.IsFalse(SkirmishMissionDispatch.TryGetGenericByIndex(f.Library,f.Index,out _));
            var other=valid;other.ScenarioId="S049";other.DefinitionId="skirmish.s049";f.Library.Configure(new[]{valid,other});
            Assert.IsFalse(SkirmishMissionDispatch.TryGetGenericByIndex(f.Library,f.Index,out _));
            f.Library.Configure(new[]{Entry(f.Definition,f.Index,false)});Assert.IsFalse(SkirmishMissionDispatch.TryGetGenericById(f.Library,"S026",out _));
            valid.ScenarioId="S002";valid.DefinitionId="skirmish.s002";f.Library.Configure(new[]{valid});
            Assert.IsFalse(SkirmishMissionDispatch.TryGetGenericByIndex(f.Library,f.Index,out _));
        }
        [Test] public void AllSixActualMenuQueuesRetainExactDefinitionAndExistingCampaignSource()
        {
            foreach(string id in new[]{"S026","S049","S050","S074","S097","S098"})
            {
                using var f=new Fixture(id);using var world=new World("GenericMenuLogicalFixture");var em=world.EntityManager;
                var config=QuickGameConfig.Defaults;config.ScenarioIndex=f.Index;
                Assert.IsTrue(SkirmishLaunchProjection.TryQueue(em,config),id);
                using var sessions=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));var session=sessions.GetSingletonEntity();
                Assert.AreEqual(id,em.GetComponentData<SkirmishExpandedSessionComponent>(session).CatalogId.ToString());
                Assert.AreSame(f.Definition,em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session).Definition);
                Assert.AreEqual(f.Index,em.GetComponentData<SkirmishMatchState>(session).ScenarioIndex);
                Assert.IsTrue(em.HasComponent<SkirmishExpandedReplayStartPending>(session));
                Assert.IsTrue(MatchSceneView.OperationMapLaunchResolver.TryResolve(world,null,null,null,out var selection,out _,out string error),error);
                Assert.AreSame(f.Definition.CampaignMap,selection.Definition);Assert.AreEqual(f.Definition.DefinitionId,selection.MissionId.ToString());
                Assert.AreEqual(f.Definition.ScenarioSetupId,selection.ScenarioId.ToString());
                Assert.AreEqual(f.Definition.CampaignMap.OperationMapId,selection.OperationMapId.ToString());
            }
            AssertNestedQueueKeepsExactOuterDefinitionAndScopes();
        }
        private static void AssertNestedQueueKeepsExactOuterDefinitionAndScopes()
        {
            var authoredBefore=SkirmishExpansionCatalogConfig.Load();
            var libraryBefore=SkirmishBattleCatalogConfig.Load();
            var defaultWorld=World.DefaultGameObjectInjectionWorld;
            WithViewBoundQueuedSession((outer,outerSession)=>
            {
                var authored=SkirmishExpansionCatalogConfig.Load();var library=SkirmishBattleCatalogConfig.Load();
                var definition=outer.GetComponentObject<SkirmishLaunchDefinitionRecord>(outerSession).Definition;
                var setup=outer.GetComponentObject<SkirmishResolvedSetupRecord>(outerSession).Setup;
                var attempt=outer.GetComponentData<SkirmishExpandedSessionComponent>(outerSession).SessionId;
                WithViewBoundQueuedSession((fresh,freshSession)=>
                {
                    Assert.AreNotEqual(outer.World.SequenceNumber,fresh.World.SequenceNumber);
                    Assert.AreNotEqual(attempt,fresh.GetComponentData<SkirmishExpandedSessionComponent>(freshSession).SessionId);
                    Assert.AreSame(definition,fresh.GetComponentObject<SkirmishLaunchDefinitionRecord>(freshSession).Definition);
                    var freshSetup=fresh.GetComponentObject<SkirmishResolvedSetupRecord>(freshSession).Setup;
                    Assert.AreNotSame(setup,freshSetup);Assert.AreEqual(setup.SetupHash,freshSetup.SetupHash);
                });
                Assert.IsTrue(outer.Exists(outerSession));Assert.AreSame(authored,SkirmishExpansionCatalogConfig.Load());
                Assert.AreSame(library,SkirmishBattleCatalogConfig.Load());Assert.AreSame(defaultWorld,World.DefaultGameObjectInjectionWorld);
            });
            Assert.AreSame(authoredBefore,SkirmishExpansionCatalogConfig.Load());
            Assert.AreSame(libraryBefore,SkirmishBattleCatalogConfig.Load());Assert.AreSame(defaultWorld,World.DefaultGameObjectInjectionWorld);
        }
        [Test] public void PayloadIdentityMutationsRejectBeforeAnySessionMutation()
        {
            using var f=new Fixture();var setup=f.Compile();
            for(int mutation=0;mutation<8;mutation++)
            {
                using var world=new World("RejectedGenericPayload");var payload=Payload(setup);
                if(mutation==0)payload.CatalogId="S049";if(mutation==1)payload.DefinitionId="skirmish.s049";
                if(mutation==2)payload.ContentVersion++;if(mutation==3)payload.MapId="opmap.wrong";
                if(mutation==4)payload.LayoutId="layout.wrong";if(mutation==5)payload.Seed++;
                if(mutation==6)payload.AllConfigHashes="00000000";if(mutation==7)payload.SessionId=new string('x',70);
                Assert.IsFalse(SkirmishExpandedLaunchProjection.TryQueue(world.EntityManager,payload,setup,null,f.Definition),"mutation "+mutation);
                Assert.AreEqual(0,world.EntityManager.UniversalQuery.CalculateEntityCount(),"Rejected queue must not create entities");
            }
        }
        [Test] public void ChangedExplicitSourceCannotFallBackToDesertOrAnotherMission()
        {
            using var f=new Fixture();using var world=new World("ChangedGenericSource");var config=QuickGameConfig.Defaults;config.ScenarioIndex=f.Index;
            Assert.IsTrue(SkirmishLaunchProjection.TryQueue(world.EntityManager,config));
            f.Definition.BindCampaignMap(AssetDatabase.LoadAssetAtPath<OperationMapDefinition>("Assets/Game/GeneratedOperationMaps/CH04M05ArmorBreak/Definition.asset"));
            Assert.IsFalse(MatchSceneView.OperationMapLaunchResolver.TryResolve(world,null,null,null,out _,out _,out _));
            Assert.Throws<InvalidOperationException>(()=>SkirmishS004Environment.LoadMapForSession(world.EntityManager));
        }
        [Test] public void UnpublishedUnknownAndDuplicateAuthoredDefinitionsCannotQueueOrNormalizeToPlayableMission()
        {
            using var f=new Fixture(published:false);using var world=new World("UnpublishedGenericMenu");var config=QuickGameConfig.Defaults;config.ScenarioIndex=f.Index;
            Assert.IsFalse(SkirmishLaunchProjection.TryQueue(world.EntityManager,config));
            config.ScenarioIndex=999;Assert.IsFalse(SkirmishLaunchProjection.TryQueue(world.EntityManager,config));
            Assert.AreEqual(0,world.EntityManager.UniversalQuery.CalculateEntityCount());
            var data=new SerializedObject(f.Authored);var entries=data.FindProperty("definitions");entries.arraySize=2;
            entries.GetArrayElementAtIndex(0).objectReferenceValue=f.Definition;entries.GetArrayElementAtIndex(1).objectReferenceValue=f.Definition;data.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsFalse(f.Authored.TryGetDefinition(f.Definition.CatalogId,out var ambiguous));Assert.IsNull(ambiguous);
        }
        [Test] public void ActualFreshReplayRetainsGenericMissionInsteadOfLegacyPhysicalMapIndex()
        {
            using var f=new Fixture("S049");using var world=new World("GenericFreshReplay");
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out _));
            var request=new SkirmishReplayRequest{CatalogId="S049",Mode=SkirmishReplayMode.FreshStart,SourceSessionId="previous-attempt",
                Seed=104729,SizeId=SkirmishSizeId.Standard,DifficultyId=SkirmishDifficultyId.Regular};
            Assert.IsTrue(SkirmishCheckpointCompositionSystemHelper.TryQueueFreshReplay(world.EntityManager,request,f.Authored.ToAuthoredSet(),matrix,
                new SkirmishContentManifest{RequiredFeatureIds=f.Definition.RequiredFeatureIds},out var payload,out var setup,out var reason),reason.ToString());
            Assert.AreEqual("S049",setup.CatalogId);Assert.AreNotEqual(request.SourceSessionId,payload.SessionId);
            using var sessions=world.EntityManager.CreateEntityQuery(typeof(SkirmishMatchState));Assert.AreEqual(f.Index,sessions.GetSingleton<SkirmishMatchState>().ScenarioIndex);
        }
        [Test] public void GenericNativeSaveEligibilityAndPathsAreExactAndDoNotAliasS005()
        {
            using var f=new Fixture();using var world=new World("GenericSaveEligibility");var config=QuickGameConfig.Defaults;config.ScenarioIndex=f.Index;
            Assert.IsTrue(SkirmishLaunchProjection.TryQueue(world.EntityManager,config));
            using var sessions=world.EntityManager.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));var entity=sessions.GetSingletonEntity();
            Assert.IsFalse(SkirmishNativePlayerSaveService.CanSave(world.EntityManager,entity));
            var state=world.EntityManager.GetComponentData<SkirmishExpandedSessionComponent>(entity);state.Phase=SkirmishSessionPhase.Playing;world.EntityManager.SetComponentData(entity,state);
            Assert.IsTrue(SkirmishNativePlayerSaveService.CanSave(world.EntityManager,entity));
            var path=typeof(SkirmishNativePlayerSaveService).GetMethod("SavePathForCatalog",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(path);string actual=(string)path.Invoke(null,new object[]{"S026"});string legacy=(string)path.Invoke(null,new object[]{"S005"});
            Assert.AreEqual("skirmish-s026-native.json",System.IO.Path.GetFileName(actual));Assert.AreEqual("skirmish-s005-native.json",System.IO.Path.GetFileName(legacy));
            Assert.AreNotEqual(actual,legacy);
            foreach(string invalid in new[]{"../S005","s026","S000","S999","S049"})Assert.IsNull(path.Invoke(null,new object[]{invalid}));
            // No save, discard, gameplay bank, purchase, physical placement or
            // successful recovery is fabricated by this eligibility/path test.
        }
        [Test] public void NormalDevelopmentMenuAvailabilityDoesNotPublishOrAlterShippedCatalogs()
        {
            var shippedLibrary=SkirmishBattleCatalogConfig.Load();var shippedAuthored=SkirmishExpansionCatalogConfig.Load();
            string libraryBefore=JsonUtility.ToJson(shippedLibrary);string authoredBefore=JsonUtility.ToJson(shippedAuthored);
            using var f=new Fixture(published:false,activateScopes:false);
            using(var available=Game.Editor.SkirmishGenericMissionAvailabilityScope.Open(f.Authored,"S026",f.Index))
            {
                Assert.IsTrue(SkirmishMissionDispatch.TryGetGenericByIndex(SkirmishBattleCatalogConfig.Load(),f.Index,out _));
                using var world=new World("UnqualifiedNormalMenuCandidate");var config=QuickGameConfig.Defaults;config.ScenarioIndex=f.Index;
                Assert.IsTrue(SkirmishLaunchProjection.TryQueue(world.EntityManager,config));
                Assert.AreEqual(libraryBefore,JsonUtility.ToJson(shippedLibrary));Assert.AreEqual(authoredBefore,JsonUtility.ToJson(shippedAuthored));
                Assert.AreSame(shippedAuthored.ToAuthoredSet().Publication,f.Authored.ToAuthoredSet().Publication);
            }
            Assert.AreSame(shippedLibrary,SkirmishBattleCatalogConfig.Load());Assert.AreSame(shippedAuthored,SkirmishExpansionCatalogConfig.Load());
            Assert.AreEqual(libraryBefore,JsonUtility.ToJson(shippedLibrary));Assert.AreEqual(authoredBefore,JsonUtility.ToJson(shippedAuthored));
        }

        // Persistence plumbing controls only. No source-fit report is invented.
        private static string DraftFileHash(string path)
        {
            using var file=System.IO.File.OpenRead(path);
            using var digest=System.Security.Cryptography.SHA256.Create();
            return BitConverter.ToString(digest.ComputeHash(file)).Replace("-","").ToLowerInvariant();
        }
        private static object DraftInvoke(string name,params object[] args)
        {
            var method=typeof(Game.Editor.SkirmishGroundSourceFitSurvey).GetMethod(name,
                System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            Assert.NotNull(method,name);return method.Invoke(null,args);
        }
        private static void WithPersistentDraftTransaction(Action<Fixture,string,SkirmishExpansionCatalogConfig,SkirmishBattleCatalogConfig> assertion)
        {
            var mainAuthored=SkirmishExpansionCatalogConfig.Load();var mainBattle=SkirmishBattleCatalogConfig.Load();
            var defaultWorld=World.DefaultGameObjectInjectionWorld;
            string ap=AssetDatabase.GetAssetPath(mainAuthored),bp=AssetDatabase.GetAssetPath(mainBattle);
            string ah=DraftFileHash(ap),bh=DraftFileHash(bp);
            var publication=mainAuthored.ToAuthoredSet().Publication;
            string pp=AssetDatabase.GetAssetPath(publication);
            Assert.IsFalse(string.IsNullOrEmpty(pp),"Actual persistent publication config required");
            string ph=DraftFileHash(pp);
            Assert.IsFalse(EditorUtility.IsDirty(mainAuthored));Assert.IsFalse(EditorUtility.IsDirty(mainBattle));
            foreach(var asset in Resources.FindObjectsOfTypeAll<ScriptableObject>())
                if(AssetDatabase.Contains(asset))Assert.IsFalse(EditorUtility.IsDirty(asset),"Refuse SaveAssets with unrelated dirty persistent object");
            string name="__LogicalDraftQaControls_"+Guid.NewGuid().ToString("N");
            string root="Assets/"+name;Assert.IsTrue(!AssetDatabase.IsValidFolder(root));
            AssetDatabase.CreateFolder("Assets",name);
            try
            {
                using var f=new Fixture(published:false,activateScopes:false);
                var binding=f.Definition.MissionLogicalBinding;var original=binding.Variants[0];
                Assert.IsNull(original.Areas,"BA draft fixture starts without non-BA typed areas.");
                var variants=new List<SkirmishMissionLogicalVariant>();
                foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
                {
                    var variant=JsonUtility.FromJson<SkirmishMissionLogicalVariant>(JsonUtility.ToJson(original));
                    if(original.Areas==null)variant.Areas=null;
                    Assert.IsNull(variant.Areas,"BA fixture JSON clone must preserve absent typed areas.");
                    variant.View=original.View;variant.Size=size;
                    variant.ReviewedViewFingerprint=SkirmishMissionLogicalBindingCompiler.ViewFingerprint(variant);
                    variants.Add(variant);
                }
                binding.Variants=variants.ToArray();
                var authored=UnityEngine.Object.Instantiate(f.Authored);var library=UnityEngine.Object.Instantiate(f.Library);
                var fields=new SerializedObject(authored);fields.FindProperty("definitions").arraySize=0;fields.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(authored,root+"/Authored.asset");AssetDatabase.CreateAsset(library,root+"/Battle.asset");
                AssetDatabase.SaveAssets();
                assertion(f,root,authored,library);
            }
            finally
            {
                // Restore/check globals only after all scopes have disposed, even on assertion.
                string full=System.IO.Path.GetFullPath(root);
                string assets=System.IO.Path.GetFullPath("Assets")+System.IO.Path.DirectorySeparatorChar;
                Assert.IsTrue(full.StartsWith(assets,StringComparison.OrdinalIgnoreCase)&&System.IO.Path.GetFileName(full)==name);
                var pending=new Stack<string>();pending.Push(full);
                while(pending.Count>0)
                {
                    string directory=pending.Pop();
                    foreach(string path in System.IO.Directory.EnumerateFileSystemEntries(directory))
                    {
                        var attributes=System.IO.File.GetAttributes(path);
                        Assert.IsFalse((attributes&System.IO.FileAttributes.ReparsePoint)!=0,"Never traverse/delete a reparse fixture");
                        if((attributes&System.IO.FileAttributes.Directory)!=0)pending.Push(path);
                    }
                }
                Assert.IsFalse((System.IO.File.GetAttributes(full)&System.IO.FileAttributes.ReparsePoint)!=0);
                Assert.IsTrue(AssetDatabase.DeleteAsset(root),"Delete exact owned logical fixture tree");
                Assert.IsFalse(System.IO.Directory.Exists(root));Assert.IsFalse(System.IO.File.Exists(root+".meta"));
                Assert.AreSame(mainAuthored,SkirmishExpansionCatalogConfig.Load());Assert.AreSame(mainBattle,SkirmishBattleCatalogConfig.Load());
                Assert.AreSame(defaultWorld,World.DefaultGameObjectInjectionWorld);
                Assert.AreEqual(ah,DraftFileHash(ap));Assert.AreEqual(bh,DraftFileHash(bp));
                Assert.AreSame(publication,mainAuthored.ToAuthoredSet().Publication);Assert.AreEqual(ph,DraftFileHash(pp));
                Assert.IsFalse(EditorUtility.IsDirty(mainAuthored));Assert.IsFalse(EditorUtility.IsDirty(mainBattle));
            }
        }
        private static void PersistFixtureDraft(Fixture f,string root,SkirmishExpansionCatalogConfig authored,SkirmishBattleCatalogConfig battle,
            Action<SkirmishScenarioDefinitionConfig,SkirmishExpansionCatalogConfig> validate)
        {
            DraftInvoke("PersistAll120LogicalAssets",f.Definition,f.Definition.MapLayout,authored,battle,root+"/Logical/Definition.asset",
                root+"/",f.Index,false,new Newtonsoft.Json.Linq.JObject(),validate);
        }
        private static void AssertPersistedFixtureQueues(SkirmishExpansionCatalogConfig authored,SkirmishBattleCatalogConfig battle,
            SkirmishScenarioDefinitionConfig definition,int index,bool throwInScope)
        {
            var normalBattle=SkirmishBattleCatalogConfig.Load();
            Assert.AreSame(Resources.Load<SkirmishBattleCatalogConfig>(SkirmishBattleCatalogConfig.ResourceName),normalBattle,
                "Caller QA scope must start from the actual normal library, without a nested override.");
            Assert.IsTrue(battle.TryGet(definition.CatalogId,out var draft));
            Assert.AreEqual(SkirmishBattleCatalogStatus.Planned,draft.Status);Assert.Less(draft.PlayableScenarioIndex,0);
            using(var scope=Game.Editor.SkirmishGenericMissionAvailabilityScope.Open(authored,definition.CatalogId,index))
            {
                var facts=(Newtonsoft.Json.Linq.JArray)DraftInvoke("AssertAll120DefinitionBindings",definition,true);
                Assert.AreEqual(12,facts.Count,"All three sizes by four actual compiler/queue seeds");
                if(throwInScope)throw new InvalidOperationException("Owned caller fixture validation failure");
            }
            Assert.AreSame(normalBattle,SkirmishBattleCatalogConfig.Load(),"The single availability scope restores the normal library.");
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error),error);
            using var rejected=new World("PersistedDraftOutsideQa");
            Assert.IsFalse(SkirmishExpandedLaunchResolver.TryCompileAndQueue(rejected.EntityManager,definition.CatalogId,
                SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,1,authored.ToAuthoredSet(),matrix,
                new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds},out _,out _,out _));
            Assert.AreEqual(0,rejected.EntityManager.UniversalQuery.CalculateEntityCount());
        }
        [Test] public void ActualLogicalAssetTransactionPersistsReloadsOnlyDraftAndDuplicateRefuses()
        {
            WithPersistentDraftTransaction((f,root,authored,battle)=>
            {
                string battleBefore=DraftFileHash(AssetDatabase.GetAssetPath(battle));
                PersistFixtureDraft(f,root,authored,battle,(definition,development)=>
                {
                    Assert.IsNull(development);Assert.IsTrue(AssetDatabase.Contains(definition));
                    Assert.IsTrue(authored.TryGetDefinition("S026",out var loaded));Assert.AreSame(definition,loaded);
                    AssertPersistedFixtureQueues(authored,battle,definition,f.Index,false);
                });
                AssetDatabase.ImportAsset(root+"/Logical/Definition.asset",ImportAssetOptions.ForceUpdate);
                var reloaded=AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(root+"/Logical/Definition.asset");
                Assert.NotNull(reloaded);Assert.AreEqual("S026",reloaded.CatalogId);
                Assert.AreEqual(3,reloaded.MissionLogicalBinding.Variants.Length);
                Assert.IsTrue(battle.TryGet("S026",out var draft));Assert.AreEqual(SkirmishBattleCatalogStatus.Planned,draft.Status);
                Assert.Less(draft.PlayableScenarioIndex,0);Assert.AreEqual(battleBefore,DraftFileHash(AssetDatabase.GetAssetPath(battle)));
                string definitionBefore=DraftFileHash(root+"/Logical/Definition.asset");
                var refusal=Assert.Throws<System.Reflection.TargetInvocationException>(()=>PersistFixtureDraft(f,root,authored,battle,(_,__)=>Assert.Fail("Duplicate must refuse before callback")));
                Assert.NotNull(refusal.InnerException);Assert.AreEqual(definitionBefore,DraftFileHash(root+"/Logical/Definition.asset"));
                Assert.AreEqual(battleBefore,DraftFileHash(AssetDatabase.GetAssetPath(battle)));
            });
        }
        [Test] public void ActualLogicalAssetTransactionThrowRollsBackSavedAssetsAndScope()
        {
            WithPersistentDraftTransaction((f,root,authored,battle)=>
            {
                string authoredBefore=DraftFileHash(AssetDatabase.GetAssetPath(authored)),battleBefore=DraftFileHash(AssetDatabase.GetAssetPath(battle));
                var failure=Assert.Throws<System.Reflection.TargetInvocationException>(()=>PersistFixtureDraft(f,root,authored,battle,
                    (definition,development)=>AssertPersistedFixtureQueues(authored,battle,definition,f.Index,true)));
                Assert.IsInstanceOf<InvalidOperationException>(failure.InnerException);
                Assert.AreEqual("Owned caller fixture validation failure",failure.InnerException.Message,"Rollback must reach the actual in-scope failure, not an earlier guard.");
                Assert.AreEqual(0,authored.Definitions.Count);
                Assert.IsNull(AssetDatabase.LoadMainAssetAtPath(root+"/Logical/Definition.asset"));
                foreach(string path in System.IO.Directory.GetFiles(root+"/Logical","*.asset",System.IO.SearchOption.AllDirectories))
                    Assert.Fail("Rollback left a logical asset: "+path);
                Assert.AreEqual(authoredBefore,DraftFileHash(AssetDatabase.GetAssetPath(authored)));
                Assert.AreEqual(battleBefore,DraftFileHash(AssetDatabase.GetAssetPath(battle)));
                using var rejected=new World("RolledBackDraftQueue");
                var config=QuickGameConfig.Defaults;config.ScenarioIndex=f.Index;
                Assert.IsFalse(SkirmishLaunchProjection.TryQueue(rejected.EntityManager,config));
                Assert.AreEqual(0,rejected.EntityManager.UniversalQuery.CalculateEntityCount());
            });
        }
        [Test] public void ActualLogicalAssetTransactionPlayableRowRefusesBeforeAnyAsset()
        {
            WithPersistentDraftTransaction((f,root,authored,battle)=>
            {
                battle.Configure(new[]{Entry(f.Definition,f.Index,true)});EditorUtility.SetDirty(battle);AssetDatabase.SaveAssets();
                string before=DraftFileHash(AssetDatabase.GetAssetPath(battle));
                Assert.Throws<System.Reflection.TargetInvocationException>(()=>PersistFixtureDraft(f,root,authored,battle,(_,__)=>Assert.Fail("Must refuse")));
                Assert.IsFalse(System.IO.Directory.Exists(root+"/Logical"));
                Assert.AreEqual(0,authored.Definitions.Count);Assert.AreEqual(before,DraftFileHash(AssetDatabase.GetAssetPath(battle)));
            });
        }
        [Test] public void ActualPublicMaterializationRejectsUnreviewedReportBeforePersistence()
        {
            string key="WARLINE_ALL120_MATERIALIZATION_REPORT";
            string previous=Environment.GetEnvironmentVariable(key);
            var authored=SkirmishExpansionCatalogConfig.Load();var battle=SkirmishBattleCatalogConfig.Load();
            string ap=AssetDatabase.GetAssetPath(authored),bp=AssetDatabase.GetAssetPath(battle);
            string ah=DraftFileHash(ap),bh=DraftFileHash(bp);
            try
            {
                Environment.SetEnvironmentVariable(key,null);
                var failure=Assert.Throws<System.Reflection.TargetInvocationException>(()=>DraftInvoke("MaterializeReviewedLogicalCandidate",false));
                Assert.NotNull(failure.InnerException);
            }
            finally{Environment.SetEnvironmentVariable(key,previous);}
            Assert.AreEqual(ah,DraftFileHash(ap));Assert.AreEqual(bh,DraftFileHash(bp));
            Assert.AreSame(authored,SkirmishExpansionCatalogConfig.Load());Assert.AreSame(battle,SkirmishBattleCatalogConfig.Load());
        }
        public static void RunLogicalDraftCallerTransactionControls()
        {
            var t=new SkirmishGenericMissionDispatchTests();
            t.ActualLogicalAssetTransactionPersistsReloadsOnlyDraftAndDuplicateRefuses();
            t.ActualLogicalAssetTransactionThrowRollsBackSavedAssetsAndScope();
            t.ActualLogicalAssetTransactionPlayableRowRefusesBeforeAnyAsset();
            t.ActualPublicMaterializationRejectsUnreviewedReportBeforePersistence();
            Debug.Log("[LogicalDraftCallerTransactionControls] result=Passed cases=4 persistedTempOnly=1 sourceFitProved=0 publicAdoptionProved=0 publicationChanged=0");
        }


        // Diagnostic collection only: observe installed Unity inline-class lifecycle.
        // No representation policy, compiler guard, publication or source-fit change.
        private static Newtonsoft.Json.Linq.JObject ObserveBaInlineAreaStage(string stage,SkirmishScenarioDefinitionConfig definition)
        {
            var facts=new Newtonsoft.Json.Linq.JObject{["stage"]=stage,["variants"]=new Newtonsoft.Json.Linq.JArray()};
            var binding=definition.MissionLogicalBinding;
            var variants=(Newtonsoft.Json.Linq.JArray)facts["variants"];
            foreach(var variant in binding.Variants)
                variants.Add(new Newtonsoft.Json.Linq.JObject{
                    ["size"]=variant.Size.ToString(),["difficulty"]=variant.Difficulty.ToString(),
                    ["areasIsNull"]=variant.Areas==null,
                    ["areasType"]=variant.Areas==null?Newtonsoft.Json.Linq.JValue.CreateNull():new Newtonsoft.Json.Linq.JValue(variant.Areas.GetType().FullName),
                    ["areasFields"]=variant.Areas==null?Newtonsoft.Json.Linq.JValue.CreateNull():Newtonsoft.Json.Linq.JObject.FromObject(variant.Areas),
                    ["reviewedAreaLogicalFingerprint"]=variant.ReviewedAreaLogicalFingerprint,
                    ["reviewedGeometryFingerprint"]=variant.ReviewedGeometryFingerprint,
                    ["reviewedViewFingerprint"]=variant.ReviewedViewFingerprint});
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error),error);
            var compiler=new Newtonsoft.Json.Linq.JArray();facts["compiler"]=compiler;
            foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
            {
                var row=new Newtonsoft.Json.Linq.JObject{["size"]=size.ToString()};compiler.Add(row);
                try
                {
                    bool accepted=SkirmishSetupCompiler.TryCompile(definition,SkirmishDifficultyId.Regular,size,155926,
                        new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out var setup,out var reasons);
                    row["setupAccepted"]=accepted;row["setupReasons"]=Newtonsoft.Json.Linq.JArray.FromObject(reasons);
                    if(accepted)
                    {
                        row["logicalAccepted"]=SkirmishMissionLogicalBindingProvider.TryCompileForLaunch(definition,setup,out var compiled,out string failure);
                        row["logicalError"]=failure;row["compiledAreasNull"]=compiled==null?Newtonsoft.Json.Linq.JValue.CreateNull():new Newtonsoft.Json.Linq.JValue(compiled.Areas==null);
                    }
                }
                catch(Exception failure){row["exceptionType"]=failure.GetType().FullName;row["exceptionMessage"]=failure.Message;}
            }
            facts["areasNullAfterCompiler"]=new Newtonsoft.Json.Linq.JArray(System.Linq.Enumerable.Select(binding.Variants,v=>v.Areas==null));
            return facts;
        }
        public static void RunBaInlineAreaSerializationCharacterization()
        {
            const string key="WARLINE_BA_INLINE_AREAS_CHARACTERIZATION_OUTPUT";
            string output=Environment.GetEnvironmentVariable(key);
            Assert.IsTrue(!string.IsNullOrEmpty(output)&&System.IO.Path.IsPathRooted(output),"Fresh absolute diagnostic output required");
            output=System.IO.Path.GetFullPath(output);
            string logs=System.IO.Path.GetFullPath("Logs")+System.IO.Path.DirectorySeparatorChar;
            Assert.IsTrue(output.StartsWith(logs,StringComparison.OrdinalIgnoreCase)&&!System.IO.File.Exists(output),"Fresh output beneath actual Logs required");
            for(string ancestor=output;!string.IsNullOrEmpty(ancestor);ancestor=System.IO.Path.GetDirectoryName(ancestor))
            {
                System.IO.FileAttributes attributes;
                try{attributes=System.IO.File.GetAttributes(ancestor);}
                catch(System.IO.FileNotFoundException){continue;}
                catch(System.IO.DirectoryNotFoundException){continue;}
                Assert.IsFalse((attributes&System.IO.FileAttributes.ReparsePoint)!=0,"Diagnostic output reparse refused");
            }
            var mainAuthored=SkirmishExpansionCatalogConfig.Load();var mainBattle=SkirmishBattleCatalogConfig.Load();
            string ap=AssetDatabase.GetAssetPath(mainAuthored),bp=AssetDatabase.GetAssetPath(mainBattle);
            string ah=DraftFileHash(ap),bh=DraftFileHash(bp);
            var publication=mainAuthored.ToAuthoredSet().Publication;
            string pp=AssetDatabase.GetAssetPath(publication),ph=DraftFileHash(pp);
            var defaultWorld=World.DefaultGameObjectInjectionWorld;
            foreach(var asset in Resources.FindObjectsOfTypeAll<ScriptableObject>())
                if(AssetDatabase.Contains(asset))Assert.IsFalse(EditorUtility.IsDirty(asset),"Refuse SaveAssets with unrelated dirty object");
            string name="__BaInlineAreaCharacterization_"+Guid.NewGuid().ToString("N"),root="Assets/"+name;
            Assert.IsFalse(AssetDatabase.IsValidFolder(root));
            var owned=new List<UnityEngine.Object>();bool rootOwned=false;
            var report=new Newtonsoft.Json.Linq.JObject{["schema_version"]=1,["scope"]="InstalledUnityBaInlineAreasCharacterization",
                ["unityVersion"]=Application.unityVersion,["representationPolicyChanged"]=false,["sourceFitCertified"]=false,["missionAccepted"]=false};
            var stages=new Newtonsoft.Json.Linq.JArray();report["stages"]=stages;
            var fixture=new Fixture(published:false,activateScopes:false);
            Exception lifecycleFailure=null;
            try
            {
                Assert.AreEqual(SkirmishObjectiveKind.BaseAssault,fixture.Definition.ObjectiveConfig.Kind);
                stages.Add(ObserveBaInlineAreaStage("OriginalFactory",fixture.Definition));
                var original=fixture.Definition.MissionLogicalBinding.Variants[0];
                var variants=new List<SkirmishMissionLogicalVariant>();
                foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
                {
                    var variant=JsonUtility.FromJson<SkirmishMissionLogicalVariant>(JsonUtility.ToJson(original));
                    variant.View=original.View;variant.Size=size;
                    variant.ReviewedViewFingerprint=SkirmishMissionLogicalBindingCompiler.ViewFingerprint(variant);variants.Add(variant);
                }
                fixture.Definition.MissionLogicalBinding.Variants=variants.ToArray();
                stages.Add(ObserveBaInlineAreaStage("JsonCloneBeforeNullReset",fixture.Definition));
                foreach(var variant in variants)if(original.Areas==null)variant.Areas=null;
                stages.Add(ObserveBaInlineAreaStage("JsonCloneAfterNullReset",fixture.Definition));
                AssetDatabase.CreateFolder("Assets",name);rootOwned=AssetDatabase.IsValidFolder(root);
                Assert.IsTrue(rootOwned);
                var fixtureAuthored=UnityEngine.Object.Instantiate(fixture.Authored);owned.Add(fixtureAuthored);
                var fixtureLibrary=UnityEngine.Object.Instantiate(fixture.Library);owned.Add(fixtureLibrary);
                var fixtureFields=new SerializedObject(fixtureAuthored);
                fixtureFields.FindProperty("definitions").arraySize=0;fixtureFields.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(fixtureAuthored,root+"/Authored.asset");AssetDatabase.CreateAsset(fixtureLibrary,root+"/Battle.asset");
                AssetDatabase.SaveAssets();
                stages.Add(ObserveBaInlineAreaStage("FixtureCatalogSaveAssets",fixture.Definition));
                var definition=UnityEngine.Object.Instantiate(fixture.Definition);owned.Add(definition);
                var layout=UnityEngine.Object.Instantiate(fixture.Definition.MapLayout);owned.Add(layout);
                var objective=UnityEngine.Object.Instantiate(fixture.Definition.ObjectiveConfig);owned.Add(objective);
                var binding=UnityEngine.Object.Instantiate(fixture.Definition.MissionLogicalBinding);owned.Add(binding);
                definition.BindMissionLogicalBinding(binding);
                stages.Add(ObserveBaInlineAreaStage("InstantiateBeforeNullReset",definition));
                for(int i=0;i<binding.Variants.Length;i++)
                    if(fixture.Definition.MissionLogicalBinding.Variants[i].Areas==null)binding.Variants[i].Areas=null;
                stages.Add(ObserveBaInlineAreaStage("InstantiateAfterNullReset",definition));
                var fields=new SerializedObject(definition);
                fields.FindProperty("mapLayout").objectReferenceValue=layout;
                fields.FindProperty("objectiveConfig").objectReferenceValue=objective;
                fields.FindProperty("missionLogicalBinding").objectReferenceValue=binding;
                fields.ApplyModifiedPropertiesWithoutUndo();
                stages.Add(ObserveBaInlineAreaStage("SerializedObjectApply",definition));
                AssetDatabase.CreateAsset(layout,root+"/Layout.asset");AssetDatabase.CreateAsset(objective,root+"/Objective.asset");
                foreach(var variant in binding.Variants)
                {
                    var view=UnityEngine.Object.Instantiate(variant.View);owned.Add(view);variant.View=view;
                    AssetDatabase.CreateAsset(view,root+"/View_"+variant.Size+".asset");
                }
                string bindingPath=root+"/Binding.asset",definitionPath=root+"/Definition.asset";
                AssetDatabase.CreateAsset(binding,bindingPath);AssetDatabase.CreateAsset(definition,definitionPath);
                stages.Add(ObserveBaInlineAreaStage("CreateAssets",definition));
                EditorUtility.SetDirty(binding);EditorUtility.SetDirty(definition);AssetDatabase.SaveAssets();
                stages.Add(ObserveBaInlineAreaStage("SaveAssets",definition));
                report["savedBindingSha256"]=DraftFileHash(bindingPath);report["savedDefinitionSha256"]=DraftFileHash(definitionPath);
                AssetDatabase.ImportAsset(bindingPath,ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(definitionPath,ImportAssetOptions.ForceUpdate);
                stages.Add(ObserveBaInlineAreaStage("ImportAssets",definition));
                Resources.UnloadAsset(definition);Resources.UnloadAsset(binding);
                var reloaded=AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(definitionPath);
                Assert.NotNull(reloaded);Assert.IsTrue(AssetDatabase.Contains(reloaded));
                Assert.AreEqual(bindingPath,AssetDatabase.GetAssetPath(reloaded.MissionLogicalBinding));
                stages.Add(ObserveBaInlineAreaStage("ForcedReload",reloaded));
                Assert.AreEqual(11,stages.Count);
            }
            catch(Exception failure){lifecycleFailure=failure;report["lifecycleFailure"]=failure.ToString();}
            finally
            {
                if(rootOwned)
                {
                    string full=System.IO.Path.GetFullPath(root),assets=System.IO.Path.GetFullPath("Assets")+System.IO.Path.DirectorySeparatorChar;
                    Assert.IsTrue(full.StartsWith(assets,StringComparison.OrdinalIgnoreCase)&&System.IO.Path.GetFileName(full)==name);
                    var pending=new Stack<string>();pending.Push(full);
                    while(pending.Count>0)
                    {
                        string directory=pending.Pop();
                        Assert.IsFalse((System.IO.File.GetAttributes(directory)&System.IO.FileAttributes.ReparsePoint)!=0);
                        foreach(string path in System.IO.Directory.EnumerateFileSystemEntries(directory))
                        {
                            var attributes=System.IO.File.GetAttributes(path);
                            Assert.IsFalse((attributes&System.IO.FileAttributes.ReparsePoint)!=0,"Preserve reparse fixture for investigation");
                            if((attributes&System.IO.FileAttributes.Directory)!=0)pending.Push(path);
                        }
                    }
                    Assert.IsTrue(AssetDatabase.DeleteAsset(root));Assert.IsFalse(System.IO.Directory.Exists(root));
                    Assert.IsFalse(System.IO.File.Exists(root+".meta"));
                }
                foreach(var value in owned)if(value!=null&&!AssetDatabase.Contains(value))UnityEngine.Object.DestroyImmediate(value);
                fixture.Dispose();
                Assert.AreSame(mainAuthored,SkirmishExpansionCatalogConfig.Load());Assert.AreSame(mainBattle,SkirmishBattleCatalogConfig.Load());
                Assert.AreSame(defaultWorld,World.DefaultGameObjectInjectionWorld);
                Assert.AreEqual(ah,DraftFileHash(ap));Assert.AreEqual(bh,DraftFileHash(bp));Assert.AreEqual(ph,DraftFileHash(pp));
                Assert.AreSame(publication,mainAuthored.ToAuthoredSet().Publication);
                Assert.IsFalse(EditorUtility.IsDirty(mainAuthored));Assert.IsFalse(EditorUtility.IsDirty(mainBattle));
            }
            report["cleanupCompleted"]=true;report["allStagesObserved"]=stages.Count==11&&lifecycleFailure==null;
            using(var file=new System.IO.FileStream(output,System.IO.FileMode.CreateNew,System.IO.FileAccess.Write,System.IO.FileShare.Read))
            using(var writer=new System.IO.StreamWriter(file,new System.Text.UTF8Encoding(false)))writer.Write(report.ToString(Newtonsoft.Json.Formatting.Indented));
            if(lifecycleFailure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(lifecycleFailure).Throw();
            Debug.Log("[BaInlineAreaSerializationCharacterization] result=Collected stages=11 sizeChecks=33 guardUnchanged=1 sourceFitCertified=0 missionReady=0");
        }

        private static void AssertManagedAreaFixtureCompiles(SkirmishScenarioDefinitionConfig definition)
        {
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error),error);
            Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition,SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,104729,
                new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out var setup,out var reasons),string.Join(";",reasons));
            Assert.IsTrue(SkirmishMissionLogicalBindingProvider.TryCompileForLaunch(definition,setup,out _,out error),error);
        }
        private static void DeleteManagedAreaFixture(string root,string name)
        {
            string full=System.IO.Path.GetFullPath(root),assets=System.IO.Path.GetFullPath("Assets")+System.IO.Path.DirectorySeparatorChar;
            Assert.IsTrue(full.StartsWith(assets,StringComparison.OrdinalIgnoreCase)&&System.IO.Path.GetFileName(full)==name);
            var pending=new Stack<string>();pending.Push(full);
            while(pending.Count>0)
            {
                string directory=pending.Pop();
                Assert.IsFalse((System.IO.File.GetAttributes(directory)&System.IO.FileAttributes.ReparsePoint)!=0);
                foreach(string path in System.IO.Directory.EnumerateFileSystemEntries(directory))
                {
                    var attributes=System.IO.File.GetAttributes(path);
                    Assert.IsFalse((attributes&System.IO.FileAttributes.ReparsePoint)!=0);
                    if((attributes&System.IO.FileAttributes.Directory)!=0)pending.Push(path);
                }
            }
            Assert.IsTrue(AssetDatabase.DeleteAsset(root));Assert.IsFalse(System.IO.Directory.Exists(root));
            Assert.IsFalse(System.IO.File.Exists(root+".meta"));
        }
        private static int RunNonBaManagedAreaRoundtripControls()
        {
            int controls=0;
            SkirmishMissionLogicalBindingNativeTestFixture.WithActiveFrontline((em,session)=>
            {
                Assert.IsTrue(SkirmishExpansionCatalogConfig.Load().TryGetDefinition("S031",out var definition));
                var original=definition.MissionLogicalBinding;Assert.AreEqual(1,original.Variants.Length);
                var area=original.Variants[0].Areas;Assert.NotNull(area);
                string expected=JsonUtility.ToJson(area),fingerprint=original.Variants[0].ReviewedAreaLogicalFingerprint;
                string geometry=original.Variants[0].ReviewedGeometryFingerprint;
                var decoded=(SkirmishObjectiveAreaConfig)DraftInvoke("ReadAll120LogicalAreas",Newtonsoft.Json.Linq.JObject.Parse(expected));
                Assert.AreEqual(expected,JsonUtility.ToJson(decoded));controls++;
                var clone=UnityEngine.Object.Instantiate(original);
                string name="__ManagedAreaRoundtrip_"+Guid.NewGuid().ToString("N"),root="Assets/"+name;
                SkirmishMissionViewProfile view=null;bool rootOwned=false;
                try
                {
                    Assert.NotNull(clone.Variants[0].Areas);Assert.AreNotSame(area,clone.Variants[0].Areas);Assert.AreEqual(expected,JsonUtility.ToJson(clone.Variants[0].Areas));
                    Assert.AreEqual(fingerprint,clone.Variants[0].ReviewedAreaLogicalFingerprint);Assert.AreEqual(geometry,clone.Variants[0].ReviewedGeometryFingerprint);
                    clone.Variants[0].Areas=decoded;definition.BindMissionLogicalBinding(clone);
                    AssertManagedAreaFixtureCompiles(definition);controls++;
                    var fields=new SerializedObject(definition);fields.FindProperty("missionLogicalBinding").objectReferenceValue=clone;
                    fields.ApplyModifiedPropertiesWithoutUndo();AssertManagedAreaFixtureCompiles(definition);
                    Assert.AreEqual(expected,JsonUtility.ToJson(clone.Variants[0].Areas));controls++;
                    foreach(var asset in Resources.FindObjectsOfTypeAll<ScriptableObject>())
                        if(AssetDatabase.Contains(asset))Assert.IsFalse(EditorUtility.IsDirty(asset),"Refuse SaveAssets with unrelated dirty object");
                    Assert.IsFalse(AssetDatabase.IsValidFolder(root));AssetDatabase.CreateFolder("Assets",name);rootOwned=AssetDatabase.IsValidFolder(root);Assert.IsTrue(rootOwned);
                    view=UnityEngine.Object.Instantiate(clone.Variants[0].View);clone.Variants[0].View=view;
                    AssetDatabase.CreateAsset(view,root+"/View.asset");string path=root+"/Binding.asset";
                    AssetDatabase.CreateAsset(clone,path);EditorUtility.SetDirty(clone);AssetDatabase.SaveAssets();
                    Assert.AreEqual(expected,JsonUtility.ToJson(clone.Variants[0].Areas));
                    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                    definition.BindMissionLogicalBinding(original);Resources.UnloadAsset(clone);
                    clone=AssetDatabase.LoadAssetAtPath<SkirmishMissionLogicalBindingConfig>(path);Assert.NotNull(clone);
                    Assert.AreEqual(expected,JsonUtility.ToJson(clone.Variants[0].Areas));
                    Assert.AreEqual(fingerprint,clone.Variants[0].ReviewedAreaLogicalFingerprint);Assert.AreEqual(geometry,clone.Variants[0].ReviewedGeometryFingerprint);
                    definition.BindMissionLogicalBinding(clone);AssertManagedAreaFixtureCompiles(definition);controls++;
                }
                finally
                {
                    definition.BindMissionLogicalBinding(original);
                    if(rootOwned)DeleteManagedAreaFixture(root,name);
                    if(clone!=null&&!AssetDatabase.Contains(clone))UnityEngine.Object.DestroyImmediate(clone);
                    if(view!=null&&!AssetDatabase.Contains(view))UnityEngine.Object.DestroyImmediate(view);
                }
            });
            return controls;
        }
        private static int RunBaManagedAreaStrictRefusals()
        {
            using var fixture=new Fixture(published:false,activateScopes:false);
            var definition=fixture.Definition;var variant=definition.MissionLogicalBinding.Variants[0];
            Assert.IsNull(variant.Areas);AssertManagedAreaFixtureCompiles(definition);
            var changes=new List<Action<SkirmishObjectiveAreaConfig>>{
                a=>{},a=>a.SchemaVersion=2,a=>a.AreaSetId="unreviewed",a=>a.ObjectiveKind=SkirmishObjectiveKind.FrontlineControl,
                a=>a.Binding.DefinitionId="x",a=>a.Binding.SourceConfigId="x",a=>a.Binding.SourceAssetGuid="x",
                a=>a.Binding.SourceContentHash="x",a=>a.Binding.SourceSemanticFingerprint="x",a=>a.Binding.LayoutId="x",
                a=>a.Binding.LayoutBindingFingerprint="x",a=>a.Binding.SourceMinX=1,a=>a.Binding.SourceMinZ=1,
                a=>a.Binding.SourceMaxX=1,a=>a.Binding.SourceMaxZ=1,a=>a.Areas=new[]{new SkirmishObjectiveArea()},
                a=>a.Measurement.ArtifactId="x",a=>a.Measurement.ArtifactSha256="x",a=>a.Measurement.GeometryFingerprint="x",
                a=>a.SchemaVersion=0,a=>a.Binding.SourceMinX=float.NaN};
            int controls=0;
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error),error);
            void Refuses()
            {
                Assert.IsFalse(SkirmishSetupCompiler.TryCompile(definition,SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,104729,
                    new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out _,out var reasons));
                Assert.IsTrue(System.Linq.Enumerable.Any(reasons,r=>r.Detail=="Generic BA view binding must not invent non-BA typed areas."));
                controls++;
            }
            foreach(var change in changes){variant.Areas=new SkirmishObjectiveAreaConfig();change(variant.Areas);Refuses();}
            variant.Areas=null;variant.ReviewedAreaLogicalFingerprint=new string('a',64);Refuses();variant.ReviewedAreaLogicalFingerprint=null;
            variant.ReviewedGeometryFingerprint=new string('b',64);Refuses();variant.ReviewedGeometryFingerprint=null;
            Assert.IsNull(variant.Areas);AssertManagedAreaFixtureCompiles(definition);Assert.AreEqual(23,controls);return controls;
        }
        private sealed class ManagedBindingInventoryRow
        {
            public string Guid,Path,AssetSha,MetaSha;
        }
        private static void AssertManagedInventoryPath(string path)
        {
            string full=System.IO.Path.GetFullPath(path),assets=System.IO.Path.GetFullPath("Assets");
            Assert.IsTrue(full.StartsWith(assets+System.IO.Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"Binding path must be under actual Assets");
            string cursor=full;
            while(true)
            {
                var flags=System.IO.File.GetAttributes(cursor);
                Assert.IsFalse((flags&System.IO.FileAttributes.ReparsePoint)!=0,"Binding inventory refuses reparse: "+cursor);
                if(string.Equals(cursor,assets,StringComparison.OrdinalIgnoreCase))break;
                cursor=System.IO.Path.GetDirectoryName(cursor);Assert.IsNotNull(cursor);
            }
            Assert.IsFalse((System.IO.File.GetAttributes(full)&System.IO.FileAttributes.Directory)!=0);
        }
        private static List<ManagedBindingInventoryRow> CaptureManagedBindingInventory()
        {
            string[] guids=AssetDatabase.FindAssets("t:SkirmishMissionLogicalBindingConfig");Array.Sort(guids,StringComparer.Ordinal);
            var rows=new List<ManagedBindingInventoryRow>();var paths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(string guid in guids)
            {
                Assert.IsTrue(!string.IsNullOrEmpty(guid)&&ids.Add(guid),"Duplicate/empty binding GUID");
                string path=AssetDatabase.GUIDToAssetPath(guid);
                Assert.IsTrue(!string.IsNullOrEmpty(path)&&paths.Add(System.IO.Path.GetFullPath(path)),"Duplicate/empty binding path");
                Assert.AreEqual(guid,AssetDatabase.AssetPathToGUID(path));
                AssertManagedInventoryPath(path);AssertManagedInventoryPath(path+".meta");
                var binding=AssetDatabase.LoadAssetAtPath<SkirmishMissionLogicalBindingConfig>(path);
                Assert.NotNull(binding);Assert.IsTrue(AssetDatabase.Contains(binding));
                Assert.AreEqual(path,AssetDatabase.GetAssetPath(binding));
                foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    Assert.IsFalse(EditorUtility.IsDirty(asset),"Inventory refuses dirty existing binding asset: "+path);
                rows.Add(new ManagedBindingInventoryRow{Guid=guid,Path=path,AssetSha=DraftFileHash(path),MetaSha=DraftFileHash(path+".meta")});
            }
            string[] end=AssetDatabase.FindAssets("t:SkirmishMissionLogicalBindingConfig");Array.Sort(end,StringComparer.Ordinal);
            CollectionAssert.AreEqual(guids,end,"Binding inventory changed during observation");
            return rows;
        }
        private static void AssertManagedBindingInventoryEqual(List<ManagedBindingInventoryRow> before,List<ManagedBindingInventoryRow> after)
        {
            Assert.AreEqual(before.Count,after.Count,"Binding inventory membership changed");
            for(int i=0;i<before.Count;i++)
            {
                Assert.AreEqual(before[i].Guid,after[i].Guid,"Binding GUID changed");
                Assert.AreEqual(before[i].Path,after[i].Path,"Binding path changed");
                Assert.AreEqual(before[i].AssetSha,after[i].AssetSha,"Binding bytes changed");
                Assert.AreEqual(before[i].MetaSha,after[i].MetaSha,"Binding metadata bytes changed");
            }
        }
        // Original migration proof remains zero-existing-assets only.
        public static void RunBaManagedAreaSerializationValidation(){RunManagedAreaSerializationCore(true);}
        // Preservation controls do not certify existing assets' migration or gameplay validity.
        public static void RunManagedAreaRepeatableSerializationValidation(){RunManagedAreaSerializationCore(false);}
        public static void RunManagedAreaRepeatableInventoryControls()
        {
            var original=CaptureManagedBindingInventory();int controls=0;
            using var fixture=new Fixture(published:false,activateScopes:false);
            var binding=UnityEngine.Object.Instantiate(fixture.Definition.MissionLogicalBinding);
            string name="ManagedAreaInventory_"+Guid.NewGuid().ToString("N"),root="Assets/"+name;
            string path=root+"/Binding.asset";bool owned=false;
            try
            {
                foreach(var asset in Resources.FindObjectsOfTypeAll<ScriptableObject>())
                    if(AssetDatabase.Contains(asset))Assert.IsFalse(EditorUtility.IsDirty(asset),"Refuse SaveAssets with unrelated dirty object");
                Assert.IsFalse(AssetDatabase.IsValidFolder(root));AssetDatabase.CreateFolder("Assets",name);
                owned=AssetDatabase.IsValidFolder(root);Assert.IsTrue(owned);
                // Persistent references must not point at disposable non-persistent fixture views.
                var view=UnityEngine.Object.Instantiate(binding.Variants[0].View);
                AssetDatabase.CreateAsset(view,root+"/View.asset");binding.Variants[0].View=view;
                AssetDatabase.CreateAsset(binding,path);AssetDatabase.SaveAssets();
                var baseline=CaptureManagedBindingInventory();Assert.AreEqual(original.Count+1,baseline.Count);
                Assert.Throws<AssertionException>(()=>RunBaManagedAreaSerializationValidation());controls++;
                RunManagedAreaRepeatableSerializationValidation();
                AssertManagedBindingInventoryEqual(baseline,CaptureManagedBindingInventory());controls++;
                byte[] raw=System.IO.File.ReadAllBytes(path);
                try
                {
                    System.IO.File.AppendAllText(path,"\n");
                    Assert.Throws<AssertionException>(()=>AssertManagedBindingInventoryEqual(baseline,CaptureManagedBindingInventory()));controls++;
                }
                finally{System.IO.File.WriteAllBytes(path,raw);}
                byte[] meta=System.IO.File.ReadAllBytes(path+".meta");
                try
                {
                    System.IO.File.AppendAllText(path+".meta","\n");
                    Assert.Throws<AssertionException>(()=>AssertManagedBindingInventoryEqual(baseline,CaptureManagedBindingInventory()));controls++;
                }
                finally{System.IO.File.WriteAllBytes(path+".meta",meta);}
                EditorUtility.SetDirty(binding);
                Assert.Throws<AssertionException>(()=>CaptureManagedBindingInventory());controls++;
                // This is the owned fixture only; no SaveAssets call after marking dirty.
                EditorUtility.ClearDirty(binding);
                var extra=UnityEngine.Object.Instantiate(binding);string extraPath=root+"/Extra.asset";
                AssetDatabase.CreateAsset(extra,extraPath);AssetDatabase.SaveAssets();
                Assert.Throws<AssertionException>(()=>AssertManagedBindingInventoryEqual(baseline,CaptureManagedBindingInventory()));controls++;
                Assert.IsTrue(AssetDatabase.DeleteAsset(extraPath));
                AssertManagedBindingInventoryEqual(baseline,CaptureManagedBindingInventory());
                string moved=root+"/Moved.asset";Assert.AreEqual("",AssetDatabase.MoveAsset(path,moved));
                Assert.Throws<AssertionException>(()=>AssertManagedBindingInventoryEqual(baseline,CaptureManagedBindingInventory()));controls++;
                Assert.AreEqual("",AssetDatabase.MoveAsset(moved,path));
                AssertManagedBindingInventoryEqual(baseline,CaptureManagedBindingInventory());
                Assert.IsTrue(AssetDatabase.DeleteAsset(path));binding=null;
                Assert.Throws<AssertionException>(()=>AssertManagedBindingInventoryEqual(baseline,CaptureManagedBindingInventory()));controls++;
            }
            finally
            {
                if(owned)DeleteManagedAreaFixture(root,name);
                if(binding!=null&&!AssetDatabase.Contains(binding))UnityEngine.Object.DestroyImmediate(binding);
                AssertManagedBindingInventoryEqual(original,CaptureManagedBindingInventory());
            }
            Assert.AreEqual(8,controls);
            Debug.Log("[ManagedAreaRepeatableInventoryControls] result=Passed controls=8 existingAssetsMigrationCertified=0 sourceFitCertified=0 missionReady=0");
        }

        private static void RunManagedAreaSerializationCore(bool migration)
        {
            var inventory=migration?null:CaptureManagedBindingInventory();
            try
            {
                // No pre-existing inline binding asset may silently become a managed-reference asset.
                if(migration)Assert.AreEqual(0,AssetDatabase.FindAssets("t:SkirmishMissionLogicalBindingConfig").Length,
                    "Existing binding assets require separately reviewed migration before this repair");
                var authoredBefore=SkirmishExpansionCatalogConfig.Load();var battleBefore=SkirmishBattleCatalogConfig.Load();
                var defaultBefore=World.DefaultGameObjectInjectionWorld;
                string authoredPath=AssetDatabase.GetAssetPath(authoredBefore),battlePath=AssetDatabase.GetAssetPath(battleBefore);
                string authoredHash=DraftFileHash(authoredPath),battleHash=DraftFileHash(battlePath);
                RunBaInlineAreaSerializationCharacterization();
                string output=Environment.GetEnvironmentVariable("WARLINE_BA_INLINE_AREAS_CHARACTERIZATION_OUTPUT");
                var report=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(output));
                Assert.IsTrue((bool)report["cleanupCompleted"]&&(bool)report["allStagesObserved"]);
                var stages=(Newtonsoft.Json.Linq.JArray)report["stages"];Assert.AreEqual(11,stages.Count);
                foreach(Newtonsoft.Json.Linq.JObject stage in stages)
                {
                    string phase=(string)stage["stage"];
                    foreach(Newtonsoft.Json.Linq.JObject variant in stage["variants"])
                        if(phase!="JsonCloneBeforeNullReset")Assert.IsTrue((bool)variant["areasIsNull"],phase);
                    foreach(Newtonsoft.Json.Linq.JObject row in stage["compiler"])
                    {
                        if(phase=="OriginalFactory"&&(string)row["size"]!="Standard")continue;
                        if(phase=="JsonCloneBeforeNullReset"&&System.Linq.Enumerable.Any(stage["variants"],v=>(bool)v["areasIsNull"]==false))
                        {Assert.IsFalse((bool)row["setupAccepted"]);continue;}
                        Assert.IsTrue((bool)row["setupAccepted"]&&(bool)row["logicalAccepted"]&&(bool)row["compiledAreasNull"],phase);
                    }
                }
                Assert.IsNull(DraftInvoke("ReadAll120LogicalAreas",new object[]{null}));
                Assert.IsNull(DraftInvoke("ReadAll120LogicalAreas",Newtonsoft.Json.Linq.JValue.CreateNull()));
                foreach(Newtonsoft.Json.Linq.JToken invalid in new Newtonsoft.Json.Linq.JToken[]{new Newtonsoft.Json.Linq.JArray(),new Newtonsoft.Json.Linq.JValue("not an area object")})
                {
                    var failure=Assert.Throws<System.Reflection.TargetInvocationException>(()=>DraftInvoke("ReadAll120LogicalAreas",invalid));
                    Assert.IsInstanceOf<InvalidOperationException>(failure.InnerException);
                }
                Assert.AreEqual(4,RunNonBaManagedAreaRoundtripControls());Assert.AreEqual(23,RunBaManagedAreaStrictRefusals());
                if(migration)Assert.AreEqual(0,AssetDatabase.FindAssets("t:SkirmishMissionLogicalBindingConfig").Length);
                Assert.AreSame(authoredBefore,SkirmishExpansionCatalogConfig.Load());Assert.AreSame(battleBefore,SkirmishBattleCatalogConfig.Load());
                Assert.AreSame(defaultBefore,World.DefaultGameObjectInjectionWorld);
                Assert.AreEqual(authoredHash,DraftFileHash(authoredPath));Assert.AreEqual(battleHash,DraftFileHash(battlePath));
            }
            finally
            {
                if(!migration)
                {
                    var after=CaptureManagedBindingInventory();AssertManagedBindingInventoryEqual(inventory,after);
                    Debug.Log("[ManagedAreaBindingInventory] preserved=1 dirty=0 before="+
                        Newtonsoft.Json.Linq.JArray.FromObject(inventory).ToString(Newtonsoft.Json.Formatting.None)+" after="+
                        Newtonsoft.Json.Linq.JArray.FromObject(after).ToString(Newtonsoft.Json.Formatting.None));
                }
            }
            if(migration)Debug.Log("[BaManagedAreaSerializationControls] result=Passed baPhases=11 nonBaRoundtrips=4 strictRefusals=23 reportReaders=4 sourceFitCertified=0 missionReady=0");
            else Debug.Log("[ManagedAreaRepeatableSerializationControls] result=Passed baPhases=11 nonBaRoundtrips=4 strictRefusals=23 reportReaders=4 existingAssetsMigrationCertified=0 sourceFitCertified=0 missionReady=0");
        }

        public static void RunBaManagedAreaMaterializationFocusedValidation()
        {
            RunBaManagedAreaSerializationValidation();
            RunLogicalDraftCallerTransactionControls();
            Game.Editor.SkirmishGroundSourceFitSurvey.RunRequestedDurableMissionSerializationControls();
            Game.Editor.SkirmishGroundSourceFitSurvey.RunRefinementHullRoundtripControls();
            Debug.Log("[BaManagedAreaMaterializationFocusedControls] result=Passed baPhases=11 nonBaRoundtrips=4 strictRefusals=23 reportReaders=4 callerTransactions=4 serialization=8 hull=12 sourceFitCertified=0 missionReady=0");
        }


        // Diagnostic only: no menu input, LaunchMatch, publication or gameplay.
        private IDisposable availabilityLifecycleScope;
        private UnityEditor.SceneManagement.SceneSetup[] availabilityLifecycleScenes;
        private UnityEditor.SceneManagement.SceneSetup[] availabilityLifecycleSavedScenes;
        private bool availabilityLifecycleUtfEmptyBaseline,availabilityLifecycleMeasuredDefaultBootstrap;
        private Newtonsoft.Json.Linq.JObject availabilityLifecycleOriginalSceneStructure;
        private bool availabilityLifecycleStarted, availabilityLifecycleOptionsEnabled, availabilityLifecycleBackground;
        private EnterPlayModeOptions availabilityLifecycleOptions;
        private string availabilityLifecycleSaveRoot, availabilityLifecycleLocale, availabilityLifecycleOutput;
        private SkirmishBattleCatalogConfig availabilityLifecycleLibrary;
        private SkirmishExpansionCatalogConfig availabilityLifecycleAuthored;
        private string availabilityLifecycleLibraryJson, availabilityLifecycleAuthoredJson;
        private byte[] availabilityLifecycleLibraryBytes, availabilityLifecycleAuthoredBytes;
        private readonly Newtonsoft.Json.Linq.JArray availabilityLifecycleObservations=new Newtonsoft.Json.Linq.JArray();
        private bool availabilityLifecycleCyclesCompleted;
        private static string AvailabilityLifecycleOwnedOutput(string value)
        {
            Assert.IsTrue(System.IO.Path.IsPathRooted(value),"Absolute owned output required.");
            foreach(string part in value.Split(new[]{'\\','/'},StringSplitOptions.RemoveEmptyEntries))
                Assert.IsTrue(part!=".."&&part!=".","Lexical output aliases refused.");
            string full=System.IO.Path.GetFullPath(value);
            string logs=System.IO.Path.GetFullPath("Logs/SkirmishProduction")+System.IO.Path.DirectorySeparatorChar;
            Assert.IsTrue(full.StartsWith(logs,StringComparison.OrdinalIgnoreCase),"Output must remain in owned Logs.");
            string cursor=full;
            while(!string.IsNullOrEmpty(cursor))
            {
                if(System.IO.File.Exists(cursor)||System.IO.Directory.Exists(cursor))
                    Assert.IsFalse((System.IO.File.GetAttributes(cursor)&System.IO.FileAttributes.ReparsePoint)!=0,"Reparse ancestry refused: "+cursor);
                cursor=System.IO.Path.GetDirectoryName(cursor);
            }
            Assert.IsTrue(System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(full)),"Existing ordinary output parent required.");
            Assert.IsFalse(System.IO.File.Exists(full)||System.IO.Directory.Exists(full),"Fresh output required.");
            return full;
        }
        private static void AvailabilityLifecycleCleanScenes()
        {
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            {
                var scene=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                Assert.IsFalse(scene.isDirty,"Refuse to discard dirty scene: "+scene.path);
                Assert.IsNotEmpty(scene.path,"A saved, restorable scene baseline is required.");
            }
        }
        private static Newtonsoft.Json.Linq.JObject AvailabilityLifecycleSceneStructure(Newtonsoft.Json.Linq.JObject inventory)
        {
            var sceneOnly=new Newtonsoft.Json.Linq.JObject();
            foreach(string key in new[]{"savedSetup","sceneManagerCount","scenes","truncated","rootInventoryOnly","childrenTraversed"})
                sceneOnly[key]=inventory[key].DeepClone();
            // Canonical JSON roundtrip compares serialized float witnesses to the actual measured JSON without widening tolerances.
            return Newtonsoft.Json.Linq.JObject.Parse(sceneOnly.ToString(Newtonsoft.Json.Formatting.None));
        }
        private static Newtonsoft.Json.Linq.JObject AvailabilityLifecycleMeasuredBootstrapStructure()
        {
            // Exact scene-only projection of measured17264/e796df27; terminal acceptance is an external launch prerequisite.
            return Newtonsoft.Json.Linq.JObject.Parse(@"{""savedSetup"":[],""sceneManagerCount"":1,""scenes"":[{""index"":0,""path"":"""",""name"":"""",""loaded"":true,""dirty"":false,""active"":true,""rootCount"":2,""roots"":[{""index"":0,""name"":""Main Camera"",""tag"":""MainCamera"",""activeSelf"":true,""activeInHierarchy"":true,""layer"":0,""hideFlags"":""None"",""localPosition"":[0.0,1.0,-10.0],""localRotation"":[0.0,0.0,0.0,1.0],""localScale"":[1.0,1.0,1.0],""childCount"":0,""componentCount"":3,""componentTypes"":[""UnityEngine.Transform, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"",""UnityEngine.Camera, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"",""UnityEngine.AudioListener, UnityEngine.AudioModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""]},{""index"":1,""name"":""Directional Light"",""tag"":""Untagged"",""activeSelf"":true,""activeInHierarchy"":true,""layer"":0,""hideFlags"":""None"",""localPosition"":[0.0,3.0,0.0],""localRotation"":[0.408217877,-0.234569684,0.109381631,0.8754261],""localScale"":[1.0,1.0,1.0],""childCount"":0,""componentCount"":2,""componentTypes"":[""UnityEngine.Transform, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"",""UnityEngine.Light, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""]}]}],""truncated"":false,""rootInventoryOnly"":true,""childrenTraversed"":false}");
        }
        private static void AvailabilityLifecycleRequireMeasuredDefaultBootstrap()
        {
            var actual=AvailabilityLifecycleSceneStructure(AvailabilityUtfReadBootstrapInventory());
            Assert.IsTrue(Newtonsoft.Json.Linq.JToken.DeepEquals(AvailabilityLifecycleMeasuredBootstrapStructure(),actual),
                "Exact measured clean single unnamed UTF DefaultGameObjects structure required; no arbitrary unsaved scene may be discarded.");
        }
        private static void AvailabilityLifecycleRequireOwnedCycleScene()
        {
            if(UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup().Length==0)
            {
                AvailabilityLifecycleRequireMeasuredDefaultBootstrap();
                return;
            }
            AvailabilityLifecycleCleanScenes();
            Assert.AreEqual(1,UnityEngine.SceneManagement.SceneManager.sceneCount,"Only the owned cycle Menu scene may be replaced.");
            Assert.AreEqual("Assets/Game/Scenes/Menu.unity",UnityEngine.SceneManagement.SceneManager.GetSceneAt(0).path);
        }
        private static void AvailabilityLifecycleRecreateMeasuredDefaultBootstrap()
        {
            AvailabilityLifecycleRequireOwnedCycleScene();
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,UnityEditor.SceneManagement.NewSceneMode.Single);
            AvailabilityLifecycleRequireMeasuredDefaultBootstrap();
        }
        private void AvailabilityLifecycleWriteRawSceneBaseline(Newtonsoft.Json.Linq.JObject inventory)
        {
            var report=new Newtonsoft.Json.Linq.JObject {
                ["schema"]=1,["scope"]="S074LifecycleRawSceneBaselineBeforeGuards_NoConclusion",["inventory"]=inventory,
                ["lifecycleStagesCollected"]=0,["productionPatchApplied"]=false,["normalMissionPassed"]=false };
            Debug.Log("[S074LifecycleRawSceneBaseline] "+report.ToString(Newtonsoft.Json.Formatting.None));
            string path=AvailabilityLifecycleOwnedOutput(availabilityLifecycleOutput+".baseline-before.json");
            byte[] bytes=System.Text.Encoding.UTF8.GetBytes(report.ToString(Newtonsoft.Json.Formatting.Indented));
            Assert.LessOrEqual(bytes.Length,4*1024*1024);
            using(var file=new System.IO.FileStream(path,System.IO.FileMode.CreateNew,System.IO.FileAccess.Write,System.IO.FileShare.Read))
            {file.Write(bytes,0,bytes.Length);file.Flush(true);}
        }
        private void AvailabilityLifecycleCaptureSceneBaseline()
        {
            availabilityLifecycleScenes=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
            var rawInventory=AvailabilityUtfReadBootstrapInventory();
            AvailabilityLifecycleWriteRawSceneBaseline(rawInventory);
            availabilityLifecycleOriginalSceneStructure=AvailabilityLifecycleSceneStructure(rawInventory);
            if(availabilityLifecycleScenes.Length==0)
            {
                AvailabilityLifecycleRequireMeasuredDefaultBootstrap();
                availabilityLifecycleMeasuredDefaultBootstrap=true;
                availabilityLifecycleSavedScenes=new UnityEditor.SceneManagement.SceneSetup[0];
                return;
            }
            Assert.IsTrue(availabilityLifecycleScenes.Length>0);
            Assert.AreEqual(availabilityLifecycleScenes.Length,UnityEngine.SceneManagement.SceneManager.sceneCount);
            int unnamedCount=0;
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            {
                var scene=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                Assert.IsFalse(scene.isDirty,"Refuse to discard dirty scene: "+scene.path);
                Assert.IsTrue(scene.isLoaded,"Loaded baseline scenes required.");
                if(!string.IsNullOrEmpty(scene.path))continue;
                unnamedCount++;
                Assert.AreEqual(UnityEngine.SceneManagement.SceneManager.sceneCount-1,i,"Only the UTF trailing empty scene is supported.");
                Assert.AreEqual(0,scene.rootCount,"Nonempty unsaved scenes must never be discarded.");
                Assert.AreEqual(scene,UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"UTF empty scene must be active.");
                Assert.IsTrue(availabilityLifecycleScenes[i].isActive&&availabilityLifecycleScenes[i].isLoaded);
                Assert.IsEmpty(availabilityLifecycleScenes[i].path);
            }
            Assert.LessOrEqual(unnamedCount,1,"Only one UTF empty scene is supported.");
            availabilityLifecycleUtfEmptyBaseline=unnamedCount==1;
            if(!availabilityLifecycleUtfEmptyBaseline)
            {
                AvailabilityLifecycleCleanScenes();
                string baselineJson=Newtonsoft.Json.JsonConvert.SerializeObject(availabilityLifecycleScenes);
                availabilityLifecycleSavedScenes=Newtonsoft.Json.JsonConvert.DeserializeObject<UnityEditor.SceneManagement.SceneSetup[]>(baselineJson);
                Assert.AreEqual(baselineJson,Newtonsoft.Json.JsonConvert.SerializeObject(availabilityLifecycleSavedScenes));
                for(int i=0;i<availabilityLifecycleScenes.Length;i++)
                    Assert.IsFalse(ReferenceEquals(availabilityLifecycleScenes[i],availabilityLifecycleSavedScenes[i]),"Independent cycle scene setup required.");
                return;
            }
            Assert.Greater(availabilityLifecycleScenes.Length,1,"A saved scene alongside the UTF empty scene is required.");
            var saved=new System.Collections.Generic.List<UnityEditor.SceneManagement.SceneSetup>();
            foreach(var setup in availabilityLifecycleScenes)
            {
                if(string.IsNullOrEmpty(setup.path))continue;
                Assert.IsTrue(setup.isLoaded);
                string setupJson=Newtonsoft.Json.JsonConvert.SerializeObject(setup);
                var copy=Newtonsoft.Json.JsonConvert.DeserializeObject<UnityEditor.SceneManagement.SceneSetup>(setupJson);
                Assert.AreEqual(setupJson,Newtonsoft.Json.JsonConvert.SerializeObject(copy));
                Assert.IsFalse(ReferenceEquals(setup,copy),"Cycle setup must not alias the captured original.");
                saved.Add(copy);
            }
            Assert.Greater(saved.Count,0);
            // The UTF transient is active; choose a saved cycle baseline without changing the captured original setup.
            var first=saved[0];first.isActive=true;saved[0]=first;
            availabilityLifecycleSavedScenes=saved.ToArray();
        }
        private void AvailabilityLifecycleRestoreSceneBaseline()
        {
            if(availabilityLifecycleMeasuredDefaultBootstrap)
            {
                AvailabilityLifecycleRecreateMeasuredDefaultBootstrap();
                Assert.IsTrue(Newtonsoft.Json.Linq.JToken.DeepEquals(availabilityLifecycleOriginalSceneStructure,
                    AvailabilityLifecycleSceneStructure(AvailabilityUtfReadBootstrapInventory())));
                Assert.AreEqual(Newtonsoft.Json.JsonConvert.SerializeObject(availabilityLifecycleScenes),
                    Newtonsoft.Json.JsonConvert.SerializeObject(UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup()));
                return;
            }
            UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(availabilityLifecycleSavedScenes);
            if(availabilityLifecycleUtfEmptyBaseline)
            {
                var empty=UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                    UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
                Assert.IsTrue(UnityEngine.SceneManagement.SceneManager.SetActiveScene(empty));
                Assert.IsEmpty(empty.path);Assert.IsFalse(empty.isDirty);Assert.AreEqual(0,empty.rootCount);
            }
            Assert.AreEqual(Newtonsoft.Json.JsonConvert.SerializeObject(availabilityLifecycleScenes),
                Newtonsoft.Json.JsonConvert.SerializeObject(UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup()));
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                Assert.IsFalse(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty);
        }
        private static Newtonsoft.Json.Linq.JObject AvailabilityLifecycleObserve(string cycle,string phase,SkirmishBattleCatalogConfig captured)
        {
            var loaded=SkirmishBattleCatalogConfig.Load();
            bool managedPresent=!ReferenceEquals(captured,null),alive=captured!=null;
            bool found=loaded!=null&&loaded.TryGet("S074",out _);
            SkirmishBattleCatalogEntry row=default;
            if(found)loaded.TryGet("S074",out row);
            var facts=new Newtonsoft.Json.Linq.JObject {
                ["cycle"]=cycle,["phase"]=phase,["isPlaying"]=EditorApplication.isPlaying,
                ["actualEnterPlayOptionsEnabled"]=EditorSettings.enterPlayModeOptionsEnabled,
                ["actualEnterPlayOptions"]=(int)EditorSettings.enterPlayModeOptions,
                ["actualSaveRoot"]=Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT"),
                ["actualLocale"]=GameLocalization.CurrentLocaleCode,
                ["capturedManagedPresent"]=managedPresent,["capturedNativeAlive"]=alive,
                ["capturedEntityId"]=alive?captured.GetEntityId().ToString():null,
                ["sameManagedLoadReference"]=ReferenceEquals(captured,loaded),
                ["loadedNativeAlive"]=loaded!=null,["rowFound"]=found,
                ["rowPlayable"]=found&&row.IsPlayable,["rowStatus"]=found?row.Status.ToString():"Missing",
                ["rowIndex"]=found?row.PlayableScenarioIndex:-1,
                ["rowDefinitionId"]=found?row.DefinitionId:null,["rowContentVersion"]=found?row.ContentVersion:0,
                ["capturedHideFlags"]=alive?captured.hideFlags.ToString():"Destroyed",
                ["historicalWorldEquivalent"]=false,["missionReady"]=false };
            Debug.Log("[S074AvailabilityLifecycleStage] "+facts.ToString(Newtonsoft.Json.Formatting.None));
            return facts;
        }
        [UnityEngine.TestTools.UnityTest]
        public System.Collections.IEnumerator ActualS074AvailabilitySurvivesSceneAndPlayCharacterization()
        {
            string requested=Environment.GetEnvironmentVariable("WARLINE_S074_SCOPE_LIFECYCLE_OUTPUT");
            if(string.IsNullOrEmpty(requested))Assert.Ignore("Owned lifecycle output binding is required; not part of ordinary test suites.");
            Assert.IsFalse(EditorApplication.isPlayingOrWillChangePlaymode);
            availabilityLifecycleOutput=AvailabilityLifecycleOwnedOutput(requested);
            AvailabilityLifecycleCaptureSceneBaseline();
            availabilityLifecycleOptions=EditorSettings.enterPlayModeOptions;
            availabilityLifecycleOptionsEnabled=EditorSettings.enterPlayModeOptionsEnabled;
            availabilityLifecycleBackground=Application.runInBackground;
            availabilityLifecycleSaveRoot=Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT");
            availabilityLifecycleLocale=GameLocalization.CurrentLocaleCode;
            availabilityLifecycleLibrary=Resources.Load<SkirmishBattleCatalogConfig>(SkirmishBattleCatalogConfig.ResourceName);
            availabilityLifecycleAuthored=Resources.Load<SkirmishExpansionCatalogConfig>(SkirmishExpansionCatalogConfig.ResourceName);
            Assert.IsNotNull(availabilityLifecycleLibrary);Assert.IsNotNull(availabilityLifecycleAuthored);
            Assert.AreSame(availabilityLifecycleLibrary,SkirmishBattleCatalogConfig.Load(),"No pre-existing catalog override.");
            Assert.AreSame(availabilityLifecycleAuthored,SkirmishExpansionCatalogConfig.Load(),"No pre-existing authored override.");
            Assert.IsFalse(EditorUtility.IsDirty(availabilityLifecycleLibrary));Assert.IsFalse(EditorUtility.IsDirty(availabilityLifecycleAuthored));
            Assert.IsTrue(availabilityLifecycleLibrary.TryGet("S074",out var shipped));
            Assert.AreEqual(SkirmishBattleCatalogStatus.Planned,shipped.Status);Assert.AreEqual(-1,shipped.PlayableScenarioIndex);
            Assert.IsTrue(string.IsNullOrEmpty(shipped.DefinitionId));
            availabilityLifecycleLibraryJson=JsonUtility.ToJson(availabilityLifecycleLibrary);
            availabilityLifecycleAuthoredJson=JsonUtility.ToJson(availabilityLifecycleAuthored);
            availabilityLifecycleLibraryBytes=System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(availabilityLifecycleLibrary));
            availabilityLifecycleAuthoredBytes=System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(availabilityLifecycleAuthored));
            availabilityLifecycleStarted=true;
            string profile=availabilityLifecycleOutput+".isolated-profile";
            Assert.IsFalse(System.IO.Directory.Exists(profile));
            // Each cycle starts with its own empty profile below the owned output directory.
            EditorSettings.enterPlayModeOptionsEnabled=true;
            EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            Assert.AreEqual(EnterPlayModeOptions.DisableDomainReload,EditorSettings.enterPlayModeOptions);
            Application.runInBackground=true;
            foreach(var flags in new[]{HideFlags.None,HideFlags.HideAndDontSave})
            {
                string cycleProfile=profile+"."+flags;
                Assert.IsFalse(System.IO.Directory.Exists(cycleProfile));
                Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",cycleProfile);
                GameLocalization.SetLocale("en",false); // Actual failed normal job was EN; no persistence.
                // Recreate only the measured engine-owned default bootstrap, or retain the existing saved-scene branch.
                if(availabilityLifecycleMeasuredDefaultBootstrap)AvailabilityLifecycleRecreateMeasuredDefaultBootstrap();
                else UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(availabilityLifecycleSavedScenes);
                availabilityLifecycleScope=Game.Editor.SkirmishGenericMissionAvailabilityScope.Open(availabilityLifecycleAuthored,"S074",1074);
                var captured=SkirmishBattleCatalogConfig.Load();
                Assert.AreNotSame(availabilityLifecycleLibrary,captured);Assert.IsFalse(AssetDatabase.Contains(captured));
                captured.hideFlags=flags; // Only the owned transient test clone; production remains unchanged.
                Assert.IsTrue(captured.TryGet("S074",out var candidate));Assert.IsTrue(candidate.IsPlayable);
                Assert.AreEqual(1074,candidate.PlayableScenarioIndex);
                Assert.IsTrue(availabilityLifecycleAuthored.TryGetDefinition("S074",out var definition));Assert.IsNotNull(definition);
                Assert.AreEqual(definition.DefinitionId,candidate.DefinitionId);Assert.AreEqual(definition.ContentVersion,candidate.ContentVersion);
                string cycle=flags.ToString();
                availabilityLifecycleObservations.Add(AvailabilityLifecycleObserve(cycle,"BeforeScene",captured));
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity");
                availabilityLifecycleObservations.Add(AvailabilityLifecycleObserve(cycle,"AfterScene",captured));
                yield return new UnityEngine.TestTools.EnterPlayMode(false);
                yield return null;
                availabilityLifecycleObservations.Add(AvailabilityLifecycleObserve(cycle,"AfterEnterPlay",captured));
                yield return new UnityEngine.TestTools.ExitPlayMode();
                availabilityLifecycleObservations.Add(AvailabilityLifecycleObserve(cycle,"AfterExitPlay",captured));
                availabilityLifecycleScope.Dispose();availabilityLifecycleScope=null;
                Assert.AreSame(availabilityLifecycleLibrary,SkirmishBattleCatalogConfig.Load());
                Assert.AreSame(availabilityLifecycleAuthored,SkirmishExpansionCatalogConfig.Load());
                AvailabilityLifecycleCleanScenes();
            }
            availabilityLifecycleCyclesCompleted=true;
        }
        [UnityEngine.TestTools.UnityTearDown]
        public System.Collections.IEnumerator RestoreActualS074AvailabilityLifecycleCharacterization()
        {
            if(!availabilityLifecycleStarted)yield break;
            if(EditorApplication.isPlayingOrWillChangePlaymode)
                yield return new UnityEngine.TestTools.ExitPlayMode();
            Exception restorationError=null;
            try
            {
                availabilityLifecycleScope?.Dispose();availabilityLifecycleScope=null;
                if(availabilityLifecycleMeasuredDefaultBootstrap)AvailabilityLifecycleRequireOwnedCycleScene();
                else AvailabilityLifecycleCleanScenes();
                AvailabilityLifecycleRestoreSceneBaseline();
                Assert.AreSame(availabilityLifecycleLibrary,SkirmishBattleCatalogConfig.Load());
                Assert.AreSame(availabilityLifecycleAuthored,SkirmishExpansionCatalogConfig.Load());
                Assert.AreEqual(availabilityLifecycleLibraryJson,JsonUtility.ToJson(availabilityLifecycleLibrary));
                Assert.AreEqual(availabilityLifecycleAuthoredJson,JsonUtility.ToJson(availabilityLifecycleAuthored));
                CollectionAssert.AreEqual(availabilityLifecycleLibraryBytes,System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(availabilityLifecycleLibrary)));
                CollectionAssert.AreEqual(availabilityLifecycleAuthoredBytes,System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(availabilityLifecycleAuthored)));
                Assert.IsFalse(EditorUtility.IsDirty(availabilityLifecycleLibrary));Assert.IsFalse(EditorUtility.IsDirty(availabilityLifecycleAuthored));
            }
            catch(Exception error){restorationError=error;}
            finally
            {
                try
                {
                    EditorSettings.enterPlayModeOptions=availabilityLifecycleOptions;
                    EditorSettings.enterPlayModeOptionsEnabled=availabilityLifecycleOptionsEnabled;
                    Application.runInBackground=availabilityLifecycleBackground;
                    Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",availabilityLifecycleSaveRoot);
                    GameLocalization.SetLocale(availabilityLifecycleLocale,false);
                    Assert.AreEqual(availabilityLifecycleOptions,EditorSettings.enterPlayModeOptions);
                    Assert.AreEqual(availabilityLifecycleOptionsEnabled,EditorSettings.enterPlayModeOptionsEnabled);
                    Assert.AreEqual(availabilityLifecycleBackground,Application.runInBackground);
                    Assert.AreEqual(availabilityLifecycleSaveRoot,Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT"));
                    Assert.AreEqual(availabilityLifecycleLocale,GameLocalization.CurrentLocaleCode);
                }
                catch(Exception settingsError)
                {
                    restorationError=restorationError==null?settingsError:new AggregateException(restorationError,settingsError);
                }
            }
            var report=new Newtonsoft.Json.Linq.JObject {
                ["schema"]=1,["scope"]="S074EditorAvailabilityScenePlayLifetime_NoLaunchNoMission",
                ["cyclesCompleted"]=availabilityLifecycleCyclesCompleted,["observations"]=availabilityLifecycleObservations,
                ["sceneBaseline"]=new Newtonsoft.Json.Linq.JObject {
                    ["utfCleanEmptyTransientRecognized"]=availabilityLifecycleUtfEmptyBaseline,
                    ["measuredDefaultBootstrapRecognized"]=availabilityLifecycleMeasuredDefaultBootstrap,
                    ["originalSceneStructure"]=availabilityLifecycleOriginalSceneStructure,
                    ["actualSceneStructure"]=AvailabilityLifecycleSceneStructure(AvailabilityUtfReadBootstrapInventory()),
                    ["originalSetup"]=Newtonsoft.Json.Linq.JArray.FromObject(availabilityLifecycleScenes),
                    ["restoredSetup"]=Newtonsoft.Json.Linq.JArray.FromObject(UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup()),
                    ["restorationScope"]="Exact captured saved setup or measured engine DefaultGameObjects structure; no original transient native identity claim" },
                ["restorationWitnesses"]=new Newtonsoft.Json.Linq.JObject {
                    ["originalEnterPlayOptions"]=(int)availabilityLifecycleOptions,
                    ["actualEnterPlayOptions"]=(int)EditorSettings.enterPlayModeOptions,
                    ["originalOptionsEnabled"]=availabilityLifecycleOptionsEnabled,
                    ["actualOptionsEnabled"]=EditorSettings.enterPlayModeOptionsEnabled,
                    ["originalSaveRoot"]=availabilityLifecycleSaveRoot,
                    ["actualSaveRoot"]=Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT"),
                    ["originalLocale"]=availabilityLifecycleLocale,["actualLocale"]=GameLocalization.CurrentLocaleCode,
                    ["originalRunInBackground"]=availabilityLifecycleBackground,["actualRunInBackground"]=Application.runInBackground },
                ["restorationPassed"]=restorationError==null,["restorationError"]=restorationError?.ToString(),
                ["productionPatchApplied"]=false,["hypothesisConclusion"]="Requires independent interpretation of actual stage witnesses",
                ["normalMissionPassed"]=false,["publicationModified"]=false };
            byte[] bytes=System.Text.Encoding.UTF8.GetBytes(report.ToString(Newtonsoft.Json.Formatting.Indented));
            Assert.AreEqual(availabilityLifecycleOutput,AvailabilityLifecycleOwnedOutput(availabilityLifecycleOutput));
            using(var file=new System.IO.FileStream(availabilityLifecycleOutput,System.IO.FileMode.CreateNew,System.IO.FileAccess.Write,System.IO.FileShare.Read))
            {file.Write(bytes,0,bytes.Length);file.Flush(true);}
            availabilityLifecycleStarted=false;
            if(restorationError!=null)throw new InvalidOperationException("Lifecycle restoration failed; partial evidence retained.",restorationError);
            Assert.IsTrue(availabilityLifecycleCyclesCompleted,"Incomplete cycles; no lifecycle conclusion.");
            Debug.Log("[S074AvailabilityLifecycle] result=Collected cycles=2 stages=8 launchInputs=0 missionReady=0");
        }
        private static Newtonsoft.Json.Linq.JObject AvailabilityUtfReadBootstrapInventory()
        {
            var scenes=new Newtonsoft.Json.Linq.JArray();
            bool truncated=false;
            int sceneCount=UnityEngine.SceneManagement.SceneManager.sceneCount;
            var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            for(int i=0;i<System.Math.Min(sceneCount,16);i++)
            {
                var scene=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                var roots=new Newtonsoft.Json.Linq.JArray();
                if(scene.isLoaded)
                {
                    var objects=scene.GetRootGameObjects();
                    if(objects.Length>128)truncated=true;
                    for(int r=0;r<System.Math.Min(objects.Length,128);r++)
                    {
                        var go=objects[r];var transform=go.transform;
                        var components=new Newtonsoft.Json.Linq.JArray();
                        var actual=go.GetComponents<Component>();
                        if(actual.Length>128)truncated=true;
                        for(int c=0;c<System.Math.Min(actual.Length,128);c++)
                            components.Add(actual[c]==null?"MissingScript":actual[c].GetType().AssemblyQualifiedName);
                        roots.Add(new Newtonsoft.Json.Linq.JObject {
                            ["index"]=r,["name"]=go.name,["tag"]=go.tag,["activeSelf"]=go.activeSelf,
                            ["activeInHierarchy"]=go.activeInHierarchy,["layer"]=go.layer,["hideFlags"]=go.hideFlags.ToString(),
                            ["localPosition"]=new Newtonsoft.Json.Linq.JArray(transform.localPosition.x,transform.localPosition.y,transform.localPosition.z),
                            ["localRotation"]=new Newtonsoft.Json.Linq.JArray(transform.localRotation.x,transform.localRotation.y,transform.localRotation.z,transform.localRotation.w),
                            ["localScale"]=new Newtonsoft.Json.Linq.JArray(transform.localScale.x,transform.localScale.y,transform.localScale.z),
                            ["childCount"]=transform.childCount,["componentCount"]=actual.Length,["componentTypes"]=components });
                    }
                }
                scenes.Add(new Newtonsoft.Json.Linq.JObject {
                    ["index"]=i,["path"]=scene.path,["name"]=scene.name,["loaded"]=scene.isLoaded,
                    ["dirty"]=scene.isDirty,["active"]=scene==active,["rootCount"]=scene.rootCount,["roots"]=roots });
            }
            return new Newtonsoft.Json.Linq.JObject {
                ["savedSetup"]=Newtonsoft.Json.Linq.JArray.FromObject(UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup()),
                ["sceneManagerCount"]=sceneCount,["scenes"]=scenes,["truncated"]=truncated||sceneCount>16,
                ["rootInventoryOnly"]=true,["childrenTraversed"]=false,
                ["enterPlayOptionsEnabled"]=EditorSettings.enterPlayModeOptionsEnabled,
                ["enterPlayOptions"]=(int)EditorSettings.enterPlayModeOptions,
                ["runInBackground"]=Application.runInBackground,
                ["validationSaveRoot"]=Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT") };
        }
        [Test]
        public void ActualS074UtfBootstrapSceneInventoryOnly()
        {
            string requested=Environment.GetEnvironmentVariable("WARLINE_S074_UTF_BASELINE_INVENTORY_OUTPUT");
            if(string.IsNullOrEmpty(requested))Assert.Ignore("Owned inventory output binding required; not an ordinary suite measurement.");
            Assert.IsFalse(EditorApplication.isPlayingOrWillChangePlaymode);
            string output=AvailabilityLifecycleOwnedOutput(requested);
            // Observe the exact UTF bootstrap before any fixture shape guard or scene/profile/options mutation.
            var before=AvailabilityUtfReadBootstrapInventory();
            var after=AvailabilityUtfReadBootstrapInventory();
            bool unchanged=Newtonsoft.Json.Linq.JToken.DeepEquals(before,after);
            var report=new Newtonsoft.Json.Linq.JObject {
                ["schema"]=1,["scope"]="S074UtfBootstrapInventoryOnly_NoSceneMutationNoLifecycleConclusion",
                ["before"]=before,["after"]=after,["observedStateUnchanged"]=unchanged,
                ["noSceneOrProfileOrOptionsMutationRequested"]=true,["localeQueried"]=false,
                ["lifecycleStagesCollected"]=0,["availabilityScopeOpened"]=false,["productionPatchApplied"]=false,
                ["normalMissionPassed"]=false,["publicationModified"]=false };
            Debug.Log("[S074UtfBootstrapRawInventory] "+report.ToString(Newtonsoft.Json.Formatting.None));
            byte[] bytes=System.Text.Encoding.UTF8.GetBytes(report.ToString(Newtonsoft.Json.Formatting.Indented));
            Assert.LessOrEqual(bytes.Length,4*1024*1024,"Bounded inventory buffer required.");
            Assert.AreEqual(output,AvailabilityLifecycleOwnedOutput(output));
            using(var file=new System.IO.FileStream(output,System.IO.FileMode.CreateNew,System.IO.FileAccess.Write,System.IO.FileShare.Read))
            {file.Write(bytes,0,bytes.Length);file.Flush(true);}
            Assert.IsTrue(unchanged,"Inventory observed a changing baseline; raw facts persisted.");
            Assert.IsFalse((bool)before["truncated"],"Inventory limits reached; partial facts persisted.");
            Debug.Log("[S074UtfBootstrapInventory] result=Collected sceneMutations=0 profileMutations=0 lifecycleStages=0 missionReady=0");
        }
        public static void RunFocusedValidation()
        {
            var t=new SkirmishGenericMissionDispatchTests();
            t.ExistingIndicesAndUnknownNormalizationRemainWhileExplicitGenericIndexSurvives();t.CatalogAmbiguityPlannedAndReservedMissionRebindingFailClosed();
            t.AllSixActualMenuQueuesRetainExactDefinitionAndExistingCampaignSource();t.PayloadIdentityMutationsRejectBeforeAnySessionMutation();
            t.ChangedExplicitSourceCannotFallBackToDesertOrAnotherMission();t.UnpublishedUnknownAndDuplicateAuthoredDefinitionsCannotQueueOrNormalizeToPlayableMission();
            t.ActualFreshReplayRetainsGenericMissionInsteadOfLegacyPhysicalMapIndex();t.GenericNativeSaveEligibilityAndPathsAreExactAndDoNotAliasS005();
            t.NormalDevelopmentMenuAvailabilityDoesNotPublishOrAlterShippedCatalogs();
            Debug.Log("[SkirmishGenericMissionDispatchTests] result=Passed tests=9 nativeMissionReadiness=Pending");
        }
    }
}
#endif
