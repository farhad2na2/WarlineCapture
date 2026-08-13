using System;
using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public sealed class CampaignMissionProgressStoreReferenceComponent : IComponentData
    {
        public CampaignMissionProgressStore Store;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CampaignMissionResultProjectionSystem))]
    public partial struct CampaignMissionProgressSettlementSystem : ISystem
    {
        private const string M02MissionId = "saga.ch01.m02.establish_base";

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CampaignMissionRootComponent>();
            state.RequireForUpdate<CampaignMissionCatalogComponent>();
            state.RequireForUpdate<CampaignMissionRuntimeComponent>();
            state.RequireForUpdate<CampaignMissionResultComponent>();
            state.RequireForUpdate<CampaignMissionSettlementRequestElement>();
            state.RequireForUpdate<CampaignMissionProgressStoreReferenceComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<CampaignMissionRootComponent>(out Entity root) ||
                !SystemAPI.TryGetSingleton(out CampaignMissionCatalogComponent catalog) ||
                !SystemAPI.TryGetSingleton(out CampaignMissionRuntimeComponent runtime) ||
                !SystemAPI.TryGetSingleton(out CampaignMissionResultComponent result) ||
                !CampaignMissionSpawnSystem.TryFindDefinition(in catalog, in runtime, out int definitionIndex))
                return;

            EntityManager entityManager = state.EntityManager;
            CampaignMissionProgressStore store = entityManager
                .GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store;
            if (store == null) return;
            DynamicBuffer<CampaignMissionSettlementRequestElement> requests =
                entityManager.GetBuffer<CampaignMissionSettlementRequestElement>(root);
            DynamicBuffer<CampaignMissionSettlementResultElement> responses =
                entityManager.GetBuffer<CampaignMissionSettlementResultElement>(root);
            ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[definitionIndex];

            while (requests.Length > 0)
            {
                CampaignMissionSettlementRequestElement request = requests[0];
                requests.RemoveAt(0);
                responses.Add(Settle(store, in request, in runtime, in result, ref definition));
            }
        }

        internal static CampaignMissionSettlementResultElement Settle(
            CampaignMissionProgressStore store,
            in CampaignMissionSettlementRequestElement request,
            in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionResultComponent result,
            ref CampaignMissionDefinitionBlob definition)
        {
            CampaignMissionSettlementResultElement response = new()
            {
                SourceVersion = request.SourceVersion,
                SessionToken = request.SessionToken
            };
            if (store == null || request.Outcome != MissionOutcomeKind.Victory ||
                request.SourceVersion == 0 || request.SourceVersion != result.SourceVersion ||
                !request.MissionId.Equals(result.MissionId) || !request.MissionId.Equals(runtime.MissionId) ||
                !request.SessionToken.Equals(result.SessionToken) ||
                !request.SessionToken.Equals(runtime.SessionToken) ||
                request.AttemptOrdinal != result.AttemptOrdinal ||
                request.AttemptOrdinal != runtime.AttemptOrdinal || result.Outcome != MissionOutcomeKind.Victory)
            {
                response.ReasonCode = new FixedString64Bytes("invalid-settlement");
                return response;
            }

            bool firstClear = runtime.RunKind == MissionRunKind.FirstClear ||
                              runtime.RunKind == MissionRunKind.Retry &&
                              runtime.LaunchOrigin == MissionLaunchOriginKind.FirstLaunch;
            if (firstClear && result.ReturnDestination != MissionReturnDestinationKind.CommandBase ||
                !firstClear && result.ReturnDestination != MissionReturnDestinationKind.CampaignOperations)
            {
                response.ReasonCode = new FixedString64Bytes("invalid-return-route");
                return response;
            }

            ref BlobArray<CampaignMissionRewardBlob> rewardSet = ref (
                firstClear ? ref definition.FirstClearRewards : ref definition.ReplayRewards);
            CampaignMissionRewardGrant[] grants = ProjectRewards(ref rewardSet);
            CampaignMissionSettlementReceipt receipt;
            try
            {
                receipt = store.SettleWithRewards(
                    request.MissionId.ToString(), request.SessionToken.ToString(), request.AttemptOrdinal,
                    firstClear, result.Stars, result.ElapsedMilliseconds, M02MissionId, grants);
            }
            catch (Exception)
            {
                response.ReasonCode = new FixedString64Bytes("settlement-failed");
                return response;
            }

            response.Accepted = receipt.Applied || receipt.IsDuplicate ? (byte)1 : (byte)0;
            response.ReasonCode = new FixedString64Bytes(receipt.Reason);
            return response;
        }

        private static CampaignMissionRewardGrant[] ProjectRewards(ref BlobArray<CampaignMissionRewardBlob> source)
        {
            CampaignMissionRewardGrant[] rewards = new CampaignMissionRewardGrant[source.Length];
            for (int index = 0; index < source.Length; index++)
            {
                ref CampaignMissionRewardBlob reward = ref source[index];
                rewards[index] = new CampaignMissionRewardGrant(
                    reward.Kind, reward.RewardConfigId.ToString(), reward.Amount);
            }
            return rewards;
        }
    }
}
