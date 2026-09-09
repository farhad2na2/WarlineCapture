using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public partial struct CampaignMissionSpawnSystem
    {
        private static void InitializeDefenseAttempt(EntityManager em, Entity root,
            ref CampaignMissionDefinitionBlob definition, ref OperationMapBlob map,
            in CampaignMissionRuntimeComponent runtime)
        {
            if (definition.Defense.Enabled == 0) return;
            SetOrAdd(em,root,new CampaignMissionCameraTourState{SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal,SourceVersion=runtime.SourceVersion});
            TryFindAnchor(ref map, definition.Defense.InnerCoreAnchorId, out OperationMapAnchorBlob core);
            SetOrAdd(em, root, new CampaignMissionDefenseStateComponent
            {
                SessionToken = runtime.SessionToken, AttemptOrdinal = runtime.AttemptOrdinal,
                SourceVersion = runtime.SourceVersion, InnerCoreCenter = core.Position, Initialized = 1,
                RuntimeBuildingBaselineId=ReadRuntimeBuildingBaseline(em),ProducedUnitBaselineCount=ReadProducedUnitBaseline(em)
            });
            SetOrAdd(em, root, new ThreatWarningLedgerState
            {
                SessionToken = runtime.SessionToken, AttemptOrdinal = runtime.AttemptOrdinal,
                SourceVersion = runtime.SourceVersion, FocusElementIndex = -1
            });
            SetOrAdd(em, root, new RadarPingState
            {
                SessionToken = runtime.SessionToken, AttemptOrdinal = runtime.AttemptOrdinal,
                SourceVersion = runtime.SourceVersion, Charges = definition.Defense.RadarPingCharges,
                CooldownMilliseconds = definition.Defense.RadarPingCooldownMilliseconds
            });
            if (!em.HasBuffer<RadarPingRequest>(root)) em.AddBuffer<RadarPingRequest>(root);
            em.GetBuffer<RadarPingRequest>(root).Clear();
            if (!em.HasBuffer<MissionDefenseInteractionRequest>(root)) em.AddBuffer<MissionDefenseInteractionRequest>(root);
            em.GetBuffer<MissionDefenseInteractionRequest>(root).Clear();
            if (!em.HasBuffer<ThreatWarningRecord>(root)) em.AddBuffer<ThreatWarningRecord>(root);
            em.GetBuffer<ThreatWarningRecord>(root).Clear();
            if (!em.HasBuffer<ThreatWarningObservation>(root)) em.AddBuffer<ThreatWarningObservation>(root);
            em.GetBuffer<ThreatWarningObservation>(root).Clear();
            if (!em.HasBuffer<CampaignMissionDefenseMember>(root)) em.AddBuffer<CampaignMissionDefenseMember>(root);
            em.GetBuffer<CampaignMissionDefenseMember>(root).Clear();
            if (!em.HasBuffer<CampaignMissionConvoyElementState>(root)) em.AddBuffer<CampaignMissionConvoyElementState>(root);
            DynamicBuffer<CampaignMissionConvoyElementState> elements = em.GetBuffer<CampaignMissionConvoyElementState>(root);
            elements.Clear();
            for (int i = 0; i < definition.Defense.Elements.Length; i++) elements.Add(default);
        }

        internal static int ReadRuntimeBuildingBaseline(EntityManager em)
        {
            using var query=new EntityQueryBuilder(Allocator.Temp).WithAll<RuntimeBuildingCombatInfo>()
                .WithNone<OperationMapBuildingComponent>().Build(em);
            using var buildings=query.ToComponentDataArray<RuntimeBuildingCombatInfo>(Allocator.Temp);
            int maximum=0; foreach(var building in buildings) maximum=Unity.Mathematics.math.max(maximum,building.RuntimeBuildingId);
            return maximum;
        }
        private static int ReadProducedUnitBaseline(EntityManager em)
        {
            using var query=em.CreateEntityQuery(typeof(BuildingRuntimeStateTag),typeof(BuildingProducedUnitReadModel));
            return query.CalculateEntityCount()==1 ? em.GetBuffer<BuildingProducedUnitReadModel>(query.GetSingletonEntity()).Length : 0;
        }

        private static void RegisterDefenseMember(EntityManager em, Entity root, Entity instance,
            ref CampaignMissionDefinitionBlob definition, ref CampaignMissionForceGroupBlob group,
            in CampaignMissionForceUnitBlob unit)
        {
            if (definition.Defense.Enabled == 0) return;
            if (definition.Defense.VehiclesSelfSupplied != 0 && em.HasComponent<UnitFuelConsumption>(instance))
            {
                UnitFuelConsumption fuel = em.GetComponentData<UnitFuelConsumption>(instance);
                fuel.Enabled = 0;
                em.SetComponentData(instance, fuel);
            }
            int elementIndex = -1;
            for (int i = 0; i < definition.Defense.Elements.Length; i++)
                if (definition.Defense.Elements[i].UnitGroupId.Equals(group.GroupId)) elementIndex = i;
            if (elementIndex >= 0) em.AddComponent<CampaignMissionConvoyRouteProgress>(instance);
            em.GetBuffer<CampaignMissionDefenseMember>(root).Add(new CampaignMissionDefenseMember
            {
                Entity = instance, ElementIndex = elementIndex, FactionId = group.FactionId,
                IsSensor = unit.MissionRoleId.Equals(definition.Defense.SensorMissionRoleId) ? (byte)1 : (byte)0
            });
        }
    }
}
