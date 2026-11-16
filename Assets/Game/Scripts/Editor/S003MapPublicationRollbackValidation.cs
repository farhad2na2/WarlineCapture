using System;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
namespace Game.Editor {
public static class S003MapPublicationRollbackValidation {
 static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
 public static void Run(){
  var definition=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>("Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset");
  Require(definition!=null,"Actual S003 map asset missing");
  var stages=new[]{"before-root","root-ready","active-written","bounds-written","metadata-written","readiness-written","load-written","identity-bound"};
  foreach(var stage in stages)foreach(bool existing in new[]{false,true}){
   using var world=new World("S003 map rollback "+stage+" "+existing);var em=world.EntityManager;using var owner=new OperationMapRuntimeBootstrapSceneSystemHelper(world);
   var grid=em.CreateEntity(typeof(GridConfig),typeof(GridWalkableSourceWitness));var cells=em.AddBuffer<GridWalkable>(grid);cells.Add(new GridWalkable{Value=1});cells.Add(new GridWalkable{Value=0});
   em.SetComponentData(grid,GridWalkableSourceWitness.Capture(definition.GridMetadata.AssetGuid,definition.GridMetadata.ContentHash,cells));
   FixedString64Bytes scenario="scenario.skirmish",mission="skirmish";Entity old=Entity.Null;OperationMapMetadataComponent oldMetadata=default;ActiveOperationMapComponent oldActive=default;OperationMapBoundsComponent oldBounds=default;OperationMapReadinessComponent oldReady=default;OperationMapLoadStateComponent oldLoad=default;
   if(existing){Require(owner.TryPublish(definition,in scenario,in mission,1,OperationMapReadinessFlags.Metadata,OperationMapReadinessFlags.Metadata,out old,out var first),"initial publish: "+first);oldMetadata=em.GetComponentData<OperationMapMetadataComponent>(old);oldActive=em.GetComponentData<ActiveOperationMapComponent>(old);oldBounds=em.GetComponentData<OperationMapBoundsComponent>(old);oldReady=em.GetComponentData<OperationMapReadinessComponent>(old);oldLoad=em.GetComponentData<OperationMapLoadStateComponent>(old);Require(oldMetadata.Blob.IsCreated,"initial blob unavailable");}
   var oldWitness=em.GetComponentData<GridWalkableSourceWitness>(grid);
   OperationMapRuntimeBootstrapSceneSystemHelper.PublicationFailureProbe=x=>{if(x==stage)throw new InvalidOperationException("injected:"+stage);};
   try {bool ok=owner.TryPublish(definition,in scenario,in mission,2,OperationMapReadinessFlags.Metadata,OperationMapReadinessFlags.Metadata,out _,out var error);Require(!ok&&error.Contains("injected:"+stage),"failure not surfaced "+stage+": "+error);}
   finally {OperationMapRuntimeBootstrapSceneSystemHelper.PublicationFailureProbe=null;}
   Require(em.GetComponentData<GridWalkableSourceWitness>(grid).Equals(oldWitness),"grid witness rollback "+stage);
   using var roots=em.CreateEntityQuery(typeof(OperationMapRootComponent));Require(roots.CalculateEntityCount()==(existing?1:0),"root rollback "+stage);
   if(existing){Require(em.Exists(old)&&oldMetadata.Blob.IsCreated,"old blob lost "+stage);Require(em.GetComponentData<OperationMapMetadataComponent>(old).Blob.Equals(oldMetadata.Blob),"blob identity "+stage);Require(em.GetComponentData<ActiveOperationMapComponent>(old).Equals(oldActive),"active "+stage);Require(em.GetComponentData<OperationMapBoundsComponent>(old).Equals(oldBounds),"bounds "+stage);Require(em.GetComponentData<OperationMapReadinessComponent>(old).Equals(oldReady),"readiness "+stage);Require(em.GetComponentData<OperationMapLoadStateComponent>(old).Equals(oldLoad),"load "+stage);Require(owner.PublishedGeneration==1,"publisher generation "+stage);}
  }
  {
   using var world=new World("S003 sparse root rollback");var em=world.EntityManager;using var owner=new OperationMapRuntimeBootstrapSceneSystemHelper(world);
   var root=em.CreateEntity(typeof(OperationMapRootComponent));FixedString64Bytes scenario="scenario.skirmish",mission="skirmish";
   OperationMapRuntimeBootstrapSceneSystemHelper.PublicationFailureProbe=x=>{if(x=="identity-bound")throw new InvalidOperationException("injected:sparse-root");};
   try{Require(!owner.TryPublish(definition,in scenario,in mission,1,OperationMapReadinessFlags.Metadata,OperationMapReadinessFlags.Metadata,out _,out var error)&&error.Contains("sparse-root"),"sparse root failure");}
   finally{OperationMapRuntimeBootstrapSceneSystemHelper.PublicationFailureProbe=null;}
   Require(em.Exists(root)&&!em.HasComponent<ActiveOperationMapComponent>(root)&&!em.HasComponent<OperationMapMetadataComponent>(root)&&!em.HasComponent<OperationMapQueueComponent>(root)&&!em.HasBuffer<OperationMapLoadRequestElement>(root)&&!em.HasBuffer<OperationMapLoadResultElement>(root),"sparse root components or buffers leaked");
  }
  {
   using var world=new World("S003 rollback failure retention");var em=world.EntityManager;using var owner=new OperationMapRuntimeBootstrapSceneSystemHelper(world);
   FixedString64Bytes scenario="scenario.skirmish",mission="skirmish";
   Require(owner.TryPublish(definition,in scenario,in mission,1,OperationMapReadinessFlags.Metadata,OperationMapReadinessFlags.Metadata,out var root,out var initial),"retention initial: "+initial);
   var oldBlob=em.GetComponentData<OperationMapMetadataComponent>(root).Blob;
   OperationMapRuntimeBootstrapSceneSystemHelper.PublicationFailureProbe=x=>{if(x=="metadata-written"||x=="rollback-before-restore")throw new InvalidOperationException("injected:"+x);};
   try{Require(!owner.TryPublish(definition,in scenario,in mission,2,OperationMapReadinessFlags.Metadata,OperationMapReadinessFlags.Metadata,out _,out var error)&&error.Contains("rollback failed"),"rollback failure not surfaced");}
   finally{OperationMapRuntimeBootstrapSceneSystemHelper.PublicationFailureProbe=null;}
   var retained=em.GetComponentData<OperationMapMetadataComponent>(root).Blob;
   Require(oldBlob.IsCreated&&retained.IsCreated&&!oldBlob.Equals(retained),"both reachable candidate blobs must remain live");
   Require(!owner.TryPublish(definition,in scenario,in mission,3,OperationMapReadinessFlags.Metadata,OperationMapReadinessFlags.Metadata,out _,out var poisoned)&&poisoned.Contains("rollback failed"),"failed publisher not disabled");
   owner.ClearPublishedState();Require(!em.Exists(root),"clear did not destroy poisoned root");
  }
  foreach(var stage in new[]{"root-created","root-first-buffer"}) {
   using var world=new World("S003 partial root rollback "+stage);var em=world.EntityManager;using var owner=new OperationMapRuntimeBootstrapSceneSystemHelper(world);
   FixedString64Bytes scenario="scenario.skirmish",mission="skirmish";
   OperationMapRuntimeRootContract.RootFailureProbe=x=>{if(x==stage)throw new InvalidOperationException("injected:"+stage);};
   try{Require(!owner.TryPublish(definition,in scenario,in mission,1,OperationMapReadinessFlags.Metadata,OperationMapReadinessFlags.Metadata,out _,out var error)&&error.Contains("injected:"+stage),"root create injection "+stage);}
   finally{OperationMapRuntimeRootContract.RootFailureProbe=null;}
   using var roots=em.CreateEntityQuery(typeof(OperationMapRootComponent));Require(roots.IsEmptyIgnoreFilter,"partial root leaked "+stage);Require(owner.PublishedGeneration==0,"partial root generation "+stage);
  }
  foreach(var stage in new[]{"reuse-before-bind","reuse-after-bind","reuse-rollback-failure"}) {
   using var world=new World("S003 map reuse rollback "+stage);var em=world.EntityManager;
   var grid=em.CreateEntity(typeof(GridConfig),typeof(GridWalkableSourceWitness));var cells=em.AddBuffer<GridWalkable>(grid);cells.Add(new GridWalkable{Value=1});
   em.SetComponentData(grid,GridWalkableSourceWitness.Capture(definition.GridMetadata.AssetGuid,definition.GridMetadata.ContentHash,cells));
   var missionRoot=em.CreateEntity(typeof(CampaignMissionRootComponent));var launches=em.AddBuffer<CampaignMissionLaunchRequestElement>(missionRoot);
   FixedString64Bytes scenario="scenario.skirmish",mission="skirmish";launches.Add(new CampaignMissionLaunchRequestElement{MissionId=mission,ScenarioId=scenario,OperationMapId=definition.OperationMapId});
   using var first=new OperationMapRuntimeBootstrapSceneSystemHelper(world);Require(first.TryPublish(definition,in scenario,in mission,1,OperationMapReadinessFlags.Metadata,OperationMapReadinessFlags.Metadata,out var root,out var firstError),"reuse initial: "+firstError);
   var before=em.GetComponentData<OperationMapMetadataComponent>(root);var source=em.GetComponentData<OperationMapGridSourceRecord>(root);var witness=em.GetComponentData<GridWalkableSourceWitness>(grid);
   OperationMapGridSourceRecordWriter.Bind(em,root,null,1);OperationMapGridSourceRecordWriter.Bind(em,root,definition,0);Require(em.GetComponentData<OperationMapGridSourceRecord>(root).Equals(source),"incomplete optional identity changed source");
   using var second=new OperationMapRuntimeBootstrapSceneSystemHelper(world);OperationMapRuntimeBootstrapSceneSystemHelper.PublicationFailureProbe=x=>{if(x==stage||(stage=="reuse-rollback-failure"&&(x=="reuse-after-bind"||x=="reuse-rollback-before-restore")))throw new InvalidOperationException("injected:"+stage);};
   try{Require(!second.TryPublish(definition,in scenario,in mission,1,OperationMapReadinessFlags.Metadata,OperationMapReadinessFlags.Metadata,out _,out var error)&&(stage=="reuse-rollback-failure"?error.Contains("rollback failed"):error.Contains("injected:"+stage)),"reuse injection "+stage);}
   finally{OperationMapRuntimeBootstrapSceneSystemHelper.PublicationFailureProbe=null;}
   if(stage=="reuse-rollback-failure")Require(!second.TryPublish(definition,in scenario,in mission,1,OperationMapReadinessFlags.Metadata,OperationMapReadinessFlags.Metadata,out _,out var blocked)&&blocked.Contains("rollback failed"),"reuse failed publisher not disabled");
   Require(before.Blob.IsCreated&&em.GetComponentData<OperationMapMetadataComponent>(root).Blob.Equals(before.Blob),"reuse blob "+stage);
   Require(em.GetComponentData<OperationMapGridSourceRecord>(root).Equals(source),"reuse source "+stage);Require(em.GetComponentData<GridWalkableSourceWitness>(grid).Equals(witness),"reuse witness "+stage);Require(second.PublishedGeneration==0,"reuse generation "+stage);
  }
  Debug.Log("[S003MapPublicationRollback] result=Passed stages=8 existing-and-fresh reuse=3 grid-witness=checked partial-root=2 rollback-failure=retained sparse-root=checked");
 }
}}
