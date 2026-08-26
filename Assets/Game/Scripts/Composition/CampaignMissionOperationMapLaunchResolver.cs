using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;

namespace Game.Composition
{
    internal sealed class CampaignMissionOperationMapReferenceComponent : IComponentData
    {
        public FixedString64Bytes MissionId;
        public FixedString64Bytes ScenarioId;
        public FixedString64Bytes OperationMapId;
        public FixedString64Bytes SessionToken;
        public OperationMapDefinition Definition;
        public ulong TransitionToken;
        public uint SourceVersion;
        public int AttemptOrdinal;
    }

    internal readonly struct OperationMapLaunchSelection
    {
        public OperationMapLaunchSelection(
            FixedString64Bytes missionId,
            FixedString64Bytes scenarioId,
            FixedString64Bytes operationMapId,
            OperationMapDefinition definition,
            bool isCampaign)
        {
            MissionId = missionId;
            ScenarioId = scenarioId;
            OperationMapId = operationMapId;
            Definition = definition;
            IsCampaign = isCampaign;
        }

        public FixedString64Bytes MissionId { get; }
        public FixedString64Bytes ScenarioId { get; }
        public FixedString64Bytes OperationMapId { get; }
        public OperationMapDefinition Definition { get; }
        public bool IsCampaign { get; }
    }

    internal static class CampaignMissionOperationMapLaunchResolver
    {
        public static bool TryResolve(
            World world,
            string fallbackMissionId,
            string fallbackScenarioId,
            string fallbackOperationMapId,
            out OperationMapLaunchSelection selection,
            out OperationMapLoadResultCode failureCode,
            out string error)
        {
            selection = default;
            failureCode = OperationMapLoadResultCode.None;
            error = null;
            if (world == null || !world.IsCreated)
                return TryCreateFallback(
                    fallbackMissionId,
                    fallbackScenarioId,
                    fallbackOperationMapId,
                    out selection,
                    out failureCode,
                    out error);

            EntityManager entityManager = world.EntityManager;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionRootComponent>());
            int rootCount = query.CalculateEntityCount();
            if (rootCount == 0)
            {
                return TryCreateFallback(
                    fallbackMissionId,
                    fallbackScenarioId,
                    fallbackOperationMapId,
                    out selection,
                    out failureCode,
                    out error);
            }

            if (rootCount != 1)
                return Reject("Campaign launch requires exactly one mission root.", out failureCode, out error);

            Entity root = query.GetSingletonEntity();
            if (!entityManager.HasBuffer<CampaignMissionLaunchRequestElement>(root))
                return Reject("Campaign mission root is missing its launch request queue.", out failureCode, out error);

            DynamicBuffer<CampaignMissionLaunchRequestElement> requests =
                entityManager.GetBuffer<CampaignMissionLaunchRequestElement>(root, true);
            if (requests.Length == 0)
            {
                return TryCreateFallback(
                    fallbackMissionId,
                    fallbackScenarioId,
                    fallbackOperationMapId,
                    out selection,
                    out failureCode,
                    out error);
            }

            if (requests.Length != 1)
                return Reject("Campaign launch requires exactly one pending request.", out failureCode, out error);

            CampaignMissionLaunchRequestElement request = requests[0];
            if (!IsValidRequest(in request))
                return Reject("Campaign launch request identity is invalid.", out failureCode, out error);
            if (!entityManager.HasComponent<CampaignMissionCatalogComponent>(root) ||
                !entityManager.HasComponent<CampaignMissionOperationMapReferenceComponent>(root))
            {
                return Reject(
                    "Campaign launch is missing its catalog or operation-map definition reference.",
                    out failureCode,
                    out error);
            }

            CampaignMissionCatalogComponent catalog =
                entityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
            if (!CatalogContainsExactRequest(in catalog, in request))
                return Reject("Campaign launch request does not match exactly one catalog mission.", out failureCode, out error);

