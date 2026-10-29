using System;using System.Linq;using Game.Configs;using Game.Components;using Game.Composition;using Newtonsoft.Json.Linq;using Unity.Collections;using Unity.Entities;using Unity.Mathematics;
namespace Game.Runtime{
 internal sealed partial class AutonomousWorld{
  OperationMapRuntimeBootstrapSceneSystemHelper actualOperationMapOwner;
  readonly System.Collections.Generic.List<string> sourcePublicationStartupTrace=new();
  internal void RecordPublicationStartup(string stage){sourcePublicationStartupTrace.Add(stage);}
  bool sourceInitialPublicationObserved;
  void ObserveInitialPublicationCompletion(){if(!sourceInitialPublicationObserved&&StartingPipeline.SourceInitialPublicationCompletions>0){sourceInitialPublicationObserved=true;RecordPublicationStartup("initial_native_cadence_completed");}}
  int PublicationRows<T>()where T:struct,IBufferElementData=>Entities.HasBuffer<T>(Boundary)?Entities.GetBuffer<T>(Boundary).Length:-1;
  internal void WritePublicationStartup(string directory){
   var root=ActualOperationMapRoot;var active=Entities.GetComponentData<ActiveOperationMapComponent>(root);var metadata=Entities.GetComponentData<OperationMapMetadataComponent>(root);var readiness=Entities.GetComponentData<OperationMapReadinessComponent>(root);
   var points=new JArray();var slots=Entities.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(Boundary);
   for(int i=0;i<slots.Length;i++){var row=slots[i];points.Add(new JObject{["faction"]=row.FactionId,["building_id"]=row.BuildingId.ToString(),["runtime_id"]=row.BuildingRuntimeId,["slot"]=row.SlotIndex,["cell"]=new JArray(row.Cell.x,row.Cell.y),["world_bits"]=new JArray(BitConverter.ToInt32(BitConverter.GetBytes(row.WorldPosition.x),0),BitConverter.ToInt32(BitConverter.GetBytes(row.WorldPosition.y),0),BitConverter.ToInt32(BitConverter.GetBytes(row.WorldPosition.z),0))});}
   var counts=new JObject{["configured_spawnables"]=PublicationRows<BuildingConfiguredSpawnableReadModel>(),["configured_units"]=PublicationRows<BuildingConfiguredUnitReadModel>(),["production_slots"]=PublicationRows<BuildingProductionSlotReadModel>(),["faction_summaries"]=PublicationRows<BuildingRuntimeFactionSummary>(),["usable_fuel_summaries"]=PublicationRows<BuildingRuntimeFactionUsableFuelSummary>(),["owned_buildings"]=PublicationRows<BuildingRuntimeOwnedBuildingSummary>(),["unit_production"]=PublicationRows<BuildingRuntimeUnitProductionSummary>(),["faction_spawn_points"]=PublicationRows<BuildingFactionProductionSpawnPointReadModel>(),["runways"]=PublicationRows<BuildingFactionRunwayReadModel>(),["surface_overlays"]=PublicationRows<BuildingRuntimeSurfaceOverlay>()};
   System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"publication-startup.json"),new JObject{["order"]=new JArray(sourcePublicationStartupTrace),["initial_cadence_completions"]=StartingPipeline.SourceInitialPublicationCompletions,["root_entity_index"]=root.Index,["root_entity_version"]=root.Version,["generation"]=active.Generation,["metadata_generation"]=metadata.Generation,["readiness_generation"]=readiness.Generation,["source_selection"]=selectedSourceOperationMap?.ExplicitCompiledSource==true?"CompiledExplicitSource":"LegacyCatalogFallback",["mission"]=active.MissionId.ToString(),["scenario"]=active.ScenarioId.ToString(),["map"]=active.OperationMapId.ToString(),["metadata_hash"]=metadata.MetadataHash.ToString(),["metadata_blob_created"]=metadata.Blob.IsCreated,["physical_source_validated"]=metadata.PhysicalSourceValidated,["ready_flags"]=(int)readiness.ReadyFlags,["required_flags"]=(int)readiness.RequiredFlags,["failed_flags"]=(int)readiness.FailedFlags,["facilities"]=Buildings.Count,["callback_buffer_rows"]=counts,["spawn_points"]=points,["metadata_readiness_scope"]="Original metadata-only flags; positive runtime runway/overlay geometry remains unsupported"}.ToString());
  }

  SourceSelectedOperationMap.Selection selectedSourceOperationMap;
  JObject selectedSourcePhysicalMap;
  string selectedSourceRotationPath, selectedSourceRotationSha, selectedSourceRotationRoot;
  string sourceBuildingRequestMetadataPath,sourceBuildingRequestMetadataSha;
  internal void BindSourceConfiguredBuildingMetadata(string sourceRepo,string dataRepo){if(sourceBuildingRequestMetadataPath==null)throw new InvalidOperationException("Validated actual configured-building metadata is required");BindActualBuildingRequestMetadata(sourceBuildingRequestMetadataPath,sourceBuildingRequestMetadataSha,sourceRepo,dataRepo,false);RecordPublicationStartup("configured_metadata_bound_without_publication");}
  internal static SourceSelectedOperationMap.Selection PreflightSelectedOperationMapInputs(JObject request,JObject graph,SourceConfigMaterializer assets,SkirmishScenarioDefinitionConfig definition,SkirmishResolvedSetup setup,string sourceRoot=null){
   var selection=SourceSelectedOperationMap.Resolve(request,graph,assets,definition,setup,(token,limit)=>Descriptor(token,limit));
   var reference=request["operation_map_rotation_reference"]as JObject;
   if(reference==null||reference.Properties().Count()!=2||reference["path"]==null||reference["sha256"]==null)throw new ArgumentException("Exact native rotation-reference path/sha descriptor required");
   Descriptor(reference,4*1024*1024);SourceOperationMapRotationReference.LoadPinned((string)reference["path"],(string)reference["sha256"],setup.CatalogId=="S002"?sourceRoot:null);
   var buildings=request["building_request_metadata"]as JObject;
   if(buildings==null||buildings.Properties().Count()!=2||buildings["path"]?.Type!=JTokenType.String||buildings["sha256"]?.Type!=JTokenType.String)throw new ArgumentException("Exact configured-building metadata path/sha descriptor required");
   Descriptor(buildings,4*1024*1024);return selection;
  }
  void RetainSelectedOperationMapInputs(JObject request, JObject graph, SourceConfigMaterializer assets,string sourceRoot=null){
   selectedSourceOperationMap=PreflightSelectedOperationMapInputs(request,graph,assets,ActualDefinition,Setup,sourceRoot);RecordPublicationStartup("selected_source_validated");
   selectedSourceRotationRoot=Setup.CatalogId=="S002"?sourceRoot:null;selectedSourceRotationPath=(string)request["operation_map_rotation_reference"]["path"];selectedSourceRotationSha=(string)request["operation_map_rotation_reference"]["sha256"];
   selectedSourcePhysicalMap=(JObject)((JObject)request["static_map"]).DeepClone();
   sourceBuildingRequestMetadataPath=(string)request["building_request_metadata"]["path"];sourceBuildingRequestMetadataSha=(string)request["building_request_metadata"]["sha256"];
  }
  void InitializeActualSelectedOperationMapPublication(){
   RequireActualPublicationWorld();
   if(actualOperationMapOwner!=null||selectedSourceOperationMap==null)throw new InvalidOperationException("Fresh validated selected source metadata is required once");
   var definition=selectedSourceOperationMap.Map;
   selectedSourceOperationMap.RequireCurrentSourceValues();
   var grid=Entities.GetComponentData<GridConfig>(GridEntity);var sourceGrid=definition.GridMetadata;
   if(grid.Width!=sourceGrid.Dimensions.x||grid.Height!=sourceGrid.Dimensions.y||grid.CellSize!=sourceGrid.CellSize||grid.Origin.x!=sourceGrid.Origin.x||grid.Origin.y!=sourceGrid.Origin.y||grid.Origin.z!=sourceGrid.Origin.z||(string)selectedSourcePhysicalMap["grid_config"]?["sha256"]!=sourceGrid.ContentHash||(string)selectedSourcePhysicalMap["surface_data"]?["sha256"]!=definition.SurfaceMetadata.ContentHash)throw new NotSupportedException("Actual owned grid/surface source differs from selected metadata");
   using(var roots=Entities.CreateEntityQuery(typeof(OperationMapRootComponent)))if(!roots.IsEmptyIgnoreFilter)throw new NotSupportedException("Fresh initial-root profile cannot adopt/reuse an existing root");
   var rotation=SourceOperationMapRotationReference.LoadPinned(selectedSourceRotationPath,selectedSourceRotationSha,selectedSourceRotationRoot);
   var owner=new OperationMapRuntimeBootstrapSceneSystemHelper(Entities,rotation);
   SourceOperationMapInitialReadiness.ResolveInitialOperationMapReadiness(false,out var ready,out var required);
   try{if(!owner.TryPublish(definition,new FixedString64Bytes(selectedSourceOperationMap.ScenarioId),new FixedString64Bytes(selectedSourceOperationMap.MissionId),1,ready,required,out var root,out string error))throw new InvalidOperationException(error);actualOperationMapOwner=owner;RecordPublicationStartup("selected_metadata_root_published");}catch{owner.Dispose();throw;}
  }

  SourceFactionProductionSpawnPointPublisher actualFacilitySpawnPublisher;
  void RequireActualPublicationWorld(){
   if(disposed||!ownersInitialized||!Entities.World.IsCreated||Setup==null||ActualDefinition==null||GridEntity==Entity.Null||Session==Entity.Null||Boundary==Entity.Null||!Entities.Exists(GridEntity)||!Entities.HasComponent<GridConfig>(GridEntity)||!Entities.Exists(Session)||!Entities.HasComponent<SkirmishExpandedSessionComponent>(Session)||!Entities.HasComponent<SkirmishResolvedSetupRecord>(Session)||!ReferenceEquals(Entities.GetComponentObject<SkirmishResolvedSetupRecord>(Session).Setup,Setup)||!Entities.Exists(Boundary)||!Entities.HasComponent<BuildingRuntimeStateTag>(Boundary)||ActualProductionWorld==null||!ReferenceEquals(ActualProductionWorld.Em,Entities)||!ReferenceEquals(ActualProductionWorld.Buildings,Buildings))throw new InvalidOperationException("Actual same owned-source publication context is incomplete or disposed");
  }
  internal Entity ActualOperationMapRoot{get{if(actualOperationMapOwner==null||!actualOperationMapOwner.TryGetPublishedRoot(Entities.World,out var root))throw new NotSupportedException("Actual owned operation-map root is not published");return root;}}
  // Explicit finite existing-source profile; this does not widen CLI/ARIA/mission admission.
  internal void AttachActualLegacyS002OperationMapRoot(string initializationPath,string initializationSha,string graphPath,string graphSha,string referencePath,string referenceSha){
   RequireActualPublicationWorld();if(actualOperationMapOwner!=null)throw new InvalidOperationException("Publish source metadata once per fresh owned world");
   if(ActualDefinition==null||ActualDefinition.CatalogId!="S002"||ActualDefinitionScenarioIndex!=SkirmishPresetConfig.DesertBaseEstablishedScenarioIndex||ActualDefinition.CampaignMap!=null||Setup.OperationMapId!=SourceLegacyS002MapSelection.OperationMapId||initializationSha!=inputHash)throw new NotSupportedException("Exact original coded legacy S002 map selection required");
   var initialization=Parse(PinnedFile(initializationPath,initializationSha,4*1024*1024));var map=initialization["static_map"]as JObject??throw new NotSupportedException("Actual admitted physical map descriptor required");var selected=map["definition"]as JObject;
   if((string)map["operation_map_id"]!=SourceLegacyS002MapSelection.OperationMapId||selected==null||(string)selected["sha256"]!=SourceLegacyS002MapSelection.AssetSha256)throw new NotSupportedException("Actual source-selected legacy map asset differs");
   PinnedFile((string)selected["path"],(string)selected["sha256"],4*1024*1024);
   var graph=Parse(PinnedFile(graphPath,graphSha,16*1024*1024));foreach(var pin in((JObject)graph["source_pins"]).Properties())PinnedFile(pin.Name,(string)pin.Value,4*1024*1024);
   var node=((JArray)graph["nodes"]).OfType<JObject>().SingleOrDefault(n=>(string)n["source_path"]==SourceLegacyS002MapSelection.AssetPath);
   if(node==null||(string)node["asset_guid"]!=SourceLegacyS002MapSelection.AssetGuid||(string)node["source_sha256"]!=SourceLegacyS002MapSelection.AssetSha256)throw new NotSupportedException("Actual55e source identity required");
   var definition=new SourceConfigMaterializer(graph).ByPath<OperationMapDefinition>(SourceLegacyS002MapSelection.AssetPath);
   // Actual native getter/canonical-input receipts, not an old normalized layout fingerprint.
   if(SkirmishBindingCanonicalValues.SourceFingerprint(definition)!="6eb2ff5503700203a722679846b3972d1c0784c10b65477a553e982d07cc850a"||SkirmishBindingCanonicalValues.FullSourceFingerprint(definition)!="6750faddb86deba5e9ed65565e4d3ba6ea306a92447bbc6b2ccc14353b5c64be")throw new NotSupportedException("Actual legacy native typed metadata/canonical input identity differs");
   var grid=Entities.GetComponentData<GridConfig>(GridEntity);var sourceGrid=definition.GridMetadata;
   if(grid.Width!=sourceGrid.Dimensions.x||grid.Height!=sourceGrid.Dimensions.y||grid.CellSize!=sourceGrid.CellSize||grid.Origin.x!=sourceGrid.Origin.x||grid.Origin.y!=sourceGrid.Origin.y||grid.Origin.z!=sourceGrid.Origin.z||(string)map["grid_config"]?["sha256"]!=sourceGrid.ContentHash||(string)map["surface_data"]?["sha256"]!=definition.SurfaceMetadata.ContentHash)throw new NotSupportedException("Actual owned grid/surface source differs from metadata");
   using(var roots=Entities.CreateEntityQuery(typeof(OperationMapRootComponent)))if(!roots.IsEmptyIgnoreFilter)throw new NotSupportedException("Fresh initial-root profile cannot adopt/reuse an existing root");
   var rotation=SourceOperationMapRotationReference.LoadPinned(referencePath,referenceSha);var owner=new OperationMapRuntimeBootstrapSceneSystemHelper(Entities,rotation);
   SourceOperationMapInitialReadiness.ResolveInitialOperationMapReadiness(false,out var ready,out var required);
   try{if(!owner.TryPublish(definition,new FixedString64Bytes(SourceLegacyS002MapSelection.ScenarioId),new FixedString64Bytes(SourceLegacyS002MapSelection.MissionId),1,ready,required,out var root,out string error))throw new InvalidOperationException(error);actualOperationMapOwner=owner;}catch{owner.Dispose();throw;}
  }
  internal void PublishActualFacilitySpawnPoints(){
   RequireActualPublicationWorld();if(ActualProductionWorld==null||!ReferenceEquals(ActualProductionWorld.Em,Entities)||!ReferenceEquals(ActualProductionWorld.Buildings,Buildings))throw new NotSupportedException("Same source production world and canonical registry required");
   if(actualFacilitySpawnPublisher==null)actualFacilitySpawnPublisher=new SourceFactionProductionSpawnPointPublisher((building,local)=>{
    if(!Buildings.TryGetValue(building.Id,out var actual)||!ReferenceEquals(actual,building)||building.Instance==null)throw new NotSupportedException("Actual live registered numeric instance required");
    if(!ActualProductionWorld.TryProducerFrame(building.Id,out var position,out _)||position.x!=building.Instance.transform.position.x||position.y!=building.Instance.transform.position.y||position.z!=building.Instance.transform.position.z)throw new NotSupportedException("Source producer root is missing or has an unadmitted transform writer");
    var point=ActualProductionWorld.ProducerPoint(building,new float3(local.x,local.y,local.z));return new Vector3(point.x,point.y,point.z);
   });
   actualFacilitySpawnPublisher.Publish(Entities,Boundary,Buildings);
  }
  void DisposeActualOperationMapPublication(){actualOperationMapOwner?.Dispose();actualOperationMapOwner=null;}
 }
}
