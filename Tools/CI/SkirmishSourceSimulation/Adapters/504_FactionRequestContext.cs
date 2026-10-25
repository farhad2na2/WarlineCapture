using System;
using Game.Components;
using Unity.Entities;
namespace Game.Runtime
{
    internal sealed partial class SourceCampAvailabilityKernel
    {
        internal delegate bool TryGetConfiguredUnitReadModelDelegate(int index,out SourceUnitPrefab prefab,out string displayName,out int price,out bool canRequest,out bool isVehicle);
        internal sealed partial class Context
        {
            internal TryGetConfiguredUnitReadModelDelegate TryGetConfiguredUnitReadModel;
            internal Func<byte,string,int> CountPendingProductionsForFaction,CountRuntimeProducedUnitsForFaction;
        }
    }
    internal sealed partial class SourceCampAvailability
    {
        internal SourceFactionProductionDispatcher CreateFactionRequestDispatcher(Entity boundary,SourceProductionDeliveryOwner owner,
            SourceCampAvailabilityKernel.TryGetConfiguredUnitReadModelDelegate actualConfiguredUnitEntries,
            Func<byte,string,int> sourceCountPending,Func<byte,string,int> sourceCountProduced,bool readDomainComplete)
        {
            if(!readDomainComplete||actualConfiguredUnitEntries==null||sourceCountPending==null||sourceCountProduced==null)
                throw new NotSupportedException("Complete actual configured-unit/count read domain required");
            if(owner==null||!ReferenceEquals(owner.World.Em,em)||!ReferenceEquals(owner.World.Buildings,buildings)||!ReferenceEquals(owner.Production,context.ProductionSystem))
                throw new InvalidOperationException("Faction requests must share the existing world/buildings/paid queue owner");
            context.TryGetConfiguredUnitReadModel=actualConfiguredUnitEntries;
            context.CountPendingProductionsForFaction=sourceCountPending;
            context.CountRuntimeProducedUnitsForFaction=sourceCountProduced;
            context.ProductionSystem.BindSourceFactionQueueOwner(owner);
            return new SourceFactionProductionDispatcher(em,boundary,owner,this,kernel,context);
        }
        internal IDisposable OpenFactionReadScope(CatalogPublicFacts facts)
        {Preflight();return new SourceCampReadScope(em,store,facts,map);}
    }
    public sealed partial class BuildingProductionQueueCompositionSystemHelper
    {
        SourceProductionDeliveryOwner sourceFactionQueueOwner;
        internal void BindSourceFactionQueueOwner(SourceProductionDeliveryOwner owner)
        {
            if(owner==null||!ReferenceEquals(owner.Production,this))throw new ArgumentException("Same existing production queue required");
            if(sourceFactionQueueOwner!=null&&!ReferenceEquals(sourceFactionQueueOwner,owner))throw new InvalidOperationException("Paid queue owner cannot be rebound");
            sourceFactionQueueOwner=owner;
        }
        internal bool TryQueuePlayerUnitFromBuilding(QueueContext context,RuntimeBuildingEntity building,int productionIndex,SourceUnitPrefab prefab,EntityManager em,float now)
        {
            if(building==null||prefab==null)return false;
            var owner=sourceFactionQueueOwner??throw new NotSupportedException("Actual faction paid queue not bound");
            if(!ReferenceEquals(owner.World.Em,em)||!owner.QueueContext.Equals(context)||!owner.World.Buildings.TryGetValue(building.Id,out var actual)||!ReferenceEquals(actual,building))
                throw new InvalidOperationException("Faction paid queue world/context/producer lineage mismatch");
            if(!owner.Recipes.TryGetValue(building.Id,out var recipes))return false;
            SourceProductionDeliveryRecipe found=null;
            foreach(var recipe in recipes)if(recipe.ProductionIndex==productionIndex){if(found!=null)throw new InvalidOperationException("Duplicate source recipe index");found=recipe;}
            if(found==null||!ReferenceEquals(found.Unit,prefab))return false;
            return owner.TryQueueFactionRequest(building.Id,productionIndex,now,out _);
        }
    }
    internal sealed class SourceFactionProductionReadModels
    {
        internal int CountPendingProductionsForFaction(SourceCampAvailabilityKernel.Context context,byte faction,string id)
            =>context.CountPendingProductionsForFaction(faction,id);
        internal int CountRuntimeProducedUnitsForFaction(SourceCampAvailabilityKernel.Context context,byte faction,string id)
            =>context.CountRuntimeProducedUnitsForFaction(faction,id);
    }
}