            CampaignMissionOperationMapReferenceComponent reference =
                entityManager.GetComponentObject<CampaignMissionOperationMapReferenceComponent>(root);
            OperationMapDefinition definition = reference?.Definition;
            if (definition == null || reference.SourceVersion != catalog.SourceVersion ||
                !reference.MissionId.Equals(request.MissionId) ||
                !reference.ScenarioId.Equals(request.ScenarioId) ||
                !reference.OperationMapId.Equals(request.OperationMapId) ||
                !reference.SessionToken.Equals(request.SessionToken) ||
                reference.TransitionToken != request.TransitionToken ||
                reference.AttemptOrdinal != request.AttemptOrdinal ||
                !request.OperationMapId.Equals(new FixedString64Bytes(definition.OperationMapId)) ||
                !definition.TryValidateMetadata(out error))
            {
                return Reject(
                    string.IsNullOrEmpty(error)
                        ? "Campaign launch operation-map definition reference is stale or mismatched."
                        : "Campaign launch operation-map definition is invalid: " + error,
                    out failureCode,
                    out error);
            }

            selection = new OperationMapLaunchSelection(
                request.MissionId,
                request.ScenarioId,
                request.OperationMapId,
                definition,
                true);
            return true;
        }

        private static bool IsValidRequest(in CampaignMissionLaunchRequestElement request) =>
            request.SchemaVersion == MissionLaunchPayloadFactory.CurrentSchemaVersion &&
            !request.MissionId.IsEmpty &&
            !request.ScenarioId.IsEmpty &&
            !request.OperationMapId.IsEmpty &&
            !request.SessionToken.IsEmpty &&
            request.LaunchOrigin != MissionLaunchOriginKind.None &&
            request.RunKind != MissionRunKind.None &&
            request.AttemptOrdinal >= 0 &&
            request.DeterministicSeed != 0 &&
            OperationMapIdentityRules.IsValidScenarioId(request.ScenarioId.ToString()) &&
            OperationMapIdentityRules.IsValidOperationMapId(request.OperationMapId.ToString());

        private static bool CatalogContainsExactRequest(
            in CampaignMissionCatalogComponent catalog,
            in CampaignMissionLaunchRequestElement request)
        {
            if (!catalog.Blob.IsCreated || catalog.SourceVersion == 0)
                return false;

            int matchCount = 0;
            ref CampaignMissionCatalogBlob blob = ref catalog.Blob.Value;
            for (int index = 0; index < blob.Missions.Length; index++)
            {
                ref CampaignMissionDefinitionBlob definition = ref blob.Missions[index];
                if (definition.MissionId.Equals(request.MissionId) &&
                    definition.ScenarioId.Equals(request.ScenarioId) &&
                    definition.OperationMapId.Equals(request.OperationMapId))
                {
                    matchCount++;
                }
            }

            return matchCount == 1;
        }

        private static bool TryCreateFallback(
            string missionId,
            string scenarioId,
            string operationMapId,
            out OperationMapLaunchSelection selection,
            out OperationMapLoadResultCode failureCode,
            out string error)
        {
            selection = default;
            if (!OperationMapIdentityRules.IsValidOperationMapId(operationMapId))
            {
                failureCode = OperationMapLoadResultCode.InvalidOperationMapId;
                error = $"Operation-map id '{operationMapId ?? "<null>"}' is not present in the catalog.";
                return false;
            }
            if (!OperationMapIdentityRules.IsValidScenarioId(scenarioId) ||
                string.IsNullOrWhiteSpace(missionId) ||
                missionId.Length > OperationMapIdentityRules.MaximumIdLength)
            {
                failureCode = OperationMapLoadResultCode.InvalidRequest;
                error = "Compatibility mission and scenario identities are invalid.";
                return false;
            }

            selection = new OperationMapLaunchSelection(
                new FixedString64Bytes(missionId),
                new FixedString64Bytes(scenarioId),
                new FixedString64Bytes(operationMapId),
                null,
                false);
            failureCode = OperationMapLoadResultCode.None;
            error = null;
            return true;
        }

        private static bool Reject(
            string message,
            out OperationMapLoadResultCode failureCode,
            out string error)
        {
            failureCode = OperationMapLoadResultCode.InvalidRequest;
            error = message;
            return false;
        }
    }
}
