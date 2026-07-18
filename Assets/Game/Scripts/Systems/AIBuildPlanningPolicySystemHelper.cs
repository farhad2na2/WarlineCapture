using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    internal static class AIBuildPlanningPolicySystemHelper
    {
        internal static AIBuildPlannerSystem.BuildDecision SelectBuildDecision(
            DynamicBuffer<AIBuildPlanEntry> entries,
            DynamicBuffer<BuildingConfiguredSpawnableReadModel> spawnables,
            DynamicBuffer<BuildingRuntimeOwnedBuildingSummary> ownedSummaries,
            DynamicBuffer<BuildingRuntimeSpawnRequest> spawnRequests,
            AIBuildPlan plan,
            in FactionEconomy economy,
            in FactionTacticalMaterialsComponent materials)
        {
            AIBuildPlannerSystem.BuildDecision decision = default;
            if (entries.Length == 0)
                return decision;

            int attempts = math.max(1, entries.Length);
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                int candidateIndex = PositiveModulo(plan.NextBuildIndex + attempt, entries.Length);
                FixedString128Bytes buildingId = NormalizeBuildId(entries[candidateIndex].BuildingId);
                if (buildingId.Length == 0)
                    continue;

                if (TryGetOwnedBuildingCount(ownedSummaries, buildingId, plan.FactionId, out int ownedCount) &&
                    ownedCount > 0)
                {
                    continue;
                }

                decision.EntryIndex = candidateIndex;
                decision.BuildingId = buildingId;
                if (HasPendingSpawnRequest(spawnRequests, buildingId, plan.FactionId))
                {
                    decision.Result = AIBuildPlannerSystem.BuildDecisionResult.Pending;
                    break;
                }

                if (!TryResolveSpawnableReadModel(
                        spawnables,
                        buildingId,
                        out BuildingConfiguredSpawnableReadModel spawnable) ||
                    spawnable.CanRequest == 0)
                {
                    decision.Result = AIBuildPlannerSystem.BuildDecisionResult.MissingConfig;
                    break;
                }

                // Match construction consumes tactical materials only. Price remains serialized
                // on legacy definitions for migration, but it is not an in-match currency.
                int cost = 0;
                int materialsCost = math.max(0, spawnable.MaterialsCost);
                decision.Spawnable = spawnable;
                decision.Cost = cost;
                decision.MaterialsCost = materialsCost;
                FactionConstructionResourceMutationResult affordability =
                    FactionConstructionResourceUtilitySystemHelper.Evaluate(
                        economy,
                        materials,
                        cost,
                        materialsCost);
                if (affordability != FactionConstructionResourceMutationResult.Applied)
                {
                    decision.Result = ToBuildDecisionResult(affordability);
                    break;
                }

                decision.Result = AIBuildPlannerSystem.BuildDecisionResult.Request;
                decision.PreferredOrigin = ResolvePreferredOriginCell(plan.BaseCenterCell, candidateIndex);
                break;
            }

            return decision;
        }

        internal static int2 ResolveDefaultBaseCenter(byte factionId, GridConfig grid)
        {
            int x = FactionIdentity.IsPlayerControlled(factionId) ? grid.Width / 4 : (grid.Width * 3) / 4;
            int y = grid.Height / 2;
            return new int2(math.max(0, x), math.max(0, y));
        }

        private static AIBuildPlannerSystem.BuildDecisionResult ToBuildDecisionResult(
            FactionConstructionResourceMutationResult result)
        {
            return result switch
            {
                FactionConstructionResourceMutationResult.InsufficientCredits =>
                    AIBuildPlannerSystem.BuildDecisionResult.InsufficientFunds,
                FactionConstructionResourceMutationResult.InsufficientMaterials =>
                    AIBuildPlannerSystem.BuildDecisionResult.InsufficientMaterials,
                FactionConstructionResourceMutationResult.InsufficientCreditsAndMaterials =>
                    AIBuildPlannerSystem.BuildDecisionResult.InsufficientCreditsAndMaterials,
                FactionConstructionResourceMutationResult.Applied =>
                    AIBuildPlannerSystem.BuildDecisionResult.Request,
                _ => AIBuildPlannerSystem.BuildDecisionResult.InvalidResources
            };
        }

        private static FixedString128Bytes NormalizeBuildId(FixedString64Bytes buildingId)
        {
            FixedString128Bytes source = buildingId;
            int start = 0;
            while (start < source.Length)
            {
                int whitespaceBytes = GetWhitespaceByteCount(ref source, start);
                if (whitespaceBytes == 0)
                    break;

                start += whitespaceBytes;
            }

            int end = source.Length;
            while (end > start)
            {
                int whitespaceBytes = GetTrailingWhitespaceByteCount(ref source, end);
                if (whitespaceBytes == 0)
                    break;

                end -= whitespaceBytes;
            }

            FixedString128Bytes normalized = source.Substring(start, end - start);
            return normalized.ToLowerAscii();
        }

        private static int GetTrailingWhitespaceByteCount(ref FixedString128Bytes value, int end)
        {
            int oneByteStart = end - 1;
            if (GetWhitespaceByteCount(ref value, oneByteStart) == 1)
                return 1;
            if (end >= 2 && GetWhitespaceByteCount(ref value, end - 2) == 2)
                return 2;
            return end >= 3 && GetWhitespaceByteCount(ref value, end - 3) == 3 ? 3 : 0;
        }

        private static int GetWhitespaceByteCount(ref FixedString128Bytes value, int index)
        {
            if (index < 0 || index >= value.Length)
                return 0;

            byte first = value[index];
            if (first == (byte)' ' || (first >= 0x09 && first <= 0x0d))
                return 1;

            if (index + 1 < value.Length && first == 0xc2)
            {
                byte second = value[index + 1];
                if (second == 0x85 || second == 0xa0)
                    return 2;
            }

            if (index + 2 >= value.Length)
                return 0;

            byte middle = value[index + 1];
            byte last = value[index + 2];
            if (first == 0xe1 && middle == 0x9a && last == 0x80)
                return 3;
            if (first == 0xe2 && middle == 0x80 &&
                ((last >= 0x80 && last <= 0x8a) || last == 0xa8 || last == 0xa9 || last == 0xaf))
            {
                return 3;
            }
            if (first == 0xe2 && middle == 0x81 && last == 0x9f)
                return 3;
            return first == 0xe3 && middle == 0x80 && last == 0x80 ? 3 : 0;
        }

        private static bool TryResolveSpawnableReadModel(
            DynamicBuffer<BuildingConfiguredSpawnableReadModel> spawnables,
            FixedString128Bytes buildingId,
            out BuildingConfiguredSpawnableReadModel spawnable)
        {
            for (int i = 0; i < spawnables.Length; i++)
            {
                BuildingConfiguredSpawnableReadModel candidate = spawnables[i];
                if (!candidate.BuildingId.Equals(buildingId))
                    continue;

                spawnable = candidate;
                return true;
            }

            spawnable = default;
            return false;
        }

        private static bool TryGetOwnedBuildingCount(
            DynamicBuffer<BuildingRuntimeOwnedBuildingSummary> ownedSummaries,
            FixedString128Bytes buildingId,
            byte factionId,
            out int count)
        {
            for (int i = 0; i < ownedSummaries.Length; i++)
            {
                BuildingRuntimeOwnedBuildingSummary summary = ownedSummaries[i];
                if (summary.FactionId != factionId || !summary.BuildingId.Equals(buildingId))
                    continue;

                count = summary.Count;
                return true;
            }

            count = 0;
            return false;
        }

        private static bool HasPendingSpawnRequest(
            DynamicBuffer<BuildingRuntimeSpawnRequest> spawnRequests,
            FixedString128Bytes buildingId,
            byte factionId)
        {
            for (int i = 0; i < spawnRequests.Length; i++)
            {
                BuildingRuntimeSpawnRequest request = spawnRequests[i];
                if (request.FactionId == factionId &&
                    request.BuildingId.Equals(buildingId) &&
                    request.Status == BuildingRuntimeSpawnRequest.Pending)
                {
                    return true;
                }
            }

            return false;
        }

        private static int2 ResolvePreferredOriginCell(int2 baseCenterCell, int entryIndex)
        {
            int ring = entryIndex / 5;
            int spacing = 14 + ring * 8;
            int2 offset = PositiveModulo(entryIndex, 5) switch
            {
                0 => new int2(0, 0),
                1 => new int2(spacing, 0),
                2 => new int2(-spacing, 0),
                3 => new int2(0, spacing),
                _ => new int2(0, -spacing)
            };

            return baseCenterCell + offset;
        }

        private static int PositiveModulo(int value, int modulo)
        {
            if (modulo <= 0)
                return 0;

            int result = value % modulo;
            return result < 0 ? result + modulo : result;
        }
    }
}
