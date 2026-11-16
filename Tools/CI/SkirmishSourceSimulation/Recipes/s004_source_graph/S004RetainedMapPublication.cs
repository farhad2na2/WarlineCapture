using System;
using Game.Components;
using Game.Composition;
using Unity.Entities;

namespace Game.Runtime
{
    internal sealed partial class AutonomousWorld
    {
        Entity s004RetainedPublishedRoot;

        internal void AttachS004RetainedMapPublication(OperationMapRuntimeBootstrapSceneSystemHelper owner,Entity root)
        {
            RequireActualPublicationWorld();
            if(actualOperationMapOwner!=null || owner==null || root==Entity.Null || !Entities.Exists(root) ||
               !owner.TryGetPublishedRoot(Entities.World,out var published) || published!=root)
                throw new InvalidOperationException("Actual same-world source publisher/root required");
            using(var roots=Entities.CreateEntityQuery(typeof(OperationMapRootComponent)))
                if(roots.CalculateEntityCount()!=1 || roots.GetSingletonEntity()!=root)
                    throw new InvalidOperationException("Exact unique source operation-map root required");
            if(!Entities.HasComponent<ActiveOperationMapComponent>(root) || !Entities.HasComponent<OperationMapMetadataComponent>(root) ||
               !Entities.HasComponent<OperationMapReadinessComponent>(root))
                throw new InvalidOperationException("Actual published map metadata/readiness components required");
            var active=Entities.GetComponentData<ActiveOperationMapComponent>(root);
            var metadata=Entities.GetComponentData<OperationMapMetadataComponent>(root);
            if(Setup.CatalogId!="S004" || active.MissionId.ToString()!=Setup.DefinitionId || active.ScenarioId.ToString()!=Setup.CatalogId ||
               active.OperationMapId.ToString()!=Setup.OperationMapId || active.Generation!=metadata.Generation ||
               !metadata.Blob.IsCreated || metadata.PhysicalSourceValidated!=0)
                throw new InvalidOperationException("Original S004 published map identity or physical-source status differs: catalog="+Setup.CatalogId+
                    " mission="+active.MissionId+" expectedMission="+Setup.DefinitionId+" scenario="+active.ScenarioId+" expectedScenario="+Setup.CatalogId+
                    " map="+active.OperationMapId+" expectedMap="+Setup.OperationMapId+" activeGeneration="+active.Generation+
                    " metadataGeneration="+metadata.Generation+" blob="+metadata.Blob.IsCreated+" physical="+metadata.PhysicalSourceValidated);
            actualOperationMapOwner=owner;s004RetainedPublishedRoot=root;
        }

        internal void RequireS004RetainedMapPublication()
        {
            RequireActualPublicationWorld();
            if(actualOperationMapOwner==null || s004RetainedPublishedRoot==Entity.Null || ActualOperationMapRoot!=s004RetainedPublishedRoot)
                throw new InvalidOperationException("Same retained source publisher/root required");
            var active=Entities.GetComponentData<ActiveOperationMapComponent>(s004RetainedPublishedRoot);
            var metadata=Entities.GetComponentData<OperationMapMetadataComponent>(s004RetainedPublishedRoot);
            if(active.MissionId.ToString()!=Setup.DefinitionId || active.ScenarioId.ToString()!=Setup.CatalogId ||
               active.OperationMapId.ToString()!=Setup.OperationMapId || active.Generation!=metadata.Generation ||
               !metadata.Blob.IsCreated || metadata.PhysicalSourceValidated!=0)
                throw new InvalidOperationException("Retained source map identity changed");
        }
    }
}
