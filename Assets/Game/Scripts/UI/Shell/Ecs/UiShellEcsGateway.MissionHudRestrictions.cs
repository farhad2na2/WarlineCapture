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
            if(TrySkirmish(out var skirmishEm,out _,out var skirmish))
            {
                int mask=0;
                using var units=skirmishEm.CreateEntityQuery(typeof(SkirmishSquadMember),typeof(UnitHealth));
                using var members=units.ToEntityArray(Unity.Collections.Allocator.Temp);
                foreach(var member in members)if(skirmishEm.GetComponentData<UnitHealth>(member).Current>0)mask|=1<<skirmishEm.GetComponentData<SkirmishSquadMember>(member).Slot;
                restrictions=new UiMissionHudRestrictionsModel("skirmish.base_assault",false,false,false,true,true,
                    skirmish.Phase!=SkirmishPhase.Playing,false,true,false,mask);
                return true;
            }
            if (!TryGetMissionRoot(out EntityManager entityManager, out Entity root) ||
                !entityManager.HasComponent<CampaignMissionRuntimeComponent>(root) ||
                !entityManager.HasComponent<CampaignMissionCatalogComponent>(root))
                return false;

            CampaignMissionRuntimeComponent runtime =
                entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
            CampaignMissionCatalogComponent catalog =
                entityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
            // Cleared campaign components remain on the root between modes. Two
            // empty session tokens must never turn Skirmish into a cinematic.
            bool cinematicInteractionLocked = runtime.Phase != MissionPhaseKind.None &&
                !runtime.SessionToken.IsEmpty &&
                (IsOpeningCinematicActive(entityManager, root, in runtime) ||
                 IsFinaleCinematicActive(entityManager, root, in runtime));
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

                bool radar = runtime.MissionId.Equals(new Unity.Collections.FixedString64Bytes("saga.ch01.m03.radar_warning"));
                bool introductory = runtime.MissionId.Equals(new Unity.Collections.FixedString64Bytes("saga.ch01.m02.establish_base"));
                restrictions = new UiMissionHudRestrictionsModel(
                    runtime.MissionId.ToString(),
                    definition.BuildingDisabled != 0,
                    definition.ProductionDisabled != 0,
                    definition.EconomyDisabled != 0,
                    definition.TransportDisabled != 0,
                    definition.AirDisabled != 0,
                    cinematicInteractionLocked,
                    definition.MissionRuntimeEnabled != 0 && !radar && !introductory,
                    definition.MissionRuntimeEnabled != 0,
                    definition.MissionRuntimeEnabled != 0 && !radar && !introductory,
                    ReadExtractionSquadMask(entityManager,root),
                    IsOpeningCinematicActive(entityManager,root,in runtime));
                return true;
            }

            if (!cinematicInteractionLocked)
                return false;

            restrictions = new UiMissionHudRestrictionsModel(
                runtime.MissionId.ToString(), false, false, false, false, false, true);
            return true;
        }

        private static int ReadExtractionSquadMask(EntityManager em,Entity root)
        {
            if(!em.HasBuffer<CampaignMissionExtractionMember>(root) || !em.HasComponent<CampaignMissionExtractionState>(root) || em.GetComponentData<CampaignMissionExtractionState>(root).Initialized==0) return -1;
            int mask=0;
            foreach(var member in em.GetBuffer<CampaignMissionExtractionMember>(root,true))
            {
                if(!em.HasComponent<UnitHealth>(member.Entity) || em.GetComponentData<UnitHealth>(member.Entity).Current<=0) continue;
                mask|=member.Kind switch {0=>1,2=>2|16,3=>4|16,_=>0};
            }
            return mask;
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
