using Unity.Collections;
using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    internal static class AIBuildPlanningReadSystemHelper
    {
        internal static NativeList<FactionEconomyRecord> BuildFactionEconomyRecords(
            EntityQuery economyQuery,
            EntityTypeHandle entityType,
            ComponentTypeHandle<FactionEconomy> economyType,
            ComponentTypeHandle<FactionTacticalMaterialsComponent> materialsType)
        {
            int count = economyQuery.CalculateEntityCount();
            NativeList<FactionEconomyRecord> records = new(count, Allocator.Temp);
            using NativeArray<ArchetypeChunk> chunks =
                economyQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
                NativeArray<FactionEconomy> economies = chunk.GetNativeArray(ref economyType);
                NativeArray<FactionTacticalMaterialsComponent> materials =
                    chunk.GetNativeArray(ref materialsType);
                for (int i = 0; i < chunk.Count; i++)
                    records.Add(new FactionEconomyRecord(entities[i], economies[i], materials[i]));
            }

            return records;
        }

        internal static bool TryFindEconomyRecord(
            NativeList<FactionEconomyRecord> records,
            byte factionId,
            out int index,
            out FactionEconomyRecord record)
        {
            for (int i = 0; i < records.Length; i++)
            {
                FactionEconomyRecord candidate = records[i];
                if (candidate.Economy.FactionId != factionId)
                    continue;

                index = i;
                record = candidate;
                return true;
            }

            index = -1;
            record = default;
            return false;
        }

        internal static bool TryGetFactionBuildingCount(
            ref SystemState state,
            Entity boundaryEntity,
            byte factionId,
            out int count)
        {
            count = 0;
            if (!state.EntityManager.HasBuffer<BuildingRuntimeFactionSummary>(boundaryEntity))
                return false;

            DynamicBuffer<BuildingRuntimeFactionSummary> summaries =
                state.EntityManager.GetBuffer<BuildingRuntimeFactionSummary>(boundaryEntity, true);
            for (int i = 0; i < summaries.Length; i++)
            {
                BuildingRuntimeFactionSummary summary = summaries[i];
                if (summary.FactionId != factionId)
                    continue;

                count = summary.BuildingCount;
                return true;
            }

            return false;
        }

        internal static bool HasMaterialsChanged(
            in FactionTacticalMaterialsComponent previous,
            in FactionTacticalMaterialsComponent current)
        {
            return previous.FactionId != current.FactionId ||
                   previous.Current != current.Current ||
                   previous.Capacity != current.Capacity ||
                   previous.LifetimeFabricated != current.LifetimeFabricated ||
                   previous.LifetimeImported != current.LifetimeImported ||
                   previous.LifetimeRewarded != current.LifetimeRewarded ||
                   previous.LifetimeExported != current.LifetimeExported ||
                   previous.LifetimeSpent != current.LifetimeSpent ||
                   previous.LifetimeConstructionSpent != current.LifetimeConstructionSpent ||
                   previous.LifetimeRepairSpent != current.LifetimeRepairSpent ||
                   previous.LifetimeInfrastructureSpent != current.LifetimeInfrastructureSpent ||
                   previous.LifetimeUpgradeSpent != current.LifetimeUpgradeSpent ||
                   previous.Version != current.Version;
        }

        internal static bool IsFactionAIControlled(
            byte factionId,
            bool hasControls,
            DynamicBuffer<FactionControlEntry> controls)
        {
            if (!hasControls)
                return FactionIdentity.IsAiControlledByDefault(factionId);

            for (int i = 0; i < controls.Length; i++)
            {
                FactionControlEntry control = controls[i];
                if (control.FactionId == factionId)
                    return control.AIControlled != 0;
            }

            return FactionIdentity.IsAiControlledByDefault(factionId);
        }

        internal static string SpawnResultLabel(BuildingRuntimeSpawnRequest request)
        {
            if (request.Status == BuildingRuntimeSpawnRequest.Succeeded)
                return "Placed";

            return request.ResultCode switch
            {
                BuildingRuntimeSpawnRequest.MissingConfig => "MissingConfig",
                BuildingRuntimeSpawnRequest.Blocked => "Blocked",
                _ => "Failed"
            };
        }

        internal readonly struct FactionEconomyRecord
        {
            internal FactionEconomyRecord(
                Entity entity,
                FactionEconomy economy,
                FactionTacticalMaterialsComponent materials)
            {
                Entity = entity;
                Economy = economy;
                Materials = materials;
            }

            internal readonly Entity Entity;
            internal readonly FactionEconomy Economy;
            internal readonly FactionTacticalMaterialsComponent Materials;
        }
    }
}
