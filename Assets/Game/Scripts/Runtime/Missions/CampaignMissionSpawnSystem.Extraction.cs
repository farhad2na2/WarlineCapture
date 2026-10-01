using Game.Components;
using Game.Missions.Contracts;
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
            bool evidenceChain = runtime.MissionId.Equals(new Unity.Collections.FixedString64Bytes(CampaignMissionSequence.EvidenceChain));
            bool armored = runtime.MissionId.Equals(new Unity.Collections.FixedString64Bytes(CampaignMissionSequence.GroundedSignal)) ||
                evidenceChain && EvidenceChainRouteChoice.Selected == EvidenceChainExtractionRoute.Armored;
            var handoff = landing.Position;
            if (runtime.MissionId.Equals(new Unity.Collections.FixedString64Bytes("saga.ch01.m04.airlift")) &&
                TryFindAnchor(ref map, new Unity.Collections.FixedString64Bytes("anchor.ch01.m04.transfer"), out var transfer))
                handoff = transfer.Position;
            if (evidenceChain && TryFindAnchor(ref map, new Unity.Collections.FixedString64Bytes("anchor.ch03.m04.route_05"), out var road))
                handoff = road.Position;
            if (armored && TryFindAnchor(ref map, new Unity.Collections.FixedString64Bytes("anchor.ch03.m04.return_rts"), out var checkpoint))
                landing = checkpoint;
            SetOrAdd(em, root, new CampaignMissionExtractionState { SessionToken = runtime.SessionToken,
                AttemptOrdinal = runtime.AttemptOrdinal, SourceVersion = runtime.SourceVersion,
                LandingCenter = landing.Position, DepartureCenter = departure.Position, HandoffCenter = handoff,
                ArmoredRoute = armored ? (byte)1 : (byte)0,
                Initialized = 1 });
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
