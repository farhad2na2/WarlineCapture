using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct AIProductionSystem
    {
        private void EnqueueProductionRequest(ref SystemState state, Entity boundaryEntity, byte factionId, FixedString128Bytes unitId, int creditsCost, int materialsCost)
        {
            DynamicBuffer<BuildingFactionUnitProductionRequest> requests =
                state.EntityManager.GetBuffer<BuildingFactionUnitProductionRequest>(boundaryEntity);
            requests.Add(new BuildingFactionUnitProductionRequest
            {
                RequestId = ++_nextProductionRequestId,
                FactionId = factionId,
                UnitId = unitId,
                Status = BuildingFactionUnitProductionRequest.Pending,
                Cost = creditsCost,
                MaterialsCost = materialsCost,
                ResourcesReserved = 1
            });
        }

        private void ProcessCompletedProductionRequests(
            ref SystemState state,
            Entity boundaryEntity,
            ref FactionEconomy economy,
            ref FactionTacticalMaterialsComponent materials,
            bool shouldLog)
        {
            if (!state.EntityManager.HasBuffer<BuildingFactionUnitProductionRequest>(boundaryEntity))
                return;

            DynamicBuffer<BuildingFactionUnitProductionRequest> requests =
                state.EntityManager.GetBuffer<BuildingFactionUnitProductionRequest>(boundaryEntity);
            for (int i = requests.Length - 1; i >= 0; i--)
            {
                BuildingFactionUnitProductionRequest request = requests[i];
                if (request.FactionId != economy.FactionId ||
                    request.Status == BuildingFactionUnitProductionRequest.Pending)
                {
                    continue;
                }

                if (request.ResourcesReserved != 0)
                {
                    if (request.Status != BuildingFactionUnitProductionRequest.Succeeded &&
                        FactionConstructionResourceUtilitySystemHelper.TryRollback(
                            ref economy, ref materials, request.Cost, request.MaterialsCost) !=
                        FactionConstructionResourceMutationResult.Applied)
                        continue; // Keep the receipt until both resources can be restored.
                }
                else if (request.Status == BuildingFactionUnitProductionRequest.Succeeded)
                    economy.Money = math.max(0, economy.Money - math.max(0, request.Cost));

                if (shouldLog)
                {
                    EnqueueDiagnostic(
                        ref state,
                        $"[AIProduction] faction={request.FactionId} producer={request.ProducerDisplayName.ToString()} unit={request.UnitDisplayName.ToString()} cost={request.Cost} queue={request.QueueCount} result={ProductionResultLabel(request)}");
                }

                requests.RemoveAt(i);
            }
        }

    }
}
