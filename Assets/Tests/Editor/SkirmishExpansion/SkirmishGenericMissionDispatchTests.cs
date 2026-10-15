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
                var actual=SkirmishExpansionCatalogConfig.Load();Assert.IsNotNull(actual);
                var profiles=actual.ToAuthoredSet();var layout=UnityEngine.Object.Instantiate(profiles.DefinitionS005.MapLayout);Owned.Add(layout);
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
        public static void WithViewBoundQueuedSession(Action<EntityManager,Entity> assertion)
        {
            using var fixture=new Fixture();using var world=new World("ViewBoundGenericQueuedFixture");
            var config=QuickGameConfig.Defaults;config.ScenarioIndex=fixture.Index;
            Assert.IsTrue(SkirmishLaunchProjection.TryQueue(world.EntityManager,config));
            using var sessions=world.EntityManager.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            assertion(world.EntityManager,sessions.GetSingletonEntity());
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
