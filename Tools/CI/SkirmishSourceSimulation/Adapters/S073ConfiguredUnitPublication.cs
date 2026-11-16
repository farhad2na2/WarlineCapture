using System;
using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed partial class GeneratedRuntimeSpawnProcessor
    {
        internal void PublishS073CapturedConfiguredUnits(GeneratedConfiguredDefinitionSource source, EntityManager em, Entity boundary)
            => PublishConfiguredUnitsReadModel(source, em, boundary);
    }

    internal sealed partial class GeneratedStartingBuildingPipeline
    {
        internal void BindS073CapturedSourceReadModels(S073NativeConfiguredUnits units,
            Func<RuntimeBuildingEntity, Vector3, Vector3> point)
        {
            if (sourceReadModelsBound || units == null || !units.BelongsTo(em) || point == null)
                throw new InvalidOperationException("Same-world captured S073 unit metadata and producer frame bind once");
            if (!definitions.ReadModelUnitDomainKnown)
                definitions.BindCapturedReadModelUnitDomain(units.Count, units.TryRead, units.TryCosts);
            else if (definitions.ConfiguredUnitCount != units.Count)
                throw new InvalidOperationException("Previously published S073 unit domain differs from the full read-model source");
            requests.BindReadModelSourceFrame(point);
            sourceReadModelsBound = true;
        }

        // Publishes only the original configured-unit read model, not the full
        // dynamic read-model cadence, live overlay permissions, or readiness.
        internal void PublishS073CapturedConfiguredUnits(S073NativeConfiguredUnits units)
        {
            if (units == null || !units.BelongsTo(em) || !em.Exists(boundary) || !em.HasComponent<BuildingRuntimeStateTag>(boundary))
                throw new InvalidOperationException("Same-world S073 unit metadata and existing building boundary required");
            definitions.BindCapturedReadModelUnitDomain(units.Count, units.TryRead, units.TryCosts);
            requests.PublishS073CapturedConfiguredUnits(definitions, em, boundary);
        }
    }
}
