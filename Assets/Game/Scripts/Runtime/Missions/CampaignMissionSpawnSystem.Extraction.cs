using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    public partial struct CampaignMissionSpawnSystem
    {
        private static void InitializeExtractionAttempt(EntityManager em, Entity root,
            ref CampaignMissionDefinitionBlob definition, ref OperationMapBlob map, in CampaignMissionRuntimeComponent runtime)
        {
            if (definition.Extraction.Enabled == 0) return;
            TryFindAnchor(ref map, definition.Extraction.LandingAnchorId, out var landing);
            TryFindAnchor(ref map, definition.Extraction.DepartureAnchorId, out var departure);
            SetOrAdd(em, root, new CampaignMissionExtractionState { SessionToken = runtime.SessionToken,
                AttemptOrdinal = runtime.AttemptOrdinal, SourceVersion = runtime.SourceVersion,
                LandingCenter = landing.Position, DepartureCenter = departure.Position, Initialized = 1 });
            SetOrAdd(em, root, new CampaignMissionCameraTourState { SessionToken = runtime.SessionToken,
                AttemptOrdinal = runtime.AttemptOrdinal, SourceVersion = runtime.SourceVersion });
            if (!em.HasBuffer<CampaignMissionExtractionMember>(root)) em.AddBuffer<CampaignMissionExtractionMember>(root);
            em.GetBuffer<CampaignMissionExtractionMember>(root).Clear();
        }

        private static void RegisterExtractionMember(EntityManager em, Entity root, Entity instance,
            ref CampaignMissionDefinitionBlob definition, ref CampaignMissionForceGroupBlob group, in CampaignMissionForceUnitBlob unit)
        {
            if (definition.Extraction.Enabled == 0) return;
            ref var config = ref definition.Extraction;
            byte kind = group.FactionId > FactionIdentity.PlayerFactionId ? (byte)4 :
                unit.MissionRoleId.Equals(config.PassengerRoleId) ? (byte)1 :
                unit.MissionRoleId.Equals(config.CarrierRoleId) ? (byte)2 :
                unit.MissionRoleId.Equals(config.AircraftRoleId) ? (byte)3 : (byte)0;
            if (config.VehiclesSelfSupplied != 0 && em.HasComponent<UnitFuelConsumption>(instance))
            { var fuel = em.GetComponentData<UnitFuelConsumption>(instance); fuel.Enabled = 0; em.SetComponentData(instance, fuel); }
            em.GetBuffer<CampaignMissionExtractionMember>(root).Add(new CampaignMissionExtractionMember { Entity = instance, Kind = kind });
            var extraction = em.GetComponentData<CampaignMissionExtractionState>(root);
            if (kind == 2) extraction.Carrier = instance;
            if (kind == 3) extraction.Aircraft = instance;
            em.SetComponentData(root, extraction);
            if (kind == 4 && !em.HasComponent<CampaignMissionConvoyRouteProgress>(instance))
                em.AddComponent<CampaignMissionConvoyRouteProgress>(instance);
        }
    }
}
