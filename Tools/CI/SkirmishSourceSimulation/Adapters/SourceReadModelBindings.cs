using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed partial class GeneratedRuntimeSpawnProcessor { internal bool SourceConfiguredReadModelsPublished => _configuredReadModelsPublished; }
    internal sealed partial class GeneratedConfiguredDefinitionSource
    {
        SourceCampAvailabilityKernel.TryGetConfiguredUnitReadModelDelegate sourceUnitReader;
        SourceCampUnitCostReads sourceUnitCosts;
        int sourceConfiguredUnitCount;
        internal bool ReadModelUnitDomainKnown => sourceUnitReader != null && sourceUnitCosts != null && RequestMetadataKnown;
        internal int ConfiguredUnitCount => ReadModelUnitDomainKnown ? sourceConfiguredUnitCount :
            throw new NotSupportedException("Actual complete configured unit domain is not bound");
        internal IReadOnlyList<RuntimeDefinition> ConfiguredSpawnableDefinitions => generatedDefinitions;

        internal void BindReadModelUnitDomain(int count,
            SourceCampAvailabilityKernel.TryGetConfiguredUnitReadModelDelegate reader, SourceCampUnitCostReads costs)
        {
            if (ReadModelUnitDomainKnown || !RequestMetadataKnown || count < 0 || reader == null || costs == null)
                throw new InvalidOperationException("Bind actual configured metadata once after its source catalog");
            for (int i = 0; i < count; i++)
                if (!reader(i, out var prefab, out _, out _, out _, out _) || prefab == null)
                    throw new NotSupportedException("Complete actual configured unit order is required");
            sourceConfiguredUnitCount = count;
            sourceUnitReader = reader;
            sourceUnitCosts = costs;
        }

        internal bool TryGetConfiguredUnitReadModel(int index, out SourceUnitPrefab prefab,
            out string name, out int price, out bool canRequest, out bool isVehicle)
        {
            if (!ReadModelUnitDomainKnown)
                throw new NotSupportedException("Actual configured unit reader is not bound");
            return sourceUnitReader(index, out prefab, out name, out price, out canRequest, out isVehicle);
        }

        internal bool TryResolveConfiguredUnitResourceCosts(SourceUnitPrefab prefab, int fallback,
            out int credits, out int materials)
        {
            if (!ReadModelUnitDomainKnown)
                throw new NotSupportedException("Actual source unit cost reader is not bound");
            return sourceUnitCosts.TryResolveConfiguredUnitResourceCosts(prefab, fallback, out credits, out materials);
        }
    }

    internal sealed partial class SourceCampAvailability
    {
        internal SourceCampUnitCostReads SameReadModelCostOwner => costs;
    }

    internal sealed partial class GeneratedStartingBuildingPipeline
    {
        readonly SourceRuntimeBuildingCountReader sourceReadModelCounts = new();
        bool sourceReadModelsBound;
        internal int SourceInitialPublicationCompletions { get; private set; }
        internal void BindSourceReadModels(int count,
            SourceCampAvailabilityKernel.TryGetConfiguredUnitReadModelDelegate units,
            SourceCampUnitCostReads costs,
            Func<RuntimeBuildingEntity, Vector3, Vector3> point)
        {
            if (sourceReadModelsBound || point == null)
                throw new InvalidOperationException("Source read-model owner binds once");
            definitions.BindReadModelUnitDomain(count, units, costs);
            requests.BindReadModelSourceFrame(point);
            sourceReadModelsBound = true;
        }

        internal void PublishSourceReadModels(GeneratedProductionReadModelSource.Context queryContext,
            FactionResourceCompositionSystemHelper resources, float now)
        {
            if (!sourceReadModelsBound || resources == null)
                throw new NotSupportedException("Complete same-world source read-model owner is not bound");
            if (!ReferenceEquals(queryContext.RuntimeBuildings, Buildings) || queryContext.TryGetEntityManager == null ||
                !queryContext.TryGetEntityManager(out EntityManager manager) || !ReferenceEquals(manager, em))
                throw new InvalidOperationException("Read-model query context must share the canonical registry and manager");
            bool wasPublished = requests.SourceConfiguredReadModelsPublished;
            requests.PublishSourceReadModels(definitions, sourceReadModelCounts, queryContext,
                resources, em, boundary, Buildings, now);
            if (!wasPublished && requests.SourceConfiguredReadModelsPublished) SourceInitialPublicationCompletions++;
        }
    }
}
