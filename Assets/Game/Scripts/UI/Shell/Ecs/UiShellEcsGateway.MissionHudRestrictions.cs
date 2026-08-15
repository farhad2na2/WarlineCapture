using Game.Components;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiMissionHudRestrictionsGateway
    {
        public bool TryReadMissionHudRestrictions(out UiMissionHudRestrictionsModel restrictions)
        {
            restrictions = UiMissionHudRestrictionsModel.Inactive;
            if (!TryGetMissionRoot(out EntityManager entityManager, out Entity root) ||
                !entityManager.HasComponent<CampaignMissionRuntimeComponent>(root) ||
                !entityManager.HasComponent<CampaignMissionCatalogComponent>(root))
                return false;

            CampaignMissionRuntimeComponent runtime =
                entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
            CampaignMissionCatalogComponent catalog =
                entityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
            if (runtime.Version == 0 || runtime.SourceVersion == 0 ||
                runtime.Phase == MissionPhaseKind.None || runtime.MissionId.Length == 0 ||
                !catalog.Blob.IsCreated)
                return false;

            ref CampaignMissionCatalogBlob blob = ref catalog.Blob.Value;
            for (int index = 0; index < blob.Missions.Length; index++)
            {
                ref CampaignMissionDefinitionBlob definition = ref blob.Missions[index];
                if (!definition.MissionId.Equals(runtime.MissionId))
                    continue;

                restrictions = new UiMissionHudRestrictionsModel(
                    runtime.MissionId.ToString(),
                    definition.BuildingDisabled != 0,
                    definition.ProductionDisabled != 0,
                    definition.EconomyDisabled != 0,
                    definition.TransportDisabled != 0,
                    definition.AirDisabled != 0);
                return true;
            }

            return false;
        }
    }
}
