using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    internal struct ArmorBreakUnitActivationState : IComponentData
    {
        public byte OriginalFuelEnabled, OriginalAutoEngage;
    }

    public partial struct CampaignMissionRuntimeSystem
    {
        private static readonly FixedString64Bytes ArmorBreakId = CampaignMissionSequence.ArmorBreak;
        private static readonly FixedString128Bytes ArmorBreakFuelBuilding = "Building_ArmorBreak_ReserveDepot";
        private const float ArmorBreakStartingFuel = 200f, ArmorBreakCivilianFloor = 40f;

        private bool TryAdvanceArmorBreak(ref SystemState system, Entity root, in CampaignMissionRuntimeComponent runtime)
        {
            if (!runtime.MissionId.Equals(ArmorBreakId)) return false;
            if (runtime.Outcome != MissionOutcomeKind.None) return true;
            var em = system.EntityManager;
            var facts = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            // Brief readiness precedes spawned gameplay entities; do not require a roster to open the mission.
            if (runtime.Phase == MissionPhaseKind.Preparing && (runtime.ReadyReadiness & runtime.RequiredReadiness) == runtime.RequiredReadiness)
            {
                if (TryTransition(in runtime, MissionPhaseKind.InteractiveBrief, MissionOutcomeKind.None, MissionReturnDestinationKind.None, out var brief)) em.SetComponentData(root, brief);
                return true;
            }
            if (runtime.Phase == MissionPhaseKind.InteractiveBrief && facts.InteractiveBriefCompleted != 0)
            {
                if (TryTransition(in runtime, MissionPhaseKind.FindSquad, MissionOutcomeKind.None, MissionReturnDestinationKind.None, out var launch)) em.SetComponentData(root, launch);
                return true;
            }
            if (facts.CommandSquadSpawned == 0 || !SystemAPI.TryGetSingleton(out OperationMapMetadataComponent metadata) || !metadata.Blob.IsCreated) return true;
            if (!em.HasComponent<CampaignMissionArmorBreakState>(root) || em.GetComponentData<CampaignMissionArmorBreakState>(root).Initialized == 0)
                InitializeArmorBreak(em, root, in runtime, ref metadata.Blob.Value);
            var mission = em.GetComponentData<CampaignMissionArmorBreakState>(root);
            if (!CampaignMissionArmorBreakRuleUtility.Matches(in mission, in runtime)) return true;
            ProjectArmorBreakFuel(em, ref metadata.Blob.Value, ref mission);
            var supportFuel = new SupportFuelScopeComponent { Storage = mission.FuelReserve, Required = 1 };
            if (em.HasComponent<SupportFuelScopeComponent>(root)) em.SetComponentData(root, supportFuel);
            else em.AddComponentData(root, supportFuel);
            bool opening = em.HasComponent<CampaignMissionOpeningPresentationComponent>(root) &&
                em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).SessionToken.Equals(runtime.SessionToken) &&
                em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).Stage >= 6;
            bool active = runtime.Phase == MissionPhaseKind.Engage && opening && mission.Ready != 0;
            ApplyArmorBreakActivationPolicy(em,root,in mission,active);
            if (active) mission.ElapsedMilliseconds = SaturatingAddMilliseconds(mission.ElapsedMilliseconds, SystemAPI.Time.DeltaTime);
            if (mission.ElapsedMilliseconds >= CampaignMissionArmorBreakRuleUtility.DeadlineMilliseconds) mission.Failure = ArmorBreakFailure.Deadline;
            var members = em.GetBuffer<CampaignMissionArmorBreakMember>(root);
            int initialized = 0, hostiles = 0, defeated = 0, air = 0, airDead = 0, command = 0, commandDead = 0, infantry = 0, infantryAtPackage = 0,
                armor = 0, armorAtApproach = 0, infantryAlive = 0, losses = 0, reliefLosses = 0, hostileArmor = 0, hostileArmorDead = 0;
            bool contested = false, airDefenseLost = false, launcherLost = false;
            for (int i = 0; i < members.Length; i++)
            {
                var member = members[i];
                bool exists = em.Exists(member.Entity) && em.HasComponent<UnitHealth>(member.Entity);
                if (!exists && member.Dead == 0) { mission.Failure = ArmorBreakFailure.Integrity; continue; }
                var health = exists ? em.GetComponentData<UnitHealth>(member.Entity) : default;
                if (health.Max > 0) member.HealthInitialized = 1;
                if (member.HealthInitialized != 0 && health.Current <= 0) member.Dead = 1;
                initialized += member.HealthInitialized;
                members[i] = member;
                bool hostile = member.Kind is ArmorBreakMemberKind.Battery or ArmorBreakMemberKind.HostileArmor or ArmorBreakMemberKind.HostileCommand or ArmorBreakMemberKind.HostileAir;
                if (hostile)
                {
                    hostiles++; defeated += member.Dead;
                    if(member.Kind==ArmorBreakMemberKind.HostileArmor){hostileArmor++;hostileArmorDead+=member.Dead;}
                    if (member.Kind == ArmorBreakMemberKind.HostileAir) { air++; airDead += member.Dead; }
                    else if (member.Kind != ArmorBreakMemberKind.Battery) { command++; commandDead += member.Dead; }
                    if (member.Kind == ArmorBreakMemberKind.Battery && member.Dead != 0) mission.BatteryDisabled = 1;
                    if (member.Dead == 0 && exists && em.HasComponent<LocalTransform>(member.Entity) &&
                        !em.HasComponent<CampaignMissionCombatSuppressedTag>(member.Entity) &&
                        math.distancesq(em.GetComponentData<LocalTransform>(member.Entity).Position.xz, mission.AuthorityCenter.xz) <= mission.AuthorityRadius * mission.AuthorityRadius) contested = true;
                    if (exists && em.HasComponent<SupportTargetEligibilityComponent>(member.Entity))
                    {
                        var knowledge = em.GetComponentData<SupportTargetEligibilityComponent>(member.Entity);
                        byte known = (byte)(active && member.Dead == 0 && !em.HasComponent<CampaignMissionCombatSuppressedTag>(member.Entity) ? 1 : 0);
                        if (knowledge.CurrentlyVisible != known || knowledge.HostileConfirmed != known)
                        { knowledge.CurrentlyVisible = knowledge.HostileConfirmed = known; knowledge.SourceVersion++; em.SetComponentData(member.Entity, knowledge); }
                    }
                }
                else if (member.Kind == ArmorBreakMemberKind.Relief) reliefLosses += member.Dead;
                else
                {
                    losses += member.Dead;
                    if (member.Kind == ArmorBreakMemberKind.Infantry)
                    {
                        infantry++;
                        if (member.Dead == 0) infantryAlive++;
                        if (member.Dead == 0 && exists && em.HasComponent<LocalTransform>(member.Entity) && !em.HasComponent<UnitTransportPassenger>(member.Entity) &&
                            !em.HasComponent<Disabled>(member.Entity) && !em.HasComponent<UnitPathRequest>(member.Entity) && !em.HasComponent<UnitPathFollow>(member.Entity) &&
                            math.distancesq(em.GetComponentData<LocalTransform>(member.Entity).Position.xz, mission.AuthorityCenter.xz) <= mission.AuthorityRadius * mission.AuthorityRadius) infantryAtPackage++;
                    }
                    if (member.Kind == ArmorBreakMemberKind.Armor && member.Dead == 0)
                    {
                        armor++;
                        if (exists && em.HasComponent<LocalTransform>(member.Entity) &&
                            math.distancesq(em.GetComponentData<LocalTransform>(member.Entity).Position.xz, mission.AssaultApproach.xz) <= 400f) armorAtApproach++;
                    }
                    if (member.Kind == ArmorBreakMemberKind.AirDefense && member.Dead != 0) airDefenseLost = true;
                    if (member.Kind == ArmorBreakMemberKind.Launcher && member.Dead != 0) launcherLost = true;
                }
            }
            if (hostiles != 12 || infantry != 4 || members.Length != 26) mission.Failure = ArmorBreakFailure.Integrity;
            if (infantry == 4 && infantryAlive == 0) mission.Failure = ArmorBreakFailure.CommandSquadLost;
            if (reliefLosses > 0) { mission.ReliefLost = 1; mission.Failure = ArmorBreakFailure.ReliefLost; }
            if (air == 3 && airDead == 3) mission.AirCleared = 1;
            if (command == 8 && commandDead == 8) mission.CommandDisabled = 1;
            if(hostileArmor==3 && hostileArmorDead==3)mission.ArmoredThreatsCleared=1;
            if (active && IsLiveArmorBreakEntity(em, mission.AirDefense) && !em.HasComponent<UnitPathRequest>(mission.AirDefense) && !em.HasComponent<UnitPathFollow>(mission.AirDefense) &&
                math.distancesq(em.GetComponentData<LocalTransform>(mission.AirDefense).Position.xz, mission.CoverageCenter.xz) <= mission.CoverageRadius * mission.CoverageRadius) mission.CoverageReady = 1;
            if (em.Exists(mission.Launcher) && em.HasComponent<SplitFrontLauncherCommandState>(mission.Launcher)) mission.LauncherShots = em.GetComponentData<SplitFrontLauncherCommandState>(mission.Launcher).Launches;
            if (CampaignMissionArmorBreakAircraftOrderSystem.HasObservedShot(em,in mission)) mission.AircraftUsed=1;
            if (em.Exists(mission.Aircraft) && em.HasComponent<UnitHealth>(mission.Aircraft) && em.GetComponentData<UnitHealth>(mission.Aircraft).Max>0 &&
                em.GetComponentData<UnitHealth>(mission.Aircraft).Current<=0 && (mission.AircraftUsed==0 || mission.ArmoredThreatsCleared==0)) mission.Failure=ArmorBreakFailure.AircraftLost;
            // Qualify losses only after observing this frame's complete original roster
            // and launcher/aircraft shot counters. Roster order cannot reject a final shot.
            if (airDefenseLost && mission.AirCleared == 0) mission.Failure = ArmorBreakFailure.AirDefenseLost;
            if (launcherLost && (mission.LauncherShots == 0 || mission.BatteryDisabled == 0)) mission.Failure = ArmorBreakFailure.LauncherLost;
            if (armor == 0 && mission.CommandDisabled == 0) mission.Failure = ArmorBreakFailure.ArmorLost;
            // Once these original targets are gone, the required player-issued
            // combined-arms demonstrations cannot be completed on replacements.
            if (mission.BatteryDisabled != 0 && mission.LauncherShots == 0 || mission.ArmoredThreatsCleared != 0 && mission.AircraftUsed == 0)
                mission.Failure = ArmorBreakFailure.MasteryLost;
            if (active && armor > 0 && armorAtApproach == armor && CampaignMissionArmorBreakRuleUtility.Stage(in mission) >= 5) mission.ArmorApproached = 1;
            if (initialized == members.Length && mission.FuelReserve != Entity.Null) mission.Ready = 1;
            bool qualified = active && CampaignMissionArmorBreakRuleUtility.Stage(in mission) == 7 && infantryAlive > 0 && infantryAtPackage == infantryAlive && !contested && mission.Failure == ArmorBreakFailure.None;
            mission.RecoveryMilliseconds = CampaignMissionArmorBreakRuleUtility.AdvanceRecovery(mission.RecoveryMilliseconds,
                (int)math.round(SystemAPI.Time.DeltaTime * 1000f), qualified);
            if (mission.RecoveryMilliseconds >= CampaignMissionArmorBreakRuleUtility.HoldMilliseconds) mission.AuthorityRecovered = 1;
            facts.ElapsedMilliseconds = mission.ElapsedMilliseconds; facts.HostileTotalCount = hostiles; facts.HostileDefeatedCount = defeated;
            facts.SquadLossCount = losses; facts.CommandSquadAlive = infantryAlive > 0 ? (byte)1 : (byte)0;
            facts.CivilianTotalCount = 3; facts.CivilianLossCount = reliefLosses;
            facts.ArmorBreakCoverageReady = mission.CoverageReady; facts.ArmorBreakArmoredThreatsCleared = mission.ArmoredThreatsCleared;
            facts.ArmorBreakAircraftUsed = mission.AircraftUsed; facts.ArmorBreakArmorApproached = mission.ArmorApproached;
            facts.ArmorBreakLauncherShots = mission.LauncherShots;
            facts.ArmorBreakAirCleared = mission.AirCleared; facts.ArmorBreakHeavyDisabled = mission.BatteryDisabled;
            facts.ArmorBreakCommandDisabled = mission.CommandDisabled; facts.ArmorBreakAuthorityRecovered = mission.AuthorityRecovered;
            facts.ArmorBreakReliefLost = mission.ReliefLost; facts.ArmorBreakRecoveryMilliseconds = mission.RecoveryMilliseconds; facts.ArmorBreakFailure = mission.Failure;
            facts.HostileRosterIntegrityFault = mission.Failure == ArmorBreakFailure.Integrity ? (byte)1 : facts.HostileRosterIntegrityFault;
            if (em.HasBuffer<CampaignMissionDefenseMember>(root))
            {
                var defenseMembers=em.GetBuffer<CampaignMissionDefenseMember>(root);
                for(int i=0;i<defenseMembers.Length;i++)
                {
                    var defenseMember=defenseMembers[i];
                    foreach(var member in members)if(member.Entity==defenseMember.Entity){defenseMember.HealthInitialized=member.HealthInitialized;defenseMember.Defeated=member.Dead;break;}
                    defenseMembers[i]=defenseMember;
                }
            }
            em.SetComponentData(root, mission); em.SetComponentData(root, facts);
            if (CampaignMissionArmorBreakRuleUtility.TryAdvance(in runtime, in facts, in mission, opening, out var next)) em.SetComponentData(root, next);
            return true;
        }

        internal static void ApplyArmorBreakSpawnPolicy(EntityManager em,Entity entity)
        {
            var activation=new ArmorBreakUnitActivationState
            {
                OriginalFuelEnabled=em.HasComponent<UnitFuelConsumption>(entity)?em.GetComponentData<UnitFuelConsumption>(entity).Enabled:(byte)0,
                OriginalAutoEngage=em.HasComponent<UnitCombat>(entity)?em.GetComponentData<UnitCombat>(entity).AutoEngage:(byte)0
            };
            if(!em.HasComponent<ArmorBreakUnitActivationState>(entity))em.AddComponentData(entity,activation);
            if(em.HasComponent<UnitFuelConsumption>(entity)){var fuel=em.GetComponentData<UnitFuelConsumption>(entity);fuel.Enabled=0;em.SetComponentData(entity,fuel);}
            if(em.HasComponent<UnitCombat>(entity)){var combat=em.GetComponentData<UnitCombat>(entity);combat.AutoEngage=0;em.SetComponentData(entity,combat);}
            if(em.HasComponent<UnitMovementBehavior>(entity)){var move=em.GetComponentData<UnitMovementBehavior>(entity);move.AllowIdleWander=0;em.SetComponentData(entity,move);}
        }

        private static void ApplyArmorBreakActivationPolicy(EntityManager em,Entity root,in CampaignMissionArmorBreakState mission,bool active)
        {
            using var ecb=new EntityCommandBuffer(Allocator.Temp);
            foreach(var member in em.GetBuffer<CampaignMissionArmorBreakMember>(root,true))
            {
                if(member.Dead!=0 || !em.Exists(member.Entity))continue;
                var entity=member.Entity;
                if(em.HasComponent<ArmorBreakUnitActivationState>(entity))
                {
                    var original=em.GetComponentData<ArmorBreakUnitActivationState>(entity);
                    if(em.HasComponent<UnitFuelConsumption>(entity))
                    {
                        var fuel=em.GetComponentData<UnitFuelConsumption>(entity);byte enabled=active?original.OriginalFuelEnabled:(byte)0;
                        if(fuel.Enabled!=enabled)
                        {
                            fuel.Enabled=enabled;em.SetComponentData(entity,fuel);
                            // Briefing/opening movement never becomes a deferred debit.
                            if(enabled!=0 && em.HasComponent<UnitFuelConsumptionState>(entity))em.SetComponentData(entity,default(UnitFuelConsumptionState));
                        }
                    }
                    if(em.HasComponent<UnitCombat>(entity))
                    {
                        bool autonomous=active && (member.Kind==ArmorBreakMemberKind.AirDefense ||
                            member.Kind==ArmorBreakMemberKind.Radar && mission.CommandDisabled!=0 ||
                            member.Kind==ArmorBreakMemberKind.Armor && mission.ArmorApproached!=0 ||
                            member.Kind==ArmorBreakMemberKind.Aircraft && mission.AircraftUsed!=0 ||
                            member.Kind==ArmorBreakMemberKind.Infantry && mission.CommandDisabled!=0);
                        var combat=em.GetComponentData<UnitCombat>(entity);combat.AutoEngage=autonomous?original.OriginalAutoEngage:(byte)0;em.SetComponentData(entity,combat);
                        if(!autonomous && em.HasComponent<EngageTarget>(entity) && em.GetComponentData<EngageTarget>(entity).IsCommanded==0)ecb.RemoveComponent<EngageTarget>(entity);
                    }
                }
                // Military armor is already targetable and armed during helicopter mastery,
                // and stays in its fortified position until the helicopter clears the original group.
                if(member.Kind==ArmorBreakMemberKind.HostileArmor)
                {
                    bool stationary=mission.ArmoredThreatsCleared==0;
                    if(stationary && !em.HasComponent<CampaignMissionStationaryUnitTag>(entity))ecb.AddComponent<CampaignMissionStationaryUnitTag>(entity);
                    if(!stationary && em.HasComponent<CampaignMissionStationaryUnitTag>(entity))ecb.RemoveComponent<CampaignMissionStationaryUnitTag>(entity);
                }
            }
            ecb.Playback(em);
        }

        private static bool IsLiveArmorBreakEntity(EntityManager em, Entity entity) => em.Exists(entity) && em.HasComponent<UnitHealth>(entity) &&
            em.GetComponentData<UnitHealth>(entity).Current > 0 && em.HasComponent<LocalTransform>(entity);

        private static void InitializeArmorBreak(EntityManager em, Entity root, in CampaignMissionRuntimeComponent runtime, ref OperationMapBlob map)
        {
            var mission = new CampaignMissionArmorBreakState { SessionToken = runtime.SessionToken, AttemptOrdinal = runtime.AttemptOrdinal, SourceVersion = runtime.SourceVersion, Initialized = 1, LowestUsableFuel = 160f };
            bool hasCoverage = CampaignMissionSpawnSystem.TryFindAnchor(ref map, "anchor.ch04.m05.coverage", out var coverage);
            bool hasApproach = CampaignMissionSpawnSystem.TryFindAnchor(ref map, "anchor.ch04.m05.assault_approach", out var approach);
            bool hasAuthority = CampaignMissionSpawnSystem.TryFindAnchor(ref map, "anchor.ch04.m05.authority", out var authority);
            bool hasRelief = CampaignMissionSpawnSystem.TryFindAnchor(ref map, "anchor.ch04.m05.relief", out var relief);
            if (!hasCoverage || !hasApproach || !hasAuthority || !hasRelief) mission.Failure = ArmorBreakFailure.Integrity;
            mission.CoverageCenter = coverage.Position; mission.CoverageRadius = math.max(6, coverage.Radius);
            mission.AssaultApproach = approach.Position; mission.AuthorityCenter = authority.Position; mission.AuthorityRadius = math.max(1, authority.Radius);
            mission.ReliefCenter = relief.Position; mission.ReliefRadius = math.max(1, relief.Radius);
            if (!em.HasBuffer<CampaignMissionArmorBreakMember>(root)) em.AddBuffer<CampaignMissionArmorBreakMember>(root);
            var roster = em.GetBuffer<CampaignMissionArmorBreakMember>(root); roster.Clear();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            using var query = em.CreateEntityQuery(typeof(CampaignMissionUnitRoleComponent), typeof(UnitHealth), typeof(LocalTransform));
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var role = em.GetComponentData<CampaignMissionUnitRoleComponent>(entity);
                if (!role.SessionToken.Equals(runtime.SessionToken)) continue;
                if (!TryArmorBreakKind(role.MissionRoleId, out var kind)) { mission.Failure = ArmorBreakFailure.Integrity; continue; }
                roster.Add(new CampaignMissionArmorBreakMember { Entity = entity, Kind = kind });
                if (kind == ArmorBreakMemberKind.AirDefense) mission.AirDefense = entity;
                if (kind == ArmorBreakMemberKind.Launcher) mission.Launcher = entity;
                if (kind == ArmorBreakMemberKind.Radar) mission.Radar = entity;
                if (kind == ArmorBreakMemberKind.Aircraft) { mission.Aircraft = entity; mission.AircraftShotBaseline = em.HasComponent<UnitAttackTraceComponent>(entity) ? em.GetComponentData<UnitAttackTraceComponent>(entity).ShotCounter : 0; }
                if(kind==ArmorBreakMemberKind.Aircraft && !em.HasComponent<CampaignMissionArmorBreakAircraftStandoff>(entity))
                    ecb.AddComponent(entity,new CampaignMissionArmorBreakAircraftStandoff{SessionToken=runtime.SessionToken,Padding=10f});
                if (kind == ArmorBreakMemberKind.Battery) mission.Battery = entity;
                if (kind is ArmorBreakMemberKind.Battery or ArmorBreakMemberKind.HostileCommand or ArmorBreakMemberKind.Relief)
                {
                    if (!em.HasComponent<CampaignMissionStationaryUnitTag>(entity)) ecb.AddComponent<CampaignMissionStationaryUnitTag>(entity);
                    if (em.HasComponent<UnitMovementBehavior>(entity)) { var movement = em.GetComponentData<UnitMovementBehavior>(entity); movement.AllowIdleWander = 0; em.SetComponentData(entity, movement); }
                }
                if (kind == ArmorBreakMemberKind.Relief && em.HasComponent<UnitCombat>(entity)) { var combat = em.GetComponentData<UnitCombat>(entity); combat.CanAttack = combat.AutoEngage = 0; em.SetComponentData(entity, combat); }
                if (kind is ArmorBreakMemberKind.Battery or ArmorBreakMemberKind.HostileArmor or ArmorBreakMemberKind.HostileCommand or ArmorBreakMemberKind.HostileAir)
                    if (em.HasComponent<UnitFuelConsumption>(entity)) { var fuel = em.GetComponentData<UnitFuelConsumption>(entity); fuel.Enabled = 0; em.SetComponentData(entity, fuel); }
            }
            if (roster.Length != 26 || mission.AirDefense == Entity.Null || mission.Launcher == Entity.Null || mission.Radar == Entity.Null || mission.Aircraft == Entity.Null || mission.Battery == Entity.Null) mission.Failure = ArmorBreakFailure.Integrity;
            if (mission.Launcher != Entity.Null && em.HasComponent<GroundMissileLauncherComponent>(mission.Launcher))
                ecb.AddComponent(mission.Launcher, new SplitFrontLauncherCommandState { ProtectedCenter = mission.ReliefCenter, ProtectedRadius = mission.ReliefRadius });
            else mission.Failure = ArmorBreakFailure.Integrity;
            ecb.Playback(em); ecb.Dispose();
            if (em.HasComponent<CampaignMissionArmorBreakState>(root)) em.SetComponentData(root, mission); else em.AddComponentData(root, mission);
        }

        private static bool TryArmorBreakKind(in FixedString64Bytes role, out ArmorBreakMemberKind kind)
        {
            string value = role.ToString();
            kind = value switch { "role.friendly.g2a" => ArmorBreakMemberKind.AirDefense, "role.friendly.g2g" => ArmorBreakMemberKind.Launcher,
                "role.friendly.radar" => ArmorBreakMemberKind.Radar, "role.friendly.armor" => ArmorBreakMemberKind.Armor,
                "role.friendly.command_squad" => ArmorBreakMemberKind.Infantry, "role.friendly.aircraft" => ArmorBreakMemberKind.Aircraft,
                "role.hostile.battery" => ArmorBreakMemberKind.Battery, "role.hostile.armor" => ArmorBreakMemberKind.HostileArmor,
                "role.hostile.command" => ArmorBreakMemberKind.HostileCommand, "role.hostile.air" => ArmorBreakMemberKind.HostileAir,
                "role.civilian.protected" => ArmorBreakMemberKind.Relief, _ => (ArmorBreakMemberKind)255 };
            return (byte)kind != 255;
        }

        private static void ProjectArmorBreakFuel(EntityManager em, ref OperationMapBlob map, ref CampaignMissionArmorBreakState mission)
        {
            if (mission.FuelReserve == Entity.Null)
            {
                using var boundaryQuery = em.CreateEntityQuery(typeof(BuildingRuntimeStateTag));
                if (boundaryQuery.CalculateEntityCount() != 1 || !CampaignMissionSpawnSystem.TryFindAnchor(ref map, "anchor.ch04.m05.fuel_reserve", out var anchor)) return;
                var boundary = boundaryQuery.GetSingletonEntity();
                if (!em.HasBuffer<BuildingRuntimeSpawnRequest>(boundary)) return;
                var requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
                if (mission.FuelSpawnRequestId == 0)
                {
                    int id = 1; foreach (var request in requests) id = math.max(id, request.RequestId + 1);
                    mission.FuelSpawnRequestId = id;
                    requests.Add(new BuildingRuntimeSpawnRequest { RequestId = id, RequestKind = BuildingRuntimeSpawnRequest.KindBuilding, HasOwnerFaction = 1,
                        FactionId = 1, BuildingId = ArmorBreakFuelBuilding, PreferredOrigin = CampaignMissionSpawnSystem.ToGridCell(anchor.Position, map.Grid) });
                }
                else foreach (var request in requests)
                {
                    if (request.RequestId != mission.FuelSpawnRequestId) continue;
                    if (request.Status == BuildingRuntimeSpawnRequest.Failed) { mission.Failure = ArmorBreakFailure.Integrity; break; }
                    if (request.Status != BuildingRuntimeSpawnRequest.Succeeded) break;
                    using var buildings = em.CreateEntityQuery(typeof(RuntimeBuildingCombatInfo), typeof(UnitHealth), typeof(BuildingResourceStorageComponent));
                    using var entities = buildings.ToEntityArray(Allocator.Temp);
                    foreach (var entity in entities)
                    {
                        var info = em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                        if (info.RuntimeBuildingId != request.BuildingRuntimeId || info.OwnerFactionId != 1) continue;
                        using var boundsQuery = em.CreateEntityQuery(typeof(OperationMapMetadataComponent), typeof(OperationMapBoundsComponent));
                        if (boundsQuery.CalculateEntityCount() != 1) { mission.Failure = ArmorBreakFailure.Integrity; break; }
                        var bounds = em.GetComponentData<OperationMapBoundsComponent>(boundsQuery.GetSingletonEntity());
                        float2 footprintMin = map.Grid.Origin.xz + (float2)request.ActualOrigin * map.Grid.CellSize;
                        float2 footprintMax = footprintMin + (float2)request.ActualFootprint * map.Grid.CellSize;
                        if (math.any(footprintMin < bounds.PlayableMin.xz) || math.any(footprintMax > bounds.PlayableMax.xz)) { mission.Failure = ArmorBreakFailure.Integrity; break; }
                        var storage = em.GetComponentData<BuildingResourceStorageComponent>(entity);
                        if (storage.FuelStorageCapacity < ArmorBreakStartingFuel) { mission.Failure = ArmorBreakFailure.Integrity; break; }
                        storage.StoredFuelBarrels = ArmorBreakStartingFuel; storage.CivilianFuelReserveBarrels = ArmorBreakCivilianFloor; storage.Version++;
                        em.SetComponentData(entity, storage); mission.FuelReserve = entity; break;
                    }
                    break;
                }
                return;
            }
            if (!em.Exists(mission.FuelReserve) || !em.HasComponent<UnitHealth>(mission.FuelReserve) || em.GetComponentData<UnitHealth>(mission.FuelReserve).Current <= 0 ||
                !em.HasComponent<BuildingResourceStorageComponent>(mission.FuelReserve)) { mission.Failure = ArmorBreakFailure.FuelReserveLost; return; }
            var reserve = em.GetComponentData<BuildingResourceStorageComponent>(mission.FuelReserve);
            if (reserve.StoredFuelBarrels + .001f < ArmorBreakCivilianFloor || reserve.CivilianFuelReserveBarrels < ArmorBreakCivilianFloor) { mission.Failure = ArmorBreakFailure.FuelReserveLost; return; }
            float usable = math.max(0, reserve.StoredFuelBarrels - reserve.ReservedFuelOutboundBarrels - reserve.CivilianFuelReserveBarrels);
            mission.LowestUsableFuel = math.min(mission.LowestUsableFuel, usable);
            if (usable < 160f - .01f) mission.FuelSpent = 1;
        }
    }
}
