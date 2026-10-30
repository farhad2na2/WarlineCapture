using System;using System.Linq;using UnityEngine;using Game.Components;using Unity.Entities;using Unity.Collections;
namespace Game.Runtime {
 internal sealed partial class AutonomousWorld {
  internal GeneratedS002CatalogAvailabilityBinding ActualCatalog {get;private set;}
  internal SourceCampAvailability ActualAvailability {get;private set;}
  internal SourceFactionProductionDispatcher ActualFactionDispatcher {get;private set;}
  internal SourceAriaPaidRecruitment ActualAriaPaid {get;private set;}
  GeneratedProductionReadModelSource productionReadModel;GeneratedProductionReadModelSource.Context productionReadContext;
  internal void BindActualPaidCatalog(string metadataPath,string metadataSha){
   if(ActualCatalog!=null||ActualDeliveryOwner?.Helicopters==null)throw new InvalidOperationException("Complete actual paid delivery context required once");
   var catalog=GeneratedS002CatalogAvailabilityBinding.Bind(Entities,Session,ActualUnitMetadata,metadataPath,metadataSha);
   Entities.GetComponentObject<SkirmishProductionCatalogRecord>(Session).Catalog.BindSourceDeliveryMetadata(ActualUnitMetadata.Ordered.ToDictionary(row=>row.Prefab.name,row=>row.Prefab,StringComparer.Ordinal),ActualUnitMetadata.TryMetadata);
   Entity bank=Entity.Null;using(var q=Entities.CreateEntityQuery(typeof(FactionEconomy),typeof(FactionTacticalMaterialsComponent)))using(var es=q.ToEntityArray(Allocator.Temp))foreach(var e in es){if(!FactionIdentity.IsPlayerControlled(Entities.GetComponentData<FactionEconomy>(e).FactionId))continue;if(bank!=Entity.Null)throw new NotSupportedException("Ambiguous source controlled-faction bank");bank=e;}
   if(bank==Entity.Null)throw new NotSupportedException("Actual source material bank missing");
   if(SourceDefinitions.Definitions.Any(d=>d.HasRunway))throw new NotSupportedException("Runway owner is outside current ordinary S002 ground profile");
   var availability=catalog.CreateAvailability(bank,Buildings,SourceDefinitions.Definitions,ActualDefinitionsByPrefab,ActualConfiguredUnits,ActualUnitsByKey,ActualRecipes,ActualDeliveryOwner.MaxQueued,ActualProduction,ActualProductionContext,ActualProductionContext.TryGetPrefabLocalBounds,null,true,true);
   productionReadModel=new GeneratedProductionReadModelSource();
   productionReadContext=new GeneratedProductionReadModelSource.Context(Buildings,(out EntityManager manager)=>{manager=Entities;return true;},(EntityManager em,out Entity boundary)=>{boundary=Boundary;return ReferenceEquals(em,Entities)&&em.Exists(boundary);},ActualProduction,BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey,(prefab,id)=>string.IsNullOrEmpty(id)||(prefab!=null&&BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(prefab.name)==id));
   var dispatcher=availability.CreateFactionRequestDispatcher(Boundary,ActualDeliveryOwner,ReadActualConfiguredEntry,(f,id)=>productionReadModel.CountPendingProductionsForFaction(productionReadContext,f,id),(f,id)=>productionReadModel.CountRuntimeProducedUnitsForFaction(productionReadContext,f,id),true);
   var paid=new SourceAriaPaidRecruitment(Entities,Session,availability,catalog.Rows,ActualDeliveryOwner,()=>SourceS002CatalogPublicFacts.Read(Entities,Session));
   ActualCatalog=catalog;ActualAvailability=availability;ActualFactionDispatcher=dispatcher;ActualAriaPaid=paid;
   // Diagnostic namespace/tick are actual existing values; no additional clock or gameplay write.
   ActualProductionWorld.DeliveryWorldId=WorldId;ActualProductionWorld.DeliveryTick=()=>Tick;
   StartingPipeline.BindSourceReadModels(ActualUnitMetadata.Ordered.Count,ReadActualConfiguredEntry,availability.SameReadModelCostOwner,(building,local)=>{var point=ActualProductionWorld.ProducerPoint(building,new Unity.Mathematics.float3(local.x,local.y,local.z));return new Vector3(point.x,point.y,point.z);});
   InitializeActualSelectedOperationMapPublication();
  }
  bool ReadActualConfiguredEntry(int index,out SourceUnitPrefab prefab,out string name,out int price,out bool can,out bool vehicle){
   prefab=null;name=string.Empty;price=0;can=false;vehicle=false;if(index<0||index>=ActualUnitMetadata.Ordered.Count)return false;
   var row=ActualUnitMetadata.Ordered[index];prefab=row.Prefab;if(prefab==null)return false;var metadata=row.Definition;
   if(ActualRecipes.TryGetValue(prefab,out var recipe)){metadata.Price=recipe.MaterialsCost;metadata.CreditsCost=recipe.CreditsCost;metadata.CanRequest=true;}
   name=!string.IsNullOrWhiteSpace(metadata.DisplayName)?metadata.DisplayName:prefab.name;price=metadata.Price;can=metadata.CanRequest;
   vehicle=metadata.FootprintCells.x>1||metadata.FootprintCells.y>1||prefab.name.IndexOf("Veh",StringComparison.OrdinalIgnoreCase)>=0;return true;
  }
  internal void AdvanceActualFactionRequests(){if(ActualFactionDispatcher!=null)ActualFactionDispatcher.Step((float)SimulationSeconds,SourceS002CatalogPublicFacts.Read(Entities,Session));}
 }
}
