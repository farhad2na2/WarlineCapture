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
        private const string M01MissionId = "saga.ch01.m01.first_contact";
        private const string M02MissionId = "saga.ch01.m02.establish_base";
        private const string M03MissionId = "saga.ch01.m03.radar_warning";

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

            MissionReturnDestinationKind expectedReturn = runtime.LaunchOrigin == MissionLaunchOriginKind.FirstLaunch
                ? MissionReturnDestinationKind.CommandBase
                : MissionReturnDestinationKind.CampaignOperations;
            if (result.ReturnDestination != expectedReturn)
            {
                response.ReasonCode = new FixedString64Bytes("invalid-return-route");
                return response;
            }

            CampaignMissionSettlementReceipt receipt;
            bool firstClear;
            try
            {
                firstClear = ResolveFirstClearSettlement(store, in runtime);
                ref BlobArray<CampaignMissionRewardBlob> rewardSet = ref (
                    firstClear ? ref definition.FirstClearRewards : ref definition.ReplayRewards);
                CampaignMissionRewardGrant[] grants = ProjectRewards(ref rewardSet);
                receipt = store.SettleWithRewards(
                    request.MissionId.ToString(), request.SessionToken.ToString(), request.AttemptOrdinal,
                    firstClear, result.Stars, result.ElapsedMilliseconds,
                    ResolveNextMissionId(in request.MissionId), grants);
            }
            catch (Exception)
            {
                response.ReasonCode = new FixedString64Bytes("settlement-failed");
                return response;
            }

            response.Accepted = receipt.Applied || receipt.IsDuplicate ? (byte)1 : (byte)0;
            response.FirstClear = response.Accepted != 0 && firstClear ? (byte)1 : (byte)0;
            response.ReasonCode = new FixedString64Bytes(receipt.Reason);
            return response;
        }

        private static bool ResolveFirstClearSettlement(
            CampaignMissionProgressStore store,
            in CampaignMissionRuntimeComponent runtime)
        {
            if (runtime.RunKind == MissionRunKind.FirstClear)
                return true;
            if (runtime.RunKind != MissionRunKind.Retry)
                return false;

            string missionId = runtime.MissionId.ToString();
            CampaignMissionProgressSaveData[] progress = store.ReadAll();
            for (int index = 0; index < progress.Length; index++)
            {
                CampaignMissionProgressSaveData entry = progress[index];
                if (entry.missionId == missionId)
                    return !entry.firstClearCompleted;
            }

            return true;
        }

        internal static string ResolveNextMissionId(in FixedString64Bytes missionId)
        {
            if (missionId.Equals(new FixedString64Bytes(M01MissionId)))
                return M02MissionId;
            if (missionId.Equals(new FixedString64Bytes(M02MissionId)))
                return M03MissionId;
            return string.Empty;
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
