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
            bool cinematicInteractionLocked =
                IsOpeningCinematicActive(entityManager, root, in runtime) ||
                IsFinaleCinematicActive(entityManager, root, in runtime);
            if (runtime.Version == 0 || runtime.SourceVersion == 0 ||
                runtime.Phase == MissionPhaseKind.None || runtime.MissionId.Length == 0 ||
                !catalog.Blob.IsCreated)
            {
                if (!cinematicInteractionLocked)
                    return false;

                restrictions = new UiMissionHudRestrictionsModel(
                    runtime.MissionId.ToString(), false, false, false, false, false, true);
                return true;
            }

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
                    definition.AirDisabled != 0,
                    cinematicInteractionLocked,
                    definition.MissionRuntimeEnabled != 0,
                    definition.MissionRuntimeEnabled != 0,
                    definition.MissionRuntimeEnabled != 0);
                return true;
            }

            if (!cinematicInteractionLocked)
                return false;

            restrictions = new UiMissionHudRestrictionsModel(
                runtime.MissionId.ToString(), false, false, false, false, false, true);
            return true;
        }

        private static bool IsOpeningCinematicActive(
            EntityManager entityManager,
            Entity root,
            in CampaignMissionRuntimeComponent runtime)
        {
            if (!entityManager.HasComponent<CampaignMissionOpeningPresentationComponent>(root))
                return false;

            CampaignMissionOpeningPresentationComponent opening =
                entityManager.GetComponentData<CampaignMissionOpeningPresentationComponent>(root);
            return opening.SessionToken.Equals(runtime.SessionToken) && opening.Stage < 6;
        }

        private static bool IsFinaleCinematicActive(
            EntityManager entityManager,
            Entity root,
            in CampaignMissionRuntimeComponent runtime)
        {
            if (!entityManager.HasComponent<CampaignMissionFinalePresentationComponent>(root))
                return false;

            CampaignMissionFinalePresentationComponent finale =
                entityManager.GetComponentData<CampaignMissionFinalePresentationComponent>(root);
            return finale.Required != 0 &&
                   finale.SessionToken.Equals(runtime.SessionToken) &&
                   finale.Stage is >= 1 and <= 3;
        }
    }
}
