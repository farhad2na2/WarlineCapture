using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    public partial struct CampaignMissionRuntimeSystem
    {
        private static readonly FixedString64Bytes CitywideId = CampaignMissionSequence.CitywideAlert;
        private bool TryAdvanceCitywideAlert(ref SystemState system, Entity root, in CampaignMissionRuntimeComponent runtime)
        {
            if (!runtime.MissionId.Equals(CitywideId)) return false;
            if (runtime.Outcome != MissionOutcomeKind.None) return true;
            var em = system.EntityManager;var facts = em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            if (runtime.Phase == MissionPhaseKind.Preparing && (runtime.ReadyReadiness & runtime.RequiredReadiness) == runtime.RequiredReadiness)
            {
                if (TryTransition(in runtime,MissionPhaseKind.InteractiveBrief,MissionOutcomeKind.None,MissionReturnDestinationKind.None,out var brief)) em.SetComponentData(root,brief);
                return true;
            }
            if (runtime.Phase == MissionPhaseKind.InteractiveBrief && facts.InteractiveBriefCompleted != 0)
            {
                if (TryTransition(in runtime,MissionPhaseKind.FindSquad,MissionOutcomeKind.None,MissionReturnDestinationKind.None,out var launch)) em.SetComponentData(root,launch);
                return true;
            }
            if (facts.CommandSquadSpawned == 0 || !SystemAPI.TryGetSingleton(out OperationMapMetadataComponent metadata) || !metadata.Blob.IsCreated) return true;
            if (!em.HasComponent<CampaignMissionCitywideAlertState>(root) || em.GetComponentData<CampaignMissionCitywideAlertState>(root).Initialized == 0)
                InitializeCitywideAlert(em,root,in runtime,ref metadata.Blob.Value);
            var mission = em.GetComponentData<CampaignMissionCitywideAlertState>(root);
            if (!CampaignMissionCitywideAlertRuleUtility.Matches(in mission,in runtime)) return true;
            ProjectCitywideBuildings(em,root,ref metadata.Blob.Value,ref mission);
            ProjectCitywideProducerReadiness(em,root,in runtime,in mission);
            var scope = new SupportFuelScopeComponent { Storage = mission.ClinicReserve, Required = 1 };
            if (em.HasComponent<SupportFuelScopeComponent>(root)) em.SetComponentData(root,scope);else em.AddComponentData(root,scope);
            bool opening = em.HasComponent<CampaignMissionOpeningPresentationComponent>(root) &&
                em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).SessionToken.Equals(runtime.SessionToken) &&
                em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).Stage >= 6;
            bool active = runtime.Phase == MissionPhaseKind.Engage && opening && mission.Ready != 0;
            ApplyCitywideActivationPolicy(em,root,active);
            int delta = active ? (int)math.round(SystemAPI.Time.DeltaTime * 1000f) : 0;
            if (active) mission.ElapsedMilliseconds = SaturatingAddMilliseconds(mission.ElapsedMilliseconds,SystemAPI.Time.DeltaTime);
            if (mission.ElapsedMilliseconds >= CampaignMissionCitywideAlertRuleUtility.DeadlineMilliseconds) mission.Failure = CitywideAlertFailure.Deadline;
            var members = em.GetBuffer<CampaignMissionCitywideAlertMember>(root);
            int initialized=0,hostiles=0,defeated=0,clinic=0,clinicDead=0,utility=0,utilityDead=0,air=0,airDead=0,staff=0,staffDead=0,friendlyAlive=0,losses=0;
            bool clinicBlocked=false,utilityBlocked=false,engineerLost=false;
            for (int i=0;i<members.Length;i++)
            {
                var member=members[i];bool exists=em.Exists(member.Entity)&&em.HasComponent<UnitHealth>(member.Entity);
                if (!exists && member.Dead==0) { MarkCitywideIntegrity(ref mission,$"original disappeared index={i} entity={member.Entity} kind={member.Kind} healthInitialized={member.HealthInitialized}");continue; }
                var health=exists?em.GetComponentData<UnitHealth>(member.Entity):default;
                if (health.Max>0) member.HealthInitialized=1;
                if (member.HealthInitialized!=0 && health.Current<=0) member.Dead=1;
                initialized+=member.HealthInitialized;members[i]=member;
                bool hostile=member.Kind is CitywideAlertMemberKind.ClinicHostile or CitywideAlertMemberKind.UtilityHostile or CitywideAlertMemberKind.HostileAir;
                if (hostile)
                {
                    hostiles++;defeated+=member.Dead;
                    if (member.Kind==CitywideAlertMemberKind.ClinicHostile) { clinic++;clinicDead+=member.Dead; }
                    if (member.Kind==CitywideAlertMemberKind.UtilityHostile) { utility++;utilityDead+=member.Dead; }
                    if (member.Kind==CitywideAlertMemberKind.HostileAir) { air++;airDead+=member.Dead; }
                    bool released=exists && !em.HasComponent<CampaignMissionCombatSuppressedTag>(member.Entity);
                    if (member.Dead==0 && released && em.HasComponent<LocalTransform>(member.Entity))
                    {
                        var position=em.GetComponentData<LocalTransform>(member.Entity).Position;
                        clinicBlocked|=math.distancesq(position.xz,mission.ClinicCenter.xz)<=math.square(mission.ServiceRadius*2);
                        utilityBlocked|=math.distancesq(position.xz,mission.UtilityCenter.xz)<=math.square(mission.ServiceRadius*2);
                    }
                    if (exists && em.HasComponent<SupportTargetEligibilityComponent>(member.Entity))
                    {
                        var knowledge=em.GetComponentData<SupportTargetEligibilityComponent>(member.Entity);byte known=(byte)(active&&released&&member.Dead==0?1:0);
                        if (knowledge.CurrentlyVisible!=known || knowledge.HostileConfirmed!=known) { knowledge.CurrentlyVisible=knowledge.HostileConfirmed=known;knowledge.SourceVersion++;em.SetComponentData(member.Entity,knowledge); }
                    }
                }
                else if (member.Kind is CitywideAlertMemberKind.ClinicStaff or CitywideAlertMemberKind.UtilityStaff) { staff++;staffDead+=member.Dead; }
                else { losses+=member.Dead;if (member.Dead==0) friendlyAlive++;
                    if (member.Kind is CitywideAlertMemberKind.ClinicEngineer or CitywideAlertMemberKind.UtilityEngineer && member.Dead!=0) engineerLost=true; }
            }
            if (members.Length!=CampaignMissionCitywideAlertRuleUtility.OriginalCount || hostiles!=11 || clinic!=5 || utility!=4 || air!=2 || staff!=4) MarkCitywideIntegrity(ref mission,$"roster={members.Length} hostile={hostiles} clinic={clinic} utility={utility} air={air} staff={staff}");
            if (staffDead>0) mission.Failure=CitywideAlertFailure.StaffLost;
            if (engineerLost) mission.Failure=CitywideAlertFailure.EngineerLost;
            if (friendlyAlive==0) mission.Failure=CitywideAlertFailure.ResponseLost;
            mission.ClinicThreatsCleared=(byte)(clinic==5&&clinicDead==5?1:0);mission.UtilityThreatsCleared=(byte)(utility==4&&utilityDead==4?1:0);
            mission.AirCleared=(byte)(air==2&&airDead==2?1:0);mission.MilitaryCleared=(byte)(hostiles==11&&defeated==11?1:0);
            if (active && CitywideStoppedAt(em,mission.AirDefense,mission.CoverageCenter,mission.CoverageRadius)) mission.CoverageReady=1;
            mission.ClinicObstructed=(byte)(clinicBlocked?1:0);mission.UtilityObstructed=(byte)(utilityBlocked?1:0);
            if (active && clinicBlocked) { mission.ClinicRecovered=0;mission.ClinicObstructionMilliseconds=SaturatingAddMilliseconds(mission.ClinicObstructionMilliseconds,SystemAPI.Time.DeltaTime); }
            if (active && utilityBlocked) { mission.UtilityRecovered=0;mission.UtilityObstructionMilliseconds=SaturatingAddMilliseconds(mission.UtilityObstructionMilliseconds,SystemAPI.Time.DeltaTime); }
            if (mission.ClinicObstructionMilliseconds>=CampaignMissionCitywideAlertRuleUtility.ObstructionGraceMilliseconds || mission.UtilityObstructionMilliseconds>=CampaignMissionCitywideAlertRuleUtility.ObstructionGraceMilliseconds) mission.Failure=CitywideAlertFailure.ServiceOutage;
            bool clinicQualified=active && mission.ClinicThreatsCleared!=0 && !clinicBlocked && CitywideStoppedAt(em,mission.ClinicEngineer,mission.ClinicRecoveryCenter,6);
            bool utilityQualified=active && mission.UtilityThreatsCleared!=0 && mission.AirCleared!=0 && !utilityBlocked && CitywideStoppedAt(em,mission.UtilityEngineer,mission.UtilityRecoveryCenter,6);
            mission.ClinicRecoveryMilliseconds=CampaignMissionCitywideAlertRuleUtility.AdvanceHold(mission.ClinicRecoveryMilliseconds,delta,clinicQualified);
            mission.UtilityRecoveryMilliseconds=CampaignMissionCitywideAlertRuleUtility.AdvanceHold(mission.UtilityRecoveryMilliseconds,delta,utilityQualified);
            if (mission.ClinicRecoveryMilliseconds>=6000) { mission.ClinicRecovered=1;mission.ClinicObstructionMilliseconds=0; }
            if (mission.UtilityRecoveryMilliseconds>=6000) { mission.UtilityRecovered=1;mission.UtilityObstructionMilliseconds=0; }
            ProjectCitywideProduction(em,ref mission);
            mission.ReinforcementReady=(byte)(mission.ReinforcementProduced!=0 && CitywideStoppedAt(em,mission.Reinforcement,mission.PerimeterCenter,mission.PerimeterRadius)?1:0);
            bool stable=active && mission.ClinicRecovered!=0 && mission.UtilityRecovered!=0 && mission.MilitaryCleared!=0 && mission.ReinforcementReady!=0 && mission.Failure==CitywideAlertFailure.None;
            mission.StableMilliseconds=CampaignMissionCitywideAlertRuleUtility.AdvanceHold(mission.StableMilliseconds,delta,stable);
            if (active && (clinicBlocked || utilityBlocked || facts.DefenseWaveActivated!=0)) mission.CommsReady=1;
            if (initialized==members.Length && mission.Clinic!=Entity.Null && mission.Utility!=Entity.Null && mission.ClinicProducer!=Entity.Null && mission.UtilityProducer!=Entity.Null && mission.ClinicReserve!=Entity.Null && mission.UtilityReserve!=Entity.Null) mission.Ready=1;
            facts.ElapsedMilliseconds=mission.ElapsedMilliseconds;facts.HostileTotalCount=hostiles;facts.HostileDefeatedCount=defeated;
            facts.SquadLossCount=losses;facts.CivilianTotalCount=staff;facts.CivilianLossCount=staffDead;facts.CommandSquadAlive=(byte)(friendlyAlive>0?1:0);
            facts.CitywideClinicRecovered=mission.ClinicRecovered;facts.CitywideUtilityRecovered=mission.UtilityRecovered;facts.CitywideReinforcementReady=mission.ReinforcementReady;
            facts.CitywideCommsReady=mission.CommsReady;facts.CitywideCoverageReady=mission.CoverageReady;facts.CitywideAirCleared=mission.AirCleared;facts.CitywideMilitaryCleared=mission.MilitaryCleared;
            facts.CitywideStableMilliseconds=mission.StableMilliseconds;facts.CitywideFailure=mission.Failure;
            if (mission.Failure==CitywideAlertFailure.Integrity) facts.HostileRosterIntegrityFault=1;
            if (em.HasBuffer<CampaignMissionDefenseMember>(root))
            {
                var defense=em.GetBuffer<CampaignMissionDefenseMember>(root);
                for (int i=0;i<defense.Length;i++) { var member=defense[i];foreach (var original in members) if (original.Entity==member.Entity) { member.HealthInitialized=original.HealthInitialized;member.Defeated=original.Dead;break; }defense[i]=member; }
            }
            em.SetComponentData(root,mission);em.SetComponentData(root,facts);
            if (CampaignMissionCitywideAlertRuleUtility.TryAdvance(in runtime,in facts,in mission,opening,out var next)) em.SetComponentData(root,next);
            return true;
        }
        internal static void QualifyCitywideEngineer(EntityManager em,Entity entity)
        {
            if(em.HasComponent<UnitCombat>(entity))
            {var combat=em.GetComponentData<UnitCombat>(entity);combat.CanAttack=combat.AutoEngage=0;em.SetComponentData(entity,combat);}
            if(em.HasComponent<ArmorBreakUnitActivationState>(entity))
            {var activation=em.GetComponentData<ArmorBreakUnitActivationState>(entity);activation.OriginalAutoEngage=0;em.SetComponentData(entity,activation);}
        }
        internal static void ProjectCitywideProducerReadiness(EntityManager em,Entity root,
            in CampaignMissionRuntimeComponent runtime,in CampaignMissionCitywideAlertState mission)
        {
            if(!em.HasComponent<CampaignMissionDefenseStateComponent>(root))return;
            var defense=em.GetComponentData<CampaignMissionDefenseStateComponent>(root);
            bool matches=runtime.MissionId.Equals(CitywideId)&&CampaignMissionCitywideAlertRuleUtility.Matches(in mission,in runtime)&&
                defense.SessionToken.Equals(runtime.SessionToken)&&defense.AttemptOrdinal==runtime.AttemptOrdinal&&defense.SourceVersion==runtime.SourceVersion;
            bool LiveProducer(Entity entity,int runtimeId)=>CitywideLive(em,entity)&&runtimeId>0&&
                !em.HasComponent<OperationMapBuildingComponent>(entity)&&em.HasComponent<RuntimeBuildingCombatInfo>(entity)&&
                em.GetComponentData<RuntimeBuildingCombatInfo>(entity).RuntimeBuildingId==runtimeId&&
                em.GetComponentData<RuntimeBuildingCombatInfo>(entity).OwnerFactionId==1&&em.HasComponent<UnitSourcePrefabKey>(entity)&&
                em.GetComponentData<UnitSourcePrefabKey>(entity).Value.Equals(new FixedString64Bytes("Building_Barrack"));
            defense.InitialProducerReady=(byte)(matches&&(LiveProducer(mission.ClinicProducer,mission.ClinicProducerRuntimeId)||
                LiveProducer(mission.UtilityProducer,mission.UtilityProducerRuntimeId))?1:0);
            em.SetComponentData(root,defense);
        }
        private static void MarkCitywideIntegrity(ref CampaignMissionCitywideAlertState mission,string detail)
        {
            if(mission.Failure!=CitywideAlertFailure.Integrity)
                UnityEngine.Debug.LogError($"[CitywideIntegrity] session={mission.SessionToken} attempt={mission.AttemptOrdinal} source={mission.SourceVersion} {detail}");
            mission.Failure=CitywideAlertFailure.Integrity;
        }
        private static bool CitywideLive(EntityManager em,Entity entity) => em.Exists(entity)&&em.HasComponent<UnitHealth>(entity)&&em.GetComponentData<UnitHealth>(entity).Max>0&&em.GetComponentData<UnitHealth>(entity).Current>0;
        internal static bool CitywideStoppedAt(EntityManager em,Entity entity,float3 center,float radius) => CitywideLive(em,entity)&&em.HasComponent<LocalTransform>(entity)&&
            !em.HasComponent<Disabled>(entity)&&!em.HasComponent<UnitTransportPassenger>(entity)&&!em.HasComponent<UnitPathRequest>(entity)&&!em.HasComponent<UnitPathFollow>(entity)&&
            math.distancesq(em.GetComponentData<LocalTransform>(entity).Position.xz,center.xz)<=radius*radius;
        private static void ApplyCitywideActivationPolicy(EntityManager em,Entity root,bool active)
        {
            foreach (var member in em.GetBuffer<CampaignMissionCitywideAlertMember>(root,true))
            {
                if (member.Dead!=0 || !em.Exists(member.Entity) || !em.HasComponent<ArmorBreakUnitActivationState>(member.Entity)) continue;
                var original=em.GetComponentData<ArmorBreakUnitActivationState>(member.Entity);
                if (em.HasComponent<UnitFuelConsumption>(member.Entity))
                {
                    var fuel=em.GetComponentData<UnitFuelConsumption>(member.Entity);byte enabled=active?original.OriginalFuelEnabled:(byte)0;
                    if (enabled!=fuel.Enabled) { fuel.Enabled=enabled;em.SetComponentData(member.Entity,fuel);if (enabled!=0&&em.HasComponent<UnitFuelConsumptionState>(member.Entity)) em.SetComponentData(member.Entity,default(UnitFuelConsumptionState)); }
                }
                if (em.HasComponent<UnitCombat>(member.Entity)) { var combat=em.GetComponentData<UnitCombat>(member.Entity);combat.AutoEngage=active?original.OriginalAutoEngage:(byte)0;em.SetComponentData(member.Entity,combat); }
            }
        }
        internal static void InitializeCitywideAlert(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime,ref OperationMapBlob map)
        {
            var mission=new CampaignMissionCitywideAlertState { SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal,SourceVersion=runtime.SourceVersion,Initialized=1,ServiceRadius=12,PerimeterRadius=14 };
            bool valid=CampaignMissionSpawnSystem.TryFindAnchor(ref map,"anchor.ch05.m01.clinic",out var clinic);
            valid&=CampaignMissionSpawnSystem.TryFindAnchor(ref map,"anchor.ch05.m01.utility",out var utilityAnchor);
            mission.ClinicCenter=clinic.Position;mission.UtilityCenter=utilityAnchor.Position;
            valid&=CampaignMissionSpawnSystem.TryFindAnchor(ref map,"anchor.ch05.m01.clinic_defense",out var clinicDefense);mission.ClinicDefense=clinicDefense.Position;
            valid&=CampaignMissionSpawnSystem.TryFindAnchor(ref map,"anchor.ch05.m01.utility_defense",out var utilityDefense);mission.UtilityDefense=utilityDefense.Position;
            valid&=CampaignMissionSpawnSystem.TryFindAnchor(ref map,"anchor.ch05.m01.coverage",out var coverage);mission.CoverageCenter=coverage.Position;mission.CoverageRadius=math.max(6,coverage.Radius);
            valid&=CampaignMissionSpawnSystem.TryFindAnchor(ref map,"anchor.ch05.m01.reinforcement_perimeter",out var perimeter);mission.PerimeterCenter=perimeter.Position;mission.PerimeterRadius=math.max(6,perimeter.Radius);
            valid&=CampaignMissionSpawnSystem.TryFindAnchor(ref map,"anchor.ch05.m01.clinic_recovery",out var clinicRecovery);mission.ClinicRecoveryCenter=clinicRecovery.Position;
            valid&=CampaignMissionSpawnSystem.TryFindAnchor(ref map,"anchor.ch05.m01.utility_recovery",out var utilityRecovery);mission.UtilityRecoveryCenter=utilityRecovery.Position;
            if (!valid) MarkCitywideIntegrity(ref mission,"required initialization anchor missing");
            if (!em.HasBuffer<CampaignMissionCitywideAlertMember>(root)) em.AddBuffer<CampaignMissionCitywideAlertMember>(root);
            var roster=em.GetBuffer<CampaignMissionCitywideAlertMember>(root);roster.Clear();
            using var query=em.CreateEntityQuery(typeof(CampaignMissionUnitRoleComponent),typeof(UnitHealth),typeof(LocalTransform));using var entities=query.ToEntityArray(Allocator.Temp);
            using var commands=new EntityCommandBuffer(Allocator.Temp);
            foreach (var entity in entities)
            {
                var role=em.GetComponentData<CampaignMissionUnitRoleComponent>(entity);if (!role.SessionToken.Equals(runtime.SessionToken)) continue;
                if (!TryCitywideKind(in role,out var kind)) { MarkCitywideIntegrity(ref mission,$"unrecognized role={role.MissionRoleId} group={role.UnitGroupId} entity={entity}");continue; }
                roster.Add(new CampaignMissionCitywideAlertMember { Entity=entity,Kind=kind });
                if (kind==CitywideAlertMemberKind.ClinicEngineer) mission.ClinicEngineer=entity;
                if (kind==CitywideAlertMemberKind.UtilityEngineer) mission.UtilityEngineer=entity;
                if (kind==CitywideAlertMemberKind.AirDefense) mission.AirDefense=entity;
                if (kind==CitywideAlertMemberKind.Radar) mission.Radar=entity;
                if(kind is CitywideAlertMemberKind.ClinicEngineer or CitywideAlertMemberKind.UtilityEngineer)
                    QualifyCitywideEngineer(em,entity);
                if (kind is CitywideAlertMemberKind.ClinicStaff or CitywideAlertMemberKind.UtilityStaff)
                {
                    if (!em.HasComponent<CampaignMissionStationaryUnitTag>(entity)) commands.AddComponent<CampaignMissionStationaryUnitTag>(entity);
                    if (em.HasComponent<UnitCombat>(entity)) { var combat=em.GetComponentData<UnitCombat>(entity);combat.CanAttack=combat.AutoEngage=0;em.SetComponentData(entity,combat); }
                }
                if (em.HasComponent<UnitMovementBehavior>(entity)) { var move=em.GetComponentData<UnitMovementBehavior>(entity);move.AllowIdleWander=0;em.SetComponentData(entity,move); }
                if (kind is CitywideAlertMemberKind.ClinicHostile or CitywideAlertMemberKind.UtilityHostile or CitywideAlertMemberKind.HostileAir)
                    if (em.HasComponent<UnitFuelConsumption>(entity)) { var fuel=em.GetComponentData<UnitFuelConsumption>(entity);fuel.Enabled=0;em.SetComponentData(entity,fuel); }
            }
            int rosterCount=roster.Length;
            commands.Playback(em);
            if (rosterCount!=27 || mission.ClinicEngineer==Entity.Null || mission.UtilityEngineer==Entity.Null || mission.AirDefense==Entity.Null || mission.Radar==Entity.Null) MarkCitywideIntegrity(ref mission,$"initialized roster={rosterCount} clinicEngineer={mission.ClinicEngineer} utilityEngineer={mission.UtilityEngineer} g2a={mission.AirDefense} radar={mission.Radar}");
            using var boundary=em.CreateEntityQuery(typeof(BuildingRuntimeStateTag));
            if (boundary.CalculateEntityCount()==1 && em.HasBuffer<BuildingProducedUnitReadModel>(boundary.GetSingletonEntity())) mission.ProducedBaseline=em.GetBuffer<BuildingProducedUnitReadModel>(boundary.GetSingletonEntity(),true).Length;
            if (!em.HasBuffer<CampaignMissionCitywideBuildingRequest>(root)) em.AddBuffer<CampaignMissionCitywideBuildingRequest>(root);
            var buildings=em.GetBuffer<CampaignMissionCitywideBuildingRequest>(root);buildings.Clear();for (byte i=0;i<6;i++) buildings.Add(new CampaignMissionCitywideBuildingRequest { Kind=i });
            if (em.HasComponent<CampaignMissionCitywideAlertState>(root)) em.SetComponentData(root,mission);else em.AddComponentData(root,mission);
        }
        private static bool TryCitywideKind(in CampaignMissionUnitRoleComponent role,out CitywideAlertMemberKind kind)
        {
            kind=role.MissionRoleId.ToString() switch {
                "role.friendly.response.clinic"=>CitywideAlertMemberKind.ClinicResponse,"role.friendly.response.utility"=>CitywideAlertMemberKind.UtilityResponse,
                "role.friendly.engineer.clinic"=>CitywideAlertMemberKind.ClinicEngineer,"role.friendly.engineer.utility"=>CitywideAlertMemberKind.UtilityEngineer,
                "role.friendly.g2a"=>CitywideAlertMemberKind.AirDefense,"role.friendly.radar"=>CitywideAlertMemberKind.Radar,
                "role.hostile.clinic"=>CitywideAlertMemberKind.ClinicHostile,"role.hostile.utility"=>CitywideAlertMemberKind.UtilityHostile,"role.hostile.air"=>CitywideAlertMemberKind.HostileAir,
                "role.civilian.protected"=>role.UnitGroupId.ToString().EndsWith("staff.clinic")?CitywideAlertMemberKind.ClinicStaff:role.UnitGroupId.ToString().EndsWith("staff.utility")?CitywideAlertMemberKind.UtilityStaff:(CitywideAlertMemberKind)255,
                _=>(CitywideAlertMemberKind)255 };
            return (byte)kind!=255;
        }
    }
}
